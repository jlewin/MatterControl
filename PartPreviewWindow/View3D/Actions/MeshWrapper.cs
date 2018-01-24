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

using MatterHackers.DataConverters3D;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl.PartPreviewWindow.View3D
{
	// Isn't this just clone, wrap, reset source Matrix to identity?

	// alt name - OperationResult

	/// <summary>
	/// The goal of MeshWrapper is to provide a mutated version of a source item by some operation. To do so we wrap and clone all
	/// properties of the source item and reset the source matrix to Identity, given that it now exists on the wrapping parent.
	/// </summary>
	public class OperationResult : Object3D
	{
		public OperationResult()
		{
		}

		public OperationResult(IObject3D child, string ownerId)
		{
			Children.Add(child);

			this.Name = child.Name;
			this.OwnerID = ownerId;
			this.MaterialIndex = child.MaterialIndex;
			this.OutputType = child.OutputType;
			this.Color = child.Color;

			// Unless this clones, we should really be leaving the mesh on the source and the operation should be using the source to generate and assign to this property
			this.Mesh = child.Mesh;

			this.Matrix = child.Matrix;
			child.Matrix = Matrix4X4.Identity;
		}
	}
}
