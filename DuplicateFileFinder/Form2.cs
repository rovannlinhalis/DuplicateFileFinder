using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DuplicateFileFinder
{
    public partial class Form2 : Form
    {
        FileViewModel Model { get; set; }
        public Form2(FileViewModel _model)
        {
            Model = _model;
            InitializeComponent();
            dataGridView1.AutoGenerateColumns = false;
            this.Text = Model.Nome;
            dataGridView1.DataSource = this.Model.Arquivos;
        }

        private void Form2_Load(object sender, EventArgs e)
        {

        }

        private void dataGridView1_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex >=0)
            {
                if (e.ColumnIndex == ColumnFileName.Index)
                {
                    ProcessStartInfo pfi = new ProcessStartInfo(Path.Combine(this.Model.Arquivos[e.RowIndex].Pasta, this.Model.Arquivos[e.RowIndex].Nome));
                    System.Diagnostics.Process.Start(pfi);
                }
                else if (e.ColumnIndex == ColumnFilePasta.Index)
                {
                    
                    if (this.Model.Arquivos != null)
                    {
                        string args = null;
                        if (String.IsNullOrWhiteSpace(this.Model.Arquivos[e.RowIndex].Nome))
                        {
                            string folder = this.Model.Arquivos[e.RowIndex].Pasta;
                            args = string.Format("\"{0}\"", folder);
                        }
                        else
                        {
                            string fileToSelect = this.Model.Arquivos[e.RowIndex].Nome;
                            args = string.Format("/Select, \"{0}\"", Path.Combine(this.Model.Arquivos[e.RowIndex].Pasta, fileToSelect));
                        }


                        ProcessStartInfo pfi = new ProcessStartInfo("Explorer.exe", args);
                        System.Diagnostics.Process.Start(pfi);
                    }
                }
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >=0)
            {
                if (e.ColumnIndex == ColumnDelete.Index)
                {
                    if (this.Model.Arquivos[e.RowIndex].Info.Exists)
                    {
                        if (MessageBox.Show("Tem certeza que deseja excluir o arquivo selecionado?","Excluir Arquivo", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            try
                            {
                                this.Model.Arquivos[e.RowIndex].Info.Delete();
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
        }
    }
}
