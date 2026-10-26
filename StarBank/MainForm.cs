using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using StarBank.Bank_Stuffs;
using StarBank.Properties;

namespace StarBank
{
    public partial class MainForm : Form
    {
        private BankInfoLoader _bankInfoLoader;
        private MapInfoCache _mapInfoCache;
        private IEnumerable<MapInfo> _mapList;
        private MapInfo _selectedMap;
        private Bank _selectedMapBank;
        private ProgressBarControl _progressBarControl;
        private bool _isBankCacheLoaded; //Used to help set the progress bar to a proper length
        private BankBackupManager _bankBackupManager;

        //Banks already backed up automatically during this session, before their first edit
        private readonly HashSet<string> _automaticallyBackedUpBanks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public MainForm()
        {
            InitializeComponent();
            splitContainer1.Visible = false;
            _bankInfoLoader = new BankInfoLoader();
            _mapInfoCache = new MapInfoCache(_bankInfoLoader);
            _bankBackupManager = new BankBackupManager(_bankInfoLoader.BanksFolder, Path.Combine(StarBankDataFolder.Location, "Backups"));
            bankEditor1.BankSaving += bankEditor1_BankSaving;
            bankEditor1.BankSaved += bank => RefreshBackupStatus();

            //Create a progress bar and show it on the form
            _progressBarControl = new ProgressBarControl();
            this.Controls.Add(_progressBarControl);
            _progressBarControl.Dock = DockStyle.Fill;
            _progressBarControl.BringToFront();
            _progressBarControl.Visible = true;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            _bankInfoLoader.ProgressChanged += (a, args) => DoReportProgress(args.ProgressPercentage);
            _mapInfoCache.ProgressChanged += (a, args) => DoReportProgress(args.ProgressPercentage);
            PopulateAccountMenu();
            SetTitleBar();
            backgroundWorker1.RunWorkerAsync();
        }

        private void listBox1_Format(object sender, ListControlConvertEventArgs e)
        {
            MapInfo mapInfo = (MapInfo) e.ListItem;
            //Maps with a bank that was never backed up are marked at the start of the name, so the marker is visible in a narrow list
            e.Value = (HasBankWithoutBackup(mapInfo) ? "[!] " : "") + mapInfo.Name;
        }

        /// <summary>
        /// True if the map has bank files (for the selected account), and at least one of them has never been backed up
        /// </summary>
        private bool HasBankWithoutBackup(MapInfo map)
        {
            return GetApplicableBankInfos(map).Any(o => !_bankBackupManager.HasBackup(o.BankPath));
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            _selectedMap = (MapInfo)listBox1.SelectedItem;
            lblMapName.Text = (_selectedMap != null ? _selectedMap.Name : "");
            lblAuthor.Text = "Created by: " + (_selectedMap != null ? _selectedMap.AuthorName : "");
            lblLastUpdate.Text = "Last update downloaded on " + (_selectedMap != null ? _selectedMap.DateCreated.ToShortDateString() : "");

            RefreshBankChoiceDropdown();
            RefreshBankListView();
        }

        private void cmbBankFile_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshBankListView();
        }

        private void RefreshBankChoiceDropdown()
        {
            if(_selectedMap != null)
            {
                IEnumerable<BankInfo> bankInfos = GetApplicableBankInfos(_selectedMap).ToList();
                panelMultipleBankFiles.Visible = (bankInfos.Count() > 1);

                //Need to repopulate the cmbBankFile dropdown anyways, because that is where
                //the ListView gets the BankInfo from
                cmbBankFile.Items.Clear();
                cmbBankFile.Items.AddRange(bankInfos.OrderBy(o => o.Name).ToArray());
                cmbBankFile.SelectedItem = bankInfos.FirstOrDefault();
            }
            else
            {
                panelMultipleBankFiles.Visible = false;
            }
        }

        private IEnumerable<BankInfo> GetApplicableBankInfos(MapInfo map)
        {
            string accountNumber = GetSelectedAccountNumber();
            return map.BankInfos.Where(o => o.PlayerNumber == accountNumber);
        }

        private string GetSelectedAccountNumber()
        {
            return (from ToolStripMenuItem menuItem in accountsToolStripMenuItem.DropDownItems
                    where menuItem.Checked
                    select menuItem.Text).FirstOrDefault();
        }

