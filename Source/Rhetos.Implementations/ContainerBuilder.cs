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
using Autofac;

namespace Rhetos.Implementations
{
    public class ContainerBuilder : IContainerBuilder
    {
        Autofac.ContainerBuilder _containerBuilder;
        string _pluginsFolder;

        public ContainerBuilder(Autofac.ContainerBuilder containerBuilder, string pluginsFolder)
        {
            _containerBuilder = containerBuilder;
            _pluginsFolder = pluginsFolder;
        }

        public void RegisterPlugins<T>()
        {
            Plugins.FindAndRegisterPlugins<T>(_containerBuilder, _pluginsFolder);
        }

        public void RegisterPlugins<T>(Type type)
        {
            Plugins.FindAndRegisterPlugins<T>(_containerBuilder, type, _pluginsFolder);
        }

        public IRegistrationBuilder RegisterType<T>()
        {
            return new RegistrationBuilder<T, Autofac.Builder.ConcreteReflectionActivatorData, Autofac.Builder.SingleRegistrationStyle>(_containerBuilder.RegisterType<T>());
        }
    }
}
