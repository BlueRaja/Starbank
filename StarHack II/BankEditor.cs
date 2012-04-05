﻿using System;
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
            objectListView1.SetObjects(_bank.Sections.SelectMany(o => o.Keys));
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

            //Save the results right away
            SaveBank();
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
                Bank.Section section = new Bank.Section {Name = newSectionForm.Text};
                _bank.Sections.Add(section);
                SaveBank();
            }
        }

        private void addNewItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //NewSectionForm newSectionForm = new NewSectionForm("Enter name of new item", "Enter item name");
            NewSectionForm newSectionForm = new NewSectionForm();
            if(newSectionForm.ShowDialog() == DialogResult.OK)
            {
                //TODO: Create a new item
            }
        }

        private void renameItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            objectListView1.EditSubItem(objectListView1.SelectedItem, 0);
        }
        #endregion
    }
}
