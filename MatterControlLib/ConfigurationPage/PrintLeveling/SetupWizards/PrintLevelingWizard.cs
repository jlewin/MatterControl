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
using Markdig.Agg;
using MatterHackers.Agg;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.PrinterCommunication;
using MatterHackers.MatterControl.SlicerConfiguration;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl.ConfigurationPage.PrintLeveling
{
	public class PrintLevelingWizard : PrinterSetupWizard
	{
		private double babySteppingValue;
		private bool wizardExited;

		public PrintLevelingWizard(PrinterConfig printer)
			: base(printer)
		{
			this.Title = "Print Leveling".Localize();
		}

		public LevelingPlan LevelingPlan { get; set; }

		public override bool Visible
		{
			get
			{
				return printer.Settings.GetValue<bool>(SettingsKey.print_leveling_enabled)
					|| printer.Settings.GetValue<bool>(SettingsKey.print_leveling_required_to_print);
			}
		}

		public override bool Enabled => true;

		public override bool SetupRequired => LevelingValidation.NeedsToBeRun(printer);

		private void Initialize()
		{
			// remember the current baby stepping values
			babySteppingValue = printer.Settings.GetValue<double>(SettingsKey.baby_step_z_offset);

			// clear them while we measure the offsets
			printer.Settings.SetValue(SettingsKey.baby_step_z_offset, "0");

			// turn off print leveling
			printer.Connection.AllowLeveling = false;

			// clear any data that we are going to be acquiring (sampled positions, after z home offset)
			var levelingData = new PrintLevelingData()
			{
				LevelingSystem = printer.Settings.GetValue<LevelingSystem>(SettingsKey.print_leveling_solution)
			};

			printer.Connection.QueueLine("T0");

			switch (levelingData.LevelingSystem)
			{
				case LevelingSystem.Probe3Points:
					LevelingPlan = new LevelWizard3Point(printer);
					break;

				case LevelingSystem.Probe7PointRadial:
					LevelingPlan = new LevelWizard7PointRadial(printer);
					break;

				case LevelingSystem.Probe13PointRadial:
					LevelingPlan = new LevelWizard13PointRadial(printer);
					break;

				case LevelingSystem.Probe100PointRadial:
					LevelingPlan = new LevelWizard100PointRadial(printer);
					break;

				case LevelingSystem.Probe3x3Mesh:
					LevelingPlan = new LevelWizardMesh(printer, 3, 3);
					break;

				case LevelingSystem.Probe5x5Mesh:
					LevelingPlan = new LevelWizardMesh(printer, 5, 5);
					break;

				case LevelingSystem.Probe10x10Mesh:
					LevelingPlan = new LevelWizardMesh(printer, 10, 10);
					break;

				case LevelingSystem.ProbeCustom:
					LevelingPlan = new LevelWizardCustom(printer);
					break;

				default:
					throw new NotImplementedException();
			}
		}

		public override void Dispose()
		{
			// If leveling was on when we started, make sure it is on when we are done.
			printer.Connection.AllowLeveling = true;

			// set the baby stepping back to the last known good value
			printer.Settings.SetValue(SettingsKey.baby_step_z_offset, babySteppingValue.ToString());

			wizardExited = true;

			// make sure we raise the probe on close
			if (printer.Settings.GetValue<bool>(SettingsKey.has_z_probe)
				&& printer.Settings.GetValue<bool>(SettingsKey.use_z_probe)
				&& printer.Settings.GetValue<bool>(SettingsKey.has_z_servo))
			{
				// make sure the servo is retracted
				var servoRetract = printer.Settings.GetValue<double>(SettingsKey.z_servo_retracted_angle);
				printer.Connection.QueueLine($"M280 P0 S{servoRetract}");
			}
		}

		protected override IEnumerator<WizardPage> GetPages()
		{
			var levelingStrings = new LevelingStrings();

			// If no leveling data has been calculated
			bool showWelcomeScreen = printer.Settings.Helpers.PrintLevelingData.SampledPositions.Count == 0
				&& !ProbeCalibrationWizard.UsingZProbe(printer);

			if (showWelcomeScreen)
			{
				yield return new WizardPage(
					this,
					"Initial Printer Setup".Localize(),
					string.Format(
						"{0}\n\n{1}",
						"Congratulations on connecting to your printer. Before starting your first print we need to run a simple calibration procedure.".Localize(),
						"The next few screens will walk your through calibrating your printer.".Localize()))
				{
					WindowTitle = Title
				};
			}

			// Switch to raw mode and construct leveling structures
			this.Initialize();

			// var probePositions = new List<ProbePosition>(Enumerable.Range(0, levelingPlan.ProbeCount).Select(p => new ProbePosition()));
			var probePositions = new List<ProbePosition>(LevelingPlan.ProbeCount);
			for (int j = 0; j < LevelingPlan.ProbeCount; j++)
			{
				probePositions.Add(new ProbePosition());
			}

			bool hasHeatedBed = printer.Settings.GetValue<bool>(SettingsKey.has_heated_bed);
			bool useZProbe = printer.Settings.Helpers.UseZProbe();
			int zProbeSamples = printer.Settings.GetValue<int>(SettingsKey.z_probe_samples);

			// Build welcome text for Print Leveling Overview page
			string buildWelcomeText()
			{
				var secondsPerManualSpot = 10 * 3;
				var secondsPerAutomaticSpot = 3 * zProbeSamples;
				var secondsToCompleteWizard = LevelingPlan.ProbeCount * (useZProbe ? secondsPerAutomaticSpot : secondsPerManualSpot);
				secondsToCompleteWizard += (hasHeatedBed ? 60 * 3 : 0);

				int numberOfSteps = LevelingPlan.ProbeCount;
				int numberOfMinutes = (int)Math.Round(secondsToCompleteWizard / 60.0);

				if (hasHeatedBed)
				{
					return "{0}\n\n\t• {1}\n\t• {2}\n\t• {3}\n\t• {4}\n\t• {5}\n\n{6}\n\n{7}".FormatWith(
						"Welcome to the print leveling wizard. Here is a quick overview on what we are going to do.".Localize(),
						"Select the material you are printing".Localize(),
						"Home the printer".Localize(),
						"Heat the bed".Localize(),
						"Sample the bed at {0} points".Localize().FormatWith(numberOfSteps),
						"Turn auto leveling on".Localize(),
						"We should be done in approximately {0} minutes.".Localize().FormatWith(numberOfMinutes),
						"Click 'Next' to continue.".Localize());
				}
				else
				{
					return "{0}\n\n\t• {1}\n\t• {2}\n\t• {3}\n\n{4}\n\n{5}".FormatWith(
						"Welcome to the print leveling wizard. Here is a quick overview on what we are going to do.".Localize(),
						"Home the printer".Localize(),
						"Sample the bed at {0} points".Localize().FormatWith(numberOfSteps),
						"Turn auto leveling on".Localize(),
						"We should be done in approximately {0} minutes.".Localize().FormatWith(numberOfMinutes),
						"Click 'Next' to continue.".Localize());
				}
			}

			yield return new WizardPage(
				this,
				"Print Leveling Overview".Localize(),
				buildWelcomeText())
			{
				WindowTitle = Title
			};

			yield return new HomePrinterPage(
				this,
				"Homing The Printer".Localize(),
				levelingStrings.HomingPageInstructions(useZProbe, hasHeatedBed),
				useZProbe);

			// if there is a level_x_carriage_markdown oem markdown page
			if (!string.IsNullOrEmpty(printer.Settings.GetValue(SettingsKey.level_x_carriage_markdown)))
			{
				yield return GetLevelXCarriagePage(this, printer);
			}

			// figure out the heating requirements
			double targetBedTemp = 0;
			double targetHotendTemp = 0;
			if (hasHeatedBed)
			{
				targetBedTemp = printer.Settings.GetValue<double>(SettingsKey.bed_temperature);
			}

			if (!useZProbe)
			{
				targetHotendTemp = printer.Settings.Helpers.ExtruderTargetTemperature(0);
			}

			if (targetBedTemp > 0 || targetHotendTemp > 0)
			{
				string heatingInstructions = "";
				if (targetBedTemp > 0 && targetHotendTemp > 0)
				{
					// heating both the bed and the hotend
					heatingInstructions = "Waiting for the bed to heat to ".Localize() + targetBedTemp + "°C\n"
						+ "and the hotend to heat to ".Localize() + targetHotendTemp + "°C.\n"
						+ "\n"
						+ "This will improve the accuracy of print leveling ".Localize()
						+ "and ensure that no filament is stuck to your nozzle.".Localize() + "\n"
						+ "\n"
						+ "Warning! The tip of the nozzle will be HOT!".Localize() + "\n"
						+ "Avoid contact with your skin.".Localize();
				}
				else if (targetBedTemp > 0)
				{
					// only heating the bed
					heatingInstructions = "Waiting for the bed to heat to ".Localize() + targetBedTemp + "°C.\n"
						+ "This will improve the accuracy of print leveling.".Localize();
				}
				else // targetHotendTemp > 0
				{
					// only heating the hotend
					heatingInstructions += "Waiting for the hotend to heat to ".Localize() + targetHotendTemp + "°C.\n"
						+ "This will ensure that no filament is stuck to your nozzle.".Localize() + "\n"
						+ "\n"
						+ "Warning! The tip of the nozzle will be HOT!".Localize() + "\n"
						+ "Avoid contact with your skin.".Localize();
				}

				yield return new WaitForTempPage(
					this,
					"Waiting For Printer To Heat".Localize(),
					heatingInstructions,
					targetBedTemp, 
					new double[] { targetHotendTemp });
			}

			double bedRadius = Math.Min(printer.Settings.GetValue<Vector2>(SettingsKey.bed_size).X, printer.Settings.GetValue<Vector2>(SettingsKey.bed_size).Y) / 2;
			double startProbeHeight = printer.Settings.GetValue<double>(SettingsKey.print_leveling_probe_start);

			int i = 0;

			var probePoints = LevelingPlan.GetPrintLevelPositionToSample().ToList();

			AutoProbePage autoProbePage = null;

			if (printer.Settings.Helpers.UseZProbe())
			{
				autoProbePage = new AutoProbePage(this, printer, "Bed Detection", probePoints, probePositions);
				yield return autoProbePage;
			}
			else
			{
				foreach (var goalProbePoint in probePoints)
				{
					if (wizardExited)
					{
						// Make sure when the wizard is done we turn off the bed heating
						printer.Connection.TurnOffBedAndExtruders(TurnOff.AfterDelay);

						if (printer.Settings.GetValue<bool>(SettingsKey.z_homes_to_max))
						{
							printer.Connection.HomeAxis(PrinterConnection.Axis.XYZ);
						}

						yield break;
					}

					var validProbePosition = EnsureInPrintBounds(printer, goalProbePoint);

					{
						yield return new GetCoarseBedHeight(
							this,
							new Vector3(validProbePosition, startProbeHeight),
							string.Format(
								"{0} {1} {2} - {3}",
								levelingStrings.GetStepString(LevelingPlan.TotalSteps),
								"Position".Localize(),
								i + 1,
								"Low Precision".Localize()),
							probePositions,
							i,
							levelingStrings);

						yield return new GetFineBedHeight(
							this,
							string.Format(
								"{0} {1} {2} - {3}",
								levelingStrings.GetStepString(LevelingPlan.TotalSteps),
								"Position".Localize(),
								i + 1,
								"Medium Precision".Localize()),
							probePositions,
							i,
							levelingStrings);

						yield return new GetUltraFineBedHeight(
							this,
							string.Format(
								"{0} {1} {2} - {3}",
								levelingStrings.GetStepString(LevelingPlan.TotalSteps),
								"Position".Localize(),
								i + 1,
								"High Precision".Localize()),
							probePositions,
							i,
							levelingStrings);
					}
					i++;
				}
			}

			// if we are not using a z-probe, reset the baby stepping at the successful conclusion of leveling
			if (!printer.Settings.GetValue<bool>(SettingsKey.use_z_probe))
			{
				// clear the baby stepping so we don't save the old values
				babySteppingValue = 0;
			}

			yield return new LastPageInstructions(
				this,
				"Print Leveling Wizard".Localize(),
				useZProbe,
				probePositions);
		}

		public static WizardPage GetLevelXCarriagePage(ISetupWizard setupWizard, PrinterConfig printer)
		{
			var levelXCarriagePage = new WizardPage(setupWizard, "Level X Carriage".Localize(), "")
			{
				PageLoad = (page) =>
				{
					// release the motors so the z-axis can be moved
					printer.Connection.ReleaseMotors();

					var markdownText = printer.Settings.GetValue(SettingsKey.level_x_carriage_markdown);
					var markdownWidget = new MarkdownWidget(ApplicationController.Instance.Theme);
					markdownWidget.Markdown = markdownText = markdownText.Replace("\\n", "\n");
					page.ContentRow.AddChild(markdownWidget);
				},
				PageClose = () =>
				{
					// home the printer again to make sure we are ready to level (same behavior as homing page)
					printer.Connection.HomeAxis(PrinterConnection.Axis.XYZ);

					if (!printer.Settings.GetValue<bool>(SettingsKey.z_homes_to_max))
					{
						// move so we don't heat the printer while the nozzle is touching the bed
						printer.Connection.MoveAbsolute(PrinterConnection.Axis.Z, 10, printer.Settings.Helpers.ManualMovementSpeeds().Z);
					}
				}

			};
			return levelXCarriagePage;
		}

		public static Vector2 EnsureInPrintBounds(PrinterConfig printer, Vector2 probePosition)
		{
			// check that the position is within the printing area and if not move it back in
			if (printer.Settings.Helpers.UseZProbe())
			{
				var probeOffset2D = new Vector2(printer.Settings.GetValue<Vector3>(SettingsKey.probe_offset));
				var actualNozzlePosition = probePosition - probeOffset2D;

				// clamp this to the bed bounds
				Vector2 bedSize = printer.Settings.GetValue<Vector2>(SettingsKey.bed_size);
				Vector2 printCenter = printer.Settings.GetValue<Vector2>(SettingsKey.print_center);
				RectangleDouble bedBounds = new RectangleDouble(printCenter - bedSize / 2, printCenter + bedSize / 2);
				Vector2 adjustedPosition = bedBounds.Clamp(actualNozzlePosition);

				// and push it back into the probePosition
				probePosition = adjustedPosition + probeOffset2D;
			}

			return probePosition;
		}
	}

	// this class is so that it is not passed by value
	public class ProbePosition
	{
		public Vector3 position;
	}
}