using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using IWshRuntimeLibrary;

namespace MovieListMake
{
    class Shortcut
    {
        public static void Make(string mySourcePath, string myDestPath)
        {
            //string path = Path.Combine(myDestPath, "sample_shortcut.lnk");

            //
            // ショートカット作成
            //
            IWshShell shell = new WshShellClass();

            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(myDestPath);
            shortcut.TargetPath = mySourcePath;
            shortcut.Description = "TEST";
            shortcut.Save();
        }
    }
}
