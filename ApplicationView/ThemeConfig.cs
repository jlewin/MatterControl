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

using System;
using MatterHackers.Agg;
using MatterHackers.Agg.UI;

namespace MatterHackers.MatterControl
{
	using System.Collections.Generic;
	using System.Linq;
	using Agg.Image;
	using CustomWidgets;
	using MatterHackers.Agg.Platform;
	using MatterHackers.Localizations;
	using MatterHackers.MatterControl.PartPreviewWindow;
	using MatterHackers.MatterControl.PrinterCommunication;
	using MatterHackers.VectorMath;
	using Newtonsoft.Json;

	public interface IColorTheme
	{
		IEnumerable<Color> GetColors();
		ThemeSet GetTheme(Color accentColor);
	}

	public class SolarizedTheme : IColorTheme
	{
		private bool darkTheme;

		private SolarizedColors solarized = new SolarizedColors();


		public SolarizedTheme(bool darkTheme)
		{
			this.darkTheme = darkTheme;
		}

		public IEnumerable<Color> GetColors()
		{
			return new[]
			{
				solarized.Blue,
				solarized.Cyan,
				solarized.Green,
				solarized.Magenta,
				solarized.Orange,
				solarized.Red,
				solarized.Violet,
				solarized.Yellow
			};
		}

		public ThemeSet GetTheme(Color accentColor)
		{
			var baseColors = darkTheme ? solarized.Dark : solarized.Light;

			return new ThemeSet()
			{
				Theme = new ThemeConfig()
				{
					Colors = new ThemeColors()
					{
						IsDarkTheme = darkTheme,
						Name = "ClassicThemeColors",
						Transparent = new Color("#00000000"),
						SecondaryTextColor = new Color("#C8C8C8"),
						PrimaryBackgroundColor = new Color("#003F69"),
						SecondaryBackgroundColor = new Color("#1B511C"),
						TertiaryBackgroundColor = new Color("#7A1F1F"),
						PrimaryTextColor = baseColors.Base0,
						PrimaryAccentColor = accentColor,
						SourceColor = baseColors.Base0
					},
					PresetColors = new PresetColors()
					{
						MaterialPreset = new Color("#FF7F00"),
						QualityPreset = new Color("#FFFF00"),
						UserOverride = new Color("#445FDC96")
					},

					SlightShade = new Color("#00000028"),
					MinimalShade = new Color("#0000000F"),
					Shade = new Color("#00000078"),
					DarkShade = new Color("#000000BE"),

					ActiveTabColor = baseColors.Base03,
					TabBarBackground = baseColors.Base03.Blend(Color.Black, darkTheme ? 0.4 : 0.1),
					//TabBarBackground = new Color(darkTheme ? "#00212B" : "#EEE8D5"),

					InactiveTabColor = baseColors.Base02,
					InteractionLayerOverlayColor = new Color(baseColors.Base03, 240),
					SplitterBackground = baseColors.Base02,
					TabBodyBackground = new Color("#00000000"),
					ToolbarButtonBackground = new Color("#00000000"),
					ThumbnailBackground = new Color("#00000000"),
					AccentMimimalOverlay = new Color(accentColor, 80),
				},
				MenuTheme = new ThemeConfig()
				{
					Colors = new ThemeColors()
					{
						IsDarkTheme = false,
						Name = "MenuColors",
						Transparent = new Color("#00000000"),
						SecondaryTextColor = new Color("#666666"),
						PrimaryBackgroundColor = baseColors.Base02,
						SecondaryBackgroundColor = new Color("#DDDDDD"),
						TertiaryBackgroundColor = new Color("#CCCCCC"),
						PrimaryTextColor = baseColors.Base1,
						PrimaryAccentColor = accentColor,
						SourceColor = new Color("#00FF00")
					},
					PresetColors = new PresetColors()
					{
						MaterialPreset = new Color("#FF7F00"),
						QualityPreset = new Color("#FFFF00"),
						UserOverride = new Color("#445FDC96")
					},
					SlightShade = new Color("#00000028"),
					MinimalShade = new Color("#0000000F"),
					Shade = new Color("#00000078"),
					DarkShade = new Color("#000000BE"),

					ActiveTabColor = baseColors.Base02,
					TabBarBackground = new Color("#B1B1B1"),
					InactiveTabColor = new Color("#D0D0D0"),
					InteractionLayerOverlayColor = new Color("#D0D0D0F0"),
					SplitterBackground = new Color("#B5B5B5"),
					TabBodyBackground = new Color("#00000000"),
					ToolbarButtonBackground = new Color("#00000000"),
					ThumbnailBackground = new Color("#00000000"),
					AccentMimimalOverlay = new Color(accentColor, 80),
				}
			};
		}

