/*
Copyright (c) 2026, John Lewin
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
using System.Text.RegularExpressions;
using Markdig.Syntax.Inlines;
using MatterControl.Printing;
using MatterHackers.Agg;
using MatterHackers.MatterControl.ConfigurationPage.PrintLeveling;
using MatterHackers.MatterControl.SlicerConfiguration;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl.PrinterCommunication.Io
{
	public class PlotterStream : GCodeStreamProxy
	{
		private PrinterMove lastReportedPosition;
		private readonly object locker = new object();
		private readonly Queue<string> commandQueue = new Queue<string>();
		private Regex eParam = new Regex(@"\s+E-*[\d|\.]+");
		private Regex zParam = new Regex(@"\s+Z[\d|\.]+");
		private double zUp = 3.1;
		private double zDown = 2.2;
		private Vector2 bedSize;

		public PlotterStream(PrinterConfig printer, GCodeStream internalStream)
			: base(printer, internalStream)
		{
			printer.Connection.PrintStarted += (s, e) =>
			{
				lock (locker)
				{
					commandQueue.Clear();
				}

				//var speeds = printer.Settings.Helpers.GetMovementSpeeds();
				//zSpeed = speeds["z"];
				//xSpeed = speeds["x"];
			};

			bedSize = printer.Settings.GetValue<Vector2>(SettingsKey.bed_size);
			rows = (int)(bedSize.Y / offsetY);
			cols = (int)(bedSize.X / offsetX);
		}

		public override string DebugInfo => $"Plotter Stream";

		public override void SetPrinterPosition(PrinterMove position)
		{
			lastReportedPosition = position;
			base.SetPrinterPosition(position);
		}

		private int rows = 0;
		private int cols = 0;

		private int offsetX = 30;
		private int offsetY = 30;


		private string PenUp()
		{
			lastReportedPosition.position.Z = zUp;
			return $"G1 Z{zUp} F12000";
		}

		private string PenDown()
		{
			lastReportedPosition.position.Z = zDown;
			return $"G1 Z{zDown} F12000";
		}

		public override string ReadLine()
		{
			string readLine = null;

			if (commandQueue.Count > 0)
			{
				lock (locker)
				{
					readLine = commandQueue.Dequeue();
				}
			}
			else if (!printer.Connection.Paused)
			{
				readLine = base.ReadLine();
			}

			if (ShouldSkipProcessing(readLine))
			{
				return readLine;
			}

			// Mutatable line
			string line = readLine;

			if (line.Contains(" E")
				&& eParam.Match(line) is Match match
				&& match.Success
				&& match.Value.ToUpper().Trim() != "E0")
			{
				line = eParam.Replace(line, "");
			}

			if (line.StartsWith("G")
				&& line.Contains(" Z")
				&& zParam.Match(line) is Match matchZ
				&& matchZ.Success)
			{

				line = zParam.Replace(line, "");
				return line;
			}

			if (line.StartsWith("; LAYER:"))
			{
				int layerNumber = GCodeFile.GetLayerNumber(line);

				int targetRow = layerNumber / cols;
				int targetCol = layerNumber % cols;
				
				double xOffset = targetCol * offsetX;
				double yOffset = (targetRow + 1) * offsetY;

				printer.Connection.QueueLine($"M206 X-{xOffset} Y-{yOffset}");

				int border = 1;
				var rect = new RectangleDouble(0, 0, offsetX, offsetY);
				rect.Inflate(-border);

				//printer.Connection.QueueLine(@$"
				//	G0 X{rect.Left} Y{rect.Bottom} F5000
				//	G1 Z{zDown}
				//	G1 X{rect.Right}
				//	G1 Y{rect.Top}
				//	G1 X{rect.Left}
				//	G1 Y{rect.Bottom}
				//	G1 Z{zUp}");

				return PenUp();
			}

			if (line.StartsWith("G0") && lastReportedPosition.position.Z <= zDown)
			{
				// Queue the current line and return the lift command
				lock (locker)
				{
					commandQueue.Enqueue(line);
				}

				// Lift to travel height
				line = PenUp();
			}
			else if (line.StartsWith("G1")
					&& lastReportedPosition.position.Z > zDown
					&& !line.Contains("E0"))
			{
				//WriteChecksumLine($"G0 Z{zFloor}"); // drop before printing)

				// Queue the current line and return the lift command
				lock (locker)
				{
					commandQueue.Enqueue(line);
				}

				line = PenDown();
			}

			return line;
		}
	}
}