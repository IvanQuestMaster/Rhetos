using Rhetos.Deployment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RhetosCLI
{
    public class InstalledPackages : IInstalledPackages
    {
        List<InstalledPackage> _instlledPackages;

        public IEnumerable<InstalledPackage> Packages { get; private set; }

        public InstalledPackages(List<InstalledPackage> instlledPackages)
        {
            _instlledPackages = instlledPackages;
        }
    }
}
