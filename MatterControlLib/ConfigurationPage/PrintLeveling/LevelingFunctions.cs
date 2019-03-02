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
using System.Text;
using MatterControl.Printing;
using MatterHackers.MatterControl.PrinterCommunication.Io;
using MatterHackers.MatterControl.SlicerConfiguration;
using MatterHackers.VectorMath;
using MIConvexHull;

namespace MatterHackers.MatterControl.ConfigurationPage.PrintLeveling
{
	public class LevelingFunctions
	{
		private Vector2 bedSize;
		private Dictionary<(int, int), int> positionToRegion = new Dictionary<(int, int), int>();
		private PrinterConfig printer;

		public LevelingFunctions(PrinterConfig printer, PrintLevelingData levelingData)
		{
			this.printer = printer;
			this.SampledPositions = new List<Vector3>(levelingData.SampledPositions);

			bedSize = printer.Settings.GetValue<Vector2>(SettingsKey.bed_size);

			// get the delaunay triangulation
			var zDictionary = new Dictionary<(double, double), double>();
			var vertices = new List<DefaultVertex>();

			if (SampledPositions.Count > 2)
			{
				foreach (var sample in SampledPositions)
				{
					vertices.Add(new DefaultVertex()
					{
						Position = new double[] { sample.X, sample.Y }
					});
					var key = (sample.X, sample.Y);
					if (!zDictionary.ContainsKey(key))
					{
						zDictionary.Add(key, sample.Z);
					}
				};
			}
			else
			{
				vertices.Add(new DefaultVertex()
				{
					Position = new double[] { 0, 0 }
				});
				zDictionary.Add((0, 0), 0);

				vertices.Add(new DefaultVertex()
				{
					Position = new double[] { 200, 0 }
				});
				zDictionary.Add((200, 0), 0);

				vertices.Add(new DefaultVertex()
				{
					Position = new double[] { 100, 200 }
				});
				zDictionary.Add((100, 200), 0);
			}

			int extraXPosition = -50000;
			vertices.Add(new DefaultVertex()
			{
				Position = new double[] { extraXPosition, vertices[0].Position[1] }
			});

			var triangles = DelaunayTriangulation<DefaultVertex, DefaultTriangulationCell<DefaultVertex>>.Create(vertices, .001);

			var probeOffset = new Vector3(0, 0, printer.Settings.GetValue<double>(SettingsKey.z_probe_z_offset));
			// make all the triangle planes for these triangles
			foreach (var triangle in triangles.Cells)
			{
				var p0 = triangle.Vertices[0].Position;
				var p1 = triangle.Vertices[1].Position;
				var p2 = triangle.Vertices[2].Position;
				if (p0[0] != extraXPosition && p1[0] != extraXPosition && p2[0] != extraXPosition)
				{
					var v0 = new Vector3(p0[0], p0[1], zDictionary[(p0[0], p0[1])]);
					var v1 = new Vector3(p1[0], p1[1], zDictionary[(p1[0], p1[1])]);
					var v2 = new Vector3(p2[0], p2[1], zDictionary[(p2[0], p2[1])]);
					// add all the regions
					Regions.Add(new LevelingTriangle(v0 - probeOffset, v1 - probeOffset, v2 - probeOffset));
				}
			}
		}

		public List<Vector3> SampledPositions { get; }

		public List<LevelingTriangle> Regions { get; } = new List<LevelingTriangle>();

		public string ApplyLeveling(string lineBeingSent, Vector3 destination, double eOverride = -1)
		{
			bool hasMovement = lineBeingSent.Contains("X") || lineBeingSent.Contains("Y") || lineBeingSent.Contains("Z");
			if (!hasMovement)
			{
				// Leave non-leveling lines untouched
				return lineBeingSent;
			}

			double extruderDelta = 0;
			GCodeFile.GetFirstNumberAfter("E", lineBeingSent, ref extruderDelta);

			if (eOverride != -1)
			{
				extruderDelta = eOverride;
			}

			double feedRate = 0;
			GCodeFile.GetFirstNumberAfter("F", lineBeingSent, ref feedRate);

			var newLine = new StringBuilder("G1");

			// Position data is not optional for leveling - fall back to fixed defaults when not yet known
			var correctedPosition = new Vector3(
			(destination.X == double.PositiveInfinity) ? 0 : destination.X,
			(destination.Y == double.PositiveInfinity) ? 0 : destination.Y,
			(destination.Z == double.PositiveInfinity) ? 0 : destination.Z);

			// get the offset to the active extruder
			var extruderOffset = printer.Settings.Helpers.ExtruderOffset(printer.Connection.ActiveExtruderIndex);
			correctedPosition += extruderOffset;

			// level it
			Vector3 outPosition = GetPositionWithZOffset(correctedPosition);

			// take the extruder offset back out
			outPosition -= extruderOffset;

			// Only output known positions
			if (destination.X != double.PositiveInfinity)
			{
				newLine.Append($" X{outPosition.X:0.##}");
			}

			if (destination.Y != double.PositiveInfinity)
			{
				newLine.Append($" Y{outPosition.Y:0.##}");
			}

			newLine.Append($" Z{outPosition.Z:0.##}");

			if (lineBeingSent.Contains("E"))
			{
				newLine.Append($" E{extruderDelta:0.###}");
			}

			if (lineBeingSent.Contains("F"))
			{
				newLine.Append($" F{feedRate:0.##}");
			}

			return newLine.ToString();
		}

		public string ApplyLeveling(string lineBeingSent, PrinterMove currentDestination)
		{
			return this.ApplyLeveling(lineBeingSent, currentDestination.position, currentDestination.extrusion);
		}

