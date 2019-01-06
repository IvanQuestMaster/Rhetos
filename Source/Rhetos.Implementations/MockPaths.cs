using System;
using System.Collections.Generic;
using System.Text;

namespace Rhetos
{
    public class MockPaths : IPaths
    {
        public string GeneratedFolder { get; set; }

        public string GeneratedFilesCacheFolder { get; set; }
    }
}
