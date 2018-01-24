/*
Copyright (c) 2017, Lars Brubaker, John Lewin
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

using System.Collections.Generic;
using System.Linq;
using MatterHackers.DataConverters3D;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl.PartPreviewWindow.View3D
{
	public class OperationWrapper : Object3D
	{
		public static OperationWrapper Create(IEnumerable<IObject3D> sourceItems)
		{
			var operationWrapper = new OperationWrapper();

			operationWrapper.Children.Modify((list) =>
			{
				list.AddRange(sourceItems);
			});

			// WTF - Child.Parent.Children.Modify?

			// Child.Parent == this
			// Child.Parent.Children.Modify == this.Childrent.Modify
			// **************** IN A LOOP ***********************

			operationWrapper.Children.Modify((list) =>
			{
				// Wrap every first descendant that has a mesh with a new node to store our operation result
				foreach (var item in operationWrapper.VisibleMeshes().ToList())
				{
					var activeTransform = item.Matrix;

					// Remove child and clear transform
					list.Remove(item);
					item.Matrix = Matrix4X4.Identity;

					// Wrap and propagate properties
					var wrapper = new OperationResult()
					{
						Name = item.Name,
						OwnerID = operationWrapper.ID,
						MaterialIndex = item.MaterialIndex,
						OutputType = item.OutputType,
						Color = item.Color,
						Mesh = item.Mesh, // TODO: Unless this clones, we should really be leaving the mesh on the source and the operation should be using the source to generate and assign to this property
						Matrix = activeTransform,
					};
					wrapper.Children.Add(item);

					// Add wrapper
					list.Add(wrapper);
				}
			});

			return operationWrapper;
		}

		public void ResetMeshes()
		{
			this.Mesh = null;

			// Find operation result nodes and reset their meshes
			var participants = this.Descendants().Where(o => o.OwnerID == this.ID).ToList();
			foreach (var item in participants)
			{
				item.Visible = true;
				// set the mesh back to the child mesh
				item.Mesh = item.Children.First().Mesh;
			}
		}
	}
}