		public Color GetAdjustedAccentColor(Color accentColor, Color backgroundColor)
		{
			return ThemeColors.GetAdjustedAccentColor(accentColor, backgroundColor);
		}

		private class SolarizedColors
		{
			public BaseColors Dark { get; } = new BaseColors()
			{
				Base03 = new Color("#002b36"),
				Base02 = new Color("#073642"),
				Base01 = new Color("#586e75"),
				Base00 = new Color("#657b83"),
				Base0 = new Color("#839496"),
				Base1 = new Color("#93a1a1"),
				Base2 = new Color("#eee8d5"),
				Base3 = new Color("#fdf6e3")
			};

			public BaseColors Light { get; } = new BaseColors()
			{
				Base03 = new Color("#fdf6e3"),
				Base02 = new Color("#eee8d5"),
				Base01 = new Color("#93a1a1"),
				Base00 = new Color("#839496"),
				Base0 = new Color("#657b83"),
				Base1 = new Color("#586e75"),
				Base2 = new Color("#073642"),
				Base3 = new Color("#002b36")
			};

			public Color Yellow { get; } = new Color("#b58900");
			public Color Orange { get; } = new Color("#cb4b16");
			public Color Red { get; } = new Color("#dc322f");
			public Color Magenta { get; } = new Color("#d33682");
			public Color Violet { get; } = new Color("#6c71c4");
			public Color Blue { get; } = new Color("#268bd2");
			public Color Cyan { get; } = new Color("#2aa198");
			public Color Green { get; } = new Color("#859900");
		}

		private class BaseColors
		{
			public Color Base03 { get; set; }
			public Color Base02 { get; set; }
			public Color Base01 { get; set; }
			public Color Base00 { get; set; }
			public Color Base0 { get; set; }
			public Color Base1 { get; set; }
			public Color Base2 { get; set; }
			public Color Base3 { get; set; }
		}
	}

	public class AltThemeColors : IColorTheme
	{
		public AltThemeColors()	{ }

		public IEnumerable<Color> GetColors()
		{
			return new[]
			{
				Color.Red,
				Color.Blue,
				Color.Green
			};
		}

		public ThemeSet GetTheme(Color accentColor)
		{
			var primaryBackgroundColor = new Color("#003F69");

			var colors = new ThemeColors
			{
				IsDarkTheme = true,
				Name = "xxx",
				SourceColor = accentColor,
				PrimaryBackgroundColor = primaryBackgroundColor,
				SecondaryBackgroundColor = new Color("#1B511C"),
				TertiaryBackgroundColor = new Color("#7A1F1F"),
				PrimaryTextColor = new Color("#FFFFFF"),
				SecondaryTextColor = new Color("#C8C8C8"),

				PrimaryAccentColor = GetAdjustedAccentColor(accentColor, primaryBackgroundColor)
			};

			return ClassicThemeColors.ThemeFromColors(colors);
		}

		public Color GetAdjustedAccentColor(Color accentColor, Color backgroundColor)
		{
			return ThemeColors.GetAdjustedAccentColor(accentColor, backgroundColor);
		}
	}

	public class ClassicThemeColors : IColorTheme
	{
		public ClassicThemeColors(bool darkTheme)
		{
			this.DarkTheme = darkTheme;
		}

		public IEnumerable<Color> GetColors()
		{
			return Colors.Values.Take(Colors.Count / 2);
		}

		public static Dictionary<string, Color> Colors = new Dictionary<string, Color>()
		{
			//Dark themes
			{ "Red - Dark", new Color(172, 25, 61) },
			{ "Pink - Dark", new Color(220, 79, 173) },
			{ "Orange - Dark", new Color(255, 129, 25) },
			{ "Green - Dark", new Color(0, 138, 23) },
			{ "Blue - Dark", new Color(0, 75, 139) },
			{ "Teal - Dark", new Color(0, 130, 153) },
			{ "Light Blue - Dark", new Color(93, 178, 255) },
			{ "Purple - Dark", new Color(70, 23, 180) },
			{ "Magenta - Dark", new Color(140, 0, 149) },
			{ "Grey - Dark", new Color(88, 88, 88) },

			//Light themes
			{ "Red - Light", new Color(172, 25, 61) },
			{ "Pink - Light", new Color(220, 79, 173) },
			{ "Orange - Light", new Color(255, 129, 25) },
			{ "Green - Light", new Color(0, 138, 23) },
			{ "Blue - Light", new Color(0, 75, 139) },
			{ "Teal - Light", new Color(0, 130, 153) },
			{ "Light Blue - Light", new Color(93, 178, 255) },
			{ "Purple - Light", new Color(70, 23, 180) },
			{ "Magenta - Light", new Color(140, 0, 149) },
			{ "Grey - Light", new Color(88, 88, 88) },
		};

