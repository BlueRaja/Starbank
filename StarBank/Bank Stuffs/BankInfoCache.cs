using System.Collections.Generic;
using System.IO;

namespace StarBank.Bank_Stuffs
{
    public class BankInfoCache
    {
        private readonly Dictionary<string, Dictionary<string, BankInfo>> _bankCaches = new Dictionary<string, Dictionary<string, BankInfo>>();

        public BankInfo GetOrAddBankInfo(string bankPath)
        {
            BankPathParser bankPathParser = new BankPathParser(bankPath);
            return GetOrAddBankInfo(bankPath, bankPathParser.PlayerNumber, bankPathParser.AuthorNumber);
        }

        public BankInfo GetOrAddBankInfo(string bankPath, string playerNumber, string authorNumber)
        {
            var bankCache = GetOrAddBankCache(playerNumber);
            string fileName = Path.GetFileNameWithoutExtension(bankPath).ToLower();

            if(bankCache.ContainsKey(fileName))
            {
                //If the new file has a more recent write-time, we want it to replace the old file
                //So, only return the old file if IT has the more recent write-time
                if(File.GetLastWriteTime(bankPath) <= bankCache[fileName].DateModified)
                {
                    return bankCache[fileName];
                }
            }

            BankInfo bankInfo = new BankInfo();
            bankInfo.BankPath = bankPath;
            bankInfo.Name = Path.GetFileNameWithoutExtension(bankPath);
            bankInfo.DateModified = File.GetLastWriteTime(bankPath);
            bankInfo.PlayerNumber = playerNumber;
            bankInfo.AuthorNumber = authorNumber;
            bankCache[fileName] = bankInfo;
            return bankInfo;
        }

        public BankInfo GetBankInfoForPlayer(string playerNumber, string bankName)
        {
            var bankCache = GetOrAddBankCache(playerNumber);
            string fileName = Path.GetFileNameWithoutExtension(bankName).ToLower();

            BankInfo bankInfo;
            bankCache.TryGetValue(fileName, out bankInfo);
            return bankInfo;
        }

        private Dictionary<string, BankInfo> GetOrAddBankCache(string playerNumber)
        {
            Dictionary<string, BankInfo> returnMe;
            if(!_bankCaches.TryGetValue(playerNumber, out returnMe))
            {
                returnMe = new Dictionary<string, BankInfo>();
                _bankCaches[playerNumber] = returnMe;
            }
            return returnMe;
        }
    }
}