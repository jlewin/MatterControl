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
using MatterHackers.Agg;
using MatterHackers.Agg.UI;

namespace MatterHackers.MatterControl
{
	using System.Collections.Generic;
	using Agg.Image;
	using CustomWidgets;
	using MatterHackers.Agg.Platform;
	using MatterHackers.Localizations;
	using MatterHackers.MatterControl.PartPreviewWindow;
	using MatterHackers.VectorMath;

	public class ThemeConfig
	{
		public static ImageBuffer RestoreNormal { get; private set; }
		public static ImageBuffer RestoreHover { get; private set; }
		private static ImageBuffer restorePressed;

		public int FontSize7 { get; } = 7;
		public int FontSize9 { get; } = 9;
		public int FontSize10 { get; } = 10;
		public int FontSize11 { get; } = 11;
		public int FontSize12 { get; } = 12;
		public int FontSize14 { get; } = 14;

		internal readonly int shortButtonHeight = 30;
		private readonly int defaultScrollBarWidth = 120;
		private readonly int sideBarButtonWidth = 138;

		public int DefaultFontSize { get; } = 11;
		public int H1PointSize { get; set; } = 11;
		public double ButtonHeight { get; internal set; } = 32;

		/// <summary>
		/// Indicates if icons should be inverted due to black source images on a dark theme
		/// </summary>
		public bool InvertIcons => this.Colors.IsDarkTheme;

		public BorderDouble ButtonSpacing { get; set; } = new BorderDouble(3, 0, 0, 0);
		public BorderDouble ToolbarPadding { get; set; } = 3;

		public LinkButtonFactory LinkButtonFactory { get; private set; }

		public TextImageButtonFactory WhiteButtonFactory { get; private set; }
		public TextImageButtonFactory ButtonFactory { get; private set; }
		public TextImageButtonFactory WizardButtons { get; private set; }
		public TextImageButtonFactory MicroButton { get; private set; }
		public TextImageButtonFactory MicroButtonMenu { get; private set; }

		/// <summary>
		/// Used to make buttons in menu rows where the background color is consistently white
		/// </summary>
		public TextImageButtonFactory MenuButtonFactory { get; private set; }

		public int SplitterWidth => (int)(6 * (GuiWidget.DeviceScale <= 1 ? GuiWidget.DeviceScale : GuiWidget.DeviceScale * 1.4));

		public IThemeColors Colors { get; set; }

		public Color SlightShade { get; } = new Color(0, 0, 0, 40);
		public Color MinimalShade { get; } = new Color(0, 0, 0, 15);
		public Color Shade { get; } = new Color(0, 0, 0, 120);
		public Color DarkShade { get; } = new Color(0, 0, 0, 190);

		public Color ActiveTabColor { get; set; }
		public Color ActiveTabBarBackground { get; set; }
		public Color InactiveTabColor { get; set; }
		public Color InteractionLayerOverlayColor { get; private set; }
		public Color SplitterBackground { get; private set; } = new Color(0, 0, 0, 60);
		public Color TabBodyBackground { get; private set; }
		public Color ToolbarButtonBackground { get; set; } = Color.Transparent;
		public Color ToolbarButtonHover => this.SlightShade;
		public Color ToolbarButtonDown => this.MinimalShade;

		public GuiWidget CreateSearchButton()
		{
			return new IconButton(AggContext.StaticData.LoadIcon("icon_search_24x24.png", 16, 16, this.InvertIcons), this)
			{
				ToolTipText = "Search".Localize(),
			};
		}

		static ThemeConfig()
		{
			// EnsureRestoreButtonImages
			int size = (int)(16 * GuiWidget.DeviceScale);

			if (AggContext.OperatingSystem == OSType.Android)
			{
				RestoreNormal = ColorCircle(size, new Color(200, 0, 0));
			}
			else
			{
				RestoreNormal = ColorCircle(size, Color.Transparent);
			}
			RestoreHover = ColorCircle(size, new Color("#DB4437"));
			restorePressed = ColorCircle(size, new Color(255, 0, 0));
		}

		public ThemeConfig()
		{
		}

