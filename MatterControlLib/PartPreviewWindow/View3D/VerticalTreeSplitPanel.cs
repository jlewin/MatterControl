/*
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
using MatterHackers.Agg;
using MatterHackers.Agg.UI;
using MatterHackers.MatterControl.CustomWidgets;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl.PartPreviewWindow
{
	public class VerticalTreeSplitPanel : VerticalResizeContainer
	{
		public VerticalTreeSplitPanel(ThemeConfig theme, GrabBarSide grabBarSide, double panel1Ratio)
			: base(theme, grabBarSide)
		{
			this.VAnchor = VAnchor.Stretch;
			this.HAnchor = HAnchor.Absolute;
			this.BackgroundColor = theme.InteractionLayerOverlayColor;
			this.SplitterBarColor = theme.SplitterBackground;
			this.SplitterWidth = theme.SplitterWidth;
			this.MinimumSize = new Vector2(theme.SplitterWidth, 0);

			var splitter = this.Splitter = new Splitter()
			{
				Orientation = Orientation.Horizontal,
				Panel1Ratio = panel1Ratio,
				SplitterSize = theme.SplitterWidth,
				SplitterBackground = theme.SplitterBackground
			};
			splitter.Panel1.MinimumSize = new Vector2(0, 60);
			splitter.Panel2.MinimumSize = new Vector2(0, 60);
			this.AddChild(splitter);

			var treeView = this.TreeView = new TreeView(theme)
			{
				Margin = new BorderDouble(left: theme.DefaultContainerPadding + 12),
			};
			treeView.ScrollArea.ChildAdded += (s, e) =>
			{
				if (e is GuiWidgetEventArgs childEventArgs
					&& childEventArgs.Child is TreeNode treeNode)
				{
					treeNode.AlwaysExpandable = true;
				}
			};
			treeView.ScrollArea.HAnchor = HAnchor.Stretch;

			this.TreeNodeContainer = new FlowLayoutWidget(FlowDirection.TopToBottom)
			{
				HAnchor = HAnchor.Stretch,
				VAnchor = VAnchor.Fit,
				Margin = new BorderDouble(12, 3)
			};
			treeView.AddChild(TreeNodeContainer);

			this.TreePanel = new FlowLayoutWidget(FlowDirection.TopToBottom)
			{
				HAnchor = HAnchor.Stretch,
				VAnchor = VAnchor.Stretch
			};

			this.TreePanel.AddChild(treeView);

			this.Splitter.Panel1.AddChild(this.TreePanel);

			splitter.Panel2.AddChild(
				this.ContentPanel = new FlowLayoutWidget(FlowDirection.TopToBottom)
				{
					HAnchor = HAnchor.Stretch,
					VAnchor = VAnchor.Stretch
				});
		}

		public TreeView TreeView { get; }

		public FlowLayoutWidget TreePanel { get; }

		public FlowLayoutWidget TreeNodeContainer { get; }

		public Splitter Splitter { get; }

		public FlowLayoutWidget ContentPanel { get; }
	}
}
