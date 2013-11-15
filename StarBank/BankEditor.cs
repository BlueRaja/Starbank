using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using BrightIdeasSoftware;
using StarBank.Bank_Stuffs;

namespace StarBank
{
    public partial class BankEditor : UserControl
    {
        private Bank _bank;
        private bool _hasBeenEdited;

        public Bank Bank
        {
            set
            {
                _bank = value;
                RefreshBankProperties();
            }
        }

        private void RefreshBankProperties()
        {
            if(_bank == null)
            {
                objectListView1.SetObjects(null);
                objectListView1.Enabled = false;
                contextMenuStrip1.Enabled = false;
            }
            else
            {
                objectListView1.SetObjects(_bank.Sections.SelectMany(o => o.Keys));
                objectListView1.Enabled = true;
                contextMenuStrip1.Enabled = true;
            }
        }

        public BankEditor()
        {
            InitializeComponent();
            this.columnName.GroupKeyGetter = GroupKeyGetter;
            this.columnName.GroupKeyToTitleConverter = GroupKeyToTitleConverter;
        }

        private object GroupKeyGetter(object rowobject)
        {
            Bank.Key key = (Bank.Key) rowobject;
            return key.Section;
        }

        private string GroupKeyToTitleConverter(object groupkey)
        {
            Bank.Section section = (Bank.Section) groupkey;
            return section.Name;
        }

        private void SaveBank()
        {
            BankWriter writer = new BankWriter();
            writer.WriteBank(_bank, _bank.BankInfo.BankPath);
        }

        #region Checkbox for type column
        //Most of the code is taken almost verbatim from the ObjectListView example
        private void objectListView1_CellEditStarting(object sender, BrightIdeasSoftware.CellEditEventArgs e)
        {
            if(e.Column != columnType)
                return;

            ComboBox cb = new ComboBox();
            cb.Bounds = e.CellBounds;
            cb.Font = ((ObjectListView)sender).Font;
            cb.DropDownStyle = ComboBoxStyle.DropDownList;
            cb.Items.AddRange(new string[] { "fixed", "flag", "int", "point", "string", "text", "unit" });
            cb.SelectedItem = ((Bank.Key) e.RowObject).Type;
            cb.SelectedIndexChanged += cb_SelectedIndexChanged;
            cb.Tag = e.RowObject;
            e.Control = cb;
        }

        private void cb_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            ((Bank.Key)cb.Tag).Type = (string) cb.SelectedItem;
        }

        private void objectListView1_CellEditFinishing(object sender, CellEditEventArgs e)
        {
            _hasBeenEdited = true;

            if(e.Column == columnType)
            {
                // Stop listening for change events
                ((ComboBox) e.Control).SelectedIndexChanged -= new EventHandler(cb_SelectedIndexChanged);

                // Any updating will have been down in the SelectedIndexChanged event handler
                // Here we simply make the list redraw the involved ListViewItem
                ((ObjectListView) sender).RefreshItem(e.ListViewItem);

                // We have updated the model object, so we cancel the auto update
                e.Cancel = true;
            }
        }

        private void objectListView1_FormatRow(object sender, FormatRowEventArgs e)
        {
            //This is a bit of a hack - CellEditFinishing gets called BEFORE the model is updated, meaning changes to
            // values won't actually get saved immediately.  FormatRow is the only other event that is called after editing a cell
            // (not even FormatCell is called - lolwut)
            //However, it is also called in a lot of other cases, so to prevent excess disk I/O, we have to manually keep track of when
            // the cell was actually edited.
            if(_hasBeenEdited)
            {
                _hasBeenEdited = false;
                SaveBank();
            }
        }
        #endregion

        private void objectListView1_CellEditValidating(object sender, CellEditEventArgs e)
        {
            if(e.Column == columnValue)
            {
                Bank.Key key = (Bank.Key) e.RowObject;
                if(!IsValidKeyValue(key.Type, (string) e.NewValue))
                {
                    MessageBox.Show("Invalid entry for type: " + key.Type, "Invalid entry", MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                    e.Cancel = true;
                    objectListView1.CancelCellEdit();
                }
            }
        }

        private bool IsValidKeyValue(string type, string value)
        {
            switch(type)
            {
                case "fixed":
                    return Regex.IsMatch(value, @"^(\d*\.?\d+)$");
                case "flag":
                    return value == "0" || value == "1";
                case "int":
                    return Regex.IsMatch(value, @"^\d+$");
                default:
                    return true;
            }
        }

        #region Context Menu
        private void addNewSectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewSectionForm newSectionForm = new NewSectionForm();
            if (newSectionForm.ShowDialog() == DialogResult.OK)
            {
                Bank.Section section = new Bank.Section {Name = newSectionForm.SectionName};
                _bank.Sections.Add(section);
                SaveBank();
                RefreshBankProperties();
            }
        }

        private void addNewItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewSectionItemForm newSectionItemForm = new NewSectionItemForm(_bank);

            Bank.Key selectedKey = GetSelectedKey();
            if(selectedKey != null)
                newSectionItemForm.Section = selectedKey.Section;

            if(newSectionItemForm.ShowDialog() == DialogResult.OK)
            {
                Bank.Section selectedSection = newSectionItemForm.Section;
                Bank.Key bankKey = new Bank_Stuffs.Bank.Key();
                bankKey.Name = newSectionItemForm.ItemName;
                bankKey.Section = selectedSection;
                bankKey.Type = "string";
                bankKey.Value = "";
                selectedSection.Keys.Add(bankKey);
                SaveBank();
                RefreshBankProperties();
            }
        }

        private void renameItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            objectListView1.EditSubItem(objectListView1.SelectedItem, 0);
        }

        private void deleteItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure you want to delete this item?", "Delete item", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if(result == DialogResult.OK)
            {
                Bank.Key selectedKey = GetSelectedKey();
                selectedKey.Section.Keys.Remove(selectedKey);
                SaveBank();
                RefreshBankProperties();
            }
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            if(_bank == null)
                e.Cancel = true;
            deleteItemToolStripMenuItem.Enabled = (GetSelectedKey() != null);
        }
        #endregion

        private Bank.Key GetSelectedKey()
        {
            return (objectListView1.SelectedObject as Bank.Key);
        }
    }
}
