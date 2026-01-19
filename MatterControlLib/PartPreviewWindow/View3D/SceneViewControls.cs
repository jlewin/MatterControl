/*
Copyright (c) 2026, Lars Brubaker, John Lewin
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
#define ENABLE_PERSPECTIVE_PROJECTION_DYNAMIC_NEAR_FAR

using System;
using System.IO;
using MatterHackers.Agg.Platform;
using MatterHackers.Agg.UI;
using MatterHackers.ImageProcessing;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.Library;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl.PartPreviewWindow
{
	public class SceneViewControls : GuiWidget
	{
		private PrinterConfig printer;
		private ViewToolBarControls viewControls3D; // Event deregistration
		private Object3DControlsLayer controlsLayer;
		private ThemedRadioIconButton printAreaButton;
		private GridOptionsPanel gridSnapButton;
		private ISceneContext sceneContext;

		public SceneViewControls(
			View3DWidget view3DWidget,
			ThemeConfig theme,
			PrinterConfig printer,
			ISceneContext sceneContext,
			Object3DControlsLayer controlsLayer,
			TrackballTumbleWidgetExtended tumbleWidget,
			ViewToolBarControls viewControls3D)
		{
			this.printer = printer;
			this.viewControls3D = viewControls3D;
			this.controlsLayer = controlsLayer;
			this.sceneContext = sceneContext;

			/*
\			gridSnapButton = new GridOptionsPanel(controlsLayer, theme)
			{
				PopupMate = new MatePoint()
				{
					Mate = new MateOptions(MateEdge.Right, MateEdge.Top)
				}
			};

#if DEBUG
			var renderOptionsButton = new RenderOptionsButton(theme, controlsLayer)
			{
				ToolTipText = "Debug Render Options".Localize(),
			};
#endif
			*/

			var staticData = StaticData.Instance;
			this.MinimumSize = new Vector2(100, 100);
			//Debugger.Break();
			bool perspectiveEnabled = UserSettings.Instance.get(UserSettingsKey.PerspectiveMode) != false.ToString();
			tumbleWidget.ChangeProjectionMode(perspectiveEnabled, false);

			var actions = new NamedAction[]
			{
				new NamedAction()
				{
					ID = "zoomToSelection",
					Title = "Zoom to Selection".Localize(),
					Icon = staticData.LoadIcon(Path.Combine("ViewTransformControls", "scale.png"), 16, 16).GrayToColor(theme.TextColor),
					IsEnabled = () => sceneContext.Scene.SelectedItem != null && (printer == null || printer.ViewState.ViewMode == PartViewMode.Model),
					Action = () => viewControls3D.ActiveButton = ViewControls3DButtons.Scale
				},
				new NamedAction()
				{
					ID = "home",
					Title = "Reset View".Localize(),
					Icon = staticData.LoadIcon(Path.Combine("ViewTransformControls", "scale.png"), 16, 16).GrayToColor(theme.TextColor),
					Action = () => viewControls3D.NotifyResetView(),
				},
				new NamedActionGroup()
				{
					ID = "mode",
					Title = "Mode",
					Group = new []
					{
						new NamedAction()
						{
							ID = "partSelectMode",
							Title = "Select Parts".Localize(),
							Icon = staticData.LoadIcon(Path.Combine("ViewTransformControls", "partSelect.png"), 16, 16).GrayToColor(theme.TextColor),
							Action = () => viewControls3D.ActiveButton = ViewControls3DButtons.PartSelect,
						},
						new NamedAction()
						{
							ID = "rotateView",
							Title = "Rotate View".Localize(),
							Icon = staticData.LoadIcon(Path.Combine("ViewTransformControls", "rotate.png"), 16, 16).GrayToColor(theme.TextColor),
							Action = () => viewControls3D.ActiveButton = ViewControls3DButtons.Rotate,
						},
						new NamedAction()
						{
							ID = "translateView",
							Title = "Move View".Localize(),
							Icon = staticData.LoadIcon(Path.Combine("ViewTransformControls", "translate.png"), 16, 16).GrayToColor(theme.TextColor),
							Action = () => viewControls3D.ActiveButton = ViewControls3DButtons.Translate,
						},
						new NamedAction()
						{
							ID = "zoomView",
							Title = "Zoom View".Localize(),
							Icon = staticData.LoadIcon(Path.Combine("ViewTransformControls", "scale.png"), 16, 16).GrayToColor(theme.TextColor),
							Action = () => viewControls3D.ActiveButton = ViewControls3DButtons.Scale,
						},
					},
					IsEnabled = () => true,
				},
				new NamedBoolAction()
				{
					ID = "showBed",
					Title = "Show Bed".Localize(),
					Icon = staticData.LoadIcon("print_area.png", 16, 16).GrayToColor(theme.TextColor),
					IsEnabled = () =>
					{
						if (sceneContext.Printer != null)
						{
							return sceneContext.Printer.PrintButtonEnabled();
						}

						return sceneContext.EditableScene
							|| (sceneContext.EditContext.SourceItem is ILibraryAsset libraryAsset
								&& string.Equals(Path.GetExtension(libraryAsset.FileName), ".gcode", StringComparison.OrdinalIgnoreCase));
					},
					GetIsActive = () => sceneContext.RendererOptions.RenderBed,
					SetIsActive = (value) => sceneContext.RendererOptions.RenderBed = value,
				},
				new NamedBoolAction()
				{
					ID = "showPrintArea",
					Title = "Show Print Area".Localize(),
					IsVisible = () => sceneContext.Printer != null,
					Icon = StaticData.Instance.LoadIcon("print_area.png", 16, 16).GrayToColor(theme.TextColor),
					IsEnabled = () => sceneContext.BuildHeight > 0 && printer?.ViewState.ViewMode != PartViewMode.Layers2D,
					GetIsActive = () => sceneContext.RendererOptions.RenderBuildVolume,
					SetIsActive = (value) => sceneContext.RendererOptions.RenderBuildVolume = value,
				},
				new NamedBoolAction()
				{
					ID = "usePerspectiveMode",
					Title = "Perspective Mode".Localize(),
					Icon = staticData.LoadIcon("perspective.png", 16, 16).GrayToColor(theme.TextColor),
					GetIsActive = () => UserSettings.Instance.get(UserSettingsKey.PerspectiveMode) != false.ToString(),
					SetIsActive = (value) =>
					{
						UserSettings.Instance.set(UserSettingsKey.PerspectiveMode, value.ToString());
						tumbleWidget.ChangeProjectionMode(value, true);
						if (true)
						{
							// Make sure the view has up going the right direction
							// WIP, this should fix the current rotation rather than reset the view
							//ResetView();
						}

						this.Invalidate();
					},
				},
				new NamedBoolAction()
				{
					ID = "turntableMode",
					Title = "Turntable Mode".Localize(),
					Icon = staticData.LoadIcon("spin.png", 16, 16).GrayToColor(theme.TextColor),
					GetIsActive = () => UserSettings.Instance.get(UserSettingsKey.TurntableMode) != "False",
					SetIsActive = (value) =>
					{
						UserSettings.Instance.set(UserSettingsKey.TurntableMode, value.ToString());
						tumbleWidget.TurntableEnabled = value;
						if (value)
						{
							// Make sure the view has up going the right direction
							// WIP, this should fix the current rotation rather than reset the view
							viewControls3D.NotifyResetView();
						}
					},
				},


			};

			this.DebugShowBounds = true;


			this.Click += (s, e) =>
			{
				if (e.Button == MouseButtons.Right)
				{
					var menuTheme = AppContext.MenuTheme;
					var popupMenu = new PopupMenu(menuTheme);
					menuTheme.CreateMenuItems(popupMenu, actions);
					popupMenu.ShowMenu(this, e.Position);
				}
			};


		}
		//view3DWidget.BindBedOptions(controlsLayer, bedButton, printAreaButton, sceneContext.RendererOptions);


		//public override void OnClosed(EventArgs e)
		//{
		//	viewControls3D.TransformStateChanged -= ViewControls3D_TransformStateChanged;
		//	if (printer?.ViewState != null)
		//	{
		//		printer.ViewState.ViewModeChanged -= ViewState_ViewModeChanged;
		//	}

		//	base.OnClosed(e);
		//}

		//private void ViewState_ViewModeChanged(object sender, ViewModeChangedEventArgs e)
		//{
		//	modelViewStyleButton.Visible = e.ViewMode == PartViewMode.Model;

		//	// Disable print area button in GCode2D view - conditionally created based on BuildHeight, only set enabled if created
		//	printAreaButton.Enabled = ;

		//	gridSnapButton.Visible = modelViewStyleButton.Visible;
		//}

		//private void ViewControls3D_TransformStateChanged(object sender, TransformStateChangedEventArgs e)
		//{ 
		//	var activeTransformState = e.TransformMode;
		//	switch (activeTransformState)
		//	{
		//		case ViewControls3DButtons.Rotate:
		//			if (rotateButton != null)
		//			{
		//				rotateButton.Checked = true;
		//			}
		//			break;

		//		case ViewControls3DButtons.Translate:
		//			if (translateButton != null)
		//			{
		//				translateButton.Checked = true;
		//			}
		//			break;

		//		case ViewControls3DButtons.Scale:
		//			if (zoomButton != null)
		//			{
		//				zoomButton.Checked = true;
		//			}
		//			break;

		//		case ViewControls3DButtons.PartSelect:
		//			if (partSelectButton != null)
		//			{
		//				partSelectButton.Checked = true;
		//			}
		//			break;
		//	}
		//}
	}
}
