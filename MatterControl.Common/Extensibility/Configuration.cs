using MatterHackers.Agg.Extensibility;
using MatterHackers.Agg.PlatformAbstract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MatterHackers.Agg.PlatformAbstract
{
	public enum OSType { Unknown, Windows, Mac, X11, Other, Android };

	// TODO: alternate names AggConfig, AggContext, AggPlatform
	// In all cases this is the compile time Agg config build by the host application
	public static class AggContext
	{
		// Takes a full typename string: i.e. "MyNamespace.MyType, MyAssembly"
		public static object LoadProviderFromAssembly(string typeString)
		{
			var type = Type.GetType(typeString);
			return (type == null) ? null : Activator.CreateInstance(type);
		}

		// Takes a full typename string: i.e. "MyNamespace.MyType, MyAssembly"
		public static T LoadProviderFromAssembly<T>(string typeString) where T : class
		{
			var type = Type.GetType(typeString);
			return (type == null) ? null : Activator.CreateInstance(type) as T;
		}

		public static IImageIOProvider ImageIO { get; }
		public static IFileDialogProvider FileDialogs { get; }
		public static IFrostedSerialPortFactory FrostedSerialPortFactory { get; }
		public static IStaticData StaticData { get; }
		public static PlatformConfig Config { get; }
		public static OSType OperatingSystem { get; }

		private static PluginManager pluginManager = null;
		public static PluginManager Plugins
		{
			get
			{
				// PluginManager initialization must occur late, after the config is loaded and after localization libraries
				// have occurred, which currently is driven by MatterControlApplication init
				if (pluginManager == null)
				{
					pluginManager = new PluginManager();
				}

				return pluginManager;
			}
		}

		static AggContext()
		{
			if(File.Exists("AggPlatform.json"))
			{
				// Use the config file from the file system if it exists
				Config = Newtonsoft.Json.JsonConvert.DeserializeObject<PlatformConfig>(File.ReadAllText("AggPlatform.json"));
			}
			else
			{
				// Use the default config embedded in the assembly if the file system override does not exist
				using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("MatterHackers.Agg.Extensibility.AggPlatform.json")))
				{
					Config = Newtonsoft.Json.JsonConvert.DeserializeObject<PlatformConfig>(reader.ReadToEnd());
				}
			}

			var providerTypes = Config.ProviderTypes;

			// OsInformation Provider
			var osInformation = LoadProviderFromAssembly<IOsInformationProvider>(providerTypes.OsInformationProvider);
			if (osInformation == null)
			{
				throw new Exception(string.Format("Unable to load the OsInformation provider"));
			}

			OperatingSystem = osInformation.OperatingSystem;

			// ImageIO Provider
			ImageIO = LoadProviderFromAssembly<IImageIOProvider>(providerTypes.ImageIOProvider);
			if (ImageIO == null)
			{
				throw new Exception(string.Format("Unable to load the ImageIO provider"));
			}

			// FileDialog Provider
			FileDialogs = LoadProviderFromAssembly<IFileDialogProvider>(providerTypes.DialogProvider);
			if (FileDialogs == null)
			{
				throw new Exception(string.Format("Unable to load the File Dialog provider"));
			}

			// FileDialog Provider
			StaticData = LoadProviderFromAssembly<IStaticData>(providerTypes.StaticDataProvider);
			if (StaticData == null)
			{
				throw new Exception(string.Format("Unable to load the StaticData provider"));
			}
		}

		public class PlatformConfig
		{
			public ProviderSettings ProviderTypes { get; set; }
			public SliceEngineSettings SliceEngine { get; set; } = new SliceEngineSettings();
		}

		public class ProviderSettings
		{
			public string OsInformationProvider { get; set; }
			public string DialogProvider { get; set; }
			public string ImageIOProvider { get; set; }
			public string StaticDataProvider { get; set; }
			public string SystemWindowProvider { get; set; }
		}

		public class SliceEngineSettings
		{
			public bool RunInProcess { get; set; } = false;
		}
	}
}
