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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MatterHackers.Agg;
using MatterHackers.Agg.Platform;
using MatterHackers.Agg.UI;
using MatterHackers.ImageProcessing;
using MatterHackers.Localizations;

namespace MatterHackers.MatterControl.PartPreviewWindow
{
    public class DynamicToolbar : Toolbar
	{
		private ThemeConfig theme;
		private IEnumerable<NamedAction> actions;
		private ISceneContext sceneContext;

		public DynamicToolbar(IEnumerable<NamedAction> actions, ISceneContext sceneContext, ThemeConfig theme, BorderDouble padding, GuiWidget rightAnchorItem = null)
			: base(padding, rightAnchorItem)
		{
			this.theme = theme;
			this.actions = actions;
			this.sceneContext = sceneContext;
			this.VAnchor = VAnchor.Fit;
			this.HAnchor = HAnchor.Fit;
			this.ActionArea.HAnchor = HAnchor.Fit;
			this.Padding = 2 * DeviceScale;

			var menuTheme = AppContext.MenuTheme;

			// Add Selected IObject3D -> Operations to toolbar
			foreach (var namedAction in actions)
			{
				if (namedAction is ActionSeparator)
				{
					this.AddChild(new ToolbarSeparator(theme.GetBorderColor(50), theme.SeparatorMargin));
					continue;
				}

				if (namedAction is NamedActionGroup group)
				{
					var dropdownButton = CreateDropDown(menuTheme, group);
					var expandedPanel = CreateGroupPanel(theme, group);

					void UpdateVisibility(object s, EventArgs e)
					{
						if (group.IsVisible())
						{
							if (group.Collapse)
							{
								dropdownButton.Visible = true;
								expandedPanel.Visible = false;
							}
							else
							{
								dropdownButton.Visible = false;
								expandedPanel.Visible = true;
							}
						}
					}

					UserSettings.Instance.SettingChanged += UpdateVisibility;
					//operationGroup.CollapseChanged += UpdateVisability;
					//operationGroup.VisibleChanged += UpdateVisability;
					this.Closed += (s, e) =>
					{
						UserSettings.Instance.SettingChanged -= UpdateVisibility;
						//operationGroup.CollapseChanged -= UpdateVisability;
						//operationGroup.VisibleChanged -= UpdateVisability;
					};

					UpdateVisibility(group, null);

					this.AddChild(dropdownButton);
					this.AddChild(expandedPanel);

					// Add a toolbar separator after group
					this.AddChild(new ToolbarSeparator(theme.GetBorderColor(50), theme.SeparatorMargin));
				}
				else
				{
					this.AddChild(new ActionButton(namedAction, theme)
					{
						Margin = theme.ButtonSpacing,
					});
				}
			}
		}

		private FlowLayoutWidget CreateGroupPanel(ThemeConfig theme, NamedActionGroup actionGroup)
		{
			var row = new FlowLayoutWidget();
			var siblingList = new ObservableCollection<GuiWidget>();

			foreach (var operation in actionGroup.Group)
			{
				if (operation == null)
				{
					continue;
				}

				var button = new ThemedRadioIconButton(operation.Icon, theme);
				button.SiblingRadioButtonList = siblingList;
				button.Click += (s, e) => UiThread.RunOnIdle(() =>
				{
					operation.Action?.Invoke();
				});
				siblingList.Add(button);

				row.AddChild(button);
			}

			//var collapseButton = row.AddChild(new ThemedIconButton(StaticData.Instance.LoadIcon("collapse_single.png", 8, 16).GrayToColor(theme.TextColor), theme));
			//collapseButton.Width = 16 * DeviceScale;
			//collapseButton.ToolTipText = "Collapse".Localize();
			//collapseButton.Click += (s, e) => UiThread.RunOnIdle(() =>
			//{
			//	//operationGroup.Collapse = true;
			//});

			return row;
		}

		private GuiWidget CreateDropDown(ThemeConfig menuTheme, NamedActionGroup group)
		{
			var defaultOperation = group.Group.First();

			PopupMenuButton splitButton = menuTheme.CreateSplitButton(
				new SplitButtonParams()
				{
					Icon = defaultOperation.Icon,
					ButtonAction = (menuButton) =>
					{
						defaultOperation.Action.Invoke();
					},
					ButtonTooltip = defaultOperation.Title,
					ButtonName = defaultOperation.Title,
					ExtendPopupMenu = (PopupMenu popupMenu) =>
					{
						foreach (var action in group.Group)
						{
							var menuItem = popupMenu.CreateMenuItem(action.Title, action.Icon);
							menuItem.Enabled = action.IsEnabled();
							menuItem.ToolTipText = action.Title;

							if (!menuItem.Enabled
								&& !string.IsNullOrEmpty(action.Title))
							{
								menuItem.ToolTipText += "\n\n" + action.Title;
							}

							menuItem.Click += (s, e) => UiThread.RunOnIdle(() =>
							{
								action.Action?.Invoke();
							});
						}

						ViewStyleMenu.Extend(popupMenu, menuTheme, sceneContext);

						popupMenu.CreateSubMenu(
							"Grid Snap".Localize(),
							menuTheme,
							(subMenu) =>
							{
								GridOptionsPanel.Extend(subMenu, menuTheme);
							});

						popupMenu.CreateSubMenu(
							"Visual Debug".Localize(),
							menuTheme,
							(menu) =>
							{
								var rendererOptionsButton = new RenderOptionsButton(theme);
								menu.AddChild(rendererOptionsButton.PopupContent());
							},
							StaticData.Instance.LoadIcon("web.png", 16, 16));
					}
				});

			return splitButton;
		}

		public PopupMenu AddModifyItems(PopupMenu popupMenu, NamedAction[] All)
		{
			bool Show(NamedAction action) => true;

			foreach (var operation in All)
			{
				if (!Show(operation))
				{
					continue;
				}

				if (operation is NamedActionGroup operationGroup)
				{
					popupMenu.CreateSubMenu(
						operationGroup.Title,
						theme,
						(subMenu) =>
						{
							foreach (var childOperation in operationGroup.Group)
							{
								if (!Show(childOperation))
								{
									continue;
								}

								var menuItem = subMenu.CreateMenuItem(childOperation.Title, childOperation.Icon);
								menuItem.Click += (s, e) => UiThread.RunOnIdle(() =>
								{
									childOperation.Action?.Invoke();
								});

								menuItem.Enabled = childOperation.IsEnabled();
								menuItem.ToolTipText = childOperation.Title;
							}
						});
				}
				else if (operation is ActionSeparator separator)
				{
				}
				else
				{
					var menuItem = popupMenu.CreateMenuItem(operation.Title, operation.Icon);
					menuItem.Click += (s, e) => operation.Action();
					menuItem.Enabled = operation.IsEnabled();
					menuItem.ToolTipText = operation.Title;
				}
			}

			return popupMenu;
		}
	}
}
