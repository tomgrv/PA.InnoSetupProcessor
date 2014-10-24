using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PA.InnoSetupProcessor
{
    internal class InnoSetupFileItem
    {
        public string Source { get; set; }
        public string Tasks { get; set; }
        public string Components { get; set; }
        public string DestDir { get; set; }

        private void PreparePath(ref string path, string variable, string name)
        {
            if (path.StartsWith(variable ))
            {
                path = "{" + name + "}" + path.Remove(0, variable.Length);
            }
        }

        private string GetPath(string title)
        {
            string path = GetProperty(title);

            this.PreparePath(ref path, Environment.GetFolderPath(Environment.SpecialFolder.Windows), "win");
            this.PreparePath(ref path, Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), "sys");
            this.PreparePath(ref path, Environment.GetFolderPath(Environment.SpecialFolder.System), "syswow64");


            this.PreparePath(ref path, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "pf");
            this.PreparePath(ref path, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "pf32");
            this.PreparePath(ref path, Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles), "cf");
            this.PreparePath(ref path, Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86), "cf32");
            
            this.PreparePath(ref path, Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "fonts");
            
            
            this.PreparePath(ref path, Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "group");
            this.PreparePath(ref path, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "localappdata");
            this.PreparePath(ref path, Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "commonappdata");
            this.PreparePath(ref path, Environment.GetFolderPath(Environment.SpecialFolder.SendTo), "sendto");

            this.PreparePath(ref path, Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "commondesktop");
            this.PreparePath(ref path, Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "userdesktop");

            this.PreparePath(ref path, Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "commondocs");
            this.PreparePath(ref path, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "userdocs");

            this.PreparePath(ref path, Environment.GetFolderPath(Environment.SpecialFolder.Favorites), "commonfavorites");

            this.PreparePath(ref path, Environment.GetFolderPath(Environment.SpecialFolder.Programs), "userprograms");
            this.PreparePath(ref path, Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms), "commonprograms");


            this.PreparePath(ref path, Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "userstartmenu");
            this.PreparePath(ref path, Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "commonstartmenu");


            this.PreparePath(ref path, Environment.GetFolderPath(Environment.SpecialFolder.Startup), "userstartup");
            this.PreparePath(ref path, Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), "commonstartup");

            this.PreparePath(ref path, Environment.GetFolderPath(Environment.SpecialFolder.Templates), "usertemplates");
            this.PreparePath(ref path, Environment.GetFolderPath(Environment.SpecialFolder.CommonTemplates), "commontemplates");

            this.PreparePath(ref path, Environment.GetFolderPath(Environment.SpecialFolder.Windows).Substring(0, 2), "sd");

            return title + ": \"" + path + "\"";
        }

        private string GetString(string title)
        {
            return title + ": \"" + GetProperty(title) + "\"";
        }

        private string GetFlag(string title)
        {
            return title + ": " + GetProperty(title);
        }

        private string GetProperty(string title)
        {
            var p = this.GetType().GetProperty(title, typeof(String));

            if (p is PropertyInfo)
            {
                return (p.GetValue(this) ?? string.Empty).ToString();
            }

            return string.Empty;
        }

        private void SetProperty(XmlAttributeCollection c, string title)
        {
            if (c[title] is XmlAttribute)
            {
                var p = this.GetType().GetProperty(title);

                if (p is PropertyInfo)
                {
                    p.SetValue(this, c[title].Value);
                }
            }
            else if (c[title.ToLower()] is XmlAttribute)
            {
                var p = this.GetType().GetProperty(title);

                if (p is PropertyInfo)
                {
                    p.SetValue(this, c[title.ToLower()].Value);
                }
            }
        }

        public InnoSetupFileItem(XmlAttributeCollection c)
        {
            this.SetProperty(c, "Source");
            this.SetProperty(c, "DestDir");
            this.SetProperty(c, "Components");
            this.SetProperty(c, "Tasks");
        }

        public override string ToString()
        {
            return this.GetString("Source") + ";" +
                this.GetPath("DestDir") + ";" +
                this.GetString("Components") + ";" +
                this.GetString("Tasks");
        }
    }
}
