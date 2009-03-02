﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using AccelerationTimeHistoryGen.Helpers;

namespace AccelerationTimeHistoryGen
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

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop, true);
            if (paths.Length >= 1)
            {
                mAthGen = new ATHMaker(paths);
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

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtExcelFile.Text))
            {
                SaveFileDialog dlg = new SaveFileDialog();
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtExcelFile.Text = dlg.FileName;
                    SurfaceATHMaker acm = new SurfaceATHMaker(txtATHSurfaceFile.Text);
                    acm.MaxValues = 8 * (int)nudATHCount.Value;
                    BaseATHMaker bm = new BaseATHMaker(txtATHBaseFile.Text);
                    ExcelReporter er = new ExcelReporter(txtExcelFile.Text);
                    er.MySurfaceATHMaker = acm;
                    er.MyBaseATHMaker = bm;
                    er.CreateReport();

                    if (File.Exists(txtExcelFile.Text))
                    {
                        Process.Start(txtExcelFile.Text);
                    }
                }
            }
        }

    }
}
