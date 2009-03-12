﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Office.Interop.Excel;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace THTool.Helpers
{
    public struct ExcelReporterOptions
    {
        public BackgroundWorker Worker { get; set; }
        public string WorkbookFilePath { get; set; }
        public bool CalculateDisplacements { get; set; }
        public RPSiteMaker MyRPSiteMaker { get; set; }
        /// <summary>
        /// Yield Acceleration in g
        /// </summary>
        public decimal YieldAccel { get; set; }
    }

    public class ExcelReporter
    {
        private string mPath;

        /// <summary>
        /// ATH from Shake91
        /// </summary>
        public SurfaceATHMaker MySurfaceATHMaker { get; set; }
        /// <summary>
        /// ATH from Earthquake Location
        /// </summary>
        public BaseATHMaker MyBaseATHMaker { get; set; }

        private Microsoft.Office.Interop.Excel.Application mExcelApp = new Microsoft.Office.Interop.Excel.ApplicationClass();
        private Microsoft.Office.Interop.Excel.Workbook mWorkBook;
        private Microsoft.Office.Interop.Excel.Sheets mWorkSheets;

        public string ExcelFilePath { get; set; }

        private Microsoft.Office.Interop.Excel.Worksheet mWSheet1 = new Microsoft.Office.Interop.Excel.WorksheetClass();
        private Worksheet mWSheet2 = new Worksheet();

        private BackgroundWorker mBwApp;

        private ExcelReporterOptions Options { get; set; }

        int startRow = 3;

        /// <summary>
        /// Filepath of the Excel Report file
        /// </summary>
        /// <param name="fp"></param>
        public ExcelReporter(ExcelReporterOptions options)
        {
            this.Options = options;
            this.mPath = options.WorkbookFilePath;
            this.mBwApp = options.Worker;
        }

        /// <summary>
        /// Export ATH to Excel
        /// </summary>
        public void CreateReport()
        {
            //************************
            //* Initialize Worksheets
            //************************

            mExcelApp.DisplayAlerts = false;

            //mWorkBook = mExcelApp.Workbooks.Open(mXlsFilePath, 0, false, 5, "", "", true, XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);
            mWorkBook = mExcelApp.Workbooks.Add(Missing.Value);

            mWorkSheets = mWorkBook.Worksheets;

            mWSheet1 = (Worksheet)mWorkSheets.get_Item("Sheet1");
           // mWSheet1.Name = "ATH and DTH";
            mWSheet2 = (Worksheet)mWorkSheets.get_Item("Sheet2");
          //  mWSheet2.Name = "Response Spectra";

            if (this.MyBaseATHMaker != null && this.MySurfaceATHMaker != null)
            {
                FillATHData(mWSheet1);
                GenerateChartATH(mWSheet1);
            }
            if (this.Options.MyRPSiteMaker != null)
            {
                FillRPData(mWSheet2);
                GenerateChartRP(mWSheet2);
            }

            AutofitColumns(mWSheet1, "A1", "Z1");

            Range time1 = (Range)mWSheet1.Columns["A", Missing.Value];
            time1.ColumnWidth = 8.5;
            time1 = (Range)mWSheet1.Columns["H", Missing.Value];
            time1.ColumnWidth = 8.5;

            mWSheet1.Name = "ATH and DTH";
            mWSheet2.Name = "Response Spectra";
            mWSheet1.Select(true);

            string ext = Path.GetExtension(mPath);

            if (ext.ToLower().Equals(".xlsx"))
            {
                ExcelFilePath = SaveAs2007(mPath);
            }
            else if (ext.ToLower().Equals(".xls"))
            {
                ExcelFilePath = SaveAs2003(mPath);
            }

            mExcelApp.DisplayAlerts = true;

            mExcelApp.Quit();

        }

        private void FillRPData(Worksheet ws)
        {
            ws.Cells[2, 1] = "Period (s)";
            ws.Cells[2, 2] = "Accl(m/ss)"; 

            this.Options.MyRPSiteMaker.ReadData();
            if (this.Options.MyRPSiteMaker.ATH.Count > 0 && this.Options.MyRPSiteMaker.PeriodList.Count > 0)
            {
                List<string> p = this.Options.MyRPSiteMaker.PeriodList;
                List<string> acc = this.Options.MyRPSiteMaker.ATH;
                double[,] arrData = new double[p.Count, 2];
                for (int i = 0; i < p.Count; i++)
                {
                    double.TryParse(p[i], out arrData[i, 0]);
                    double.TryParse(acc[i], out arrData[i, 1]);
                }
                Range rng = ws.get_Range(string.Format("A{0}", startRow), string.Format("B{0}", startRow + acc.Count - 1));
                rng.Value2 = arrData;
            }
        }

        private void FillATHData(Worksheet ws)
        {
            List<string> accBase = MyBaseATHMaker.ReadATH();
            int dtBase = MyBaseATHMaker.DT;

            mBwApp.ReportProgress(0, (this.Options.CalculateDisplacements ? 10 : 4));
            mBwApp.ReportProgress(2, "Filling Base ATH, VTH and DTH...");

            double[,] arrData = new double[accBase.Count, 1];
            string[,] arrString = new string[accBase.Count, 1];

            Range headings = ws.get_Range("A1", "Z2");
            headings.Font.Bold = true;

            ws.Cells[1, 1] = "Base: " + MyBaseATHMaker.Title;

            // Times
            ws.Cells[2, 1] = "Time (s)";
            Range rTime = ws.get_Range(string.Format("A{0}", startRow), string.Format("A{0}", startRow + accBase.Count - 1));
            for (int i = 0; i < accBase.Count; i++)
            {
                arrData[i, 0] = (double)(i * dtBase / 1000.0);
            }
            rTime.Value2 = arrData;
            mBwApp.ReportProgress(1);

            // ATH (G)
            ws.Cells[2, 2] = "Accel (g)";
            Range rAthG = ws.get_Range(string.Format("B{0}", startRow), string.Format("B{0}", startRow + accBase.Count - 1));
            for (int i = 0; i < accBase.Count; i++)
            {
                double value;
                double.TryParse(accBase[i], out value);
                arrData[i, 0] = value;
            }
            rAthG.Value2 = arrData;
            mBwApp.ReportProgress(1);

            if (this.Options.CalculateDisplacements)
            {
                // ATH (m/ss)
                ws.Cells[2, 3] = "Accel (m/ss)";
                Range rAth = ws.get_Range(string.Format("C{0}", startRow), string.Format("C{0}", startRow + accBase.Count - 1));
                for (int i = 0; i < accBase.Count; i++)
                {
                    arrString[i, 0] = "=RC[-1]*9.81";
                }
                rAth.FormulaArray = arrString;
                mBwApp.ReportProgress(1);

                // VTH (m/s)
                ws.Cells[2, 4] = "veloc (m/s)";
                Range rVth = ws.get_Range(string.Format("D{0}", startRow + 1), string.Format("D{0}", startRow + accBase.Count - 1));
                for (int i = 0; i < accBase.Count; i++)
                {
                    arrString[i, 0] = "=0.5*(RC[-1]+R[-1]C[-1])*(RC[-3]-R[-1]C[-3])+R[-1]C";
                }
                rVth.FormulaArray = arrString;
                rVth.Style = "Calculation";
                mBwApp.ReportProgress(1);

                // DTH (m)
                ws.Cells[2, 5] = "disp (m)";
                Range rDth = ws.get_Range(string.Format("E{0}", startRow + 1), string.Format("E{0}", startRow + accBase.Count - 1));
                for (int i = 0; i < accBase.Count; i++)
                {
                    arrString[i, 0] = "=0.5*(RC[-1]+R[-1]C[-1])*(RC[-4]-R[-1]C[-4])+R[-1]C";
                }
                rDth.FormulaArray = arrString;
                rDth.Style = "Output";
                mBwApp.ReportProgress(1);

            }

            List<string> accSurface = MySurfaceATHMaker.ReadATH();
            double dt = ((double)accBase.Count / (double)accSurface.Count) * dtBase;

            arrData = new double[accSurface.Count, 1];
            arrString = new string[accSurface.Count, 1];

            mBwApp.ReportProgress(2, "Filling Base ATH, VTH and DTH...");

            ws.Cells[1, 8] = "Surface: " + MySurfaceATHMaker.Title;

            // Times
            ws.Cells[2, 8] = "Time (s)";
            Range rTimeS = ws.get_Range(string.Format("H{0}", startRow), string.Format("H{0}", startRow + accSurface.Count - 1));
            for (int i = 0; i < accSurface.Count; i++)
            {
                arrData[i, 0] = (double)(i * dt / 1000.0);
            }
            rTimeS.Value2 = arrData;
            mBwApp.ReportProgress(1);

            // ATH (G)
            ws.Cells[2, 9] = "Accel (g)";
            Range rAthGS = ws.get_Range(string.Format("I{0}", startRow), string.Format("I{0}", startRow + accSurface.Count - 1));
            for (int i = 0; i < accSurface.Count; i++)
            {
                double value;
                double.TryParse(accSurface[i], out value);
                arrData[i, 0] = value;
            }
            rAthGS.Value2 = arrData;
            mBwApp.ReportProgress(1);

            if (this.Options.CalculateDisplacements)
            {
                // ATH (m/ss)
                ws.Cells[2, 10] = "Accel (m/ss)";
                Range rAthS = ws.get_Range(string.Format("J{0}", startRow), string.Format("J{0}", startRow + accSurface.Count - 1));
                for (int i = 0; i < accSurface.Count; i++)
                {
                    arrString[i, 0] = "=RC[-1]*9.81";
                }
                rAthS.FormulaArray = arrString;
                mBwApp.ReportProgress(1);

                // VTH (m/s)
                ws.Cells[2, 11] = "veloc (m/s)";
                Range rVthS = ws.get_Range(string.Format("K{0}", startRow + 1), string.Format("K{0}", startRow + accSurface.Count - 1));
                for (int i = 0; i < accSurface.Count; i++)
                {
                    arrString[i, 0] = "=0.5*(RC[-1]+R[-1]C[-1])*(RC[-3]-R[-1]C[-3])+R[-1]C";
                }
                rVthS.FormulaArray = arrString;
                rVthS.Style = "Calculation";
                mBwApp.ReportProgress(1);

                // DTH (m)
                ws.Cells[2, 12] = "disp (m)";
                Range rDthS = ws.get_Range(string.Format("L{0}", startRow + 1), string.Format("L{0}", startRow + accSurface.Count - 1));
                for (int i = 0; i < accSurface.Count; i++)
                {
                    arrString[i, 0] = "=0.5*(RC[-1]+R[-1]C[-1])*(RC[-4]-R[-1]C[-4])";
                }
                rDthS.FormulaArray = arrString;
                rDthS.Style = "Output";
                mBwApp.ReportProgress(1);

                // DTH (m) above yield acceleration
                ws.Cells[2, 12] = "disp (m)";

                ws.Cells[1, 15] = "Yield Accel (g)";
                ws.Cells[2, 15] = "Disp  (m)";
                ws.Cells[3, 15] = "Disp (mm)";
                Range ay = (Range)ws.Cells[1, 16];
                ay.Name = "ay";
                ay.Value2 = this.Options.YieldAccel;
                ay.Style = "Input";
                Range rDthAy = ws.get_Range(string.Format("M{0}", startRow + 1), string.Format("M{0}", startRow + accSurface.Count - 1));
                for (int i = 0; i < accSurface.Count; i++)
                {
                    arrString[i, 0] = "=IF(RC[-4]>ay,RC[-1],0)";
                }
                rDthAy.FormulaArray = arrString;
                rDthAy.Style = "Output";

                // Total Positive Displacement above Yield Acceleration
                ws.Cells[2, 13] = "disp (above a_y)";
                Range disp = (Range)ws.Cells[2, 16];
                disp.FormulaR1C1 = string.Format("=SUM(R[{0}]C[-3]:R[{1}]C[-3])", startRow - 1, accSurface.Count + startRow - 1);
                disp.Style = "Calculation";
                // also show in mm
                Range disp_mm = (Range)ws.Cells[3, 16];
                disp_mm.FormulaR1C1 = "=R[-1]C*1000";
                disp_mm.Style = "Output";
                mBwApp.ReportProgress(1);

            }

            SetNumberFormat(ws, "B1", "E1", "0.0000E+00");
            SetNumberFormat(ws, "I1", "L1", "0.0000E+00");

            mBwApp.ReportProgress(2, "Ready");

        }

        private void SetNumberFormat(Worksheet ws, string colStart, string colFinish, string numberFormat)
        {
            Range rng = ws.get_Range(colStart, colFinish);
            rng.NumberFormat = numberFormat;
        }

        private void GenerateChartRP(Worksheet ws)
        {
            ChartObjects chartObjs = (ChartObjects)ws.ChartObjects(Type.Missing);
            ChartObject chartObj = chartObjs.Add(300, 20, 640, 480);
            Chart xlChart = chartObj.Chart;

            SeriesCollection seriesCollection = (SeriesCollection)xlChart.SeriesCollection(Type.Missing);

            seriesCollection.NewSeries();
            seriesCollection.Item(1).Name = "Shake91";
            seriesCollection.Item(1).XValues = string.Format("={0}!$A{1}:$A{2}", ws.Name, startRow, this.Options.MyRPSiteMaker.PeriodList.Count);
            seriesCollection.Item(1).Values = string.Format("={0}!$B{1}:$B{2}", ws.Name, startRow, this.Options.MyRPSiteMaker.ATH.Count);

            xlChart.ChartType = XlChartType.xlXYScatterSmoothNoMarkers;

            // Customize axes:
            Axis xAxis = (Axis)xlChart.Axes(XlAxisType.xlCategory,
                XlAxisGroup.xlPrimary);
            xAxis.HasTitle = true;
            xAxis.AxisTitle.Text = "Period (s)";

            Axis yAxis = (Axis)xlChart.Axes(XlAxisType.xlValue,
                XlAxisGroup.xlPrimary);
            yAxis.HasTitle = true;
            yAxis.AxisTitle.Text = "Acceleration (g)";

            // Add title:
            xlChart.HasTitle = true;
            xlChart.ChartTitle.Text = "Response Spectra (Shake91)";
            // Remove legend:
            xlChart.HasLegend = true;

            xlChart.Location(XlChartLocation.xlLocationAsNewSheet, "RP (Shake91)");

        }

        private void GenerateChartATH(Worksheet ws)
        {
            try
            {
                // Now create the chart.

                // Chart xlChart = (Chart)mWorkBook.Charts.Add(Missing.Value, Missing.Value,Missing.Value, Missing.Value);
                ChartObjects chartObjs = (ChartObjects)ws.ChartObjects(Type.Missing);
                ChartObject chartObj = chartObjs.Add(300, 20, 960, 540);
                Chart xlChart = chartObj.Chart;


                SeriesCollection seriesCollection = (SeriesCollection)xlChart.SeriesCollection(Type.Missing);

                seriesCollection.NewSeries();
                seriesCollection.Item(1).Name = "Base";
                seriesCollection.Item(1).XValues = string.Format("={0}!$A{1}:$A{2}", ws.Name, startRow, MyBaseATHMaker.ATH.Count);
                seriesCollection.Item(1).Values = string.Format("={0}!$B{1}:$B{2}", ws.Name, startRow, MyBaseATHMaker.ATH.Count);

                seriesCollection.NewSeries();
                seriesCollection.Item(2).Name = "Surface";
                seriesCollection.Item(2).XValues = string.Format("={0}!R{1}C8:R{2}C8", ws.Name, startRow, MySurfaceATHMaker.ATH.Count);
                seriesCollection.Item(2).Values = string.Format("={0}!R{1}C9:R{2}C9", ws.Name, startRow, MySurfaceATHMaker.ATH.Count);

                xlChart.ChartType = XlChartType.xlXYScatterSmoothNoMarkers;

                // Customize axes:
                Axis xAxis = (Axis)xlChart.Axes(XlAxisType.xlCategory,
                    XlAxisGroup.xlPrimary);
                xAxis.HasTitle = true;
                xAxis.AxisTitle.Text = "Time (s)";

                Axis yAxis = (Axis)xlChart.Axes(XlAxisType.xlValue,
                    XlAxisGroup.xlPrimary);
                yAxis.HasTitle = true;
                yAxis.AxisTitle.Text = "Acceleration (g)";

                // Add title:
                xlChart.HasTitle = true;
                xlChart.ChartTitle.Text = "Acceleration Time History";
                // Remove legend:
                xlChart.HasLegend = true;


                xlChart.Location(XlChartLocation.xlLocationAsNewSheet, "ATH (Base vs. Surface)");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                mExcelApp.Quit();
            }

        }

        public string SaveAs2003(string filePath)
        {
            mWorkBook.SaveAs(filePath,
                             Microsoft.Office.Interop.Excel.XlFileFormat.xlWorkbookNormal,
                             Missing.Value,
                             Missing.Value,
                             Missing.Value,
                             Missing.Value,
                             Microsoft.Office.Interop.Excel.XlSaveAsAccessMode.xlNoChange,
                             Missing.Value,
                             Missing.Value,
                             Missing.Value,
                             Missing.Value,
                             Missing.Value);

            return filePath;
        }

        public string SaveAs2003(string dir, string fileName)
        {
            return SaveAs2003(Path.Combine(dir, fileName + ".xls"));
        }

        public string SaveAs2007(string filePath)
        {
            mWorkBook.SaveAs(filePath,
                             Microsoft.Office.Interop.Excel.XlFileFormat.xlOpenXMLWorkbook,
                             Missing.Value,
                             Missing.Value,
                             Missing.Value,
                             Missing.Value,
                             Microsoft.Office.Interop.Excel.XlSaveAsAccessMode.xlNoChange,
                             Missing.Value,
                             Missing.Value,
                             Missing.Value,
                             Missing.Value,
                             Missing.Value);

            return filePath;
        }

        public string SaveAs2007(string dir, string fileName)
        {
            return (Path.Combine(dir, fileName + ".xlsx"));
        }

        private void AutofitColumns(Worksheet ws, string colStart, string colFinish)
        {
            Microsoft.Office.Interop.Excel.Range oRng;
            oRng = ws.get_Range(colStart, colFinish);
            oRng.EntireColumn.AutoFit();
        }
    }
}
