using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace StarBank.Bank_Stuffs
{
    public class BankPathParser
    {
        private static readonly Regex VALID_NUMBER = new Regex(@"\A\d-S\d-\d-\d+\Z");

        public BankPathParser(Bank bank) : this(bank.BankInfo.BankPath)
        {
        }

        public BankPathParser(string bankPath)
        {
            if(!IsValidBankPath(bankPath))
                throw new ArgumentException("Cannot parse bank-path: " + bankPath, "bankPath"); //TODO: Handle thrown exception
            AuthorNumber = GetAuthorNumber(bankPath);
            PlayerNumber = GetPlayerNumber(bankPath);
        }

        /// <summary>
        /// Path should look something like this:
        /// C:\Users\BlueRaja\Documents\StarCraft II\Accounts\123456789\1-S2-1-1231231\Banks\1-S2-1-7897897\MyBank.SC2Bank
        /// The first group of "1-S2-1-..." numbers is the player; the second group is the map-author
        /// 
        /// This method returns true if the path is of the above form, or false otherwise
        /// </summary>
        /// <param name="bankPath"></param>
        /// <returns></returns>
        public static bool IsValidBankPath(string bankPath)
        {
            return IsValidPlayerOrAuthorNumber(GetAuthorNumber(bankPath))
                && IsValidPlayerOrAuthorNumber(GetPlayerNumber(bankPath));
        }

        /// <summary>
        /// Returns true only if the number is of the form "1-S2-1-...", the format
        /// required for player and author numbers.
        /// </summary>
        public static bool IsValidPlayerOrAuthorNumber(string playerOrAuthorNumber)
        {
            return VALID_NUMBER.IsMatch(playerOrAuthorNumber);
        }

        private static string GetAuthorNumber(string bankPath)
        {
            return Path.GetFileName(Path.GetDirectoryName(bankPath));
        }

        private static string GetPlayerNumber(string bankPath)
        {
            return Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(bankPath))));
        }

        public string AuthorNumber { get; private set; }
        public string PlayerNumber { get; private set; }
    }
}
