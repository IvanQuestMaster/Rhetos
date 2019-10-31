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
using Rhetos.Utilities.ApplicationConfiguration;
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

        public Dictionary<string, object> Load()
        {
            var configuration = new Dictionary<string, object>();

            args.HasCommand("generate", () => configuration.Add("RunGenerators", true))
                .HasOption("help|h", hasOption => configuration.Add("RunGenerators__ShowHelp", hasOption))
                .GetOptionValue("output|o", option => configuration.Add("RunGenerators__GeneratedFolderPath", option))
                .GetOptionValues("assembly|a", options => configuration.Add("RunGenerators__Assemblies", options))
                .GetOptionValues("source|s", options => configuration.Add("RunGenerators__GeneratorSources", options));

            args.HasOption("help|h", hasOption => configuration.Add("ShowHelp", hasOption));

            args.HasCommand("help", () => configuration.Add("ShowHelp", true));

            args.HasCommand("deploy", () => configuration.Add("Deploy", true));

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

        public static string GetRunGeneratorsHelp()
        {
            return $@"Options:
    -h, --help              Show command line help.
    -o, --output            Path to the folder where the generated files will be created
    -a, --assembly          List of assemblies that will be used to generate the files.
    -s, --source            List of source folders that will be used as input to generate the files.";
        }
    }
}
