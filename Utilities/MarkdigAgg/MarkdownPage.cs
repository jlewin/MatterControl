// Copyright (c) 2016-2017 Nicolas Musset. All rights reserved.
// This file is licensed under the MIT license.
// See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using Markdig.Renderers;
using MatterHackers.Agg.UI;
using MatterHackers.MatterControl;
using Newtonsoft.Json;

namespace Markdig.Agg
{

	public class WikiQueryResult
	{
		public WikiQueryDetails query { get; set; }
	}

	public class WikiQueryDetails
	{
		public List<WikiPageResult> pages { get; set; }
	}

	public class WikiPageResult
	{
		public int pageid { get; set; }
		public int ns { get; set; }
		public string title { get; set; }
		public List<PageRevision> revisions { get; set; }
	}

	public class PageRevision
	{
		public string contentformat { get; set; }
		public string contentmodel { get; set; }
		public string content { get; set; }
	}

	public class MarkdownPage : DialogPage
	{
		public MarkdownPage()
		{
			this.WindowTitle = this.HeaderText = "Markdown Tests";
			//contentRow.AddChild(new MarkdownWidget(new Uri("https://raw.githubusercontent.com/lunet-io/markdig/master/"),
			//	new Uri("https://raw.githubusercontent.com/lunet-io/markdig/master/readme.md")));
		}

		public override void OnLoad(EventArgs args)
		{
			base.OnLoad(args);

			var client = new HttpClient();

			string url = "http://wiki.mattercontrol.com/api.php?action=query&titles=Main_Page&prop=revisions&rvprop=content&format=json&formatversion=2";

			client.GetStringAsync(url).ContinueWith(t =>
			{
				var json = t.Result;

				var xxx = JsonConvert.DeserializeObject<WikiQueryResult>(json);

				string raw = xxx.query.pages.FirstOrDefault().revisions.FirstOrDefault().content;
				var yyy = PandocConvert(raw);

				var document = new MarkdownDocument(new Uri(url))
				{
					Markdown = yyy
				};
			});
		}

		public string PandocConvert(string source)
		{
			string processName = @"C:\Users\jlewin\AppData\Local\Pandoc\pandoc.exe";

			var process = new Process
			{
				StartInfo = new ProcessStartInfo(processName, "-f mediawiki -t commonmark --reference-links")
				{
					RedirectStandardOutput = true,
					RedirectStandardInput = true,
					UseShellExecute = false
				}
			};
			process.Start();

			byte[] inputBuffer = Encoding.UTF8.GetBytes(source);

			process.StandardInput.BaseStream.Write(inputBuffer, 0, inputBuffer.Length);
			process.StandardInput.Close();

			process.WaitForExit(2000);
			string xxx = "";
			using (var sr = new StreamReader(process.StandardOutput.BaseStream))
			{
				xxx = sr.ReadToEnd();
			}

			return xxx;
		}


	}
}
