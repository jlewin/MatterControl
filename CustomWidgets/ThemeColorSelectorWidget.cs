/*
Copyright (c) 2018, Kevin Pope, John Lewin
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
using System.Linq;
using MatterHackers.Agg;
using MatterHackers.Agg.UI;
using MatterHackers.MatterControl.PartPreviewWindow;

namespace MatterHackers.MatterControl
{
	public class ThemeColorSelectorWidget : FlowLayoutWidget
	{
		private int containerHeight = (int)(20 * GuiWidget.DeviceScale);
		private Action<Color> previewTheme;

		public ThemeColorSelectorWidget(Action<Color> previewTheme)
		{
			this.Padding = new BorderDouble(2, 0);
			this.previewTheme = previewTheme;

			var themeColors = ClassicThemeColors.Colors;

			foreach(var color in themeColors.Values.Take(themeColors.Count /2))
			{
				var themeButton = CreateThemeButton(color);
				themeButton.Width = containerHeight;

				this.AddChild(themeButton);
			}
		}

		public ColorButton CreateThemeButton(Color color)
		{
			var colorButton = new ColorButton(color)
			{
				Cursor = Cursors.Hand,
				Width = containerHeight,
				Height = containerHeight,
				Margin = 2
			};
			colorButton.Click += (s, e) =>
			{
				ApplicationController.Instance.Theme.SetTheme(colorButton.BackgroundColor);
			};

			colorButton.MouseEnterBounds += (s, e) =>
			{
				previewTheme(colorButton.BackgroundColor);
			};

			return colorButton;
		}
	}
}