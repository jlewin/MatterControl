// Copyright (c) 2016-2017 Nicolas Musset. All rights reserved.
// This file is licensed under the MIT license.
// See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Markdig.Renderers;
using Markdig.Syntax.Inlines;
using MatterHackers.Agg.UI;
using MatterHackers.MatterControl;
using MatterHackers.VectorMath;

namespace Markdig.Agg
{

	public class MarkdownDocumentLink
	{
		public Uri Uri { get; internal set; }
		public LinkInline LinkInline { get; internal set; }
		public string PageID { get; internal set; }
	}

	public class MarkdownDocument
	{
		private string _markDownText = null;
		private MarkdownPipeline _pipeLine = null;
		private Uri baseUri;
		private static readonly MarkdownPipeline DefaultPipeline = new MarkdownPipelineBuilder().UseSupportedExtensions().Build();

		public MarkdownDocument(Uri baseUri)
		{
			this.baseUri = baseUri;
		}

		public List<MarkdownDocumentLink> Children { get; private set; } = new List<MarkdownDocumentLink>();

		public static MarkdownDocument Load(Uri uri)
		{
			var webClient = new WebClient();

			string rawText = webClient.DownloadString(uri);

			return new MarkdownDocument(uri)
			{
				Markdown = rawText,
			};
		}

		/// <summary>
		/// Gets or sets the Markdown to display.
		/// </summary>
		public string Markdown
		{
			get => _markDownText;
			set
			{
				if (_markDownText != value)
				{
					_markDownText = value;
				}
			}
		}

		/// <summary>
		/// Gets or sets the Markdown pipeline to use.
		/// </summary>
		public MarkdownPipeline Pipeline
		{
			get => _pipeLine ?? DefaultPipeline;
			set
			{
				if (_pipeLine != value)
				{
					_pipeLine = value;
				}
			}
		}

		public void Parse()
		{
			if (!string.IsNullOrEmpty(this.Markdown))
			{
				var pipeline = Pipeline;

				// why do we check the pipeline here?
				pipeline = pipeline ?? new MarkdownPipelineBuilder().Build();

				var rootWidget = new GuiWidget();

				var renderer = new AggRenderer(rootWidget)
				{
					BaseUri = baseUri,
					ChildLinks = new List<MarkdownDocumentLink>()
				};

				pipeline.Setup(renderer);

				var document = Markdig.Markdown.Parse(this.Markdown, pipeline);

				renderer.Render(document);

				this.Children = renderer.ChildLinks;
			}
		}
	}
}
