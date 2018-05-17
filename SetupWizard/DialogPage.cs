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

using System;
using System.Linq;
using MatterHackers.Agg;
using MatterHackers.Agg.UI;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.CustomWidgets;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl
{
	public class DialogPage : FlowLayoutWidget
	{
		protected FlowLayoutWidget headerRow;
		protected FlowLayoutWidget contentRow;
		private FlowLayoutWidget footerRow;

		private WrappedTextWidget headerLabel;
		private GuiWidget cancelButton;

		public Vector2 WindowSize { get; set; }

		protected LinkButtonFactory linkButtonFactory { get; }

		protected double labelFontSize = 12 * GuiWidget.DeviceScale;
		protected double errorFontSize = 10 * GuiWidget.DeviceScale;

		protected ThemeConfig theme;

		public DialogPage(string cancelButtonText = null)
			: base (FlowDirection.TopToBottom)
		{
			theme = ApplicationController.Instance.Theme;

			this.HAnchor = HAnchor.Stretch;
			this.VAnchor = VAnchor.Stretch;
			this.BackgroundColor = theme.Colors.PrimaryBackgroundColor;
			linkButtonFactory = theme.LinkButtonFactory;

			if (cancelButtonText == null)
			{
				cancelButtonText = "Cancel".Localize();
			}

			cancelButton = theme.CreateDialogButton(cancelButtonText);
			cancelButton.Margin = 0;
			cancelButton.Name = "Cancel Wizard Button";

			// Create the header row for the widget
			headerRow = new FlowLayoutWidget(FlowDirection.LeftToRight)
			{
				Name = "HeaderRow",
				Margin = new BorderDouble(0, 3, 0, 0),
				Padding = new BorderDouble(0, 12),
				HAnchor = HAnchor.Stretch,
				VAnchor = VAnchor.Fit
			};
			this.AddChild(headerRow);

			headerLabel = new WrappedTextWidget("Setup Wizard".Localize(), pointSize: 24, textColor: theme.Colors.PrimaryAccentColor)
			{
				HAnchor = HAnchor.Stretch
			};
			headerRow.AddChild(headerLabel);

			// Create the main control container
			contentRow = new FlowLayoutWidget(FlowDirection.TopToBottom)
			{
				Padding = new BorderDouble(10),
				BackgroundColor = theme.MinimalShade,
				HAnchor = HAnchor.Stretch,
				VAnchor = VAnchor.Stretch
			};
			this.AddChild(contentRow);

			// Create the footer (button) container
			footerRow = new FlowLayoutWidget(FlowDirection.LeftToRight)
			{
				HAnchor = HAnchor.Stretch,
				VAnchor = VAnchor.Fit,
				Margin = new BorderDouble(0, 6),
				Padding = new BorderDouble(top: 4, bottom: 2)
			};
			this.AddChild(footerRow);

#if !__ANDROID__
			headerRow.Padding = new BorderDouble(0, 3, 0, 3);

			headerLabel.TextWidget.PointSize = 14;
			headerLabel.TextColor = theme.Colors.PrimaryTextColor;
			contentRow.Padding = new BorderDouble(5);

			footerRow.Padding = 0;
			footerRow.Margin = new BorderDouble(top: theme.DefaultContainerPadding);
#endif
		}

		public DialogWindow WizardWindow { get; set; }

		public string WindowTitle { get; set; }

		public bool AlwaysOnTopOfMain { get; set; } = true;

		public string HeaderText
		{
			get => headerLabel.Text;
			set => headerLabel.Text = value;
		}

		public void AddPageAction(GuiWidget button)
		{
			button.Margin = theme.ButtonSpacing;
			footerRow.AddChild(button);
		}

		protected void SetCancelButtonName(string newName)
		{
			cancelButton.Name = newName;
		}

		protected void SetCancelButtonText(string text)
		{
			cancelButton.Text = text;
		}

		protected void HideCancelButton()
		{
			cancelButton.Visible = false;
		}

		public override void OnLoad(EventArgs args)
		{
			// Add 'Close' event listener after derived types have had a chance to register event listeners
			cancelButton.Click += (s, e) =>
			{
				this.OnCancel(out bool abortCancel);

				if (!abortCancel)
				{
					UiThread.RunOnIdle(() => WizardWindow?.Close());
				}
			};

			var systemWindow = this.Parents<SystemWindow>().FirstOrDefault();
			if(systemWindow != null)
			{
				EventHandler<KeyEventArgs> checkEscape = null;
				checkEscape = (s, e) =>
				{
					if(e.KeyCode == Keys.Escape)
					{
						systemWindow.KeyDown -= checkEscape;

						this.OnCancel(out bool abortCancel);

						if (!abortCancel)
						{
							UiThread.RunOnIdle(() => WizardWindow?.Close());
						}
					}
				};

				systemWindow.KeyDown += checkEscape;
			}

			footerRow.AddChild(new HorizontalSpacer());
			footerRow.AddChild(cancelButton);

			base.OnLoad(args);
		}

		protected virtual void OnCancel(out bool abortCancel)
		{
			abortCancel = false;
		}

		public virtual void PageIsBecomingActive()
		{
		}

		public virtual void PageIsBecomingInactive()
		{
		}
	}
}