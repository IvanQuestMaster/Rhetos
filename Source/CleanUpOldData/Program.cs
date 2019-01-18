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

using Autofac;
using DeployPackages;
using Rhetos;
using Rhetos.Deployment;
using Rhetos.Logging;
using Rhetos.Persistence;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace CleanupOldData
{
    class Program
    {
        public static object Plugins { get; private set; }

        static int Main(string[] args)
        {
            try
            {
                //TODO:
                Paths.InitializeRhetosServerRootPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".."));

                GenerateApplication(new ConsoleLogger(), new DeployArguments(args));

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                DeploymentUtility.WriteError(ex.Message);
                Console.WriteLine("Details:");
                Console.WriteLine(ex);
                if (Environment.UserInteractive) 
                    Thread.Sleep(3000);
                return 1;
            }
        }

        private static void GenerateApplication(ILogger logger, DeployArguments arguments)
        {
            logger.Trace("Loading plugins.");
            var stopwatch = Stopwatch.StartNew();

            var builder = new ContainerBuilder();
            builder.RegisterModule(new AutofacModuleConfiguration(
                deploymentTime: true,
                configurationArguments: arguments));

            using (var container = builder.Build())
            {
                logger.Write(stopwatch, "CleanupOldData.Program: Modules and plugins registered.");
                
                var connnectionStringConfiguration = container.Resolve<IConnectionStringSettings>();
                Console.WriteLine("SQL connection: " + connnectionStringConfiguration.SqlConnectionInfo(connnectionStringConfiguration.ConnectionString));

                container.Resolve<DatabaseCleaner>().DeleteAllMigrationData();
            }
        }
    }
}
