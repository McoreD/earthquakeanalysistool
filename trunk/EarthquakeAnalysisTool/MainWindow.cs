﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using THTool.Helpers;
using THTool.Properties;

namespace THTool
{
    public partial class MainWindow : Form
    {
        private ATHMaker mAthGen = null;

        public MainWindow()
        {
            InitializeComponent();

            Control ctl = this.GetNextControl(this, true); // Get the first control in the tab order.

            while (ctl != null)
            {
                if (ctl.GetType() == typeof(TextBox))
                {
                    ctl.AllowDrop = true;
                    ((TextBox)ctl).DragDrop += new DragEventHandler(TextBox_DragDrop);
                    ctl.DragEnter += new DragEventHandler(TextBox_DragEnter);
                }
                ctl = this.GetNextControl(ctl, true); // Get the next control in the tab order.
            }

            decimal.Parse("0.00009181");

        }

        void TextBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.All;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        void TextBox_DragDrop(object sender, DragEventArgs e)
        {
            TextBox myTextBox = (TextBox)sender;
            string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop, true);
            if (paths.Length == 1)
            {
                myTextBox.Text = paths[0];
            }
        }

        private void tpATHGen_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.All;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void btnBrowseBaseATH_Click(object sender, EventArgs e)
        {
            BrowseFile(ref txtATHBaseFile);
        }

        private void BrowseFile(ref TextBox textBox)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                textBox.Text = dlg.FileName;
            }
        }

        private void btnBrowseSurfaceATH_Click(object sender, EventArgs e)
        {
            BrowseFile(ref txtATHSurfaceFile);
        }

        private bool IsValidWorkbook(string fp)
        {
            if (!string.IsNullOrEmpty(fp))
            {
                string fext = Path.GetExtension(fp);
                string[] exts = new string[] { ".xls", ".xlsx", ".xlsm" };
                foreach (string ext in exts)
                {
                    if (ext == fext)
                        return true;
                }
            }
            return false;
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (!IsValidWorkbook(txtExcelFile.Text))
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.Filter = "Excel Workbook (*.xlsx)|*.xlsx|Excel 97-2003 Workbook (*.xls)|*.xls";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtExcelFile.Text = dlg.FileName;
                }
            }

            if (IsValidWorkbook(txtExcelFile.Text))
            {
                bwApp.RunWorkerAsync();
                btnExport.Enabled = false;
            }

        }

        private void bwApp_DoWork(object sender, DoWorkEventArgs e)
        {

            ExcelReporterOptions ropt = new ExcelReporterOptions();
            ropt.Worker = this.bwApp;
            ropt.WorkbookFilePath = txtExcelFile.Text;
            ropt.CalculateDisplacements = chkCalcDisp.Checked;
            ropt.YieldAccel = nudYieldAccel.Value;

            SurfaceATHMakerOptions sao = new SurfaceATHMakerOptions();
            sao.InputFilePath = txtATHSurfaceFile.Text;
            sao.IgnoreZeroAcc = chkIgnoreZeroAccel.Checked;

            SurfaceATHMaker acm = new SurfaceATHMaker(sao);
            acm.MaxValues = 8 * (int)nudATHCount.Value;
            BaseATHMaker bm = new BaseATHMaker(txtATHBaseFile.Text);

            if (File.Exists(txtRPShake91.Text))
            {
                RPSiteMakerOptions rpsm_opt = new RPSiteMakerOptions();
                rpsm_opt.FilePath = txtRPShake91.Text;
                RPSiteMaker rpsm = new RPSiteMaker(rpsm_opt);
                ropt.MyRPSiteMaker = rpsm;
            }

            ExcelReporter er = new ExcelReporter(ropt);
            er.MySurfaceATHMaker = acm;
            er.MyBaseATHMaker = bm;
            er.CreateReport();

            if (File.Exists(txtExcelFile.Text))
            {
                Process.Start(txtExcelFile.Text);
            }

        }

        private void bwApp_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            switch (e.ProgressPercentage)
            {
                case 0: // Update Progress Max
                    pbarApp.Maximum = (int)e.UserState;
                    pbarApp.Value = 0;
                    break;
                case 1: // Increment Progress
                    pbarApp.Increment(1);
                    break;
                case 2: // Update Message
                    statusApp.Text = e.UserState.ToString();
                    break;
            }
        }

        private void bwApp_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            btnExport.Enabled = true;
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            Settings.Default.Save();
        }

        private void btnExportATH_Click(object sender, EventArgs e)
        {
            if (File.Exists(txtShake91ATH.Text))
            {
                mAthGen = new ATHMaker(new string[] { txtShake91ATH.Text });
            }
        }

    }
}
