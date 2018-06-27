// Copyright (c) 2016-2017 Nicolas Musset. All rights reserved.
// This file is licensed under the MIT license.
// See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MatterHackers.MatterControl;
using Newtonsoft.Json;

namespace Markdig.Agg
{
	#region MediaWiki Api Types
	public class WikiQueryResult
	{
		public WikiQueryDetails query { get; set; }
	}

	public class WikiQueryDetails
	{
		public List<WikiPageResult> pages { get; set; }
	}

	public class ImageQueryResult
	{
		public ImageQueryDetails query { get; set; }
	}

	public class ImageQueryDetails
	{
		public Dictionary<string, WikiPageResult> pages { get; set; }
	}

	public class WikiPageResult
	{
		public int pageid { get; set; }
		public int ns { get; set; }
		public string title { get; set; }
		public List<PageRevision> revisions { get; set; }
		public List<ImageUrl> imageinfo { get; set; }
	}

	public class ImageUrl
	{
		public string url { get; set; }
		public string descriptionurl { get; set; }
	}

	public class PageRevision
	{
		public string contentformat { get; set; }
		public string contentmodel { get; set; }
		public string content { get; set; }
	}
	#endregion



	public static class SupportSiteCollector
	{
		private static string outputDirectory = @"c:\temp\wiki_import";
		private static HttpClient client;

		static SupportSiteCollector()
		{
			JsonConvert.DefaultSettings = () => new JsonSerializerSettings
			{
				Formatting = Newtonsoft.Json.Formatting.Indented,
				ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
			};

			client = new HttpClient();

			Directory.CreateDirectory(outputDirectory);
		}

		private static Dictionary<string, string> urlMap = new Dictionary<string, string>();

		public static void BuildDocumentsTree()
		{
			var root = ProcTree(@"C:\Temp\wiki_import\Site\docs");
			File.WriteAllText(@"c:\temp\guides2.json", JsonConvert.SerializeObject(root, Formatting.Indented));
		}

		private static HelpContainer ProcTree(string directoryPath)
		{
			var directoryInfo = new DirectoryInfo(directoryPath);

			var container = new HelpContainer()
			{
				Name = directoryInfo.Name
			};

			foreach(var subdirectory in directoryInfo.GetDirectories())
			{
				container.Containers.Add(ProcTree(subdirectory.FullName));
			}

			foreach(var file in directoryInfo.GetFiles())
			{
				container.Items.Add(new GuideAsset()
				{
					MenuName = file.Name,
				});
			}

			return container;
		}

		public static void AppendMarkdownExtensions()
		{
			var allFiles = Directory.GetFiles(@"C:\Temp\wiki_import\Site\docs", "*.md", SearchOption.AllDirectories);

			var documentNames = new HashSet<string>(allFiles.Select(f => Path.GetFileNameWithoutExtension(f)));

			foreach(var filePath in allFiles)
			{
				string markdownText = File.ReadAllText(filePath);

				var document = new MarkdownDocument(new Uri("http://matterhackers.com"))
				{
					Markdown = markdownText
				};

				document.Parse();

				foreach (var link in document.Children)
				{
					if (link.LinkInline.Url.Contains("#"))
					{
						string leftValue = link.LinkInline.Url;
						string rightValue = "";

						int splitPosition = leftValue.IndexOf("#");
						if (splitPosition > 0)
						{
							rightValue = leftValue.Substring(splitPosition);
							leftValue = leftValue.Substring(0, splitPosition) + ".md";

							string linkText = leftValue + rightValue;

							markdownText = Regex.Replace(markdownText, $"\\(\\s*{link.LinkInline.Url}", $"(" + linkText);
						}
					}
				}

				File.WriteAllText(filePath, markdownText);
			}
		}

