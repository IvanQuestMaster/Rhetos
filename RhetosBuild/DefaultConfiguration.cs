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
using Rhetos;
using Rhetos.Dsl;
using Rhetos.Utilities;
using Rhetos.Extensibility;
using System.Diagnostics.Contracts;

namespace RhetosBuild
{
    public class DefaultConfiguration : Module
    {
        IRhetosConfiguration _rhetosConfiguration;

        public DefaultConfiguration(IRhetosConfiguration rhetosConfiguration)
        {
            _rhetosConfiguration = rhetosConfiguration;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterGeneric(typeof(PluginsMetadataCache<>)).SingleInstance();
            builder.RegisterGeneric(typeof(PluginsContainer<>)).As(typeof(IPluginsContainer<>)).InstancePerLifetimeScope();
            builder.RegisterGeneric(typeof(NamedPlugins<>)).As(typeof(INamedPlugins<>)).InstancePerLifetimeScope();

            builder.RegisterType<DiskDslScriptLoader>().As<IDslScriptsProvider>().SingleInstance();
            builder.RegisterType<Tokenizer>().SingleInstance();
            builder.RegisterType<DslModelFile>().As<IDslModelFile>().SingleInstance();
            builder.RegisterType<DslParser>().As<IDslParser>();
            builder.RegisterType<MacroOrderRepository>().As<IMacroOrderRepository>();
            builder.RegisterType<ConceptMetadata>().SingleInstance();
            builder.RegisterType<InitializationConcept>().As<IConceptInfo>(); // This plugin is registered manually because FindAndRegisterPlugins does not scan core Rhetos dlls.
            Plugins.FindAndRegisterPlugins<IConceptInfo>(builder, _rhetosConfiguration.PluginsFolder);
            Plugins.FindAndRegisterPlugins<IConceptMacro>(builder, typeof(IConceptMacro<>), _rhetosConfiguration.PluginsFolder);

            builder.RegisterType<DslModel>().As<IDslModel>().SingleInstance();

            builder.RegisterType<DslContainer>();
            Plugins.FindAndRegisterPlugins<IDslModelIndex>(builder, _rhetosConfiguration.PluginsFolder);
            builder.RegisterType<DslModelIndexByType>().As<IDslModelIndex>(); // This plugin is registered manually because FindAndRegisterPlugins does not scan core Rhetos dlls.
            builder.RegisterType<DslModelIndexByReference>().As<IDslModelIndex>(); // This plugin is registered manually because FindAndRegisterPlugins does not scan core Rhetos dlls.

            base.Load(builder);
        }
    }
}
