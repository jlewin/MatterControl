/*
Copyright (c) 2017, Lars Brubaker, John Lewin
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
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MatterHackers.Agg.Image;
using MatterHackers.Agg.Platform;
using MatterHackers.Agg.UI;
using MatterHackers.DataConverters3D;
using MatterHackers.Localizations;

namespace MatterHackers.MatterControl.Library.Export
{
	public class McxzExport : IExportPlugin
	{
		public string ButtonText => "MCXZ File".Localize();

		public string FileExtension => ".mcxz";

		public string ExtensionFilter => "Save as MCXZ|*.mcxz";

		public ImageBuffer Icon { get; } = AggContext.StaticData.LoadIcon(Path.Combine("filetypes", "zip.png"));

		public void Initialize(PrinterConfig printer)
		{
		}

		public bool Enabled => true;

		public bool ExportPossible(ILibraryAsset libraryItem) => libraryItem is InteractiveScene;

		private class ZipAssetManager : IAssetManager
		{
			private ZipArchive zipArchive;

			public ZipAssetManager(ZipArchive zipArchive)
			{
				this.zipArchive = zipArchive;
			}

			public Task AcquireAsset(string sha1PlusExtension, CancellationToken cancellationToken, Action<double, string> progress)
			{
				throw new NotImplementedException();
			}

			public Task<Stream> LoadAsset(IAssetObject assetObject, CancellationToken cancellationToken, Action<double, string> progress)
			{
				throw new NotImplementedException();
			}

			public Task PublishAsset(string sha1PlusExtension, CancellationToken cancellationToken, Action<double, string> progress)
			{
				Console.WriteLine($"PublishAsset: {sha1PlusExtension}");
				return Task.CompletedTask;
			}

			public Task StoreAsset(IAssetObject assetObject, bool publishAfterSave, CancellationToken cancellationToken, Action<double, string> progress)
			{
				Console.WriteLine($"StoreAsset: {assetObject.AssetPath}");
				return Task.CompletedTask;
			}

			public Task<string> StoreFile(string filePath, bool publishAfterSave, CancellationToken cancellationToken, Action<double, string> progress)
			{
				Console.WriteLine($"StoreFile: {filePath}");
				return Task.FromResult("");
			}

			public Task<string> StoreMcx(IObject3D object3D, bool publishAfterSave)
			{
				Console.WriteLine($"StoreMcx: {object3D.MeshPath}");
				return Task.FromResult("");
			}

			public Task StoreMesh(IObject3D object3D, bool publishAfterSave, CancellationToken cancellationToken, Action<double, string> progress)
			{
				Console.WriteLine($"StoreMesh: {object3D.MeshPath}");
				return Task.FromResult("");
			}

			public string StoreStream(Stream stream, string extension)
			{
				Console.WriteLine($"StoreStream: !!!!!");

				return "";
			}
		}

		public async Task<bool> Generate(IEnumerable<ILibraryItem> libraryItems, string outputPath)
		{
			if (libraryItems.FirstOrDefault() is InMemoryLibraryItem item &&
				await item.GetObject3D(null) is InteractiveScene scene)
			{
				await Task.Run(async () =>
				{
					try
					{
						if (File.Exists(outputPath))
						{
							File.Delete(outputPath);
						}

						using (ZipArchive zipArchive = ZipFile.Open(outputPath, ZipArchiveMode.Create))
						{
							var xxx = new ZipAssetManager(zipArchive);

							var newEntry = zipArchive.CreateEntry("rootitem.mcx");

							using (Stream zipEntryStream = newEntry.Open())
							{
								scene.SaveTo(zipEntryStream, xxx, publishAssets: true);
							}
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex.Message);
					}
				});

				return true;
			}

			return false;
		}
	}

	public class ZipExport : IExportPlugin
	{
		public string ButtonText => "ZIP File".Localize();

		public string FileExtension => ".zip";

		public string ExtensionFilter => "Save as ZIP|*.zip";

		public ImageBuffer Icon { get; } = AggContext.StaticData.LoadIcon(Path.Combine("filetypes", "zip.png"));

		public void Initialize(PrinterConfig printer)
		{
		}

		public bool Enabled => true;

		public bool ExportPossible(ILibraryAsset libraryItem) => true;

		public async Task<bool> Generate(IEnumerable<ILibraryItem> libraryItems, string outputPath)
		{
			var streamItems = libraryItems.OfType<ILibraryAssetStream>();
			if (streamItems.Any())
			{
				await Task.Run(async () =>
				{
					try
					{
						if (File.Exists(outputPath))
						{
							File.Delete(outputPath);
						}

						using (ZipArchive zipArchive = ZipFile.Open(outputPath, ZipArchiveMode.Create))
						{
							foreach (var item in streamItems)
							{
								// TODO: need to test for and resolve name conflicts
								var entry = zipArchive.CreateEntry(item.FileName);

								using (var sourceStream = await item.GetStream(null))
								using (var outputStream = entry.Open())
								{
									sourceStream.Stream.CopyTo(outputStream);
								}
							}

						}
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex.Message);
					}
				});

				return true;
			}

			return false;
		}
	}
}
