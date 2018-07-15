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

namespace MatterHackers.MatterControl.ConfigurationPage
{
	public class ThemeColorPanel : FlowLayoutWidget
	{
		private ThemePreviewButton previewWidget;
		private Color lastColor;
		private ThemeColorSelectorWidget colorSelector;

		public IColorTheme ThemeProvider { get; private set; }

		public ThemeColorPanel(ThemeConfig theme)
			: base (FlowDirection.TopToBottom)
		{
			string currentProvider = UserSettings.Instance.get(UserSettingsKey.ColorThemeProviderName);

			if (AppContext.ThemeProviders.TryGetValue(currentProvider, out IColorTheme themeProvider))
			{
				this.ThemeProvider = themeProvider;
			}
			else
			{
				this.ThemeProvider = AppContext.ThemeProviders.Values.First();
			}

			previewWidget = new ThemePreviewButton(theme, this, true)
			{
				HAnchor = HAnchor.Absolute,
				VAnchor = VAnchor.Absolute,
				Width = 80,
				Height = 65,
				Margin = new BorderDouble(5, 15, 10, 10),
				ActiveThemeSet = AppContext.ThemeSet
			};

			// Add color selector
			this.AddChild(colorSelector = new ThemeColorSelectorWidget(this)
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

			int i = 0;

			foreach (var item in AppContext.ThemeProviders)
			{
				var newItem = droplist.AddItem(item.Key);

				if (item.Value == themeProvider)
				{
					droplist.SelectedIndex = i;
				}

				i++;
			}

			droplist.SelectionChanged += (s, e) =>
			{
				if (AppContext.ThemeProviders.TryGetValue(droplist.SelectedValue, out IColorTheme provider))
				{
					this.ThemeProvider = provider;

					var previewColor = provider.GetColors().First();

					colorSelector.RebuildColorButtons();

					this.previewWidget.ActiveThemeSet = provider.GetTheme(previewColor);
					previewWidget.PreviewThemeColor(previewColor);

					UserSettings.Instance.set(UserSettingsKey.ColorThemeProviderName, droplist.SelectedValue);
				}
			};

			previewPanel.AddChild(droplist);

			this.AddChild(previewPanel);
		}

		public void PreviewTheme(Color accentColor)
		{
			previewWidget.PreviewThemeColor(accentColor);
		}

		public void SetThemeColor(Color accentColor)
		{
			lastColor = accentColor;
			AppContext.SetTheme(ThemeProvider.GetTheme(accentColor));
		}

		public class ThemeColorSelectorWidget : FlowLayoutWidget
		{
			private int containerHeight = (int)(20 * GuiWidget.DeviceScale);
			private ThemeColorPanel themeColorPanel;

			public ThemeColorSelectorWidget(ThemeColorPanel themeColorPanel)
			{
				this.Padding = new BorderDouble(2, 0);
				this.themeColorPanel = themeColorPanel;

				this.RebuildColorButtons();
			}

			public void RebuildColorButtons()
			{
				this.CloseAllChildren();

				foreach (var color in themeColorPanel.ThemeProvider.GetColors())
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
					themeColorPanel.SetThemeColor(colorButton.BackgroundColor);
				};

				colorButton.MouseEnterBounds += (s, e) =>
				{
					themeColorPanel.PreviewTheme(colorButton.BackgroundColor);
				};

				return colorButton;
			}
		}
	}
}