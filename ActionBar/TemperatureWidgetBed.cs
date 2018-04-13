﻿/*
Copyright (c) 2017, Kevin Pope, John Lewin
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
using MatterHackers.Agg.ImageProcessing;
using MatterHackers.Agg.Platform;
using MatterHackers.Agg.UI;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.ConfigurationPage;
using MatterHackers.MatterControl.CustomWidgets;
using MatterHackers.MatterControl.PrinterCommunication;
using MatterHackers.MatterControl.SlicerConfiguration;

namespace MatterHackers.MatterControl.ActionBar
{
	internal class TemperatureWidgetBed : TemperatureWidgetBase
	{
		private string sliceSettingsNote = "Note: Slice Settings are applied before the print actually starts. Changes while printing will not effect the active print.".Localize();
		private string waitingForBedToHeatMessage = "The bed is currently heating and its target temperature cannot be changed until it reaches {0}°C.\n\nYou can set the starting bed temperature in SETTINGS -> Filament -> Temperatures.\n\n{1}".Localize();
		private string waitingForBedToHeatTitle = "Waiting For Bed To Heat".Localize();

		public TemperatureWidgetBed(PrinterConfig printer, ThemeConfig theme)
			: base(printer, "150.3°", theme)
		{
			this.Name = "Bed TemperatureWidget";
			this.DisplayCurrentTemperature();
			this.ToolTipText = "Current bed temperature".Localize();

			this.ImageWidget.Image = AggContext.StaticData.LoadIcon("bed.png", theme.InvertIcons);

			this.PopupContent = this.GetPopupContent(ApplicationController.Instance.MenuTheme);

			printer.Connection.BedTemperatureRead.RegisterEvent((s, e) => DisplayCurrentTemperature(), ref unregisterEvents);
		}

		protected override int ActualTemperature => (int)printer.Connection.ActualBedTemperature;
		protected override int TargetTemperature => (int)printer.Connection.TargetBedTemperature;

		private GuiWidget GetPopupContent(ThemeConfig theme)
		{
			var widget = new IgnoredPopupWidget()
			{
				Width = 300,
				HAnchor = HAnchor.Absolute,
				VAnchor = VAnchor.Fit,
				BackgroundColor = theme.Colors.PrimaryBackgroundColor,
				Padding = new BorderDouble(12, 0)
			};

			var container = new FlowLayoutWidget(FlowDirection.TopToBottom)
			{
				HAnchor = HAnchor.Stretch,
				VAnchor = VAnchor.Fit | VAnchor.Top,
			};
			widget.AddChild(container);

			GuiWidget hotendRow;

			container.AddChild(hotendRow = new SettingsItem(
				"Heated Bed".Localize(),
				theme,
				new SettingsItem.ToggleSwitchConfig()
				{
					Checked = false,
					ToggleAction = (itemChecked) =>
					{
						var goalTemp = itemChecked ? printer.Settings.GetValue<double>(SettingsKey.bed_temperature) : 0;

						if (itemChecked)
						{
							SetTargetTemperature(goalTemp);
						}
						else
						{
							SetTargetTemperature(0);
						}
					}
				},
				enforceGutter: false));

			var toggleWidget = hotendRow.Children.Where(o => o is ICheckbox).FirstOrDefault();
			toggleWidget.Name = "Toggle Heater";

			heatToggle = toggleWidget as ICheckbox;

			int tabIndex = 0;
			var settingsContext = new SettingsContext(printer, null, NamedSettingsLayers.All);

			var settingsData = SettingsOrganizer.Instance.GetSettingsData(SettingsKey.bed_temperature);
			var temperatureRow = SliceSettingsTabView.CreateItemRow(settingsData, settingsContext, printer, theme, ref tabIndex);
			SliceSettingsRow.AddBordersToEditFields(temperatureRow);
			container.AddChild(temperatureRow);

			alwaysEnabled.Add(hotendRow);

			// add in the temp graph
			var graph = new DataViewGraph()
			{
				DynamiclyScaleRange = false,
				MinValue = 0,
				ShowGoal = true,
				GoalColor = ActiveTheme.Instance.PrimaryAccentColor,
				GoalValue = printer.Settings.GetValue<double>(SettingsKey.bed_temperature),
				MaxValue = 150, // could come from some profile value in the future
				Width = widget.Width - 20,
				Height = 35, // this works better if it is a common multiple of the Width
				Margin = new BorderDouble(0, 5, 0, 0),
			};

			var runningInterval = UiThread.SetInterval(() =>
			{
				graph.AddData(this.ActualTemperature);
			}, 1);
			this.Closed += (s, e) => runningInterval.Continue = false;

			var valueField = temperatureRow.Descendants<MHNumberEdit>().FirstOrDefault();
			var settingsRow = temperatureRow.DescendantsAndSelf<SliceSettingsRow>().FirstOrDefault();
			ActiveSliceSettings.SettingChanged.RegisterEvent((s, e) =>
			{
				if (e is StringEventArgs stringEvent)
				{
					var temp = printer.Settings.GetValue<double>(SettingsKey.bed_temperature);
					valueField.Value = temp;
					graph.GoalValue = temp;
					settingsRow.UpdateStyle();
					if (stringEvent.Data == SettingsKey.bed_temperature
						&& heatToggle.Checked)
					{
						SetTargetTemperature(temp);
					}
				};
			}, ref unregisterEvents);

			container.AddChild(graph);

			return widget;
		}

		public override void OnDraw(Graphics2D graphics2D)
		{
			if (heatToggle != null)
			{
				heatToggle.Checked = printer.Connection.TargetBedTemperature != 0;
			}

			base.OnDraw(graphics2D);
		}

		protected override void SetTargetTemperature(double targetTemp)
		{
			double goalTemp = (int)(targetTemp + .5);
			if (printer.Connection.PrinterIsPrinting
				&& printer.Connection.DetailedPrintingState == DetailedPrintingState.HeatingBed
				&& goalTemp != printer.Connection.TargetBedTemperature)
			{
				string message = string.Format(waitingForBedToHeatMessage, printer.Connection.TargetBedTemperature, sliceSettingsNote);
				StyledMessageBox.ShowMessageBox(message, waitingForBedToHeatTitle);
			}
			else
			{
				printer.Connection.TargetBedTemperature = (int)(targetTemp + .5);
			}
		}
	}
}