﻿/*
Copyright (c) 2014, Lars Brubaker
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

using ClipperLib;
using MatterHackers.Agg;
using MatterHackers.Agg.VertexSource;
using MatterHackers.MeshVisualizer;
using MatterHackers.PolygonMesh;
using MatterHackers.RayTracer;
using MatterHackers.VectorMath;
using System;
using System.Collections.Generic;

namespace MatterHackers.MatterControl
{
	using DataConverters3D;
	using Localizations;
	using System.Collections;
	using System.Linq;
	using Polygon = List<IntPoint>;

	using Polygons = List<List<IntPoint>>;

	public static class PlatingHelper
	{
		public static PathStorage PolygonToPathStorage(Polygon polygon)
		{
			PathStorage output = new PathStorage();

			bool first = true;
			foreach (IntPoint point in polygon)
			{
				if (first)
				{
					output.Add(point.X, point.Y, ShapePath.FlagsAndCommand.CommandMoveTo);
					first = false;
				}
				else
				{
					output.Add(point.X, point.Y, ShapePath.FlagsAndCommand.CommandLineTo);
				}
			}

			output.ClosePolygon();

			output.Add(0, 0, ShapePath.FlagsAndCommand.CommandStop);

			return output;
		}

		public static PathStorage PolygonToPathStorage(Polygons polygons)
		{
			PathStorage output = new PathStorage();

			foreach (Polygon polygon in polygons)
			{
				bool first = true;
				foreach (IntPoint point in polygon)
				{
					if (first)
					{
						output.Add(point.X, point.Y, ShapePath.FlagsAndCommand.CommandMoveTo);
						first = false;
					}
					else
					{
						output.Add(point.X, point.Y, ShapePath.FlagsAndCommand.CommandLineTo);
					}
				}

				output.ClosePolygon();
			}
			output.Add(0, 0, ShapePath.FlagsAndCommand.CommandStop);

			return output;
		}

		public static void ArrangeMeshGroups(List<IObject3D> object3DList, List<Matrix4X4> asyncMeshGroupTransforms, List<PlatingData> MeshGroupExtraData,
	Action<double, string> reportProgressChanged)
		{
			// TODO: ******************** !!!!!!!!!!!!!!! ********************
		}

		/*
		public static void ArrangeMeshGroups(List<IObject3D> object3DList, List<Matrix4X4> asyncMeshGroupTransforms, List<PlatingMeshGroupData> MeshGroupExtraData,
			Action<double, string> reportProgressChanged)
		{


			// move them all out of the way
			for (int i = 0; i < MeshGroups.Count; i++)
			{
				asyncMeshGroupTransforms[i] *= Matrix4X4.CreateTranslation(10000, 10000, 0);
			}

			// sort them by size
			for (int i = 0; i < MeshGroups.Count; i++)
			{
				AxisAlignedBoundingBox iAABB = MeshGroups[i].GetAxisAlignedBoundingBox(asyncMeshGroupTransforms[i]);
				for (int j = i + 1; j < MeshGroups.Count; j++)
				{
					AxisAlignedBoundingBox jAABB = MeshGroups[j].GetAxisAlignedBoundingBox(asyncMeshGroupTransforms[j]);
					if (Math.Max(iAABB.XSize, iAABB.YSize) < Math.Max(jAABB.XSize, jAABB.YSize))
					{
						PlatingMeshGroupData tempData = MeshGroupExtraData[i];
						MeshGroupExtraData[i] = MeshGroupExtraData[j];
						MeshGroupExtraData[j] = tempData;

						MeshGroup tempMeshGroup = MeshGroups[i];
						MeshGroups[i] = MeshGroups[j];
						MeshGroups[j] = tempMeshGroup;

						Matrix4X4 iTransform = asyncMeshGroupTransforms[i];
						Matrix4X4 jTransform = asyncMeshGroupTransforms[j];
						Matrix4X4 tempTransform = iTransform;
						iTransform = jTransform;
						jTransform = tempTransform;

						asyncMeshGroupTransforms[i] = jTransform;
						asyncMeshGroupTransforms[j] = iTransform;

						iAABB = jAABB;
					}
				}
			}

			double ratioPerMeshGroup = 1.0 / MeshGroups.Count;
			double currentRatioDone = 0;
			// put them onto the plate (try the center) starting with the biggest and moving down
			for (int meshGroupIndex = 0; meshGroupIndex < MeshGroups.Count; meshGroupIndex++)
			{
				reportProgressChanged(currentRatioDone, "Calculating Positions...".Localize());

				MeshGroup meshGroup = MeshGroups[meshGroupIndex];
				Vector3 meshLowerLeft = meshGroup.GetAxisAlignedBoundingBox(asyncMeshGroupTransforms[meshGroupIndex]).minXYZ;
				asyncMeshGroupTransforms[meshGroupIndex] *= Matrix4X4.CreateTranslation(-meshLowerLeft);

				PlatingHelper.MoveMeshGroupToOpenPosition(meshGroupIndex, MeshGroupExtraData, MeshGroups, asyncMeshGroupTransforms);

				// and create the trace info so we can select it
				if (MeshGroupExtraData[meshGroupIndex].meshTraceableData.Count == 0)
				{
					PlatingHelper.CreateITraceableForMeshGroup(MeshGroupExtraData, MeshGroups, meshGroupIndex, null);
				}

				currentRatioDone += ratioPerMeshGroup;

				// and put it on the bed
				PlatingHelper.PlaceMeshGroupOnBed(MeshGroups, asyncMeshGroupTransforms, meshGroupIndex);
			}

			// and finally center whatever we have as a group
			{
				AxisAlignedBoundingBox bounds = MeshGroups[0].GetAxisAlignedBoundingBox(asyncMeshGroupTransforms[0]);
				for (int i = 1; i < MeshGroups.Count; i++)
				{
					bounds = AxisAlignedBoundingBox.Union(bounds, MeshGroups[i].GetAxisAlignedBoundingBox(asyncMeshGroupTransforms[i]));
				}

				Vector3 boundsCenter = (bounds.maxXYZ + bounds.minXYZ) / 2;
				for (int i = 0; i < MeshGroups.Count; i++)
				{
					asyncMeshGroupTransforms[i] *= Matrix4X4.CreateTranslation(-boundsCenter + new Vector3(0, 0, bounds.ZSize / 2));
				}
			}
		}
		*/
		public static void PlaceMeshGroupOnBed(IObject3D object3D)
		{
			AxisAlignedBoundingBox bounds = object3D.GetAxisAlignedBoundingBox(Matrix4X4.Identity);
			Vector3 boundsCenter = (bounds.maxXYZ + bounds.minXYZ) / 2;

			object3D.Matrix *= Matrix4X4.CreateTranslation(new Vector3(0, 0, -boundsCenter.z + bounds.ZSize / 2));
		}

		public static void PlaceMeshAtHeight(IObject3D objectToMove, double zHeight)
		{
			AxisAlignedBoundingBox bounds = objectToMove.GetAxisAlignedBoundingBox(Matrix4X4.Identity);

			objectToMove.Matrix *= Matrix4X4.CreateTranslation(new Vector3(0, 0, zHeight - bounds.minXYZ.z));
		}

		public static void CenterMeshGroupXY(List<MeshGroup> meshesGroupList, List<Matrix4X4> meshTransforms, int index)
		{
			AxisAlignedBoundingBox bounds = GetAxisAlignedBoundingBox(meshesGroupList[index], meshTransforms[index]);
			Vector3 boundsCenter = (bounds.maxXYZ + bounds.minXYZ) / 2;

			meshTransforms[index] *= Matrix4X4.CreateTranslation(new Vector3(-boundsCenter.x + bounds.XSize / 2, -boundsCenter.y + bounds.YSize / 2, 0));
		}

		public static void MoveToOpenPosition(IObject3D objectToAdd, IObject3D scene)
		{
			if (objectToAdd == null || !scene.HasChildren)
			{
				return;
			}

			// find the bounds of all items in the scene
			AxisAlignedBoundingBox allPlacedMeshBounds = scene.Children.GetUnionedAxisAlignedBoundingBox();
			
			// move the part to the total bounds lower left side
			Vector3 meshLowerLeft = objectToAdd.GetAxisAlignedBoundingBox(Matrix4X4.Identity).minXYZ;
			objectToAdd.Matrix *= Matrix4X4.CreateTranslation(-meshLowerLeft + allPlacedMeshBounds.minXYZ);

			// keep moving the item until its in an open slot 
			MoveToOpenPosition(objectToAdd, scene, allPlacedMeshBounds);

			//PlaceMeshGroupOnBed(objectToAdd);
		}

		public static void MoveToOpenPosition(IObject3D itemToMove, IObject3D scene, AxisAlignedBoundingBox allPlacedMeshBounds)
		{
			double xStart = allPlacedMeshBounds.minXYZ.x;
			double yStart = allPlacedMeshBounds.minXYZ.y;

			// find a place to put it that doesn't hit anything
			AxisAlignedBoundingBox itemToMoveBounds = itemToMove.GetAxisAlignedBoundingBox(Matrix4X4.Identity);

			// add in a few mm so that it will not be touching
			itemToMoveBounds.minXYZ -= new Vector3(2, 2, 0);
			itemToMoveBounds.maxXYZ += new Vector3(2, 2, 0);

			Matrix4X4 transform = Matrix4X4.Identity;
			int currentSize = 1;
			bool partPlaced = false;

			while (!partPlaced && itemToMove != null)
			{
				int yStep = 0;
				int xStep = currentSize;
				// check far right edge
				for (yStep = 0; yStep < currentSize; yStep++)
				{
					partPlaced = CheckPosition(scene, itemToMove, itemToMoveBounds, yStep, xStep, ref transform);

					if (partPlaced)
					{
						break;
					}
				}

				if (!partPlaced)
				{
					yStep = currentSize;
					// check top edge 
					for (xStep = 0; xStep < currentSize; xStep++)
					{
						partPlaced = CheckPosition(scene, itemToMove, itemToMoveBounds, yStep, xStep, ref transform);

						if (partPlaced)
						{
							break;
						}
					}

					if (!partPlaced)
					{
						xStep = currentSize;
						// check top right point
						partPlaced = CheckPosition(scene, itemToMove, itemToMoveBounds, yStep, xStep, ref transform);
					}
				}

				currentSize++;
			}

			itemToMove.Matrix *= transform;
		}

		private static bool CheckPosition(IObject3D scene, IObject3D itemToMove, AxisAlignedBoundingBox meshToMoveBounds, int yStep, int xStep, ref Matrix4X4 transform)
		{
			double xStepAmount = 5;
			double yStepAmount = 5;

			Matrix4X4 positionTransform = Matrix4X4.CreateTranslation(xStep * xStepAmount, yStep * yStepAmount, 0);
			Vector3 newPosition = Vector3.Transform(Vector3.Zero, positionTransform);

			transform = Matrix4X4.CreateTranslation(newPosition);

			AxisAlignedBoundingBox testBounds = meshToMoveBounds.NewTransformed(transform);

			foreach (IObject3D meshToTest in scene.Children)
			{
				if (meshToTest != itemToMove)
				{
					AxisAlignedBoundingBox existingMeshBounds = meshToTest.GetAxisAlignedBoundingBox(Matrix4X4.Identity);
					AxisAlignedBoundingBox intersection = AxisAlignedBoundingBox.Intersection(testBounds, existingMeshBounds);
					if (intersection.XSize > 0 && intersection.YSize > 0)
					{
						return false;
					}
				}
			}

			return true;
		}

		static AxisAlignedBoundingBox GetAxisAlignedBoundingBox(MeshGroup meshGroup, Matrix4X4 transform)
		{
			return meshGroup.GetAxisAlignedBoundingBox(transform);
		}

		/*
		public static void CreateITraceableForMeshGroup(List<PlatingMeshGroupData> perMeshGroupInfo, List<MeshGroup> meshGroups, int meshGroupIndex, ReportProgressRatio reportProgress)
		{
			if (meshGroups != null)
			{
				MeshGroup meshGroup = meshGroups[meshGroupIndex];
				perMeshGroupInfo[meshGroupIndex].meshTraceableData.Clear();
				int totalActionCount = 0;
				foreach (Mesh mesh in meshGroup.Meshes)
				{
					totalActionCount += mesh.Faces.Count;
				}
				int currentAction = 0;
				bool needUpdateTitle = true;
				for (int i = 0; i < meshGroup.Meshes.Count; i++)
				{
					Mesh mesh = meshGroup.Meshes[i];
					List<IPrimitive> allPolys = AddTraceDataForMesh(mesh, totalActionCount, ref currentAction, ref needUpdateTitle, reportProgress);

					needUpdateTitle = true;
					if (reportProgress != null)
					{
						bool continueProcessing;
						reportProgress(currentAction / (double)totalActionCount, "Creating Trace Group", out continueProcessing);
					}

					// only allow limited recursion to speed this up building this data
					IPrimitive traceData = BoundingVolumeHierarchy.CreateNewHierachy(allPolys, 0);
					perMeshGroupInfo[meshGroupIndex].meshTraceableData.Add(traceData);
				}
			}
		}

		public static IPrimitive CreateTraceDataForMesh(Mesh mesh)
		{
			int unusedInt = 0;
			bool unusedBool = false;
			List<IPrimitive> allPolys = AddTraceDataForMesh(mesh, 0, ref unusedInt, ref unusedBool, null);
			return BoundingVolumeHierarchy.CreateNewHierachy(allPolys);
		}

		private static List<IPrimitive> AddTraceDataForMesh(Mesh mesh, int totalActionCount, ref int currentAction, ref bool needToUpdateProgressReport, ReportProgressRatio reportProgress)
		{
			bool continueProcessing;

			List<IPrimitive> allPolys = new List<IPrimitive>();
			List<Vector3> positions = new List<Vector3>();

			foreach (Face face in mesh.Faces)
			{
				if (false)
				{
					MeshFaceTraceable triangle = new MeshFaceTraceable(face);
					allPolys.Add(triangle);
				}
				else
				{
					positions.Clear();
					foreach (Vertex vertex in face.Vertices())
					{
						positions.Add(vertex.Position);
					}

					// We should use the tessellator for this if it is greater than 3.
					Vector3 next = positions[1];
					for (int positionIndex = 2; positionIndex < positions.Count; positionIndex++)
					{
						TriangleShape triangle = new TriangleShape(positions[0], next, positions[positionIndex], null);
						allPolys.Add(triangle);
						next = positions[positionIndex];
					}
				}

				if (reportProgress != null)
				{
					if ((currentAction % 256) == 0 || needToUpdateProgressReport)
					{
						reportProgress(currentAction / (double)totalActionCount, "Creating Trace Polygons", out continueProcessing);
						needToUpdateProgressReport = false;
					}
					currentAction++;
				}
			}

			return allPolys;
		}
		*/

		public static Matrix4X4 ApplyAtCenter(IObject3D object3DToApplayTo, Matrix4X4 transformToApply)
		{
			return ApplyAtCenter(object3DToApplayTo.GetAxisAlignedBoundingBox(Matrix4X4.Identity), object3DToApplayTo.Matrix, transformToApply);
		}

		public static Matrix4X4 ApplyAtCenter(AxisAlignedBoundingBox boundsToApplyTo, Matrix4X4 currentTransform, Matrix4X4 transformToApply)
		{
			return ApplyAtPosition(currentTransform, transformToApply, boundsToApplyTo.Center);
		}

		public static Matrix4X4 ApplyAtPosition(Matrix4X4 currentTransform, Matrix4X4 transformToApply, Vector3 postionToApplyAt)
		{
			currentTransform *= Matrix4X4.CreateTranslation(-postionToApplyAt);
			currentTransform *= transformToApply;
			currentTransform *= Matrix4X4.CreateTranslation(postionToApplyAt);
			return currentTransform;
		}
	}
}
