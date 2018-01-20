using System.Collections.Generic;

namespace MatterHackers.MatterControl.Extensibility
{
	public class PluginInfo
	{
		public PluginInfo()
		{
			this.Extras = new Dictionary<string, string>();
		}

		public string Name { get; set; }
		public string UUID { get; set; }
		public string About { get; set; }
		public string Developer { get; set; }
		public string Url { get; set; }
		public Dictionary<string, string> Extras { get; private set; }
	}

	public interface IApplicationPlugin
	{
		PluginInfo MetaData { get; }
		void Initialize(object application);
	}
}