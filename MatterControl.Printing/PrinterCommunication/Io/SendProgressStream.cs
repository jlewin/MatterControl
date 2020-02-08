﻿/*
Copyright (c) 2019, Tyler Anderson, John Lewin
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
using MatterHackers.MatterControl.SlicerConfiguration;

namespace MatterControl.Printing.Pipelines
{
	public class SendProgressStream : GCodeStreamProxy
	{
		private double nextPercent = -1;
		private PrinterConnection connection;

		public SendProgressStream(GCodeStream internalStream, PrinterConnection connection, PrinterSettings settings)
			: base(settings, internalStream)
		{
			this.connection = connection;
		}

		public override string DebugInfo => "";

		public override string ReadLine()
		{
			if (settings.GetValue(SettingsKey.progress_reporting) != "None"
				&& connection.CommunicationState == CommunicationStates.Printing
				&& connection.ActivePrintTask != null
				&& connection.ActivePrintTask.PercentDone > nextPercent)
			{
				nextPercent = Math.Round(connection.ActivePrintTask.PercentDone) + 0.5;
				if (settings.GetValue(SettingsKey.progress_reporting) == "M73")
				{
					return string.Format("M73 P{0:0}", connection.ActivePrintTask.PercentDone);
				}
				else
				{
					return string.Format("M117 Printing - {0:0}%", connection.ActivePrintTask.PercentDone);
				}
			}

			return base.ReadLine();
		}
	}
}
