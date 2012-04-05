using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace StarBank
{
    public sealed partial class NewSectionForm : Form
    {
        public NewSectionForm()
        {
            InitializeComponent();
        }

        public string SectionName
        {
            get { return textBox1.Text; }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            btnOk.Enabled = (!String.IsNullOrEmpty(textBox1.Text));
        }
    }
}
