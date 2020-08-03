﻿/*
Copyright (c) 2019, John Lewin
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
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using MatterHackers.Agg;
using MatterHackers.Agg.Platform;
using Newtonsoft.Json;

namespace MatterHackers.MatterControl.Library
{
	public class GitHubContainer : LibraryContainer
	{
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1310 // Field names should not contain underscore
		internal struct FileInfo
		{
			public LinkFields _links;
			public string download_url;
			public string name;
			public string type;
		}

		// JSON parsing methods
		internal struct LinkFields
		{
			public string self;
		}
#pragma warning restore SA1310 // Field names should not contain underscore
#pragma warning restore SA1307 // Accessible fields should begin with upper-case letter

		public string Account { get; }

		public string Repository { get; }

		public string RepoDirectory { get; }

		public GitHubContainer(string containerName, string account, string repositor, string repoDirectory)
		{
			this.ChildContainers = new List<ILibraryContainerLink>();
			this.Items = new List<ILibraryItem>();
			this.Name = containerName;
			this.Account = account;
			this.Repository = repositor;
			this.RepoDirectory = repoDirectory;
		}

		public override async void Load()
		{
			try
			{
				await GetRepo();
			}
			catch
			{
				// show an error
			}

			OnContentChanged();
		}

		// Get all files from a repo
		public async Task GetRepo()
		{
			var client = new HttpClient();
			await ReadDirectory("root",
				client,
				$"https://api.github.com/repos/{Account}/{Repository}/contents/{RepoDirectory}");
			client.Dispose();
		}

		private async Task ReadDirectory(string name, HttpClient client, string uri)
		{
			// get the directory contents
			var request = new HttpRequestMessage(HttpMethod.Get, uri);
			AddCromeHeaders(request);

			// parse result
			HttpResponseMessage response = await client.SendAsync(request);
			string jsonStr = await response.Content.ReadAsStringAsync();
			response.Dispose();
			FileInfo[] dirContents = JsonConvert.DeserializeObject<FileInfo[]>(jsonStr);

			// read in data
			foreach (FileInfo file in dirContents)
			{
				if (file.type == "dir")
				{
					this.ChildContainers.Add(
						new DynamicContainerLink(
							() => file.name,
							AggContext.StaticData.LoadIcon(Path.Combine("Library", "calibration_library_folder.png")),
							() => new GitHubContainer(file.name, Account, Repository, RepoDirectory + "/" + file.name),
							() =>
							{
								return true;
							})
						{
							IsReadOnly = true
						});

					// read in the subdirectory
					// Directory sub = await ReadDirectory(file.name, client, file._links.self, access_token);
					// result.subDirs.Add(sub);
				}
				else if (file.type == "file")
				{
					this.Items.Add(new GitHubLibraryItem(file.name, file.download_url));
				}
			}
		}

		public static void AddCromeHeaders(HttpRequestMessage request)
		{
			request.Headers.Add("Connection", "keep-alive");
			request.Headers.Add("Upgrade-Insecure-Requests", "1");
			request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4147.105 Safari/537.36");
			request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
			request.Headers.Add("Sec-Fetch-Site", "none");
			request.Headers.Add("Sec-Fetch-Mode", "navigate");
			request.Headers.Add("Sec-Fetch-User", "?1");
			request.Headers.Add("Sec-Fetch-Dest", "document");
			request.Headers.Add("Accept-Language", "en-US,en;q=0.9");
		}

		private class StaticDataItem : ILibraryAssetStream
		{
			public StaticDataItem()
			{
			}

			public StaticDataItem(string relativePath)
			{
				this.AssetPath = relativePath;
			}

			public string AssetPath { get; }

			public string Category { get; } = "";

			public string ContentType => Path.GetExtension(AssetPath).ToLower().Trim('.');

			public DateTime DateCreated { get; } = DateTime.Now;

			public DateTime DateModified { get; } = DateTime.Now;

			public string FileName => Path.GetFileName(AssetPath);

			public long FileSize { get; } = -1;

			public string ID => agg_basics.GetLongHashCode(AssetPath).ToString();

			public bool IsProtected => true;

			public bool IsVisible => true;

			public bool LocalContentExists => true;

			public string Name => this.FileName;

			public Task<StreamAndLength> GetStream(Action<double, string> progress)
			{
				return Task.FromResult(new StreamAndLength()
				{
					Stream = AggContext.StaticData.OpenStream(AssetPath),
					Length = -1
				});
			}
		}

		public class GitHubContainerLink : ILibraryContainerLink
		{
			private string containerName;
			private string owner;
			private string repository;
			private string path;

			public GitHubContainerLink(string containerName, string owner, string repository, string path)
			{
				this.containerName = containerName;
				this.owner = owner;
				this.repository = repository;
				this.path = path;
			}

			public bool IsReadOnly { get; set; } = true;

			public bool UseIncrementedNameDuringTypeChange { get; set; }

			public string ID => containerName
				.GetLongHashCode(owner
					.GetLongHashCode(repository
						.GetLongHashCode(path
							.GetLongHashCode()))).ToString();

			public string Name => containerName;

			public bool IsProtected => false;

			public bool IsVisible => true;

			public DateTime DateModified => DateTime.Now;

			public DateTime DateCreated => DateTime.Now;

			public Task<ILibraryContainer> GetContainer(Action<double, string> reportProgress)
			{
				return Task.FromResult<ILibraryContainer>(new GitHubContainer(containerName, owner, repository, path));
			}
		}

	}

	public static class LibraryJsonFile
	{ 
		internal struct LibraryDocument
		{
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
			public string type; // values: "GitHub", "local"

			public string owner; // for type GitHub is the repository owner

			public string repository; // for the GitHub repository to look in

			public string path; // the path within the type (GitHub the sub folder, local the entire file path)
#pragma warning restore SA1310 // Field names should not contain underscore

			public ILibraryContainerLink CreateContainer(string containerName)
			{
				switch (this.type)
				{
					case "GitHub":
						return new GitHubContainer.GitHubContainerLink(containerName, this.owner, this.repository, this.path);

					case "local":
						return new FileSystemContainer.DirectoryContainerLink(this.path);
				}

				return null;
			}

		}

		public static ILibraryContainerLink ContainerFromLocalFile(string jsonLibraryFilename)
		{
			if (File.Exists(jsonLibraryFilename))
			{
				var content = File.ReadAllText(jsonLibraryFilename);
				var libraryDocument = JsonConvert.DeserializeObject<LibraryDocument>(content);

				return libraryDocument.CreateContainer(Path.GetFileNameWithoutExtension(jsonLibraryFilename));
			}

			return null;
		}
	}
}