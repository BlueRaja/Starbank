using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StarBank.Bank_Stuffs
{
    /// <summary>
    /// Represents information about a bank file.
    /// Does not contain any information FROM the bank-file; only information ABOUT it.
    /// </summary>
    public class BankInfo
    {
        public string BankPath;
        public string Name;
        public DateTime DateModified;
        public string PlayerNumber;
        public string AuthorNumber;

        public override string ToString()
        {
            return Name; //Debugging
        }
    }
}
