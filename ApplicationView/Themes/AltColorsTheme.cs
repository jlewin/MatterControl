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

	public class AltColorsTheme : IColorTheme
	{
		public AltColorsTheme()	{ }

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

			return ClassicColorsTheme.ThemeFromColors(colors);
		}

		public Color GetAdjustedAccentColor(Color accentColor, Color backgroundColor)
		{
			return ThemeColors.GetAdjustedAccentColor(accentColor, backgroundColor);
		}
	}
}