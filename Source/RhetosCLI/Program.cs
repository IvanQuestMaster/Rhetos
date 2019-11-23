﻿using Rhetos.Logging;
using Rhetos.Utilities;
using Rhetos.Utilities.ApplicationConfiguration;
using System;

namespace Rhetos
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            var logProvider = new ConsoleLogProvider();
            var logger = logProvider.GetLogger("DeployPackages");

            logger.Trace(() => "Logging configured.");

            try
            {
                var configuration = BuildConfigurationProvider(args);

                if (configuration.GetValue<bool>("ShowHelp", false))
                    Console.Write(CommandLineArgumentsSourceNew.GetHelp());

                if (configuration.GetValue<bool>("Build__ShowHelp", false))
                    Console.Write(CommandLineArgumentsSourceNew.GetBuildHelp());

                if (configuration.GetValue<bool>("Deploy__ShowHelp", false))
                    Console.Write(CommandLineArgumentsSourceNew.GetDeployHelp());
            }
            catch (Exception e)
            {
                return 1;
            }

            Console.ReadKey();
            return 0;
        }

        private static IConfigurationProvider BuildConfigurationProvider(string[] args)
        {
            return new ConfigurationBuilder()
                .AddConfigurationManagerConfiguration()
                .Add(new CommandLineArgumentsSourceNew(args))
                .Build();
        }
    }
}