using Microsoft.Build.Evaluation;
using NuGet.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Reflection;

namespace PA.InnoSetupProcessor
{
    public class SolutionProcessor
    {

        internal Solution Solution { get; private set; }

        public SolutionProcessor(string solution)
        {
            var configuration = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyConfigurationAttribute>();
            this.Solution = new Solution(solution);
        }

        public void Init()
        {
            var solution = new Solution(this.Solution.DirectoryName + Path.DirectorySeparatorChar + this.Solution.SolutionName);

            using (var pc = new ProjectCollection())
            {
                foreach (ProjectInSolution prj in solution.Projects.Where(p => !p.IsSolutionFolder))
                {
                    var project = new ProjectProcessor(pc.LoadProject(Path.Combine(solution.DirectoryName, prj.RelativePath)));
                    project.Init();
                }
            }
        }


        internal IEnumerable<InnoSetupFileItem> GetFiles(string configuration, string platform)
        {
            using (var pc = new ProjectCollection())
            {
                foreach (ProjectInSolution prj in this.Solution.Projects.Where(p => !p.IsSolutionFolder))
                {
                    var pp = new ProjectProcessor(pc.LoadProject(Path.Combine(this.Solution.DirectoryName, prj.RelativePath)), configuration, platform);

                    foreach (InnoSetupFileItem file in pp.GetFiles(configuration, platform))
                    {
                        yield return file;
                    }
                }
            }
        }





    }
}
