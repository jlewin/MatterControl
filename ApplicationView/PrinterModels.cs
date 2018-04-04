﻿/*
Copyright (c) 2017, Lars Brubaker, John Lewin
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The views and conclusions contained in the software and documentation are those
of the authors and should not be interpreted as representing official policies,
either expressed or implied, of the FreeBSD Project.
*/

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MatterControl.Printing;
using MatterHackers.Agg.UI;
using MatterHackers.MatterControl.DataStorage;
using MatterHackers.MatterControl.PrintQueue;
using MatterHackers.MatterControl.SlicerConfiguration;
using Newtonsoft.Json;

namespace MatterHackers.MatterControl
{
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Threading;
	using MatterHackers.Agg;
	using MatterHackers.DataConverters3D;
	using MatterHackers.GCodeVisualizer;
	using MatterHackers.Localizations;
	using MatterHackers.MatterControl.Library;
	using MatterHackers.MatterControl.PartPreviewWindow;
	using MatterHackers.MatterControl.PrinterCommunication;
	using MatterHackers.MeshVisualizer;
	using MatterHackers.PolygonMesh;
	using MatterHackers.VectorMath;

	public class BedConfig
	{
		public event EventHandler ActiveLayerChanged;

		public event EventHandler LoadedGCodeChanged;

		public event EventHandler SceneLoaded;

		public View3DConfig RendererOptions { get; } = new View3DConfig();

		public PrinterConfig Printer { get; set; }

		public EditContext EditContext { get; private set; }

		public Mesh PrinterShape { get; private set; }

		public BedConfig(PrinterConfig printer = null)
		{
			this.Printer = printer;
		}

		public async Task LoadContent(EditContext editContext)
		{
			// Make sure we don't have a selection
			this.Scene.SelectedItem = null;

			// Store
			this.EditContext = editContext;

			// Load
			if (editContext.SourceItem is ILibraryAssetStream contentStream
				&& contentStream.ContentType == "gcode")
			{
				using (var task = await contentStream.GetStream(null))
				{
					await LoadGCodeContent(task.Stream);
				}

				this.Scene.Children.Modify(children => children.Clear());
				this.EditableScene = false;
			}
			else
			{
				// Load last item or fall back to empty if unsuccessful
				editContext.Content = await editContext.SourceItem.CreateContent(null) ?? new Object3D();
				this.Scene.Load(editContext.Content);
				this.EditableScene = true;
			}

			// Notify
			this.SceneLoaded?.Invoke(this, null);
		}

		private async Task LoadGCodeContent(Stream stream)
		{
			await ApplicationController.Instance.Tasks.Execute("Loading G-Code".Localize(), (reporter, cancellationToken) =>
			{
				var progressStatus = new ProgressStatus();
				reporter.Report(progressStatus);

				this.LoadGCode(stream, cancellationToken, (progress0To1, status) =>
				{
					progressStatus.Status = status;
					progressStatus.Progress0To1 = progress0To1;
					reporter.Report(progressStatus);
				});

				return Task.CompletedTask;
			});
		}

		internal static ILibraryItem NewPlatingItem()
		{
			string now = "Workspace " + DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss");
			string mcxPath = Path.Combine(ApplicationDataStorage.Instance.PlatingDirectory, now + ".mcx");

			File.WriteAllText(mcxPath, new Object3D().ToJson());

			return new FileSystemFileItem(mcxPath);
		}

		public Task<ILibraryItem> ToPersistedLibraryItem(string newName)
		{
			// Save the scene to disk
			//await ApplicationController.Instance.Tasks.Execute("Saving".Localize(), this.SaveChanges);
			this.Scene.PersistAssets();

			return Task.FromResult<ILibraryItem>(new InMemoryLibraryItem(this.Scene)
			{
				Name = newName
			});
		}

		internal async Task ClearPlate()
		{
			// Clear existing
			this.LoadedGCode = null;
			this.GCodeRenderer = null;

			// Load
			await this.LoadContent(new EditContext()
			{
				ContentStore = ApplicationController.Instance.Library.PlatingHistory,
				SourceItem = BedConfig.NewPlatingItem()
			});
		}

