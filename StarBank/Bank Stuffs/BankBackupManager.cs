using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace StarBank.Bank_Stuffs
{
    /// <summary>
    /// A single backed-up copy of a bank file
    /// </summary>
    public class BankBackup
    {
        public string BackupPath;
        public DateTime DateCreated;

        /// <summary>
        /// The kind of backup, eg. "All banks", "Automatic" or "Maps\Some Map"
        /// </summary>
        public string Category;
    }

    /// <summary>
    /// Creates, finds and restores backups of bank files.
    ///
    /// Every backup is a dated folder that keeps the folder structure below the Accounts folder:
    ///   Backups\[category]\2026-10-07 01-11-32\[account]\[player]\Banks\[author]\[name].SC2Bank
    /// A bank is identified by its path below the Accounts folder (account, player, map author number and bank name),
    /// so backups of a bank are found no matter which kind of backup they were made by.
    /// </summary>
    public class BankBackupManager
    {
        public const string CATEGORY_ALL_BANKS = "All banks";
        public const string CATEGORY_AUTOMATIC = "Automatic";
        public const string CATEGORY_BEFORE_RESTORE = "Before restore";
        private const string CATEGORY_MAPS = "Maps";

        private const string DATE_FORMAT = "yyyy-MM-dd HH-mm-ss";

        private readonly string _banksFolder;
        private readonly string _backupsFolder;

        //Lazily built index of all backups, keyed by GetBankKey(); null when it needs to be rebuilt
        private Dictionary<string, List<BankBackup>> _backupsByBank;

        public BankBackupManager(string banksFolder, string backupsFolder)
        {
            _banksFolder = banksFolder;
            _backupsFolder = backupsFolder;
        }

        public string BackupsFolder
        {
            get { return _backupsFolder; }
        }

        /// <summary>
        /// The category to use for backups of a single map's banks
        /// </summary>
        public static string GetMapCategory(string mapName)
        {
            string safeName = mapName;
            foreach(char c in Path.GetInvalidFileNameChars())
            {
                safeName = safeName.Replace(c, '_');
            }
            safeName = safeName.Substring(0, Math.Min(60, safeName.Length)).Trim(' ', '.');
            return Path.Combine(CATEGORY_MAPS, (safeName.Length > 0 ? safeName : "(unknown)"));
        }

        /// <summary>
        /// Copies the given bank files into a new dated folder of the given category, and returns that folder
        /// </summary>
        public string BackupBankFiles(IEnumerable<string> bankFiles, string category)
        {
            string backupFolder = Path.Combine(Path.Combine(_backupsFolder, category), DateTime.Now.ToString(DATE_FORMAT));
            foreach(string bankFile in bankFiles)
            {
                string pathBelowBanksFolder = GetPathBelowBanksFolder(bankFile);
                if(pathBelowBanksFolder == null)
                    throw new ArgumentException("Bank file is not inside the StarCraft II Accounts folder: " + bankFile);
                string destinationPath = Path.Combine(backupFolder, pathBelowBanksFolder);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                File.Copy(bankFile, destinationPath, true);
            }
            _backupsByBank = null;
            return backupFolder;
        }

        /// <summary>
        /// Returns all backups of the given bank file, newest first
        /// </summary>
        public IList<BankBackup> GetBackups(string bankPath)
        {
            string key = GetBankKey(bankPath);
            List<BankBackup> backups;
            if(key != null && GetBackupIndex().TryGetValue(key, out backups))
                return backups;
            return new List<BankBackup>();
        }

        public bool HasBackup(string bankPath)
        {
            return GetBackups(bankPath).Count > 0;
        }

        /// <summary>
        /// Returns true if the newest backup of the bank has exactly the same contents as the bank does now
        /// </summary>
        public bool IsLatestBackupUpToDate(string bankPath)
        {
            IList<BankBackup> backups = GetBackups(bankPath);
            return backups.Count > 0 && IsSameAsCurrent(backups[0], bankPath);
        }

        public bool IsSameAsCurrent(BankBackup backup, string bankPath)
        {
            try
            {
                return File.Exists(bankPath) && File.ReadAllBytes(backup.BackupPath).SequenceEqual(File.ReadAllBytes(bankPath));
            }
            catch(IOException)
            {
                return false;
            }
        }

        /// <summary>
        /// Overwrites the bank file with the given backup.  The current bank is backed up first
        /// (unless an identical backup already exists), so a restore can always be undone.
        /// </summary>
        public void RestoreBackup(BankBackup backup, string bankPath)
        {
            if(File.Exists(bankPath) && !IsLatestBackupUpToDate(bankPath) && !IsSameAsCurrent(backup, bankPath))
            {
                BackupBankFiles(new[] {bankPath}, CATEGORY_BEFORE_RESTORE);
            }
            File.Copy(backup.BackupPath, bankPath, true);
        }

        private Dictionary<string, List<BankBackup>> GetBackupIndex()
        {
            if(_backupsByBank != null)
                return _backupsByBank;

            _backupsByBank = new Dictionary<string, List<BankBackup>>(StringComparer.OrdinalIgnoreCase);
            if(!Directory.Exists(_backupsFolder))
                return _backupsByBank;

            string separator = Path.DirectorySeparatorChar.ToString();
            foreach(string backupPath in Directory.GetFiles(_backupsFolder, "*.SC2Bank", SearchOption.AllDirectories))
            {
                //[category parts...]\[date]\[path below the Accounts folder]
                //The first folder named like a date separates the category from the bank's path
                string[] parts = backupPath.Substring(_backupsFolder.Length).Trim(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar);
                int dateIndex = -1;
                DateTime dateCreated = DateTime.MinValue;
                for(int i = 0; i < parts.Length - 1 && dateIndex < 0; i++)
                {
                    if(DateTime.TryParseExact(parts[i], DATE_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateCreated))
                        dateIndex = i;
                }
                if(dateIndex < 0)
                    continue;

                BankBackup backup = new BankBackup
                {
                    BackupPath = backupPath,
                    DateCreated = dateCreated,
                    Category = String.Join(separator, parts, 0, dateIndex)
                };

                string key = String.Join(separator, parts, dateIndex + 1, parts.Length - dateIndex - 1).ToLowerInvariant();
                List<BankBackup> backups;
                if(!_backupsByBank.TryGetValue(key, out backups))
                {
                    backups = new List<BankBackup>();
                    _backupsByBank[key] = backups;
                }
                backups.Add(backup);
            }

            foreach(List<BankBackup> backups in _backupsByBank.Values)
            {
                backups.Sort((a, b) => b.DateCreated.CompareTo(a.DateCreated));
            }
            return _backupsByBank;
        }

        /// <summary>
        /// Returns the bank's path below the Accounts folder, eg. [account]\[player]\Banks\[author]\[name].SC2Bank,
        /// or null if the bank is not inside the Accounts folder
        /// </summary>
        private string GetPathBelowBanksFolder(string bankPath)
        {
            string fullPath = Path.GetFullPath(bankPath);
            if(!fullPath.StartsWith(_banksFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                return null;
            return fullPath.Substring(_banksFolder.Length + 1);
        }

        /// <summary>
        /// Identifies a bank by its path below the Accounts folder, or null if it is not inside the Accounts folder
        /// </summary>
        private string GetBankKey(string bankPath)
        {
            string pathBelowBanksFolder = GetPathBelowBanksFolder(bankPath);
            return (pathBelowBanksFolder != null ? pathBelowBanksFolder.ToLowerInvariant() : null);
        }
    }
}
