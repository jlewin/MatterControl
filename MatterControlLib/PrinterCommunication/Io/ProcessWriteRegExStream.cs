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

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MatterHackers.MatterControl.PrinterCommunication.Io
{
	/// <summary>
	/// Provides a G-code stream proxy that processes outgoing G-code lines using regular expressions to perform write-time
	/// replacements based on printer configuration settings.
	/// </summary>
	/// <remarks>This stream intercepts each line of G-code before it is sent to the printer and applies any
	/// configured write-line regular expression replacements. If a replacement results in multiple output lines,
	/// additional lines are queued for subsequent processing. This class is typically used to support advanced printer
	/// scripting or macro expansion scenarios where G-code output must be dynamically modified.</remarks>
	public class ProcessWriteRegexStream : GCodeStreamProxy
	{
		public static Regex GetQuotedParts { get; } = new Regex(@"([""'])(\\?.)*?\1", RegexOptions.Compiled);

		private QueuedCommandsStream queueStream;

		public override string DebugInfo => "";

		public ProcessWriteRegexStream(PrinterConfig printer, GCodeStream internalStream, QueuedCommandsStream queueStream)
			: base(printer, internalStream)
		{
			this.queueStream = queueStream;
		}

		public override string ReadLine()
		{
			var baseLine = base.ReadLine();

			if (baseLine == null)
			{
				return null;
			}

			if (baseLine.EndsWith("; NO_PROCESSING"))
			{
				return baseLine;
			}

			// if the line has no content don't process it
			if (baseLine.Length == 0
				|| baseLine.Trim().Length == 0)
			{
				return baseLine;
			}

			var lines = ProcessWriteRegEx(baseLine, printer);
			for (int i = lines.Count - 1; i >= 1; i--)
			{
				queueStream.Add(lines[i], true);
			}

			var lineToSend = lines[0];

			return lineToSend;
		}

		public static List<string> ProcessWriteRegEx(string lineToWrite, PrinterConfig printer)
		{
			var linesToWrite = new List<string>
			{
				lineToWrite
			};

			var addedLines = new List<string>();
			for (int i = 0; i < linesToWrite.Count; i++)
			{
				foreach (var item in printer.Settings.Helpers.WriteLineReplacements)
				{
					var splitReplacement = item.Replacement.Split(',');
					if (splitReplacement.Length > 0)
					{
						if (item.Regex.IsMatch(lineToWrite))
						{
							// replace on the first replacement group only
							var replacedString = item.Regex.Replace(lineToWrite, splitReplacement[0]);
							linesToWrite[i] = replacedString;
							// add in the other replacement groups
							for (int j = 1; j < splitReplacement.Length; j++)
							{
								addedLines.Add(splitReplacement[j]);
							}

							break;
						}
					}
				}
			}

			linesToWrite.AddRange(addedLines);

			return linesToWrite;
		}
	}
}