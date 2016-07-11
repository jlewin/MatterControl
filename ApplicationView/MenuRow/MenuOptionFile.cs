﻿using MatterHackers.Agg;
using MatterHackers.Agg.UI;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.DataStorage;
using MatterHackers.MatterControl.PrinterCommunication;
using MatterHackers.MatterControl.PrinterControls.PrinterConnections;
using MatterHackers.MatterControl.PrintQueue;
using System.Collections.Generic;
using MatterHackers.VectorMath;
using System;
using System.IO;
using System.Linq;

namespace MatterHackers.MatterControl
{
	public class MenuOptionFile : MenuBase
	{
		private static CreateFolderWindow createFolderWindow = null;

		public static MenuOptionFile CurrentMenuOptionFile = null;

		public EventHandler RedeemDesignCode;
		public EventHandler EnterShareCode;

		public MenuOptionFile()
			: base("File".Localize())
		{
			Name = "File Menu";
			CurrentMenuOptionFile = this;

		}

		protected override IEnumerable<MenuItemAction> GetMenuActions()
		{
			return new List<MenuItemAction>
			{
				new MenuItemAction("Add Printer".Localize(), () => WizardWindow.Show()),
				new MenuItemAction("Add File To Queue".Localize(), importFile_Click),
				new MenuItemAction("Redeem Design Code".Localize(), () => RedeemDesignCode?.Invoke(this, null)),
				new MenuItemAction("Enter Share Code".Localize(), () => EnterShareCode?.Invoke(this, null)),
				new MenuItemAction("------------------------", null),
				new MenuItemAction("Exit".Localize(), () =>
				{
					MatterControlApplication app = this.Parents<MatterControlApplication>().FirstOrDefault();
					app.RestartOnClose = false;
					app.Close();
				})
			};
		}

		private void importFile_Click()
		{
			UiThread.RunOnIdle(() =>
			{
				FileDialog.OpenFileDialog(
					new OpenFileDialogParams(ApplicationSettings.OpenPrintableFileParams)
					{
						MultiSelect = true,
						ActionButtonLabel = "Add to Queue",
						Title = "MatterControl: Select A File"
					},
					(openParams) =>
					{
						if (openParams.FileNames != null)
						{
							foreach (string loadedFileName in openParams.FileNames)
							{
                                if (Path.GetExtension(loadedFileName).ToUpper() == ".ZIP")
                                {
                                    List<PrintItem> partFiles = ProjectFileHandler.ImportFromProjectArchive(loadedFileName);
                                    if (partFiles != null)
                                    {
                                        foreach (PrintItem part in partFiles)
                                        {
                                            QueueData.Instance.AddItem(new PrintItemWrapper(new PrintItem(part.Name, part.FileLocation)));
                                        }
                                    }
                                }
                                else
                                {
                                    QueueData.Instance.AddItem(new PrintItemWrapper(new PrintItem(Path.GetFileNameWithoutExtension(loadedFileName), Path.GetFullPath(loadedFileName))));
                                }
							}
						}
					});
			});
		}

	}
}
