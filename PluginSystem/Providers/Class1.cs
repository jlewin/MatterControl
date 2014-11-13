using MatterHackers.MatterControl.Extensibility.FileDialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MatterHackers.MatterControl.Extensibility
{
    public abstract class FileDialogCreator
    {
        public delegate void OpenFileDialogDelegate(OpenFileDialogParams openParams);
        public delegate void SelectFolderDialogDelegate(SelectFolderDialogParams folderParams);
        public delegate void SaveFileDialogDelegate(SaveFileDialogParams saveParams);

        public abstract Stream OpenFileDialog(ref OpenFileDialogParams openParams);
        public abstract bool OpenFileDialog(OpenFileDialogParams openParams, OpenFileDialogDelegate callback);

        public abstract string SelectFolderDialog(ref SelectFolderDialogParams folderParams);
        public abstract bool SelectFolderDialog(SelectFolderDialogParams folderParams, SelectFolderDialogDelegate callback);

        public abstract Stream SaveFileDialog(ref SaveFileDialogParams saveParams);
        public abstract bool SaveFileDialog(SaveFileDialogParams saveParams, SaveFileDialogDelegate callback);
    }
}
