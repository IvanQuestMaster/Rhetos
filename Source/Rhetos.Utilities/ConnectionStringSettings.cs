/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Configuration;

namespace Rhetos.Utilities
{
    public class ConnectionStringSettings : IConnectionStringSettings
    {
        private static string _databaseLanguage;
        private static string _nationalLanguage;
        private string _connectionString;

        private void SetLanguageFromProviderName(string connectionStringProvider)
        {
            var match = new Regex(@"^Rhetos\.(?<DatabaseLanguage>\w+)(.(?<NationalLanguage>\w+))?$").Match(connectionStringProvider);
            if (!match.Success)
                throw new FrameworkException("Invalid 'providerName' format in 'ServerConnectionString' connection string. Expected providerName format is 'Rhetos.<database language>' or 'Rhetos.<database language>.<natural language settings>', for example 'Rhetos.MsSql' or 'Rhetos.Oracle.XGERMAN_CI'.");
            _databaseLanguage = match.Groups["DatabaseLanguage"].Value ?? "";
            _nationalLanguage = match.Groups["NationalLanguage"].Value ?? "";
        }

        public string DatabaseLanguage
        {
            get
            {
                if (_databaseLanguage == null)
                    SetLanguageFromProviderName(GetProviderNameFromConnectionString());

                return _databaseLanguage;
            }
        }

        public string NationalLanguage
        {
            get
            {
                if (_nationalLanguage == null)
                    SetLanguageFromProviderName(GetProviderNameFromConnectionString());

                return _nationalLanguage;
            }
        }

        public string ConnectionString
        {
            get
            {
                if (_connectionString == null)
                {
                    _connectionString = GetConnectionString().ConnectionString;
                    if (string.IsNullOrEmpty(_connectionString))
                        throw new FrameworkException("Empty 'ServerConnectionString' connection string in application configuration.");
                }
                return _connectionString;
            }
        }

        //TODO Ovo bi vjeratno trebalo biti static
        public string SqlConnectionInfo(string connectionString)
        {
            SqlConnectionStringBuilder cs;
            try
            {
                cs = new SqlConnectionStringBuilder(connectionString);
            }
            catch
            {
                // This is not be a blocking error, because other database providers should be supported.
                return "(cannot parse connection string)";
            }

            var elements = new ListOfTuples<string, string>
            {
                { "DataSource", cs.DataSource },
                { "InitialCatalog", cs.InitialCatalog },
            };

            return
                string.Join(", ", elements
                    .Where(e => !string.IsNullOrWhiteSpace(e.Item2))
                    .Select(e => e.Item1 + "=" + e.Item2));
        }

        private const string ServerConnectionStringName = "ServerConnectionString";

        private System.Configuration.ConnectionStringSettings GetConnectionString()
        {
            System.Configuration.ConnectionStringSettings connectionStringConfiguration = System.Configuration.ConfigurationManager.ConnectionStrings[ServerConnectionStringName];

            if (connectionStringConfiguration == null && !Paths.IsRhetosServer)
                connectionStringConfiguration = RhetosWebConfig.Value.ConnectionStrings.ConnectionStrings[ServerConnectionStringName];

            if (connectionStringConfiguration == null)
                throw new FrameworkException("Missing '" + ServerConnectionStringName + "' connection string in the Rhetos server's configuration.");

            return connectionStringConfiguration;
        }

        private static Lazy<System.Configuration.Configuration> RhetosWebConfig = new Lazy<System.Configuration.Configuration>(InitializeWebConfiguration);

        private static System.Configuration.Configuration InitializeWebConfiguration()
        {
            VirtualDirectoryMapping vdm = new VirtualDirectoryMapping(Paths.RhetosServerRootPath, true);
            WebConfigurationFileMap wcfm = new WebConfigurationFileMap();
            wcfm.VirtualDirectories.Add("/", vdm);
            return WebConfigurationManager.OpenMappedWebConfiguration(wcfm, "/");
        }

        private string GetProviderNameFromConnectionString()
        {
            var connectionStringProvider = GetConnectionString().ProviderName;
            if (string.IsNullOrEmpty(connectionStringProvider))
                throw new FrameworkException("Missing 'providerName' attribute in 'ServerConnectionString' connection string. Expected providerName format is 'Rhetos.<database language>' or 'Rhetos.<database language>.<natural language settings>', for example 'Rhetos.MsSql' or 'Rhetos.Oracle.XGERMAN_CI'.");
            return connectionStringProvider;
        }
    }
}
