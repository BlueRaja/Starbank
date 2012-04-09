using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace StarBank.Bank_Stuffs
{
    public class BankInfoCache
    {
        private readonly string BANKS_FOLDER = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                                            @"StarCraft II\Accounts");

        private Dictionary<string, BankInfo> _bankCache = new Dictionary<string, BankInfo>();

        //Regex to look for the string BankLoad("something") - the "something" is what we're looking for
        //(the name of the bank)
        private readonly Regex BANK_LOAD_REGEX = new Regex(@"BankLoad\(""(.+?)""", RegexOptions.Singleline);

        public BankInfoCache()
        {
            InitializeCache();
        }

        /// <summary>
        /// Build the initial cache of BankInfo items.
        /// Each BankInfo will associate the bank name (eg. MyBank.SC2Bank) to
        /// one particular file.  The reason is that different versions of the same
        /// old copies of the same bank (with the same file-name) lying around.  Since I'm not
        /// sure how to get the author-number from the MPQ file, we have to make a guesstimate
        /// as to which bank is the correct one.  I take the one most recently modified.
        /// </summary>
        private void InitializeCache()
        {
            DirectoryInfo bankFolder = new DirectoryInfo(BANKS_FOLDER);
            IEnumerable<FileInfo> bankFiles = bankFolder.GetFiles("*.SC2Bank", SearchOption.AllDirectories);
            foreach(FileInfo file in bankFiles)
            {
                string bankCacheKey = file.Name.ToLower();
                //If the cache already contains a newer version of a bank with the same name, skip this one
                if (_bankCache.ContainsKey(bankCacheKey) && _bankCache[bankCacheKey].DateModified >= file.LastWriteTime)
                    continue;

                //Ignore bank files that are not in the correct folders
                if(BankPathParser.IsValidBankPath(file.FullName))
                {
                    //Add the new bank to the cache
                    BankInfo bankInfo = CreateBankInfo(file.FullName);
                    _bankCache[bankCacheKey] = bankInfo;
                }
            }
        }

        private BankInfo CreateBankInfo(string bankPath)
        {
            BankPathParser bankPathParser = new BankPathParser(bankPath);
            return CreateBankInfo(bankPath, bankPathParser.PlayerNumber, bankPathParser.AuthorNumber);
        }

        private BankInfo CreateBankInfo(string bankPath, string playerNumber, string authorNumber)
        {
            BankInfo bankInfo = new BankInfo();
            bankInfo.BankPath = bankPath;
            bankInfo.Name = Path.GetFileNameWithoutExtension(bankPath);
            bankInfo.DateModified = File.GetLastWriteTime(bankPath);
            bankInfo.PlayerNumber = playerNumber;
            bankInfo.AuthorNumber = authorNumber;
            return bankInfo;
        }

        /// <summary>
        /// Given the trigger-code for some map, will determine if it uses a bank, and if so, 
        /// return references to all the BankInfos for the banks it uses
        /// </summary>
        /// <exception cref="BankNotFoundException">
        /// Throws a BankNotFoundException if the given code references a bank, but that bank
        /// is not found in the cache
        /// </exception>
        public IEnumerable<BankInfo> GetBanksFromCode(string galaxyScriptCode)
        {
            //We guess the bank name by finding all references to BankLoad()
            //Then we search for that bank file in the bank folder
            MatchCollection matches = BANK_LOAD_REGEX.Matches(galaxyScriptCode);
            List<BankInfo> banksUsed = new List<BankInfo>();
            foreach(Match match in matches)
            {
                string bankCacheKey = match.Groups[1].Value.ToLower() + ".sc2bank";
                if(_bankCache.ContainsKey(bankCacheKey))
                {
                    if (!banksUsed.Contains(_bankCache[bankCacheKey]))
                        banksUsed.Add(_bankCache[bankCacheKey]);
                } else
                {
                    //TODO: Bank not found.  Figure out what to do here.
                }
            }
            return banksUsed;
        }

        /// <summary>
        /// Returns the BankInfo object for the bank located at the given path
        /// </summary>
        public BankInfo GetBankInfoFromPath(string bankPath)
        {
            BankInfo bankInfo = _bankCache.FirstOrDefault(o => o.Value.BankPath == bankPath).Value;
            if(bankInfo != null)
                return bankInfo;

            return CreateBankInfo(bankPath);
        }

        /// <summary>
        /// Returns the BankInfo object for the bank located at the given path
        /// </summary>
        public BankInfo GetBankInfoFromPath(string bankPath, string playerNumber, string authorNumber)
        {
            BankInfo bankInfo = _bankCache.FirstOrDefault(o => o.Value.BankPath == bankPath).Value;
            if(bankInfo != null)
                return bankInfo;

            return CreateBankInfo(bankPath, playerNumber, authorNumber);
        }
    }
}
