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
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Web.Configuration;

namespace Rhetos.Utilities
{
    public static class ConfigUtility
    {
        private static bool _initialized;
        private static Dictionary<string, string> _settings;
        private static ConnectionStringSettings _connectionStringSettings;

        public static void Initialize(Dictionary<string, string> settings, ConnectionStringSettings connectionStringSettings)
        {
            if (_initialized)
                throw new Exception("ConfigUtility has already been initialized.");

            _settings = settings;
            _connectionStringSettings = connectionStringSettings;
            _initialized = true;
        }

        /// <summary>
        /// Use "Configuration.GetInt" or "Configuration.GetBool" instead.
        /// Reads the web service configuration from appSettings group in web.config file.
        /// When used in another application (for example, DeployPackages.exe),
        /// the application's ".config" file can be used to override the default settings from the web.config.
        /// </summary>
        public static string GetAppSetting(string key)
        {
            if (_initialized)
            {
                string value = null;
                _settings.TryGetValue(key, out value);
                return value;
            }

            string settingValue = System.Configuration.ConfigurationManager.AppSettings[key];

            if (settingValue == null && !Paths.IsRhetosServer)
            {
                var setting = RhetosWebConfig.Value.AppSettings.Settings[key];
                if (setting != null)
                    settingValue = setting.Value;
            }

            return settingValue;
        }

        private const string ServerConnectionStringName = "ServerConnectionString";

        public static System.Configuration.ConnectionStringSettings GetConnectionString()
        {
            if (_initialized) 
                return _connectionStringSettings;

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
    }
}
