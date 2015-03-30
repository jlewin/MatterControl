using System;
using System.Collections.Generic;
using System.IO;
using MatterHackers.Agg;
using MatterHackers.Agg.ImageProcessing;
using MatterHackers.Agg.PlatformAbstract;
using MatterHackers.Agg.UI;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.ConfigurationPage.PrintLeveling;
using MatterHackers.MatterControl.CustomWidgets;
using MatterHackers.MatterControl.DataStorage;
using MatterHackers.MatterControl.PrinterCommunication;
using MatterHackers.MatterControl.EeProm;
using MatterHackers.VectorMath;
using MatterHackers.Agg.Image;

namespace MatterHackers.MatterControl.ConfigurationPage
{
    public class CloudSettingsWidget : SettingsViewBase
    {
       // DisableableWidget cloudMonitorContainer;
        SettingsItem notificationSettingsContainer;
        
        //Button enableCloudMonitorButton;
        //Button disableCloudMonitorButton;
        //Button goCloudMonitoringWebPageButton;
        
        Button cloudMonitorInstructionsLink;
        TextWidget cloudMonitorStatusLabel;        
        Button configureNotificationSettingsButton;
        
		public CloudSettingsWidget(SettingsItem.WidgetFactory widgetFactories)
			: base(LocalizedString.Get("Cloud Settings"))
        {
			/*
            cloudMonitorContainer = new DisableableWidget();
            cloudMonitorContainer.AddChild(GetCloudMonitorControls());
            mainContainer.AddChild(cloudMonitorContainer);

            mainContainer.AddChild(new HorizontalLine(separatorLineColor));
			*/


			if (ApplicationSettings.Instance.get("HardwareHasCamera") == "true") 
			{
				SettingsItem cameraSection = new SettingsItem(
					"Camera Sync".Localize(),
					widgetFactories,
					Path.Combine("PrintStatusControls", "camera-24x24.png"),
					toggleSwitchConfig: new SettingsItem.ToggleSwitchConfig(){
						Checked = PrinterSettings.Instance.get("PublishBedImage") == "true",
						ToggleAction = (itemChecked) => {
							PrinterSettings.Instance.set("PublishBedImage", itemChecked ? "true" : "false");
						}
					}
				);

				SettingsItem cameraPreviewSection = new SettingsItem(
					"Frame Camera View".Localize(),
					widgetFactories,
					null,
					itemClickedAction: () => MatterControlApplication.Instance.OpenCameraPreview()
				);

				mainContainer.AddChild(cameraSection);
				mainContainer.AddChild(new HorizontalLine(separatorLineColor));

				mainContainer.AddChild(cameraPreviewSection);
				mainContainer.AddChild(new HorizontalLine(separatorLineColor));

				cameraSection.ChildSettings.Add(cameraPreviewSection);
			}

			notificationSettingsContainer = new SettingsItem(
				"Notification Settings".Localize(),	
				widgetFactories,
				Path.Combine("PrintStatusControls", "notify-24x24.png"),
				new SettingsItem.ToggleSwitchConfig(){ 
					Checked = UserSettings.Instance.get("PrintNotificationsEnabled") == "true",
					ToggleAction = (itemChecked) => UserSettings.Instance.set("PrintNotificationsEnabled", itemChecked ? "true" : "false")
				}
			);
			mainContainer.AddChild(notificationSettingsContainer);
			mainContainer.AddChild(new HorizontalLine(separatorLineColor));

			SettingsItem notificationWindowItem = new SettingsItem(
				"Print Completed Options".Localize(),	
				widgetFactories,
				null,
				itemClickedAction: configureNotificationSettingsButton_Click
			);

			notificationSettingsContainer.ChildSettings.Add(notificationWindowItem);
			mainContainer.AddChild(notificationWindowItem);
			//mainContainer.AddChild(new HorizontalLine(separatorLineColor));


            AddChild(mainContainer);

            SetCloudButtonVisiblity();
            
            AddHandlers();
        }

        private void SetDisplayAttributes()
        {
            //this.BackgroundColor = ActiveTheme.Instance.PrimaryBackgroundColor;
            this.Margin = new BorderDouble(2, 4, 2, 0);
            this.textImageButtonFactory.normalFillColor = RGBA_Bytes.White;
            this.textImageButtonFactory.disabledFillColor = RGBA_Bytes.White;

            this.textImageButtonFactory.FixedHeight = TallButtonHeight;
            this.textImageButtonFactory.fontSize = 11;

            this.textImageButtonFactory.disabledTextColor = RGBA_Bytes.DarkGray;
            this.textImageButtonFactory.hoverTextColor = ActiveTheme.Instance.PrimaryTextColor;
            this.textImageButtonFactory.normalTextColor = RGBA_Bytes.Black;
            this.textImageButtonFactory.pressedTextColor = ActiveTheme.Instance.PrimaryTextColor;

            this.linkButtonFactory.fontSize = 11;
        }

        

        public delegate void OpenDashboardPage(object state);
        public static OpenDashboardPage openDashboardPageFunction = null;
        void goCloudMonitoringWebPageButton_Click(object sender, EventArgs mouseEvent)
        {
            if (openDashboardPageFunction != null)
            {
                UiThread.RunOnIdle((state) =>
                {
                    openDashboardPageFunction(null);
                });
            }
        }

        public delegate void OpenInstructionsPage(object state);
        public static OpenInstructionsPage openInstructionsPageFunction = null;
        void goCloudMonitoringInstructionsButton_Click(object sender, EventArgs mouseEvent)
        {
            if (openDashboardPageFunction != null)
            {
                UiThread.RunOnIdle((state) =>
                {
                    openInstructionsPageFunction(null);
                });
            }
        }

        void SetCloudButtonVisiblity()
        {
            bool cloudMontitorEnabled = (PrinterSettings.Instance.get("CloudMonitorEnabled") == "true");
            //enableCloudMonitorButton.Visible = !cloudMontitorEnabled;
            //disableCloudMonitorButton.Visible = cloudMontitorEnabled;
            //goCloudMonitoringWebPageButton.Visible = cloudMontitorEnabled;

        }

        public override void OnClosed(EventArgs e)
        {
            if (unregisterEvents != null)
            {
                unregisterEvents(this, null);
            }
            base.OnClosed(e);
        }

        event EventHandler unregisterEvents;
        private void AddHandlers()
        {
            PrinterConnectionAndCommunication.Instance.CommunicationStateChanged.RegisterEvent(onPrinterStatusChanged, ref unregisterEvents);
            PrinterConnectionAndCommunication.Instance.EnableChanged.RegisterEvent(onPrinterStatusChanged, ref unregisterEvents);
        }

        private void onPrinterStatusChanged(object sender, EventArgs e)
        {
            SetVisibleControls();
            this.Invalidate();
        }

        public delegate void OpenNotificationFormWindow(object state);
        public static OpenNotificationFormWindow openPrintNotificationFunction = null;
        void configureNotificationSettingsButton_Click()
        {
            if (openPrintNotificationFunction != null)
            {
                UiThread.RunOnIdle((state) =>
                {
                    openPrintNotificationFunction(null);
                });
            }
        }

        private void SetVisibleControls()
        {
			/*
            if (ActivePrinterProfile.Instance.ActivePrinter == null)
            {
                // no printer selected                         
                cloudMonitorContainer.SetEnableLevel(DisableableWidget.EnableLevel.Disabled);
            }
            else // we at least have a printer selected
            {
                cloudMonitorContainer.SetEnableLevel(DisableableWidget.EnableLevel.Enabled);
            } */
        }
    }
}