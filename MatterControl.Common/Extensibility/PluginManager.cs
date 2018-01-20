using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using MatterHackers.Agg.UI;
using Newtonsoft.Json;

namespace MatterHackers.MatterControl.Extensibility
{
	public class PluginManager
	{
		private string pluginStateFile = "DisabledPlugins.json";
		private string knownPluginsFile = "KnownPlugins.json";

		public PluginManager()
		{
			if (File.Exists(pluginStateFile))
			{
				try
				{
					this.Disabled = JsonConvert.DeserializeObject<HashSet<string>>(File.ReadAllText(pluginStateFile));
				}
				catch
				{
				}
			}
			else
			{
				this.Disabled = new HashSet<string>();
			}

			if (File.Exists(knownPluginsFile))
			{
				try
				{
					this.KnownPlugins = JsonConvert.DeserializeObject<List<PluginState>>(File.ReadAllText(knownPluginsFile));
				}
				catch
				{
				}
			}

			var plugins = new List<IApplicationPlugin>();

			// Extensions path
			string searchDirectory = Path.Combine(
								Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
								"Extensions");

			var pluginAssemblies = Directory.GetFiles(searchDirectory, "*.dll").Select(s => Assembly.LoadFile(s)).ToList();

			Type applicationPlugin = typeof(IApplicationPlugin);

			foreach (var assembly in pluginAssemblies)
			{
				try
				{
					foreach (Type type in assembly.GetTypes().Where(t => t != null
						&& t.IsClass
						&& applicationPlugin.IsAssignableFrom(t)))
					{
						if (!type.IsPublic)
						{
							// TODO: We need to be able to log this in a usable way - consider MatterControl terminal as output target?
							Trace.WriteLine("IApplicationPlugin exists but is not a Public Class: {0}", type.ToString());
							continue;
						}

						if (Disabled?.Contains(type.FullName) == true)
						{
							continue;
						}

						Console.WriteLine("Loading Plugin: " + type.FullName);

						var instance = Activator.CreateInstance(type) as IApplicationPlugin;
						if (instance == null)
						{
							// TODO: We need to be able to log this in a usable way - consider MatterControl terminal as output target?
							Trace.WriteLine("Unable to create Plugin Instance: {0}", type.ToString());
							continue;
						}

						plugins.Add(instance);
					}
				}
				catch (Exception ex)
				{
					Trace.WriteLine(string.Format("An unexpected exception occurred while loading plugins: {0}\r\n{1}", assembly.FullName, ex.Message));
				}
			}

			this.Plugins = plugins;

			/*
			// Uncomment to generate new KnownPlugins.json file
			KnownPlugins = plugins.Where(p => p.MetaData != null).Select(p => new PluginState { TypeName = p.GetType().FullName, Name = p.MetaData.Name }).ToList();

			File.WriteAllText(
				Path.Combine("..", "..", "knownPlugins.json"),
				JsonConvert.SerializeObject(KnownPlugins, Formatting.Indented)); */

		}

		public List<IApplicationPlugin> Plugins { get; }

		public List<PluginState> KnownPlugins { get; }

		public class PluginState
		{
			public string Name { get; set; }
			public string TypeName { get; set; }
			//public bool Enabled { get; set; }
			//public bool UpdateAvailable { get; set; }
		}

		public HashSet<string> Disabled { get; }

		public void Disable(string typeName) => Disabled.Add(typeName);

		public void Enable(string typeName) => Disabled.Remove(typeName);

		public void Save()
		{
			File.WriteAllText(
				pluginStateFile,
				JsonConvert.SerializeObject(Disabled, Formatting.Indented));
		}

		public void InitializePlugins(SystemWindow systemWindow)
		{
			foreach (var plugin in this.Plugins)
			{
				plugin.Initialize(systemWindow);
			}
		}

		public IEnumerable<TResult> OfType<TResult>() where TResult : class, IApplicationPlugin
		{
			return this.Plugins.OfType<TResult>();
		}

		public class MatterControlPluginItem
		{
			public string Name { get; set; }
			public string Url { get; set; }
			public string Version { get; set; }
			public DateTime ReleaseDate { get; set; }
		}

		private string dumpPath = @"C:\Data\Sources\MatterHackers\MatterControl\PluginRequestResults.json";

		public void GeneratePluginItems()
		{
			var source = new MatterControlPluginItem[]{
				new MatterControlPluginItem(){
					Name = "Test1",
					ReleaseDate = DateTime.Parse("12/1/2001"),
					Url = "http://something/1",
					Version = "1.2.3"
				},
				new MatterControlPluginItem(){
					Name = "Test2",
					ReleaseDate = DateTime.Parse("12/2/2001"),
					Url = "http://something/4",
					Version = "1.2.3"
				},
				new MatterControlPluginItem(){
					Name = "Test3",
					ReleaseDate = DateTime.Parse("12/3/2001"),
					Url = "http://something/2",
					Version = "1.0.0"
				}
			};

			File.WriteAllText(dumpPath, Newtonsoft.Json.JsonConvert.SerializeObject(source));
		}

		public void QueryPluginSource()
		{
			string sourceUrl = "http://someurl";

			WebClient client = new WebClient();

			// Build request keys
			var xxx = new NameValueCollection();
			xxx.Add("userToken", "xxx");

			// Perform request
			var results = client.UploadValues(sourceUrl, xxx);

			var pluginsText = File.ReadAllText(dumpPath);

			// Work with results
			var plugins = Newtonsoft.Json.JsonConvert.DeserializeObject<MatterControlPluginItem[]>(pluginsText);

			// Likely display results

			// or

			// Compare the results looking for package upgrades

			// Display a notification that updates are available
		}
	}
}