		public InsertionGroup AddToPlate(IEnumerable<ILibraryItem> selectedLibraryItems)
		{
			InsertionGroup insertionGroup = null;

			var context = ApplicationController.Instance.DragDropData;
			var scene = context.SceneContext.Scene;
			scene.Children.Modify(list =>
			{
				list.Add(
					insertionGroup = new InsertionGroup(
						selectedLibraryItems,
						context.View3DWidget,
						scene,
						context.SceneContext.BedCenter,
						dragOperationActive: () => false));
			});

			return insertionGroup;
		}

		public async Task StashAndPrint(IEnumerable<ILibraryItem> selectedLibraryItems)
		{
			// Clear plate
			await this.ClearPlate();

			// Add content
			var insertionGroup = this.AddToPlate(selectedLibraryItems);
			await insertionGroup.LoadingItemsTask;

			// Persist changes
			await this.SaveChanges(null, CancellationToken.None);

			// Slice and print
			await ApplicationController.Instance.PrintPart(
				this.EditContext,
				this.Printer,
				null,
				CancellationToken.None);
		}

		internal static ILibraryItem GetLastPlateOrNew()
		{
			// Find the last used bed plate mcx
			var directoryInfo = new DirectoryInfo(ApplicationDataStorage.Instance.PlatingDirectory);
			var firstFile = directoryInfo.GetFileSystemInfos("*.mcx").OrderByDescending(fl => fl.LastWriteTime).FirstOrDefault();

			// Set as the current item - should be restored as the Active scene in the MeshViewer
			if (firstFile != null)
			{
				return new FileSystemFileItem(firstFile.FullName);
			}

			// Otherwise generate a new plating item
			return NewPlatingItem();
		}

		private GCodeFile loadedGCode;
		public GCodeFile LoadedGCode
		{
			get => loadedGCode;
			private set
			{
				if (loadedGCode != value)
				{
					loadedGCode = value;
					LoadedGCodeChanged?.Invoke(null, null);
				}
			}
		}

		internal void EnsureGCodeLoaded()
		{
			if (this.loadedGCode == null
				&& File.Exists(this.EditContext?.GCodeFilePath))
			{
				UiThread.RunOnIdle(async () =>
				{
					using (var stream = File.OpenRead(this.EditContext.GCodeFilePath))
					{
						await LoadGCodeContent(stream);
					}
				});
			}
		}

		public WorldView World { get; } = new WorldView(0, 0);

		public double BuildHeight  { get; internal set; }
		public Vector3 ViewerVolume { get; internal set; }
		public Vector2 BedCenter { get; internal set; }
		public BedShape BedShape { get; internal set; }

		// TODO: Make assignment private, wire up post slicing initialization here
		public GCodeRenderer GCodeRenderer { get; set; }

		private int _activeLayerIndex;
		public int ActiveLayerIndex
		{
			get => _activeLayerIndex;
			set
			{
				if (_activeLayerIndex != value)
				{
					_activeLayerIndex = value;

					// Clamp activeLayerIndex to valid range
					if (this.GCodeRenderer == null || _activeLayerIndex < 0)
					{
						_activeLayerIndex = 0;
					}
					else if (_activeLayerIndex >= this.LoadedGCode.LayerCount)
					{
						_activeLayerIndex = this.LoadedGCode.LayerCount - 1;
					}

					// When the active layer changes we update the selected range accordingly - constrain to applicable values
					if (this.RenderInfo != null)
					{
						// TODO: Unexpected that rendering layer 2 requires that we set the range to 0-3. Seems like model should be updated to allow 0-2 to mean render up to layer 2
						this.RenderInfo.EndLayerIndex = Math.Min(this.LoadedGCode == null ? 0 : this.LoadedGCode.LayerCount, Math.Max(_activeLayerIndex + 1, 1));
					}

					ActiveLayerChanged?.Invoke(this, null);
				}
			}
		}

		public InteractiveScene Scene { get; } = new InteractiveScene();

		public GCodeRenderInfo RenderInfo { get; set; }

