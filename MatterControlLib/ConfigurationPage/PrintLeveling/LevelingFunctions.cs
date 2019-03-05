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
using MatterHackers.MatterControl.PartPreviewWindow;
using MatterHackers.MatterControl.PrinterCommunication.Io;
using MatterHackers.MatterControl.SlicerConfiguration;
using MatterHackers.VectorMath;
using MIConvexHull;
using static MatterHackers.MatterControl.PrinterCommunication.Io.PrintLevelingStream;

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
			var vertices = new List<BedPoint>();

			if (SampledPositions.Count > 2)
			{
				foreach (var sample in SampledPositions)
				{
					vertices.Add(new BedPoint(sample.X, sample.Y, sample.Z));
				};
			}
			else
			{
				vertices.Add(new BedPoint(0, 0, 0));
				vertices.Add(new BedPoint(200, 0, 0));
				vertices.Add(new BedPoint(100, 200, 0));
			}

			int extraXPosition = -50000;
			vertices.Add(new BedPoint(extraXPosition, vertices[0].Vector3.Y, 0));

			List<LevelingTriangle> regions = CreateLevelingRegions(printer, vertices, extraXPosition);

			InferRegionsToBedBounds(printer, vertices, regions);

			// Redo triangulation with inferred points for bed
			this.Regions = CreateLevelingRegions(printer, vertices, extraXPosition);
		}

		private class BedPoint : DefaultVertex
		{
			public BedPoint(Vector3 vector3)
			{
				this.Vector3 = vector3;
				this.Position = new[] { vector3.X, vector3.Y };
			}

			public BedPoint(double x, double y, double z)
				: this(new Vector3(x, y, z))
			{
			}

			public Vector3 Vector3 { get; }
		}

		private static void InferRegionsToBedBounds(PrinterConfig printer, List<BedPoint> vertices, List<LevelingTriangle> regions)
		{
			var vertices2 = new List<Vector3Float>();
			var pointCounts = new Dictionary<Vector3Float, int>();

			foreach (var region in regions)
			{
				foreach (var point in new[] { new Vector3Float(region.V0), new Vector3Float(region.V1), new Vector3Float(region.V2) })
				{
					int index = vertices2.IndexOf(point);
					if (index == -1)
					{
						index = vertices.Count;
						vertices2.Add(point);
					}

					if (!pointCounts.TryGetValue(point, out int pointCount))
					{
						pointCount = 0;
					}

					pointCounts[point] = pointCount + 1;
				}
			}

			List<Vector3Float> outerPoints = LevelingMeshVisualizer.GetOuterPoints(vertices2, pointCounts, printer.Bed.BedCenter);

			var bedCenter = printer.Bed.BedCenter;

			var bedEdges = new HashSet<LevelingPlaneEdge>();

			var bounds = printer.Bed.Bounds;

			var topLeft = new Vector3(bounds.Left, bounds.Top, 0);
			var topRight = new Vector3(bounds.Right, bounds.Top, 0);
			var bottomLeft = new Vector3(bounds.Left, bounds.Bottom, 0);
			var bottomRight = new Vector3(bounds.Right, bounds.Bottom, 0);

			bedEdges.Add(new LevelingPlaneEdge(topLeft, topRight));
			bedEdges.Add(new LevelingPlaneEdge(topRight, bottomRight));
			bedEdges.Add(new LevelingPlaneEdge(bottomRight, bottomLeft));
			bedEdges.Add(new LevelingPlaneEdge(bottomLeft, topLeft));

			var center = new Vector3(bedCenter);

			foreach (var point in outerPoints)
			{
				// Get a ray from bed center to outer point and extend past bed bounds
				var point2D = new Vector2(point);
				var normal = (bedCenter - point2D).GetNormal();

				var extended = bedCenter + normal * 1000;

				// Find the intercept
				foreach (var edge in bedEdges)
				{
					PrintLevelingStream.FindIntersection(edge.Start, edge.End, center, new Vector3(extended), out bool linesIntersect, out bool segmentsIntersect, out Vector2 intersection);

					if (segmentsIntersect)
					{
						var newPoint = new Vector3(intersection, point.Z);

						vertices.Add(new BedPoint(newPoint.X, newPoint.Y, newPoint.Z));
					}
				}
			}

			// Add bed extents
			vertices.Add(new BedPoint(topLeft.X, topLeft.Y, 0));
			vertices.Add(new BedPoint(topRight.X, topRight.Y, 0));
			vertices.Add(new BedPoint(bottomRight.X, bottomRight.Y, 0));
			vertices.Add(new BedPoint(bottomLeft.X, bottomLeft.Y, 0));
		}

		private static List<LevelingTriangle> CreateLevelingRegions(PrinterConfig printer, List<BedPoint> vertices, int extraXPosition)
		{
			var triangles = DelaunayTriangulation<BedPoint, DefaultTriangulationCell<BedPoint>>.Create(vertices, .001);

			var probeOffset = new Vector3(0, 0, printer.Settings.GetValue<double>(SettingsKey.z_probe_z_offset));

			var regions = new List<LevelingTriangle>();

			// make the triangle planes
			foreach (var triangle in triangles.Cells)
			{
				var p0 = triangle.Vertices[0];
				var p1 = triangle.Vertices[1];
				var p2 = triangle.Vertices[2];
				if (p0.Vector3.X != extraXPosition && p1.Vector3.X != extraXPosition && p2.Vector3.X != extraXPosition)
				{
					// add all the regions
					regions.Add(
						new LevelingTriangle(
							p0.Vector3 - probeOffset,
							p1.Vector3 - probeOffset,
							p2.Vector3 - probeOffset));
				}
			}

			return regions;
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

			int bestIndex;
			if (!positionToRegion.TryGetValue((xIndex, yIndex), out bestIndex))
			{
				// else calculate the region and store it
				double bestDist = double.PositiveInfinity;

				currentDestination.Z = 0;
				for (int regionIndex = 0; regionIndex < Regions.Count; regionIndex++)
				{
					var dist = (Regions[regionIndex].Center - currentDestination).LengthSquared;
					if (Regions[regionIndex].PointInPolyXY(currentDestination.X, currentDestination.Y))
					{
						// we found the one it is in
						return Regions[regionIndex];
					}
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
				this.Center = (V0 + V1 + V2) / 3;
				this.Plane = new Plane(V0, V1, V2);
			}

			public Vector3 Center { get; }
			public Plane Plane { get; }
			public Vector3 V0 { get; }
			public Vector3 V1 { get; }
			public Vector3 V2 { get; }

			public Vector3 GetPositionWithZOffset(Vector3 currentDestination)
			{
				var destinationAtZ0 = new Vector3(currentDestination.X, currentDestination.Y, 0);

				double hitDistance = this.Plane.GetDistanceToIntersection(destinationAtZ0, Vector3.UnitZ);
				currentDestination.Z += hitDistance;

				return currentDestination;
			}

			private int FindSideOfLine(Vector2 sidePoint0, Vector2 sidePoint1, Vector2 testPosition)
			{
				if (Vector2.Cross(testPosition - sidePoint0, sidePoint1 - sidePoint0) < 0)
				{
					return 1;
				}

				return -1;
			}

			public bool PointInPolyXY(double x, double y)
			{
				// check the bounding rect
				Vector2 vertex0 = new Vector2(V0[0], V0[1]);
				Vector2 vertex1 = new Vector2(V1[0], V1[1]);
				Vector2 vertex2 = new Vector2(V2[0], V2[1]);
				Vector2 hitPosition = new Vector2(x, y);
				int sumOfLineSides = FindSideOfLine(vertex0, vertex1, hitPosition);
				sumOfLineSides += FindSideOfLine(vertex1, vertex2, hitPosition);
				sumOfLineSides += FindSideOfLine(vertex2, vertex0, hitPosition);
				if (sumOfLineSides == -3 || sumOfLineSides == 3)
				{
					return true;
				}

				return false;
			}
		}
	}
}