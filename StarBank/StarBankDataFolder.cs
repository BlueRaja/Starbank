using System;
using System.IO;

namespace StarBank
{
    /// <summary>
    /// Decides where StarBank stores its own data (the map cache and bank backups)
    /// </summary>
    public static class StarBankDataFolder
    {
        private static readonly Lazy<string> _location = new Lazy<string>(FindLocation);

        /// <summary>
        /// The folder of StarBank.exe if it is writable (eg. a portable copy), otherwise %LOCALAPPDATA%\StarBank
        /// (eg. when StarBank is installed in Program Files)
        /// </summary>
        public static string Location
        {
            get { return _location.Value; }
        }

        private static string FindLocation()
        {
            string exeFolder = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            if(IsWritableFolder(exeFolder))
                return exeFolder;
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StarBank");
        }

        private static bool IsWritableFolder(string folder)
        {
            try
            {
                string testFile = Path.Combine(folder, Path.GetRandomFileName());
                File.WriteAllText(testFile, "");
                File.Delete(testFile);
                return true;
            }
            catch(Exception)
            {
                return false;
            }
        }
    }
}
