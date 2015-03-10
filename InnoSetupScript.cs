using System;
using System.Collections.Generic;
using IniParser;
using IniParser.Model;
using IniParser.Parser;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Linq;

namespace PA.InnoSetupProcessor
{
    public class InnoSetupScript
    {
        public string ScriptName { get; set; }

        public bool WithDefine { get; private set; }

        private readonly InnoSetupScriptParser parser = new InnoSetupScriptParser();

        public InnoSetupScript(string filename)
        {
            Console.Out.WriteLine("Using <" + filename + ">");
            this.ScriptName = filename;
        }

        internal void UpdateDefine()
        {
            var iss = this.parser.ReadFile(this.ScriptName);

            var appname = GetAttribute<AssemblyTitleAttribute>(t => t.Title);
            var version = GetAttribute<AssemblyInformationalVersionAttribute>(t => t.InformationalVersion);

            this.UpdateDefine(iss.Global, "AppName", appname);
            this.UpdateDefine(iss.Global, "AppNamePascalCase",  Regex.Replace(ToPascalCase(appname), @"[^0-9a-zA-Z\._]", string.Empty));
            this.UpdateDefine(iss.Global, "AppGeneration", GetAttribute<AssemblyFileVersionAttribute>(t => t.Version).Split('.').First());
            this.UpdateDefine(iss.Global, "AppVersion", GetAttribute<AssemblyFileVersionAttribute>(t => t.Version));
            this.UpdateDefine(iss.Global, "AppDescription", GetAttribute<AssemblyDescriptionAttribute>(t => t.Description));
            this.UpdateDefine(iss.Global, "AppCompany", GetAttribute<AssemblyCompanyAttribute>(t => t.Company));
            this.UpdateDefine(iss.Global, "AppCopyright", GetAttribute<AssemblyCopyrightAttribute>(t => t.Copyright));
            this.UpdateDefine(iss.Global, "AppInfoVersion", version);
            this.UpdateDefine(iss.Global, "AppInfoSemVer", version.Substring(0, version.IndexOf('+') - 1));
            this.UpdateDefine(iss.Global, "AppProduct", GetAttribute<AssemblyProductAttribute>(t => t.Product));

            this.parser.WriteFile(this.ScriptName, iss);

            this.WithDefine = true;
        }

        private void UpdateDefine(KeyDataCollection keys, string key, string value)
        {
            KeyData data = keys.FirstOrDefault(kd => Regex.Match(kd.KeyName, @"#(\w+\s){2}").Groups[1].Value.Trim() == key.Trim());

            if (data == null)
            {
                keys.AddKey("#define " + key + " \"" + value + "\"");
            }
            else
            {
                data.KeyName = Regex.Match(data.KeyName, @"#(\w+\s){2}").Value + "\"" + value + "\"";
            }
        }

        internal void UpdateSetup()
        {
            var iss = this.parser.ReadFile(this.ScriptName);

            if (!iss.Sections.ContainsSection("Setup"))
            {
                iss.Sections.AddSection("Setup");
            }

            var section = iss.Sections["Setup"];

            if (this.WithDefine)
            {
                section["AppName"] = "{#AppName}";
                section["AppVersion"] = "{#AppVersion}";
                section["AppComments"] = "{#AppDescription}";
                section["AppPublisher"] = "{#AppCompany}";
                section["AppCopyright"] = "{#AppCopyright}";
                section["VersionInfoVersion"] = "{#AppVersion}";
                section["VersionInfoCompany"] = "{#AppCompany}";
                section["VersionInfoProductName"] = "{#AppProduct}";
                section["VersionInfoDescription"] = "{#AppDescription}";
                section["VersionInfoTextVersion"] = "{#AppInfoVersion}";
                section["OutputBaseFilename"] = "Setup_{#AppNamePascalCase}.{#AppInfoSemVer}.exe";
            }
            else
            {
                section["AppName"] = GetAttribute<AssemblyTitleAttribute>(t => t.Title);
                section["AppVersion"] = GetAttribute<AssemblyFileVersionAttribute>(t => t.Version);
                section["AppComments"] = GetAttribute<AssemblyDescriptionAttribute>(t => t.Description);
                section["AppPublisher"] = GetAttribute<AssemblyCompanyAttribute>(t => t.Company);
                section["AppCopyright"] = GetAttribute<AssemblyCopyrightAttribute>(t => t.Copyright);
                section["VersionInfoVersion"] = GetAttribute<AssemblyFileVersionAttribute>(t => t.Version);
                section["VersionInfoCompany"] = GetAttribute<AssemblyCompanyAttribute>(t => t.Company);
                section["VersionInfoProductName"] = GetAttribute<AssemblyTitleAttribute>(t => t.Title);
                section["VersionInfoDescription"] = GetAttribute<AssemblyDescriptionAttribute>(t => t.Description);
                section["VersionInfoTextVersion"] = GetAttribute<AssemblyInformationalVersionAttribute>(t => t.InformationalVersion);
            }

            this.parser.WriteFile(this.ScriptName, iss);
        }

        private string GetAttribute<T>(Func<T, string> getField, string defValue = "")
            where T : Attribute
        {
            var attribute = Assembly.GetEntryAssembly().GetCustomAttribute<T>();
            return attribute != null ? getField(attribute) : defValue;
        }

        internal void UpdateFileSection(IEnumerable<InnoSetupFileItem> list)
        {
            var iss = this.parser.ReadFile(this.ScriptName);

            if (!iss.Sections.ContainsSection("Files"))
            {
                iss.Sections.AddSection("Files");
            }

            var sec = iss.Sections["Files"];

            sec.RemoveAllKeys();

            foreach (var i in InnoSetupFileItem.OptimizeFileItems(list))
            {
                sec.AddKey(i.ToString(), "");
            }

            this.parser.WriteFile(this.ScriptName, iss);
        }


        // Convert the string to Pascal case.
        internal static string ToPascalCase(string the_string)
        {
            // If there are 0 or 1 characters, just return the string.
            if (the_string == null) return the_string;
            if (the_string.Length < 2) return the_string.ToUpper();

            // Split the string into words.
            string[] words = the_string.Split(
                new char[] { },
                StringSplitOptions.RemoveEmptyEntries);

            // Combine the words.
            string result = "";
            foreach (string word in words)
            {
                result +=
                    word.Substring(0, 1).ToUpper() +
                    word.Substring(1);
            }

            return result;
        }

    }
}

