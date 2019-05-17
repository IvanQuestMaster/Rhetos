using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RhetosCLI
{
    public class MainArgs
    {
        public List<string> Packages {get; set;}
        public string OutputFolder { get; set; }
        public string PluginsFolder { get; set; }
        public string ProjectFolder { get; set; }
        public bool ShowHelp { get; set; }
        public string DatabaseLanguage { get; set; }

        public MainArgs()
        {
            Packages = new List<string>();
            DatabaseLanguage = "MsSql";
        }
    }
}
