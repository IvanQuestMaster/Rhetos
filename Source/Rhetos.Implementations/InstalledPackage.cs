using System;
using System.Collections.Generic;
using System.Text;

namespace Rhetos
{
    public class InstalledPackage : IInstalledPackage
    {
        public string Id { get; set; }

        public string Version { get; set; }

        public string Folder { get; set; }
    }
}
