﻿/*
Copyright (c) 2014, Lars Brubaker
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
using MatterHackers.GuiAutomation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MatterHackers.MatterControl
{
	public static class PerformanceTests
	{
		public static void ReportDrawTimeWhileSwitching(GuiWidget container, string firstWidgetName, string secondWidgetName, double switchTimeSeconds)
		{
			StatisticsTracker testTracker = new StatisticsTracker("SwitchBetweenTabs");
			bool clickFirstItem = true;
			bool done = false;
			AutomationRunner clickPreview;
			Stopwatch timeSinceLastClick = Stopwatch.StartNew();
			Stopwatch totalDrawTime = Stopwatch.StartNew();
			int drawCount = 0;

			EventHandler formLoad = (sender1, e1) =>
            {
				clickPreview = new AutomationRunner();
				Task.Run(() =>
				{
					while (!done)
					{
						if (clickPreview != null && timeSinceLastClick.Elapsed.TotalSeconds > switchTimeSeconds)
						{
							if (clickFirstItem)
							{
								clickPreview.ClickByName(firstWidgetName);
							}
							else
							{
								clickPreview.ClickByName(secondWidgetName);
							}
							clickFirstItem = !clickFirstItem;
							timeSinceLastClick.Restart();
						}
					}
				});
			};

			container.Load += formLoad;

			container.BeforeDraw += (sender, e) =>
			{
				totalDrawTime.Restart();
			};

			DrawEventHandler afterDraw = null;
			afterDraw = (sender, e) =>
			{
				totalDrawTime.Stop();
				if (drawCount++ > 30 && testTracker.Count < 100)
				{
					testTracker.AddValue(totalDrawTime.ElapsedMilliseconds);
					if (testTracker.Count == 100)
					{
						Trace.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(testTracker));
						container.Load -= formLoad;
						container.BeforeDraw -= afterDraw;
						done = true;
					}
				}
			};

			container.AfterDraw += afterDraw;
		}

		public static void ClickStuff(GuiWidget container, string[] clickThings, double secondsBetweenClicks = .1)
		{
			AutomationRunner clickPreview;

			DrawEventHandler beforeDraw = null;
			beforeDraw = (sender, e) =>
			{
				clickPreview = new AutomationRunner();
				Task.Run(() =>
				{
					foreach (string clickName in clickThings)
					{
						clickPreview.ClickByName(clickName, 10);
						Thread.Sleep((int)(secondsBetweenClicks * 1000));
					}
				});

				container.BeforeDraw -= beforeDraw;
			};

			container.BeforeDraw += beforeDraw;
		}

		public static void CreateButtonOpensPluginWindow(GuiWidget container, double secondsBetweenClicks = .1)
		{
			//To run test invoke method in the queue data widget 
			AutomationRunner testRunner;
			DrawEventHandler beforeDraw = null;
			beforeDraw = (sender, e) =>
			{
				testRunner = new AutomationRunner();
				Task.Run(() =>
				{
					testRunner.ClickByName("Queue Tab");
					testRunner.ClickByName("Design Tool Button");
				});
				container.BeforeDraw -= beforeDraw;
			};
			container.BeforeDraw += beforeDraw;
			
		}

		public static void AddLocalLibraryItemToQueue(GuiWidget container, double secondsBetweenClicks = .1)
		{
			AutomationRunner testrunner;
			DrawEventHandler beforeDraw = null;
			beforeDraw = (sender, e) =>
			{
				testrunner = new AutomationRunner();
				Task.Run(() =>
				{
					testrunner.ClickByName("Library Tab");
					NavigateToFolder(testrunner, "Local Library Row Item Collection");
					
					testrunner.ClickByName("Library Edit Button");
					testrunner.ClickByName("Row Item Calibration - Box");
					testrunner.Wait(2);
					testrunner.ClickByName("Library Add To Queue Button");
					testrunner.ClickByName("Queue Tab");
				});
				container.BeforeDraw -= beforeDraw;
			};
			container.BeforeDraw += beforeDraw;
		}

		public static void RenameLibraryItem(GuiWidget container, double secondsBetweenClicks = .1)
		{
			AutomationRunner testrunner;
			DrawEventHandler beforeDraw = null;
			beforeDraw = (sender, e) =>
			{
				testrunner = new AutomationRunner();
				Task.Run(() =>
				{
					testrunner.ClickByName("Library Tab");
					NavigateToFolder(testrunner, "Local Library Row Item Collection");

					testrunner.ClickByName("Library Edit Button");
					testrunner.ClickByName("Row Item Calibration - Box");
					testrunner.Wait(2);
					testrunner.ClickByName("Rename From Library Button");
					testrunner.Wait(2);
					testrunner.Type("Renamed Calibration Cube");
					testrunner.ClickByName("Rename Button");

					
				});
				container.BeforeDraw -= beforeDraw;
			};
			container.BeforeDraw += beforeDraw;
		}

		public static void CreateAndRenameLocalLibraryFolder(GuiWidget container, double secondsBetweenClicks = .1)
		{
			AutomationRunner testrunner;
			DrawEventHandler beforeDraw = null;
			beforeDraw = (sender, e) =>
			{
				testrunner = new AutomationRunner();
				Task.Run(() =>
				{
					testrunner.ClickByName("Library Tab");
					NavigateToFolder(testrunner, "Local Library Row Item Collection");
					testrunner.ClickByName("Create Folder From Library Button");
					testrunner.Wait(2);
					testrunner.Type("New Folder");
					testrunner.ClickByName("Create Folder Button");
					testrunner.ClickByName("Library Edit Button");
					testrunner.ClickByName("Row Item New Folder");
					testrunner.ClickByName("Rename From Library Button");
					testrunner.Wait(2);
					testrunner.Type("Renamed Folder");
					testrunner.ClickByName("Rename Button");


				});
				container.BeforeDraw -= beforeDraw;
			};
			container.BeforeDraw += beforeDraw;
		}

		//This is Temporary and will probably be moved once we get a functional test harness!!!
		public static bool NavigateToFolder(AutomationRunner testRunner, string libraryRowItemName)
		{
			bool goodNavigate = true;
			SearchRegion libraryRowItemRegion = testRunner.GetRegionByName(libraryRowItemName, 3);
			goodNavigate &= testRunner.ClickByName(libraryRowItemName);
			goodNavigate &= testRunner.MoveToByName(libraryRowItemName);
			testRunner.Wait(.5);
			goodNavigate &= testRunner.ClickByName("Open Collection", searchRegion: libraryRowItemRegion);
			testRunner.Wait(.5);

			return goodNavigate;
		}
	}
}