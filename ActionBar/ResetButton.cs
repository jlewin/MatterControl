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
using MatterHackers.Agg;
using MatterHackers.Agg.Platform;
using MatterHackers.Agg.UI;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.SlicerConfiguration;

namespace MatterHackers.MatterControl.ActionBar
{
	/*
	public class ResetButton : GuiWidget
	{
		private readonly string resetConnectionText = "Reset\nConnection".Localize();
		private EventHandler unregisterEvents;

		public ResetButton(PrinterConfig printer, ThemeConfig theme)
		{
			this.HAnchor = HAnchor.Stretch | HAnchor.Fit;
			this.VAnchor = VAnchor.Fit;
			this.BackgroundColor = ActiveTheme.Instance.PrimaryBackgroundColor;

			Button resetConnectionButton = buttonFactory.Generate(resetConnectionText, AggContext.StaticData.LoadIcon("e_stop4.png", theme.InvertRequired));
			resetConnectionButton.Visible = printer.Settings.GetValue<bool>(SettingsKey.show_reset_connection);
			resetConnectionButton.Click += (s, e) =>
			{
				UiThread.RunOnIdle(printer.Connection.RebootBoard);
			};
			this.AddChild(resetConnectionButton);

			ActiveSliceSettings.SettingChanged.RegisterEvent((s, e) =>
			{
				var stringEvent = e as StringEventArgs;
				if (stringEvent?.Data == SettingsKey.show_reset_connection)
				{
					resetConnectionButton.Visible = printer.Settings.GetValue<bool>(SettingsKey.show_reset_connection);
				}
			}, ref unregisterEvents);
		}
	} */
}