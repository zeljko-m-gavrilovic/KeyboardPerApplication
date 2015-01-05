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

namespace WindowsFormsApplication3
{
    public partial class Form1 : Form
    {
        DataTable dt;

        public Form1()
        {
            InitializeComponent();
            DataGridViewComboBoxColumn klc = (DataGridViewComboBoxColumn) dataGridView1.Columns[1];
            foreach (InputLanguage lang in InputLanguage.InstalledInputLanguages)
            {
                klc.Items.Add(lang.Culture.EnglishName.ToLower());
            }
        }

        public Form1(DataTable dt)
        {
            InitializeComponent();
            DataGridViewComboBoxColumn klc = (DataGridViewComboBoxColumn)dataGridView1.Columns[1];
            foreach (InputLanguage lang in InputLanguage.InstalledInputLanguages)
            {
                klc.Items.Add(lang.Culture.EnglishName.ToLower());
            }
            this.dt = dt;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //DataGridViewColumn ac = dataGridView1.Columns[0];

            int ri = 0;
            foreach (DataRow dr in dt.Rows)
            {
                //DataRow dataRow = new DataRow();
                //dataRow[0] = dr.ItemArray[0];
                
                dataGridView1.Rows.Add(dr.ItemArray[0]);
                DataGridViewComboBoxCell cell = ((DataGridViewComboBoxCell)dataGridView1.Rows[ri++].Cells[1]);
                    cell.Value = cell.Items[3];
            }
            
            //foreach (SettingsProperty userProperty in Properties.Settings.Default.Properties)
            //{
             //   dt.Rows.Add(userProperty.Name, userProperty.DefaultValue);
            //}
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void okButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
