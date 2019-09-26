﻿/*
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Utilities
{
    public class Configuration : IConfiguration
    {
        public Lazy<string> GetString(string key, string defaultValue)
        {
            return new Lazy<string>(() => ConfigUtility.GetAppSetting(key) ?? defaultValue);
        }

        public Lazy<int> GetInt(string key, int defaultValue)
        {
            return new Lazy<int>(() =>
            {
                string value = ConfigUtility.GetAppSetting(key);
                if (!string.IsNullOrEmpty(value))
                {
                    if (int.TryParse(value, out int result))
                        return result;
                    throw new FrameworkException("Invalid '" + key + "' parameter in configuration file: '" + value + "' is not an integer value.");
                }
                else
                    return defaultValue;
            });
        }

        public Lazy<bool> GetBool(string key, bool defaultValue)
        {
            return new Lazy<bool>(() =>
            {
                string value = ConfigUtility.GetAppSetting(key);
                if (!string.IsNullOrEmpty(value))
                {
                    if (bool.TryParse(value, out bool result))
                        return result;
                    throw new FrameworkException("Invalid '" + key + "' parameter in configuration file: '" + value + "' is not a boolean value. Allowed values are True and False.");
                }
                else
                    return defaultValue;
            });
        }

        public Lazy<T> GetEnum<T>(string key, T defaultValue) where T: struct
        {
            return new Lazy<T>(() =>
            {
                var value = ConfigUtility.GetAppSetting(key);
                if (!string.IsNullOrEmpty(value))
                {
                    if (Enum.TryParse(value, true, out T parsedValue))
                        return parsedValue;
                    else
                        throw new FrameworkException(
                            $"Invalid '{key}' parameter in configuration file: '{value}' is not a valid value." +
                            $" Allowed values are: {string.Join(", ", Enum.GetNames(typeof(T)))}.");
                }
                else
                    return defaultValue;
            });
        }
    }
}
