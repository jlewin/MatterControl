﻿/*
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
using MatterHackers.Agg.UI;
using MatterHackers.DataConverters3D;

namespace MatterHackers.MatterControl.PartPreviewWindow
{
	public class MakeSupport : IUndoRedoCommand
	{
		// Map items and their original OutputType
		private List<(IObject3D object3D, PrintOutputTypes originalOutputType)> sourceItems = new List<(IObject3D item, PrintOutputTypes originalOutputType)>();

		public MakeSupport(IObject3D selectedItem)
		{
			// Ensure enumerable
			var source = (selectedItem is SelectionGroup) ? selectedItem.Children.OfType<IObject3D>() :  new List<IObject3D> { selectedItem };

			// Store object3D and original OutputType
			foreach (var item in source)
			{
				sourceItems.Add((item, item.OutputType));
			}
		}

		void IUndoRedoCommand.Do()
		{
			// Force Support
			foreach(var item in sourceItems)
			{
				item.object3D.OutputType = PrintOutputTypes.Support;
			}
		}

		void IUndoRedoCommand.Undo()
		{
			// Restore original
			foreach(var item in sourceItems)
			{
				item.object3D.OutputType = item.originalOutputType;
			}
		}
	}
}
