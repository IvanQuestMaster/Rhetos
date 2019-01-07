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

using System.Collections.Generic;
using System.Linq;

namespace Rhetos
{
    /// <summary>
    /// Note: Names plugins cannot be used to list all plugins (Autofac limitation). It that feature is needed, the plugin must
    /// be registered both as a named (keyed) service, and as a simple service, then use PluginsContainer to get all plugins.
    /// </summary>
    public interface INamedPlugins<TPlugin>
    {
        IEnumerable<TPlugin> GetPlugins(string name);
    }

    public static class NamedPluginsExtensions
    {
        public static TPlugin GetPlugin<TPlugin>(this INamedPlugins<TPlugin> namedPlugins, string name)
        {
            var plugins = namedPlugins.GetPlugins(name);

            if (plugins.Count() == 0)
                throw new Rhetos.FrameworkException("There is no " + typeof(TPlugin).Name + " plugin named '" + name + "'.");

            if (plugins.Count() > 1)
                throw new Rhetos.FrameworkException("There is more than one " + typeof(TPlugin).Name + " plugin named '" + name
                    + "': " + plugins.First().GetType().FullName + ", " + plugins.Last().GetType().FullName + ".");

            return plugins.First();
        }
    }
}
