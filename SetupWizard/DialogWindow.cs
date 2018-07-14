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
using System.Collections.Generic;
using MatterHackers.Agg;
using MatterHackers.Agg.UI;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl
{
	public class DialogWindow : SystemWindow
	{
		private DialogPage activePage;
		private EventHandler unregisterEvents;
		private static Dictionary<Type, DialogWindow> allWindows = new Dictionary<Type, DialogWindow>();
		private ThemeConfig theme;

		private DialogWindow()
			: base(500 * GuiWidget.DeviceScale, 500 * GuiWidget.DeviceScale)
		{
			theme = ApplicationController.Instance.Theme;

			this.AlwaysOnTopOfMain = true;
			this.MinimumSize = new Vector2(200, 200);
			this.BackgroundColor = theme.ActiveTabColor;

			var defaultPadding = theme.DefaultContainerPadding;
			this.Padding = new BorderDouble(defaultPadding, defaultPadding, defaultPadding, 2);
		}

		public static void Close(Type type)
		{
			if (allWindows.TryGetValue(type, out DialogWindow existingWindow))
			{
				existingWindow.Close();
			}
		}

		public static void Show<PanelType>() where PanelType : DialogPage, new()
		{
			DialogWindow wizardWindow = GetWindow(typeof(PanelType));
			var newPanel = wizardWindow.ChangeToPage<PanelType>();
			wizardWindow.Title = newPanel.WindowTitle;

			SetSizeAndShow(wizardWindow, newPanel);
		}

		public static DialogWindow Show(DialogPage wizardPage)
		{
			DialogWindow wizardWindow = GetWindow(wizardPage.GetType());
			wizardWindow.Title = wizardPage.WindowTitle;

			SetSizeAndShow(wizardWindow, wizardPage);

			wizardWindow.ChangeToPage(wizardPage);

			return wizardWindow;
		}

		// Allow the WizardPage MinimumSize to override our MinimumSize
		public override Vector2 MinimumSize
		{
			get => activePage?.MinimumSize ?? base.MinimumSize;
			set => base.MinimumSize = value;
		}

		public static void SetSizeAndShow(DialogWindow wizardWindow, DialogPage wizardPage)
		{
			if (wizardPage.WindowSize != Vector2.Zero)
			{
				wizardWindow.Size = wizardPage.WindowSize;
			}

			wizardWindow.AlwaysOnTopOfMain = wizardPage.AlwaysOnTopOfMain;

			wizardWindow.ShowAsSystemWindow();
		}

		public static bool IsOpen(Type type) => allWindows.ContainsKey(type);

		private static DialogWindow GetWindow(Type type)
		{
			if (allWindows.TryGetValue(type, out DialogWindow wizardWindow))
			{
				wizardWindow.BringToFront();
				wizardWindow.Focus();
			}
			else
			{
				wizardWindow = new DialogWindow();
				wizardWindow.Closed += (s, e) => allWindows.Remove(type);
				allWindows[type] = wizardWindow;
			}

			return wizardWindow;
		}

		public override void OnClosed(ClosedEventArgs e)
		{
			unregisterEvents?.Invoke(this, null);
			base.OnClosed(e);
		}

		public void ChangeToPage(DialogPage pageToChangeTo)
		{
			activePage = pageToChangeTo;

			pageToChangeTo.DialogWindow = this;
			this.CloseAllChildren();
			this.AddChild(pageToChangeTo);
			this.Invalidate();
		}

		public DialogPage ChangeToPage<PanelType>() where PanelType : DialogPage, new()
		{
			var panel = new PanelType
			{
				DialogWindow = this
			};
			ChangeToPage(panel);

			// in the event of a reload all make sure we rebuild the contents correctly
			ApplicationController.Instance.DoneReloadingAll.RegisterEvent((s,e) =>
			{
				// fix the main window background color if needed
				BackgroundColor = theme.ActiveTabColor;

				// find out where the contents we put in last time are
				int thisIndex = GetChildIndex(panel);
				RemoveAllChildren();

				// make new content with the possibly changed theme
				var newPanel = new PanelType
				{
					DialogWindow = this
				};
				AddChild(newPanel, thisIndex);
				panel.CloseOnIdle();

				// remember the new content
				panel = newPanel;
			}, ref unregisterEvents);

			return panel;
		}
	}
}