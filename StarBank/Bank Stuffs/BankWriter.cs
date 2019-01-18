using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace StarBank.Bank_Stuffs
{
    public class BankWriter
    {
        private BankSignatureGenerator _signatureGenerator = new BankSignatureGenerator();

        public void WriteBank(Bank bank, string path)
        {
            XDocument document = CreateDocument(bank);
            document.Save(path);
        }

        private XDocument CreateDocument(Bank bank)
        {
            return new XDocument(new XDeclaration("1.0", "utf-8", "yes"),
                                  new XElement("Bank", new XAttribute("version", bank.Version),
                                   CreateSections(bank.Sections),
                                   CreateSignature(bank)));
        }

        private XElement[] CreateSections(IEnumerable<Bank.Section> sections)
        {
            IList<XElement> sectionElements = new List<XElement>();
            foreach(Bank.Section section in sections)
            {
                sectionElements.Add(CreateSection(section));
            }
            return sectionElements.ToArray();
        }

        private XElement CreateSection(Bank.Section section)
        {
            return new XElement("Section", new XAttribute("name", section.Name), CreateKeys(section.Keys));
        }

        private XElement[] CreateKeys(IEnumerable<Bank.Key> keys)
        {
            IList<XElement> keyElements = new List<XElement>();
            foreach(Bank.Key key in keys)
            {
                keyElements.Add(CreateKey(key));
            }
            return keyElements.ToArray();
        }

        private XElement CreateKey(Bank.Key key)
        {
            
            return new XElement("Key", new XAttribute("name", key.Name), CreateItems(key.Items));
        }

        private XElement[] CreateItems(IEnumerable<Bank.Item> items)
        {
            IList<XElement> itemElements = new List<XElement>();
            foreach (var item in items)
            {
                itemElements.Add(CreateItem(item));
            }
            return itemElements.ToArray();
        }

        private XElement CreateItem(Bank.Item item)
        {

            return new XElement(item.Name, new XAttribute(item.Type, item.Value));
        }



        private XElement CreateSignature(Bank bank)
        {
            _signatureGenerator.UpdateSignature(bank);
            return new XElement("Signature", new XAttribute("value", bank.Signature));
        }
    }
}
