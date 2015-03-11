using System;
using System.Runtime.InteropServices;
using Microsoft.Build.Evaluation;
﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Common;
using System.Xml;
using System.Text;
using CLAP;
using CLAP.Validation;


namespace PA.InnoSetupProcessor
{
    public static class Program
    {
        public class AppManager
        {
            public SolutionProcessor Solution { get; private set; }

            [Global(Aliases = "sln", Description = "Solution file")]
            void SolutionFile([FileExists] string filename)
            {
                this.Solution = new SolutionProcessor(filename);
            }


            public InnoSetupScript Script { get; private set; }

            [Global(Aliases = "iss", Description = "Script file")]
            void ScriptFile([FileExists] string filename)
            {
                this.Script = new InnoSetupScript(filename);
            }

            [Verb(IsDefault = true, Description = "Update File section")]
            void Update([Aliases("cfg"), DefaultValue("Release")] string configuration, [Aliases("pf"), DefaultValue("AnyCPU")] string platform)
            {
                if (this.Solution == null)
                {
                    try
                    {
                        this.Solution = new SolutionProcessor(Directory.EnumerateFiles(Environment.CurrentDirectory, "*.sln").First());
                    }
                    catch
                    {
                        Console.Error.WriteLine("No Solution specified");
                        return;
                    }
                }

                if (this.Script == null)
                {
                    try
                    {
                        this.Script = new InnoSetupScript(Environment.CurrentDirectory + Path.DirectorySeparatorChar + this.Solution.Solution.SolutionName + ".iss");
                    }
                    catch
                    {
                        Console.Error.WriteLine("No Script specified");
                        return;
                    }
                }

                this.Script.UpdateDefine();
                this.Script.UpdateSetup();
                this.Script.UpdateFileSection(this.Solution.GetFiles(configuration, platform));
            }

            [Verb(Description = "Create missing setup.config")]
            void Init()
            {
                if (this.Solution != null)
                {
                    this.Solution.Init();
                }
            }

            [Help]
            void ShowHelp(string help)
            {
                Console.Error.WriteLine(help);
            }
        }


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            CLAP.Parser.Run<AppManager>(args, new AppManager());
        }

    }
}