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

using System.IO;
using MatterHackers.Agg;
using MatterHackers.Agg.Platform;
using MatterHackers.Agg.UI;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.PrinterControls.PrinterConnections;
using MatterHackers.MatterControl.SettingsManagement;
using MatterHackers.MatterControl.SlicerConfiguration;

namespace MatterHackers.MatterControl.PartPreviewWindow.PlusTab
{
	public class PlusTabPage : FlowLayoutWidget
	{
		public PlusTabPage(PartPreviewContent partPreviewContent, SimpleTabs simpleTabs, ThemeConfig theme)
		{
			var leftContent = new FlowLayoutWidget(FlowDirection.TopToBottom)
			{
				VAnchor = VAnchor.Stretch,
				Padding = 15
			};
			this.AddChild(leftContent);

			if (OemSettings.Instance.ShowShopButton)
			{
				this.AddChild(new ExplorePanel(theme));
			}

			this.HAnchor = HAnchor.Stretch;
			this.VAnchor = VAnchor.Stretch;

			BorderDouble buttonSpacing = 3;

			// put in the add new design stuff
			var createItemsSection = CreateSection(leftContent, "Create New".Localize() + ":");

			var createPart = theme.ButtonFactory.Generate("Create Part".Localize());
			createPart.Margin = buttonSpacing;
			createPart.HAnchor = HAnchor.Left;
			createItemsSection.AddChild(createPart);
			createPart.Click += (s, e) =>
			{
				UiThread.RunOnIdle(() =>
				{
					BedConfig bed;
					simpleTabs.RemoveTab(simpleTabs.ActiveTab);
					partPreviewContent.CreatePartTab(
						"New Part", 
						bed = new BedConfig(),
						theme);

					if (partTabWidget.TabContent is PartTabPage partTab)
					{
						partTab.view3DWidget.meshViewerWidget.SuppressUiVolumes = true;
					}

					var d = new[] {
						"m111,52.0625 l-42.999992,78.921265 l42.999992,79.016235 l86,0l43,-79.016235 l-43,-78.921265 l-86,0z",
						"m -762.85715,129.50504 c -13.44619,9.60443 15.72949,49.28263 0.25566,55.07966 -15.47384,5.79703 -19.55047,-43.28414 -35.99743,-41.68956 -16.44696,1.59458 -11.01917,50.54475 -27.31842,47.82821 -16.29925,-2.71655 4.71087,-47.2604 -10.32991,-54.10293 -15.04077,-6.84254 -34.81526,38.26345 -47.57255,27.76123 -12.75729,-10.50222 27.70993,-38.57327 18.10551,-52.01947 -9.60443,-13.446187 -49.28263,15.72949 -55.07966,0.25566 -5.79703,-15.47383 43.28414,-19.550466 41.68956,-35.997423 -1.59458,-16.446955 -50.54475,-11.019174 -47.82821,-27.318421 2.71654,-16.299247 47.2604,4.710867 54.10293,-10.329905 6.84253,-15.040771 -38.26345,-34.8152603 -27.76123,-47.5725513 10.50222,-12.7572917 38.57327,27.7099283 52.01946,18.1055048 13.4462,-9.60442353 -15.72949,-49.2826255 -0.25565,-55.0796545 15.47383,-5.79703 19.55046,43.2841379 35.99742,41.6895592 16.44696,-1.5945787 11.01918,-50.5447502 27.31842,-47.8282092 16.29925,2.716541 -4.71086,47.2603977 10.32991,54.1029302 15.04077,6.8425322 34.81526,-38.2634502 47.57255,-27.7612332 12.75729,10.502217 -27.70993,38.573271 -18.10551,52.019464 9.60443,13.446192 49.28263,-15.72949 55.07966,-0.255658 5.79703,15.473833 -43.28414,19.550469 -41.68956,35.997426 1.59458,16.446955 50.54475,11.019174 47.82821,27.318421 -2.71654,16.29925 -47.2604,-4.710866 -54.10293,10.32991 -6.84253,15.04077 38.26345,34.81526 27.76123,47.57255 -10.50222,12.75729 -38.57327,-27.70993 -52.01946,-18.10551 z",
						"m12.16472,100.99991l81.85645,-81.5815l81.85645,81.5815l-40.92823,0l0,81.97406l-81.85645,0l0,-81.97406l-40.92823,0z"
						};


					bed.LoadContent(
						new EditContext()
						{
							ContentStore = ApplicationController.Instance.Library.PlatingHistory,
							SourceItem = new InMemoryItem(new VertexStorageObject3D(d[2]))
						}).ConfigureAwait(false);
				});
			};

			var createPrinter = theme.ButtonFactory.Generate("Create Printer".Localize());
			createPrinter.Name = "Create Printer";
			createPrinter.Margin = buttonSpacing;
			createPrinter.HAnchor = HAnchor.Left;
			createPrinter.Click += (s, e) =>
			{
				UiThread.RunOnIdle(() =>
				{
					simpleTabs.RemoveTab(simpleTabs.ActiveTab);

					if (ApplicationController.Instance.ActivePrinter.Connection.PrinterIsPrinting
					|| ApplicationController.Instance.ActivePrinter.Connection.PrinterIsPaused)
					{
						StyledMessageBox.ShowMessageBox("Please wait until the print has finished and try again.".Localize(), "Can't add printers while printing".Localize());
					}
					else
					{
						DialogWindow.Show(PrinterSetup.GetBestStartPage(PrinterSetup.StartPageOptions.ShowMakeModel));
					}
				});
			};
			createItemsSection.AddChild(createPrinter);

			var importButton = theme.ButtonFactory.Generate("Import Printer".Localize());
			importButton.Margin = buttonSpacing;
			importButton.HAnchor = HAnchor.Left;
			importButton.Click += (s, e) =>
			{
				UiThread.RunOnIdle(() =>
				{
					AggContext.FileDialogs.OpenFileDialog(
						new OpenFileDialogParams(
							"settings files|*.ini;*.printer;*.slice"),
							(result) =>
							{
								if (!string.IsNullOrEmpty(result.FileName)
									&& File.Exists(result.FileName))
								{
									simpleTabs.RemoveTab(simpleTabs.ActiveTab);
									ImportSettingsPage.ImportFromExisting(result.FileName);
								}
							});
				});
			};
			createItemsSection.AddChild(importButton);

			var existingPrinterSection = CreateSection(leftContent, "Open Existing".Localize() + ":");

			var printerSelector = new PrinterSelector()
			{
				Margin = new BorderDouble(left: 15)
			};
			existingPrinterSection.AddChild(printerSelector);

			var otherItemsSection = CreateSection(leftContent, "Other".Localize() + ":");

			var redeemDesignCode = theme.ButtonFactory.Generate("Redeem Design Code".Localize());
			redeemDesignCode.Name = "Redeem Design Code Button";
			redeemDesignCode.Margin = buttonSpacing;
			redeemDesignCode.HAnchor = HAnchor.Left;
			redeemDesignCode.Click += (s, e) =>
			{
				UiThread.RunOnIdle(() =>
				{
					simpleTabs.RemoveTab(simpleTabs.ActiveTab);
					// Implementation already does RunOnIdle
					ApplicationController.Instance.RedeemDesignCode?.Invoke();
				});
			};
			otherItemsSection.AddChild(redeemDesignCode);

			var redeemShareCode = theme.ButtonFactory.Generate("Enter Share Code".Localize());
			redeemShareCode.Name = "Enter Share Code Button";
			redeemShareCode.Margin = buttonSpacing;
			redeemShareCode.HAnchor = HAnchor.Left;
			redeemShareCode.Click += (s, e) =>
			{
				UiThread.RunOnIdle(() =>
				{
					simpleTabs.RemoveTab(simpleTabs.ActiveTab);

					// Implementation already does RunOnIdle
					ApplicationController.Instance.EnterShareCode?.Invoke();
				});
			};
			otherItemsSection.AddChild(redeemShareCode);
		}

		private FlowLayoutWidget CreateSection(GuiWidget parent, string headingText)
		{
			// Add heading
			parent.AddChild(new TextWidget(headingText, textColor: ActiveTheme.Instance.PrimaryTextColor)
			{
				HAnchor = HAnchor.Left
			});

			// Add container
			var container = new FlowLayoutWidget(FlowDirection.TopToBottom)
			{
				Margin = new BorderDouble(10, 10, 10, 8),
			};
			parent.AddChild(container);

			return container;
		}
	}
}
