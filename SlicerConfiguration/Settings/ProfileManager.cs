﻿/*
Copyright (c) 2016, Lars Brubaker, John Lewin
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
using System.IO;
using System.Linq;
using MatterHackers.Agg.UI;
using Newtonsoft.Json;

namespace MatterHackers.MatterControl.SlicerConfiguration
{
	using System.Collections.ObjectModel;
	using System.Threading.Tasks;
	using Agg;
	using DataStorage;
	using Localizations;
	using MatterHackers.CloudServices;
	using SettingsManagement;

	public class ProfileManager
	{
		public static RootedObjectEventHandler ProfilesListChanged = new RootedObjectEventHandler();

		public static ProfileManager Instance { get; set; }

		public const string ProfileExtension = ".printer";
		public const string ConfigFileExtension = ".slice";

		private static object writeLock = new object();
		private static EventHandler unregisterEvents;
		private static readonly string userDataPath = ApplicationDataStorage.ApplicationUserDataPath;

		/// <summary>
		/// The user specific path to the Profiles directory
		/// </summary>
		private static string ProfilesPath
		{
			get
			{
				// Determine username
				string username = ApplicationController.Instance.GetSessionUsernameForFileSystem();
				if (string.IsNullOrEmpty(username))
				{
					username = "guest";
				}

				// Append userName to ProfilesPath
				string path = Path.Combine(userDataPath, "Profiles", username);

				// Ensure directory exists
				Directory.CreateDirectory(path);

				return path;
			}
		}

		private const string userDBExtension = ".profiles";
		private const string guestDBFileName = "guest" + userDBExtension;

		private static string GuestDBPath => Path.Combine(ProfilesPath, guestDBFileName);

		internal static string ProfilesDBPath
		{
			get
			{
				string username = UserSettings.Instance.get("ActiveUserName");
				if (string.IsNullOrEmpty(username))
				{ 
					username = GuestDBPath;

					// If ActiveUserName is empty or invalid and the credentials file exists, delete local credentials, resetting to unauthenticated guest mode
					ApplicationController.ClearCachedCredentials();
				}
				else
				{
					username = Path.Combine(ProfilesPath, $"{username}{userDBExtension}");
				}
				return username;
			}
		}

		static ProfileManager()
		{
			SliceSettingsWidget.SettingChanged.RegisterEvent(SettingsChanged, ref unregisterEvents);

			// Ensure the profiles directory exists
			Directory.CreateDirectory(ProfilesPath);

			Reload();
		}

		public ProfileManager()
		{
		}

		[JsonIgnore]
		public bool IsGuestProfile => Path.GetFileName(ProfilesDBPath) == guestDBFileName;

		public static void Reload()
		{
			if (Instance?.Profiles != null)
			{
				// Release event registration
				Instance.Profiles.CollectionChanged -= Profiles_CollectionChanged;
			}

			// Load the profiles document
			if (File.Exists(ProfilesDBPath))
			{
				string json = File.ReadAllText(ProfilesDBPath);
				Instance = JsonConvert.DeserializeObject<ProfileManager>(json);
			}
			else
			{
				Instance = new ProfileManager();
			}

			if (ActiveSliceSettings.Instance?.ID != Instance.LastProfileID)
			{
				Task.Run(async () =>
				{
					// Load or download on a background thread
					var lastProfile = await LoadProfileAsync(Instance.LastProfileID);

					UiThread.RunOnIdle(() =>
					{
						// Assign on the UI thread
						ActiveSliceSettings.Instance = lastProfile ?? LoadEmptyProfile();
					});
				});
			}

			// In either case, wire up the CollectionChanged event
			Instance.Profiles.CollectionChanged += Profiles_CollectionChanged;
		}

		internal static ProfileManager LoadGuestDB()
		{
			if (File.Exists(GuestDBPath))
			{
				string json = File.ReadAllText(GuestDBPath);
				return JsonConvert.DeserializeObject<ProfileManager>(json);
			}

			return null;
		}

		internal static void SettingsChanged(object sender, EventArgs e)
		{
			string settingsKey = ((StringEventArgs)e).Data;
			switch (settingsKey)
			{
				case SettingsKey.printer_name:
					Instance.ActiveProfile.Name = ActiveSliceSettings.Instance.GetValue(SettingsKey.printer_name);
					Instance.Save();
					break;

				case SettingsKey.com_port:
					Instance.ActiveProfile.ComPort = ActiveSliceSettings.Instance.Helpers.ComPort();
					Instance.Save();
					break;
			}
		}

		public ObservableCollection<PrinterInfo> Profiles { get; set; } = new ObservableCollection<PrinterInfo>();

		[JsonIgnore]
		public IEnumerable<PrinterInfo> ActiveProfiles => Profiles.Where(profile => !profile.MarkedForDelete).ToList();

		[JsonIgnore]
		public PrinterInfo ActiveProfile => this[ActiveSliceSettings.Instance.ID];

		public PrinterInfo this[string profileID]
		{
			get
			{
				return Profiles.Where(p => p.ID == profileID).FirstOrDefault();
			}
		}

		public static PrinterSettings LoadEmptyProfile()
		{
			var emptyProfile = new PrinterSettings() { ID = "EmptyProfile" };
			emptyProfile.UserLayer[SettingsKey.printer_name] = "Printers...".Localize();

			return emptyProfile;
		}

		[JsonIgnore]
		public string LastProfileID
		{
			get
			{
				string activeUserName = UserSettings.Instance.get("ActiveUserName");
				return UserSettings.Instance.get($"ActiveProfileID-{activeUserName}");
			}
		}

		public bool PrintersImported { get; set; } = false;

		public PrinterSettings LoadLastProfileWithoutRecovery()
		{
			return LoadWithoutRecovery(this.LastProfileID);
		}

		public void SetLastProfile(string printerID)
		{
			string activeUserName = UserSettings.Instance.get("ActiveUserName");
			UserSettings.Instance.set($"ActiveProfileID-{activeUserName}", printerID);
		}

		public string ProfilePath(PrinterInfo printer)
		{
			return Path.Combine(ProfileManager.ProfilesPath, printer.ID + ProfileExtension);
		}

		public string ProfilePath(string printerID)
		{
			return ProfilePath(this[printerID]);
		}

		public static PrinterSettings LoadWithoutRecovery(string profileID)
		{
			string profilePath = Instance[profileID]?.ProfilePath;
			if (profilePath != null && File.Exists(profilePath))
			{
				try
				{
					return PrinterSettings.LoadFile(profilePath);
				}
				catch
				{
					return null;
				}
			}

			return null;
		}

		/// <summary>
		/// Loads the specified PrinterProfile, performing recovery options if required
		/// </summary>
		/// <param name="profileID">The profile ID to load</param>
		/// <param name="useActiveInstance">Return the in memory instance if already loaded. Alternatively, reload from disk</param>
		/// <returns></returns>
		public static async Task<PrinterSettings> LoadProfileAsync(string profileID, bool useActiveInstance = true)
		{
			if (useActiveInstance && ActiveSliceSettings.Instance?.ID == profileID)
			{
				return ActiveSliceSettings.Instance;
			}

			// Only load profiles by ID that are defined in the profiles document
			var printerInfo = ProfileManager.Instance[profileID];
			if (printerInfo == null)
			{
				return null;
			}

			// Attempt to load from disk, pull from the web or fall back using recovery logic
			PrinterSettings printerSettings = LoadWithoutRecovery(profileID);
			if (printerSettings != null)
			{
				return printerSettings;
			}
			else
			{
				// Attempt to load from MCWS if missing on disk
				printerSettings = await ApplicationController.WebServices.Devices.GetPrinterProfileAsync(printerInfo);
				if (printerSettings != null)
				{
					// If successful, persist downloaded profile and return
					printerSettings.Save();
					return printerSettings;
				}
			}

			// Otherwise attempt to recover to a working profile
			return await PrinterSettings.RecoverProfile(printerInfo);
		}

		internal static bool ImportFromExisting(string settingsFilePath)
		{
			if (string.IsNullOrEmpty(settingsFilePath) || !File.Exists(settingsFilePath))
			{
				return false;
			}

			var printerInfo = new PrinterInfo
			{
				Name = Path.GetFileNameWithoutExtension(settingsFilePath),
				ID = Guid.NewGuid().ToString()
			};

			bool importSuccessful = false;

			string importType = Path.GetExtension(settingsFilePath).ToLower();
			switch (importType)
			{
				case ProfileManager.ProfileExtension:
					// Add the Settings as a profile before performing any actions on it to ensure file paths resolve
					Instance.Profiles.Add(printerInfo);

					var profile = PrinterSettings.LoadFile(settingsFilePath);
					profile.ID = printerInfo.ID;
					profile.ClearValue(SettingsKey.device_token);
					printerInfo.DeviceToken = "";

					// TODO: Resolve name conflicts
					profile.Helpers.SetName(printerInfo.Name);

					profile.Save();
					importSuccessful = true;
					break;

				case ".ini":
					//Scope variables
					{
						var settingsToImport = PrinterSettingsLayer.LoadFromIni(settingsFilePath);
						var layeredProfile = new PrinterSettings()
						{
							ID = printerInfo.ID,
						};

						bool containsValidSetting = false;
						var activeSettings = layeredProfile;

						foreach (var item in settingsToImport)
						{
							if (activeSettings.Contains(item.Key))
							{
								containsValidSetting = true;
								string currentValue = activeSettings.GetValue(item.Key).Trim();
								// Compare the value to import to the layer cascade value and only set if different
								if (currentValue != item.Value)
								{
									activeSettings.OemLayer[item.Key] = item.Value;
								}
							}
						}
						if(containsValidSetting)
						{
							// TODO: Resolve name conflicts
							layeredProfile.UserLayer[SettingsKey.printer_name] = printerInfo.Name;

							layeredProfile.ClearValue(SettingsKey.device_token);
							printerInfo.DeviceToken = "";
							Instance.Profiles.Add(printerInfo);

							layeredProfile.Save();
							importSuccessful = true;
						}
					}
					break;
			}
			return importSuccessful;
		}

		internal static async Task AcquireNewProfile(string make, string model, string printerName)
		{
			string guid = Guid.NewGuid().ToString();

			var publicDevice = OemSettings.Instance.OemProfiles[make][model];
			
			// TODO: jlewin - how can we handle lookup failures at this point? Should we throw and check for the exception?
			//if (publicDevice == null)

			var printerSettings = await LoadOemProfileAsync(publicDevice, make, model);
			printerSettings.ID = guid;
			printerSettings.DocumentVersion = PrinterSettings.LatestVersion;

			printerSettings.UserLayer[SettingsKey.printer_name.ToString()] = printerName;

			// Import named macros as defined in the following printers: (Airwolf Axiom, HD, HD-R, HD2x, HDL, HDx, Me3D Me2, Robo R1[+])
			var classicDefaultMacros = printerSettings.GetValue("default_macros");
			if (!string.IsNullOrEmpty(classicDefaultMacros))
			{
				var namedMacros = new Dictionary<string, string>();
				namedMacros["Lights On"] = "M42 P6 S255";
				namedMacros["Lights Off"] = "M42 P6 S0";
				namedMacros["Offset 0.8"] = "M565 Z0.8;\nM500";
				namedMacros["Offset 0.9"] = "M565 Z0.9;\nM500";
				namedMacros["Offset 1"] = "M565 Z1;\nM500";
				namedMacros["Offset 1.1"] = "M565 Z1.1;\nM500";
				namedMacros["Offset 1.2"] = "M565 Z1.2;\nM500";
				namedMacros["Z Offset"] = "G1 Z10;\nG28;\nG29;\nG1 Z10;\nG1 X5 Y5 F4000;\nM117;";

				foreach (string namedMacro in classicDefaultMacros.Split(','))
				{
					string gcode;
					if (namedMacros.TryGetValue(namedMacro.Trim(), out gcode))
					{
						printerSettings.Macros.Add(new GCodeMacro()
						{
							Name = namedMacro.Trim(),
							GCode = gcode
						});
					}
				}
			}

			// Add to Profiles - fires ProfileManager.Save due to ObservableCollection event listener
			Instance.Profiles.Add(new PrinterInfo
			{
				Name = printerName,
				ID = guid,
				Make = make,
				Model = model
			});

			// Persist changes to PrinterSettings - must come after adding to Profiles above
			printerSettings.Save();

			// Set as active profile
			UserSettings.Instance.set("ActiveProfileID", guid);

			ActiveSliceSettings.Instance = printerSettings;
		}

		public static List<string> ThemeIndexNameMapping = new List<string>()
		{
			"Blue - Dark",
			"Teal - Dark",
			"Green - Dark",
			"Light Blue - Dark",
			"Orange - Dark",
			"Purple - Dark",
			"Red - Dark",
			"Pink - Dark",
			"Grey - Dark",
			"Pink - Dark",

			//Light themes
			"Blue - Light",
			"Teal - Light",
			"Green - Light",
			"Light Blue - Light",
			"Orange - Light",
			"Purple - Light",
			"Red - Light",
			"Pink - Light",
			"Grey - Light",
			"Pink - Light",
		};

		public async static Task<PrinterSettings> LoadOemProfileAsync(PublicDevice publicDevice, string make, string model)
		{
			string cacheScope = Path.Combine("public-profiles", make);
			string cacheKey = publicDevice.CacheKey();
			string cachePath = ApplicationController.CacheablePath(cacheScope, cacheKey);

			return await ApplicationController.LoadCacheableAsync<PrinterSettings>(
				cacheKey,
				cacheScope,
				async () =>
				{
					// The collector specifically returns null to ensure LoadCacheable skips writing the
					// result to the cache. After this result is returned, it will attempt to load from
					// the local cache if the collector yielded no result
					if(File.Exists(cachePath))
					{
						return null;
					}
					else
					{
						// If the cache file for the current deviceToken does not exist, attempt to download it.
						// An http 304 results in a null value and LoadCacheable will then load from the cache
						return await ApplicationController.WebServices.Devices.DownloadPublicProfileAsync(publicDevice.ProfileToken);
					}
				},
				Path.Combine("Profiles", make, model + ProfileManager.ProfileExtension));
		}

		public void EnsurePrintersImported()
		{
			if (IsGuestProfile && !PrintersImported)
			{
				// Import Sqlite printer profiles into local json files
				DataStorage.ClassicDB.ClassicSqlitePrinterProfiles.ImportPrinters(Instance, ProfilesPath);
				PrintersImported = true;
				Save();
			}
		}

		private static void Profiles_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			// Any time the list changes, persist the updates to disk
			Instance.Save();

			ProfilesListChanged.CallEvents(null, null);
		}

		public void Save()
		{
			lock(writeLock)
			{
				File.WriteAllText(ProfilesDBPath, JsonConvert.SerializeObject(this, Formatting.Indented));
			}
		}
	}
}