		private Mesh _bedMesh;
		public Mesh Mesh
		{
			get
			{
				if (_bedMesh == null)
				{

					// Load bed and build volume meshes
					var bedGenerator = new BedMeshGenerator();
					(_bedMesh, _buildVolumeMesh) = bedGenerator.CreatePrintBedAndVolume(Printer);

					Task.Run(() =>
					{
						try
						{
							string url = Printer.Settings.GetValue("PrinterShapeUrl");
							string extension = Printer.Settings.GetValue("PrinterShapeExtension");

							if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(extension))
							{
								return;
							}

							using (var stream = ApplicationController.Instance.LoadHttpAsset(url))
							{
								var mesh = MeshFileIo.Load(stream, extension, CancellationToken.None).Mesh;

								BspNode bspTree = null;

								// if there is a chached bsp tree load it
								var meshHashCode = mesh.GetLongHashCode();
								string cachePath = ApplicationController.CacheablePath("MeshBspData", $"{meshHashCode}.bsp");
								if (File.Exists(cachePath))
								{
									JsonConvert.DeserializeObject<BspNode>(File.ReadAllText(cachePath));
								}
								else
								{
									// else calculate it
									bspTree = FaceBspTree.Create(mesh, 20, true);
									// and save it
									File.WriteAllText(cachePath, JsonConvert.SerializeObject(bspTree));
								}

								// set the mesh to use the new tree
								UiThread.RunOnIdle(() =>
								{
									mesh.FaceBspTree = bspTree;
									this.PrinterShape = mesh;

									// TODO: Need to send a notification that the mesh changed so the UI can pickup and render
								});
							}
						}
						catch { }
					});
				}

				return _bedMesh;
			}
		}

		private Mesh _buildVolumeMesh;

		public Mesh BuildVolumeMesh
		{
			get
			{
				return _buildVolumeMesh;
			}
		}

		public bool EditableScene { get; private set; }

		internal void RenderGCode3D(DrawEventArgs e)
		{
			if (this.RenderInfo != null)
			{
				// If needed, update the RenderType flags to match to current user selection
				if (RendererOptions.IsDirty)
				{
					this.RenderInfo.RefreshRenderType();
					RendererOptions.IsDirty = false;
				}

				this.GCodeRenderer.Render3D(this.RenderInfo, e);
			}
		}

		public void LoadGCode(string filePath, CancellationToken cancellationToken, Action<double, string> progressReporter)
		{
			if (File.Exists(filePath))
			{
				using (var stream = File.OpenRead(filePath))
				{
					this.LoadGCode(stream, cancellationToken, progressReporter);
				}
			}
		}

		private RenderType GetRenderType()
		{
			var options = this.RendererOptions;

			RenderType renderType = RenderType.Extrusions;

			if (options.RenderMoves)
			{
				renderType |= RenderType.Moves;
			}
			if (options.RenderRetractions)
			{
				renderType |= RenderType.Retractions;
			}

			if (options.GCodeLineColorStyle == "Speeds")
			{
				renderType |= RenderType.SpeedColors;
			}
			else if (options.GCodeLineColorStyle != "Materials")
			{
				renderType |= RenderType.GrayColors;
			}

			if (options.SimulateExtrusion)
			{
				renderType |= RenderType.SimulateExtrusion;
			}
			if (options.TransparentExtrusion)
			{
				renderType |= RenderType.TransparentExtrusion;
			}
			if (options.HideExtruderOffsets)
			{
				renderType |= RenderType.HideExtruderOffsets;
			}

			return renderType;
		}

