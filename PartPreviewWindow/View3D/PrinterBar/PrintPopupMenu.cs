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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MatterHackers.Agg;
using MatterHackers.Agg.UI;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.CustomWidgets;
using MatterHackers.MatterControl.SlicerConfiguration;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl.PartPreviewWindow
{
	public class PrintPopupMenu : PopupMenuButton
	{
		private PrinterConfig printer;
		private EventHandler unregisterEvents;
		private Dictionary<string, UIField> allUiFields = new Dictionary<string, UIField>();
		private SettingsContext settingsContext;

		public PrintPopupMenu(PrinterConfig printer, ThemeConfig theme)
		{
			this.printer = printer;
			this.DrawArrow = true;
			this.BackgroundColor = theme.ToolbarButtonBackground;
			this.HoverColor = theme.ToolbarButtonHover;
			this.MouseDownColor = theme.ToolbarButtonDown;
			this.Name = "PrintPopupMenu";
			this.HAnchor = HAnchor.Fit;
			this.VAnchor = VAnchor.Fit;

			settingsContext = new SettingsContext(printer, null, NamedSettingsLayers.All);

			this.DynamicPopupContent = () =>
			{
				var menuTheme = ApplicationController.Instance.MenuTheme;

				int tabIndex = 0;

				allUiFields.Clear();

				var column = new FlowLayoutWidget(FlowDirection.TopToBottom)
				{
					Padding = 10,
					BackgroundColor = menuTheme.Colors.PrimaryBackgroundColor
				};

				column.AddChild(new TextWidget("Options".Localize(), textColor: menuTheme.Colors.PrimaryTextColor)
				{
					HAnchor = HAnchor.Left
				});

				var optionsPanel = new IgnoredFlowLayout()
				{
					Name = "PrintPopupMenu Panel",
					HAnchor = HAnchor.Fit | HAnchor.Left,
					VAnchor = VAnchor.Fit,
					Padding = 5,
					MinimumSize = new Vector2(400, 65),
					Margin = new BorderDouble(top: 10),
				};
				column.AddChild(optionsPanel);

				foreach (var key in new[] { "layer_height", "fill_density", "support_material", "create_raft"})
				{
					var settingsData = SettingsOrganizer.Instance.GetSettingsData(key);
					var row = SliceSettingsTabView.CreateItemRow(settingsData, settingsContext, printer, menuTheme, ref tabIndex, allUiFields);

					SliceSettingsRow.AddBordersToEditFields(row);

					optionsPanel.AddChild(row);
				}

				var subPanel = new FlowLayoutWidget(FlowDirection.TopToBottom);

				// TODO: lookup settings values
				bool anySettingOverridden = false;

				var sectionWidget = new SectionWidget("Advanced", subPanel, menuTheme, expanded: true)
				{
					Name = "Advanced Section",
					HAnchor = HAnchor.Stretch,
					VAnchor = VAnchor.Fit,
					Margin = 0
				};
				column.AddChild(sectionWidget);

				sectionWidget.Load += (s, e) =>
				{
					sectionWidget.Checkbox.Checked = anySettingOverridden;
				};

				foreach (var key in new[] { "spiral_vase", "layer_to_pause" })
				{
					var settingsData = SettingsOrganizer.Instance.GetSettingsData(key);
					var row = SliceSettingsTabView.CreateItemRow(settingsData, settingsContext, printer, menuTheme, ref tabIndex, allUiFields);

					SliceSettingsRow.AddBordersToEditFields(row);

					subPanel.AddChild(row);
				}

				sectionWidget.ApplyBoxStyle();
				sectionWidget.Margin = new BorderDouble(0, 10);
				sectionWidget.ContentPanel.Children<SettingsRow>().First().Border = new BorderDouble(0, 1);
				sectionWidget.ContentPanel.Children<SettingsRow>().Last().Border = 0;

				var button = new TextButton("Start Print".Localize(), menuTheme)
				{
					Name = "Start Print Button",
					HAnchor = HAnchor.Right,
					VAnchor = VAnchor.Absolute,
					BackgroundColor = theme.MinimalShade
				};
				button.Click += (s, e) =>
				{
					UiThread.RunOnIdle(async () =>
					{
						// Save any pending changes before starting print operation
						await ApplicationController.Instance.Tasks.Execute("Saving Changes".Localize(), printer.Bed.SaveChanges);

						await ApplicationController.Instance.PrintPart(
							printer.Bed.EditContext,
							printer,
							null,
							CancellationToken.None);
					});
				};
				column.AddChild(button);

				return column;
			};

			this.AddChild(new TextButton("Print".Localize(), theme)
			{
				Selectable = false,
				Padding = theme.ButtonFactory.Options.Margin.Clone(right: 5)
			});

			ActiveSliceSettings.SettingChanged.RegisterEvent((s, e) =>
			{
				if (e is StringEventArgs stringEvent)
				{
					string settingsKey = stringEvent.Data;
					if (allUiFields.TryGetValue(settingsKey, out UIField uifield))
					{
						string currentValue = settingsContext.GetValue(settingsKey);
						if (uifield.Value != currentValue
							|| settingsKey == "com_port")
						{
							uifield.SetValue(
								currentValue,
								userInitiated: false);
						}
					}
				}
			},
			ref unregisterEvents);
		}

		public override void OnClosed(ClosedEventArgs e)
		{
			unregisterEvents?.Invoke(this, null);
			base.OnClosed(e);
		}

		private class IgnoredFlowLayout : FlowLayoutWidget, IIgnoredPopupChild
		{
			public IgnoredFlowLayout()
				: base(FlowDirection.TopToBottom)
			{
			}
		}
	}
}