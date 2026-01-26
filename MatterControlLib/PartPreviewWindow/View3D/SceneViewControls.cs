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

using System.IO;
using MatterHackers.Agg;
using MatterHackers.Agg.Platform;
using MatterHackers.Agg.UI;
using MatterHackers.ImageProcessing;
using MatterHackers.Localizations;
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

            var staticData = StaticData.Instance;

            bool perspectiveEnabled = UserSettings.Instance.get(UserSettingsKey.PerspectiveMode) != false.ToString();
            tumbleWidget.ChangeProjectionMode(perspectiveEnabled, false);

            NamedAction[] actions = BuildActions(view3DWidget, theme, printer, sceneContext, tumbleWidget, viewControls3D, staticData);

            controlsLayer.AddChild(new DynamicToolbar(actions, sceneContext, theme, 10)
            {
                VAnchor = VAnchor.Top | VAnchor.Fit,
                HAnchor = HAnchor.Fit | HAnchor.Right,
            });

            var tumbleCubeControl = new TumbleCubeControl(controlsLayer, theme, tumbleWidget)
            {
                Margin = new BorderDouble(0, 0, 25, 40),
                VAnchor = VAnchor.Top,
                HAnchor = HAnchor.Right,
                Name = "Tumble Cube Control",
                Visible = printer == null || printer.ViewState.ViewMode == PartViewMode.Model,
            };
            controlsLayer.AddChild(tumbleCubeControl);
        }

        private NamedAction[] BuildActions(View3DWidget view3DWidget, ThemeConfig theme, PrinterConfig printer, ISceneContext sceneContext, TrackballTumbleWidgetExtended tumbleWidget, ViewToolBarControls viewControls3D, StaticData staticData)
        {
            return new NamedAction[]
                        {
                new NamedActionGroup()
                {
                    ID = "view",
                    Title = "View",
                    Collapse = true,
                    Group = new []
                    {
                        new NamedAction()
                        {
                            ID = "home",
                            Title = "Reset View".Localize(),
                            Icon = staticData.LoadIcon("fa-home_16.png", 16, 16).GrayToColor(theme.TextColor),
                            Action = () => viewControls3D.NotifyResetView(),
                        },
                        new NamedToggleAction()
                        {
                            ID = "showBed",
                            Title = "Show Bed".Localize(),
                            Icon = staticData.LoadIcon("bed.png", 16, 16).GrayToColor(theme.TextColor),
                            IsActive = () => sceneContext.RendererOptions.RenderBed,
                            Action = () =>
                            {
                                sceneContext.RendererOptions.RenderBed = !sceneContext.RendererOptions.RenderBed;
                            }
                        },
                        new NamedToggleAction()
                        {
                            ID = "showPrintArea",
                            Title = "Show Print Area".Localize(),
                            IsVisible = () => sceneContext.Printer != null,
                            Icon = StaticData.Instance.LoadIcon("print_area.png", 16, 16).GrayToColor(theme.TextColor),
                            IsEnabled = () => sceneContext.BuildHeight > 0 && printer?.ViewState.ViewMode != PartViewMode.Layers2D,
                            IsActive = () => sceneContext.RendererOptions.RenderBuildVolume,
                            Action = () =>
                            {
                                sceneContext.RendererOptions.RenderBuildVolume = !sceneContext.RendererOptions.RenderBuildVolume;
                            }
                        },
                        new NamedToggleAction()
                        {
                            ID = "usePerspectiveMode",
                            Title = "Perspective Mode".Localize(),
                            Icon = staticData.LoadIcon("perspective.png", 16, 16).GrayToColor(theme.TextColor),
                            IsActive = () => UserSettings.Instance.get(UserSettingsKey.PerspectiveMode) != false.ToString(),
                            Action = () =>
                            {
								// Retrieve and toggle value
								bool usePerspectiveMode = UserSettings.Instance.get(UserSettingsKey.PerspectiveMode) != false.ToString();
                                usePerspectiveMode = !usePerspectiveMode;

                                UserSettings.Instance.set(UserSettingsKey.PerspectiveMode, usePerspectiveMode.ToString());
                                tumbleWidget.ChangeProjectionMode(usePerspectiveMode, true);
                                if (true)
                                {
									// Make sure the view has up going the right direction
									// WIP, this should fix the current rotation rather than reset the view
									//ResetView();
								}

                                this.Invalidate();
                            },
                        },
                        new NamedToggleAction()
                        {
                            ID = "turntableMode",
                            Title = "Turntable Mode".Localize(),
                            Icon = staticData.LoadIcon("spin.png", 16, 16).GrayToColor(theme.TextColor),
                            IsActive = () => UserSettings.Instance.get(UserSettingsKey.TurntableMode) != "False",
                            Action = () =>
                            {
								// Retrieve and toggle value
								bool useTurntableMode = UserSettings.Instance.get(UserSettingsKey.TurntableMode) != "False";
                                useTurntableMode = !useTurntableMode;

                                UserSettings.Instance.set(UserSettingsKey.TurntableMode, useTurntableMode.ToString());
                                tumbleWidget.TurntableEnabled = useTurntableMode;
                                if (useTurntableMode)
                                {
									// Make sure the view has up going the right direction
									// WIP, this should fix the current rotation rather than reset the view
									viewControls3D.NotifyResetView();
                                }
                            },
                        },
                    },
                },
                new NamedAction()
                {
                    ID = "zoomToSelection",
                    Title = "Zoom to Selection".Localize(),
                    Icon = staticData.LoadIcon("select.png", 16, 16).GrayToColor(theme.TextColor),
                    IsEnabled = () => sceneContext.Scene.SelectedItem != null && (printer == null || printer.ViewState.ViewMode == PartViewMode.Model),
                    Action = () => view3DWidget.ZoomToSelection(),
                },
                new ActionSeparator(),
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

            };
        }
    }
}
