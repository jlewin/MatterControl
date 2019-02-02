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
using System.Drawing;
using MatterHackers.Agg.UI;
using MatterHackers.DataConverters3D;
using MatterHackers.RenderOpenGl;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl.PartPreviewWindow
{
	public class NormalsDrawable : IDrawableItem
	{
		private ISceneContext sceneContext;
		private InteractiveScene scene;

		public NormalsDrawable(ISceneContext sceneContext)
		{
			this.sceneContext = sceneContext;
			this.scene = sceneContext.Scene;
		}

		public void Draw(GuiWidget sender, IObject3D item, DrawEventArgs e, Matrix4X4 itemMaxtrix, WorldView world)
		{
			throw new NotImplementedException();
#if false
			if (item.Mesh?.Faces.Count <= 0)
			{
				return;
			}

			var frustum = world.GetClippingFrustum();

			var mesh = item.Mesh;

			foreach (var face in mesh.Faces)
			{
				int vertexCount = 0;
				Vector3 faceCenter = Vector3.Zero;
				foreach (var v in  new[] { face.v0, face.v1, face.v2 })
				{
					var vertex = mesh.Vertices[v];

					faceCenter += vertex.Position;
					vertexCount++;
				}
				faceCenter /= vertexCount;

				var matrix = item.WorldMatrix();

				var transformed1 = Vector3Ex.Transform(faceCenter, matrix);
				var normal = Vector3Ex.TransformNormal(face.Normal, matrix).GetNormal();

				world.Render3DLineNoPrep(frustum, transformed1, transformed1 + normal, Color.Red, 2);
			}
#endif
		}
	}
}