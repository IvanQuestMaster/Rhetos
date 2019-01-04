using Autofac;
using Rhetos;
using Rhetos.Utilities;
using System;

namespace RhetosBuild
{
    class Program
    {
        static void Main(string[] args)
        {
            var rhetosConfiguration = new RhetosConfiguration
            {
                GeneratedFilesCacheFolder = "",
                GeneratedFolder = "",
                PluginsFolder = ""
            };

            var builder = new ContainerBuilder();
            builder.RegisterInstance(rhetosConfiguration).As<IRhetosConfiguration>();
            builder.RegisterModule(new DefaultConfiguration(rhetosConfiguration));

            using (var container = builder.Build())
            {
                var performanceLogger = container.Resolve<ILogProvider>().GetLogger("Performance");
                var generators = container.Resolve<IPluginsContainer<IGenerator>>().GetPlugins();

                foreach (var generator in generators)
                    generator.Generate();
            }
        }
    }
}
