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

using Rhetos.Utilities;
using Rhetos.Utilities.ApplicationConfiguration.ConfigurationSources;
using System.Collections.Generic;

namespace Rhetos
{
    public class CommandLineArgumentsSourceNew : IConfigurationSource
    {
        private readonly string[] args;

        public CommandLineArgumentsSourceNew(string[] args)
        {
            this.args = args;
        }

        public IDictionary<string, object> Load()
        {
            var configuration = new Dictionary<string, object>();

            if (args.HasCommand("build"))
            {
                var remainaingArgs = args.GetCommand("build", () => configuration.Add("Build", true))
                   .GetOption("help|h", hasOption => configuration.Add("Build__ShowHelp", hasOption))
                   .GetOptionValue("output|o", option => configuration.Add("Build__GeneratedFolderPath", option))
                   .GetOptionValues("assembly|a", options => configuration.Add("Build__Assemblies", options))
                   .GetOptionValues("source|s", options => configuration.Add("Build__GeneratorSources", options));
            }
            else if (args.HasCommand("deploy"))
            {
                var remainaingArgs = args.GetCommand("deploy", () => configuration.Add("Deploy", true))
                    .GetOption("help|h", hasOption => configuration.Add("Deploy__ShowHelp", hasOption))
                    .GetOptionValue("output|o", option => configuration.Add("Deploy__GeneratedFolderPath", option))
                    .GetOptionValues("assembly|a", options => configuration.Add("Deploy__Assemblies", options))
                    .GetOption("short-transactions", hasOption => configuration.Add("Deploy__ShortTransactions", hasOption));
            }
            else if (args.HasCommand("help"))
            {
                var remainaingArgs = args.GetCommand("help", () => configuration.Add("ShowHelp", true));
            }
            else {
                args.GetOption("help|h", hasOption => configuration.Add("ShowHelp", hasOption));
            }

            return configuration;
        }

        public static string GetHelp()
        {
            return $@"Rhetos v3.0.0
Usage: rhetos command [options]

Rhetos commands:
    help                Show command line help.
    generate            Generates ...
    deploy              Deploy the generated applications

Run 'rhetos [command] --help' for more information on a command.";
        }

        public static string GetBuildHelp()
        {
            return $@"Options:
    -h, --help              Show command line help.
    -o, --output            Path to the folder where the generated files will be created
    -a, --assembly          List of assemblies that will be used to generate the files.
    -s, --source            List of source folders that will be used as input to generate the files.";
        }

        public static string GetDeployHelp()
        {
            return $@"Options:
    -h, --help              Show command line help.
    -o, --output            Path to the folder where the generated files are located
    -a, --assembly          List of assemblies that will be used to deploy the application.
    --short-transactions    Commit transaction after creating or dropping each database object.";
        }
    }
}
