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

        public MainForm()
        {
            InitializeComponent();
            splitContainer1.Visible = false;
            _bankInfoLoader = new BankInfoLoader();
            _mapInfoCache = new MapInfoCache(_bankInfoLoader);

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
            e.Value = mapInfo.Name;
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
                string accountNumber = GetSelectedAccountNumber();
                _selectedMapBank = (_selectedMap.BankInfos.Any(o => o.PlayerNumber == accountNumber)
                                        ? bankReader.LoadBankFromPath((BankInfo) cmbBankFile.SelectedItem)
                                        : null);
                bankEditor1.Bank = _selectedMapBank;
            }
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
        private ExplorerHelper _explorerHelper = new ExplorerHelper();

        private void contextMenuStrip1_Opened(object sender, EventArgs e)
        {
            openMapToolStripMenuItem.Image = (_selectedMap.IsProtected ? Properties.Resources.lockIcon : null);
        }

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
            }
        }

        private void openMapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(_selectedMap.IsProtected)
            {
                MessageBox.Show(
                    "This map is protected; opening it may not work correctly.\n\nTo unprotect it, use \"Save As...\" instead.",
                    "Protected map", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            _explorerHelper.OpenFile(_selectedMap.CachePath);
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
                File.Copy(_selectedMap.CachePath, saveFileDialog1.FileName);
                if(unprotectMap)
                {
                    MapProtection mapProtection = new MapProtection();
                    mapProtection.UnprotectMap(saveFileDialog1.FileName);
                }
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
            _explorerHelper.OpenFile(_selectedMap.BankInfos.First().BankPath);
        }

        private void openBankFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _explorerHelper.OpenFolder(_selectedMap.BankInfos.First().BankPath);
        }

        private void saveBankToToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.FileName = _explorerHelper.GetFileSafeName(_selectedMap.BankInfos.First().Name) + ".SC2Bank";
            saveFileDialog1.Title = "Save bank file to...";
            saveFileDialog1.Filter = "Starcraft 2 Bank File (*.SC2Bank)|*.SC2Bank";
            if(saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                File.Copy(_selectedMap.BankInfos.First().BankPath, saveFileDialog1.FileName);
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

        private string GetSelectedAccountNumber()
        {
            return (from ToolStripMenuItem menuItem in accountsToolStripMenuItem.DropDownItems
                    where menuItem.Checked
                    select menuItem.Text).FirstOrDefault();
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
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if(!_mapList.Any())
            {
                MessageBox.Show("Warning:  No maps found.  Is Starcraft II installed?", "No maps found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            RefreshListBox();
            splitContainer1.Visible = true;

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
