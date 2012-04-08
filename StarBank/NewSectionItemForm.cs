using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using StarBank.Bank_Stuffs;

namespace StarBank
{
    public sealed partial class NewSectionItemForm : Form
    {
        public NewSectionItemForm(Bank _bank)
        {
            InitializeComponent();
            cmbSections.Items.AddRange(_bank.Sections.ToArray());
        }

        public string ItemName
        {
            get { return txtName.Text; }
        }

        public Bank.Section Section
        {
            get { return (Bank.Section) cmbSections.SelectedItem; }
            set { cmbSections.SelectedItem = value; }
        }

        private void cmbSections_Format(object sender, ListControlConvertEventArgs e)
        {
            Bank.Section section = (Bank.Section) e.ListItem;
            e.Value = section.Name;
        }

        #region Refresh OK button
        private void txtName_TextChanged(object sender, EventArgs e)
        {
            RefreshOkButton();
        }

        private void cmbSections_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshOkButton();
        }

        private void RefreshOkButton()
        {
            btnOk.Enabled = (cmbSections.SelectedItem != null && !String.IsNullOrEmpty(txtName.Text));
        }
        #endregion

        private void btnOk_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }
    }
}
