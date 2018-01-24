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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MatterHackers.Agg;
using MatterHackers.Agg.UI;
using MatterHackers.DataConverters3D;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.CustomWidgets;
using MatterHackers.PolygonMesh;

namespace MatterHackers.MatterControl.PartPreviewWindow.View3D
{
	public class SubtractEditor : IObject3DEditor
	{
		private OperationWrapper group;
		private View3DWidget view3DWidget;
		public string Name => "Subtract";

		public bool Unlocked { get; } = true;

		public GuiWidget Create(IObject3D group, View3DWidget view3DWidget, ThemeConfig theme)
		{
			this.view3DWidget = view3DWidget;
			this.group = group as OperationWrapper;

			var mainContainer = new FlowLayoutWidget(FlowDirection.TopToBottom);

			if (group is OperationWrapper)
			{
				AddSubtractSelector(view3DWidget, mainContainer, theme);
			}

			return mainContainer;
		}

		public IEnumerable<Type> SupportedTypes() => new Type[]
		{
			typeof(OperationWrapper),
		};

		private void AddSubtractSelector(View3DWidget view3DWidget, FlowLayoutWidget tabContainer, ThemeConfig theme)
		{
			var children = group.Children.ToList();

			tabContainer.AddChild(new TextWidget("Set Subtract")
			{
				TextColor = ActiveTheme.Instance.PrimaryTextColor,
				HAnchor = HAnchor.Left,
				AutoExpandBoundsToText = true,
			});

			// create this early so we can use enable disable it on button changed state
			var updateButton = theme.ButtonFactory.Generate("Update".Localize());
			updateButton.Margin = new BorderDouble(5);
			updateButton.HAnchor = HAnchor.Right;
			updateButton.Click += (s, e) =>
			{
				// make sure the mesh on the group is not visible
				group.ResetMeshes();
				updateButton.Enabled = false;
				ProcessBooleans(group);
			};

			List<GuiWidget> radioSiblings = new List<GuiWidget>();
			for (int i = 0; i < children.Count; i++)
			{
				var itemIndex = i;
				var item = children[itemIndex];
				FlowLayoutWidget rowContainer = new FlowLayoutWidget();

				GuiWidget selectWidget;
				if (children.Count == 2)
				{
					var radioButton = new RadioButton(string.IsNullOrWhiteSpace(item.Name) ? $"{itemIndex}" : $"{item.Name}")
					{
						Checked = item.OutputType == PrintOutputTypes.Hole,
						TextColor = ActiveTheme.Instance.PrimaryTextColor
					};
					radioSiblings.Add(radioButton);
					radioButton.SiblingRadioButtonList = radioSiblings;
					selectWidget = radioButton;
				}
				else
				{
					selectWidget = new CheckBox(string.IsNullOrWhiteSpace(item.Name) ? $"{itemIndex}" : $"{item.Name}")
					{
						Checked = item.OutputType == PrintOutputTypes.Hole,
						TextColor = ActiveTheme.Instance.PrimaryTextColor
					};
				}
				rowContainer.AddChild(selectWidget);
				ICheckbox checkBox = selectWidget as ICheckbox;

				checkBox.CheckedStateChanged += (s, e) =>
				{
					// make sure the mesh on the group is not visible
					group.ResetMeshes();

					var wrappedItems = item.Descendants().Where((obj) => obj.OwnerID == group.ID).ToList();
					foreach (var meshWrapper in wrappedItems)
					{
						// and set the output type for this checkbox
						meshWrapper.OutputType = checkBox.Checked ? PrintOutputTypes.Hole : PrintOutputTypes.Solid;
					}

					var allItems = group.Descendants().Where((obj) => obj.OwnerID == group.ID).ToList();
					int holeCount = allItems.Where((o) => o.OutputType == PrintOutputTypes.Hole).Count();
					int solidCount = allItems.Where((o) => o.OutputType != PrintOutputTypes.Hole).Count();
					updateButton.Enabled = allItems.Count != holeCount && allItems.Count != solidCount;
				};

				tabContainer.AddChild(rowContainer);
			}

			bool operationApplied = group.Descendants()
				.Where((obj) => obj.OwnerID == group.ID)
				.Where((objId) => objId.Mesh != objId.Children.First().Mesh).Any();

			bool selectionHasBeenMade = group.Descendants()
				.Where((obj) => obj.OwnerID == group.ID && obj.OutputType == PrintOutputTypes.Hole)
				.Any();

			if (!operationApplied && !selectionHasBeenMade)
			{
				// select the last item
				if (tabContainer.Descendants().Where((d) => d is ICheckbox).Last() is ICheckbox lastCheckBox)
				{
					lastCheckBox.Checked = true;
				}
			}
			else
			{
				updateButton.Enabled = !operationApplied;
			}

			// add this last so it is at the bottom
			tabContainer.AddChild(updateButton);
		}

