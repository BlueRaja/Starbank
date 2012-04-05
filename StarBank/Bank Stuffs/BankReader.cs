using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace StarBank.Bank_Stuffs
{
    public class BankReader
    {
        public Bank LoadBankFromPath(BankInfo bankInfo)
        {
            XDocument xmlDocument = XDocument.Load(bankInfo.BankPath);
            Bank bank = CreateBank(xmlDocument);
            bank.BankInfo = bankInfo;
            return bank;
        }

        private Bank CreateBank(XDocument xmlDocument)
        {
            Bank bank = new Bank();
            
            //Check for bank-tag
            var bankNode = xmlDocument.Descendants("Bank").FirstOrDefault();
            if(bankNode == null)
                return null;

            //Check for version
            var versionAttribute = bankNode.Attribute("version");
            if (versionAttribute != null)
                bank.Version = versionAttribute.Value;

            //Add sections
            XAttribute nameAttribute;
            foreach(var sectionNode in bankNode.Descendants("Section"))
            {
                Bank.Section section = new Bank.Section();
                bank.Sections.Add(section);

                nameAttribute = sectionNode.Attribute("name");
                if(nameAttribute != null)
                    section.Name = nameAttribute.Value;

                //Add keys
                foreach(var keyNode in sectionNode.Descendants("Key"))
                {
                    Bank.Key key = new Bank.Key();
                    key.Section = section;
                    section.Keys.Add(key);

                    nameAttribute = keyNode.Attribute("name");
                    if(nameAttribute != null)
                        key.Name = nameAttribute.Value;

                    //Key type/value are stored in the attributes of a single child "Value" node
                    var valueNode = keyNode.Descendants("Value").FirstOrDefault();
                    if(valueNode != null)
                    {
                        var valueAttribute = valueNode.Attributes().FirstOrDefault();
                        if(valueAttribute != null)
                        {
                            key.Type = valueAttribute.Name.ToString();
                            key.Value = valueAttribute.Value;
                        }
                    }
                }
            }

            return bank;
        }
    }
}
