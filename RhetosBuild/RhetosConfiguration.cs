using System;
using System.Collections.Generic;
using System.Text;
using Rhetos.Utilities;

namespace RhetosBuild
{
    public class RhetosConfiguration : IRhetosConfiguration
    {
        public string GeneratedFolder { get; set; }

        public string GeneratedFilesCacheFolder { get; set; }

        public string PluginsFolder { get; set; }
    }
}
