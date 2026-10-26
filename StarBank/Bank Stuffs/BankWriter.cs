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
            return new XElement("Section", new XAttribute("name", section.Name), CreateKeys(section));
        }

        private XElement[] CreateKeys(Bank.Section section)
        {
            IList<XElement> keyElements = new List<XElement>();
            foreach(List<Bank.Key> keyGroup in Bank.GroupKeys(section))
            {
                keyElements.Add(CreateKey(keyGroup));
            }
            return keyElements.ToArray();
        }

        /// <summary>
        /// Creates a single &lt;Key&gt; element containing the values of all the given keys (which share the same name)
        /// </summary>
        private XElement CreateKey(List<Bank.Key> keyGroup)
        {
            XElement keyElement = new XElement("Key", new XAttribute("name", keyGroup[0].Name));
            foreach(Bank.Key key in keyGroup.Where(o => o.ValueName != null))
            {
                XElement valueElement = new XElement(key.ValueName);
                if(!String.IsNullOrEmpty(key.Type))
                    valueElement.Add(new XAttribute(key.Type, key.Value ?? ""));
                keyElement.Add(valueElement);
            }
            return keyElement;
        }

        private XElement CreateSignature(Bank bank)
        {
            _signatureGenerator.UpdateSignature(bank);
            return new XElement("Signature", new XAttribute("value", bank.Signature));
        }
    }
}