		public void RebuildTheme(IThemeColors colors)
		{
			this.Colors = colors;

			DefaultThumbView.ThumbColor = new Color(colors.PrimaryTextColor, 30);

			var commonOptions = new ButtonFactoryOptions();
			commonOptions.NormalTextColor = colors.PrimaryTextColor;
			commonOptions.HoverTextColor = colors.PrimaryTextColor;
			commonOptions.PressedTextColor = colors.PrimaryTextColor;
			commonOptions.DisabledTextColor = colors.TertiaryBackgroundColor;
			commonOptions.Margin = new BorderDouble(14, 0);
			commonOptions.FontSize = this.DefaultFontSize;
			commonOptions.ImageSpacing = 8;
			commonOptions.BorderWidth = 0;
			commonOptions.FixedHeight = 32;

			this.TabBodyBackground = this.ResolveColor(
				colors.TertiaryBackgroundColor,
				new Color(
					Color.White,
					(colors.IsDarkTheme) ? 3 : 25));

			this.ActiveTabColor = this.TabBodyBackground;
			this.ActiveTabBarBackground = this.ActiveTabColor.AdjustLightness(0.85).ToColor();

			// Active tab color with slight transparency
			this.InteractionLayerOverlayColor = new Color(this.ActiveTabColor, 240);

			float alpha0to1 = (colors.IsDarkTheme ? 20 : 60) / 255.0f;

			this.InactiveTabColor = ResolveColor(colors.PrimaryBackgroundColor, new Color(Color.White, this.SlightShade.alpha));

			this.SplitterBackground = this.ActiveTabColor.AdjustLightness(0.87).ToColor();

			this.ButtonFactory = new TextImageButtonFactory(commonOptions);

			this.WizardButtons = new TextImageButtonFactory(new ButtonFactoryOptions(commonOptions)
			{
#if __ANDROID__
				FontSize = this.FontSize14,
				FixedHeight = 34 * GuiWidget.DeviceScale,
				Margin = commonOptions.Margin * 1.2
#endif
			});

			var commonGray = new ButtonFactoryOptions(commonOptions)
			{
				NormalTextColor = Color.Black,
				NormalFillColor = Color.LightGray,
				HoverTextColor = Color.Black,
				PressedTextColor = Color.Black,
				PressedFillColor = Color.LightGray,
			};

			this.MenuButtonFactory = new TextImageButtonFactory(new ButtonFactoryOptions(commonGray)
			{
				Margin = new BorderDouble(8, 0)
			});

			this.MicroButton = new TextImageButtonFactory(new ButtonFactoryOptions()
			{
				FixedHeight = 20 * GuiWidget.DeviceScale,
				FixedWidth = 30 * GuiWidget.DeviceScale,
				FontSize = 8,
				Margin = 0,
				CheckedBorderColor = colors.PrimaryTextColor
			});

			this.MicroButtonMenu = new TextImageButtonFactory(new ButtonFactoryOptions(commonGray)
			{
				FixedHeight = 20 * GuiWidget.DeviceScale,
				FixedWidth = 30 * GuiWidget.DeviceScale,
				FontSize = 8,
				Margin = 0,
				BorderWidth = 1,
				CheckedBorderColor = Color.Black
			});

#region PartPreviewWidget

			WhiteButtonFactory = new TextImageButtonFactory(new ButtonFactoryOptions(commonOptions)
			{
				FixedWidth = sideBarButtonWidth,
				FixedHeight = shortButtonHeight,

				NormalTextColor = Color.Black,
				NormalFillColor = Color.White,
				NormalBorderColor = new Color(colors.PrimaryTextColor, 200),

				HoverTextColor = Color.Black,
				HoverFillColor = new Color(255, 255, 255, 200),
				HoverBorderColor = new Color(colors.PrimaryTextColor, 200),

				BorderWidth = 1,
			});
#endregion

			this.LinkButtonFactory = new LinkButtonFactory()
			{
				fontSize = FontSize11,
				textColor = colors.PrimaryTextColor
			};
		}

		public Color GetBorderColor(int alpha)
		{
			return new Color(this.Colors.SecondaryTextColor, alpha);
		}

		// Compute a fixed color from a source and a target alpha
		public Color ResolveColor(Color background, Color overlay)
		{
			return new BlenderRGBA().Blend(background, overlay);
		}

