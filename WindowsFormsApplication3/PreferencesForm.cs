using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using Microsoft.Win32;

namespace BigNumbers.KeyboardPerApplication
{
    public partial class PreferencesForm : Form
    {
        DataTable dt;
        const string registryKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        const string appName = "KeyboardPerApplication";

        public PreferencesForm()
        {
            InitializeComponent();
            DataGridViewComboBoxColumn klc = (DataGridViewComboBoxColumn) dataGridView1.Columns[1];
            foreach (InputLanguage lang in InputLanguage.InstalledInputLanguages)
            {
                klc.Items.Add(lang.Culture.EnglishName.ToLower());
            }
        }

        public PreferencesForm(DataTable dt)
        {
            InitializeComponent();
            DataGridViewComboBoxColumn klc = (DataGridViewComboBoxColumn) dataGridView1.Columns[1];
            foreach (InputLanguage lang in InputLanguage.InstalledInputLanguages)
            {
                klc.Items.Add(lang.Culture.EnglishName.ToLower());
            }
            this.dt = dt;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            int ri = 0;
            foreach (DataRow dr in dt.Rows)
            {       
                dataGridView1.Rows.Add(dr.ItemArray[0]);
                DataGridViewComboBoxCell cell = ((DataGridViewComboBoxCell) dataGridView1.Rows[ri++].Cells[1]);
                cell.Value = dr.ItemArray[1].ToString().ToLower();
            }
            RegistryKey rk = Registry.CurrentUser.OpenSubKey(registryKey, true);
            checkBox1.Checked = (rk.GetValue(appName) != null);
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void okButton_Click(object sender, EventArgs e)
        {
            dt.Rows.Clear();
            foreach(DataGridViewRow dvr in dataGridView1.Rows) {
                string application = (string) dvr.Cells[0].Value;
                string keyboardLanguage  = (string) dvr.Cells[1].Value;
                bool validApplication = (application != null) && (application.Length > 0);
                bool validKeyboardLanguage = (keyboardLanguage != null) && (keyboardLanguage.Length > 0);
                if(validApplication && validKeyboardLanguage) {
                    dt.Rows.Add(application, keyboardLanguage);
                } else 
                {   
                    if((!validApplication && validKeyboardLanguage) ||
                       (validApplication && !validKeyboardLanguage))
                    {
                        MessageBox.Show("Preference with invalid/mising data [" + application + 
                                     ", " + keyboardLanguage+ "] can not be persisted and will be ignored");
                    }
                }

                }

            DialogResult = DialogResult.OK;
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey(registryKey, true);

            if (checkBox1.Checked)
            {
                rk.SetValue(appName, Application.ExecutablePath.ToString());
            }
            else
            {
                rk.DeleteValue(appName, false);
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
