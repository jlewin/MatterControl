/*
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

using System;
using System.Linq;
using MatterHackers.Agg;
using MatterHackers.Agg.Image;
using MatterHackers.Agg.Platform;
using MatterHackers.Agg.Transform;
using MatterHackers.Agg.UI;
using MatterHackers.ImageProcessing;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.ConfigurationPage;
using MatterHackers.MatterControl.CustomWidgets;
using MatterHackers.MatterControl.PartPreviewWindow;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl
{
	public class BrandMenuButton : PopupMenuButton
	{
		public BrandMenuButton(ThemeConfig theme)
			: base (theme)
		{
			this.Name = "MatterControl BrandMenuButton";
			this.VAnchor = VAnchor.Fit;
			this.HAnchor = HAnchor.Fit;
			this.Margin = 0;

			this.DynamicPopupContent = () => BrandMenuButton.CreatePopupMenu(theme);

			var row = new FlowLayoutWidget()
			{
				HAnchor = HAnchor.Fit,
				VAnchor = VAnchor.Fit,
			};
			this.AddChild(row);

			row.AddChild(new ThemedIconButton(theme.LoadIcon("material-design", "menu.png"), theme)
			{
				VAnchor = VAnchor.Center,
				Margin = theme.ButtonSpacing,
				Selectable = false
			});

			row.AddChild(new TextWidget(ApplicationController.Instance.ProductName, textColor: theme.TextColor)
			{
				VAnchor = VAnchor.Center
			});

			foreach (var child in this.Children)
			{
				child.Selectable = false;
			}
		}

		private static PopupMenu CreatePopupMenu(ThemeConfig theme)
		{
			var menuTheme = ApplicationController.Instance.MenuTheme;

			var popupMenu = new PopupMenu(menuTheme)
			{
				MinimumSize = new Vector2(300, 0)
			};

			PopupMenu.MenuItem menuItem;

			menuItem = popupMenu.CreateMenuItem("Help".Localize(), StaticData.Instance.LoadIcon("help_page.png", 16, 16).GrayToColor(menuTheme.TextColor));
			menuItem.Click += (s, e) => ApplicationController.Instance.ShowApplicationHelp("Docs");

			menuItem = popupMenu.CreateMenuItem("Interface Tour".Localize(), StaticData.Instance.LoadIcon("tour.png", 16, 16).GrayToColor(menuTheme.TextColor));
			menuItem.Click += (s, e) =>
			{
				UiThread.RunOnIdle(() =>
				{
					DialogWindow.Show<Tour.WelcomePage>();
				});
			};

			menuItem = popupMenu.CreateMenuItem("Settings".Localize(), StaticData.Instance.LoadIcon("fa-cog_16.png", 16, 16).GrayToColor(menuTheme.TextColor));
			menuItem.Click += (s, e) => DialogWindow.Show<ApplicationSettingsPage>();
			menuItem.Name = "Settings MenuItem";

			popupMenu.CreateSeparator();

			menuItem = popupMenu.CreateMenuItem("About".Localize() + " " + ApplicationController.Instance.ProductName);
			menuItem.Click += (s, e) => ApplicationController.Instance.ShowAboutPage();
			return popupMenu;
		}
	}
}
