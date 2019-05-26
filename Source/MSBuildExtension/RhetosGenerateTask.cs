using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Autofac;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json.Linq;
using Rhetos.Deployment;
using Rhetos.Dom;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;

namespace Rhetos.MSBuildExtension
{
    public class RhetosGenerateTask : Task
    {
        [Required]
        public string NugetFolder { get; set; }

        [Required]
        public string ResolvedPackagesFile { get; set; }

        [Required]
        public ITaskItem[] References { get; set; }

        [Required]
        public string ProjectFullPath { get; set; }

        [Required]
        public string OutputFolder { get; set; }

        private List<string> references;

        private List<string> packagesPaths;

        private string projectFolderFullPath;

        private string generatedFolderFullPath;

        public override bool Execute()
        {
            projectFolderFullPath = Path.GetDirectoryName(ProjectFullPath);
            generatedFolderFullPath = Path.Combine(projectFolderFullPath, OutputFolder);
            references = References.Select(x => x.ToString()).ToList();
            var resolvedPackages = JObject.Parse(File.ReadAllText(ResolvedPackagesFile));
            packagesPaths = new List<string>();
            foreach (var library in resolvedPackages["libraries"])
            {
                var libraryValue = library.Value<JProperty>().Value as JObject;
                packagesPaths.Add(Path.Combine(NugetFolder, (string)libraryValue["path"]));
            }

            AppDomain.CurrentDomain.AssemblyResolve += SearchForAssembly;

            ExecuteGenerateCommand();

            return true;
        }

        void ExecuteGenerateCommand()
        {
            Paths.InitializePaths(projectFolderFullPath, Path.Combine(projectFolderFullPath, @"\bin\Debug"), generatedFolderFullPath, packagesPaths.ToArray(), references.ToArray());
            ConfigUtility.Initialize(new Dictionary<string, string>(), new ConnectionStringSettings("ServerConnectionString", "EmptyString", "Rhetos.MsSql"));

            Rhetos.Logging.ILogger logger = new ConsoleLogger("DeployPackages"); // Using the simplest logger outside of try-catch block.
            DeployArguments arguments = new DeployArguments(new string[] { "/ExecuteGeneratorsOnly" });

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

        private void InitialCleanup(Rhetos.Logging.ILogger logger)
        {

            logger.Trace("Moving old generated files to cache.");
            var filesUtility = new FilesUtility(DeploymentUtility.InitializationLogProvider);
            new GeneratedFilesCache(DeploymentUtility.InitializationLogProvider).MoveGeneratedFilesToCache();
            filesUtility.SafeCreateDirectory(Paths.GeneratedFolder);
        }

        private void GenerateApplication(Rhetos.Logging.ILogger logger, DeployArguments arguments)
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

        protected Assembly SearchForAssembly(object sender, ResolveEventArgs args)
        {
            string pluginAssemblyPath = Path.Combine(Paths.PluginsFolder, new AssemblyName(args.Name).Name + ".dll");
            if (File.Exists(pluginAssemblyPath))
                return Assembly.LoadFrom(pluginAssemblyPath);
            string reference = Paths.References.FirstOrDefault(x => Path.GetFileNameWithoutExtension(x) == args.Name);
            if (reference != null && File.Exists(reference))
                return Assembly.LoadFrom(reference);
            string reference2 = Paths.References.FirstOrDefault(x => Path.GetFileNameWithoutExtension(x) == args.Name.Split(',').FirstOrDefault());
            if (reference2 != null && File.Exists(reference2))
                return Assembly.LoadFrom(reference2);
            return null;
        }
    }
}