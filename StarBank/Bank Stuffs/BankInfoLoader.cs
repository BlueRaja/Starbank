using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace StarBank.Bank_Stuffs
{
    public class BankInfoLoader
    {
        private readonly string BANKS_FOLDER =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                @"StarCraft II\Accounts");

        /// <summary>
        /// The folder that contains every account's bank files
        /// </summary>
        public string BanksFolder
        {
            get { return BANKS_FOLDER; }
        }

        /// <summary>
        /// Returns the paths of all bank files of all accounts
        /// </summary>
        public string[] GetAllBankFiles()
        {
            if(!Directory.Exists(BANKS_FOLDER))
                return new string[0];
            return Directory.GetFiles(BANKS_FOLDER, "*.SC2Bank", SearchOption.AllDirectories);
        }

        //Regex to look for the string BankLoad("something") - the "something" is what we're looking for
        //(the name of the bank)
        private readonly Regex BANK_LOAD_REGEX = new Regex(@"BankLoad\(""(.+?)""",
            RegexOptions.Singleline | RegexOptions.Compiled);

        private readonly Regex BANK_LOAD_VARIABLE_REGEX = new Regex(@"BankLoad\(([^""]+?)[,\)]",
            RegexOptions.Singleline | RegexOptions.Compiled);

        private readonly BankInfoCache _bankCache = new BankInfoCache();

        private readonly Lazy<IEnumerable<string>> _accountNumbers;

        public BankInfoLoader()
        {
            _accountNumbers = new Lazy<IEnumerable<string>>(GetAccountNumbers);
        }

        //For progress bars
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        private void OnProgressChanged(double progress)
        {
            if(ProgressChanged != null)
                ProgressChanged(this, new ProgressChangedEventArgs((int) (progress*100), null));
        }

        /// <summary>
        /// Build the initial cache of BankInfo items.
        /// Each BankInfo will associate the bank name (eg. MyBank.SC2Bank) to
        /// one particular file.  The reason is that different versions of the same
        /// old copies of the same bank (with the same file-name) lying around.  Since I'm not
        /// sure how to get the author-number from the MPQ file, we have to make a guesstimate
        /// as to which bank is the correct one.  I take the one most recently modified.
        /// </summary>
        public void InitializeCache()
        {
            DirectoryInfo bankFolder = new DirectoryInfo(BANKS_FOLDER);
            if(!bankFolder.Exists)
                return;
            IEnumerable<FileInfo> bankFiles = bankFolder.GetFiles("*.SC2Bank", SearchOption.AllDirectories);
            int numBanksTotal = bankFiles.Count();
            int numBanksProcessed = 0;

            foreach(FileInfo file in bankFiles)
            {
                //Ignore bank files that are not in the correct folders
                if(BankPathParser.IsValidBankPath(file.FullName))
                {
                    //Add the new bank to the cache
                    _bankCache.GetOrAddBankInfo(file.FullName);
                }
                numBanksProcessed++;
                OnProgressChanged((double) numBanksProcessed/numBanksTotal);
            }
        }

        /// <summary>
        /// Given the trigger-code for some map, will determine if it uses a bank, and if so, 
        /// return references to all the BankInfos for the banks it uses
        /// </summary>
        public IEnumerable<BankInfo> GetBanksFromCode(string galaxyScriptCode)
        {
            //We guess the bank name by finding all references to BankLoad()
            //Then we search for that bank file in the bank folders
            return GetBanksFromBankNames(GetBankNamesFromCode(galaxyScriptCode));
        }

        /// <summary>
        /// Given bank names found by GetBankNamesFromCode(), returns references to the BankInfos
        /// of all the matching bank files of every account
        /// </summary>
        public IEnumerable<BankInfo> GetBanksFromBankNames(IEnumerable<string> bankNames)
        {
            IEnumerable<string> accountNumbers = GetCachedAccountNumbers();

            return (from accountNumber in accountNumbers
                from bankName in bankNames
                let bankInfo = _bankCache.GetBankInfoForPlayer(accountNumber, bankName)
                where bankInfo != null
                select bankInfo).ToList();
        }

        /// <summary>
        /// Given the galaxyscript code, returns a list of possible banknames used by it
        /// </summary>
        public List<string> GetBankNamesFromCode(string galaxyScriptCode)
        {
            List<string> bankNames = new List<string>();

            //We guess the bank name by finding all references to BankLoad()
            //Then we search for that bank file in the bank folder
            MatchCollection matches = BANK_LOAD_REGEX.Matches(galaxyScriptCode);
            foreach(Match match in matches)
            {
                string bankName = match.Groups[1].Value.ToLower() + ".sc2bank";
                if(!bankNames.Contains(bankName))
                    bankNames.Add(bankName);
            }

            //A bit more difficult:  some maps use a variable instead of hard-coding the bank-name directly in the function.
            //I'm not going to write a parser o_O but we can take care of the 90% case by simply searching for the string 'variableName = "whatever"'
            //The same variable is often passed to BankLoad() many times; only search the (potentially huge) script once per variable
            matches = BANK_LOAD_VARIABLE_REGEX.Matches(galaxyScriptCode);
            HashSet<string> variableNames = new HashSet<string>();
            foreach(Match match in matches)
            {
                variableNames.Add(match.Groups[1].Value.Trim());
            }

            foreach(string variableName in variableNames)
            {
                Regex variableRegex = new Regex(Regex.Escape(variableName) + @"\s*=\s*""([a-zA-Z1-9_ ]+?)""");
                MatchCollection variableMatches = variableRegex.Matches(galaxyScriptCode);
                foreach(Match variableMatch in variableMatches)
                {
                    string bankName = variableMatch.Groups[1].Value.ToLower() + ".sc2bank";
                    if(!bankNames.Contains(bankName))
                        bankNames.Add(bankName);
                }
            }

            return bankNames;
        }

        /// <summary>
        /// Returns a list of paths to all SC2 accounts on the current machine
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetAccountNumbers()
        {
            DirectoryInfo bankFolder = new DirectoryInfo(BANKS_FOLDER);
            if(!bankFolder.Exists)
                return new string[0];

            return bankFolder.EnumerateDirectories()
                    .SelectMany(o => o.EnumerateDirectories())
                    .Select(s => s.Name)
                    .Where(BankPathParser.IsValidPlayerOrAuthorNumber)
                    .ToList();
        }

        /// <summary>
        /// Same as GetAccountNumbers(), but only scans the disk once instead of once per map
        /// </summary>
        private IEnumerable<string> GetCachedAccountNumbers()
        {
            return _accountNumbers.Value;
        }
    }
}