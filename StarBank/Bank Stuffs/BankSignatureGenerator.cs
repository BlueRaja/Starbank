using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StarBank.Bank_Stuffs
{
    class BankSignatureGenerator
    {
        public void UpdateSignature(Bank bank)
        {
            //Start with author number, player number, and bank name
            StringBuilder inputString = new StringBuilder();
            inputString.Append(bank.BankInfo.AuthorNumber);
            inputString.Append(bank.BankInfo.PlayerNumber);
            inputString.Append(bank.BankInfo.Name);

            //Append each section name (ordered by ASCII values, not dictionary-ordering)...
            foreach(Bank.Section section in bank.Sections.OrderBy(o => o.Name, StringComparer.Ordinal))
            {
                inputString.Append(section.Name);
                
                //And between those, each key-name/value
                foreach(Bank.Key key in section.Keys.OrderBy(o => o.Name, StringComparer.Ordinal))
                {
                    foreach(var item in key.Items)
                    {
                        inputString.Append(key.Name);
                        inputString.Append(item.Name);
                        inputString.Append(item.Type);
                        inputString.Append(item.Value);
                    }
                }
            }

            //Finally, set the signature on the bank by taking the hash of the string
            bank.Signature = Sha1Hash(inputString.ToString());
        }

        private string Sha1Hash(string hashMe)
        {
            byte[] signatureInputBytes = Encoding.ASCII.GetBytes(hashMe);
            SHA1CryptoServiceProvider sha1Provider = new SHA1CryptoServiceProvider();
            byte[] hash = sha1Provider.ComputeHash(signatureInputBytes);
            return BitConverter.ToString(hash).Replace("-", "");
        }
    }
}
