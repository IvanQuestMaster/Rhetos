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
using System.Linq;
using System.Text;
using Autofac;
using Rhetos.Utilities;
using Rhetos.Persistence;
using Rhetos.Extensibility;

namespace Rhetos.Configuration.Autofac
{
    public class UtilitiesModuleConfiguration : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<XmlUtility>().SingleInstance();
            builder.RegisterType<Rhetos.Utilities.Configuration>().As<Rhetos.Utilities.IConfiguration>().SingleInstance();
            Plugins.FindAndRegisterPlugins<ILocalizer>(builder);
            builder.RegisterType<NoLocalizer>().As<ILocalizer>().SingleInstance().PreserveExistingDefaults();
            builder.RegisterType<GeneratedFilesCache>().SingleInstance();
            builder.RegisterType<MsSqlExecuter>().As<ISqlExecuter>().InstancePerLifetimeScope();
            builder.RegisterType<MsSqlUtility2>().As<ISqlUtility>().InstancePerLifetimeScope();
            builder.RegisterType<SqlTransactionBatches>().InstancePerLifetimeScope();
            builder.RegisterType<SqlCommandConfig>().InstancePerLifetimeScope();

            base.Load(builder);
        }
    }
}
