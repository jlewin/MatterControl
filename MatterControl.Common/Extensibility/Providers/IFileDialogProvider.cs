using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MatterHackers.Agg.PlatformAbstract
{
	public interface IFileDialogProvider
	{
		bool OpenFileDialog(OpenFileDialogParams openParams, Action<OpenFileDialogParams> callback);
		bool SelectFolderDialog(SelectFolderDialogParams folderParams, Action<SelectFolderDialogParams> callback);
		bool SaveFileDialog(SaveFileDialogParams saveParams, Action<SaveFileDialogParams> callback);
		string ResolveFilePath(string path);
	}
}
