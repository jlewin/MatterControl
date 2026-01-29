/*
Copyright (c) 2022, John Lewin, Lars Brubaker
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
using MatterHackers.Agg.Image;
using MatterHackers.Agg.Platform;
using MatterHackers.Agg.UI;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.CustomWidgets;
using MatterHackers.MatterControl.Library.Widgets.HardwarePage;
using MatterHackers.MatterControl.PartPreviewWindow;
using MatterHackers.MatterControl.SlicerConfiguration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MatterHackers.MatterControl.Library.Widgets
{
    public class AddMaterialWidget : SearchableTreePanel
    {
        public class MaterialInfo
        {
            public string Path { get; set; }

            public string Sku { get; set; }

            public string Name { get; set; }
        }

        private Action<bool> nextButtonEnabled;
        private FlowLayoutWidget materialInfo;
        public MaterialInfo SelectedMaterial { get; private set; }

        public AddMaterialWidget(GuiWidget nextButton, ThemeConfig theme, Action<bool> nextButtonEnabled)
            : base(theme)
        {
            this.nextButtonEnabled = nextButtonEnabled;
            this.Name = "AddPrinterWidget";

            horizontalSplitter.Panel2.Padding = theme.DefaultContainerPadding;

            TreeView.AfterSelect += this.TreeView_AfterSelect;

            TreeView.NodeMouseDoubleClick += (s, e) =>
            {
                if (e is MouseEventArgs mouseEvent
                    && mouseEvent.Button == MouseButtons.Left
                        && mouseEvent.Clicks == 2
                        && TreeView?.SelectedNode is TreeNode treeNode)
                {
                    nextButton.InvokeClick();
                }
            };

            UiThread.RunOnIdle(() =>
            {
                foreach (var rootDirectory in Directory.EnumerateDirectories(Path.Combine(StaticData.RootPath, "Materials")).OrderBy(f => Path.GetFileName(f)))
                {
                    var rootNode = this.CreateTreeNode(rootDirectory);
                    rootNode.Expandable = true;
                    rootNode.TreeView = TreeView;
                    contentPanel.AddChild(rootNode);
                }

                this.TreeLoaded = true;
            });

            var container = new FlowLayoutWidget(FlowDirection.TopToBottom)
            {
                HAnchor = HAnchor.Stretch,
                Margin = new BorderDouble(theme.DefaultContainerPadding).Clone(top: 0)
            };

            var panel2Column = new FlowLayoutWidget(FlowDirection.TopToBottom)
            {
                HAnchor = HAnchor.Stretch,
                VAnchor = VAnchor.Stretch
            };

            materialInfo = new FlowLayoutWidget(FlowDirection.TopToBottom)
            {
                HAnchor = HAnchor.Stretch,
                VAnchor = VAnchor.Stretch
            };

            // Padding allows space for scrollbar
            materialInfo.Padding = new BorderDouble(right: theme.DefaultContainerPadding + 2);

            panel2Column.AddChild(materialInfo);

            horizontalSplitter.Panel2.Padding = horizontalSplitter.Panel2.Padding.Clone(right: 0, bottom: 0);

            horizontalSplitter.Panel2.AddChild(panel2Column);
        }

        protected override bool NodeMatchesFilter(TreeNode context, string filter)
        {
            // check if the node matches the filter
            return context.Text.IndexOf(filter, StringComparison.OrdinalIgnoreCase) != -1;
        }

        private static void SetImage(TreeNode node, ImageBuffer image)
        {
            node.Image = image;

            // Push to children
            foreach (var child in node.Nodes)
            {
                SetImage(child, image);
            }
        }

        private TreeNode CreateTreeNode(string directory)
        {
            var treeNode = new TreeNode(theme)
            {
                Text = Path.GetFileName(directory).Replace("_", " ").Trim(),
            };

            foreach (var subDirectory in Directory.EnumerateDirectories(directory).OrderBy(f => Path.GetFileName(f)))
            {
                treeNode.Nodes.Add(CreateTreeNode(subDirectory));
            }

            foreach (var materialFile in Directory.EnumerateFiles(directory, "*.material").OrderBy(f => Path.GetFileName(f)))
            {
                var materialToAdd = PrinterSettings.LoadFile(materialFile);
                var name = materialToAdd.MaterialLayers[0].Name;
                var fileName = Path.GetFileNameWithoutExtension(materialFile).Replace("_", " ").Trim();
                var sku = "";
                if (materialToAdd.MaterialLayers[0].ContainsKey(SettingsKey.material_sku))
                {
                    sku = materialToAdd.MaterialLayers[0][SettingsKey.material_sku];
                }

                treeNode.Nodes.Add(new TreeNode(theme, nodeParent: treeNode)
                {
                    Text = fileName,
                    Name = fileName,
                    Tag = new MaterialInfo()
                    {
                        Name = name,
                        Path = materialFile,
                        Sku = sku
                    }
                });
            }

            if (Path.GetFileName(directory).Contains("MatterHackers"))
            {
                treeNode.Expanded = true;
            }

            return treeNode;
        }

        private void TreeView_AfterSelect(object sender, TreeNode e)
        {
            if (TreeView.SelectedNode?.Tag != null)
            {
                UiThread.RunOnIdle(() =>
                {
                    if (TreeView.SelectedNode != null)
                    {
                        string printerName = TreeView.SelectedNode.Tag.ToString();

                        this.SelectedMaterial = TreeView.SelectedNode.Tag as MaterialInfo;
                        materialInfo.CloseChildren();

                        nextButtonEnabled(TreeView.SelectedNode != null);
                    }
                });
            }
            else
            {
                nextButtonEnabled(false);
            }
        }
    }
}