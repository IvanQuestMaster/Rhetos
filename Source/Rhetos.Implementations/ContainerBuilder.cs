using System;
using Autofac;
using Rhetos.Extensibility;

namespace Rhetos
{
    public class ContainerBuilder : IContainerBuilder
    {
        Autofac.ContainerBuilder _containerBuilder;
        string _pluginsFolder;

        public ContainerBuilder(Autofac.ContainerBuilder containerBuilder, string pluginsFolder)
        {
            _containerBuilder = containerBuilder;
            _pluginsFolder = pluginsFolder;
        }

        public void RegisterPlugins<T>()
        {
            Plugins.FindAndRegisterPlugins<T>(_containerBuilder, _pluginsFolder);
        }

        public void RegisterPlugins<T>(Type type)
        {
            Plugins.FindAndRegisterPlugins<T>(_containerBuilder, type, _pluginsFolder);
        }

        public IRegistrationBuilder RegisterType<T>()
        {
            return new RegistrationBuilder<T, Autofac.Builder.ConcreteReflectionActivatorData, Autofac.Builder.SingleRegistrationStyle>(_containerBuilder.RegisterType<T>());
        }
    }
}
