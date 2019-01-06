using System;
using System.Collections.Generic;
using System.Text;

namespace Rhetos
{
    public class MockInstalledPackages : IInstalledPackages
    {
        public IEnumerable<IInstalledPackage> Packages {
            get { return new List<IInstalledPackage>(); }
        }
    }
}