		public FlowLayoutWidget CreateMenuItems(PopupMenu popupMenu, IEnumerable<NamedAction> menuActions)
		{
			// Create menu items in the DropList for each element in this.menuActions
			popupMenu.CloseAllChildren();
			foreach (var menuAction in menuActions)
			{
				if (menuAction.Title == "----")
				{
					popupMenu.CreateHorizontalLine();
				}
				else
				{
					PopupMenu.MenuItem menuItem;

					if (menuAction is NamedBoolAction boolAction)
					{
						menuItem = popupMenu.CreateBoolMenuItem(menuAction.Title, boolAction.GetIsActive, boolAction.SetIsActive);
					}
					else
					{
						menuItem = popupMenu.CreateMenuItem(menuAction.Title, menuAction.Icon, menuAction.Shortcut);
					}

					menuItem.Name = $"{menuAction.Title} Menu Item";

					menuItem.Enabled = menuAction.Action != null
						&& menuAction.IsEnabled?.Invoke() != false;

					menuItem.ClearRemovedFlag();

					if (menuItem.Enabled)
					{
						menuItem.Click += (s, e) =>
						{
							menuAction.Action();
						};
					}
				}
			}

			return popupMenu;
		}

		private static ImageBuffer ColorCircle(int size, Color color)
		{
			ImageBuffer imageBuffer = new ImageBuffer(size, size);
			Graphics2D normalGraphics = imageBuffer.NewGraphics2D();
			Vector2 center = new Vector2(size / 2.0, size / 2.0);

			Color barColor;
			if (color != Color.Transparent)
			{
				normalGraphics.Circle(center, size / 2.0, color);
				barColor = Color.White;
			}
			else
			{
				barColor = new Color("#999");
			}

			normalGraphics.Line(center + new Vector2(-size / 4.0, -size / 4.0), center + new Vector2(size / 4.0, size / 4.0), barColor, 2 * GuiWidget.DeviceScale);
			normalGraphics.Line(center + new Vector2(-size / 4.0, size / 4.0), center + new Vector2(size / 4.0, -size / 4.0), barColor, 2 * GuiWidget.DeviceScale);

			return imageBuffer;
		}

		internal Button CreateSmallResetButton()
		{
			return new Button(
				new ButtonViewStates(
					new ImageWidget(RestoreNormal),
					new ImageWidget(RestoreHover),
					new ImageWidget(restorePressed),
					new ImageWidget(RestoreNormal)))
			{
				VAnchor = VAnchor.Center,
				Margin = new BorderDouble(0, 0, 5, 0)
			};
		}

		public SolidSlider CreateSolidSlider(GuiWidget wordOptionContainer, string header, double min = 0, double max = .5)
		{
			double scrollBarWidth = 10;

			wordOptionContainer.AddChild(new TextWidget(header, textColor: this.Colors.PrimaryTextColor)
			{
				Margin = new BorderDouble(10, 3, 3, 5),
				HAnchor = HAnchor.Left
			});

			var namedSlider = new SolidSlider(new Vector2(), scrollBarWidth, 0, 1)
			{
				TotalWidthInPixels = defaultScrollBarWidth,
				Minimum = min,
				Maximum = max,
				Margin = new BorderDouble(12, 4),
				HAnchor = HAnchor.Stretch,
			};

			wordOptionContainer.AddChild(namedSlider);

			return namedSlider;
		}

		public MenuItem CreateCheckboxMenuItem(string text, string itemValue, bool itemChecked, BorderDouble padding, EventHandler eventHandler)
		{
			var checkbox = new CheckBox(text)
			{
				Checked = itemChecked
			};
			checkbox.CheckedStateChanged += eventHandler;

			return new MenuItem(checkbox, itemValue)
			{
				Padding = padding,
			};
		}
	}

	public static class ThemeExtensionMethods
	{
		public static SectionWidget ApplyBoxStyle(this SectionWidget sectionWidget)
		{
			return ApplyBoxStyle(sectionWidget, ApplicationController.Instance.Theme.MinimalShade, margin: new BorderDouble(10, 0, 10, 10));
		}

		public static SectionWidget ApplyBoxStyle(this SectionWidget sectionWidget, BorderDouble margin)
		{
			return ApplyBoxStyle(sectionWidget, ApplicationController.Instance.Theme.MinimalShade, margin);
		}

		public static SectionWidget ApplyBoxStyle(this SectionWidget sectionWidget, Color backgroundColor, BorderDouble margin)
		{
			// Enforce panel padding
			// sectionWidget.ContentPanel.Padding = new BorderDouble(10, 0, 10, 2);
			//sectionWidget.ContentPanel.Padding = 0;

			sectionWidget.BorderColor = Color.Transparent;
			sectionWidget.BorderRadius = 5;
			sectionWidget.Margin = margin;
			sectionWidget.BackgroundColor = backgroundColor;

			return sectionWidget;
		}
	}
}