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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using MatterHackers.Agg;
using MatterHackers.Agg.Platform;
using MatterHackers.Agg.UI;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.CustomWidgets;

namespace MatterHackers.MatterControl
{
	public class TerminalWidget : FlowLayoutWidget
	{
		private CheckBox filterOutput;
		private CheckBox autoUppercase;
		private MHTextEditWidget manualCommandTextEdit;
		private TextScrollWidget textScrollWidget;
		private PrinterConfig printer;

		public TerminalWidget(PrinterConfig printer, ThemeConfig theme)
			: base(FlowDirection.TopToBottom)
		{
			this.printer = printer;
			this.Name = "TerminalWidget";
			this.Padding = new BorderDouble(5, 0);

			// Header
			var headerRow = new FlowLayoutWidget(FlowDirection.LeftToRight)
			{
				HAnchor = HAnchor.Left | HAnchor.Stretch,
				Padding = new BorderDouble(0, 8)
			};
			this.AddChild(headerRow);

			filterOutput = new CheckBox("Filter Output".Localize(), textSize: theme.DefaultFontSize)
			{
				TextColor = theme.Colors.PrimaryTextColor,
				VAnchor = VAnchor.Bottom,
			};
			filterOutput.CheckedStateChanged += (s, e) =>
			{
				if (filterOutput.Checked)
				{
					textScrollWidget.SetLineStartFilter(new string[] { "<-wait", "<-ok", "<-T" });
				}
				else
				{
					textScrollWidget.SetLineStartFilter(null);
				}

				UserSettings.Instance.Fields.SetBool(UserSettingsKey.TerminalFilterOutput, filterOutput.Checked);
			};
			headerRow.AddChild(filterOutput);

			autoUppercase = new CheckBox("Auto Uppercase".Localize(), textSize: theme.DefaultFontSize)
			{
				Margin = new BorderDouble(left: 25),
				Checked = UserSettings.Instance.Fields.GetBool(UserSettingsKey.TerminalAutoUppercase, true),
				TextColor = theme.Colors.PrimaryTextColor,
				VAnchor = VAnchor.Bottom
			};
			autoUppercase.CheckedStateChanged += (s, e) =>
			{
				UserSettings.Instance.Fields.SetBool(UserSettingsKey.TerminalAutoUppercase, autoUppercase.Checked);
			};
			headerRow.AddChild(autoUppercase);

			// Body
			var bodyRow = new FlowLayoutWidget();
			bodyRow.AnchorAll();
			this.AddChild(bodyRow);

			textScrollWidget = new TextScrollWidget(printer, printer.Connection.TerminalLog.PrinterLines)
			{
				BackgroundColor = theme.MinimalShade,
				TextColor = theme.Colors.PrimaryTextColor,
				HAnchor = HAnchor.Stretch,
				VAnchor = VAnchor.Stretch,
				Margin = 0,
				Padding = new BorderDouble(3, 0)
			};
			bodyRow.AddChild(textScrollWidget);
			bodyRow.AddChild(new TextScrollBar(textScrollWidget, 15));

			// Input Row
			var inputRow = new FlowLayoutWidget(FlowDirection.LeftToRight)
			{
				BackgroundColor = this.BackgroundColor,
				HAnchor = HAnchor.Stretch,
				Margin = new BorderDouble(bottom: 2)
			};
			this.AddChild(inputRow);

			manualCommandTextEdit = new MHTextEditWidget("", typeFace: ApplicationController.GetTypeFace(NamedTypeFace.Liberation_Mono))
			{
				Margin = new BorderDouble(right: 3),
				HAnchor = HAnchor.Stretch,
				VAnchor = VAnchor.Bottom
			};
			manualCommandTextEdit.ActualTextEditWidget.EnterPressed += (s, e) =>
			{
				SendManualCommand();
			};
			manualCommandTextEdit.ActualTextEditWidget.KeyDown += (s, keyEvent) =>
			{
				bool changeToHistory = false;
				if (keyEvent.KeyCode == Keys.Up)
				{
					commandHistoryIndex--;
					if (commandHistoryIndex < 0)
					{
						commandHistoryIndex = 0;
					}
					changeToHistory = true;
				}
				else if (keyEvent.KeyCode == Keys.Down)
				{
					commandHistoryIndex++;
					if (commandHistoryIndex > commandHistory.Count - 1)
					{
						commandHistoryIndex = commandHistory.Count - 1;
					}
					else
					{
						changeToHistory = true;
					}
				}
				else if (keyEvent.KeyCode == Keys.Escape)
				{
					manualCommandTextEdit.Text = "";
				}

				if (changeToHistory && commandHistory.Count > 0)
				{
					manualCommandTextEdit.Text = commandHistory[commandHistoryIndex];
				}
			};
			inputRow.AddChild(manualCommandTextEdit);

			// Footer
			var toolbarPadding = theme.ToolbarPadding;
			var footerRow = new FlowLayoutWidget
			{
				HAnchor = HAnchor.Stretch,
				Padding = new BorderDouble(0, toolbarPadding.Bottom, toolbarPadding.Right, toolbarPadding.Top)
			};
			this.AddChild(footerRow);

			var sendButton = theme.CreateDialogButton("Send".Localize());
			sendButton.Margin = 0;
			sendButton.Click += (s, e) =>
			{
				SendManualCommand();
			};
			footerRow.AddChild(sendButton);

			var clearButton = theme.CreateDialogButton("Clear".Localize());
			clearButton.Margin = theme.ButtonSpacing;
			clearButton.Click += (s, e) =>
			{
				printer.Connection.TerminalLog.Clear();
			};
			footerRow.AddChild(clearButton);

			var exportButton = theme.CreateDialogButton("Export".Localize());
			exportButton.Margin = theme.ButtonSpacing;
			exportButton.Click += (s, e) =>
			{
				UiThread.RunOnIdle(() =>
				{
					AggContext.FileDialogs.SaveFileDialog(
						new SaveFileDialogParams("Save as Text|*.txt")
						{
							Title = "MatterControl: Terminal Log",
							ActionButtonLabel = "Export",
							FileName = "print_log.txt"
						},
						(saveParams) =>
						{
							if (!string.IsNullOrEmpty(saveParams.FileName))
							{
								string filePathToSave = saveParams.FileName;
								if (filePathToSave != null && filePathToSave != "")
								{
									try
									{
										textScrollWidget.WriteToFile(filePathToSave);
									}
									catch (UnauthorizedAccessException ex)
									{
										Debug.Print(ex.Message);

										printer.Connection.TerminalLog.PrinterLines.Add("");
										printer.Connection.TerminalLog.PrinterLines.Add(writeFaildeWaring);
										printer.Connection.TerminalLog.PrinterLines.Add(cantAccessPath.FormatWith(filePathToSave));
										printer.Connection.TerminalLog.PrinterLines.Add("");

										UiThread.RunOnIdle(() =>
										{
											StyledMessageBox.ShowMessageBox(ex.Message, "Couldn't save file".Localize());
										});
									}
								}
							}
						});
				});
			};
			footerRow.AddChild(exportButton);

			footerRow.AddChild(new HorizontalSpacer());

			this.AnchorAll();
		}

#if !__ANDROID__
		public override void OnLoad(EventArgs args)
		{
			filterOutput.Checked = UserSettings.Instance.Fields.GetBool(UserSettingsKey.TerminalFilterOutput, false);
			UiThread.RunOnIdle(manualCommandTextEdit.Focus);
			base.OnLoad(args);
		}
#endif

		string writeFaildeWaring = "WARNING: Write Failed!".Localize();
		string cantAccessPath = "Can't access '{0}'.".Localize();

		private List<string> commandHistory = new List<string>();
		private int commandHistoryIndex = 0;

		private void SendManualCommand()
		{
			string textToSend = manualCommandTextEdit.Text.Trim();
			if (autoUppercase.Checked)
			{
				textToSend = textToSend.ToUpper();
			}
			commandHistory.Add(textToSend);
			commandHistoryIndex = commandHistory.Count;
			printer.Connection.QueueLine(textToSend);
			manualCommandTextEdit.Text = "";
		}
	}
}
