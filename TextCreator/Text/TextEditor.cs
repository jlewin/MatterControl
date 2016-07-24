﻿/*
Copyright (c) 2016, Lars Brubaker, John Lewin
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

using MatterHackers.Agg;
using MatterHackers.Agg.UI;
using MatterHackers.DataConverters3D;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.PartPreviewWindow;
using MatterHackers.PolygonMesh;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace MatterHackers.MatterControl.Plugins.TextCreator
{
	public class TextEditor : IObject3DEditor
	{
		private MHTextEditWidget textToAddWidget;
		private SolidSlider spacingScrollBar;
		private CheckBox createUnderline;

		private TextGenerator textGenerator;
		private View3DWidget view3DWidget;

		private TextObject injectedItem = null;

		public IEnumerable<Type> SupportedTypes()
		{
			return new Type[] { typeof(TextObject) };
		}

		public GuiWidget Create(IObject3D item, View3DWidget parentView3D)
		{
			injectedItem = parentView3D.Scene?.SelectedItem as TextObject;

			textGenerator = new TextGenerator();
			this.view3DWidget = parentView3D;

			FlowLayoutWidget mainContainer = new FlowLayoutWidget(FlowDirection.TopToBottom);

			FlowLayoutWidget tabContainer = new FlowLayoutWidget(FlowDirection.TopToBottom)
			{
				HAnchor = HAnchor.AbsolutePosition,
				Visible = true,
				Width = view3DWidget.WhiteButtonFactory.FixedWidth
			};
			mainContainer.AddChild(tabContainer);

			textToAddWidget = new MHTextEditWidget("", messageWhenEmptyAndNotSelected: "Text".Localize())
			{
				HAnchor = HAnchor.ParentLeftRight,
				Margin = new BorderDouble(5),
				Text = injectedItem.Text,
				Width = 50
			};
			textToAddWidget.ActualTextEditWidget.EnterPressed += (s, e) => RebuildText(textToAddWidget.Text);
			tabContainer.AddChild(textToAddWidget);

			spacingScrollBar = PartPreview3DWidget.InsertUiForSlider(tabContainer, "Spacing:".Localize(), .5, 1);
			spacingScrollBar.ValueChanged += (sender, e) =>
			{
				if (injectedItem != null)
				{
					textGenerator.SetWordSpacing(injectedItem, spacingScrollBar.Value, rebuildUnderline: true);
				}
			};

			createUnderline = new CheckBox(new CheckBoxViewText("Underline".Localize(), textColor: ActiveTheme.Instance.PrimaryTextColor))
			{
				Checked = true,
				Margin = new BorderDouble(10, 5),
				HAnchor = HAnchor.ParentLeft
			};
			createUnderline.CheckedStateChanged += CreateUnderline_CheckedStateChanged;
			tabContainer.AddChild(createUnderline);

			Button updateButton = view3DWidget.textImageButtonFactory.Generate("Update".Localize());
			updateButton.Margin = new BorderDouble(5);
			updateButton.HAnchor = HAnchor.ParentRight;
			updateButton.Click += (s, e) => RebuildText(textToAddWidget.Text);
			tabContainer.AddChild(updateButton);

			return mainContainer;
		}

		public string Name { get; } = "Text";

		private void ResetWordLayoutSettings()
		{
			spacingScrollBar.Value = 1;
			textGenerator.ResetSettings();
		}

		private async void RebuildText(string text)
		{
			injectedItem.Text = text;

			// Clear prior selection

			if (text.Length <= 0)
			{
				return;
			}

			ResetWordLayoutSettings();

			//view3DWidget.processingProgressControl.ProcessType = "Inserting Text".Localize();
			//view3DWidget.processingProgressControl.Visible = true;
			//view3DWidget.processingProgressControl.PercentComplete = 0;

			view3DWidget.LockEditControls();

			var generatedItem = await Task.Run(() =>
			{
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

				return textGenerator.CreateText(
					text,
					1,
					.25,
					spacingScrollBar.Value,
					createUnderline.Checked);
			});

			view3DWidget.Scene.ModifyChildren(children =>
			{
				var item = children.Find(child => child == injectedItem);
				item.Children.Clear();
				item.Children.AddRange(generatedItem.Children);
			});

			//PlatingHelper.MoveToOpenPosition(injectedItem, view3DWidget.Scene);

			//view3DWidget.InsertNewItem(injectedItem);

			view3DWidget.UnlockEditControls();
		}

		private void CreateUnderline_CheckedStateChanged(object sender, EventArgs e)
		{
			ModifyInjectedItem(workItem =>
			{
				// Change the contents, adding or removing the underline
				textGenerator.EnableUnderline(workItem, enableUnderline: createUnderline.Checked);
			});
		}

		private void ModifyInjectedItem(Action<IObject3D> modifier)
		{
			// Create a copy of the injected group
			IObject3D workItem = injectedItem.Clone();

			// Invoke the passed in action
			modifier(workItem);

			// Modify the scene graph, swapping in the modified item
			view3DWidget.Scene.ModifyChildren(children =>
			{
				children.Remove(injectedItem);
				children.Add(workItem);
			});

			// Update the injected item and the scene selection
			injectedItem = workItem as TextObject;
			view3DWidget.Scene.Select(injectedItem);
		}

		internal void SetInitialFocus()
		{
			textToAddWidget.Focus();
		}
	}
}