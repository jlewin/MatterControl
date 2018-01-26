﻿/*
Copyright (c) 2017, John Lewin
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
using System.Threading.Tasks;
using MatterHackers.DataConverters3D;

namespace MatterHackers.MatterControl.Library
{
	public class GraphOperation
	{
		public string Title { get; set; }
		public List<Type> MappedTypes { get; set; }
		public Func<IObject3D, Task> Operation { get; set; }
		public Func<IObject3D, bool> IsEnabled { get; set; }
	}

	public class GraphConfig
	{
		private List<GraphOperation> _operations = new List<GraphOperation>();

		public IEnumerable<GraphOperation> Operations => _operations;

		public void RegisterOperation(Type type, string title, Func<IObject3D, Task> operation, Func<IObject3D, bool> isEnabled)
		{
			_operations.Add(new GraphOperation()
			{
				MappedTypes = new List<Type> { type },
				Title = title,
				Operation = operation,
				IsEnabled = isEnabled
			});
		}
	}
}
