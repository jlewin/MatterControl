﻿/*
Copyright (c) 2019, Lars Brubaker, John Lewin
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
using MatterHackers.Agg;
using MatterHackers.Agg.UI;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.ConfigurationPage.PrintLeveling;
using MatterHackers.MatterControl.CustomWidgets;
using MatterHackers.MatterControl.PartPreviewWindow;
using MatterHackers.MatterControl.PrinterControls;
using MatterHackers.MatterControl.SlicerConfiguration;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl
{
	public class PrinterCalibrationWizard : IStagedSetupWizard
	{
		public PrinterCalibrationWizard(PrinterConfig printer, ThemeConfig theme)
		{
			this.Stages = new ISetupWizard[]
			{
				new ProbeCalibrationWizard(printer),
				new PrintLevelingWizard(printer),
				new XyCalibrationWizard(printer, 1)
			};

			this.HomePageGenerator = () =>
			{
				var homePage = new WizardSummaryPage()
				{
					HeaderText = "Printer Setup & Calibration".Localize()
				};

				var contentRow = homePage.ContentRow;

				contentRow.AddChild(
					new WrappedTextWidget(
						@"Select the calibration task on the left to continue".Replace("\r\n", "\n"),
						pointSize: theme.DefaultFontSize,
						textColor: theme.TextColor));
				contentRow.BackgroundColor = Color.Transparent;

				foreach (var stage in this.Stages.Where(s => s.Enabled && s.Visible))
				{
					var widget = new GuiWidget();

					if (stage is ProbeCalibrationWizard probeWizard)
					{
						var column = CreateColumn(theme);
						column.FlowDirection = FlowDirection.LeftToRight;

						var offset = printer.Settings.GetValue<Vector3>(SettingsKey.probe_offset);

						column.AddChild(
							new ValueTag(
								"Z Offset".Localize(),
								offset.Z.ToString("0.###"),
								new BorderDouble(12, 5, 2, 5),
								5,
								11));

						widget = column;
					}

					if (stage is PrintLevelingWizard levelingWizard)
					{
						PrintLevelingData levelingData = printer.Settings.Helpers.PrintLevelingData;

						if (levelingData != null
							&& printer.Settings?.GetValue<bool>(SettingsKey.print_leveling_enabled) == true)
						{
							//levelingWizard.LevelingPlan
							//
							//if (currentLevelingFunctions == null
							//	|| currentProbeZOffset != printer.Settings.GetValue<Vector3>(SettingsKey.probe_offset)
							//	|| !levelingData.SamplesAreSame(currentLevelingFunctions.SampledPositions))
							//{
							//	currentProbeZOffset = printer.Settings.GetValue<Vector3>(SettingsKey.probe_offset);
							//	currentLevelingFunctions = new LevelingFunctions(printer, levelingData);
							//}

							var positions = levelingData.SampledPositions;

							var levelingSolution = printer.Settings.GetValue(SettingsKey.print_leveling_solution);

							var column = CreateColumn(theme);
							column.AddChild(new TextWidget(levelingSolution, pointSize: theme.DefaultFontSize, textColor: theme.TextColor)
							{
								Margin = new BorderDouble(bottom: 4)
							});

							var probeWidget = new ProbePositionsWidget(printer, positions.Select(v => new Vector2(v)).ToList(), positions.Select(v => new ProbePosition() { position = v }).ToList(), theme)
							{
								HAnchor = HAnchor.Absolute,
								VAnchor = VAnchor.Absolute,
								Height = 200,
								Width = 200,
								RenderLevelingData = true,
								RenderProbePath = false,
								SimplePoints = true,
							};
							column.AddChild(probeWidget);

							var row = new FlowLayoutWidget();

							row.AddChild(column);

							widget = row;
						}
					}

					if (stage.SetupRequired)
					{
						var column = CreateColumn(theme);
						column.AddChild(new TextWidget("Setup Required".Localize(), pointSize: theme.DefaultFontSize, textColor: theme.TextColor));

						widget = column;
					}

					var section = new SectionWidget(stage.Title, widget, theme, expandingContent: false);
					theme.ApplyBoxStyle(section);

					section.Margin = section.Margin.Clone(left: 0);

					if (stage.SetupRequired)
					{
						section.BackgroundColor = Color.Red.WithAlpha(30);
					}

					contentRow.AddChild(section);
				}

				return homePage;
			};
		}

		private static FlowLayoutWidget CreateColumn(ThemeConfig theme)
		{
			return new FlowLayoutWidget(FlowDirection.TopToBottom)
			{
				Margin = new BorderDouble(15, theme.DefaultContainerPadding, theme.DefaultContainerPadding, 4)
			};
		}

		public string Title { get; } = "Printer Calibration".Localize();

		public Vector2 WindowSize { get; } = new Vector2(1200, 700);

		public IEnumerable<ISetupWizard> Stages { get; }

		public Func<DialogPage> HomePageGenerator { get; }

		public static bool SetupRequired(PrinterConfig printer)
		{
			return LevelingValidation.NeedsToBeRun(printer) // PrintLevelingWizard
				|| ProbeCalibrationWizard.NeedsToBeRun(printer)
				|| FilamentSetupWizard.SetupRequired(printer);
		}
	}
}