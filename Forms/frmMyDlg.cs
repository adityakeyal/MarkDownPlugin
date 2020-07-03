using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Kbg.NppPluginNET
{
    public partial class frmMyDlg : Form
    {
        public string filename { get; set; }
        public frmMyDlg()
        {
            InitializeComponent();
            DialogResult dialogResult = saveFileDialog1.ShowDialog();
            filename  = saveFileDialog1.FileName;
            this.Load += (s, e) => this.Close();
        }

     
    }
}