		private void ProcessBooleans(IObject3D group)
		{
			// spin up a task to remove holes from the objects in the group
			ApplicationController.Instance.Tasks.Execute((reporter, cancellationToken) =>
			{
				var progressStatus = new ProgressStatus()
				{
					Status = "Processing Booleans"
				};

				reporter.Report(progressStatus);

				var participants = group.Descendants().Where(o => o.OwnerID == group.ID).ToList();
				var removeObjects = participants.Where((obj) => obj.OutputType == PrintOutputTypes.Hole).ToList();
				var keepObjects = participants.Where((obj) => obj.OutputType != PrintOutputTypes.Hole).ToList();

				Subtract(keepObjects, removeObjects, cancellationToken, reporter);
				return Task.CompletedTask;
			});
		}

		public static void Subtract(List<IObject3D> keepObjects, List<IObject3D> removeObjects)
		{
			Subtract(keepObjects, removeObjects, CancellationToken.None, null);
		}

		public static void Subtract(List<IObject3D> keepObjects, List<IObject3D> removeObjects, CancellationToken cancellationToken, IProgress<ProgressStatus> reporter)
		{
			if (removeObjects.Any()
				&& keepObjects.Any())
			{
				var totalOperations = removeObjects.Count * keepObjects.Count;
				double amountPerOperation = 1.0 / totalOperations;
				double percentCompleted = 0;

				ProgressStatus progressStatus = new ProgressStatus();
 				foreach (var remove in removeObjects)
				{
					foreach (var keep in keepObjects)
					{
						progressStatus.Status = "Copy Remove";
						reporter?.Report(progressStatus);
						var transformedRemove = Mesh.Copy(remove.Mesh, CancellationToken.None);
						transformedRemove.Transform(remove.WorldMatrix(null));

						progressStatus.Status = "Copy Keep";
						reporter?.Report(progressStatus);
						var transformedKeep = Mesh.Copy(keep.Mesh, CancellationToken.None);
						transformedKeep.Transform(keep.WorldMatrix(null));

						progressStatus.Status = "Do CSG";
						reporter?.Report(progressStatus);
						transformedKeep = PolygonMesh.Csg.CsgOperations.Subtract(transformedKeep, transformedRemove, (status, progress0To1) =>
						{
							// Abort if flagged
							cancellationToken.ThrowIfCancellationRequested();

							progressStatus.Status = status;
							progressStatus.Progress0To1 = percentCompleted + amountPerOperation * progress0To1;
							reporter?.Report(progressStatus);
						}, cancellationToken);
						var inverse = keep.WorldMatrix(null);
						inverse.Invert();
						transformedKeep.Transform(inverse);

						keep.Mesh = transformedKeep;
						keep.Invalidate();

						percentCompleted += amountPerOperation;
						progressStatus.Progress0To1 = percentCompleted;
						reporter?.Report(progressStatus);
					}

					remove.Visible = false;
				}
			}
		}
	}
}