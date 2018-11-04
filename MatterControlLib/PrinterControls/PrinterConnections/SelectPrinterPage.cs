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
using MatterHackers.Agg.Platform;
using MatterHackers.Agg.UI;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.CustomWidgets;
using MatterHackers.MatterControl.Library.Widgets.HardwarePage;
using MatterHackers.MatterControl.PrintLibrary;
using MatterHackers.MatterControl.SlicerConfiguration;

namespace MatterHackers.MatterControl.PrinterControls.PrinterConnections
{
	public class SelectPrinterPage : DialogPage
	{
		private Action<PrinterConfig> printerLoaded;

		public SelectPrinterPage(string continueButtonText, Action<PrinterConfig> printerLoaded = null)
		{
			this.printerLoaded = printerLoaded;

			this.WindowTitle = "Select Printer".Localize();
			this.HeaderText = "Select a printer to continue".Localize();

			var treeView = new TreeView(theme)
			{
				HAnchor = HAnchor.Stretch,
				VAnchor = VAnchor.Stretch,
				Margin = theme.DefaultContainerPadding
			};
			treeView.ScrollArea.HAnchor = HAnchor.Stretch;
			contentRow.AddChild(treeView);

			var nextButton = new TextButton(continueButtonText, theme)
			{
				Enabled = false
			};
			nextButton.Click += (s, e) =>
			{
				if (treeView?.SelectedNode.Tag is PrinterInfo printerInfo)
				{
					this.SwitchToPrinter(printerInfo);
				}
			};
			this.AddPageAction(nextButton);

			treeView.NodeMouseDoubleClick += (s, e) =>
			{
				if (e is MouseEventArgs mouseEvent
					&& mouseEvent.Button == MouseButtons.Left
						&& mouseEvent.Clicks == 2)
				{
					if (treeView?.SelectedNode.Tag is PrinterInfo printerInfo)
					{
						this.SwitchToPrinter(printerInfo);
					}
				}
			};

			treeView.AfterSelect += async (s, e) =>
			{
				if (treeView.SelectedNode.Tag is PrinterInfo printerInfo)
				{
					nextButton.Enabled = true;
				}
			};

			// Printers
			var printersNode = new TreeNode(theme)
			{
				Text = "Printers".Localize(),
				HAnchor = HAnchor.Stretch,
				AlwaysExpandable = true,
				Image = AggContext.StaticData.LoadIcon("printer.png", 16, 16, theme.InvertIcons)
			};
			printersNode.TreeView = treeView;
			treeView.AddChild(printersNode);

			InventoryTreeView.RebuildPrintersList(printersNode, theme);
			this.Invalidate();
		}

		private void SwitchToPrinter(PrinterInfo printerInfo)
		{
			if (printerLoaded == null)
			{
				// Switch to the given printer and load the most recent bed plate
				PrinterDetails.SwitchPrinters(printerInfo.ID);
			}
			else
			{
				// Switch to the given printer and let the caller do as they must
				PrinterDetails.OpenPrinterAsync(printerInfo.ID).ContinueWith(task =>
				{
					printerLoaded?.Invoke(task.Result);
				});
			}

			this.DialogWindow.CloseOnIdle();
		}
	}
}