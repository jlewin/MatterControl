﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using MatterHackers.Agg;
using MatterHackers.Agg.UI;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.DataStorage;
using MatterHackers.MatterControl.PrinterCommunication;
using MatterHackers.MatterControl.PrintQueue;
using MatterHackers.Agg.PlatformAbstract; 


namespace MatterHackers.MatterControl.SlicerConfiguration
{
	public class MatterSliceInfo : SliceEngineInfo
	{
		public MatterSliceInfo()
            : base(MatterSliceInfo.DisplayName)
		{
		}

        public static string DisplayName = "MatterSlice";

        public override ActivePrinterProfile.SlicingEngineTypes GetSliceEngineType()
        {
            return ActivePrinterProfile.SlicingEngineTypes.MatterSlice;
        }

        public override bool Exists()
        {
            if (Configuration.OsInformation.OperatingSystem == OSType.Android || Configuration.OsInformation.OperatingSystem == OSType.Mac || SlicingQueue.runInProcess)
            {
				return true;
            }
            else
            {
				if (this.GetEnginePath() == null)
				{
					return false;
				}
				else
				{
					return System.IO.File.Exists(this.GetEnginePath());
				}
            }
        }

        protected override string getWindowsPath()
		{
			string matterSliceRelativePath = Path.Combine(".", "MatterSlice.exe");
			return Path.GetFullPath(matterSliceRelativePath);
		}

        protected override string getMacPath()
		{
			string applicationPath = Path.Combine(ApplicationDataStorage.Instance.ApplicationPath, "MatterSlice");
			return applicationPath;
		}

        protected override string getLinuxPath()
		{
			string matterSliceRelativePath = Path.Combine(".", "MatterSlice.exe");
			return Path.GetFullPath(matterSliceRelativePath);
		}

       


	}
}

