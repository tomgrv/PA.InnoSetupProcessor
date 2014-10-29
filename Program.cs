using System;
using System.Runtime.InteropServices;
using Microsoft.Build.Evaluation;
﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Common;
using System.Xml;
using System.IO;
using System.Text;


namespace PA.InnoSetupProcessor
{
    public static class Program
    {

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Usage: BuildSetup.exe [solution file]");
                return;
            }

            SolutionProcessor p = new SolutionProcessor(args[0]);

            if (args.Contains("-i"))
            {
                p.InitProjects();
            }

            p.CreateIssFile();
        }
    }
}