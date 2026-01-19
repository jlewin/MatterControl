/*
Copyright (c) 2018, Lars Brubaker, John Lewin
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using MatterHackers.Agg;
using MatterHackers.Agg.Platform;
using MatterHackers.Agg.UI;
using MatterHackers.Agg.VertexSource;
using MatterHackers.ImageProcessing;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.DataStorage;
using MatterHackers.VectorMath;
using static MatterHackers.MatterControl.CustomWidgets.LibraryListView;

namespace MatterHackers.MatterControl.PartPreviewWindow
{
	public class MCSceneViewControls : GuiWidget
	{
		private ThemedRadioIconButton translateButton;
		private ThemedRadioIconButton rotateButton;
		private ThemedRadioIconButton zoomButton;
		private ThemedRadioIconButton partSelectButton;
		private PrinterConfig printer;

		//private TrackballTumbleWidgetExtended tumbleWidget;
		private ViewToolBarControls viewControls3D; // Event deregistration
		private Object3DControlsLayer controlsLayer;
		private ThemedRadioIconButton printAreaButton;
		private ViewStyleButton modelViewStyleButton;
		private GridOptionsPanel gridSnapButton;
		private ISceneContext sceneContext;

		public MCSceneViewControls(
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

			viewControls3D.TransformStateChanged += ViewControls3D_TransformStateChanged;

			var scale = GuiWidget.DeviceScale;
			var tumbleCubeControl = new TumbleCubeControl(controlsLayer, theme, tumbleWidget)
			{
				Margin = new BorderDouble(0, 0, 40, 45),
				VAnchor = VAnchor.Top,
				HAnchor = HAnchor.Right,
				Name = "Tumble Cube Control"
			};
			tumbleCubeControl.Visible = printer.ViewState.ViewMode == PartViewMode.Model;

			var cubeCenterFromRightTop = new Vector2(tumbleCubeControl.Margin.Right * scale + tumbleCubeControl.Width / 2,
				tumbleCubeControl.Margin.Top * scale + tumbleCubeControl.Height / 2);
			
			this.AddChild(tumbleCubeControl);

			var hudBackground = controlsLayer.AddChild(new GuiWidget()
			{
				VAnchor = VAnchor.Stretch,
				HAnchor = HAnchor.Stretch,
				Selectable = false,
			});

			// add the view controls
			var buttonGroupA = new ObservableCollection<GuiWidget>();
			partSelectButton = new ThemedRadioIconButton(StaticData.Instance.LoadIcon(Path.Combine("ViewTransformControls", "partSelect.png"), 16, 16).GrayToColor(theme.TextColor), theme)
			{
				SiblingRadioButtonList = buttonGroupA,
				ToolTipText = "Select Parts".Localize(),
				Margin = theme.ButtonSpacing,
			};

			partSelectButton.MouseEnterBounds += (s, e) => partSelectButton.SetActiveUiHint("Ctrl + A = Select All, 'Space' = Clear Selection, 'ESC' = Cancel Drag".Localize());
			AddRoundButton(partSelectButton, RotatedMargin(partSelectButton, MathHelper.Tau * .15, cubeCenterFromRightTop));
			partSelectButton.Click += (s, e) => viewControls3D.ActiveButton = ViewControls3DButtons.PartSelect;
			buttonGroupA.Add(partSelectButton);

			rotateButton = new ThemedRadioIconButton(StaticData.Instance.LoadIcon(Path.Combine("ViewTransformControls", "rotate.png"), 16, 16).GrayToColor(theme.TextColor), theme)
			{
				SiblingRadioButtonList = buttonGroupA,
				ToolTipText = "Rotate View".Localize(),
				Margin = theme.ButtonSpacing
			};
			rotateButton.MouseEnterBounds += (s, e) => rotateButton.SetActiveUiHint("Rotate: Right Mouse Button, Ctrl + Left Mouse Button, Arrow Keys".Localize());
			AddRoundButton(rotateButton, RotatedMargin(rotateButton, MathHelper.Tau * .05, cubeCenterFromRightTop));
			rotateButton.Click += (s, e) => viewControls3D.ActiveButton = ViewControls3DButtons.Rotate;
			buttonGroupA.Add(rotateButton);

			translateButton = new ThemedRadioIconButton(StaticData.Instance.LoadIcon(Path.Combine("ViewTransformControls", "translate.png"), 16, 16).GrayToColor(theme.TextColor), theme)
			{
				SiblingRadioButtonList = buttonGroupA,
				ToolTipText = "Move View".Localize(),
				Margin = theme.ButtonSpacing
			};
			translateButton.MouseEnterBounds += (s, e) => translateButton.SetActiveUiHint("Move: Middle Mouse Button, Ctrl + Shift + Left Mouse Button, Shift Arrow Keys".Localize());
			AddRoundButton(translateButton, RotatedMargin(translateButton, -MathHelper.Tau * .05, cubeCenterFromRightTop));
			translateButton.Click += (s, e) => viewControls3D.ActiveButton = ViewControls3DButtons.Translate;
			buttonGroupA.Add(translateButton);

			zoomButton = new ThemedRadioIconButton(StaticData.Instance.LoadIcon(Path.Combine("ViewTransformControls", "scale.png"), 16, 16).GrayToColor(theme.TextColor), theme)
			{
				SiblingRadioButtonList = buttonGroupA,
				ToolTipText = "Zoom View".Localize(),
				Margin = theme.ButtonSpacing
			};
			zoomButton.MouseEnterBounds += (s, e) => zoomButton.SetActiveUiHint("Zoom: Mouse Wheel, Ctrl + Alt + Left Mouse Button, Ctrl + '+' & Ctrl + '-'".Localize());
			AddRoundButton(zoomButton, RotatedMargin(zoomButton, -MathHelper.Tau * .15, cubeCenterFromRightTop));
			zoomButton.Click += (s, e) => viewControls3D.ActiveButton = ViewControls3DButtons.Scale;
			buttonGroupA.Add(zoomButton);

			var bottomButtonOffset = 0;
			var hudBackgroundColor = theme.BedBackgroundColor.WithAlpha(120);
			var hudStrokeColor = theme.TextColor.WithAlpha(120);

			// add the background render for the view controls
			// controlLayer.BeforeDraw += (s, e) => // enable to debug any rendering errors that might be due to double buffered hudBackground
			hudBackground.BeforeDraw += (s, e) =>
			{
				var tumbleCubeRadius = tumbleCubeControl.Width / 2;
				var tumbleCubeCenter = new Vector2(controlsLayer.Width - tumbleCubeControl.Margin.Right * scale - tumbleCubeRadius,
					controlsLayer.Height - tumbleCubeControl.Margin.Top * scale - tumbleCubeRadius);

				void renderPath(IVertexSource vertexSource, double width)
				{
					var background = new Stroke(vertexSource, width * 2);
					background.LineCap = LineCap.Round;
					e.Graphics2D.Render(background, hudBackgroundColor);
					e.Graphics2D.Render(new Stroke(background, scale), hudStrokeColor);
				}

				void renderRoundedGroup(double spanRatio, double startRatio)
				{
					var angle = MathHelper.Tau * spanRatio;
					var width = 17 * scale;
					var start = MathHelper.Tau * startRatio - angle / 2;
					var end = MathHelper.Tau * startRatio + angle / 2;
					var arc = new Arc(tumbleCubeCenter, tumbleCubeRadius + 12 * scale + width / 2, start, end);

					renderPath(arc, width);
				}

				renderRoundedGroup(.3, .25);
				renderRoundedGroup(.1, .5 + .1);

				// render the perspective and turntable group background
				renderRoundedGroup(.1, 1 - .1); // when we have both ortho and turntable

				void renderRoundedLine(double lineWidth, double heightBelowCenter)
				{
					lineWidth *= scale;
					var width = 17 * scale;
					var height = tumbleCubeCenter.Y - heightBelowCenter * scale;
					var start = tumbleCubeCenter.X - lineWidth;
					var end = tumbleCubeCenter.X + lineWidth;
					var line = new VertexStorage();
					line.MoveTo(start, height);
					line.LineTo(end, height);

					renderPath(line, width);
				}

				tumbleCubeCenter.X += bottomButtonOffset;

				renderRoundedLine(18, 101);

				// e.Graphics2D.Circle(controlLayer.Width - cubeCenterFromRightTop.X, controlLayer.Height - cubeCenterFromRightTop.Y, 150, Color.Cyan);

				// ImageIO.SaveImageData("C:\\temp\\test.png", hudBackground.BackBuffer);
			};

			//// add the home button
			var homeButton = new ThemedIconButton(StaticData.Instance.LoadIcon("fa-home_16.png", 16, 16).GrayToColor(theme.TextColor), theme)
			{
				ToolTipText = "Reset View".Localize(),
				Margin = theme.ButtonSpacing
			};
			homeButton.MouseEnterBounds += (s1, e1) => homeButton.SetActiveUiHint("W Key");
			AddRoundButton(homeButton, RotatedMargin(homeButton, MathHelper.Tau * .3, cubeCenterFromRightTop)).Click += (s, e) => viewControls3D.NotifyResetView();

			var zoomToSelectionButton = new ThemedIconButton(StaticData.Instance.LoadIcon("select.png", 16, 16).GrayToColor(theme.TextColor), theme)
			{
				Name = "Zoom to selection button",
				ToolTipText = "Zoom to Selection".Localize(),
				Margin = theme.ButtonSpacing
			};
			zoomToSelectionButton.MouseEnterBounds += (s1, e1) => zoomToSelectionButton.SetActiveUiHint("Z Key");
			void SetZoomEnabled(object s, EventArgs e)
			{
				zoomToSelectionButton.Enabled = sceneContext.Scene.SelectedItem != null
					&& (printer == null || printer.ViewState.ViewMode == PartViewMode.Model);
			}

			AddRoundButton(zoomToSelectionButton, RotatedMargin(zoomToSelectionButton, MathHelper.Tau * .4, cubeCenterFromRightTop)).Click += (s, e) => view3DWidget.ZoomToSelection();

			var turntableEnabled = UserSettings.Instance.get(UserSettingsKey.TurntableMode) != "False";
			tumbleWidget.TurntableEnabled = turntableEnabled;

			var turnTableButton = new ThemedRadioIconButton(StaticData.Instance.LoadIcon("spin.png", 16, 16).GrayToColor(theme.TextColor), theme)
			{
				ToolTipText = "Turntable Mode".Localize(),
				Margin = theme.ButtonSpacing,
				Padding = 2,
				ToggleButton = true,
				SiblingRadioButtonList = new List<GuiWidget>(),
				Checked = turntableEnabled,
				//DoubleBuffer = true,
			};
			turnTableButton.MouseEnterBounds += (s, e) => turnTableButton.SetActiveUiHint("Switch between turn table and trackball modes".Localize());

			AddRoundButton(turnTableButton, RotatedMargin(turnTableButton, -MathHelper.Tau * .4, cubeCenterFromRightTop)); // 2 button position
			turnTableButton.CheckedStateChanged += (s, e) =>
			{
				UserSettings.Instance.set(UserSettingsKey.TurntableMode, turnTableButton.Checked.ToString());
				tumbleWidget.TurntableEnabled = turnTableButton.Checked;
				if (turnTableButton.Checked)
				{
					// Make sure the view has up going the right direction
					// WIP, this should fix the current rotation rather than reset the view
					viewControls3D.NotifyResetView();
				}
			};

			var perspectiveEnabled = UserSettings.Instance.get(UserSettingsKey.PerspectiveMode) != false.ToString();
			tumbleWidget.ChangeProjectionMode(perspectiveEnabled, false);
			var projectionButton = new ThemedRadioIconButton(StaticData.Instance.LoadIcon("perspective.png", 16, 16).GrayToColor(theme.TextColor), theme)
			{
				Name = "Projection mode button",
				ToolTipText = "Perspective Mode".Localize(),
				Margin = theme.ButtonSpacing,
				ToggleButton = true,
				SiblingRadioButtonList = new List<GuiWidget>(),
				Checked = tumbleWidget.PerspectiveMode,
			};
			projectionButton.MouseEnterBounds += (s, e) => projectionButton.SetActiveUiHint("Turn on and off perspective rendering".Localize());
			AddRoundButton(projectionButton, RotatedMargin(projectionButton, -MathHelper.Tau * .3, cubeCenterFromRightTop));
			projectionButton.CheckedStateChanged += (s, e) =>
			{
				UserSettings.Instance.set(UserSettingsKey.PerspectiveMode, projectionButton.Checked.ToString());
				tumbleWidget.ChangeProjectionMode(projectionButton.Checked, true);
				if (true)
				{
					// Make sure the view has up going the right direction
					// WIP, this should fix the current rotation rather than reset the view
					//ResetView();
				}

				Invalidate();
			};

			var startHeight = 180;
			var ySpacing = 40;
			cubeCenterFromRightTop.X -= bottomButtonOffset;

			// put in the bed and build volume buttons
			var bedButton = new ThemedRadioIconButton(StaticData.Instance.LoadIcon("bed.png", 16, 16).GrayToColor(theme.TextColor), theme)
			{
				Name = "Bed Button",
				ToolTipText = "Show Print Bed".Localize(),
				Checked = sceneContext.RendererOptions.RenderBed,
				ToggleButton = true,
				SiblingRadioButtonList = new List<GuiWidget>(),
			};
			bedButton.MouseEnterBounds += (s, e) => bedButton.SetActiveUiHint("Hide and show the bed".Localize());

			AddRoundButton(bedButton, new Vector2((cubeCenterFromRightTop.X + 18 * scale - bedButton.Width / 2) / scale, startHeight));
			printAreaButton = new ThemedRadioIconButton(StaticData.Instance.LoadIcon("print_area.png", 16, 16).GrayToColor(theme.TextColor), theme)
			{
				Name = "Bed Button",
				ToolTipText = BuildHeightValid() ? "Show Print Area".Localize() : "Define printer build height to enable",
				Checked = sceneContext.RendererOptions.RenderBuildVolume,
				ToggleButton = true,
				Enabled = BuildHeightValid() && printer?.ViewState.ViewMode != PartViewMode.Layers2D && bedButton.Checked,
				SiblingRadioButtonList = new List<GuiWidget>(),
			};

			bedButton.CheckedStateChanged += (s, e) =>
			{
				sceneContext.RendererOptions.RenderBed = bedButton.Checked;
				printAreaButton.Enabled = BuildHeightValid() && printer?.ViewState.ViewMode != PartViewMode.Layers2D && bedButton.Checked;
			};

			AddRoundButton(printAreaButton, new Vector2((cubeCenterFromRightTop.X - 18 * scale - bedButton.Width / 2) / scale, startHeight));

			printAreaButton.CheckedStateChanged += (s, e) =>
			{
				sceneContext.RendererOptions.RenderBuildVolume = printAreaButton.Checked;
			};

			view3DWidget.BindBedOptions(controlsLayer, bedButton, printAreaButton, sceneContext.RendererOptions);

			// put in the view list buttons
			modelViewStyleButton = new ViewStyleButton(sceneContext, theme)
			{
				PopupMate = new MatePoint()
				{
					Mate = new MateOptions(MateEdge.Right, MateEdge.Top)
				}
			};
			modelViewStyleButton.MouseEnterBounds += (s, e) => modelViewStyleButton.SetActiveUiHint("Change the current rendering mode".Localize());

			modelViewStyleButton.AnchorMate.Mate.VerticalEdge = MateEdge.Bottom;
			modelViewStyleButton.AnchorMate.Mate.HorizontalEdge = MateEdge.Right;
			var marginCenter = cubeCenterFromRightTop.X / scale;
			AddRoundButton(modelViewStyleButton, new Vector2(marginCenter, startHeight + 1 * ySpacing), true);
			modelViewStyleButton.BackgroundColor = hudBackgroundColor;
			modelViewStyleButton.BorderColor = hudStrokeColor;

			if (printer?.ViewState != null)
			{
				printer.ViewState.ViewModeChanged += ViewState_ViewModeChanged;
			}

			// Add the grid snap button
			gridSnapButton = new GridOptionsPanel(controlsLayer, theme)
			{
				PopupMate = new MatePoint()
				{
					Mate = new MateOptions(MateEdge.Right, MateEdge.Top)
				}
			};
			gridSnapButton.MouseEnterBounds += (s, e) => gridSnapButton.SetActiveUiHint("Adjust the grid that objects will snap to when moved".Localize());

			gridSnapButton.AnchorMate.Mate.VerticalEdge = MateEdge.Bottom;
			gridSnapButton.AnchorMate.Mate.HorizontalEdge = MateEdge.Right;
			AddRoundButton(gridSnapButton, new Vector2(marginCenter, startHeight + 2 * ySpacing), true);
			gridSnapButton.BackgroundColor = hudBackgroundColor;
			gridSnapButton.BorderColor = hudStrokeColor;

#if DEBUG
			var renderOptionsButton = new RenderOptionsButton(theme, controlsLayer)
			{
				ToolTipText = "Debug Render Options".Localize(),
				PopupMate = new MatePoint()
				{
					Mate = new MateOptions(MateEdge.Left, MateEdge.Top)
				},
				AnchorMate = new MatePoint()
				{
					Mate = new MateOptions(MateEdge.Left, MateEdge.Bottom)
				}
			};
			AddRoundButton(renderOptionsButton, new Vector2(marginCenter, startHeight + 3 * ySpacing), true);
#endif
		}

		private static Vector2 RotatedMargin(GuiWidget widget, double angle, Vector2 cubeCenterFromRightTop)
		{
			var scale = GuiWidget.DeviceScale;
			var radius = 70 * scale;
			var widgetCenter = new Vector2(widget.Width / 2, widget.Height / 2);
			// divide by scale to convert from pixels to margin units
			return (cubeCenterFromRightTop - widgetCenter - new Vector2(0, radius).GetRotated(angle)) / scale;
		}

		bool BuildHeightValid() => sceneContext.BuildHeight > 0;

		public override void OnClosed(EventArgs e)
		{
			viewControls3D.TransformStateChanged -= ViewControls3D_TransformStateChanged;
			if (printer?.ViewState != null)
			{
				printer.ViewState.ViewModeChanged -= ViewState_ViewModeChanged;
			}

			base.OnClosed(e);
		}

		private GuiWidget AddRoundButton(GuiWidget widget, Vector2 offset, bool center = false)
		{
			var scale = GuiWidget.DeviceScale;

			widget.BackgroundRadius = new RadiusCorners(Math.Min(widget.Width / 2, widget.Height / 2));
			widget.BackgroundOutlineWidth = 1;
			widget.VAnchor = VAnchor.Top;
			widget.HAnchor = HAnchor.Right;
			if (center)
			{
				offset.X -= (widget.Width / 2) / scale;
			}

			widget.Margin = new BorderDouble(0, 0, offset.X, offset.Y);
			//widget.DebugShowBounds = true;
			return controlsLayer.AddChild(widget);
		}

		private void ViewState_ViewModeChanged(object sender, ViewModeChangedEventArgs e)
		{
			modelViewStyleButton.Visible = e.ViewMode == PartViewMode.Model;

			// Disable print area button in GCode2D view - conditionally created based on BuildHeight, only set enabled if created
			printAreaButton.Enabled = BuildHeightValid() && printer.ViewState.ViewMode != PartViewMode.Layers2D;

			gridSnapButton.Visible = modelViewStyleButton.Visible;
		}


		private void ViewControls3D_TransformStateChanged(object sender, TransformStateChangedEventArgs e)
		{ 
			var activeTransformState = e.TransformMode;
			switch (activeTransformState)
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
					if (zoomButton != null)
					{
						zoomButton.Checked = true;
					}
					break;

				case ViewControls3DButtons.PartSelect:
					if (partSelectButton != null)
					{
						partSelectButton.Checked = true;
					}
					break;
			}
		}
	}
}
