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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MatterHackers.Agg;
using MatterHackers.Agg.Transform;
using MatterHackers.Agg.UI;
using MatterHackers.Agg.VertexSource;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.CustomWidgets;
using MatterHackers.MatterControl.SlicerConfiguration;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl
{
	public class NozzleOffsetCalibrationIntroPage : DialogPage
	{
		public NozzleOffsetCalibrationIntroPage(PrinterConfig printer)
		{
			this.WindowTitle = "Nozzle Offset Calibration Wizard".Localize();
			this.HeaderText = "Nozzle Offset Calibration".Localize() + ":";
			this.Name = "Nozzle Offset Calibration Wizard";

			this.ContentRow.AddChild(
				new WrappedTextWidget(
					"Offset Calibration required. We'll now print a calibration guide on the printer to tune your nozzel offsets".Localize(), 
					textColor: theme.TextColor, 
					pointSize: theme.DefaultFontSize));

			var nextButton = theme.CreateDialogButton("Next".Localize());
			nextButton.Name = "Begin calibration print";
			nextButton.Click += (s, e) =>
			{
				this.DialogWindow.ChangeToPage(new NozzleOffsetCalibrationPrintPage(printer));
			};

			theme.ApplyPrimaryActionStyle(nextButton);

			this.AddPageAction(nextButton);
		}
	}

	public class NozzleOffsetCalibrationPrintPage: DialogPage
	{
		private PrinterConfig printer;
		private TextButton nextButton;
		private double[] activeOffsets;

		public NozzleOffsetCalibrationPrintPage(PrinterConfig printer)
		{
			this.WindowTitle = "Nozzle Offset Calibration Wizard".Localize();
			this.HeaderText = "Nozzle Offset Calibration".Localize() + ":";
			this.Name = "Nozzle Offset Calibration Wizard";
			this.printer = printer;

			this.ContentRow.AddChild(new TextWidget("Printing Calibration Guide".Localize(), pointSize: theme.DefaultFontSize, textColor: theme.TextColor));

			this.ContentRow.AddChild(new TextWidget("Heating printer...".Localize(), pointSize: theme.DefaultFontSize, textColor: theme.TextColor));

			this.ContentRow.AddChild(new TextWidget("Printing Guide...".Localize(), pointSize: theme.DefaultFontSize, textColor: theme.TextColor));

			nextButton = theme.CreateDialogButton("Next".Localize());
			nextButton.Name = "Configure Calibration";
			nextButton.Enabled = false;
			nextButton.Click += (s, e) =>
			{
				this.DialogWindow.ChangeToPage(new NozzleOffsetCalibrationResultsPage(printer, activeOffsets));
			};
			theme.ApplyPrimaryActionStyle(nextButton);

			this.AddPageAction(nextButton);
		}

		bool vertical = false;

		public override void OnLoad(EventArgs args)
		{
			// Replace with calibration template code
			Task.Run(() =>
			{
				var gcodeSketch = new GCodeSketch();

				var vertical = true;
				if (vertical)
				{
					//turtle.Transform = Affine.NewTranslation(90, 160);
					gcodeSketch.Transform = Affine.NewRotation(MathHelper.DegreesToRadians(90)) * Affine.NewTranslation(110, 45);
				}

				var rect = new RectangleDouble(0, 0, 123, 30);

				var originalRect = rect;

				double nozzleWidth = 0.4;

				int towerSize = 10;

				gcodeSketch.Speed = (int)(printer.Settings.GetValue<double>(SettingsKey.first_layer_speed) * 60);

				double y1 = rect.Bottom;
				gcodeSketch.MoveTo(rect.Left, y1);

				var towerRect = new RectangleDouble(0, 0, towerSize, towerSize);
				towerRect.Offset(originalRect.Left - towerSize, originalRect.Bottom);

				// Draw purge box
				while (towerRect.Width > 4)
				{
					towerRect.Inflate(-nozzleWidth);
					gcodeSketch.DrawRectangle(towerRect);
				}

				// Draw box
				for (var i = 0; i < 3; i++)
				{
					rect.Inflate(-nozzleWidth);
					gcodeSketch.DrawRectangle(rect);
				}

				y1 = rect.YCenter + (nozzleWidth / 2);

				// Draw centerline
				gcodeSketch.MoveTo(rect.Left, y1);
				gcodeSketch.LineTo(rect.Right, y1);
				y1 += nozzleWidth;
				gcodeSketch.MoveTo(rect.Right, y1);
				gcodeSketch.LineTo(rect.Left, y1);

				y1 -= nozzleWidth / 2;

				var x = rect.Left + 1.5;

				double sectionHeight = rect.Height / 2;

				var step = (rect.Width - 3) / 40;
				double y2 = y1 - sectionHeight - (nozzleWidth * 1.5);
				double y3 = y2 - 5;

				var up = true;

				bool drawGlpyphs = false;

				// Draw calibration lines
				for (var i = 0; i <= 40; i++)
				{
					gcodeSketch.MoveTo(x, up ? y1 : y2);

					if ((i % 5 == 0))
					{
						gcodeSketch.LineTo(x, y3);

						var currentPos = gcodeSketch.CurrentPosition;

						gcodeSketch.Speed = 500;

						PrintLineEnd(gcodeSketch, drawGlpyphs, i, currentPos);

						gcodeSketch.Speed = 1800;

						gcodeSketch.MoveTo(x, y3);
						gcodeSketch.MoveTo(x, y2);
					}

					gcodeSketch.LineTo(x, up ? y2 : y1);

					x = x + step;

					up = !up;
				}

				x = rect.Left + 1.5;
				y1 = rect.Top + (nozzleWidth * .5);
				y2 = y1 - sectionHeight + (nozzleWidth * .5);

				gcodeSketch.WriteRaw("T1");
				gcodeSketch.ResetE();

				gcodeSketch.MoveTo(rect.Left, rect.Top);
				towerRect = new RectangleDouble(0, 0, towerSize, towerSize);
				towerRect.Offset(originalRect.Left - towerSize, originalRect.Top - towerSize);

				gcodeSketch.PenDown();

				gcodeSketch.Speed = 800;

				// Draw purge box
				while (towerRect.Width > 4)
				{
					towerRect.Inflate(-nozzleWidth);
					gcodeSketch.DrawRectangle(towerRect);
				}

				gcodeSketch.Speed = 1000;

				up = true;

				// Build offsets
				activeOffsets = new double[41];
				activeOffsets[20] = 0;

				var leftStep = 1.5d / 20;
				var rightStep = 1.5d / 20;

				for (var i = 1; i <= 20; i++)
				{
					activeOffsets[20 - i] = i * leftStep * -1;
					activeOffsets[20 + i] = i * rightStep;
				}

				// Draw calibration lines
				for (var i = 0; i <= 40; i++)
				{
					gcodeSketch.MoveTo(x + activeOffsets[i], up ? y1 : y2, retract: true);
					gcodeSketch.LineTo(x + activeOffsets[i], up ? y2 : y1);

					x = x + step;

					up = !up;
				}

				gcodeSketch.PenUp();

				string gcode = gcodeSketch.ToGCode();

				Console.WriteLine("--------------------------------------------------");
				Console.WriteLine(gcode);

				File.WriteAllText(@"c:\temp\calibration.gcode", gcode);

				printer.Connection.QueueLine(gcode);
				printer.Connection.QueueLine("G1 Z20");
			});

			// TODO: At conclusion of calibration template print, enable next button
			Task.Run(() =>
			{
				// TODO: Silly hack to replicate expected behavior
				Thread.Sleep(10 * 1000);
				nextButton.Enabled = true;
			});

			base.OnLoad(args);
		}

		private static void PrintLineEnd(GCodeSketch turtle, bool drawGlpyphs, int i, Vector2 currentPos)
		{
			if (drawGlpyphs && CalibrationLine.Glyphs.TryGetValue(i, out IVertexSource vertexSource))
			{
				var flattened = new FlattenCurves(vertexSource);

				var verticies = flattened.Vertices();
				var firstItem = verticies.First();
				var position = turtle.CurrentPosition;

				var scale = 0.32;

				if (firstItem.command != ShapePath.FlagsAndCommand.MoveTo)
				{
					turtle.MoveTo((firstItem.position * scale) + currentPos);
				}

				bool closed = false;

				foreach (var item in verticies)
				{
					switch (item.command)
					{
						case ShapePath.FlagsAndCommand.MoveTo:
							turtle.MoveTo((item.position * scale) + currentPos);
							break;

						case ShapePath.FlagsAndCommand.LineTo:
							turtle.LineTo((item.position * scale) + currentPos);
							break;

						case ShapePath.FlagsAndCommand.FlagClose:
							turtle.LineTo((firstItem.position * scale) + currentPos);
							closed = true;
							break;
					}
				}

				if (!closed)
				{
					turtle.LineTo((firstItem.position * scale) + currentPos);
				}
			}
		}
	}

	public class NozzleOffsetCalibrationResultsPage : DialogPage
	{
		private TextWidget activeOffset;
		private CalibrationLine calibrationLine;

		public NozzleOffsetCalibrationResultsPage(PrinterConfig printer, double[] activeOffsets)
		{
			this.WindowTitle = "Nozzle Offset Calibration Wizard".Localize();
			this.HeaderText = "Nozzle Offset Calibration".Localize() + ":";
			this.Name = "Nozzle Offset Calibration Wizard";

			var commonMargin = new BorderDouble(4, 2);

			var row = new FlowLayoutWidget()
			{
				HAnchor = HAnchor.Stretch,
				VAnchor = VAnchor.Absolute,
				Padding = new BorderDouble(6, 0),
				Height = 125
			};
			contentRow.AddChild(row);

			for(var i = 0; i <= 40; i++)
			{
				var calibrationLine = new CalibrationLine(theme)
				{
					Width = 8,
					Margin = 1,
					HAnchor = HAnchor.Absolute,
					VAnchor = VAnchor.Stretch,
					GlyphIndex = (i % 5 == 0) ? i : -1,
					IsNegative = i < 20,
					OffsetIndex = i
				};
				calibrationLine.Click += (s, e) =>
				{
					activeOffset.Text = (activeOffsets[calibrationLine.OffsetIndex] * -1).ToString("0.####");

				};
				row.AddChild(calibrationLine);

				// Add spacers to stretch to size
				if (i < 40)
				{
					row.AddChild(new HorizontalSpacer());
				}
			}

			contentRow.AddChild(activeOffset = new TextWidget("", pointSize: theme.DefaultFontSize, textColor: theme.TextColor));

			row.AfterDraw += (s, e) =>
			{
				int strokeWidth = 3;

				var rect = new RectangleDouble(0, 20, row.LocalBounds.Width, row.LocalBounds.Height);
				rect.Inflate(-2);

				var center = rect.Center;

				e.Graphics2D.Rectangle(rect, theme.TextColor, strokeWidth);
				e.Graphics2D.Line(rect.Left, center.Y, rect.Right, center.Y, theme.TextColor, strokeWidth);
			};

			var nextButton = theme.CreateDialogButton("Next".Localize());
			nextButton.Name = "Begin calibration print";
			nextButton.Click += (s, e) =>
			{
				var hotendOffset = printer.Settings.Helpers.ExtruderOffset(1);
				hotendOffset.Y += double.Parse(activeOffset.Text);
				printer.Settings.Helpers.SetExtruderOffset(1, hotendOffset);
			};

			theme.ApplyPrimaryActionStyle(nextButton);

			this.AddPageAction(nextButton);
		}
	}
}
