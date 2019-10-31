using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos
{
    public class RhetosArguments
    {
        public bool RunGenerators { get; set; }

        public bool Deploy { get; set; }

        public bool ShowHelp { get; set; }

        public string[] RunGenerators__GeneratorSources { get; set; }

        public string[] RunGenerators__ShowHelp { get; set; }

        public string[] RunGenerators__Assemblies { get; set; }

        public string RunGenerators__GeneratedFolderPath { get; set; }

        public string[] Deploy__ShowHelp { get; set; }

        public string[] Deploy__Assemblies { get; set; }

        public string Deploy__GeneratedFolderPath { get; set; }
    }

    public class RunGeneratorsOptions
    {
    }
}
