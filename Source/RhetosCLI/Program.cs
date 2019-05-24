using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Mono.Options;
using Rhetos.Deployment;
using Rhetos.Dom;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;
using System.Configuration;
using System.Threading;

namespace RhetosCLI
{
    class Program
    {
        static void Main(string[] args)
        {
            var mainArgs = new MainArgs();

            var command = args[0];

            var p = new OptionSet() {
                { "project-folder=",
                   v => mainArgs.ProjectFolder = v },
                { "output-folder=",
                    v => mainArgs.OutputFolder = v },
                { "plugins-folder=",
                   v => mainArgs.PluginsFolder = v },
                { "p|package=",
                   v => mainArgs.Packages.Add (v) },
                { "r|reference=",
                   v => mainArgs.References.Add (v) },
                { "database-language=",
                   v => mainArgs.DatabaseLanguage = v },
                { "connection-string=",
                   v => mainArgs.ConnectionString = v },
                { "h|help",  "show this message and exit",
                   v => mainArgs.ShowHelp = v != null },
            };
            p.Parse(args);

            if (mainArgs.ShowHelp || command.Equals("help", StringComparison.InvariantCultureIgnoreCase))
                ShowHelp(p);

            if (command.Equals("generate", StringComparison.InvariantCultureIgnoreCase))
                ExecuteGenerateCommand(mainArgs);

            if (command.Equals("deploy", StringComparison.InvariantCultureIgnoreCase))
                ExecuteDeployCommand(mainArgs);
        }

        static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Rhetos - A DSL platform");
            p.WriteOptionDescriptions(Console.Out);
        }

        static void ExecuteGenerateCommand(MainArgs args)
        {
            Paths.InitializePaths(args.ProjectFolder, args.PluginsFolder, args.OutputFolder, args.Packages.ToArray(), args.References.ToArray());
            SqlUtility.Initialize(args.DatabaseLanguage);
            ConfigUtility.Initialize(new Dictionary<string, string>(), new ConnectionStringSettings("ServerConnectionString", "", "Rhetos.MsSql"));

            ILogger logger = new ConsoleLogger("DeployPackages"); // Using the simplest logger outside of try-catch block.
            DeployArguments arguments = new DeployArguments(new string[] { "/ExecuteGeneratorsOnly"});

            try
            {
                logger = DeploymentUtility.InitializationLogProvider.GetLogger("DeployPackages"); // Setting the final log provider inside the try-catch block, so that the simple ConsoleLogger can be used (see above) in case of an initialization error.

                InitialCleanup(logger);
                GenerateApplication(logger, arguments);
              
                logger.Trace("Done.");
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());

                if (ex is ReflectionTypeLoadException)
                    logger.Error(CsUtility.ReportTypeLoadException((ReflectionTypeLoadException)ex));
            }
        }

        private static void InitialCleanup(ILogger logger)
        {

            logger.Trace("Moving old generated files to cache.");
            var filesUtility = new FilesUtility(DeploymentUtility.InitializationLogProvider);
            new GeneratedFilesCache(DeploymentUtility.InitializationLogProvider).MoveGeneratedFilesToCache();
            filesUtility.SafeCreateDirectory(Paths.GeneratedFolder);
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
                var performanceLogger = container.Resolve<ILogProvider>().GetLogger("Performance");
                performanceLogger.Write(stopwatch, "DeployPackages.Program: Modules and plugins registered.");
                Plugins.LogRegistrationStatistics("Generating application", container);

                if (arguments.Debug)
                    container.Resolve<DomGeneratorOptions>().Debug = true;

                container.Resolve<ApplicationGenerator>().ExecuteGenerators(arguments);
            }
        }

        private static void ExecuteDeployCommand(MainArgs args)
        {
            Paths.InitializePaths(args.ProjectFolder, args.PluginsFolder, args.OutputFolder, args.Packages.ToArray(), new string[0]);
            ConfigUtility.Initialize(new Dictionary<string, string>(), new ConnectionStringSettings("ServerConnectionString", args.ConnectionString, "Rhetos." + args.DatabaseLanguage));

            AppDomain.CurrentDomain.AssemblyResolve += SearchForAssembly;

            ILogger logger = new ConsoleLogger("DeployPackages"); // Using the simplest logger outside of try-catch block.
            DeployArguments arguments = new DeployArguments(new string[] { });

            logger.Trace("Loading generated plugins.");
            var stopwatch = Stopwatch.StartNew();

            var builder = new ContainerBuilder();
            builder.RegisterModule(new AutofacModuleConfiguration(
                deploymentTime: false,
                configurationArguments: arguments));
            builder.RegisterInstance(arguments);

            using (var container = builder.Build())
            {
                var performanceLogger = container.Resolve<ILogProvider>().GetLogger("Performance");
                container.Resolve<ApplicationGenerator>().UpdateDatabase(arguments);
                var initializers = ApplicationInitialization.GetSortedInitializers(container);

                performanceLogger.Write(stopwatch, "DeployPackages.Program: New modules and plugins registered.");
                Plugins.LogRegistrationStatistics("Initializing application", container);

                if (!initializers.Any())
                {
                    logger.Trace("No server initialization plugins.");
                }
                else
                {
                    foreach (var initializer in initializers)
                        ApplicationInitialization.ExecuteInitializer(container, initializer);
                }
            }
        }

        protected static Assembly SearchForAssembly(object sender, ResolveEventArgs args)
        {
            string pluginAssemblyPath = Path.Combine(Paths.PluginsFolder, new AssemblyName(args.Name).Name + ".dll");
            if (File.Exists(pluginAssemblyPath))
                return Assembly.LoadFrom(pluginAssemblyPath);
            string reference = Paths.References.FirstOrDefault(x => Path.GetFileNameWithoutExtension(x) == args.Name);
            if ( reference!= null && File.Exists(reference))
                return Assembly.LoadFrom(reference);
            return null;
        }

        private static void PrintSummary(Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("=== ERROR SUMMARY ===");
            DeploymentUtility.WriteError(ex.GetType().Name + ": " + ExceptionsUtility.SafeFormatUserMessage(ex));
            Console.WriteLine();
            Console.WriteLine("See DeployPackages.log for more information on error. Enable TraceLog in DeployPackages.exe.config for even more details.");
        }
    }
}
