using Microsoft.Build.Evaluation;
using NuGet.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PA.InnoSetupProcessor
{
    public class SolutionProcessor
    {

        public FileInfo SolutionFile { get; private set; }

        public SolutionProcessor(string solution)
        {
            this.SolutionFile = new FileInfo(solution);
        }

        public void CreateIssFile()
        {
            string name = this.SolutionFile.Name.Replace(this.SolutionFile.Extension, ".iss");
            var solution = new Solution(this.SolutionFile.FullName);

            Console.Out.WriteLine("Creating " + name + " file for " + this.SolutionFile.Name + "...");

            File.WriteAllLines(name, this.GetSolutionFilesOptimized(solution).Select(f => f.ToString()));

            Console.Out.WriteLine("Done");
        }

        public void InitProjects()
        {
            var solution = new Solution(this.SolutionFile.FullName);

            using (var pc = new ProjectCollection())
            {
                foreach (ProjectInSolution prj in solution.Projects.Where(p => !p.IsSolutionFolder))
                {
                    var project = pc.LoadProject(Path.Combine(solution.DirectoryName, prj.RelativePath));
                    var current = Path.GetDirectoryName(project.ProjectFileLocation.LocationString);

                    ProjectItem pi = project.Items.FirstOrDefault(i => i.ItemType == "None" && i.EvaluatedInclude == "setup.config");

                    if (pi == null)
                    {
                        using (StreamWriter sw = File.CreateText(current + Path.DirectorySeparatorChar + "setup.config"))
                        {
                            sw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
                            sw.WriteLine("<setup Component=\"{app}\\.\" >");
                            sw.WriteLine("<target DestDir=\"{app}\\.\"  />");
                            sw.WriteLine("<dependencies DestDir=\"{app}\\.\"   />");
                            sw.WriteLine("<files DestDir=\"{app}\\.\"   />");
                            sw.WriteLine("</setup>");
                        }

                        project.AddItem("None", "setup.config");
                        project.Save();
                    }
                }
            }
        }

        private string MergeConditions(string condA, string condB)
        {
            if (condA is String && condA != string.Empty && condB is String && condB != string.Empty && condA != condB)
            {
                condA += " or (" + condB + ")";
            }

            return condA ?? string.Empty;
        }

        private IEnumerable<InnoSetupFileItem> GetSolutionFilesOptimized(Solution solution)
        {
            InnoSetupFileItem[] files = this.GetSolutionFiles(solution).ToArray();

            for (int i = 0; i < files.Length; i++)
            {
                if (files[i] is InnoSetupFileItem)
                {
                    for (int j = i + 1; j < files.Length; j++)
                    {
                        if (files[j] is InnoSetupFileItem)
                        {
                            if (Path.GetFullPath(files[i].Source) == Path.GetFullPath(files[j].Source) && Path.GetFullPath(files[i].DestDir) == Path.GetFullPath(files[j].DestDir))
                            {
                                files[i].Tasks = MergeConditions(files[i].Tasks, files[j].Tasks);
                                files[i].Components = MergeConditions(files[i].Components, files[j].Components);
                                files[j] = null;
                            }
                        }
                    }
                }
            }

            return files.Where(s => s is InnoSetupFileItem).OrderBy(s => s.Source);
        }

        private IEnumerable<InnoSetupFileItem> GetSolutionFiles(Solution solution)
        {
            using (var pc = new ProjectCollection())
            {
                foreach (ProjectInSolution prj in solution.Projects.Where(p => !p.IsSolutionFolder))
                {
                    var project = pc.LoadProject(Path.Combine(solution.DirectoryName, prj.RelativePath));
                    var current = Path.GetDirectoryName(project.ProjectFileLocation.LocationString);

                    ProjectItem pi = project.Items.FirstOrDefault(i => i.ItemType == "None" && i.EvaluatedInclude == "setup.config");

                    if (pi is ProjectItem)
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.Load(project.DirectoryPath + @"\" + pi.EvaluatedInclude);

                        XmlNode setup = doc.DocumentElement.SelectSingleNode("/setup");

                        if (setup.Attributes["Components"] == null)
                        {
                            setup.Attributes.Append(doc.CreateAttribute("Components"));
                            setup.Attributes["Components"].Value = Path.GetFileNameWithoutExtension(project.ProjectFileLocation.LocationString);
                        }

                        foreach (XmlNode node in setup.ChildNodes)
                        {
                            switch (node.Name)
                            {
                                case "target":

                                    yield return new InnoSetupFileItem(current + Path.DirectorySeparatorChar + project.Items.Single(i => i.ItemType == "IntermediateAssembly").EvaluatedInclude,
                                        GetAttribute(node, "DestDir"),
                                        GetAttribute(node.ParentNode, "Components"),
                                        GetAttribute(node, "Tasks"));

                                    foreach (var files in GetProjectFiles(project, node, "AppConfigFileDestination"))
                                    {
                                        yield return files;
                                    }

                                    break;

                                case "dependencies":

                                    foreach (var files in GetProjectFiles(project, node, "Reference"))
                                    {
                                        yield return files;
                                    }

                                    foreach (var rPath in GetProjectRefs(project, node))
                                    {
                                        Project r = pc.LoadProject(Path.Combine(solution.DirectoryName, rPath));

                                       yield return new InnoSetupFileItem(Path.GetDirectoryName(rPath) + Path.DirectorySeparatorChar + r.Items.Single(i => i.ItemType == "IntermediateAssembly").EvaluatedInclude,
                                       GetAttribute(node, "DestDir"),
                                       GetAttribute(node.ParentNode, "Components"),
                                       GetAttribute(node, "Tasks"));
                                    }

                                    break;

                                case "files":

                                    foreach (var files in GetProjectFiles(project, node, "None", "CopyToOuputDirectory"))
                                    {
                                        yield return files;
                                    }
                                    break;

                                default:

                                    break;

                            }
                        }


                    }
                }
            }
        }

        private IEnumerable<string> GetProjectRefs(Project p, XmlNode node)
        {
            foreach (ProjectItem ppi in p.Items.Where(i => i.ItemType == "ProjectReference"
                && i.DirectMetadataCount > 0
                && !i.DirectMetadata.Any(m => m.Name == "Private")))
            {
                yield return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(p.ProjectFileLocation.LocationString), ppi.EvaluatedInclude));
            }
        }

        private IEnumerable<InnoSetupFileItem> GetProjectFiles(Project p, XmlNode node, string itemType, string option = "")
        {
            foreach (ProjectItem ppi in p.Items.Where(i => i.ItemType == itemType
                && i.DirectMetadataCount > 0
                && !i.DirectMetadata.Any(m => m.Name == "Private")))
            {
                if (option == string.Empty || ppi.Metadata.FirstOrDefault(m => m.Name == option) is ProjectMetadata)
                {
                    var pm = ppi.Metadata.FirstOrDefault(m => m.Name == "HintPath");
                    yield return new InnoSetupFileItem(Path.GetFullPath(pm is ProjectMetadata ? Path.Combine(Path.GetDirectoryName(p.FullPath), pm.EvaluatedValue) : ppi.EvaluatedInclude),
                         GetAttribute(node, "DestDir"),
                         GetAttribute(node.ParentNode, "Components"),
                         GetAttribute(node, "Tasks"));


                }
            }
        }

        private string GetAttribute(XmlNode n, string name, string defValue = "")
        {
            if (n.Attributes[name] is XmlAttribute)
            {
                return n.Attributes[name].Value;
            }
            else if (n.Attributes[name.ToLower()] is XmlAttribute)
            {
                return n.Attributes[name.ToLower()].Value;
            }
            else
            {
                return defValue;
            }
        }
    }
}
