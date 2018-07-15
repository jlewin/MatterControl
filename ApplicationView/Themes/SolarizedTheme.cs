﻿/*
Copyright (c) 2018, John Lewin
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

using MatterHackers.Agg;
using MatterHackers.Agg.UI;

namespace MatterHackers.MatterControl
{
	using System.Collections.Generic;
	using Agg.Image;

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
						IsDarkTheme = true,
						Name = "MenuColors",
						Transparent = new Color("#00000000"),
						SecondaryTextColor = new Color("#666666"),
						PrimaryBackgroundColor = new Color("#2d2f31"),
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

					ActiveTabColor = new Color("#2d2f31"),
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
}