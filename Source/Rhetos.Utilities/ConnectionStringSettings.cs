using System;
using System.Collections.Generic;
using System.Text;

namespace Rhetos.Utilities
{
    public class ConnectionStringSettings
    {
        public string Name { get; private set; }

        public string ConnectionString { get; private set; }

        public string ProviderName { get; private set; }

        public ConnectionStringSettings(string name, string connectionString, string providerName)
        {
            Name = name;
            ConnectionString = connectionString;
            ProviderName = providerName;
        }
    }
}
