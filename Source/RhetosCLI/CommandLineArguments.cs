using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos
{
    public class RhetosArguments
    {
        public bool Build { get; set; }

        public string[] Build__GeneratorSources { get; set; }

        public string[] Build__ShowHelp { get; set; }

        public string[] Build__Assemblies { get; set; }

        public string Build__GeneratedFolderPath { get; set; }

        public bool Deploy { get; set; }

        public string[] Deploy__ShowHelp { get; set; }

        public string[] Deploy__Assemblies { get; set; }

        public string Deploy__GeneratedFolderPath { get; set; }

        public bool Deploy__ShortTransactions { get; set; }

        public bool ShowHelp { get; set; }
    }

    public class RunGeneratorsOptions
    {
    }
}
