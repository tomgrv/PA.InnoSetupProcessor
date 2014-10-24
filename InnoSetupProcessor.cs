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
    static class Program
    {

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Usage: BuildSetup.exe [solution file]");
                return;
            }

            var solution = new Solution(args[0]);
            var solutionpath = Path.GetDirectoryName(args[0]);

            var pc = new ProjectCollection();

            foreach (ProjectInSolution prj in solution.Projects.Where(p => !p.IsSolutionFolder))
            {


                var project = pc.LoadProject(Path.Combine(solutionpath, prj.RelativePath));

                ProjectItem pi = project.Items.FirstOrDefault(i => i.ItemType == "None" && i.EvaluatedInclude == "setup.config");

                if (pi is ProjectItem)
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(project.DirectoryPath + @"\" + pi.EvaluatedInclude);

                    List<InnoSetupFileItem> files = new List<InnoSetupFileItem>();

                    XmlNodeList setup = doc.DocumentElement.SelectNodes("/setup/*");

                    foreach (XmlNode node in setup)
                    {
                        switch (node.Name)
                        {
                            case "target":

                                files.AddRange(GetFiles(project, "AppConfigFileDestination", node.Attributes));
                                break;

                            case "dependencies":

                                files.AddRange(GetFiles(project, "Reference", node.Attributes));
                                break;

                            default:

                                break;

                        }
                    }

                    files.ForEach(a => Console.Error.WriteLine(a.ToString()));

                    
                }


            }

            Console.Read();
        }

        static IEnumerable<InnoSetupFileItem> GetFiles(Project p, string itemType, XmlAttributeCollection att)
        {
            foreach (ProjectItem ppi in p.Items.Where(i => i.ItemType == itemType))
            {
                if (ppi.DirectMetadataCount > 0)
                {
                    var file = new InnoSetupFileItem(att);

                    var pm = ppi.Metadata.FirstOrDefault(m => m.Name == "HintPath");

                    if (pm is ProjectMetadata)
                    {
                        file.Source = Path.Combine(Path.GetDirectoryName(p.FullPath), pm.EvaluatedValue);
                    }
                    else
                    {
                        file.Source = ppi.EvaluatedInclude;
                    }

                    yield return file;
                }
            }
        }

    }
}