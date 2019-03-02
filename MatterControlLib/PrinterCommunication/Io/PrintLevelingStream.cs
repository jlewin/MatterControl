/*
Copyright (c) 2018, Lars Brubaker
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using MatterControl.Printing;
using MatterHackers.MatterControl.ConfigurationPage.PrintLeveling;
using MatterHackers.MatterControl.SlicerConfiguration;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl.PrinterCommunication.Io
{
	public class PrintLevelingStream : GCodeStreamProxy
	{
		private PrinterMove _lastDestination = PrinterMove.Unknown;
		private bool activePrinting;
		private LevelingFunctions currentLevelingFunctions = null;
		private double currentProbeOffset;
		private bool wroteLevelingStatus = false;
		private bool gcodeAlreadyLeveled = false;

		private StreamWriter writerA;

		private Queue<(string, PrinterMove)> movesToSend = new Queue<(string, PrinterMove)>();

		private static int fileI = 0;

		public PrintLevelingStream(PrinterConfig printer, GCodeStream internalStream, bool activePrinting)
			: base(printer, internalStream)
		{
			// always reset this when we construct
			AllowLeveling = true;
			this.activePrinting = activePrinting;

			writerA = new StreamWriter($@"C:\Users\mr_bl\Desktop\source{fileI++}.gcode");
		}

		public override string DebugInfo
		{
			get
			{
				return $"Last Destination = {_lastDestination}";
			}
		}

		public override void Dispose()
		{
			writerA.Dispose();

			base.Dispose();
		}

		public bool AllowLeveling { get; set; }

		bool LevelingActive
		{
			get
			{
				return AllowLeveling
					&& printer.Settings.GetValue<bool>(SettingsKey.print_leveling_enabled)
					&& !printer.Settings.GetValue<bool>(SettingsKey.has_hardware_leveling);
			}
		}

		public class DebugLevelingItem
		{
			public string SourceText { get; set; }
			public LevelingPlaneEdge SourceLine { get; set; }

			public List<Vector2> Splits { get; } = new List<Vector2>();
			public LevelingPlaneEdge Edge { get; internal set; }
		}

		public static List<DebugLevelingItem> AllDebugItems = new List<DebugLevelingItem>();

		public override string ReadLine()
		{
			if (movesToSend.Count > 0)
			{
				return this.SendLineFromQueue();
			}

			if (!wroteLevelingStatus && LevelingActive)
			{
				wroteLevelingStatus = true;
				return "; Software Leveling Applied";
			}

			string lineToSend = base.ReadLine();

			writerA.WriteLine(lineToSend);

			if (lineToSend != null
				&& lineToSend.EndsWith("; NO_PROCESSING"))
			{
				return lineToSend;
			}

			if (lineToSend == "; Software Leveling Applied")
			{
				gcodeAlreadyLeveled = true;
			}

			if (lineToSend != null
				&& LevelingActive
				&& !gcodeAlreadyLeveled)
			{
				if (LineIsMovement(lineToSend))
				{
					PrinterMove currentDestination = GetPosition(lineToSend, _lastDestination);

					var timer = Stopwatch.StartNew();

					var intersections = new List<(LevelingPlaneEdge Edge, Vector2 Position)>();

					foreach(var edge in levelingEdges)
					{
						FindIntersection(edge.Start, edge.End, _lastDestination.position, currentDestination.position, out bool linesIntersect, out bool segmentsIntersect, out Vector2 intersection);

						if (segmentsIntersect)
						{
							intersections.Add((edge, intersection));
						}
					}

					if (timer.ElapsedMilliseconds > 0)
					{
						Console.WriteLine("edgeTest in " + timer.ElapsedMilliseconds);
					}

					if (intersections.Count > 0)
					{
						var localLastDestination = _lastDestination;

						var lastPosition = new Vector2(localLastDestination.position);

						foreach (var intersectionInfo in intersections.OrderBy(ix => ix.Position.Distance(lastPosition)))
						{
							var debugItem = new DebugLevelingItem()
							{
								SourceText = lineToSend,
								SourceLine = new LevelingPlaneEdge(_lastDestination.position, currentDestination.position),
								Edge = intersectionInfo.Edge
							};

							AllDebugItems.Add(debugItem);

							var intersection = intersectionInfo.Position;

							var destination = new Vector2(currentDestination.position);

							var eDelta = currentDestination.extrusion - localLastDestination.extrusion;
							var zDelta = currentDestination.position.Z - localLastDestination.position.Z;

							var thisLength = lastPosition.Distance(intersection);
							var totalLength = lastPosition.Distance(destination);

							var lengthFactor = thisLength / totalLength;

							var thisE = localLastDestination.extrusion + (eDelta * lengthFactor);
							var thisZ = localLastDestination.position.Z + (zDelta * lengthFactor);

							double feedRate = 0;
							GCodeFile.GetFirstNumberAfter("F", lineToSend, ref feedRate);

							localLastDestination = new PrinterMove(
								new Vector3(intersection.X, intersection.Y, thisZ),
								thisE, feedRate);

							movesToSend.Enqueue((lineToSend, localLastDestination));

							lastPosition = new Vector2(localLastDestination.position);

							debugItem.Splits.Add(intersection);

							//currentDestination.extrusion = currentDestination.extrusion - revisedE;

						}

						movesToSend.Enqueue((lineToSend, currentDestination));

						if (movesToSend.Count > 0)
						{
							return this.SendLineFromQueue();
						}
					}

					var leveledLine = GetLeveledPosition(lineToSend, currentDestination);

					// TODO: clamp to 0 - baby stepping - extruder z-offset, so we don't go below the bed (for the active extruder)

					_lastDestination = currentDestination;

					return leveledLine;
				}
				else if (lineToSend.StartsWith("G29"))
				{
					// remove G29 (machine prob bed) if we are running our own leveling.
					lineToSend = base.ReadLine(); // get the next line instead
				}
			}

			return lineToSend;
		}

		private string SendLineFromQueue()
		{
			(string sourceLine, PrinterMove destination) = movesToSend.Dequeue();

			var leveledLine = currentLevelingFunctions.ApplyLeveling(sourceLine, destination);
			_lastDestination = destination;

			return "  " + leveledLine;
		}

		// http://csharphelper.com/blog/2014/08/determine-where-two-lines-intersect-in-c/
		// Find the point of intersection between
		// the lines p1 --> p2 and p3 --> p4.
		private void FindIntersection(
			Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4,
			out bool lines_intersect, out bool segments_intersect,
			out Vector2 intersection)
		{
			// Get the segments' parameters.
			double dx12 = p2.X - p1.X;
			double dy12 = p2.Y - p1.Y;
			double dx34 = p4.X - p3.X;
			double dy34 = p4.Y - p3.Y;

			// Solve for t1 and t2
			double denominator = (dy12 * dx34 - dx12 * dy34);

			double t1 =
				((p1.X - p3.X) * dy34 + (p3.Y - p1.Y) * dx34)
					/ denominator;
			if (double.IsInfinity(t1))
			{
				// The lines are parallel (or close enough to it).
				lines_intersect = false;
				segments_intersect = false;
				intersection = new Vector2(float.NaN, float.NaN);
				return;
			}
			lines_intersect = true;

			double t2 =
				((p3.X - p1.X) * dy12 + (p1.Y - p3.Y) * dx12)
					/ -denominator;

			// Find the point of intersection.
			intersection = new Vector2(p1.X + dx12 * t1, p1.Y + dy12 * t1);

			// The segments intersect if t1 and t2 are between 0 and 1.
			segments_intersect =
				((t1 >= 0) && (t1 <= 1) &&
				 (t2 >= 0) && (t2 <= 1));
		}

		public override void SetPrinterPosition(PrinterMove position)
		{
			if (LevelingActive
				&& position.PositionFullyKnown)
			{
				string lineBeingSent = CreateMovementLine(position);
				string leveledPosition = GetLeveledPosition(lineBeingSent, position);

				PrinterMove leveledDestination = GetPosition(leveledPosition, PrinterMove.Unknown);
				PrinterMove deltaToLeveledPosition = leveledDestination - position;

				PrinterMove withoutLevelingOffset = position - deltaToLeveledPosition;

				_lastDestination = withoutLevelingOffset;
				_lastDestination.extrusion = position.extrusion;
				_lastDestination.feedRate = position.feedRate;

				internalStream.SetPrinterPosition(_lastDestination);
			}
			else
			{
				internalStream.SetPrinterPosition(position);
			}
		}

		public class LevelingPlaneEdge : IEquatable<LevelingPlaneEdge>
		{
			public LevelingPlaneEdge(Vector3 start, Vector3 end)
			{
				bool swap = start.Length > end.Length;

				this.Start = swap ? end : start;
				this.End = swap ? start: end;
			}

			public Vector3 Start { get; }

			public Vector3 End { get; }

			public bool Equals(LevelingPlaneEdge other)
			{
				return (this.Start == other.Start && this.End == other.End)
					|| (this.Start == other.End && this.End == other.Start);
			}

			public override int GetHashCode()
			{
				unchecked // Overflow is fine, just wrap
				{
					int hash = 17;

					// Suitable nullity checks etc, of course :)
					hash = hash * 23 + Start.GetHashCode();
					hash = hash * 23 + End.GetHashCode();

					return hash;
				}
			}

			public override string ToString()
			{
				return $"{Start}-{End}";
			}
		}

		private HashSet<LevelingPlaneEdge> levelingEdges = new HashSet<LevelingPlaneEdge>();

		private string GetLeveledPosition(string lineBeingSent, PrinterMove currentDestination)
		{
			PrintLevelingData levelingData = printer.Settings.Helpers.GetPrintLevelingData();

			if (levelingData != null
				&& printer.Settings?.GetValue<bool>(SettingsKey.print_leveling_enabled) == true
				&& (lineBeingSent.StartsWith("G0 ") || lineBeingSent.StartsWith("G1 ")))
			{
				if (currentLevelingFunctions == null
					|| currentProbeOffset != printer.Settings.GetValue<double>(SettingsKey.z_probe_z_offset)
					|| !levelingData.SamplesAreSame(currentLevelingFunctions.SampledPositions))
				{
					currentProbeOffset = printer.Settings.GetValue<double>(SettingsKey.z_probe_z_offset);
					currentLevelingFunctions = new LevelingFunctions(printer, levelingData);

					foreach (var region in currentLevelingFunctions.Regions)
					{
						levelingEdges.Add(new LevelingPlaneEdge(region.V0, region.V1));
						levelingEdges.Add(new LevelingPlaneEdge(region.V1, region.V2));
						levelingEdges.Add(new LevelingPlaneEdge(region.V2, region.V0));
					}
				}

				lineBeingSent = currentLevelingFunctions.ApplyLeveling(lineBeingSent, currentDestination.position);
			}

			return lineBeingSent;
		}
	}
}