        private void RefreshBankListView()
        {
            if(_selectedMap == null)
            {
                _selectedMapBank = null;
                bankEditor1.Bank = null;
                return;
            }

            if(_selectedMapBank == null || cmbBankFile.SelectedItem != _selectedMapBank.BankInfo)
            {
                //Refresh the bank also
                BankReader bankReader = new BankReader();
                _selectedMapBank = (GetApplicableBankInfos(_selectedMap).Any()
                                        ? bankReader.LoadBankFromPath((BankInfo) cmbBankFile.SelectedItem)
                                        : null);
                bankEditor1.Bank = _selectedMapBank;
            }
            RefreshBackupStatus();
        }

        /// <summary>
        /// Re-adds all maps to the list (so their "[!]" no-backup markers are updated), keeping the selected map and bank
        /// </summary>
        private void RefreshListBoxKeepingSelection()
        {
            MapInfo selectedMap = _selectedMap;
            BankInfo selectedBank = cmbBankFile.SelectedItem as BankInfo;
            RefreshListBox();
            if(selectedMap != null && listBox1.Items.Contains(selectedMap))
            {
                listBox1.SelectedItem = selectedMap;
                if(selectedBank != null && cmbBankFile.Items.Contains(selectedBank))
                    cmbBankFile.SelectedItem = selectedBank;
            }
            RefreshBackupStatus();
        }

        private void RefreshListBox()
        {
            IEnumerable<MapInfo> mapsToAdd = _mapList;
            if(mapsToAdd != null && mapsToAdd.Any())
            {
                if (cbxHideBlizzard.Checked)
                    mapsToAdd = mapsToAdd.Where(o => o.AuthorName != "Blizzard Entertainment");
                if (cbxHideMapsWithoutBank.Checked)
                    mapsToAdd = mapsToAdd.Where(o => GetApplicableBankInfos(o).Any());

                listBox1.Items.Clear();
                listBox1.Items.AddRange(mapsToAdd.ToArray());
            }
        }

        private void cmbBankFile_Format(object sender, ListControlConvertEventArgs e)
        {
            BankInfo bankInfo = (BankInfo) e.ListItem;
            e.Value = bankInfo.Name;
        }

        private void cbxCheckedChanged(object sender, EventArgs e)
        {
            RefreshListBox();
        }

