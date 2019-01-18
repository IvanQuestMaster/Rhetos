using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using Rhetos.Utilities;

namespace Rhetos.Deployment
{
    public class SqlResourceProvider : ISqlResourceProvider
    {
        private readonly IConnectionStringSettings _connectionStringSettings;

        private readonly ResourceManager _resourceManager;

        public SqlResourceProvider(IConnectionStringSettings connectionStringSettings)
        {
            _connectionStringSettings = connectionStringSettings;

            string resourceName = typeof(SqlResourceProvider).Namespace + ".Sql." + "MsSql";
            var resourceAssembly = typeof(SqlResourceProvider).Assembly;
            _resourceManager = new ResourceManager(resourceName, resourceAssembly);
        }

        public string TryGet(string resourceName)
        {
            return _resourceManager.GetString(resourceName);
        }

        public string Get(string resourceName)
        {
            var value = TryGet(resourceName);
            if (value == null)
                throw new FrameworkException("Missing SQL resource '" + resourceName + "' for database language '" + _connectionStringSettings.DatabaseLanguage + "'.");
            return value;
        }

        public string Format(string resourceName, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, Get(resourceName), args);
        }
    }
}
