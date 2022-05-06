using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ArxivarSAPIntegration
{
    public partial class FormConfiguration : Form
    {
        private bool _toSave;

        public bool ToSave
        {
            get { return _toSave; }
            set { _toSave = value; }
        }


        public FormConfiguration(ServiceConfiguration config)
        {
            InitializeComponent();
            propertyGrid1.SelectedObject = config;
            ToSave = false;
        }

        private void btnSalva_Click(object sender, EventArgs e)
        {
            ToSave = true;
            this.Hide();
        }

        private void btnAnnulla_Click(object sender, EventArgs e)
        {
            ToSave = false;
            this.Hide();
        }


    }
}
