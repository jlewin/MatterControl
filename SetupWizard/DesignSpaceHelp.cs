﻿/*
Copyright (c) 2018, Lars Brubaker, John Lewin
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
using System.IO;
using System.Linq;
#if !__ANDROID__
using Markdig.Agg;
#endif
using MatterHackers.Agg;
using MatterHackers.Agg.Image;
using MatterHackers.Agg.UI;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.CustomWidgets;
using MatterHackers.MatterControl.PartPreviewWindow;
using MatterHackers.VectorMath;
using Newtonsoft.Json;

namespace MatterHackers.MatterControl
{
	public class GuideAsset
	{
		/// <summary>
		/// The name that is in the navigation list with categories
		/// </summary>
		public string MenuName;

		/// <summary>
		/// The markdown to show
		/// </summary>
		public string Description;

		/// <summary>
		/// This is the immutable key assigned to this guide. It can
		/// be used to navigate to this guide while opening the control
		/// </summary>
		public string Key;
	}

	public class HelpContainer
	{
		public List<HelpContainer> Containers { get; set; } = new List<HelpContainer>();
		public List<GuideAsset> Items { get; set; } = new List<GuideAsset>();
		public string Name { get; set; }
	}

	public class DesignSpaceGuide : DialogPage
	{
		private Dictionary<string, TreeNode> nodesByID;
		private TreeView treeView;

		public DesignSpaceGuide(string guideKey = null)
			: base("Close".Localize())
		{
			WindowSize = new Vector2(800, 600);

			this.WindowTitle = "MatterControl " + "Help".Localize();
			this.HeaderText = "How to succeed with MatterControl".Localize();
			this.ChildBorderColor = theme.GetBorderColor(75);

			var tabControl = new SimpleTabs(theme, new GuiWidget())
			{
				HAnchor = HAnchor.Stretch,
				VAnchor = VAnchor.Stretch
			};
			tabControl.TabBar.BackgroundColor = theme.TabBarBackground;

			contentRow.AddChild(tabControl);
			contentRow.Padding = 0;

			// add the mouse commands
			var mouseControls = new FlowLayoutWidget()
			{
				HAnchor = HAnchor.Fit | HAnchor.Center,
				Padding = theme.DefaultContainerPadding
			};

			var mouseTab = new ToolTab("Mouse".Localize(), tabControl, mouseControls, theme, hasClose: false)
			{
				// this can be used to navigate to this tab on construction
				Name = "Mouse Tab"
			};
			tabControl.AddTab(mouseTab);

			var mouseKeys = new FlowLayoutWidget(FlowDirection.TopToBottom);
			mouseControls.AddChild(mouseKeys);

			var mouseActions = new FlowLayoutWidget(FlowDirection.TopToBottom)
			{
				Border = new BorderDouble(1, 0, 0, 0),
				BorderColor = this.ChildBorderColor
			};
			mouseControls.AddChild(mouseActions);

			var mouseKeyActions = new List<(string key, string action)>(new(string, string)[]
			{
				("left click".Localize(), "Make Selection".Localize()),
				("left click".Localize() + " + shift","Add to Selection".Localize()),
				("left click".Localize() + " + ctrl","Toggle Selection".Localize()),
				("left drag".Localize(), "Rubber Band Selection".Localize()),
				("left drag".Localize(), "Move Part".Localize()),
				("left drag".Localize() + " + shift", "Move Part Constrained".Localize()),
				("left drag".Localize() + " + shift + ctrl", "Pan View".Localize()),
				("left drag".Localize() + " + ctrl","Rotate View".Localize()),
				("middle drag".Localize(), "Pan View".Localize()),
				("right drag".Localize(), "Rotate View".Localize()),
				("wheel".Localize(), "Zoom".Localize())
			});

			AddContent(mouseKeys, "Mouse".Localize(), true, true);
			AddContent(mouseActions, "Action".Localize(), false, true);

			foreach (var keyAction in mouseKeyActions)
			{
				AddContent(mouseKeys, keyAction.key, true, false);
				AddContent(mouseActions, keyAction.action, false, false);
			}

			// center the vertical bar in the view by adding margin to the small side
			var left = Math.Max(0, mouseActions.Width - mouseKeys.Width);
			var right = Math.Max(0, mouseKeys.Width - mouseActions.Width);
			mouseControls.Margin = new BorderDouble(left, 0, right, 0);

			// now add the keyboard commands
			var shortcutKeys = new FlowLayoutWidget()
			{
				HAnchor = HAnchor.Fit | HAnchor.Center,
				Padding = theme.DefaultContainerPadding
			};

			var keyboardTab = new ToolTab("Keys".Localize(), tabControl, shortcutKeys, theme, hasClose: false)
			{
				// this can be used to navigate to this tab on construction
				Name = "Keys Tab"
			};
			tabControl.AddTab(keyboardTab);

			var keys = new FlowLayoutWidget(FlowDirection.TopToBottom);
			shortcutKeys.AddChild(keys);

			var actions = new FlowLayoutWidget(FlowDirection.TopToBottom)
			{
				Border = new BorderDouble(1, 0, 0, 0),
				BorderColor = this.ChildBorderColor
			};
			shortcutKeys.AddChild(actions);

			tabControl.TabBar.Padding = theme.ToolbarPadding.Clone(left: 2, bottom: 0);

			var keyActions = new List<(string key, string action)>(new(string, string)[]
			{
				("F1","Show Help".Localize()),
				("ctrl + +","Zoom in".Localize()),
				("ctrl + -","Zoom out".Localize()),
				("← → ↑ ↓","Rotate".Localize()),
				("shift + ← → ↑ ↓","Pan".Localize()),
				//("f","Zoom to fit".Localize()),
				("w","Zoom to window".Localize()),
				("ctrl + z","Undo".Localize()),
				("ctrl + y","Redo".Localize()),
				("delete","Delete selection".Localize()),
				("space bar","Clear selection".Localize()),
				("esc","Cancel command".Localize()),
				//("enter","Accept command".Localize())
			});

			AddContent(keys, "Keys".Localize(), true, true);
			AddContent(actions, "Action".Localize(), false, true);

			foreach (var keyAction in keyActions)
			{
				AddContent(keys, keyAction.key, true, false);
				AddContent(actions, keyAction.action, false, false);
			}

			// center the vertical bar in the view by adding margin to the small side
			left = Math.Max(0, actions.Width - keys.Width);
			right = Math.Max(0, keys.Width - actions.Width);
			shortcutKeys.Margin = new BorderDouble(left, 0, right, 0);

			var guideSectionContainer = new FlowLayoutWidget(FlowDirection.TopToBottom)
			{
				HAnchor = HAnchor.Stretch,
				VAnchor = VAnchor.Stretch,
				Padding = theme.DefaultContainerPadding
			};

			var guideTab = new ToolTab("Guides".Localize(), tabControl, guideSectionContainer, theme, hasClose: false)
			{
				// this can be used to navigate to this tab on construction
				Name = "Guides Tab"
			};
			tabControl.AddTab(guideTab);

			AddGuides(guideSectionContainer);

			// If guideKey is empty, switch to first tab
			if (string.IsNullOrEmpty(guideKey))
			{
				tabControl.SelectedTabIndex = 0;
			}
			else
			{
				// Otherwise switch to guides tab and select the target item
				tabControl.SelectedTabIndex = tabControl.GetTabIndex(guideTab);

				// TODO: Find the target TreeNode by ID and select
				if (nodesByID.TryGetValue(guideKey, out TreeNode guideNode))
				{
					treeView.SelectedNode = guideNode;
				}
			}
		}

		private void AddGuides(FlowLayoutWidget guideContainer)
		{
			nodesByID = new Dictionary<string, TreeNode>();

			var sequence = new ImageSequence()
			{
				FramesPerSecond = 3,
			};

			sequence.AddImage(new ImageBuffer(1, 1));

			var rightPanel = new FlowLayoutWidget(FlowDirection.TopToBottom)
			{
				HAnchor = HAnchor.Stretch,
				VAnchor = VAnchor.Stretch,
				Padding = theme.DefaultContainerPadding
			};

#if __ANDROID__
			var description = new GuiWidget();
#else
			var description = new MarkdownWidget(new Uri("https://raw.githubusercontent.com/lunet-io/markdig/master/"))
			{
				Margin = new BorderDouble(10, 4, 10, 10),
			};
#endif
			rightPanel.AddChild(description);

			treeView = new TreeView(theme)
			{
				HAnchor = HAnchor.Stretch,
				VAnchor = VAnchor.Fit | VAnchor.Top,
			};
			treeView.AfterSelect += (s, e) =>
			{
				if (treeView.SelectedNode.Tag is GuideAsset guide)
				{
#if !__ANDROID__
					description.Markdown = guide.Description;
#endif
				}
			};

			var rootNode = new TreeNode(useIcon: false)
			{
				Text = "Help",
				TextColor = theme.Colors.PrimaryTextColor,
				PointSize = theme.DefaultFontSize,
				TreeView = treeView,
			};

			treeView.Load += (s, e) =>
			{
				rootNode.Expanded = true;

				if (treeView.SelectedNode == null)
				{
					treeView.SelectedNode = rootNode.Nodes.FirstOrDefault();
				}
			};

			TreeNode firstNode = null;
			double maxMenuItemWidth = 0;


			//foreach (var guide in ApplicationController.Instance.FeatureGuides.OrderBy(g => g.MenuName))
			//{
			//	var treeNode = new TreeNode(useIcon: false)
			//	{
			//		Text = guide.MenuName,
			//		Tag = guide,
			//		TextColor = theme.Colors.PrimaryTextColor,
			//		PointSize = theme.DefaultFontSize,
			//	};

			//	maxMenuItemWidth = Math.Max(maxMenuItemWidth, treeNode.Width);

			//	if (firstNode == null)
			//	{
			//		firstNode = treeNode;
			//	}

			//	nodesByID.Add(guide.MenuName, treeNode);

			//	rootNode.Nodes.Add(treeNode);
			//}

			//treeView.AddChild(rootNode);

			var rootContainer = JsonConvert.DeserializeObject<HelpContainer>(File.ReadAllText(@"C:\Temp\guides2.json"));

			treeView.AddChild(ProcTree(rootContainer));

			var splitter = new Splitter()
			{
				HAnchor = HAnchor.Stretch,
				VAnchor = VAnchor.Stretch,
				SplitterBackground = theme.SplitterBackground
			};
			splitter.SplitterDistance = maxMenuItemWidth + 30;
			splitter.Panel1.AddChild(treeView);
			splitter.Panel2.AddChild(rightPanel);
			guideContainer.AddChild(splitter);
		}

		private TreeNode ProcTree(HelpContainer container)
		{
			var treeNode = new TreeNode(false)
			{
				Text = container.Name,
			};

			foreach(var child in container.Containers)
			{
				treeNode.Nodes.Add(ProcTree(child));
			}

			foreach (var item in container.Items)
			{
				treeNode.Nodes.Add(new TreeNode()
				{
					Text = item.MenuName
				});
			}

			return treeNode;
		}

		public Color ChildBorderColor { get; private set; }

		private void AddContent(GuiWidget column, string text, bool left, bool bold)
		{
			var container = new GuiWidget()
			{
				HAnchor = HAnchor.Fit | (left ? HAnchor.Right: HAnchor.Left),
				VAnchor = VAnchor.Fit
			};
			var content = new TextWidget(text, bold: bold, textColor: theme.Colors.PrimaryTextColor, pointSize: theme.DefaultFontSize)
			{
				Margin = (left ? new BorderDouble(5, 3, 10, 3) : new BorderDouble(10, 3, 5, 3))
			};
			container.AddChild(content);

			column.AddChild(container);
			column.AddChild(new GuiWidget()
			{
				HAnchor = HAnchor.Stretch,
				Border = new BorderDouble(0, 1, 0, 0),
				BorderColor = this.ChildBorderColor,
			});
		}
	}
}