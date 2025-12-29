/*
Copyright (c) 2025, Lars Brubaker, John Lewin
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

using MatterHackers.Agg.UI;

namespace MatterHackers.MatterControl.PrinterCommunication.Io
{
	/// <summary>
	/// Provides a G-code stream proxy that periodically injects temperature request commands (M105) into the stream based
	/// on printer connection state and timing conditions.
	/// </summary>
	/// <remarks>This stream is typically used to ensure that temperature readings are requested from the printer at
	/// regular intervals when the printer is connected and temperature monitoring is enabled. It wraps an existing
	/// GCodeStream and automatically issues M105 commands as needed, without requiring manual intervention.</remarks>
	public class RequestTemperaturesStream : GCodeStreamProxy
	{
		private long nextReadTimeMs = 0;

		public RequestTemperaturesStream(PrinterConfig printer, GCodeStream internalStream)
			: base(printer, internalStream)
		{
			nextReadTimeMs = UiThread.CurrentTimerMs + 1000;
		}

		public override string DebugInfo => "";

		public override string ReadLine()
		{
			if (!printer.Connection.WaitingForPositionRead
				&& nextReadTimeMs < UiThread.CurrentTimerMs
				&& printer.Connection.IsConnected
				&& printer.Connection.MonitorPrinterTemperature)
			{
				nextReadTimeMs = UiThread.CurrentTimerMs + 1000;
				return "M105";
			}

			return base.ReadLine();
		}
	}
}