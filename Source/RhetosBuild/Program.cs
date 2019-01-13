using Autofac;
using System.IO;
using Autofac.Builder;
using Rhetos;
using System;
using System.Linq;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using Rhetos.Implementations;
using System.Collections.Generic;
using System.Xml.Linq;

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
            SetupInitalContiner(containerBuilder, pluginsFolder, installedPackages, projectFolder);
            var containerBuilderImplementation = new Rhetos.Implementations.ContainerBuilder(containerBuilder, pluginsFolder);

            var modules = MefPluginScanner.FindPlugins(typeof(IModule), pluginsFolder).Select(x => x.Type);
            foreach (var module in modules)
            {
                var instance = Activator.CreateInstance(module) as IModule;
                instance.Load(containerBuilderImplementation);
            }

            containerBuilderImplementation.RegisterPlugins<IGenerator>();
            var container = containerBuilder.Build();

            var generators = container.Resolve<IPluginsContainer<IGenerator>>();
            foreach (var generator in generators.GetPlugins())
            {
                generator.Generate();
            }

            var initalizers = container.Resolve<IPluginsContainer<IServerInitializer>>();
            foreach (var initializer in initalizers.GetPlugins())
            {
                initializer.Initialize();
            }
        }

        static void SetupInitalContiner(Autofac.ContainerBuilder containerBuilder, string pluginsFolder, InstalledPackages installedPackages, string projectFolder)
        {
            var paths = new Paths
            {
                GeneratedFolder = projectFolder + @"\bin\Generated",
                GeneratedFilesCacheFolder = projectFolder + @"\bin\GeneratedCache"
            };
            if (!Directory.Exists(paths.GeneratedFolder))
                Directory.CreateDirectory(paths.GeneratedFolder);
            if (!Directory.Exists(paths.GeneratedFilesCacheFolder))
                Directory.CreateDirectory(paths.GeneratedFilesCacheFolder);

            containerBuilder.Register(c => paths).As<IPaths>();
            containerBuilder.Register(c => installedPackages).As<IInstalledPackages>();

            containerBuilder.RegisterType<NLogProvider>().As<ILogProvider>().SingleInstance();
            containerBuilder.RegisterGeneric(typeof(PluginsMetadataCache<>)).SingleInstance();
            containerBuilder.RegisterGeneric(typeof(PluginsContainer<>)).As(typeof(IPluginsContainer<>)).InstancePerLifetimeScope();
            containerBuilder.RegisterGeneric(typeof(NamedPlugins<>)).As(typeof(INamedPlugins<>)).InstancePerLifetimeScope();

            containerBuilder.RegisterGeneric(typeof(Index<,>)).As(typeof(IIndex<,>));
            containerBuilder.RegisterType<MockConfiguration>().As<IConfiguration>();
        }
    }
}
