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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MatterHackers.Agg;
using MatterHackers.Agg.Image;
using MatterHackers.Agg.ImageProcessing;
using MatterHackers.Agg.Platform;
using MatterHackers.Agg.UI;
using MatterHackers.DataConverters3D;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.CustomWidgets;
using MatterHackers.MatterControl.DataStorage;
using MatterHackers.MatterControl.Library;
using MatterHackers.MeshVisualizer;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl.PartPreviewWindow
{
	public enum ViewControls3DButtons
	{
		Rotate,
		Scale,
		Translate,
		PartSelect
	}

	public enum PartViewMode
	{
		Layers2D,
		Layers3D,
		Model
	}

	public class ViewModeChangedEventArgs : EventArgs
	{
		public PartViewMode ViewMode { get; set; }
	}

	public class TransformStateChangedEventArgs : EventArgs
	{
		public ViewControls3DButtons TransformMode { get; set; }
	}

	public class ViewControls3D : OverflowBar
	{
		public event EventHandler ResetView;

		public event EventHandler<TransformStateChangedEventArgs> TransformStateChanged;

		private RadioIconButton translateButton;
		private RadioIconButton rotateButton;
		private RadioIconButton scaleButton;
		private RadioIconButton partSelectButton;

		private View3DWidget view3DWidget;
		private BedConfig sceneContext;

		private ViewControls3DButtons activeTransformState = ViewControls3DButtons.PartSelect;
		private List<(GuiWidget button, SceneSelectionOperation operation)> operationButtons;

		public bool IsPrinterMode { get; }

		public ViewControls3DButtons ActiveButton
		{
			get
			{
				return activeTransformState;
			}
			set
			{
				this.activeTransformState = value;
				switch (this.activeTransformState)
				{
					case ViewControls3DButtons.Rotate:
						if (rotateButton != null)
						{
							rotateButton.Checked = true;
						}

						break;

					case ViewControls3DButtons.Translate:
						if (translateButton != null)
						{
							translateButton.Checked = true;
						}

						break;

					case ViewControls3DButtons.Scale:
						if (scaleButton != null)
						{
							scaleButton.Checked = true;
						}

						break;

					case ViewControls3DButtons.PartSelect:
						if (partSelectButton != null)
						{
							partSelectButton.Checked = true;
						}
						break;
				}

				TransformStateChanged?.Invoke(this, new TransformStateChangedEventArgs()
				{
					TransformMode = activeTransformState
				});
			}
		}

		internal void SetView3DWidget(View3DWidget view3DWidget)
		{
			this.view3DWidget = view3DWidget;
		}

		public ViewControls3D(BedConfig sceneContext, ThemeConfig theme, UndoBuffer undoBuffer, bool isPrinterType)
			: base (theme)
		{
			this.ActionArea.Click += (s, e) =>
			{
				view3DWidget.InteractionLayer.Focus();
			};

			this.IsPrinterMode = isPrinterType;
			this.sceneContext = sceneContext;

			string iconPath;

			var commonMargin = theme.ButtonSpacing;

			double height = theme.ButtonFactory.Options.FixedHeight;

			this.AddChild(CreateBedButton(sceneContext, theme));

			this.AddChild(new VerticalLine(50)
			{
				Margin = 4
			});

			var homeButton = new IconButton(AggContext.StaticData.LoadIcon("fa-home_16.png", theme.InvertIcons), theme)
			{
				ToolTipText = "Reset View".Localize(),
				Margin = commonMargin
			};
			homeButton.Click += (s, e) => ResetView?.Invoke(this, null);
			AddChild(homeButton);

			var undoButton = new IconButton(AggContext.StaticData.LoadIcon("Undo_grey_16x.png", 16, 16, theme.InvertIcons), theme)
			{
				Name = "3D View Undo",
				ToolTipText = "Undo",
				Enabled = false,
				MinimumSize = new Vector2(height, height),
				Margin = commonMargin,
				VAnchor = VAnchor.Center
			};
			undoButton.Click += (sender, e) =>
			{
				sceneContext.Scene.SelectedItem = null;
				undoBuffer.Undo();
				view3DWidget.InteractionLayer.Focus();
			};
			this.AddChild(undoButton);

			var redoButton = new IconButton(AggContext.StaticData.LoadIcon("Redo_grey_16x.png", 16, 16, theme.InvertIcons), theme)
			{
				Name = "3D View Redo",
				Margin = commonMargin,
				MinimumSize = new Vector2(height, height),
				ToolTipText = "Redo",
				Enabled = false,
				VAnchor = VAnchor.Center
			};
			redoButton.Click += (sender, e) =>
			{
				undoBuffer.Redo();
				view3DWidget.InteractionLayer.Focus();
			};
			this.AddChild(redoButton);

			this.AddChild(new VerticalLine(50)
			{
				Margin = 4
			});

			undoBuffer.Changed += (sender, e) =>
			{
				undoButton.Enabled = undoBuffer.UndoCount > 0;
				redoButton.Enabled = undoBuffer.RedoCount > 0;
			};

			var buttonGroupA = new ObservableCollection<GuiWidget>();

			if (UserSettings.Instance.IsTouchScreen)
			{
				iconPath = Path.Combine("ViewTransformControls", "rotate.png");
				rotateButton = new RadioIconButton(AggContext.StaticData.LoadIcon(iconPath, 32, 32, theme.InvertIcons), theme)
				{
					SiblingRadioButtonList = buttonGroupA,
					ToolTipText = "Rotate (Alt + Left Mouse)".Localize(),
					Margin = commonMargin
				};
				rotateButton.Click += (s, e) => this.ActiveButton = ViewControls3DButtons.Rotate;
				buttonGroupA.Add(rotateButton);
				AddChild(rotateButton);

				iconPath = Path.Combine("ViewTransformControls", "translate.png");
				translateButton = new RadioIconButton(AggContext.StaticData.LoadIcon(iconPath, 32, 32, theme.InvertIcons), theme)
				{
					SiblingRadioButtonList = buttonGroupA,
					ToolTipText = "Move (Shift + Left Mouse)".Localize(),
					Margin = commonMargin
				};
				translateButton.Click += (s, e) => this.ActiveButton = ViewControls3DButtons.Translate;
				buttonGroupA.Add(translateButton);
				AddChild(translateButton);

				iconPath = Path.Combine("ViewTransformControls", "scale.png");
				scaleButton = new RadioIconButton(AggContext.StaticData.LoadIcon(iconPath, 32, 32, theme.InvertIcons), theme)
				{
					SiblingRadioButtonList = buttonGroupA,
					ToolTipText = "Zoom (Ctrl + Left Mouse)".Localize(),
					Margin = commonMargin
				};
				scaleButton.Click += (s, e) => this.ActiveButton = ViewControls3DButtons.Scale;
				buttonGroupA.Add(scaleButton);
				AddChild(scaleButton);

				rotateButton.Checked = true;

				// Add vertical separator
				this.AddChild(new VerticalLine(50)
				{
					Margin = 3
				});

				iconPath = Path.Combine("ViewTransformControls", "partSelect.png");
				partSelectButton = new RadioIconButton(AggContext.StaticData.LoadIcon(iconPath, 32, 32, theme.InvertIcons), theme)
				{
					SiblingRadioButtonList = buttonGroupA,
					ToolTipText = "Select Part".Localize(),
					Margin = commonMargin
				};
				partSelectButton.Click += (s, e) => this.ActiveButton = ViewControls3DButtons.PartSelect;
				buttonGroupA.Add(partSelectButton);
				AddChild(partSelectButton);
			}

			operationButtons = new List<(GuiWidget, SceneSelectionOperation)>();

			// Add Selected IObject3D -> Operations to toolbar
			foreach (var namedAction in ApplicationController.Instance.RegisteredSceneOperations)
			{
				if (namedAction is SceneSelectionSeparator)
				{
					var margin = new BorderDouble(3);
					margin = margin.Clone(left: margin.Left + theme.ButtonSpacing.Left);

					this.AddChild(new VerticalLine(50)
					{
						Margin = margin
					});

					continue;
				}

				GuiWidget button;

				if (namedAction.Icon != null)
				{
					button = new IconButton(namedAction.Icon, theme)
					{
						Name = namedAction.Title + " Button",
						ToolTipText = namedAction.Title,
						Margin = theme.ButtonSpacing,
						BackgroundColor = theme.ToolbarButtonBackground,
						HoverColor = theme.ToolbarButtonHover,
						MouseDownColor = theme.ToolbarButtonDown,
					};
				}
				else
				{
					button = new TextButton(namedAction.Title, theme)
					{
						Name = namedAction.Title + " Button",
						Margin = theme.ButtonSpacing,
						BackgroundColor = theme.ToolbarButtonBackground,
						HoverColor = theme.ToolbarButtonHover,
						MouseDownColor = theme.ToolbarButtonDown,
					};
				}

				operationButtons.Add((button, namedAction));

				button.Click += (s, e) =>
				{
					namedAction.Action.Invoke(sceneContext.Scene);
					var partTab = button.Parents<PartTabPage>().FirstOrDefault();
					var view3D = partTab.Descendants<View3DWidget>().FirstOrDefault();
					view3D.InteractionLayer.Focus();
				};
				this.AddChild(button);
			}

			//////////////////// maybe set default printer view mode?

			sceneContext.Scene.SelectionChanged += Scene_SelectionChanged;

			// Run on load
			Scene_SelectionChanged(null, null);
		}

		private void Scene_SelectionChanged(object sender, EventArgs e)
		{
			// Set enabled level based on operation rules
			foreach(var item in operationButtons)
			{
				item.button.Enabled = item.operation.IsEnabled?.Invoke(sceneContext.Scene) ?? false;
			}
		}

		private GuiWidget CreateBedButton(BedConfig sceneContext, ThemeConfig theme)
		{
			var buttonView = new TextIconButton(
				(IsPrinterMode) ? "Bed".Localize() : "Part".Localize(),
				AggContext.StaticData.LoadIcon((IsPrinterMode) ? "bed.png" : "cube.png", theme.InvertIcons),
				theme);

			// Remove right Padding for drop style
			buttonView.Padding = buttonView.Padding.Clone(right: 0);

			var popupMenu = new PopupMenu(ApplicationController.Instance.MenuTheme)
			{
				HAnchor = HAnchor.Fit,
				VAnchor = VAnchor.Fit,
			};

			return new PopupMenuButton(buttonView, theme)
			{
				Name = "Bed Options Menu",
				DynamicPopupContent = () =>
				{
					var menuContent = theme.CreateMenuItems(popupMenu, this.BedMenuActions(sceneContext, ApplicationController.Instance.MenuTheme));
					menuContent.MinimumSize = new Vector2(200, 0);

					return menuContent;
				},
				BackgroundColor = theme.ToolbarButtonBackground,
				HoverColor = theme.ToolbarButtonHover,
				MouseDownColor = theme.ToolbarButtonDown,
				DrawArrow = true,
				Margin = theme.ButtonSpacing,
			};
		}

		private async void LoadAndAddPartsToPlate(string[] filesToLoad, InteractiveScene scene)
		{
			if (filesToLoad != null && filesToLoad.Length > 0)
			{
				await Task.Run(() => loadAndAddPartsToPlate(filesToLoad, scene));

				if (HasBeenClosed)
				{
					return;
				}

				bool addingOnlyOneItem = scene.Children.Count == scene.Children.Count + 1;

				if (scene.HasChildren())
				{
					if (addingOnlyOneItem)
					{
						// if we are only adding one part to the plate set the selection to it
						scene.SelectLastChild();
					}
				}

				scene.Invalidate();
				this.Invalidate();
			}
		}

		private async Task loadAndAddPartsToPlate(string[] filesToLoadIncludingZips, InteractiveScene scene)
		{
			if (filesToLoadIncludingZips?.Any() == true)
			{
				List<string> filesToLoad = new List<string>();
				foreach (string loadedFileName in filesToLoadIncludingZips)
				{
					string extension = Path.GetExtension(loadedFileName).ToUpper();
					if ((extension != ""
						&& extension != ".ZIP"
						&& ApplicationController.Instance.Library.IsContentFileType(loadedFileName)))
					{
						filesToLoad.Add(loadedFileName);
					}
					else if (extension == ".ZIP")
					{
						List<PrintItem> partFiles = ProjectFileHandler.ImportFromProjectArchive(loadedFileName);
						if (partFiles != null)
						{
							foreach (PrintItem part in partFiles)
							{
								filesToLoad.Add(part.FileLocation);
							}
						}
					}
				}

				var itemCache = new Dictionary<string, IObject3D>();

				foreach (string filePath in filesToLoad)
				{
					var libraryItem = new FileSystemFileItem(filePath);

					IObject3D object3D = null;

					await ApplicationController.Instance.Tasks.Execute("Loading".Localize() + " " + Path.GetFileName(filePath), async (progressReporter, cancellationToken) =>
					{
						var progressStatus = new ProgressStatus();

						progressReporter.Report(progressStatus);

						object3D = await libraryItem.CreateContent((double progress0To1, string processingState) =>
						{
							progressStatus.Progress0To1 = progress0To1;
							progressStatus.Status = processingState;
							progressReporter.Report(progressStatus);
						});
					});

					if (object3D != null)
					{
						scene.Children.Modify(list => list.Add(object3D));

						PlatingHelper.MoveToOpenPositionRelativeGroup(object3D, scene.Children);
					}
				}
			}
		}

		private NamedAction[] BedMenuActions(BedConfig sceneContext, ThemeConfig theme)
		{
			return new[]
			{
				new NamedAction()
				{
					Title = "Insert".Localize(),
					Icon = AggContext.StaticData.LoadIcon("cube.png", 16, 16, theme.InvertIcons),
					Action = () =>
					{
						var extensionsWithoutPeriod = new HashSet<string>(ApplicationSettings.OpenDesignFileParams.Split('|').First().Split(',').Select(s => s.Trim().Trim('.')));

						foreach(var extension in ApplicationController.Instance.Library.ContentProviders.Keys)
						{
							extensionsWithoutPeriod.Add(extension.ToUpper());
						}

						var extensionsArray = extensionsWithoutPeriod.OrderBy(t => t).ToArray();

						string filter = string.Format(
							"{0}|{1}",
							string.Join(",", extensionsArray),
							string.Join("", extensionsArray.Select(e => $"*.{e.ToLower()};").ToArray()));

						UiThread.RunOnIdle(() =>
						{
							AggContext.FileDialogs.OpenFileDialog(
								new OpenFileDialogParams(filter, multiSelect: true),
								(openParams) =>
								{
									this.LoadAndAddPartsToPlate(openParams.FileNames, sceneContext.Scene);
								});
						});
					}
				},
				new NamedAction() { Title = "----" },
				new NamedAction()
				{
					Title = "Cut".Localize(),
					Shortcut = "Ctrl+X",
					Action = () =>
					{
						sceneContext.Scene.Cut();
					},
					IsEnabled = () => sceneContext.EditableScene
				},
				new NamedAction()
				{
					Title = "Copy".Localize(),
					Shortcut = "Ctrl+C",
					Action = () =>
					{
						sceneContext.Scene.Copy();
					},
					IsEnabled = () => sceneContext.EditableScene
				},
				new NamedAction()
				{
					Title = "Paste".Localize(),
					Shortcut = "Ctrl+V",
					Action = () =>
					{
						sceneContext.Scene.Paste();
					},
					IsEnabled = () => sceneContext.EditableScene
				},
				new NamedAction() { Title = "----" },
				new NamedAction()
				{
					Title = "Save".Localize(),
					Shortcut = "Ctrl+S",
					Action = async () =>
					{
						await ApplicationController.Instance.Tasks.Execute("Saving".Localize(), sceneContext.SaveChanges);
					},
					IsEnabled = () => sceneContext.EditableScene
				},
				new NamedAction()
				{
					Title = "Save As".Localize(),
					Action = () => UiThread.RunOnIdle(() =>
					{
						DialogWindow.Show(
							new SaveAsPage(
								async (newName, destinationContainer) =>
								{
									// Save to the destination provider
									if (destinationContainer is ILibraryWritableContainer writableContainer)
									{
										// Wrap stream with ReadOnlyStream library item and add to container
										writableContainer.Add(new[] { new InMemoryLibraryItem(sceneContext.Scene) });
										destinationContainer.Dispose();
									}
								}));
					}),
					IsEnabled = () => sceneContext.EditableScene
				},
				new NamedAction()
				{
					Title = "Export".Localize(),
					Action = () =>
					{
						UiThread.RunOnIdle(async () =>
						{
							DialogWindow.Show(
								new ExportPrintItemPage(new[]
								{
									new InMemoryLibraryItem(sceneContext.Scene)
								}));
						});
					},
					IsEnabled = () => sceneContext.EditableScene
				},
				new NamedAction()
				{
					Title = "Publish".Localize(),
					Action = () =>
					{
						UiThread.RunOnIdle(() => DialogWindow.Show<PublishPartToMatterHackers>());
					},
					IsEnabled = () => sceneContext.EditableScene
				},
				new NamedAction()
				{
					Title = "Arrange All Parts".Localize(),
					Action = () =>
					{
						sceneContext.Scene.AutoArrangeChildren(view3DWidget);
					},
					IsEnabled = () => sceneContext.EditableScene
				},
				new NamedAction() { Title = "----" },
				new NamedAction()
				{
					Title = "Clear Bed".Localize(),
					Action = () =>
					{
						UiThread.RunOnIdle(() =>
						{
							sceneContext.ClearPlate().ConfigureAwait(false);
						});
					}
				}
			};
		}

		public override void OnClosed(ClosedEventArgs e)
		{
			base.OnClosed(e);
		}
	}
}