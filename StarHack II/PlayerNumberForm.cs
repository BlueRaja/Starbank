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
    public partial class PlayerNumberForm : Form
    {
        private readonly Font _nonBoldFont;
        private readonly Font _boldFont;

        public string PlayerNumber { get { return txtPlayerNumber.Text; } }
        public string AuthorNumber { get { return txtAuthorNumber.Text; } }

        public PlayerNumberForm()
        {
            InitializeComponent();
            _nonBoldFont = lblPlayerNumber.Font;
            _boldFont = new Font(lblPlayerNumber.Font, FontStyle.Bold);
            ValidateTextChanged(this, null);
        }

        private void ValidateTextChanged(object sender, EventArgs e)
        {
            btnOk.Enabled = true;
            lblError.Visible = false;
            lblAuthorNumber.Font = _nonBoldFont;
            lblPlayerNumber.Font = _nonBoldFont;

            if(!BankPathParser.IsValidPlayerOrAuthorNumber(txtAuthorNumber.Text))
            {
                lblError.Text = "Invalid author number!";
                lblError.Visible = true;
                btnOk.Enabled = false;
                lblAuthorNumber.Font = _boldFont;
            }

            if(!BankPathParser.IsValidPlayerOrAuthorNumber(txtPlayerNumber.Text))
            {
                lblError.Text = "Invalid player number!";
                lblError.Visible = true;
                btnOk.Enabled = false;
                lblPlayerNumber.Font = _boldFont;
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