		public async static Task ResolveLinks()
		{
			string apiUrl = $"http://wiki.mattercontrol.com/api.php";

			string imageApi = "http://wiki.mattercontrol.com/api.php?action=query&titles=Image:{0}&prop=imageinfo&iiprop=url&format=json";

			foreach (var filePath in Directory.GetFiles(@"C:\Temp\wiki_import\Site\docs"))
			{
				string fileText = File.ReadAllText(filePath);

				var document = new MarkdownDocument(new Uri(apiUrl))
				{
					Markdown = fileText
				};

				document.Parse();

				foreach (var link in document.Children.Where(l => l.LinkInline.IsImage))
				{
					string lowerPageID = link.PageID.ToLower();

					if (!urlMap.TryGetValue(lowerPageID, out string actualUrl))
					{
						string apiJson = await client.GetStringAsync(string.Format(imageApi, link.PageID));

						var apiResult = JsonConvert.DeserializeObject<ImageQueryResult>(apiJson);

						actualUrl = apiResult.query?.pages?.Values?.FirstOrDefault()?.imageinfo?.FirstOrDefault()?.url;

						urlMap[lowerPageID] = actualUrl;
					}
					else
					{
						Console.WriteLine("hit: " + lowerPageID);
					}

					if (!string.IsNullOrEmpty(actualUrl))
					{
						fileText = fileText.Replace(link.PageID, actualUrl);
					}
				}

				File.WriteAllText(filePath, fileText);
			}
		}

		// string pageID = "Main_Page";
		public static async Task CollectWikiPage(string pageID)
		{
			pageID = pageID.Trim();
			string extension = Path.GetExtension(pageID);

			var altID = pageID.Replace("/", "__");

			if (pageID.StartsWith("Category:", StringComparison.OrdinalIgnoreCase)
				|| pageID == "#"
				|| (extension != "" && extension.Length < 5)
				|| File.Exists(Path.Combine(outputDirectory, $"{altID}.wiki.api"))
				|| File.Exists(Path.Combine(outputDirectory, $"{pageID}.wiki.api")))
			{
				return;
			}

			string apiUrl = $"http://wiki.mattercontrol.com/api.php?action=query&titles={pageID}&prop=revisions&rvprop=content&format=json&formatversion=2";

			string apiJson = await client.GetStringAsync(apiUrl);

			pageID = pageID.Replace("/", "__");

			File.WriteAllText(Path.Combine(outputDirectory, $"{pageID}.wiki.api"), apiJson);

			var apiResult = JsonConvert.DeserializeObject<WikiQueryResult>(apiJson);

			string rawWikiText = apiResult.query?.pages?.FirstOrDefault()?.revisions?.FirstOrDefault()?.content;

			if (rawWikiText == null)
			{
				File.AppendAllText(Path.Combine(outputDirectory, "pages_failed.txt"), pageID + "\r\n");
				return;
			}
			else
			{
				File.AppendAllText(Path.Combine(outputDirectory, "pages.txt"), pageID + "\r\n");
			}

			File.WriteAllText(Path.Combine(outputDirectory, $"{pageID}.wiki.txt"), rawWikiText);

			var convertedMarkdown = PandocConvert(rawWikiText);
			File.WriteAllText(Path.Combine(outputDirectory, $"{pageID}.md"), convertedMarkdown);

			var document = new MarkdownDocument(new Uri(apiUrl))
			{
				Markdown = convertedMarkdown
			};

			document.Parse();

			foreach (var link in document.Children)
			{
				File.WriteAllText(Path.Combine(outputDirectory, $"{pageID}.link"), JsonConvert.SerializeObject(link));

				await CollectWikiPage(link.PageID);
			}
		}

		private static string PandocConvert(string source)
		{
			string processName = Environment.ExpandEnvironmentVariables(@"%localappdata%\Pandoc\pandoc.exe");

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

			process.WaitForExit(4000);

			string pandocResult = "";
			using (var sr = new StreamReader(process.StandardOutput.BaseStream))
			{
				pandocResult = sr.ReadToEnd();
			}

			return pandocResult;
		}
	}
}
