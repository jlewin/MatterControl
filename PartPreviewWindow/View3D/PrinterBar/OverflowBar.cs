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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MatterHackers.Agg;
using MatterHackers.Agg.Platform;
using MatterHackers.Agg.UI;
using MatterHackers.MatterControl.CustomWidgets;
using MatterHackers.MatterControl.PrintLibrary;

namespace MatterHackers.MatterControl.PartPreviewWindow
{
	public class OverflowBar : Toolbar
	{
		private static HashSet<Type> ignoredTypes = new HashSet<Type> { typeof(HorizontalLine), typeof(SearchInputBox) };
		private static HashSet<Type> ignoredInMenuTypes = new HashSet<Type> { typeof(VerticalLine), typeof(HorizontalLine), typeof(SearchInputBox), typeof(HorizontalSpacer) };

		public OverflowBar(ThemeConfig theme)
		{
			this.Padding = theme.ToolbarPadding.Clone(left: 0);
			this.theme = theme;

			this.OverflowButton = new OverflowMenuButton(this, theme)
			{
				AlignToRightEdge = true,
			};

			this.ActionArea.Margin = new BorderDouble(right: this.OverflowButton.Width);
			this.SetRightAnchorItem(this.OverflowButton);
		}

		private ThemeConfig theme;

		public OverflowMenuButton OverflowButton { get; }

		public Action<PopupMenu> ExtendOverflowMenu { get; set; }

		protected virtual void OnExtendPopupMenu(PopupMenu popupMenu)
		{
			this.ExtendOverflowMenu(popupMenu);
		}

		public override void OnBoundsChanged(EventArgs e)
		{
			if (this.RightAnchorItem == null)
			{
				return;
			}

			double maxRight = this.Width - RightAnchorItem.Width;

			double accumulatedX = 0;

			bool withinLimits = true;

			foreach (var widget in this.ActionArea.Children.Where(c => !ignoredTypes.Contains(c.GetType())))
			{
				var totalX = widget.Width + widget.Margin.Width;

				withinLimits &= (accumulatedX + totalX) < maxRight;

				// Widget is visible when no previous sibling has been rejected and its right edge is less than maxRight
				widget.Visible = withinLimits; // widget.Position.X + widget.Width < maxRight;

				// Ignore streched widgets
				if (widget.HAnchor != HAnchor.Stretch)
				{
					accumulatedX += totalX;
				}
			}

			base.OnBoundsChanged(e);
		}

		/// <summary>
		/// A PopupMenuButton with the standard overflow icon
		/// </summary>
		public class OverflowMenuButton : PopupMenuButton
		{
			public OverflowMenuButton(OverflowBar overflowBar, ThemeConfig theme)
				: base(CreateOverflowIcon(theme), theme)
			{
				this.DynamicPopupContent = () =>
				{
					var popupMenu = new PopupMenu(ApplicationController.Instance.MenuTheme);

					// Perform overflow
					bool hasOverflowItems = false;
					foreach (var widget in overflowBar.ActionArea.Children.Where(c => !c.Visible && !ignoredInMenuTypes.Contains(c.GetType())))
					{
						hasOverflowItems = true;

						PopupMenu.MenuItem menuItem;

						var iconButton = widget as IconButton;

						menuItem = popupMenu.CreateMenuItem(
							widget.ToolTipText ?? widget.Text,
							iconButton?.IconImage);

						menuItem.Enabled = widget.Enabled;

						menuItem.Click += (s, e) =>
						{
							widget.OnClick(e);
						};
					}

					if (hasOverflowItems)
					{
						popupMenu.CreateHorizontalLine();
					}

					// Extend menu with non-overflow/standard items
					overflowBar.OnExtendPopupMenu(popupMenu);

					return popupMenu;
				};
			}


			private static ImageWidget CreateOverflowIcon(ThemeConfig theme)
			{
				return new ImageWidget(AggContext.StaticData.LoadIcon(Path.Combine("ViewTransformControls", "overflow.png"), 32, 32, theme.InvertRequired))
				{
					HAnchor = HAnchor.Right,
					VAnchor = VAnchor.Center
				};
			}
		}
	}
}