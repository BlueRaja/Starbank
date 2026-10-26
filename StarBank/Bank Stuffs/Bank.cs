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

        /// <summary>
        /// A single value of a key in the bank file.
        /// Most keys have exactly one value, stored as &lt;Value type="value"/&gt;.  Some maps store several named values
        /// in one key (eg. &lt;XP fixed="1.5"/&gt;&lt;Type string="odkt"/&gt;); those are loaded as one Key object per value,
        /// all with the same Name and KeyGroup.
        /// </summary>
        public class Key
        {
            public const string DEFAULT_VALUE_NAME = "Value";

            public string Name;
            public string Type;
            public string Value;
            public Section Section;

            /// <summary>
            /// The name of the element the value is stored in ("Value" for normal keys).
            /// Null for a key without any value.
            /// </summary>
            public string ValueName = DEFAULT_VALUE_NAME;

            /// <summary>
            /// Keys loaded from the same &lt;Key&gt; element share the same KeyGroup; null means the key is its own group
            /// </summary>
            public object KeyGroup;

            /// <summary>
            /// True if this is one of several named values of a key
            /// </summary>
            public bool IsNamedValue
            {
                get { return ValueName != null && ValueName != DEFAULT_VALUE_NAME; }
            }

            /// <summary>
            /// The name shown in the editor: "Key" for normal keys, "Key.ValueName" for named values
            /// </summary>
            public string DisplayName
            {
                get { return (IsNamedValue ? Name + "." + ValueName : Name); }
            }

            public override string ToString()
            {
                return Name; //Debugging
            }
        }

        /// <summary>
        /// Groups the keys of a section the way they are stored in the bank file:
        /// one list per &lt;Key&gt; element, in the order the elements appear
        /// </summary>
        public static List<List<Key>> GroupKeys(Section section)
        {
            List<List<Key>> groups = new List<List<Key>>();
            Dictionary<object, List<Key>> groupsByToken = new Dictionary<object, List<Key>>();
            foreach(Key key in section.Keys)
            {
                object token = key.KeyGroup ?? key;
                List<Key> group;
                if(!groupsByToken.TryGetValue(token, out group))
                {
                    group = new List<Key>();
                    groupsByToken[token] = group;
                    groups.Add(group);
                }
                group.Add(key);
            }
            return groups;
        }
    }
}
