using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace StarBank
{
    /// <summary>
    /// Various functions to interact with Windows Explorer (opening files, folders, etc)
    /// </summary>
    public class ExplorerHelper
    {
        /// <summary>
        /// Opens the folder the file belongs to in explorer, and highlights it
        /// </summary>
        public void OpenFolder(string filePath)
        {
            if(!File.Exists(filePath))
                throw new FileNotFoundException("Error opening folder:  file not found", filePath); //TODO: Handle thrown exception

            Process.Start("explorer.exe", "/select, " + filePath);
        }

        /// <summary>
        /// Opens the file in the default file-viewer for that extension
        /// </summary>
        public void OpenFile(string filePath)
        {
            if(!File.Exists(filePath))
                throw new FileNotFoundException("Error opening file: file not found", filePath); //TODO: Handle thrown exception

            Process.Start(filePath);
        }
    }
}
