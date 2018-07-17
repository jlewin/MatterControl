﻿/*
Copyright (c) 2017, Lars Brubaker, John Lewin
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
using MatterHackers.Agg.VertexSource;
using MatterHackers.MatterControl.CustomWidgets;

namespace MatterHackers.MatterControl.PartPreviewWindow
{
	public class PopupMenuButton : PopupButton
	{
		private ThemeConfig theme;

		public PopupMenuButton(ThemeConfig theme)
		{
			this.theme = theme;
			this.DisabledColor = theme.DisabledColor;
			this.HoverColor = theme.MinimalShade;
		}

		public PopupMenuButton(GuiWidget viewWidget, ThemeConfig theme)
			: base(viewWidget)
		{
			viewWidget.Selectable = false;
			viewWidget.BackgroundColor = Color.Transparent;

			this.theme = theme;
			this.DisabledColor = theme.DisabledColor;

			this.HoverColor = theme.ToolbarButtonHover;
			this.BackgroundColor = theme.ToolbarButtonBackground;
			this.MouseDownColor = theme.ToolbarButtonDown;
		}

		public PopupMenuButton(string text, ThemeConfig theme)
			: this (new TextButton(text, theme)
			{
				Selectable = false,
				Padding = theme.TextButtonPadding.Clone(right: 5)
			}, theme)
		{
			this.DrawArrow = true;
			this.HoverColor = theme.ToolbarButtonHover;
			this.BackgroundColor = theme.ToolbarButtonBackground;
			this.MouseDownColor = theme.ToolbarButtonDown;
		}

		public Color DisabledColor { get; set; }

		public Color MouseDownColor { get; set;} = Color.Transparent;

		private bool _drawArrow = false;
		public bool DrawArrow
		{
			get => _drawArrow;
			set
			{
				if (_drawArrow != value)
				{
					_drawArrow = value;

					if (_drawArrow)
					{
						this.Padding = new BorderDouble(this.Padding.Left, this.Padding.Bottom, 20, this.Padding.Top);
					}
				}
			}
		}

		private VertexStorage dropArrow = DropArrow.DownArrow;

		public override void OnDraw(Graphics2D graphics2D)
		{
			base.OnDraw(graphics2D);

			if (this.DrawArrow)
			{
				// Draw directional arrow
				graphics2D.Render(
					dropArrow,
					LocalBounds.Right - DropArrow.ArrowHeight * 2 - 2,
					LocalBounds.Center.Y + DropArrow.ArrowHeight / 2,
					this.Enabled ? theme.BorderColor : this.DisabledColor);
			}
		}

		private bool mouseInBounds;
		public override void OnMouseEnterBounds(MouseEventArgs mouseEvent)
		{
			mouseInBounds = true;
			base.OnMouseEnterBounds(mouseEvent);
			this.Invalidate();
		}

		public override void OnMouseLeaveBounds(MouseEventArgs mouseEvent)
		{
			mouseInBounds = false;
			base.OnMouseLeaveBounds(mouseEvent);
			this.Invalidate();
		}

		public override Color BackgroundColor
		{
			get
			{
				if (this.MouseCaptured
					&& mouseInBounds
					&& this.Enabled)
				{
					return this.MouseDownColor;
				}
				else if (this.mouseInBounds
					&& this.Enabled)
				{
					return this.HoverColor;
				}
				else
				{
					return base.BackgroundColor;
				}
			}
			set => base.BackgroundColor = value;
		}

		protected override void OnBeforePopup()
		{
			// Force off-white if content has transparent background
			if (this.PopupContent.BackgroundColor == Color.Transparent)
			{
				this.PopupContent.BackgroundColor = new Color("#f6f6f6");
			}

			base.OnBeforePopup();
		}
	}
}