		public void LoadGCode(Stream stream, CancellationToken cancellationToken, Action<double, string> progressReporter)
		{
			var settings = this.Printer.Settings;
			var maxAcceleration = settings.GetValue<double>(SettingsKey.max_acceleration);
			var maxVelocity = settings.GetValue<double>(SettingsKey.max_velocity);
			var jerkVelocity = settings.GetValue<double>(SettingsKey.jerk_velocity);
			var multiplier = settings.GetValue<double>(SettingsKey.print_time_estimate_multiplier) / 100.0;

			var loadedGCode = GCodeMemoryFile.Load(stream,
				new Vector4(maxAcceleration, maxAcceleration, maxAcceleration, maxAcceleration),
				new Vector4(maxVelocity, maxVelocity, maxVelocity, maxVelocity),
				new Vector4(jerkVelocity, jerkVelocity, jerkVelocity, jerkVelocity),
				new Vector4(multiplier, multiplier, multiplier, multiplier),
				cancellationToken, progressReporter);
			this.GCodeRenderer = new GCodeRenderer(loadedGCode);
			this.RenderInfo = new GCodeRenderInfo(
					0,
					Math.Max(1, this.ActiveLayerIndex),
					Agg.Transform.Affine.NewIdentity(),
					1,
					0,
					1,
					new Vector2[]
					{
						settings.Helpers.ExtruderOffset(0),
						settings.Helpers.ExtruderOffset(1)
					},
					this.GetRenderType,
					MeshViewerWidget.GetExtruderColor);

			if (ActiveSliceSettings.Instance.PrinterSelected)
			{
				GCodeRenderer.ExtruderWidth = ActiveSliceSettings.Instance.GetValue<double>(SettingsKey.nozzle_diameter);
			}
			else
			{
				GCodeRenderer.ExtruderWidth = .4;
			}

			try
			{
				// TODO: After loading we reprocess the entire document just to compute filament used. If it's a feature we need, seems like it should just be normal step during load and result stored in a property
				GCodeRenderer.GCodeFileToDraw?.GetFilamentUsedMm(ActiveSliceSettings.Instance.GetValue<double>(SettingsKey.filament_diameter));
			}
			catch (Exception ex)
			{
				Debug.Print(ex.Message);
			}

			// Assign property causing event and UI load
			this.LoadedGCode = loadedGCode;

			// Constrain to max layers
			if (this.ActiveLayerIndex > loadedGCode.LayerCount)
			{
				this.ActiveLayerIndex = loadedGCode.LayerCount;
			}

			ActiveLayerChanged?.Invoke(this, null);
		}

		public void InvalidateBedMesh()
		{
			// Invalidate bed mesh cache
			_bedMesh = null;
		}

		/// <summary>
		/// Persists modified meshes to assets and saves pending changes back to the EditContext
		/// </summary>
		/// <param name="progress"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public Task SaveChanges(IProgress<ProgressStatus> progress, CancellationToken cancellationToken)
		{
			var progressStatus = new ProgressStatus()
			{
				Status = "Saving Changes"
			};

			progress?.Report(progressStatus);

			if (this.Scene.Persistable)
			{
				this.Scene.PersistAssets((progress0to1, status) =>
				{
					if (progress != null)
					{
						progressStatus.Status = status;
						progressStatus.Progress0To1 = progress0to1;
						progress.Report(progressStatus);
					}
				});

				this.EditContext?.Save();
			}

			return Task.CompletedTask;
		}

