using Autofac;
using System.IO;
using Autofac.Builder;
using Rhetos;
using System;
using System.Linq;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using Rhetos.Extensibility;
using System.Collections.Generic;

namespace RhetosBuild
{
    class Program
    {
        static void Main(string[] args)
        {
            var projectFolder = args[0];
            var pluginsFolder = projectFolder + @"\bin\Debug\netcoreapp2.1";

            var projectDirectory = Directory.GetDirectories(projectFolder, "Rhetos").Single();

            var installedPackages = new InstalledPackages(new List<InstalledPackage>
            {
                new InstalledPackage{ Id = "Project", Version = "1.0.0", Folder = projectDirectory}
            });

            var containerBuilder = new Autofac.ContainerBuilder();
            SetupInitalContiner(containerBuilder, pluginsFolder, installedPackages);
            var containerBuilderImplementation = new Rhetos.ContainerBuilder(containerBuilder, pluginsFolder);

            var modules = MefPluginScanner.FindPlugins(typeof(IModule), pluginsFolder).Select(x => x.Type);
            foreach (var module in modules)
            {
                var instance = Activator.CreateInstance(module) as IModule;
                instance.Load(containerBuilderImplementation);
            }

            var container = containerBuilder.Build();

            var generators = container.Resolve<IPluginsContainer<IGenerator>>();
            var a = generators.GetPlugins().Count();
            foreach (var generator in generators.GetPlugins())
            {
                generator.Generate();
            }
        }

        static void SetupInitalContiner(Autofac.ContainerBuilder containerBuilder, string pluginsFolder, InstalledPackages installedPackages)
        {
            var paths = new MockPaths
            {
                GeneratedFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Generated"),
                GeneratedFilesCacheFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedCache")
            };
            if (!Directory.Exists(paths.GeneratedFolder))
                Directory.CreateDirectory(paths.GeneratedFolder);
            if (!Directory.Exists(paths.GeneratedFilesCacheFolder))
                Directory.CreateDirectory(paths.GeneratedFilesCacheFolder);

            containerBuilder.Register(c => paths).As<IPaths>();
            containerBuilder.Register(c => installedPackages).As<IInstalledPackages>();

            containerBuilder.RegisterType<NLogProvider>().As<ILogProvider>().SingleInstance();
            containerBuilder.RegisterType<MockInstalledPackages>().As<IInstalledPackages>();

            containerBuilder.RegisterGeneric(typeof(PluginsMetadataCache<>)).SingleInstance();
            containerBuilder.RegisterGeneric(typeof(PluginsContainer<>)).As(typeof(IPluginsContainer<>)).InstancePerLifetimeScope();
            containerBuilder.RegisterGeneric(typeof(NamedPlugins<>)).As(typeof(INamedPlugins<>)).InstancePerLifetimeScope();

            Plugins.FindAndRegisterModules(containerBuilder, pluginsFolder);
        }
    }
}
