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

using System;
using System.Collections.Generic;
using MatterHackers.Agg;
using MatterHackers.Agg.Image;
using MatterHackers.Agg.Platform;
using MatterHackers.Agg.UI;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl.CustomWidgets
{
	public class IconListView : FlowLayoutWidget, IListContentView
	{
		public int ThumbWidth { get; set; } = 100;
		public int ThumbHeight { get; set; } = 100;

		private FlowLayoutWidget rowButtonContainer = null;

		private int cellIndex = 0;
		private int columnCount = 1;
		private int iconViewPadding = IconViewItem.ItemPadding;
		private double leftRightMargin;
		private int reflownWidth = -1;
		private ThemeConfig theme;

		private List<IconViewItem> allIconViews = new List<IconViewItem>();

		public IconListView(ThemeConfig theme, int thumbnailSize = -1)
			: base(FlowDirection.TopToBottom)
		{
			this.theme = theme;

			if (thumbnailSize != -1)
			{
				this.ThumbHeight = thumbnailSize;
				this.ThumbWidth = thumbnailSize;
			}
		}

		public override void OnBoundsChanged(EventArgs e)
		{
			int currentWidth = (int)this.Size.X;
			if (reflownWidth != currentWidth)
			{
				reflownWidth = currentWidth;

				int newColumnCount = RecomputeFlowValues();
				if (newColumnCount != columnCount)
				{
					columnCount = newColumnCount;

					// Reflow Children
					foreach (var iconView in allIconViews)
					{
						iconView.Parent.RemoveChild(iconView);
						iconView.Margin = new BorderDouble(leftRightMargin, 0);
					}

					this.CloseAllChildren();

					foreach (var iconView in allIconViews)
					{
						iconView.ClearRemovedFlag();
						AddColumnAndChild(iconView);
					}
				}
				else
				{
					foreach (var iconView in allIconViews)
					{
						iconView.Margin = new BorderDouble(leftRightMargin, 0);
					}
				}
			}

			base.OnBoundsChanged(e);
		}

		private int RecomputeFlowValues()
		{
			int scaledWidth = (int)(ThumbWidth * GuiWidget.DeviceScale);
			int itemWidth = scaledWidth + (iconViewPadding * 2);

			int newColumnCount = (int)Math.Floor(this.LocalBounds.Width / itemWidth);
			int remainingSpace = (int)this.LocalBounds.Width - newColumnCount * itemWidth;

			// Reset position before reflow
			cellIndex = 0;
			rowButtonContainer = null;

			// There should always be at least one visible column
			if (newColumnCount < 1)
			{
				newColumnCount = 1;
				remainingSpace = 0;
			}

			// Only center items if extra space exists

			// we find the space we want between each column and the sides
			double spacePerColumn = (remainingSpace > 0) ? remainingSpace / (newColumnCount + 1) : 0;

			// set the margin to be 1/2 the space (it will happen on each side of each icon)

			// put in padding to get the "other" side of the outside icons
			double leftRightMarginRaw = (remainingSpace > 0 ? spacePerColumn / 2 : 0);

			// Inflate to account for scaling
			leftRightMargin = Math.Ceiling(leftRightMarginRaw / GuiWidget.DeviceScale);

			this.Padding = new BorderDouble(leftRightMargin, 0);

			return newColumnCount;
		}

		public ListViewItemBase AddItem(ListViewItem item)
		{
			var iconView = new IconViewItem(item, this.ThumbWidth, this.ThumbHeight, theme);
			iconView.Margin = new BorderDouble(leftRightMargin, 0);

			allIconViews.Add(iconView);

			AddColumnAndChild(iconView);

			return iconView;
		}

		private void AddColumnAndChild(IconViewItem iconView)
		{
			if (rowButtonContainer == null)
			{
				rowButtonContainer = new FlowLayoutWidget(FlowDirection.LeftToRight)
				{
					HAnchor = HAnchor.Stretch,
					Padding = 0
				};
				this.AddChild(rowButtonContainer);
			}

			rowButtonContainer.AddChild(iconView);

			if (cellIndex++ >= columnCount - 1)
			{
				rowButtonContainer = null;
				cellIndex = 0;
			}
		}

		public void ClearItems()
		{
			cellIndex = 0;
			rowButtonContainer = null;
			allIconViews.Clear();

			this.CloseAllChildren();
		}

		public void BeginReload()
		{
			columnCount = RecomputeFlowValues();
		}

		public void EndReload()
		{
		}
	}

	public class IconViewItem : ListViewItemBase
	{
		private static ImageBuffer loadingImage = AggContext.StaticData.LoadIcon("IC_32x32.png");

		internal static int ItemPadding = 0;

		private TextWidget text;

		public IconViewItem(ListViewItem item, int thumbWidth, int thumbHeight, ThemeConfig theme)
			: base(item, thumbWidth, thumbHeight, theme)
		{
			this.VAnchor = VAnchor.Fit;
			this.HAnchor = HAnchor.Fit;
			this.Padding = IconViewItem.ItemPadding;
			this.Margin = new BorderDouble(6, 0, 0, 6);
			this.Border = 2;

			int scaledWidth = (int)(thumbWidth * GuiWidget.DeviceScale);
			int scaledHeight = (int)(thumbHeight * GuiWidget.DeviceScale);

			int maxWidth = scaledWidth - 4;

			if (thumbWidth < 75)
			{
				imageWidget = new ImageWidget(scaledWidth, scaledHeight)
				{
					AutoResize = false,
					Name = "List Item Thumbnail",
					BackgroundColor = theme.ThumbnailBackground,
					Margin = 0,
				};
				this.AddChild(imageWidget);
			}
			else
			{
				var container = new FlowLayoutWidget(FlowDirection.TopToBottom);
				this.AddChild(container);

				imageWidget = new ImageWidget(scaledWidth, scaledHeight)
				{
					AutoResize = false,
					Name = "List Item Thumbnail",
					BackgroundColor = theme.ThumbnailBackground,
					Margin = 0,
				};
				container.AddChild(imageWidget);

				text = new TextWidget(item.Model.Name, 0, 0, 9, textColor: ActiveTheme.Instance.PrimaryTextColor)
				{
					AutoExpandBoundsToText = false,
					EllipsisIfClipped = true,
					HAnchor = HAnchor.Center,
					Margin = new BorderDouble(0, 0, 0, 3),
				};

				text.MaximumSize = new Vector2(maxWidth, 20);
				if (text.Printer.LocalBounds.Width > maxWidth)
				{
					text.Width = maxWidth;
					text.Text = item.Model.Name;
				}

				container.AddChild(text);
			}

			this.SetItemThumbnail(loadingImage);
		}

		public override string ToolTipText
		{
			get => text?.ToolTipText;
			set
			{
				if (text != null)
				{
					text.ToolTipText = value;
				}
			}
		}

		public override async void OnLoad(EventArgs args)
		{
			base.OnLoad(args);
			await this.LoadItemThumbnail();
		}

		public override Color BackgroundColor
		{
			get => this.IsSelected ? theme.AccentMimimalOverlay : Color.Transparent;
			set { }
		}
	}
}