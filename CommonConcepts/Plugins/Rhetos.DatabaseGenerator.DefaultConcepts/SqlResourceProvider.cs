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

using System.Globalization;
using System.Resources;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    public class SqlResourceProvider : ISqlResourceProvider
    {
        private readonly ResourceManager _resourceManager;

        public SqlResourceProvider()
        {
            string resourceName = typeof(SqlResourceProvider).Namespace + ".Sql.MsSql";
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
                throw new FrameworkException("Missing SQL resource '" + resourceName + "' for database language 'MsSql'.");
            return value;
        }

        public string Format(string resourceName, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, Get(resourceName), args);
        }
    }
}
