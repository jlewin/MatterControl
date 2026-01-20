using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MatterHackers.Agg;
using MatterHackers.Agg.Platform;
using MatterHackers.Agg.UI;
using MatterHackers.ImageProcessing;
using MatterHackers.MatterControl.PrinterCommunication;
using Zeroconf;
using System.Threading;
using System.Net.NetworkInformation;

namespace MatterHackers.MatterControl.SlicerConfiguration
{
	class IpAddressField : UIField
	{
		private DropDownList dropdownList;
		private ThemedIconButton refreshButton;

		private PrinterConfig printer;
		private ThemeConfig theme;

		public IpAddressField(PrinterConfig printer, ThemeConfig theme)
		{
			this.printer = printer;
			this.theme = theme;
		}

		public override void Initialize(ref int tabIndex)
		{
			base.Initialize(ref tabIndex);

			bool canChangeComPort = !printer.Connection.IsConnected && printer.Connection.CommunicationState != CommunicationStates.AttemptingToConnect;
			//This setting defaults to Manual
			var selectedMachine = printer.Settings.GetValue(SettingsKey.selector_ip_address);
			dropdownList = new MHDropDownList(selectedMachine, theme, maxHeight: 200 * GuiWidget.DeviceScale)
			{
				ToolTipText = HelpText,
				Margin = new BorderDouble(),
				TabIndex = tabIndex,
				VAnchor = VAnchor.Center,
				Enabled = canChangeComPort,
				TextColor = canChangeComPort ? theme.TextColor : new Color(theme.TextColor, 150),
			};

			// Create default option
			MenuItem defaultOption = dropdownList.AddItem("Manual", "127.0.0.1:23");
			defaultOption.Selected += (sender, e) =>
			{
				printer.Settings.SetValue(SettingsKey.selector_ip_address, defaultOption.Text);
			};

			// Prevent droplist interaction when connected
			void CommunicationStateChanged(object s, EventArgs e)
			{
				canChangeComPort = !printer.Connection.IsConnected && printer.Connection.CommunicationState != CommunicationStates.AttemptingToConnect;
				dropdownList.TextColor = theme.TextColor;
				dropdownList.Enabled = canChangeComPort;
			}

			printer.Connection.CommunicationStateChanged += CommunicationStateChanged;
			dropdownList.Closed += (s, e) => printer.Connection.CommunicationStateChanged -= CommunicationStateChanged;

			var widget = new FlowLayoutWidget();
			widget.AddChild(dropdownList);
			refreshButton = new ThemedIconButton(StaticData.Instance.LoadIcon("fa-refresh_14.png", 14, 14).GrayToColor(theme.TextColor), theme)
			{
				Margin = new BorderDouble(left: 5)
			};

			refreshButton.Click += (s, e) => RebuildMenuItems();
			widget.AddChild(refreshButton);

			this.Content = widget;
		
			UiThread.RunOnIdle(RebuildMenuItems);
		}

		protected override void OnValueChanged(FieldChangedEventArgs fieldChangedEventArgs)
		{
			dropdownList.SelectedLabel = this.Value;
			base.OnValueChanged(fieldChangedEventArgs);
		}

		private async void RebuildMenuItems()
		{
			refreshButton.Enabled = false;
			dropdownList.MenuItems.Clear();

			MenuItem defaultOption = dropdownList.AddItem("Manual", "127.0.0.1:23");
			defaultOption.Selected += (sender, e) =>
			{
				printer.Settings.SetValue(SettingsKey.selector_ip_address, defaultOption.Text);
			};

			try
			{
				IReadOnlyList<IZeroconfHost> possibleHosts = await ProbeForNetworkedTelnetConnections();
				foreach (IZeroconfHost host in possibleHosts)
				{
					// Add each found telnet host to the dropdown list
					IService service;
					bool exists = host.Services.TryGetValue("_telnet._tcp.local.", out service);
					int port = exists ? service.Port : 23;
					MenuItem newItem = dropdownList.AddItem(host.DisplayName, $"{host.IPAddress}:{port}"); // The port may be unnecessary
																										   // When the given menu item is selected, save its value back into settings
					newItem.Selected += (sender, e) =>
					{
						if (sender is MenuItem menuItem)
						{
							// this.SetValue(
							// menuItem.Text,
							// userInitiated: true);
							string[] ipAndPort = menuItem.Value.Split(':');
							printer.Settings.SetValue(SettingsKey.ip_address, ipAndPort[0]);
							printer.Settings.SetValue(SettingsKey.ip_port, ipAndPort[1]);
							printer.Settings.SetValue(SettingsKey.selector_ip_address, menuItem.Text);
						}
					};
				}
			}
			catch (Exception ex) {
				Console.WriteLine("Error in ProbeForNetworkedTelnetConnections: " + ex.Message);
			}

			refreshButton.Enabled = true;
		}

		public static async Task<IReadOnlyList<IZeroconfHost>> ProbeForNetworkedTelnetConnections()
		{
			try
			{
				// Pass the filtered interfaces to ZeroconfResolver
				return await ZeroconfResolver.ResolveAsync("_telnet._tcp.local.",
					scanTime: TimeSpan.FromSeconds(5),
					retries: 2,
					retryDelayMilliseconds: 2000,
					callback: null,
					cancellationToken: CancellationToken.None,
					netInterfacesToSendRequestOn: GetValidInterfaces());
			}
			catch (Exception)
			{
				// If filtering fails or no valid interfaces found, return empty list
				return new List<IZeroconfHost>();
			}
		}

		private static NetworkInterface[] GetValidInterfaces()
		{
			// Get only network interfaces that are operational and have IPv4 support
			return NetworkInterface.GetAllNetworkInterfaces()
				.Where(ni => ni.OperationalStatus == OperationalStatus.Up
						&& ni.NetworkInterfaceType != NetworkInterfaceType.Loopback
						&& ni.Supports(NetworkInterfaceComponent.IPv4))
				.ToArray();
		}
	}
}
