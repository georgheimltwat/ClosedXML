﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClosedXML.Excel;
using ClosedXML;
using System.Drawing;
using System.IO;
using DocumentFormat.OpenXml.Packaging;

namespace ClosedXML_Sandbox
{
    class Program
    {

        static void Main(string[] args)
        {
            //new ClosedXML_Examples.Ranges.UsingTables().Create(@"C:\Excel Files\ForTesting\Sandbox.xlsx");
            var wb = new XLWorkbook(@"C:\Excel Files\ForTesting\Hyperlinks_2.xlsx");
            //var wb = new XLWorkbook();
            //var c = wb.Worksheets.Add("New").Cell(1, 1);
            //c.Value = "Hello";
            //c.ShareString = false;
            wb.SaveAs(@"C:\Excel Files\ForTesting\Sandbox.xlsx");
            
            //String test = @"../file.txt";
            //Console.WriteLine("Uri.IsWellFormedUriString says: {0}", Uri.IsWellFormedUriString(test, UriKind.RelativeOrAbsolute));
            //Console.WriteLine("TryCreate says: {0}", TryCreate(test));
            //Console.WriteLine("IsValidUri says: {0}", IsValidUri(test));
            //Console.ReadKey();
        }
        public static String GetSheetPassword(String password)
        {
            Int32 pLength = password.Length;
            Int32 hash = 0;
            if (pLength == 0) return hash.ToString("X");

            for (Int32 i = pLength - 1; i >= 0; i--)
            {
                hash ^= password[i];
                hash = hash >> 14 & 0x01 | hash << 1 & 0x7fff;
            }
            hash ^= 0x8000 | 'N' << 8 | 'K';
            hash ^= pLength;
            return hash.ToString("X");
        }

        static Boolean IsValidUri(String uri) 
        { 
            try 
            { 
                new Uri(uri, UriKind.Relative); 
                return true; 
            }
            catch 
            {
                return false; 
            }
        }

        static Boolean TryCreate(String address)
        {
            Uri uri;
            return Uri.TryCreate(address, UriKind.Absolute, out uri);
        }

        static void Main_5961(string[] args)
        {
            var fi = new FileInfo(@"C:\Excel Files\ForTesting\Issue_5961.xlsx");
            XLWorkbook wb = new XLWorkbook(fi.FullName);
            {
                IXLWorksheet s = wb.Worksheets.Add("test1");
                s.Cell(1, 1).Value = DateTime.Now.ToString();
            }
            {
                IXLWorksheet s = wb.Worksheets.Add("test2");
                s.Cell(1, 1).Value = DateTime.Now.ToString();
            }
            wb.Save();
            wb = new XLWorkbook(fi.FullName);
            wb.Worksheets.Delete("test1");
            {
                IXLWorksheet s = wb.Worksheets.Add("test3");
                s.Cell(1, 1).Value = DateTime.Now.ToString();
            }
            wb.Save();
            wb = new XLWorkbook(fi.FullName);

            wb.Save();
        }

        static void xMain(string[] args)
        {
            FillStyles();
            List<Double> runningSave = new List<Double>();
            List<Double> runningLoad = new List<Double>();
            List<Double> runningSavedBack = new List<Double>();
            
            foreach (Int32 r in Enumerable.Range(1, 5))
            {
                var wb = new XLWorkbook();
                var startTotal = DateTime.Now;
                foreach (var i in Enumerable.Range(1, 1))
                {
                    var ws = wb.Worksheets.Add("Sheet" + i);
                    foreach (var ro in Enumerable.Range(1, 2000))
                    {
                        foreach (var co in Enumerable.Range(1, 100))
                        {
                            ws.Cell(ro, co).Style = GetRandomStyle();
                            ws.Cell(ro, co).Value = GetRandomValue();
                        }
                        //System.Threading.Thread.Sleep(10);
                    }
                }
                //var start3 = DateTime.Now;
                //foreach (var ws in wb.Worksheets)
                //{
                //    ws.Style = wb.Style;
                //}
                //var end3 = DateTime.Now;
                //Console.WriteLine("Bolded all cells in {0} secs.", (end3 - start3).TotalSeconds);
                
                var start = DateTime.Now;
                wb.SaveAs(@"C:\Excel Files\ForTesting\Benchmark.xlsx");
                var end = DateTime.Now;
                var saved = (end - start).TotalSeconds;
                runningSave.Add(saved);
                Console.WriteLine("Saved in {0} secs.", saved);

                var start1 = DateTime.Now;
                var wb1 = new XLWorkbook(@"C:\Excel Files\ForTesting\Benchmark.xlsx");
                var end1 = DateTime.Now;
                var loaded = (end1 - start1).TotalSeconds;
                runningLoad.Add(loaded);
                Console.WriteLine("Loaded in {0} secs.", loaded);

                var start2 = DateTime.Now;
                //wb1.SaveAs(@"C:\Excel Files\ForTesting\Benchmark_Saved.xlsx");
                var end2 = DateTime.Now;
                var savedBack = (end2 - start2).TotalSeconds;
                runningSavedBack.Add(savedBack);
                Console.WriteLine("Saved back in {0} secs.", savedBack);

                var endTotal = DateTime.Now;
                Console.WriteLine("It all took {0} secs.", (endTotal - startTotal).TotalSeconds);
            }
            Console.WriteLine("-------");
            Console.WriteLine("Avg Save time: {0}", runningSave.Average());
            Console.WriteLine("Avg Load time: {0}", runningLoad.Average());
            Console.WriteLine("Avg Save Back time: {0}", runningSavedBack.Average());
            //Console.ReadKey();
        }

        private static IXLStyle style1;
        private static IXLStyle style2;
        private static IXLStyle style3;
        private static void FillStyles()
        {

            style1 = XLWorkbook.DefaultStyle;
            style1.Font.Bold = true;
            style1.Fill.BackgroundColor = XLColor.Azure;
            style1.Border.BottomBorder = XLBorderStyleValues.Medium;
            style1.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            style2 = XLWorkbook.DefaultStyle;
            style2.Font.Italic = true;
            style2.Fill.BackgroundColor = XLColor.Orange;
            style2.Border.LeftBorder = XLBorderStyleValues.Medium;
            style2.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            style3 = XLWorkbook.DefaultStyle;
            style3.Font.FontColor = XLColor.FromColor(Color.Red);
            style3.Fill.PatternColor = XLColor.Blue;
            style3.Fill.PatternType = XLFillPatternValues.DarkTrellis;
            style3.Border.DiagonalBorder = XLBorderStyleValues.Dotted;
        }
        private static IXLStyle GetRandomStyle()
        {

            var val = rnd.Next(1, 4);
            if (val == 1)
            {
                return style1;
            }
            else if (val == 2)
            {
                return style2;
            }
            else
                return style3;

        }
        private static DateTime baseDate = DateTime.Now;
        private static Random rnd = new Random();
        private static object GetRandomValue()
        {
            var val = rnd.Next(1, 7);
            if (val == 1)
                return Guid.NewGuid().ToString().Substring(1, 5);
            else if (val == 2)
                return true;
            else if (val == 3)
                return false;
            else if (val == 4)
                return DateTime.Now;
            else if (val == 5)
                return rnd.Next(1, 1000);
            else
                return (DateTime.Now - baseDate);
        }



        class Person
        {
            public String Name { get; set; }
            public Int32 Age { get; set; }
        }

        // Save defaults to a .config file
    }
}