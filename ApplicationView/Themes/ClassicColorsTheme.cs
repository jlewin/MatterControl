﻿/*
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

using MatterHackers.Agg;
using MatterHackers.Agg.UI;

namespace MatterHackers.MatterControl
{
	using System.Collections.Generic;
	using System.Linq;
	using Newtonsoft.Json;

	public class ClassicColorsTheme : IColorTheme
	{
		public ClassicColorsTheme(bool darkTheme)
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

			return ThemeFromColors(colors, this.DarkTheme);
		}

		public static ThemeSet ThemeFromColors(ThemeColors colors, bool darkTheme)
		{
			var json = JsonConvert.SerializeObject(colors);

			var clonedColors = JsonConvert.DeserializeObject<ThemeColors>(json);
			clonedColors.IsDarkTheme = false;
			clonedColors.PrimaryTextColor = new Color("#222");
			clonedColors.PrimaryBackgroundColor = new Color("#fff");

			return new ThemeSet()
			{
				Theme = BuildTheme(colors, darkTheme),
				MenuTheme = BuildTheme(clonedColors, darkTheme)
			};
		}

		private static ThemeConfig BuildTheme(ThemeColors colors, bool darkTheme)
		{
			var theme = new ThemeConfig();

			theme.Colors = colors;

			theme.SlightShade = new Color(0, 0, 0, 40);
			theme.MinimalShade = new Color(0, 0, 0, 15);
			theme.Shade = new Color(0, 0, 0, 120);
			theme.DarkShade = new Color(0, 0, 0, 190);

			theme.ActiveTabColor = theme.ResolveColor(
				new Color(darkTheme ? "#3E3E3E" : "#BEBEBE"),
				new Color(
					Color.White,
					(colors.IsDarkTheme) ? 3 : 25));
			theme.TabBarBackground = theme.ActiveTabColor.AdjustLightness(0.85).ToColor();
			theme.ThumbnailBackground = theme.MinimalShade;
			theme.AccentMimimalOverlay = new Color(theme.Colors.PrimaryAccentColor, 50);
			theme.InteractionLayerOverlayColor = new Color(theme.ActiveTabColor, 240);
			theme.InactiveTabColor = theme.ResolveColor(theme.ActiveTabColor, new Color(Color.White, theme.MinimalShade.alpha));

			theme.SplitterBackground = theme.ActiveTabColor.AdjustLightness(0.87).ToColor();

			theme.BorderColor = new Color(darkTheme ? "#C8C8C8" : "#333");

			theme.PresetColors = new PresetColors();

			return theme;
		}
	}
}