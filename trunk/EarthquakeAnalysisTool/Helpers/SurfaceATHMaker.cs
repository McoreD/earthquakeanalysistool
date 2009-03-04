﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;


namespace AccelerationTimeHistoryGen
{
    /// <summary>
    /// ATH generated by Shake91 is processed using this class to generate a XYScatter chart in Excel
    /// </summary>
    class SurfaceATHMaker
    {
        private string mPath;
        /// <summary>
        /// Acceleration Time Histories
        /// </summary>
        public List<string> ATH { get; private set; }
        /// <summary>
        /// Time Interval in Milliseconds
        /// </summary>
        // public int DT { get; set; }
        /// <summary>
        /// Maximum Values that the ATH should hold
        /// </summary>
        public int MaxValues { get; set; }

        public SurfaceATHMaker(string p)
        {
            ATH = new List<string>();
            // this.DT = 20;
            this.MaxValues = 8 * 128;
            this.mPath = p;
        }

        /// <summary>
        /// Creates a column from an ATH file generated by Shake91
        /// </summary>
        public List<string> ReadATH()
        {
            ATH.Clear();

            using (StreamReader sr = new StreamReader(mPath))
            {
                string line = sr.ReadLine();
                line = sr.ReadLine();
                line = sr.ReadLine();

                while (ATH.Count < this.MaxValues)
                {
                    AddAccelelations(line);
                    line = sr.ReadLine();
                }
            }

            return ATH;
        }

        public void WriteATH()
        {
            string dest = Path.Combine(Path.GetDirectoryName(mPath), Path.GetFileNameWithoutExtension(mPath) + "-for-excel.txt");
            StreamWriter sw = new StreamWriter(dest);
            foreach (string s in ATH)
            {
                sw.WriteLine(s);
            }
            sw.Close();
        }

        private void AddAccelelations(string line)
        {
            List<string> nums = SplitLine(line);
            for (int i = 0; i < nums.Count; i++)
            {
                ATH.Add(nums[i]);
            }
        }

        private List<string> SplitLine(string line)
        {
            char[] chars = line.ToCharArray();
            List<string> acc = new List<string>();

            if (chars.Length > 72)
            {
                for (int i = 0; i < chars.Length; i++)
                {
                    string num = line.Substring(i, 9);
                    acc.Add(num);
                    i = i + 8;
                    if (acc.Count == 8)
                    {
                        return acc;
                    }
                }
            }

            return acc;
        }

        private bool IsValidLine(string line)
        {
            bool valid;
            valid = SplitLine(line).Count == 8;
            return valid;
        }


    }
}