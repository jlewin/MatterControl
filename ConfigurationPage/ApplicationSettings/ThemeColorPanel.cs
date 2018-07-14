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
using MatterHackers.Agg;
using MatterHackers.Agg.UI;

namespace MatterHackers.MatterControl.ConfigurationPage
{
	public class ThemeColorPanel : FlowLayoutWidget
	{
		public ThemeColorPanel(ThemeConfig theme)
			: base (FlowDirection.TopToBottom)
		{
			var previewWidget = new ThemePreviewButton(theme, ApplicationController.ThemeProvider, true)
			{
				HAnchor = HAnchor.Absolute,
				VAnchor = VAnchor.Absolute,
				Width = 80,
				Height = 65,
				Margin = new BorderDouble(5, 15, 10, 10)
			};

			Action<Color> previewTheme = (color) =>
			{
				previewWidget.PreviewTheme(color);
			};

			// Add color selector
			this.AddChild(new ThemeColorSelectorWidget(previewTheme)
			{
				Margin = new BorderDouble(right: 5)
			});

			var previewPanel = new FlowLayoutWidget()
			{
				HAnchor = HAnchor.Stretch,
				VAnchor = VAnchor.Fit,
			};

			previewPanel.AddChild(previewWidget);

			var droplist = new DropDownList("Custom", theme.Colors.PrimaryTextColor, maxHeight: 200, pointSize: theme.DefaultFontSize)
			{
				BorderColor = theme.GetBorderColor(75),
				Margin = new BorderDouble(0, 0, 10, 0)
			};
			droplist.AddItem("Classic Dark");
			droplist.AddItem("Classic Light");
			droplist.AddItem("Modern Dark");

			droplist.SelectionChanged += (s, e) =>
			{
				ApplicationController.ThemeProvider = ApplicationController.GetColorProvider(droplist.SelectedValue);
				UserSettings.Instance.set(UserSettingsKey.ColorThemeProviderName, droplist.SelectedValue);
			};

			previewPanel.AddChild(droplist);

			this.AddChild(previewPanel);
		}
	}
}