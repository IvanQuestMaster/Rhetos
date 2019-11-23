using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos
{
    public class RhetosOptions
    {
        public bool ShowBuildHelp { get; set; }

        public string[] Assemblies { get; set; }

        public string[] Sources { get; set; }

        public string GeneratedFolder { get; set; }

        public string GeneratedCacheFolder { get; set; }

        public bool ShowHelp { get; set; }

        public bool ShowDeployHelp { get; set; }
    }
}
