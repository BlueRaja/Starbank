using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace StarBank
{
    public partial class ProgressBarControl : UserControl
    {
        public ProgressBarControl()
        {
            InitializeComponent();
        }

        public string Status
        {
            get { return lblStatus.Text; }
            set { this.Invoke((Action)(() => lblStatus.Text = value)); }
        }

        public int Progress
        {
            get { return progressBar1.Value; }
            set { progressBar1.Value = value; }
        }
    }
}
