using System;
using System.Collections.Generic;
using System.IO;
using MatterHackers.Agg;
using MatterHackers.Agg.ImageProcessing;
using MatterHackers.Agg.PlatformAbstract;
using MatterHackers.Agg.UI;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.ConfigurationPage.PrintLeveling;
using MatterHackers.MatterControl.SlicerConfiguration;
using MatterHackers.MatterControl.CustomWidgets;
using MatterHackers.MatterControl.DataStorage;
using MatterHackers.MatterControl.PrinterCommunication;
using MatterHackers.MatterControl.EeProm;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl.ConfigurationPage
{
    public class HardwareSettingsWidget : SettingsViewBase
    {
		Button openGcodeTerminalButton;
		Button openCameraButton;

		SettingsItem eePromControlsContainer;
		DisableableWidget terminalCommunicationsContainer;
		SettingsItem levelingSection;

		static EePromMarlinWindow openEePromMarlinWidget = null;
		static EePromRepetierWindow openEePromRepetierWidget = null;

        event EventHandler unregisterEvents;

		public HardwareSettingsWidget(SettingsItem.WidgetFactory widgetFactories)
			: base(LocalizedString.Get("Hardware Settings"))
        {
            //terminalCommunicationsContainer = new DisableableWidget();
			//terminalCommunicationsContainer.AddChild(GetGcodeTerminalControl());

			if (!ActiveSliceSettings.Instance.HasHardwareLeveling())
			{
				levelingSection = new SettingsItem(
					"Automatic Print Leveling".Localize(),
					widgetFactories,
					Path.Combine("PrintStatusControls", "leveling-24x24.png"),
					toggleSwitchConfig: new SettingsItem.ToggleSwitchConfig()
					{
						Checked = ActivePrinterProfile.Instance.DoPrintLeveling,  
						ToggleAction = (itemChecked)=> {
							ActivePrinterProfile.Instance.DoPrintLeveling = itemChecked;
						}
					}
				);
				mainContainer.AddChild(levelingSection);

				SettingsItem levelingWizardRow = new SettingsItem(
					"Start Print Leveling Wizard".Localize(),
					widgetFactories,
					itemClickedAction: () => {
						UiThread.RunOnIdle((state) =>
						{
							LevelWizardBase.ShowPrintLevelWizard(LevelWizardBase.RuningState.UserRequestedCalibration);
						});
					}
				);

				levelingSection.ChildSettings.Add(levelingWizardRow);
				mainContainer.AddChild(new HorizontalLine(separatorLineColor));

				SettingsItem editPositionsRow = new SettingsItem(
					"Edit Sampled Positions".Localize(),
					widgetFactories,
					itemClickedAction: () => {
						UiThread.RunOnIdle((state) =>
						{
							if (editLevelingSettingsWindow == null)
							{
								editLevelingSettingsWindow = new EditLevelingSettingsWindow();
								editLevelingSettingsWindow.Closed += (sender, e) =>
								{
									editLevelingSettingsWindow = null;
								};
							}
							else
							{
								editLevelingSettingsWindow.BringToFront();
							}
						});
					}
				);

				levelingSection.ChildSettings.Add(editPositionsRow);
				mainContainer.AddChild(new HorizontalLine(separatorLineColor));

				mainContainer.AddChild(levelingWizardRow);
				mainContainer.AddChild(new HorizontalLine(separatorLineColor));

				mainContainer.AddChild(editPositionsRow);
				mainContainer.AddChild(new HorizontalLine(separatorLineColor));
			}

			eePromControlsContainer = new SettingsItem(
				"EEProm Settings".Localize(),
				widgetFactories,
				Path.Combine("PrintStatusControls", "leveling-24x24.png"),
				itemClickedAction: configureEePromButton_Click
			);

			mainContainer.AddChild(eePromControlsContainer);
			mainContainer.AddChild(new HorizontalLine(separatorLineColor));

			terminalCommunicationsContainer = new SettingsItem(
				"Gcode Console".Localize(),
				widgetFactories,
				Path.Combine("PrintStatusControls", "terminal-24x24.png"),
#if(__ANDROID__)
				toggleSwitchConfig: new SettingsItem.ToggleSwitchConfig()
				{
					Checked = UserSettings.Instance.get("ConsoleVisible") == "true",  
					ToggleAction = (itemChecked)=> {
						bool consoleVisible = UserSettings.Instance.get("ConsoleVisible") == "true";

						// Toggle the state of the option
						UserSettings.Instance.set("ConsoleVisible", consoleVisible ? "false" : "true");

						// Update the UI
						// TODO: This is horribly fragile and mearly depicts the potential of avoiding a full control reload
						CompactApplicationView mainView = ApplicationController.Instance.MainView as CompactApplicationView;
						foreach(GuiWidget widget in mainView.Children[0].Children[3].Children)
						{
							if(widget is CompactTabView)
							{
								(widget as CompactTabView).SetTerminalVisibility();
							}
						}
					}
				}
#else
				itemClickedAction: () => {
					UiThread.RunOnIdle((state) => TerminalWindow.Show());
				}
#endif
			);
			mainContainer.AddChild(terminalCommunicationsContainer);

#if(__ANDROID__)
			SettingsItem showHistoryItem = new SettingsItem(
				"Print History".Localize(),
				widgetFactories,
				Path.Combine("PrintStatusControls", "terminal-24x24.png"),
				toggleSwitchConfig: new SettingsItem.ToggleSwitchConfig()
				{
					Checked = UserSettings.Instance.get("HistoryVisible") == "true",  
					ToggleAction = (itemChecked)=> {
						bool historyVisible = UserSettings.Instance.get("HistoryVisible") == "true";

						// Toggle the state of the option
						UserSettings.Instance.set("HistoryVisible", historyVisible ? "false" : "true");

						// Update the UI
						// TODO: This is horribly fragile and mearly depicts the potential of avoiding a full control reload
						CompactApplicationView mainView = ApplicationController.Instance.MainView as CompactApplicationView;
						foreach(GuiWidget widget in mainView.Children[0].Children[3].Children)
						{
							if(widget is CompactTabView)
							{
								(widget as CompactTabView).SetHistoryVisibility();
							}
						}
					}
				}
			);
			mainContainer.AddChild(showHistoryItem);
#endif

            AddChild(mainContainer);
            AddHandlers();
            SetVisibleControls();
        }

        EditLevelingSettingsWindow editLevelingSettingsWindow;
        TextWidget printLevelingStatusLabel;

        public override void OnClosed(EventArgs e)
        {
            if (unregisterEvents != null)
            {
                unregisterEvents(this, null);
            }
            base.OnClosed(e);
        }

        private FlowLayoutWidget GetGcodeTerminalControl()
		{
			FlowLayoutWidget buttonRow = new FlowLayoutWidget();
			buttonRow.HAnchor = HAnchor.ParentLeftRight;
			buttonRow.Margin = new BorderDouble(0,4);

            Agg.Image.ImageBuffer terminalSettingsImage = StaticData.Instance.LoadIcon(Path.Combine("PrintStatusControls", "terminal-24x24.png"));
            if (!ActiveTheme.Instance.IsDarkTheme)
            {
                InvertLightness.DoInvertLightness(terminalSettingsImage);
            }

            ImageWidget terminalIcon = new ImageWidget(terminalSettingsImage);
            terminalIcon.Margin = new BorderDouble(right: 6, bottom: 6);

			TextWidget gcodeTerminalLabel = new TextWidget("Gcode Console");
			gcodeTerminalLabel.AutoExpandBoundsToText = true;
			gcodeTerminalLabel.TextColor = ActiveTheme.Instance.PrimaryTextColor;
			gcodeTerminalLabel.VAnchor = VAnchor.ParentCenter;

			GuiWidget consoleSwitchContainer = new FlowLayoutWidget();
			consoleSwitchContainer.VAnchor = VAnchor.ParentCenter;
			consoleSwitchContainer.Margin = new BorderDouble(left: 16, right: 32);

			ToggleSwitch consoleSwitch = GenerateToggleSwitch(consoleSwitchContainer, true);
			consoleSwitch.SwitchState = ActivePrinterProfile.Instance.DoPrintLeveling;
			consoleSwitch.SwitchStateChanged += (sender, e) => 
			{
				ActivePrinterProfile.Instance.DoPrintLeveling = consoleSwitch.SwitchState;
			};
			consoleSwitchContainer.SetBoundsToEncloseChildren();

			//openGcodeTerminalButton = textImageButtonFactory.Generate("Show Terminal".Localize().ToUpper());
			//openGcodeTerminalButton.Click += new EventHandler(openGcodeTerminalButton_Click);

            buttonRow.AddChild(terminalIcon);
            buttonRow.AddChild(gcodeTerminalLabel);
			buttonRow.AddChild(new HorizontalSpacer());
			buttonRow.AddChild(consoleSwitchContainer);

			return buttonRow;
		}

        private void AddHandlers()
        {
            PrinterConnectionAndCommunication.Instance.CommunicationStateChanged.RegisterEvent(onPrinterStatusChanged, ref unregisterEvents);
            PrinterConnectionAndCommunication.Instance.EnableChanged.RegisterEvent(onPrinterStatusChanged, ref unregisterEvents);
        }

		string noEepromMappingTitle = "Warning - No EEProm Mapping".Localize();
		string noEepromMappingMessage = "Oops! There is no eeprom mapping for your printer's firmware.".Localize() + "\n\n" + "You may need to wait a minute for your printer to finish initializing.".Localize();
        void configureEePromButton_Click()
        {
            UiThread.RunOnIdle((state) =>
            {
#if false // This is to force the creation of the repetier window for testing when we don't have repetier firmware.
                        new MatterHackers.MatterControl.EeProm.EePromRepetierWidget();
#else
                    switch (PrinterConnectionAndCommunication.Instance.FirmwareType)
                    {
                        case PrinterConnectionAndCommunication.FirmwareTypes.Repetier:
                            if (openEePromRepetierWidget != null)
                            {
                                openEePromRepetierWidget.BringToFront();
                            }
                            else
                            {
                                openEePromRepetierWidget = new EePromRepetierWindow();
                                openEePromRepetierWidget.Closed += (RepetierWidget, RepetierEvent) =>
                                {
                                    openEePromRepetierWidget = null;
                                };
                            }
                            break;

                        case PrinterConnectionAndCommunication.FirmwareTypes.Marlin:
                            if (openEePromMarlinWidget != null)
                            {
                                openEePromMarlinWidget.BringToFront();
                            }
                            else
                            {
                                openEePromMarlinWidget = new EePromMarlinWindow();
                                openEePromMarlinWidget.Closed += (marlinWidget, marlinEvent) =>
                                {
                                    openEePromMarlinWidget = null;
                                };
                            }
                            break;

                        default:
                            StyledMessageBox.ShowMessageBox(null, noEepromMappingMessage, noEepromMappingTitle, StyledMessageBox.MessageType.OK);
                            break;
                    }
#endif
            });
        }

        private void onPrinterStatusChanged(object sender, EventArgs e)
        {
            SetVisibleControls();
            this.Invalidate();
        }

        private void SetVisibleControls()
        {
            if (ActivePrinterProfile.Instance.ActivePrinter == null)
            {
                // no printer selected                         
                eePromControlsContainer.SetEnableLevel(DisableableWidget.EnableLevel.Disabled);
                terminalCommunicationsContainer.SetEnableLevel(DisableableWidget.EnableLevel.Enabled);
                levelingSection.SetEnableLevel(DisableableWidget.EnableLevel.Disabled);
                //cloudMonitorContainer.SetEnableLevel(DisableableWidget.EnableLevel.Disabled);
            }
            else // we at least have a printer selected
            {
                //cloudMonitorContainer.SetEnableLevel(DisableableWidget.EnableLevel.Enabled);
                switch (PrinterConnectionAndCommunication.Instance.CommunicationState)
                {
                    case PrinterConnectionAndCommunication.CommunicationStates.Disconnecting:
                    case PrinterConnectionAndCommunication.CommunicationStates.ConnectionLost:
                    case PrinterConnectionAndCommunication.CommunicationStates.Disconnected:
                    case PrinterConnectionAndCommunication.CommunicationStates.AttemptingToConnect:
                    case PrinterConnectionAndCommunication.CommunicationStates.FailedToConnect:
                        eePromControlsContainer.SetEnableLevel(DisableableWidget.EnableLevel.Disabled);
                        levelingSection.SetEnableLevel(DisableableWidget.EnableLevel.Enabled);
                        terminalCommunicationsContainer.SetEnableLevel(DisableableWidget.EnableLevel.Enabled);
                        break;

                    case PrinterConnectionAndCommunication.CommunicationStates.FinishedPrint:
                    case PrinterConnectionAndCommunication.CommunicationStates.Connected:
                        eePromControlsContainer.SetEnableLevel(DisableableWidget.EnableLevel.Enabled);
                        levelingSection.SetEnableLevel(DisableableWidget.EnableLevel.Enabled);
                        terminalCommunicationsContainer.SetEnableLevel(DisableableWidget.EnableLevel.Enabled);
                        break;

                    case PrinterConnectionAndCommunication.CommunicationStates.PrintingFromSd:
                        eePromControlsContainer.SetEnableLevel(DisableableWidget.EnableLevel.Disabled);
                        levelingSection.SetEnableLevel(DisableableWidget.EnableLevel.Disabled);
                        terminalCommunicationsContainer.SetEnableLevel(DisableableWidget.EnableLevel.Enabled);
                        break;

                    case PrinterConnectionAndCommunication.CommunicationStates.PreparingToPrint:
                    case PrinterConnectionAndCommunication.CommunicationStates.Printing:
                        switch (PrinterConnectionAndCommunication.Instance.PrintingState)
                        {
                            case PrinterConnectionAndCommunication.DetailedPrintingState.HomingAxis:
                            case PrinterConnectionAndCommunication.DetailedPrintingState.HeatingBed:
                            case PrinterConnectionAndCommunication.DetailedPrintingState.HeatingExtruder:
                            case PrinterConnectionAndCommunication.DetailedPrintingState.Printing:
                                eePromControlsContainer.SetEnableLevel(DisableableWidget.EnableLevel.Disabled);
                                levelingSection.SetEnableLevel(DisableableWidget.EnableLevel.Disabled);
                                terminalCommunicationsContainer.SetEnableLevel(DisableableWidget.EnableLevel.Enabled);
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                        break;

                    case PrinterConnectionAndCommunication.CommunicationStates.Paused:
                        eePromControlsContainer.SetEnableLevel(DisableableWidget.EnableLevel.Enabled);
                        levelingSection.SetEnableLevel(DisableableWidget.EnableLevel.Disabled);
                        terminalCommunicationsContainer.SetEnableLevel(DisableableWidget.EnableLevel.Enabled);
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }
}