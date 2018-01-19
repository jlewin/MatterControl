﻿/*
Copyright (c) 2017, John Lewin
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
using MatterHackers.Localizations;
using MatterHackers.MatterControl.CustomWidgets;

namespace MatterHackers.MatterControl
{
	public class PluginsPage : DialogPage
	{
		public PluginsPage()
		{
			this.AnchorAll();

			var mainContainer = new FlowLayoutWidget(FlowDirection.TopToBottom);
			mainContainer.AnchorAll();

			this.WindowTitle = "MatterControl Plugins".Localize();

			var contentScroll = new ScrollableWidget(true);
			contentScroll.ScrollArea.HAnchor |= HAnchor.Stretch;
			contentScroll.ScrollArea.VAnchor = VAnchor.Fit;
			contentScroll.AnchorAll();

			var formContainer = new FlowLayoutWidget(FlowDirection.TopToBottom)
			{
				HAnchor = HAnchor.Stretch
			};

			// TODO: Move to instance
			var plugins = ApplicationController.Plugins;

			foreach (var plugin in plugins.KnownPlugins)
			{
				var rowContainer = new FlowLayoutWidget(FlowDirection.TopToBottom)
				{
					HAnchor = HAnchor.Stretch
				};

				var checkbox = new CheckBox(plugin.Name, textColor: ActiveTheme.Instance.PrimaryTextColor, textSize: 12 * TextWidget.DeviceScale)
				{
					Margin = new BorderDouble(0, 2, 0, 16),
					HAnchor = Agg.UI.HAnchor.Stretch,
					TextColor = ActiveTheme.Instance.PrimaryTextColor,
					Checked = !plugins.Disabled.Contains(plugin.TypeName),
					Cursor = Cursors.Hand
				};

				// TODO: The closure here seems excessive. Consider a better long term approach possibly with a custom checkbox type
				checkbox.CheckedStateChanged += (s, e) =>
				{
					if (checkbox.Checked)
					{
						plugins.Enable(plugin.TypeName);
					}
					else
					{
						plugins.Disable(plugin.TypeName);
					}
				};

				rowContainer.AddChild(checkbox);
				formContainer.AddChild(rowContainer);
				formContainer.AddChild(new HorizontalSpacer());
			}

			contentScroll.AddChild(formContainer);

			contentRow.AddChild(contentScroll);

			var buttonBottomPanel = new FlowLayoutWidget(FlowDirection.LeftToRight)
			{
				HAnchor = HAnchor.Stretch,
				Padding = new BorderDouble(10, 3, 10, 3),
				BackgroundColor = ActiveTheme.Instance.PrimaryBackgroundColor
			};

			var buttonFactory = ApplicationController.Instance.Theme.ButtonFactory;

			Button saveButton = buttonFactory.Generate("Save".Localize());
			saveButton.Click += (s,e) => 
			{
				ApplicationController.Plugins.Save();
				this.WizardWindow.CloseOnIdle();
			};

			this.AddPageAction(saveButton);
		}
	}
}