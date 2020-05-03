using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DuplicateFileFinder
{
    public partial class Form1 : Form
    {
        List<string> IgnoredFolders = new List<string>() {"DOCUMENTS AND SETTINGS", "APPDATA","ALLUSERS","PROGRAM DATA", "PROGRAMDATA", "WINDOWS","PROGRAM FILES","PROGRAM FILES (X86)","ARQUIVOS DE PROGRAMAS","ARQUIVOS DE PROGRAMAS (X86)" };
        ConcurrentQueue<string> FilaFiles;
        List<ErrorDir> ErrorDirs;
        SortableBindingList<FileViewModel> Source;
        List<FileViewModel> SourceAux;
        int lidos = 0, processados = 0, progresso =0;
        string atual = "";

        bool runn = false;
        bool listing = false;
        public Form1()
        {
            InitializeComponent();
            dataGridView1.AutoGenerateColumns = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (runn)
            {
                runn = false;
                listing = false;
                button1.Text = "Start";
            }
            else
            {
                button1.Text = "Stop";
                if (!backgroundWorker1.IsBusy)
                {
                    progressBar1.Style = ProgressBarStyle.Continuous;
                    progressBar1.Value = 0;
                    Source = new SortableBindingList<FileViewModel>();
                    SourceAux = new List<FileViewModel>();
                    FilaFiles = new ConcurrentQueue<string>();
                    ErrorDirs = new List<ErrorDir>();
                    lidos = 0;
                    processados = 0;
                    progresso = 0;
                    
                    dataGridView1.DataSource = Source;
                    runn = true;
                    listing = true;
                    backgroundWorker1.RunWorkerAsync(new { dirs = textBox1.Text, exts = textBox2.Text });
                    if (!backgroundWorker2.IsBusy)
                        backgroundWorker2.RunWorkerAsync(numericUpDown1.Value);
                }
            }
        }

        private void GetFiles(string dir, string[] exts)
        {
            atual = dir;
            foreach (string ex in exts)
            {
                if (runn && listing)
                {
                    string[] files = Directory.GetFiles(dir, ex, SearchOption.TopDirectoryOnly);
                    lidos += files.Length;
                    foreach (string f in files)
                        FilaFiles.Enqueue(f);
                }
            }
            string[] dirs = Directory.GetDirectories(dir, "*", SearchOption.TopDirectoryOnly);

            if (runn && listing)
            foreach (string d in dirs)
            {
                if (!IgnoredFolders.Contains(d.ToUpper()) && !d.StartsWith("$"))
                {
                    try
                    {
                        GetFiles(d, exts);
                    }
                    catch (Exception ex)
                    {
                        ErrorDirs.Add(new ErrorDir() { Dir = d, Erro = ex.Message });
                    }
                }
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            dynamic arg = e.Argument as dynamic;
            
            string[] searchDirs = arg.dirs.Split(',');
            string[] searchExts = arg.exts.Split(',');

            e.Result = Parallel.ForEach(searchDirs, x => {
                GetFiles(x, searchExts);
            });

        }

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            int mb = (int)((decimal)e.Argument);
            long tamanhoMinimo = 1;
            for (int i = 0; i < mb; i++)
            {
                tamanhoMinimo = tamanhoMinimo + 1024 * 1024;
            }
            tamanhoMinimo = tamanhoMinimo == 1 ? 0 : tamanhoMinimo;

            while (listing && runn)
            {
                backgroundWorker2.ReportProgress(0);

                while (FilaFiles.Count > 0 && runn)
                {
                    if (FilaFiles.TryDequeue(out string file))
                    {
                        progresso++;
                        XFile obj = new XFile(file);

                        if (obj.Info.Exists)
                            if (obj.Info.Length >= tamanhoMinimo || tamanhoMinimo == 0)
                            {
                                obj.GerarHash();

                                if (Source.Any(x => x.Hash == obj.Hash && x.Extensao.ToLower() == obj.Info.Extension.ToLower() && x.Tamanho == obj.Info.Length))
                                {
                                    var src = Source.Where(x => x.Hash == obj.Hash && x.Extensao.ToLower() == obj.Info.Extension.ToLower() && x.Tamanho == obj.Info.Length).FirstOrDefault();

                                    if (src != null)
                                        src.Arquivos.Add(obj);
                                    else
                                        FilaFiles.Enqueue(file);

                                }
                                else if (SourceAux.Any(x => x.Hash == obj.Hash && x.Extensao.ToLower() == obj.Info.Extension.ToLower() && x.Tamanho == obj.Info.Length))
                                {
                                    var src = SourceAux.Where(x => x.Hash == obj.Hash && x.Extensao.ToLower() == obj.Info.Extension.ToLower() && x.Tamanho == obj.Info.Length).FirstOrDefault();
                                    if (src != null)
                                    {
                                        src.Arquivos.Add(obj);
                                        src.PropertyChanged += (ss, ee) =>
                                        {

                                            if (dataGridView1.InvokeRequired)
                                            {
                                                dataGridView1.Invoke((MethodInvoker)delegate { dataGridView1.Invalidate(); });
                                            }
                                            else
                                                dataGridView1.Invalidate();

                                        };

                                        if (dataGridView1.InvokeRequired)
                                        {
                                            dataGridView1.Invoke((MethodInvoker)delegate { Source.Add(src); });
                                        }
                                        else
                                            Source.Add(src);
                                    }
                                    else
                                        FilaFiles.Enqueue(file);
                                }
                                else
                                {
                                    FileViewModel model = new FileViewModel(obj);
                                    SourceAux.Add(model);
                                }
                                processados++;
                                atual = obj.Info.Name;

                                backgroundWorker2.ReportProgress(0);
                            }
                    }
                }

                if (runn)
                    Thread.Sleep(3000);
            }
        }

        public class ErrorDir
        {
            public string Dir { get; set; }
            public string Erro { get; set; }
        }

        private void backgroundWorker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            
            progressBar1.Style = ProgressBarStyle.Continuous;
            progressBar1.Value = 100;
            runn = false;
            button1.Text = "Start";
            dataGridView2.DataSource = ErrorDirs;
            labelStatus.Text = "Lidos=" + lidos + " / Processados=" + processados + "/ Ignorados=" + (progresso - processados) + " - Fim do Processo";
            tabPage1.Text = "Arquivos [" + dataGridView1.Rows.Count + "]";
            tabPage2.Text = "Pastas Não Processadas [" + dataGridView2.Rows.Count + "]";
        }

        private void dataGridView1_Validated(object sender, EventArgs e)
        {
            
        }

        //private void dataGridView1_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        //{
        //    string strColumnName = dataGridView1.Columns[e.ColumnIndex].DataPropertyName;
        //    if (!String.IsNullOrWhiteSpace(strColumnName))
        //    {
        //        if (strColumnName == "TamanhoF")
        //            strColumnName = "Tamanho";
        //        else if (strColumnName == "TamanhoTotalF")
        //            strColumnName = "TamanhoTotal";

        //        SortOrder strSortOrder = getSortOrder(e.ColumnIndex);

        //        if (strSortOrder == SortOrder.Ascending)
        //        {
        //            Source = new BindingList<FileViewModel>(Source.OrderBy(x => typeof(FileViewModel).GetProperty(strColumnName).GetValue(x, null)).ToList());
        //        }
        //        else
        //        {
        //            Source = new BindingList<FileViewModel>(Source.OrderByDescending(x => typeof(FileViewModel).GetProperty(strColumnName).GetValue(x, null)).ToList());
        //        }
        //        dataGridView2.DataSource = Source;
        //        dataGridView2.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection = strSortOrder;
        //    }
        //}
        private SortOrder getSortOrder(int columnIndex)
        {
            if (dataGridView1.Columns[columnIndex].HeaderCell.SortGlyphDirection == SortOrder.None ||
                dataGridView1.Columns[columnIndex].HeaderCell.SortGlyphDirection == SortOrder.Descending)
            {
                dataGridView1.Columns[columnIndex].HeaderCell.SortGlyphDirection = SortOrder.Ascending;
                return SortOrder.Ascending;
            }
            else
            {
                dataGridView1.Columns[columnIndex].HeaderCell.SortGlyphDirection = SortOrder.Descending;
                return SortOrder.Descending;
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            listing = false;
        }

        private void backgroundWorker2_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            tabPage1.Text = "Arquivos [" + dataGridView1.Rows.Count + "]";
            labelStatus.Text = "Lidos=" + lidos + " / Processados=" + processados + "/ Ignorados="+ (lidos - processados)+ " / Arquivo="+ atual;
            progressBar1.Maximum = lidos;
            progressBar1.Minimum = 0;
            progressBar1.Value = progresso > progressBar1.Maximum ? progressBar1.Maximum : progresso < progressBar1.Minimum ? progressBar1.Minimum : progresso;
        }

        private void dataGridView1_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                FileViewModel model = Source[e.RowIndex];
                Form2 form = new Form2(model);
                form.Show();
            }
        }
    }
}
