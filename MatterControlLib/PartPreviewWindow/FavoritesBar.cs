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

using System;
using MatterHackers.Agg;
using MatterHackers.Agg.Platform;
using MatterHackers.Agg.UI;
using MatterHackers.ImageProcessing;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.CustomWidgets;
using MatterHackers.MatterControl.Library;

namespace MatterHackers.MatterControl.PartPreviewWindow
{
    public class FavoritesBar : GuiWidget
	{
		public FavoritesBar(ThemeConfig theme)
		{
			VAnchor = VAnchor.Stretch;
			HAnchor = HAnchor.Fit;
			Border = new BorderDouble(top: 1, right: 1);
			BorderColor = theme.BorderColor20;

			bool expanded = UserSettings.Instance.get(UserSettingsKey.FavoritesBarExpansion) != "0";

			var favoritesBarContext = new LibraryConfig()
			{
				ActiveContainer = ApplicationController.Instance.Library.RootLibaryContainer
			};

			var favoritesBar = new LibraryListView(favoritesBarContext, theme)
			{
				Name = "LibraryView",
				// Drop containers
				ContainerFilter = (container) => false,
				// HAnchor = HAnchor.Fit,
				HAnchor = HAnchor.Absolute,
				VAnchor = VAnchor.Stretch,
				AllowContextMenu = false,
				ActiveSort = SortKey.ModifiedDate,
				Ascending = true,
				// restore to state for favorites bar size
				Width = expanded ? 55 * GuiWidget.DeviceScale : 33 * GuiWidget.DeviceScale,
				ListContentView = new IconView(theme, expanded ? 48 * GuiWidget.DeviceScale : 24 * GuiWidget.DeviceScale)
				{
					VAnchor = VAnchor.Fit | VAnchor.Top
				},
			};

			// favoritesBar.ScrollArea.HAnchor = HAnchor.Fit;
			favoritesBar.ListContentView.HAnchor = HAnchor.Fit;
			this.AddChild(favoritesBar);

			void UpdateWidth(object s, EventArgs e)
			{
				if (s is GuiWidget widget)
				{
					favoritesBar.Width = widget.Width;
				}
			}

			favoritesBar.ListContentView.BoundsChanged += UpdateWidth;

			favoritesBar.ScrollArea.VAnchor = VAnchor.Fit;

			favoritesBar.VerticalScrollBar.Show = ScrollBar.ShowState.Never;

			var expandedImage = StaticData.Instance.LoadIcon("expand.png", 16, 16).GrayToColor(theme.TextColor);
			var collapsedImage = StaticData.Instance.LoadIcon("collapse.png", 16, 16).GrayToColor(theme.TextColor);

			var expandBarButton = new ThemedIconButton(expanded ? collapsedImage : expandedImage, theme)
			{
				HAnchor = HAnchor.Center,
				VAnchor = VAnchor.Absolute | VAnchor.Bottom,
				Margin = new BorderDouble(bottom: 3, top: 3),
				Height = theme.ButtonHeight - 6 * GuiWidget.DeviceScale,
				Width = theme.ButtonHeight - 6 * GuiWidget.DeviceScale,
				ToolTipText = expanded ? "Reduced Width".Localize() : "Expand Width".Localize(),
			};

			expandBarButton.Click += (s, e) => UiThread.RunOnIdle(async () =>
			{
				expanded = !expanded;

				// remove from the one we are deleting
				favoritesBar.ListContentView.BoundsChanged -= UpdateWidth;
				UserSettings.Instance.set(UserSettingsKey.FavoritesBarExpansion, expanded ? "1" : "0");
				favoritesBar.ListContentView = new IconView(theme, expanded ? 48 * GuiWidget.DeviceScale : 24 * GuiWidget.DeviceScale);
				favoritesBar.ListContentView.HAnchor = HAnchor.Fit;
				// add to the one we created
				favoritesBar.ListContentView.BoundsChanged += UpdateWidth;
				expandBarButton.SetIcon(expanded ? collapsedImage : expandedImage);
				expandBarButton.Invalidate();
				expandBarButton.ToolTipText = expanded ? "Reduced Width".Localize() : "Expand Width".Localize();

				await favoritesBar.Reload();
				UpdateWidth(favoritesBar.ListContentView, null);
			});
			this.AddChild(expandBarButton);

			favoritesBar.Margin = new BorderDouble(bottom: expandBarButton.Height + expandBarButton.Margin.Height);


		}
	}
}
