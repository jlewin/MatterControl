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
	public class SettingsItem : DisableableWidget
	{
		public class WidgetFactory
		{
			public TextImageButtonFactory ButtonFactory {
				get;
				set;
			}

			public ToggleSwitchFactory SwitchFactory {
				get;
				set;
			}
		}

		public class ToggleSwitchConfig
		{
			public bool Checked {
				get;
				set;
			}

			public Action<bool> ToggleAction {
				get;
				set;
			}
		}

		public List<SettingsItem> ChildSettings {
			get;
			private set;
		}

		//sectionIconPath = Path.Combine("PrintStatusControls", "notify-24x24.png")
		// text = LocalizedString.Get("Notification Settings")
		public SettingsItem (string text, WidgetFactory factories, string sectionIconPath = null, ToggleSwitchConfig toggleSwitchConfig = null, Action itemClickedAction = null)
		{
			this.ChildSettings = new List<SettingsItem>();
			this.HAnchor = HAnchor.ParentLeftRight;
			this.Height = 40;

			FlowLayoutWidget flowLayout = new FlowLayoutWidget(FlowDirection.LeftToRight){
				HAnchor = HAnchor.ParentLeftRight,
				VAnchor = Agg.UI.VAnchor.ParentCenter,
				Margin = new BorderDouble(top: 1),
				Padding = new BorderDouble(0),
				Height = 40
			};
			this.AddChild(flowLayout);

			TextWidget sectionLabel = new TextWidget(text){
				AutoExpandBoundsToText = true,
				TextColor = ActiveTheme.Instance.PrimaryTextColor,
				VAnchor = VAnchor.ParentCenter
			};

			GuiWidget switchContainer = new FlowLayoutWidget(){
				VAnchor = VAnchor.ParentCenter,
				Margin = new BorderDouble(left: 16),
				Width = 45
			};

			ToggleSwitch toggleSwitch = null;
			if(toggleSwitchConfig != null)
			{
				toggleSwitch = GenerateToggleSwitch(switchContainer, factories, toggleSwitchConfig.Checked);
				toggleSwitch.SwitchStateChanged += (sender, e) => 
				{
					if(toggleSwitchConfig.ToggleAction != null)
					{
						toggleSwitchConfig.ToggleAction(toggleSwitch.SwitchState);
					}
				};
				switchContainer.SetBoundsToEncloseChildren();
			}

			MouseEventArgs mouseDown = null;

			ClickWidget clickOverlay = new ClickWidget(){ HAnchor = HAnchor.ParentLeftRight, VAnchor = VAnchor.ParentBottomTop };
			clickOverlay.MouseDown += (sender, mouseEvent) => {
				Console.WriteLine("{0}, {1}, {2}", mouseEvent.X, mouseEvent.Y, sender);
				mouseDown = mouseEvent;
			};
			clickOverlay.Click += (sender, e) => {
				
				if(itemClickedAction != null)
				{
					itemClickedAction();
				}
				else if(toggleSwitchConfig != null && toggleSwitch != null)
				{
					toggleSwitch.SwitchState = !toggleSwitch.SwitchState;
				};
			};

			if(sectionIconPath != null)
			{
				ImageBuffer sectionIconBuffer = StaticData.Instance.LoadIcon(sectionIconPath);
				if (!ActiveTheme.Instance.IsDarkTheme)
				{
					InvertLightness.DoInvertLightness(sectionIconBuffer);
				}
				ImageWidget icon = new ImageWidget(sectionIconBuffer){ Margin = new BorderDouble(right: 6,left: 6), VAnchor = VAnchor.ParentCenter};
				flowLayout.AddChild(icon);
#if DEBUG
				if(icon.LocalBounds.Height != 24 || icon.LocalBounds.Width != 24)
				{
					// Fix invalid icon size, should be 24x24
					System.Diagnostics.Debugger.Launch();
				}
#endif
			}
			else
			{
				// Add an icon place holder to get consistent label indenting on items lacking icons 
				flowLayout.AddChild(new GuiWidget(){
					Width = 24+12,
					Height = 24,
					Margin = new BorderDouble(0)
				});
			}

			// Add flag to align all labels - fill empty space if sectionIconPath is empty
			flowLayout.AddChild (sectionLabel, -1);
			flowLayout.AddChild(new HorizontalSpacer());
			flowLayout.AddChild(switchContainer);

			if(itemClickedAction != null)
			{
				Button actionButton = factories.ButtonFactory.GenerateAdditionalSettingsButton();
				actionButton.Margin = new BorderDouble(left: 6);
				actionButton.VAnchor = VAnchor.ParentCenter;
				actionButton.Click += (sender, e) => itemClickedAction();

				flowLayout.AddChild(actionButton);
			}

			this.AddChild(clickOverlay);
		}

		// Override SetEnableLevel to ensure child settings stay in sync
		public override void SetEnableLevel (EnableLevel enabledLevel)
		{
			// Cascade enabled level to child settings
			foreach(SettingsItem setting in this.ChildSettings)
			{
				setting.SetEnableLevel(enabledLevel);
			}

			base.SetEnableLevel(enabledLevel);
		}

		private ToggleSwitch GenerateToggleSwitch(GuiWidget parentContainer, WidgetFactory factories, bool initiallyChecked)
		{
			TextWidget toggleLabel = new TextWidget(initiallyChecked ? "On" : "Off", pointSize: 10, textColor: ActiveTheme.Instance.PrimaryTextColor);
			toggleLabel.VAnchor = Agg.UI.VAnchor.ParentCenter;
			toggleLabel.Margin = new BorderDouble (right: 4);

			ToggleSwitch toggleSwitch = factories.SwitchFactory.GenerateGivenTextWidget(toggleLabel, "On", "Off", initiallyChecked);
			toggleSwitch.VAnchor = Agg.UI.VAnchor.ParentCenter;
			toggleSwitch.SwitchState = initiallyChecked;

			parentContainer.AddChild(toggleLabel);
			parentContainer.AddChild(toggleSwitch);

			return toggleSwitch;
		}
	}
}