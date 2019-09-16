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

using Autofac;
using Rhetos.Configuration.Autofac;
using Rhetos.Deployment;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Security;
using Rhetos.Utilities;
using System.Collections.Generic;

namespace DeployPackages
{
    public static class AutofacModuleConfiguration
    {
        public static ContainerBuilder AddRhetosDeployPackages(this ContainerBuilder builder, bool deploymentTime, DeployArguments configurationArguments)
        {
            // Specific registrations and initialization:
            Plugins.SetInitializationLogging(DeploymentUtility.InitializationLogProvider);

            if (deploymentTime)
            {
                builder.RegisterModule(new DatabaseGeneratorModuleConfiguration(configurationArguments.ShortTransactions));
                builder.RegisterType<DataMigrationScripts>();
                builder.RegisterType<DatabaseCleaner>();
                builder.RegisterType<ApplicationGenerator>();
                Plugins.FindAndRegisterPlugins<IGenerator>(builder);
            }
            else
            {
                builder.RegisterType<ApplicationInitialization>();
                builder.RegisterInstance(configurationArguments).As<DeployArguments>();
                Plugins.FindAndRegisterPlugins<IServerInitializer>(builder);
            }

            // General registrations:
            // builder.AddRhetos(deploymentTime, configurationArguments.DeployDatabaseOnly);
            builder.RegisterModule(new Rhetos.Configuration.Autofac.DefaultAutofacConfiguration(deploymentTime, configurationArguments.DeployDatabaseOnly));

            // Specific registrations override:
            builder.RegisterType<ProcessUserInfo>().As<IUserInfo>();
            builder.RegisterInstance(DeploymentUtility.InitializationLogProvider).As<ILogProvider>(); // InitializationLogProvider allows overriding deployment logging (both within and outside IoC).
            
            return builder;
        }
    }
}