        #region Context menu
        private readonly ExplorerHelper _explorerHelper = new ExplorerHelper();

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            Point clientPoint = listBox1.PointToClient(MousePosition);
            int listBoxIndex = listBox1.IndexFromPoint(clientPoint);
            if(listBoxIndex == -1)
            {
                e.Cancel = true;
            }
            else
            {
                listBox1.SelectedIndex = listBoxIndex;
                //_selectedMap will be set here by listBox1_SelectedIndexChanged
                bankFileToolStripMenuItem.Enabled = GetApplicableBankInfos(_selectedMap).Any();
                backupMapToolStripMenuItem.Enabled = _selectedMap.BankInfos.Any();
                restoreBackupToolStripMenuItem.Enabled = (cmbBankFile.SelectedItem != null);
            }
        }

        private void openMapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Copy the map to a temp-folder first so we can un-protect it
            string newLocation = Path.Combine(Path.GetTempPath(), _explorerHelper.GetFileSafeName(_selectedMap.Name) + ".SC2Map");
            CopyMap(_selectedMap.CachePath, newLocation);
            _explorerHelper.OpenFile(newLocation);
        }

        private void openMapFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _explorerHelper.OpenFolder(_selectedMap.CachePath);
        }

        private void saveMapToToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.FileName = _explorerHelper.GetFileSafeName(_selectedMap.Name) + ".SC2Map";
            saveFileDialog1.Title = "Save map to...";
            saveFileDialog1.Filter = "Starcraft 2 Map (*.SC2Map)|*.SC2Map|Starcraft 2 Cache File (*.s2ma)|*.s2ma|Starcraft 2 Cache File (*.s2ml)|*.s2ml|Blizzard Archive (*.mpq)|*.mpq";
            if(saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                bool unprotectMap = false;
                if(_selectedMap.IsProtected)
                {
                    DialogResult result =MessageBox.Show("This map is protected; opening it may not work correctly."
                        + "\n\nWould you like to unprotect it now?",
                            "Unprotect map?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information);
                    if(result == DialogResult.Cancel)
                        return;
                    if(result == DialogResult.Yes)
                        unprotectMap = true;
                }
                CopyMap(_selectedMap.CachePath, saveFileDialog1.FileName, unprotectMap);
            }
        }

        private void CopyMap(string oldLocation, string newLocation, bool unprotectMap = true)
        {
            File.Copy(oldLocation, newLocation, true);
            if (unprotectMap)
            {
                MapProtection mapProtection = new MapProtection();
                mapProtection.UnprotectMap(newLocation);
            }
        }

        private void openGalaxyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string extractTo = Path.Combine(Path.GetTempPath(), _explorerHelper.GetFileSafeName(_selectedMap.Name) + ".galaxy");
            _mapInfoCache.ExtractGalaxyScriptFileTo(_selectedMap, extractTo);
            _explorerHelper.OpenFile(extractTo);
        }

        private void saveGalaxyToToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.FileName = _explorerHelper.GetFileSafeName(_selectedMap.Name) + ".galaxy";
            saveFileDialog1.Title = "Save map trigger code to...";
            saveFileDialog1.Filter = "Starcraft 2 Galaxyscript (*.galaxy)|(*.galaxy)|Text file (*.txt)|*.txt";
            if(saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                _mapInfoCache.ExtractGalaxyScriptFileTo(_selectedMap, saveFileDialog1.FileName);
            }
        }

        private void openBankToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _explorerHelper.OpenFile(GetApplicableBankInfos(_selectedMap).First().BankPath);
        }

        private void openBankFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _explorerHelper.OpenFolder(GetApplicableBankInfos(_selectedMap).First().BankPath);
        }

        private void saveBankToToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.FileName = _explorerHelper.GetFileSafeName(GetApplicableBankInfos(_selectedMap).First().Name) + ".SC2Bank";
            saveFileDialog1.Title = "Save bank file to...";
            saveFileDialog1.Filter = "Starcraft 2 Bank File (*.SC2Bank)|*.SC2Bank";
            if(saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                File.Copy(GetApplicableBankInfos(_selectedMap).First().BankPath, saveFileDialog1.FileName);
            }
        }
        #endregion

        #region Menu bar
        private void UnprotectMapFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "Locate map to unprotect";
            openFileDialog1.Filter = "Starcraft 2 Map (*.SC2Map,*.s2ma,*.mpq)|*.SC2Map;*.s2ma;*.mpq|All files (*.*)|*.*";
            if(openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //Safety checks
                MapProtection mapProtection = new MapProtection();
                if(!mapProtection.IsMapProtected(openFileDialog1.FileName))
                {
                    MessageBox.Show("Map is not protected.", "Cannot Unprotect", MessageBoxButtons.OK,
                                    MessageBoxIcon.Exclamation);
                    return;
                }
                if(Path.GetExtension(openFileDialog1.FileName) == ".s2ma")
                {
                    DialogResult result = MessageBox.Show(
                        "Changing Starcraft II cache files can corrupt your cache.\nAre you sure you want to continue?",
                        "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                    if(result != DialogResult.Yes)
                        return;
                }

                //Unprotect the map
                mapProtection.UnprotectMap(openFileDialog1.FileName);
            }
        }

        private void ResignExternalBankToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "Locate bank to sign";
            openFileDialog1.Filter = "Starcraft 2 Bank File (*.SC2Bank)|*.SC2Bank|XML file (*.xml)|*.xml|All files (*.*)|*.*";
            if(openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //Read the bank
                BankReader reader = new BankReader();
                BankInfo bankInfo;
                BankInfoCache bankInfoCache = new BankInfoCache();
                if(!BankPathParser.IsValidBankPath(openFileDialog1.FileName))
                {
                    PlayerNumberForm numberForm = new PlayerNumberForm();
                    if(numberForm.ShowDialog() != DialogResult.OK)
                        return;
                    bankInfo = bankInfoCache.GetOrAddBankInfo(openFileDialog1.FileName, numberForm.PlayerNumber,
                                                                  numberForm.AuthorNumber);
                }
                else
                {
                    bankInfo = bankInfoCache.GetOrAddBankInfo(openFileDialog1.FileName);
                }
                Bank bank = reader.LoadBankFromPath(bankInfo);

                //Write the bank back; automatically re-signs
                BankWriter bankWriter = new BankWriter();
                bankWriter.WriteBank(bank, openFileDialog1.FileName);
            }
        }

        private void BackupBanksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string banksFolder = _bankInfoLoader.BanksFolder;
            string[] bankFiles = _bankInfoLoader.GetAllBankFiles();
            if(bankFiles.Length == 0)
            {
                MessageBox.Show("No bank files were found in:\n" + banksFolder, "Nothing to back up",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            BackUpBankFiles(bankFiles, BankBackupManager.CATEGORY_ALL_BANKS, "all bank files");
        }

        /// <summary>
        /// Backs up the given bank files and tells the user where they went
        /// </summary>
        private void BackUpBankFiles(ICollection<string> bankFiles, string category, string description)
        {
            string backupFolder;
            try
            {
                backupFolder = _bankBackupManager.BackupBankFiles(bankFiles, category);
            }
            catch(Exception exception)
            {
                MessageBox.Show("Backing up the bank files failed:\n" + exception.Message, "Backup failed",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            RefreshListBoxKeepingSelection();

            DialogResult result = MessageBox.Show("Backed up " + bankFiles.Count + " bank file(s) of " + description + " to:\n" + backupFolder
                                                  + "\n\nYou can restore a bank with the \"Restore bank\" button below the bank editor."
                                                  + "\n\nOpen the backup folder now?",
                                                  "Backup complete", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if(result == DialogResult.Yes)
            {
                Process.Start("explorer.exe", "\"" + backupFolder + "\"");
            }
        }

        #region Map backups
        private void backupMap_Click(object sender, EventArgs e)
        {
            if(_selectedMap == null || !_selectedMap.BankInfos.Any())
                return;

            //Back up the map's banks of every account, not only the selected one
            List<string> bankFiles = _selectedMap.BankInfos.Select(o => o.BankPath).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            BackUpBankFiles(bankFiles, BankBackupManager.GetMapCategory(_selectedMap.Name), "\"" + _selectedMap.Name + "\"");
        }

        /// <summary>
        /// Re-signs the bank selected in the bank dropdown, eg. after it was edited outside of StarBank
        /// </summary>
        private void btnResignBank_Click(object sender, EventArgs e)
        {
            BankInfo bankInfo = cmbBankFile.SelectedItem as BankInfo;
            if(bankInfo == null)
                return;

            try
            {
                //Re-signing overwrites the bank, so back it up first (unless an identical backup already exists)
                if(!_bankBackupManager.IsLatestBackupUpToDate(bankInfo.BankPath))
                    _bankBackupManager.BackupBankFiles(new[] {bankInfo.BankPath}, BankBackupManager.CATEGORY_AUTOMATIC);

                //Writing the bank back automatically re-signs it
                Bank bank = new BankReader().LoadBankFromPath(bankInfo);
                new BankWriter().WriteBank(bank, bankInfo.BankPath);
            }
            catch(Exception exception)
            {
                MessageBox.Show("Re-signing the bank failed:\n" + exception.Message, "Re-sign failed",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            //Reload the re-signed bank into the editor
            _selectedMapBank = null;
            RefreshBankListView();
            RefreshListBoxKeepingSelection();

            MessageBox.Show("The bank \"" + bankInfo.Name + "\" has been re-signed."
                            + "\n\nThe bank as it was before re-signing is backed up and can be restored with the \"Restore bank\" button.",
                            "Bank re-signed", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnRestoreBackup_Click(object sender, EventArgs e)
        {
            PopulateRestoreMenu(restoreBackupContextMenuStrip.Items);
            restoreBackupContextMenuStrip.Show(btnRestoreBackup, new Point(0, btnRestoreBackup.Height));
        }

        private void restoreBackupToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            PopulateRestoreMenu(restoreBackupToolStripMenuItem.DropDownItems);
        }

        /// <summary>
        /// Fills the menu with all backups of the selected bank, newest first
        /// </summary>
        private void PopulateRestoreMenu(ToolStripItemCollection items)
        {
            items.Clear();
            BankInfo bankInfo = cmbBankFile.SelectedItem as BankInfo;
            IList<BankBackup> backups = (bankInfo != null ? _bankBackupManager.GetBackups(bankInfo.BankPath) : new List<BankBackup>());
            if(backups.Count == 0)
            {
                items.Add(new ToolStripMenuItem("(no backups of this bank)") {Enabled = false});
                return;
            }

            ToolStripMenuItem header = new ToolStripMenuItem("Restore \"" + bankInfo.Name + "\" from:") {Enabled = false};
            items.Add(header);
            foreach(BankBackup backup in backups)
            {
                string text = backup.DateCreated.ToString("g") + "   -   " + (backup.Category.Length > 0 ? backup.Category : "Backup");
                if(_bankBackupManager.IsSameAsCurrent(backup, bankInfo.BankPath))
                    text += "   (same as current)";
                ToolStripMenuItem item = new ToolStripMenuItem(text);
                item.Tag = backup;
                item.Click += restoreBackupItem_Click;
                items.Add(item);
            }
        }

        private void restoreBackupItem_Click(object sender, EventArgs e)
        {
            BankBackup backup = (BankBackup) ((ToolStripMenuItem) sender).Tag;
            BankInfo bankInfo = cmbBankFile.SelectedItem as BankInfo;
            if(bankInfo == null)
                return;

            DialogResult result = MessageBox.Show("Restore the bank \"" + bankInfo.Name + "\" from the backup made on "
                                                  + backup.DateCreated.ToString("g") + "?"
                                                  + "\n\nThe current bank will be backed up first (Backups\\" + BankBackupManager.CATEGORY_BEFORE_RESTORE + "),"
                                                  + " so this can be undone."
                                                  + "\n\nMake sure StarCraft II is not running, otherwise it may overwrite the restored bank.",
                                                  "Restore bank", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if(result != DialogResult.Yes)
                return;

            try
            {
                _bankBackupManager.RestoreBackup(backup, bankInfo.BankPath);
            }
            catch(Exception exception)
            {
                MessageBox.Show("Restoring the bank failed:\n" + exception.Message, "Restore failed",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            //Reload the restored bank into the editor
            _selectedMapBank = null;
            RefreshBankListView();
            RefreshListBoxKeepingSelection();
        }

        /// <summary>
        /// Before StarBank overwrites a bank for the first time this session, back it up
        /// (unless an identical backup already exists)
        /// </summary>
        private void bankEditor1_BankSaving(Bank bank)
        {
            string bankPath = bank.BankInfo.BankPath;
            if(!_automaticallyBackedUpBanks.Add(bankPath))
                return;

            try
            {
                if(!_bankBackupManager.IsLatestBackupUpToDate(bankPath))
                {
                    _bankBackupManager.BackupBankFiles(new[] {bankPath}, BankBackupManager.CATEGORY_AUTOMATIC);

                    //The map may have lost its "[!]" no-backup marker; update the list once the edit has finished
                    BeginInvoke(new Action(RefreshListBoxKeepingSelection));
                }
            }
            catch(Exception exception)
            {
                MessageBox.Show("Could not back up the bank before saving your change:\n" + exception.Message,
                                "Automatic backup failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Shows whether the selected bank (and the other banks of the selected map) have been backed up
        /// </summary>
        private void RefreshBackupStatus()
        {
            BankInfo selectedBank = (_selectedMap != null ? cmbBankFile.SelectedItem as BankInfo : null);
            btnBackupMap.Enabled = (_selectedMap != null && _selectedMap.BankInfos.Any());
            btnRestoreBackup.Enabled = (selectedBank != null);
            btnResignBank.Enabled = (selectedBank != null);

            if(selectedBank == null)
            {
                lblBackupStatus.ForeColor = SystemColors.GrayText;
                lblBackupStatus.Text = (_selectedMap != null ? "This map has no bank files to back up." : "");
                return;
            }

            IList<BankBackup> backups = _bankBackupManager.GetBackups(selectedBank.BankPath);
            string status;
            if(backups.Count == 0)
            {
                lblBackupStatus.ForeColor = Color.Firebrick;
                status = "Backup: none yet!";
            }
            else
            {
                bool isUpToDate = _bankBackupManager.IsSameAsCurrent(backups[0], selectedBank.BankPath);
                lblBackupStatus.ForeColor = (isUpToDate ? Color.DarkGreen : Color.DarkOrange);
                status = "Backup: " + backups.Count + "x, latest " + backups[0].DateCreated.ToString("g")
                         + (isUpToDate ? " (up to date)" : " (bank changed since)");
            }

            List<BankInfo> mapBanks = GetApplicableBankInfos(_selectedMap).ToList();
            int numBanksWithoutBackup = mapBanks.Count(o => !_bankBackupManager.HasBackup(o.BankPath));
            if(mapBanks.Count > 1 && numBanksWithoutBackup > 0)
            {
                status += "\n" + numBanksWithoutBackup + " of " + mapBanks.Count + " banks of this map have no backup";
                lblBackupStatus.ForeColor = Color.Firebrick;
            }
            lblBackupStatus.Text = status;
        }
        #endregion

        private void PopulateAccountMenu()
        {
            foreach (string accountNumber in _bankInfoLoader.GetAccountNumbers().OrderBy(o => o))
            {
                ToolStripMenuItem menuItem = new ToolStripMenuItem(accountNumber);
                menuItem.Click += accountToolStripMenuItem_Click;
                Icon icon = GetAccountRegionIcon(accountNumber);
                if(icon != null)
                {
                    menuItem.Image = icon.ToBitmap();
                }
                accountsToolStripMenuItem.DropDownItems.Add(menuItem);
            }

            if (accountsToolStripMenuItem.DropDownItems.Count > 0)
            {
                accountsToolStripMenuItem.DropDownItems[0].PerformClick();
            }

            accountsToolStripMenuItem.Visible = (accountsToolStripMenuItem.DropDownItems.Count > 1);
        }

        private void SetTitleBar()
        {
            this.Text += " (v" + GetVersionNumber() + ")";
        }

        private string GetVersionNumber()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            return versionInfo.ProductVersion;
        }

        private void accountToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ToolStripMenuItem menuItem in accountsToolStripMenuItem.DropDownItems)
            {
                menuItem.Checked = (menuItem == sender);
                menuItem.BackColor = (menuItem == sender ? SystemColors.ControlLight : SystemColors.Control);
            }
            RefreshListBox();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AboutBox().ShowDialog();
        }
        #endregion

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            //Loading the caches is heavy on CPU and disk; run it below normal priority so the rest of the system stays responsive
            Process currentProcess = Process.GetCurrentProcess();
            ProcessPriorityClass originalPriority = currentProcess.PriorityClass;
            currentProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
            try
            {
                _isBankCacheLoaded = false;
                _progressBarControl.Status = "Initializing bank cache (Step 1/2)";
                _bankInfoLoader.InitializeCache();

                _isBankCacheLoaded = true;
                _progressBarControl.Status = "Initializing map cache (Step 2/2)";
                _mapList = _mapInfoCache.GetMaps();
            }
            catch(Exception exception)
            {
                this.Invoke(new Action(() => 
                {
                    MessageBox.Show(this,
                        "An error occurred while loading the maps.\r\nPlease screenshot this error and inform the author of this program.\n" + exception.Message + "\r\n" + exception.StackTrace,
                        "An error occurred", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }));
            }
            finally
            {
                currentProcess.PriorityClass = originalPriority;
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_mapList != null && _mapList.Any())
            {
                RefreshListBox();
                RefreshBackupStatus();
                splitContainer1.Visible = true;  
            }
            else
            {
                MessageBox.Show("Warning:  No maps found.  Is Starcraft II installed?", "No maps found",
                   MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            //Hide progress bar and remove floating references
            _progressBarControl.Visible = false;
            this.Controls.Remove(_progressBarControl);
            _progressBarControl = null;
        }

        private bool _isReportingProgress;
        private void DoReportProgress(int progressPercentage)
        {
            backgroundWorker1.ReportProgress(progressPercentage);
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //Make the bank-cache go from 0 to bankCachePercentage, then the map-cache go from bankCachePercentage to 100
            const int bankCachePercentage = 20;
            int newProgress = 0;
            if(!_isBankCacheLoaded)
            {
                newProgress = e.ProgressPercentage*bankCachePercentage/100;
            }
            else
            {
                newProgress = bankCachePercentage +
                                               e.ProgressPercentage*(100 - bankCachePercentage)/100;
            }
            _progressBarControl.Progress = Math.Max(newProgress, _progressBarControl.Progress);
        }

        private static Icon GetAccountRegionIcon(string accountNumber)
        {
            int regionNumber;
            int.TryParse(accountNumber.Substring(0, 1), out regionNumber);

            switch(regionNumber)
            {
                case 1:
                    return Resources.USA;
                case 2:
                    return Resources.EU;
                case 3:
                    return Resources.KOR;
                case 6:
                    return Resources.SEA;
                default:
                    return null;
            }
        }
    }
}
