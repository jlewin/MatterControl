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
using MatterHackers.Agg;
using MatterHackers.Agg.Font;
using MatterHackers.Agg.UI;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl
{
	public class MHTextEditWidget : GuiWidget
	{
		protected TextWidget noContentFieldDescription = null;
		private ThemeConfig theme;

		public TextEditWidget ActualTextEditWidget { get; }

		public MHTextEditWidget(string text, ThemeConfig theme, double pixelWidth = 0, double pixelHeight = 0, bool multiLine = false, int tabIndex = 0, string messageWhenEmptyAndNotSelected = "", TypeFace typeFace = null)
		{
			this.Padding = new BorderDouble(3);
			this.HAnchor = HAnchor.Fit;
			this.VAnchor = VAnchor.Fit;
			this.Border = 1;
			this.theme = theme;

			this.ActualTextEditWidget = new TextEditWidget(text, 0, 0, theme.DefaultFontSize, pixelWidth, pixelHeight, multiLine, tabIndex: tabIndex, typeFace: typeFace)
			{
				VAnchor = VAnchor.Bottom,
				BackgroundColor = Color.Transparent
			};

			var internalWidget = this.ActualTextEditWidget.InternalTextEditWidget;
			internalWidget.TextColor = theme.EditFieldColors.Inactive.TextColor;
			internalWidget.FocusChanged += (s, e) =>
			{
				internalWidget.TextColor = (internalWidget.Focused) ? theme.EditFieldColors.Focused.TextColor : theme.EditFieldColors.Inactive.TextColor;
				noContentFieldDescription.TextColor = (internalWidget.Focused) ? theme.EditFieldColors.Focused.LightTextColor : theme.EditFieldColors.Inactive.LightTextColor;
			};

			this.ActualTextEditWidget.InternalTextEditWidget.BackgroundColor = Color.Transparent;

			this.ActualTextEditWidget.MinimumSize = new Vector2(Math.Max(ActualTextEditWidget.MinimumSize.X, pixelWidth), Math.Max(ActualTextEditWidget.MinimumSize.Y, pixelHeight));
			this.AddChild(this.ActualTextEditWidget);

			this.AddChild(noContentFieldDescription = new TextWidget(messageWhenEmptyAndNotSelected, pointSize: theme.DefaultFontSize, textColor: theme.EditFieldColors.Focused.LightTextColor)
			{
				VAnchor = VAnchor.Top,
				AutoExpandBoundsToText = true
			});

			SetNoContentFieldDescriptionVisibility();
		}

		public override Color BackgroundColor
		{
			get
			{
				if (base.BackgroundColor != Color.Transparent)
				{
					return base.BackgroundColor;
				}
				else if (this.ContainsFocus)
				{
					return theme.EditFieldColors.Focused.BackgroundColor;
				}
				else if (this.mouseInBounds)
				{
					return theme.EditFieldColors.Hovered.BackgroundColor;
				}
				else
				{
					return theme.EditFieldColors.Inactive.BackgroundColor;
				}
			}
			set => base.BackgroundColor = value;
		}

		public override Color BorderColor
		{
			get
			{
				if (base.BorderColor != Color.Transparent)
				{
					return base.BackgroundColor;
				}
				else if (this.ContainsFocus)
				{
					return theme.EditFieldColors.Focused.BorderColor;
				}
				else if (this.mouseInBounds)
				{
					return theme.EditFieldColors.Hovered.BorderColor;
				}
				else
				{
					return theme.EditFieldColors.Inactive.BorderColor;
				}
			}
			set => base.BorderColor = value;
		}

		private bool mouseInBounds = false;

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

		public override HAnchor HAnchor
		{
			get => base.HAnchor;
			set
			{
				base.HAnchor = value;
				if (ActualTextEditWidget != null)
				{
					ActualTextEditWidget.HAnchor = value;
				}
			}
		}

		private void SetNoContentFieldDescriptionVisibility()
		{
			if (noContentFieldDescription != null)
			{
				noContentFieldDescription.Visible = (Text == "");
			}
		}

		public override void OnDraw(Graphics2D graphics2D)
		{
			SetNoContentFieldDescriptionVisibility();
			base.OnDraw(graphics2D);
		}

		public override string Text
		{
			get => ActualTextEditWidget.Text;
			set => ActualTextEditWidget.Text = value;
		}

		public bool SelectAllOnFocus
		{
			get => ActualTextEditWidget.InternalTextEditWidget.SelectAllOnFocus;
			set => ActualTextEditWidget.InternalTextEditWidget.SelectAllOnFocus = value;
		}
		public bool ReadOnly { get => ActualTextEditWidget.ReadOnly; set => ActualTextEditWidget.ReadOnly = value; }

		public void DrawFromHintedCache()
		{
			ActualTextEditWidget.Printer.DrawFromHintedCache = true;
			ActualTextEditWidget.DoubleBuffer = false;
		}

		public override void Focus()
		{
			ActualTextEditWidget.Focus();
		}
	}
}