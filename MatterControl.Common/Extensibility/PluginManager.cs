using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
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

			// Probing path
			string searchDirectory = Path.Combine(
								Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
								"Extensions");

			var pluginAssemblies = new List<string>(Directory.GetFiles(searchDirectory, "*.dll"));

			// HACK: Drop support for exe plugins - this very questionable -  why would anyone distribute a plugin as an 
			// executable and why would be want to support that scenario? It could be done - it's poor form and not appropriate
			pluginAssemblies.AddRange(Directory.GetFiles(searchDirectory, "*.exe"));

			Type pluginInterface = typeof(IApplicationPlugin);

			foreach (string assemblyPath in pluginAssemblies)
			{
				try
				{
					Assembly assembly = Assembly.LoadFile(assemblyPath);

					foreach (Type type in assembly.GetTypes().Where(t => t != null && t.IsClass && pluginInterface.IsAssignableFrom(t)))
					{
						if (!type.IsPublic)
						{
							// TODO: We need to be able to log this in a usable way - consider MatterControl terminal as output target?
							Trace.WriteLine("IApplicationPlugin exists but is not a Public Class: {0}", type.ToString());
							continue;
						}

						if (Disabled != null && Disabled.Contains(type.FullName))
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
					Trace.WriteLine(string.Format("An unexpected exception occurred while loading plugins: {0}\r\n{1}", assemblyPath, ex.Message));
				}
			}

			this.Plugins = plugins;

			/* Generated new knownPlugins.json file
			KnownPlugins = plugins.Where(p => p.MetaData != null).Select(p => new PluginState { TypeName = p.GetType().FullName, Name = p.MetaData.Name }).ToList();

			File.WriteAllText(
				"knownPlugins.json",
				JsonConvert.SerializeObject(KnownPlugins, Newtonsoft.Json.Formatting.Indented));
				*/
		}

		public List<IApplicationPlugin> Plugins { get; }

		//public Dictionary<string, PluginState> KnownPlugins { get; }
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

		public IEnumerable<T> FromType<T>() where T : class, IApplicationPlugin
		{
			return Plugins.Where(p => p is T).Select(p => p as T);
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
			NameValueCollection xxx = new NameValueCollection();
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