		public Vector3 GetPositionWithZOffset(Vector3 currentDestination)
		{
			LevelingTriangle region = GetCorrectRegion(currentDestination);

			return region.GetPositionWithZOffset(currentDestination);
		}

		public LevelingTriangle GetCorrectRegion(Vector3 currentDestination)
		{
			int xIndex = (int)Math.Round(currentDestination.X * 100 / bedSize.X);
			int yIndex = (int)Math.Round(currentDestination.Y * 100 / bedSize.Y);

			var point2D = new Vector2(currentDestination);
			foreach(var region in Regions)
			{
				if (region.Contains2(point2D))
				{
					return region;
				}
			}


			int bestIndex;
			if (!positionToRegion.TryGetValue((xIndex, yIndex), out bestIndex))
			{
				// else calculate the region and store it
				double bestDist = double.PositiveInfinity;

				currentDestination.Z = 0;
				for (int regionIndex = 0; regionIndex < Regions.Count; regionIndex++)
				{
					var dist = (Regions[regionIndex].Center - currentDestination).LengthSquared;
					if (dist < bestDist)
					{
						bestIndex = regionIndex;
						bestDist = dist;
					}
				}

				positionToRegion.Add((xIndex, yIndex), bestIndex);
			}

			return Regions[bestIndex];
		}

		public class LevelingTriangle
		{
			public LevelingTriangle(Vector3 v0, Vector3 v1, Vector3 v2)
			{
				this.V0 = v0;
				this.V1 = v1;
				this.V2 = v2;

				this.p0 = new Vector2(v0);
				this.p1 = new Vector2(v1);
				this.p2 = new Vector2(v2);

				this.Center = (V0 + V1 + V2) / 3;
				this.Plane = new Plane(V0, V1, V2);
			}

			public Vector3 Center { get; }
			public Plane Plane { get; }
			public Vector3 V0 { get; }
			public Vector3 V1 { get; }
			public Vector3 V2 { get; }

			private Vector2 p0;
			private Vector2 p1;
			private Vector2 p2;

			public Vector3 GetPositionWithZOffset(Vector3 currentDestination)
			{
				var destinationAtZ0 = new Vector3(currentDestination.X, currentDestination.Y, 0);

				double hitDistance = this.Plane.GetDistanceToIntersection(destinationAtZ0, Vector3.UnitZ);
				currentDestination.Z += hitDistance;

				return currentDestination;
			}

			public bool Contains(Vector2 p)
			{
				var s = p0.Y * p2.X - p0.X * p2.Y + (p2.Y - p0.Y) * p.X + (p0.X - p2.X) * p.Y;
				var t = p0.X * p1.Y - p0.Y * p1.X + (p0.Y - p1.Y) * p.X + (p1.X - p0.X) * p.Y;

				if ((s < 0) != (t < 0))
					return false;

				var A = -p1.Y * p2.X + p0.Y * (p2.X - p1.X) + p0.X * (p1.Y - p2.Y) + p1.X * p2.Y;

				return A < 0 ?
						(s <= 0 && s + t >= A) :
						(s >= 0 && s + t <= A);
			}

			public bool Contains2(Vector2 p)
			{
				double x1 = p0.X;
				double y1 = p0.Y;

				double x2 = p1.X;
				double y2 = p1.Y;

				double x3 = p2.X;
				double y3 = p2.Y;

				double x, y;
				x = p.X;
				y = p.Y;

				double a = ((y2 - y3) * (x - x3) + (x3 - x2) * (y - y3)) / ((y2 - y3) * (x1 - x3) + (x3 - x2) * (y1 - y3));
				double b = ((y3 - y1) * (x - x3) + (x1 - x3) * (y - y3)) / ((y2 - y3) * (x1 - x3) + (x3 - x2) * (y1 - y3));
				double c = 1 - a - b;

				if (a == 0 || b == 0 || c == 0)
				{
					Console.WriteLine("Point is on the side of the triangle");
					return isInside(x1, y1, x2, y2, x3, y3, x, y);
				}
				else if (a >= 0 && a <= 1 && b >= 0 && b <= 1 && c >= 0 && c <= 1)
				{
					Console.WriteLine("Point is inside of the triangle.");
					return true;
				}
				else
				{
					Console.WriteLine("Point is outside of the triangle.");
					return false;
				}
			}

			/* A utility function to calculate area of triangle  
    formed by (x1, y1) (x2, y2) and (x3, y3) */
			static double area(double x1, double y1, double x2,
							   double y2, double x3, double y3)
			{
				return Math.Abs((x1 * (y2 - y3) +
								 x2 * (y3 - y1) +
								 x3 * (y1 - y2)) / 2.0);
			}

			/* A function to check whether point P(x, y) lies 
			inside the triangle formed by A(x1, y1), 
			B(x2, y2) and C(x3, y3) */
			static bool isInside(double x1, double y1, double x2,
								 double y2, double x3, double y3,
								 double x, double y)
			{
				/* Calculate area of triangle ABC */
				double A = area(x1, y1, x2, y2, x3, y3);

				/* Calculate area of triangle PBC */
				double A1 = area(x, y, x2, y2, x3, y3);

				/* Calculate area of triangle PAC */
				double A2 = area(x1, y1, x, y, x3, y3);

				/* Calculate area of triangle PAB */
				double A3 = area(x1, y1, x2, y2, x, y);

				/* Check if sum of A1, A2 and A3 is same as A */
				return (A == A1 + A2 + A3);
			}
		}
	}
}