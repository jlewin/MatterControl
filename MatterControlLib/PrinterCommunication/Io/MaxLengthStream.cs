/*
Copyright (c) 2025, Lars Brubaker, John Lewin
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
using MatterControl.Printing;

namespace MatterHackers.MatterControl.PrinterCommunication.Io
{
	/// <summary>
	/// Provides a G-code stream proxy that splits long movement commands into multiple segments, ensuring that no single
	/// movement exceeds a specified maximum length. This helps maintain consistent movement speeds and improves print
	/// quality by limiting the length of each movement sent to the printer.
	/// </summary>
	/// <remarks>MaxLengthStream should be inserted into the G-code stream before any streams that modify movement
	/// points, such as BabyStepsStream or PrintLevelingStream. This ensures that the segmentation of movements occurs
	/// prior to any additional transformations. The maximum segment length can be adjusted dynamically during printing,
	/// and the stream is designed to operate efficiently by batching and reusing movement segments as needed.</remarks>
	public class MaxLengthStream : GCodeStreamProxy
	{
		// 20 instruction per second
		private double maxSecondsPerSegment = 1.0 / 20.0;
		private List<PrinterMove> movesToSend = new List<PrinterMove>();
		private int layerCount = -1;

		public MaxLengthStream(PrinterConfig printer, GCodeStream internalStream, double maxSegmentLength, bool testing = false)
			: base(printer, internalStream)
		{
			// make sure there is no BabyStepStream already (it must come after max length)
#if DEBUG
			if (!testing)
			{
				foreach (var subStream in this.InternalStreams())
				{
					if (subStream is BabyStepsStream)
					{
						throw new Exception("MaxLengthStream must come before BabyStepsSteam (we need the max length points to be baby stepped).");
					}

					if (subStream is PrintLevelingStream)
					{
						throw new Exception("MaxLengthStream must come before PrintLevelingStream (we need the max length points to be leveled).");
					}
				}
			}
#endif
			this.MaxSegmentLength = maxSegmentLength;
		}

		PrinterMove lastDestination = PrinterMove.Unknown;
		public double MaxSegmentLength { get; set; }

		public override string DebugInfo => $"Last Destination = {lastDestination}";

		public override void Cancel()
		{
			lock (movesToSend)
			{
				movesToSend.Clear();
			}

			base.Cancel();
		}

		public override string ReadLine()
		{
			if (movesToSend.Count == 0)
			{
				string lineToSend = base.ReadLine();

				if (ShouldSkipProcessing(lineToSend))
				{
					return lineToSend;
				}

				if (layerCount < 1
					&& GCodeFile.IsLayerChange(lineToSend))
				{
					layerCount++;
					if (layerCount == 1)
					{
						MaxSegmentLength = 5;
					}
				}


				if (LineIsMovement(lineToSend))
				{
					PrinterMove currentDestination = GetPosition(lineToSend, lastDestination);

					if (currentDestination.FullyKnown)
					{
						// If lastDestination is unknown, initialize it to currentDestination before calculating delta
						// to avoid issues with PositiveInfinity values
						if (!lastDestination.FullyKnown)
						{
							lastDestination = currentDestination;
							return lineToSend;
						}

						PrinterMove deltaToDestination = currentDestination - lastDestination;
						deltaToDestination.feedRate = 0; // remove the changing of the federate (we'll set it initially)
						double lengthSquared = Math.Max(deltaToDestination.LengthSquared, deltaToDestination.extrusion * deltaToDestination.extrusion);
						if (lengthSquared > MaxSegmentLength * MaxSegmentLength)
						{
							// create the line segments to send
							double length = Math.Sqrt(lengthSquared);
							int numSegmentsToCutInto = (int)Math.Ceiling(length / MaxSegmentLength);

							// segments = (((mm/min) / (60s/min))mm/s / s/segment)segments*mm / mm
							double maxSegmentsCanTransmit = 1 / (((currentDestination.feedRate / 60) * maxSecondsPerSegment) / length);

							int numSegmentsToSend = Math.Max(1, Math.Min(numSegmentsToCutInto, (int)maxSegmentsCanTransmit));

							if (numSegmentsToSend > 1)
							{
								PrinterMove deltaForSegment = deltaToDestination / numSegmentsToSend;
								PrinterMove nextPoint = lastDestination + deltaForSegment;
								nextPoint.feedRate = currentDestination.feedRate;
								for (int i = 0; i < numSegmentsToSend; i++)
								{
									lock (movesToSend)
									{
										movesToSend.Add(nextPoint);
									}

									nextPoint += deltaForSegment;
								}

								// send the first one
								PrinterMove positionToSend = movesToSend[0];
								lock (movesToSend)
								{
									movesToSend.RemoveAt(0);
								}

								string altredLineToSend = CreateMovementLine(positionToSend, lastDestination);
								lastDestination = positionToSend;
								return altredLineToSend;
							}
						}
					}

					lastDestination = currentDestination;
				}
				return lineToSend;
			}
			else
			{
				PrinterMove positionToSend = movesToSend[0];
				lock (movesToSend)
				{
					movesToSend.RemoveAt(0);
				}

				string lineToSend = CreateMovementLine(positionToSend, lastDestination);

				lastDestination = positionToSend;

				return lineToSend;
			}
		}

		public override void SetPrinterPosition(PrinterMove position)
		{
			this.lastDestination.CopyKnowSettings(position);
			internalStream.SetPrinterPosition(lastDestination);
		}
	}
}