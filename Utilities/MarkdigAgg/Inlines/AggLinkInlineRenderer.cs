﻿// Copyright (c) 2016-2017 Nicolas Musset. All rights reserved.
// This file is licensed under the MIT license.
// See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using Markdig.Agg;
using Markdig.Syntax.Inlines;
using MatterHackers.Agg;
using MatterHackers.Agg.Image;
using MatterHackers.Agg.Platform;
using MatterHackers.Agg.UI;
using MatterHackers.MatterControl;

namespace Markdig.Renderers.Agg.Inlines
{
	public class TextLinkX : FlowLayoutWidget
	{
		private LinkInline linkInline;

		public TextLinkX(LinkInline linkInline)
		{
			this.HAnchor = HAnchor.Fit;
			this.VAnchor = VAnchor.Fit;
			this.Cursor = Cursors.Hand;
			this.linkInline = linkInline;

		}

		public override void OnClick(MouseEventArgs mouseEvent)
		{
			if (linkInline.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
			{
				ApplicationController.Instance.LaunchBrowser(linkInline.Url);
			}
			else
			{
				// Inline link?
				Debugger.Break();
			}

			base.OnClick(mouseEvent);
		}

		public override void AddChild(GuiWidget childToAdd, int indexInChildrenList = -1)
		{
			if (childToAdd is TextWidget textWidget)
			{
				// Mark with underline if any character data exists
				textWidget.Underline = textWidget.Text.Trim().Length > 0;
			}

			// Allow link parent to own mouse events
			childToAdd.Selectable = false;

			base.AddChild(childToAdd, indexInChildrenList);
		}
	}

	public class ImageLinkSimpleX : FlowLayoutWidget
	{
		private static ImageBuffer icon = AggContext.StaticData.LoadIcon("internet.png", 16, 16);

		public ImageLinkSimpleX(string url)
		{
			this.HAnchor = HAnchor.Fit;
			this.VAnchor = VAnchor.Fit;
			this.Selectable = false;

			this.Url = url;

			if (true)
			{
				var imageSequence = new ImageSequence(icon);
				var sequenceWidget = new ImageSequenceWidget(imageSequence);
				this.AddChild(sequenceWidget);

				if (url.StartsWith("http"))
				{
					ApplicationController.Instance.DownloadToImageSequenceAsync(imageSequence, url);
				}
			}
			else
			{
				var imageBuffer = new ImageBuffer(icon);
				var imageWidget = new ImageWidget(imageBuffer);
				this.AddChild(imageWidget);

				if (url.StartsWith("http"))
				{
					ApplicationController.Instance.DownloadToImageAsync(imageBuffer, url, false);
				}
			}
		}

		public string Url { get; }
	}

	public class ImageLinkAdvancedX : FlowLayoutWidget
	{
		private static HttpClient client = new HttpClient();

		private static ImageBuffer icon = AggContext.StaticData.LoadIcon("internet.png", 16, 16);

		public string Url { get; }

		public ImageLinkAdvancedX(string url)
		{
			HAnchor = HAnchor.Fit;
			VAnchor = VAnchor.Fit;
			this.Url = url;

			var imageBuffer = new ImageBuffer(icon);
			var imageWidget = new ImageWidget(imageBuffer);

			this.AddChild(imageWidget);

			try
			{
				if (url.StartsWith("http"))
				{
					client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ContinueWith(task =>
					{
						var response = task.Result;

						if (response.IsSuccessStatusCode)
						{
							Console.WriteLine(task.Result.Headers);

							response.Content.ReadAsStreamAsync().ContinueWith(streamTask =>
							{
								//response.Headers.TryGetValues("", s[""] == "" ||
								if (string.Equals(Path.GetExtension(url), ".svg", StringComparison.OrdinalIgnoreCase))
								{
									// Load svg into SvgWidget, swap for ImageWidget
									try
									{
										var svgWidget = new SvgWidget(streamTask.Result, 1)
										{
											Border = 1,
											BorderColor = Color.YellowGreen
										};

										this.ReplaceChild(imageWidget, svgWidget);
									}
									catch (Exception svgEx)
									{
										Debug.WriteLine("Error loading svg: {0} :: {1}", url, svgEx.Message);
									}
								}
								else
								{
									// Load img
									if (!AggContext.ImageIO.LoadImageData(streamTask.Result, imageBuffer))
									{
										Debug.WriteLine("Error loading image: " + url);
									}
								}
							});
						}
					});
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}
	}

	/// <summary>
	/// A Agg renderer for a <see cref="LinkInline"/>.
	/// </summary>
	/// <seealso cref="Markdig.Renderers.Agg.AggObjectRenderer{Markdig.Syntax.Inlines.LinkInline}" />
	public class AggLinkInlineRenderer : AggObjectRenderer<LinkInline>
	{
		/// <inheritdoc/>
		protected override void Write(AggRenderer renderer, LinkInline link)
		{
			var url = link.GetDynamicUrl != null ? link.GetDynamicUrl() ?? link.Url : link.Url;

			if (!Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
			{
				url = "#";
			}

			if (!url.StartsWith("http"))
			{
				var pageID = url;

				url = new Uri(renderer.BaseUri, url).AbsoluteUri;

				renderer.ChildLinks.Add(new MarkdownDocumentLink()
				{
					Uri = new Uri(url),
					LinkInline = link,
					PageID = pageID
				});
			}

			if (link.IsImage) //link.IsImage)
			{
				renderer.WriteInline(new ImageLinkSimpleX(url)); // new ImageLinkAdvancedX(url)); //new InlineUIContainer(image));
			}
			else
			{
				renderer.Push(new TextLinkX(link)); // hyperlink);
				renderer.WriteChildren(link);
				renderer.Pop();
			}
		}
	}
}