		public bool DarkTheme { get; set; }

		public ThemeSet GetTheme(Color accentColor)
		{
			var colors = ThemeColors.Create("Unknown", accentColor, this.DarkTheme);

			return ThemeFromColors(colors);
		}

		public static ThemeSet ThemeFromColors(ThemeColors colors)
		{
			var json = JsonConvert.SerializeObject(colors);

			var clonedColors = JsonConvert.DeserializeObject<ThemeColors>(json);
			clonedColors.IsDarkTheme = false;
			clonedColors.Name = "MenuColors";
			clonedColors.PrimaryTextColor = new Color("#222");
			clonedColors.SecondaryTextColor = new Color("#666");
			clonedColors.PrimaryBackgroundColor = new Color("#fff");
			clonedColors.SecondaryBackgroundColor = new Color("#ddd");
			clonedColors.TertiaryBackgroundColor = new Color("#ccc");

			return new ThemeSet()
			{
				Theme = BuildTheme(colors),
				MenuTheme = BuildTheme(clonedColors)
			};
		}

		private Color GetAdjustedAccentColor(Color accentColor, Color backgroundColor)
		{
			return ThemeColors.GetAdjustedAccentColor(accentColor, backgroundColor);
		}

		private static ThemeConfig BuildTheme(ThemeColors colors)
		{
			var theme = new ThemeConfig();

			theme.Colors = colors;

			theme.ActiveTabColor = theme.ResolveColor(
				colors.TertiaryBackgroundColor,
				new Color(
					Color.White,
					(colors.IsDarkTheme) ? 3 : 25));
			theme.TabBarBackground = theme.ActiveTabColor.AdjustLightness(0.85).ToColor();
			theme.ThumbnailBackground = theme.MinimalShade;
			theme.AccentMimimalOverlay = new Color(theme.Colors.PrimaryAccentColor, 50);
			theme.InteractionLayerOverlayColor = new Color(theme.ActiveTabColor, 240);
			theme.InactiveTabColor = theme.ResolveColor(theme.ActiveTabColor, new Color(Color.White, theme.MinimalShade.alpha));
			theme.SplitterBackground = theme.ActiveTabColor.AdjustLightness(0.87).ToColor();

			theme.PresetColors = new PresetColors();

			theme.SlightShade = new Color(0, 0, 0, 40);
			theme.MinimalShade = new Color(0, 0, 0, 15);
			theme.Shade = new Color(0, 0, 0, 120);
			theme.DarkShade = new Color(0, 0, 0, 190);

			return theme;
		}

	}

	public class ThemeConfig
	{
		private static ImageBuffer restoreNormal;
		private static ImageBuffer restoreHover;
		private static ImageBuffer restorePressed;

		public int FontSize7 { get; } = 7;
		public int FontSize8 { get; } = 8;
		public int FontSize9 { get; } = 9;
		public int FontSize10 { get; } = 10;
		public int FontSize11 { get; } = 11;
		public int FontSize12 { get; } = 12;
		public int FontSize14 { get; } = 14;

		public int DefaultFontSize { get; set; } = 11;
		public int DefaultContainerPadding { get; } = 10;
		public int H1PointSize { get; } = 11;

		public double ButtonHeight => 32 * GuiWidget.DeviceScale;
		public double TabButtonHeight => 30 * GuiWidget.DeviceScale;
		public double MenuGutterWidth => 35 * GuiWidget.DeviceScale;

		private double microButtonHeight => 20 * GuiWidget.DeviceScale;
		private double microButtonWidth => 30 * GuiWidget.DeviceScale;
		private readonly int defaultScrollBarWidth = 120;

		/// <summary>
		/// Indicates if icons should be inverted due to black source images on a dark theme
		/// </summary>
		public bool InvertIcons => this?.Colors.IsDarkTheme ?? false;

