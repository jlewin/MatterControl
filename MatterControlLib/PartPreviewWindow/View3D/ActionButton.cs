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
using MatterHackers.Agg.UI;

namespace MatterHackers.MatterControl.PartPreviewWindow
{
	public class ActionButton : GuiWidget
	{
		private GuiWidget button;
		private NamedAction namedAction;
		private ThemeConfig theme;
		private bool isActive;

		public ActionButton(NamedAction action, ThemeConfig theme)
		{
			this.namedAction = action;
			this.VAnchor = VAnchor.Fit;
			this.HAnchor = HAnchor.Fit;
			this.theme = theme;

			if (namedAction.Icon != null)
			{
				button = new ThemedIconButton(namedAction.Icon, theme)
				{
					Name = namedAction.Title + " Button",
					ToolTipText = namedAction.Title,
					Margin = theme.ButtonSpacing,
					BackgroundColor = theme.ToolbarButtonBackground,
					HoverColor = theme.ToolbarButtonHover,
					MouseDownColor = theme.ToolbarButtonDown,
				};
			}
			else
			{
				button = new ThemedTextButton(namedAction.Title, theme)
				{
					Name = namedAction.Title + " Button",
					Margin = theme.ButtonSpacing,
					BackgroundColor = theme.ToolbarButtonBackground,
					HoverColor = theme.ToolbarButtonHover,
					MouseDownColor = theme.ToolbarButtonDown,
				};
			}

			button.Click += ChildButton_Click;

			this.AddChild(button);
			UpdateToggleState();
		}

		private void ChildButton_Click(object sender, MouseEventArgs e)
		{
			UiThread.RunOnIdle(() =>
			{
				namedAction.Action?.Invoke();

				UpdateToggleState();
			});
		}

		private void UpdateToggleState()
		{
			if (namedAction is NamedToggleAction toggle)
			{
				isActive = toggle.IsActive();
				this.BackgroundColor = isActive ? theme.ToolbarButtonDown : theme.ButtonBackgroundColor;
			}
		}

		public override void OnClosed(EventArgs e)
		{
			button.Click -= ChildButton_Click;
			base.OnClosed(e);
		}

		public override void OnDraw(Graphics2D graphics2D)
		{
			if (isActive)
			{
				graphics2D.Rectangle(0, 0, LocalBounds.Right, 2 * DeviceScale, theme.PrimaryAccentColor);
			}

			base.OnDraw(graphics2D);
		}
	}
}
