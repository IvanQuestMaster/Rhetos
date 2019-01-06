using System;
using System.Collections.Generic;
using System.Text;

namespace Rhetos
{
    public class InstalledPackages : IInstalledPackages
    {
        List<InstalledPackage> _packages;

        public IEnumerable<IInstalledPackage> Packages {
            get {
                return _packages;
            }
        }

        public InstalledPackages(List<InstalledPackage> packages)
        {
            _packages = packages;
        }
    }
}