		internal void ApplyPrimaryActionStyle(GuiWidget guiWidget)
		{
			guiWidget.BackgroundColor = this.AccentMimimalOverlay;

			Color hoverColor = new Color(this.AccentMimimalOverlay, 90);

			switch (guiWidget)
			{
				case PopupMenuButton menuButton:
					menuButton.HoverColor = hoverColor;
					break;
				case SimpleFlowButton flowButton:
					flowButton.HoverColor = hoverColor;
					break;
				case SimpleButton button:
					button.HoverColor = hoverColor;
					break;
			}
		}

		internal void RemovePrimaryActionStyle(GuiWidget guiWidget)
		{
			guiWidget.BackgroundColor = Color.Transparent;

			switch (guiWidget)
			{
				case PopupMenuButton menuButton:
					menuButton.HoverColor = Color.Transparent;
					break;
				case SimpleFlowButton flowButton:
					flowButton.HoverColor = Color.Transparent;
					break;
				case SimpleButton button:
					button.HoverColor = Color.Transparent;
					break;
			}
		}

		public BorderDouble TextButtonPadding { get; } = new BorderDouble(14, 0);

		public BorderDouble ButtonSpacing { get; } = new BorderDouble(right: 3);

		public BorderDouble ToolbarPadding { get; } = 3;

		public BorderDouble TabbarPadding { get; } = new BorderDouble(3, 1);

		/// <summary>
		/// The height or width of a given vertical or horizontal splitter bar
		/// </summary>
		public int SplitterWidth
		{
			get
			{
				double splitterSize = 6 * GuiWidget.DeviceScale;

				if (GuiWidget.TouchScreenMode)
				{
					splitterSize *= 1.4;
				}

				return (int)splitterSize;
			}
		}

		public ThemeColors Colors { get; set; } = new ThemeColors();

		public PresetColors PresetColors { get; set; }

		public Color SlightShade { get; set; }
		public Color MinimalShade { get; set; }
		public Color Shade { get; set; }
		public Color DarkShade { get; set; }

		public Color ActiveTabColor { get; set; }
		public Color TabBarBackground { get; set; }
		public Color InactiveTabColor { get; set; }
		public Color InteractionLayerOverlayColor { get; set; }

		public TextWidget CreateHeading(string text)
		{
			return new TextWidget(text, pointSize: this.H1PointSize, textColor: this.Colors.PrimaryTextColor, bold: true)
			{
				Margin = new BorderDouble(0, 5)
			};
		}

		public Color SplitterBackground { get; set; } = new Color(0, 0, 0, 60);
		public Color TabBodyBackground { get; set; }
		public Color ToolbarButtonBackground { get; set; } = Color.Transparent;
		public Color ToolbarButtonHover => this.SlightShade;
		public Color ToolbarButtonDown => this.MinimalShade;

		public Color ThumbnailBackground { get; set; }
		public Color AccentMimimalOverlay { get; set; }
		public BorderDouble SeparatorMargin { get; }

		public ImageBuffer GeneratingThumbnailIcon { get; private set; }

		public GuiWidget CreateSearchButton()
		{
			return new IconButton(AggContext.StaticData.LoadIcon("icon_search_24x24.png", 16, 16, this.InvertIcons), this)
			{
				ToolTipText = "Search".Localize(),
			};
		}

		public ThemeConfig()
		{
			this.SeparatorMargin = (this.ButtonSpacing * 2).Clone(left: this.ButtonSpacing.Right);


			//this.Colors = ActiveTheme.Instance as ThemeColors;

			this.RebuildTheme();
		}

		public void RebuildTheme()
		{
			int size = (int)(16 * GuiWidget.DeviceScale);

			// In TouchScreenMode, use red icon as no hover, otherwise transparent and red on hover
			restoreNormal = ColorCircle(size, (GuiWidget.TouchScreenMode) ? new Color(200, 0, 0) : Color.Transparent);
			restoreHover = ColorCircle(size, new Color("#DB4437"));
			restorePressed = ColorCircle(size, new Color(255, 0, 0));

			this.GeneratingThumbnailIcon = AggContext.StaticData.LoadIcon("building_thumbnail_40x40.png", 40, 40, this.InvertIcons);

			DefaultThumbView.ThumbColor = new Color(this.Colors.PrimaryTextColor, 30);
		}

