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

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Rhetos
{
    public static class NetCoreConfigurationSourcesBuilderExtensions
    {
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddKeyValues(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, IEnumerable<KeyValuePair<string, object>> keyValues)
        {
            //TODO: Instead of ToString it should be used something more sophisticated
            //In worst case scenatio it should be used ToString and throw an exception if something is not supported
            builder.AddInMemoryCollection(keyValues.Select(x => new KeyValuePair<string, string>(x.Key, x.Value.ToString())));
            return builder;
        }

        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddKeyValues(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, params KeyValuePair<string, object>[] keyValues)
        {
            builder.AddKeyValues(keyValues.AsEnumerable());
            return builder;
        }

        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddKeyValue(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, string key, object value)
        {
            builder.AddKeyValues(new KeyValuePair<string, object>(key, value));
            return builder;
        }

        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddOptions(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, object options, string configurationPath = "")
        {
            if (string.IsNullOrEmpty(configurationPath))
                configurationPath = OptionsAttribute.GetConfigurationPath(options.GetType());

            var members = options.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Select(member => (member.Name, Value: member.GetValue(options)))
                .Concat(options.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public).Select(member => (member.Name, Value: member.GetValue(options))));

            string keyPrefix = !string.IsNullOrEmpty(configurationPath) ? configurationPath + ConfigurationProvider.ConfigurationPathSeparator : "";
            var settings = members
                .Select(member => new KeyValuePair<string, object>(keyPrefix + member.Name, member.Value))
                .ToList();

            return builder.AddKeyValues(settings);
        }
    }
}
