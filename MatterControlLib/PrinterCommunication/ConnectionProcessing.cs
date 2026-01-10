using MatterHackers.MatterControl.PrinterCommunication;
using MatterHackers.MatterControl.PrinterCommunication.Io;

namespace MatterControlLib.PrinterCommunication
{
	public record ConnectionProcessing
	{
		public GCodeStream StreamPipeline { get; set; }
		public IHeatableTarget HeatableTarget { get; set; }
		public IPausableTarget PausableTarget { get; set; }
		public ILevelingTarget LevelingTarget { get; set; }
		public IQueuedCommands QueuedCommands { get; set; }
	}
}
