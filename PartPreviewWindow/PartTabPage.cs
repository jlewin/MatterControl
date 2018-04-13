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
using MatterHackers.Agg.UI;
using MatterHackers.MeshVisualizer;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl.PartPreviewWindow
{
	public class PartTabPage : TabPage
	{
		internal View3DWidget view3DWidget;

		protected ViewControls3D viewControls3D;

		internal BedConfig sceneContext;
		protected ThemeConfig theme;

		protected GuiWidget view3DContainer;
		protected FlowLayoutWidget topToBottom;
		protected FlowLayoutWidget leftToRight;

		public PartTabPage(PrinterConfig printer, BedConfig sceneContext, ThemeConfig theme, string tabTitle)
			: base (tabTitle)
		{
			this.sceneContext = sceneContext;
			this.theme = theme;
			this.BackgroundColor = theme.ActiveTabColor;
			this.Padding = 0;

			bool isPrinterType = this is PrinterTabPage;

			viewControls3D = new ViewControls3D(sceneContext, theme, sceneContext.Scene.UndoBuffer, isPrinterType)
			{
				//BackgroundColor = new Color(0, 0, 0, theme.OverlayAlpha),
				VAnchor = VAnchor.Top | VAnchor.Fit,
				HAnchor = HAnchor.Left | HAnchor.Stretch,
				Visible = true,
			};
			viewControls3D.ResetView += (sender, e) =>
			{
				if (view3DWidget.Visible)
				{
					this.view3DWidget.ResetView();
				}
			};
			viewControls3D.ExtendOverflowMenu = this.GetViewControls3DOverflowMenu;
			viewControls3D.OverflowButton.Name = "View3D Overflow Menu";

			// The 3D model view
			view3DWidget = new View3DWidget(
				printer,
				sceneContext,
				View3DWidget.AutoRotate.Disabled,
				viewControls3D,
				theme,
				this,
				editorType: (isPrinterType) ? MeshViewerWidget.EditorType.Printer : MeshViewerWidget.EditorType.Part);

			viewControls3D.SetView3DWidget(view3DWidget);

			this.AddChild(topToBottom = new FlowLayoutWidget(FlowDirection.TopToBottom)
			{
				HAnchor = HAnchor.Stretch,
				VAnchor = VAnchor.Stretch
			});

			topToBottom.AddChild(leftToRight = new FlowLayoutWidget()
			{
				Name = "View3DContainerParent",
				HAnchor = HAnchor.Stretch,
				VAnchor = VAnchor.Stretch
			});

			view3DContainer = new GuiWidget()
			{
				HAnchor = HAnchor.Stretch,
				VAnchor = VAnchor.Stretch
			};

			var toolbarAndView3DWidget = new FlowLayoutWidget(FlowDirection.TopToBottom)
			{
				HAnchor = HAnchor.Stretch,
				VAnchor = VAnchor.Stretch
			};
			toolbarAndView3DWidget.AddChild(viewControls3D);
			toolbarAndView3DWidget.AddChild(view3DWidget);
			view3DContainer.AddChild(toolbarAndView3DWidget);

			leftToRight.AddChild(view3DContainer);

			view3DWidget.BackgroundColor = ActiveTheme.Instance.TertiaryBackgroundColor;

			if (sceneContext.World.RotationMatrix == Matrix4X4.Identity)
			{
				this.view3DWidget.ResetView();
			}

			this.AnchorAll();
		}

		public override void OnFocusChanged(EventArgs e)
		{
			base.OnFocusChanged(e);
			view3DWidget.Focus();
		}

		protected virtual void GetViewControls3DOverflowMenu(PopupMenu popupMenu)
		{
			view3DWidget.ShowOverflowMenu(popupMenu);
		}
	}
}