		public JogControls.MoveButton CreateMoveButton(PrinterConfig printer, string label, PrinterConnection.Axis axis, double movementFeedRate, bool levelingButtons = false)
		{
			return new JogControls.MoveButton(label, printer, axis, movementFeedRate, this)
			{
				BackgroundColor = this.MinimalShade,
				Border = 1,
				BorderColor = this.GetBorderColor(40),
				VAnchor = VAnchor.Absolute,
				HAnchor = HAnchor.Absolute,
				Margin = 0,
				Padding = 0,
				Height = (levelingButtons ? 45 : 40) * GuiWidget.DeviceScale,
				Width = (levelingButtons ? 90 : 40) * GuiWidget.DeviceScale,
			};
		}

		public JogControls.ExtrudeButton CreateExtrudeButton(PrinterConfig printer, string label, double movementFeedRate, int extruderNumber, bool levelingButtons = false)
		{
			return new JogControls.ExtrudeButton(printer, label, movementFeedRate, extruderNumber, this)
			{
				BackgroundColor = this.MinimalShade,
				Border = 1,
				BorderColor = this.GetBorderColor(40),
				VAnchor = VAnchor.Absolute,
				HAnchor = HAnchor.Absolute,
				Margin = 0,
				Padding = 0,
				Height = (levelingButtons ? 45 : 40) * GuiWidget.DeviceScale,
				Width = (levelingButtons ? 90 : 40) * GuiWidget.DeviceScale,
			};
		}

		public RadioTextButton CreateMicroRadioButton(string text, IList<GuiWidget> siblingRadioButtonList = null)
		{
			var radioButton = new RadioTextButton(text, this, this.FontSize8)
			{
				SiblingRadioButtonList = siblingRadioButtonList,
				Padding = new BorderDouble(5, 0),
				SelectedBackgroundColor = this.SlightShade,
				UnselectedBackgroundColor = this.SlightShade,
				HoverColor = this.AccentMimimalOverlay,
				Margin = new BorderDouble(right: 1),
				HAnchor = HAnchor.Absolute,
				Height = this.microButtonHeight,
				Width = this.microButtonWidth
			};

			// Add to sibling list if supplied
			siblingRadioButtonList?.Add(radioButton);

			return radioButton;
		}

		public TextButton CreateLightDialogButton(string text)
		{
			return CreateDialogButton(text, new Color(Color.White, 15), new Color(Color.White, 25));
		}

		public TextButton CreateDialogButton(string text)
		{
			return CreateDialogButton(text, this.MinimalShade, this.SlightShade);
		}

		public TextButton CreateDialogButton(string text, Color backgroundColor, Color hoverColor)
		{
#if !__ANDROID__
			return new TextButton(text, this)
			{
				BackgroundColor = backgroundColor,
				HoverColor = hoverColor
			};
#else
			var button = new TextButton(text, this, this.FontSize14)
			{
				BackgroundColor = backgroundColor,
				HoverColor = hoverColor,
				// Enlarge button height and margin on Android
				Height = 34 * GuiWidget.DeviceScale,
			};
			button.Padding = button.Padding * 1.2;

			return button;
#endif
		}

		public Color GetBorderColor(int alpha)
		{
			return new Color(this.Colors.SecondaryTextColor, alpha);
		}

		// Compute a fixed color from a source and a target alpha
		public Color ResolveColor(Color background, Color overlay)
		{
			return new BlenderBGRA().Blend(background, overlay);
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
					new ImageWidget(restoreNormal),
					new ImageWidget(restoreHover),
					new ImageWidget(restorePressed),
					new ImageWidget(restoreNormal)))
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

		public SectionWidget ApplyBoxStyle(SectionWidget sectionWidget)
		{
			return ApplyBoxStyle(
				sectionWidget,
				this.MinimalShade,
				margin: new BorderDouble(this.DefaultContainerPadding, 0, this.DefaultContainerPadding, this.DefaultContainerPadding));
		}

		public SectionWidget ApplyBoxStyle(SectionWidget sectionWidget, BorderDouble margin)
		{
			return ApplyBoxStyle(sectionWidget, this.MinimalShade, margin);
		}

		public SectionWidget ApplyBoxStyle(SectionWidget sectionWidget, Color backgroundColor, BorderDouble margin)
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

	public class PresetColors
	{
		public Color MaterialPreset { get; set; } = Color.Orange;
		public Color QualityPreset { get; set; } = Color.Yellow;
		public Color UserOverride { get; set; } = new Color(68, 95, 220, 150);
	}
}