using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StarBank.Bank_Stuffs;

namespace StarBank
{
    public class MapInfo : IComparable<MapInfo>
    {
        public string CachePath;
        public string Name;
        public string AuthorName;
        public bool IsProtected;
        public DateTime DateCreated;
        public IEnumerable<BankInfo> BankInfos;

        public int CompareTo(MapInfo other)
        {
            return Name.CompareTo(other.Name);
        }
    }
}
