using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StarBank.Bank_Stuffs
{
    /// <summary>
    /// Represents the data stored inside a bank file
    /// </summary>
    public class Bank
    {
        public BankInfo BankInfo;
        public readonly IList<Section> Sections = new List<Section>();
        public string Signature;
        public string Version;

        public class Section
        {
            public readonly IList<Key> Keys = new List<Key>();
            public string Name;
            public override string ToString()
            {
                return Name; //Debugging
            }
        }

        public class Key
        {
            public string Name;
            public string Type;
            public string Value;
            public Section Section;
            public override string ToString()
            {
                return Name; //Debugging
            }
        }
    }
}