		public List<BoolOption> GetBaseViewOptions()
		{
			var viewOptions = new List<BoolOption>
			{
				new BoolOption(
					"Show Print Bed".Localize(),
					() => this.RendererOptions.RenderBed,
					(value) =>
					{
						this.RendererOptions.RenderBed = value;
					})
			};

			if (this.BuildHeight > 0
				&& this.Printer?.ViewState.ViewMode != PartViewMode.Layers2D)
			{
				viewOptions.Add(
					new BoolOption(
						"Show Print Area".Localize(),
						() => this.RendererOptions.RenderBuildVolume,
						(value) => this.RendererOptions.RenderBuildVolume = value));
			}

			return viewOptions;
		}
	}

	public class EditContext
	{
		private ILibraryItem _sourceItem;

		public IContentStore ContentStore { get; set; }

		public ILibraryItem SourceItem
		{
			get => _sourceItem;
			set
			{
				if (_sourceItem != value)
				{
					_sourceItem = value;

					if (_sourceItem is FileSystemFileItem fileItem)
					{
						printItem = new PrintItemWrapper(new PrintItem(fileItem.FileName, fileItem.Path));
					}
				}
			}
		}

		public IObject3D Content { get; set; }

		public string GCodeFilePath => printItem?.GetGCodePathAndFileName();

		public string SourceFilePath => printItem?.FileLocation;

		/// <summary>
		/// Short term stop gap that should only be used until GCode path helpers, hash code and print recovery components can be extracted
		/// </summary>
		[Obsolete]
		internal PrintItemWrapper printItem { get; set; }

		internal void Save()
		{
			if (this.ContentStore != null)
			{
				var thumbnailPath = ApplicationController.Instance.ThumbnailCachePath(this.SourceItem);
				if (File.Exists(thumbnailPath))
				{
					File.Delete(thumbnailPath);
				}

				// Purge any specifically sized thumbnails
				foreach(var sizedThumbnail in Directory.GetFiles(Path.GetDirectoryName(thumbnailPath), Path.GetFileNameWithoutExtension(thumbnailPath) + "-*.png"))
				{
					File.Delete(sizedThumbnail);
				}

				// Call save on the provider
				this.ContentStore.Save(this.SourceItem, this.Content);
			}
		}
	}

	public class PrinterViewState
	{
		public event EventHandler<ViewModeChangedEventArgs> ViewModeChanged;

		public event EventHandler ConfigurePrinterChanged;

		public bool SliceSettingsTabPinned
		{
			get => UserSettings.Instance.get(UserSettingsKey.SliceSettingsTabPinned) == "true";
			set
			{
				UserSettings.Instance.set(UserSettingsKey.SliceSettingsTabPinned, value ? "true" : "false");
			}
		}

		public int SliceSettingsTabIndex
		{
			get
			{
				int.TryParse(UserSettings.Instance.get(UserSettingsKey.SliceSettingsTabIndex), out int tabIndex);
				return tabIndex;
			}
			set
			{
				UserSettings.Instance.set(UserSettingsKey.SliceSettingsTabIndex, value.ToString());
			}
		}

		public bool DockWindowFloating { get; internal set; }

		double DefaultSliceSettingsWidth => 450;
		public double SliceSettingsWidth
		{
			get
			{
				double.TryParse(UserSettings.Instance.get(UserSettingsKey.SliceSettingsWidth), out double controlWidth);
				if(controlWidth == 0)
				{
					controlWidth = DefaultSliceSettingsWidth;
				}
				return controlWidth;
			}
			set
			{
				UserSettings.Instance.set(UserSettingsKey.SliceSettingsWidth, value.ToString());
			}
		}

		private PartViewMode viewMode = PartViewMode.Model;
		public PartViewMode ViewMode
		{
			get => viewMode;
			set
			{
				if (viewMode != value)
				{
					viewMode = value;

					ViewModeChanged?.Invoke(this, new ViewModeChangedEventArgs()
					{
						ViewMode = this.ViewMode
					});
				}
			}
		}

		public bool _configurePrinterVisible = UserSettings.Instance.get(UserSettingsKey.ConfigurePrinterTabVisible) == "true";

		public bool ConfigurePrinterVisible
		{
			get => _configurePrinterVisible;
			set
			{
				if (_configurePrinterVisible != value)
				{
					if (value)
					{
						this.SliceSettingsTabIndex = 3;
					}

					_configurePrinterVisible = value;

					UserSettings.Instance.set(UserSettingsKey.ConfigurePrinterTabVisible, _configurePrinterVisible ? "true" : "false");

					ConfigurePrinterChanged?.Invoke(this, null);
				}
			}
		}

		public double SelectedObjectPanelWidth
		{
			get
			{
				if (double.TryParse(UserSettings.Instance.get(UserSettingsKey.SelectedObjectPanelWidth), out double controlWidth))
				{
					return Math.Max(controlWidth, 150);
				}

				return 200;
			}
			set
			{
				var minimumValue = Math.Max(value, 150);
				UserSettings.Instance.set(UserSettingsKey.SelectedObjectPanelWidth, minimumValue.ToString());
			}
		}

		public double GCodePanelWidth
		{
			get
			{
				if (double.TryParse(UserSettings.Instance.get(UserSettingsKey.GCodePanelWidth), out double controlWidth))
				{
					return Math.Max(controlWidth, 200);
				}

				return 200;
			}
			set
			{
				var minimumValue = Math.Max(value, 200);
				UserSettings.Instance.set(UserSettingsKey.GCodePanelWidth, minimumValue.ToString());
			}
		}
	}

	public class PrinterConfig
	{
		public BedConfig Bed { get; }

		private EventHandler unregisterEvents;

		public PrinterConfig(PrinterSettings settings)
		{
			this.Bed = new BedConfig(this);
			this.Connection = new PrinterConnection(printer: this);
			this.Settings = settings;
			this.Settings.printer = this;

			// TODO: ActiveSliceSettings is not our Settings! Move SettingsChanged to instance rather than static
			ActiveSliceSettings.SettingChanged.RegisterEvent(Printer_SettingChanged, ref unregisterEvents);

			this.Connection.PrintFinished.RegisterEvent((s, e) =>
			{
				// clear single use setting on print completion
				foreach (var keyValue in this.Settings.BaseLayer)
				{
					string currentValue = this.Settings.GetValue(keyValue.Key);

					bool valueIsClear = currentValue == "0" | currentValue == "";

					SliceSettingData data = SettingsOrganizer.Instance.GetSettingsData(keyValue.Key);
					if (data?.ResetAtEndOfPrint == true && !valueIsClear)
					{
						this.Settings.ClearValue(keyValue.Key);
					}
				}
			}, ref unregisterEvents);

			if (!string.IsNullOrEmpty(this.Settings.GetValue(SettingsKey.baud_rate)))
			{
				this.Connection.BaudRate = this.Settings.GetValue<int>(SettingsKey.baud_rate);
			}
			this.Connection.ConnectGCode = this.Settings.GetValue(SettingsKey.connect_gcode);
			this.Connection.CancelGCode = this.Settings.GetValue(SettingsKey.cancel_gcode);
			this.Connection.EnableNetworkPrinting = this.Settings.GetValue<bool>(SettingsKey.enable_network_printing);
			this.Connection.AutoReleaseMotors = this.Settings.GetValue<bool>(SettingsKey.auto_release_motors);
			this.Connection.RecoveryIsEnabled = this.Settings.GetValue<bool>(SettingsKey.recover_is_enabled);
			this.Connection.ExtruderCount = this.Settings.GetValue<int>(SettingsKey.extruder_count);
			this.Connection.SendWithChecksum = this.Settings.GetValue<bool>(SettingsKey.send_with_checksum);
			this.Connection.ReadLineReplacementString = this.Settings.GetValue(SettingsKey.read_regex);
		}

		public PrinterViewState ViewState { get; } = new PrinterViewState();

		private PrinterSettings _settings;
		public PrinterSettings Settings
		{
			get => _settings;
			private set
			{
				if (_settings != value)
				{
					_settings = value;
					this.ReloadSettings();
					this.Bed.InvalidateBedMesh();
				}
			}
		}

		public PrinterConnection Connection { get; private set; }

		public string PrinterConnectionStatus
		{
			get
			{
				switch (this.Connection.CommunicationState)
				{
					case CommunicationStates.Disconnected:
						return "Not Connected".Localize();

					case CommunicationStates.Disconnecting:
						return "Disconnecting".Localize();

					case CommunicationStates.AttemptingToConnect:
						return "Connecting".Localize() + "...";

					case CommunicationStates.ConnectionLost:
						return "Connection Lost".Localize();

					case CommunicationStates.FailedToConnect:
						return "Unable to Connect".Localize();

					case CommunicationStates.Connected:
						return "Connected".Localize();

					case CommunicationStates.PreparingToPrint:
						return "Preparing To Print".Localize();

					case CommunicationStates.Printing:
						switch (this.Connection.DetailedPrintingState)
						{
							case DetailedPrintingState.HomingAxis:
								return "Homing".Localize();

							case DetailedPrintingState.HeatingBed:
								return "Waiting for Bed to Heat to".Localize() + $" {this.Connection.TargetBedTemperature}°";

							case DetailedPrintingState.HeatingExtruder:
								return "Waiting for Extruder to Heat to".Localize() + $" {this.Connection.GetTargetHotendTemperature(0)}°";

							case DetailedPrintingState.Printing:
							default:
								return "Printing".Localize();
						}

					case CommunicationStates.PrintingFromSd:
						return "Printing From SD Card".Localize();

					case CommunicationStates.Paused:
						return "Paused".Localize();

					case CommunicationStates.FinishedPrint:
						return "Finished Print".Localize();

					default:
						throw new NotImplementedException("Make sure every status returns the correct connected state.");
				}
			}
		}

		internal void SwapToSettings(PrinterSettings printerSettings)
		{
			_settings = printerSettings;
			ApplicationController.Instance.ReloadAll();
		}

		private void ReloadSettings()
		{
			this.Bed.BuildHeight = this.Settings.GetValue<double>(SettingsKey.build_height);
			this.Bed.ViewerVolume = new Vector3(this.Settings.GetValue<Vector2>(SettingsKey.bed_size), this.Bed.BuildHeight);
			this.Bed.BedCenter = this.Settings.GetValue<Vector2>(SettingsKey.print_center);
			this.Bed.BedShape = this.Settings.GetValue<BedShape>(SettingsKey.bed_shape);
		}

		private void Printer_SettingChanged(object sender, EventArgs e)
		{
			if (e is StringEventArgs stringEvent)
			{
				if (stringEvent.Data == SettingsKey.bed_size
					|| stringEvent.Data == SettingsKey.print_center
					|| stringEvent.Data == SettingsKey.build_height
					|| stringEvent.Data == SettingsKey.bed_shape)
				{
					this.ReloadSettings();
					this.Bed.InvalidateBedMesh();
				}

				// Sync settings changes to printer connection
				switch(stringEvent.Data)
				{
					case SettingsKey.feedrate_ratio:
						this.Connection.FeedRateRatio = this.Settings.GetValue<double>(SettingsKey.feedrate_ratio);
						break;

					case SettingsKey.baud_rate:
						if (!string.IsNullOrEmpty(this.Settings.GetValue(SettingsKey.baud_rate)))
						{
							this.Connection.BaudRate = this.Settings.GetValue<int>(SettingsKey.baud_rate);
						}
						break;

					case SettingsKey.connect_gcode:
						this.Connection.ConnectGCode = this.Settings.GetValue(SettingsKey.connect_gcode);
						break;

					case SettingsKey.cancel_gcode:
						this.Connection.CancelGCode = this.Settings.GetValue(SettingsKey.cancel_gcode);
						break;

					case SettingsKey.enable_network_printing:
						this.Connection.EnableNetworkPrinting = this.Settings.GetValue<bool>(SettingsKey.enable_network_printing);
						break;

					case SettingsKey.auto_release_motors:
						this.Connection.AutoReleaseMotors = this.Settings.GetValue<bool>(SettingsKey.auto_release_motors);
						break;

					case SettingsKey.recover_is_enabled:
						this.Connection.RecoveryIsEnabled = this.Settings.GetValue<bool>(SettingsKey.recover_is_enabled);
						break;

					case SettingsKey.extruder_count:
						this.Connection.ExtruderCount = this.Settings.GetValue<int>(SettingsKey.extruder_count);
						break;

					case SettingsKey.send_with_checksum:
						this.Connection.SendWithChecksum = this.Settings.GetValue<bool>(SettingsKey.send_with_checksum);
						break;

					case SettingsKey.read_regex:
						this.Connection.ReadLineReplacementString = this.Settings.GetValue(SettingsKey.read_regex);
						break;
				}
			}
		}

		/// <summary>
		/// Loads content to the bed and prepares edit/persistence context for use
		/// </summary>
		/// <param name="editContext"></param>
		/// <returns></returns>
		internal async Task LoadPlateFromHistory()
		{
			await this.Bed.LoadContent(new EditContext()
			{
				ContentStore = ApplicationController.Instance.Library.PlatingHistory,
				SourceItem = BedConfig.GetLastPlateOrNew()
			});
		}
	}

	public class View3DConfig : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public bool IsDirty { get; internal set; }

		public bool RenderBed
		{
			get
			{
				string value = UserSettings.Instance.get("GcodeViewerRenderGrid");
				if (value == null)
				{
					return true;
				}
				return (value == "True");
			}
			set
			{
				if (this.RenderBed != value)
				{
					UserSettings.Instance.set("GcodeViewerRenderGrid", value.ToString());
					this.IsDirty = true;
					this.OnPropertyChanged(nameof(RenderBed));
				}
			}
		}

		public bool RenderMoves
		{
			get => UserSettings.Instance.get("GcodeViewerRenderMoves") == "True";
			set
			{
				if (this.RenderMoves != value)
				{
					UserSettings.Instance.set("GcodeViewerRenderMoves", value.ToString());
					this.IsDirty = true;
					this.OnPropertyChanged(nameof(RenderMoves));
				}
			}
		}

		public bool RenderRetractions
		{
			get => UserSettings.Instance.get("GcodeViewerRenderRetractions") == "True";
			set
			{
				if (this.RenderRetractions != value)
				{
					UserSettings.Instance.set("GcodeViewerRenderRetractions", value.ToString());
					this.IsDirty = true;
					this.OnPropertyChanged(nameof(RenderRetractions));
				}
			}
		}

		public string GCodeModelView
		{
			get => UserSettings.Instance.get("GcodeModelView");
			set
			{
				if (this.GCodeModelView != value)
				{
					UserSettings.Instance.set("GcodeModelView", value);
					this.IsDirty = true;
					this.OnPropertyChanged(nameof(GCodeModelView));
				}
			}
		}

		public string GCodeLineColorStyle
		{
			get => UserSettings.Instance.get("GCodeLineColorStyle");
			set
			{
				if (this.GCodeLineColorStyle != value)
				{
					UserSettings.Instance.set("GCodeLineColorStyle", value);
					this.IsDirty = true;
					this.OnPropertyChanged(nameof(GCodeLineColorStyle));
				}
			}
		}

		public bool SimulateExtrusion
		{
			get => UserSettings.Instance.get("GcodeViewerSimulateExtrusion") == "True";
			set
			{
				if (this.SimulateExtrusion != value)
				{
					UserSettings.Instance.set("GcodeViewerSimulateExtrusion", value.ToString());
					this.IsDirty = true;
					this.OnPropertyChanged(nameof(SimulateExtrusion));
				}
			}
		}

		public bool TransparentExtrusion
		{
			get => UserSettings.Instance.get("GcodeViewerTransparentExtrusion") == "True";
			set
			{
				if (this.TransparentExtrusion != value)
				{
					UserSettings.Instance.set("GcodeViewerTransparentExtrusion", value.ToString());
					this.IsDirty = true;
					this.OnPropertyChanged(nameof(TransparentExtrusion));
				}
			}
		}

		public bool HideExtruderOffsets
		{
			get
			{
				string value = UserSettings.Instance.get("GcodeViewerHideExtruderOffsets");
				if (value == null)
				{
					return true;
				}
				return (value == "True");
			}
			set
			{
				if (this.HideExtruderOffsets != value)
				{
					UserSettings.Instance.set("GcodeViewerHideExtruderOffsets", value.ToString());
					this.IsDirty = true;
					this.OnPropertyChanged(nameof(HideExtruderOffsets));
				}
			}
		}

		public bool SyncToPrint
		{
			get => UserSettings.Instance.get("LayerViewSyncToPrint") == "True";
			set
			{
				if (this.SyncToPrint != value)
				{
					UserSettings.Instance.set("LayerViewSyncToPrint", value.ToString());
					this.IsDirty = true;
					this.OnPropertyChanged(nameof(SyncToPrint));
				}
			}
		}

		private bool _renderBuildVolume;
		public bool RenderBuildVolume
		{
			get => _renderBuildVolume;
			set
			{
				if (_renderBuildVolume != value)
				{
					_renderBuildVolume = value;
					this.OnPropertyChanged(nameof(RenderBuildVolume));
				}
			}
		}

		protected void OnPropertyChanged(string name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}