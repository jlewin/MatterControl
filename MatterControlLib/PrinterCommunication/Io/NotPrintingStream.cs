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

using System.Threading;

namespace MatterHackers.MatterControl.PrinterCommunication.Io
{
	/// <summary>
	/// Represents a G-code stream that simulates a non-printing state for a 3D printer. This stream does not send or
	/// process any G-code commands and always returns empty responses.
	/// </summary>
	/// <remarks>Use this stream when a printer is not actively printing or when a placeholder G-code stream is
	/// required. It can be useful for testing, simulation, or disabling printing functionality without affecting the rest
	/// of the printer pipeline.</remarks>
	public class NotPrintingStream : GCodeStream
	{
		public NotPrintingStream(PrinterConfig printer)
			: base(printer)
		{
		}

		public override string ReadLine()
		{
			Thread.Sleep(100);
			return "";
		}

		public override void SetPrinterPosition(PrinterMove position)
		{
		}

		public override GCodeStream InternalStream => null;

		public override string DebugInfo => "";
	}
}