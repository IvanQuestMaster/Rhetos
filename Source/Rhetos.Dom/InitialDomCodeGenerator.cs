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

using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Extensibility;
using Rhetos.Utilities;
using System;
using System.Text;

namespace Rhetos.Dom
{
    public class InitialDomCodeGenerator : IConceptCodeGenerator
    {
        public static readonly string RhetosHostBuilderInitialConfigurationTag = "/*RhetosHostBuilder.InitialConfiguration*/";
        public static readonly string RhetosHostBuilderPluginAssembliesTag = "/*RhetosHostBuilder.PluginAssemblies*/";
        public static readonly string RhetosHostBuilderPluginTypesTag = "/*RhetosHostBuilder.PluginTypes*/";

        private readonly PluginInfoCollection _plugins;
        private readonly DatabaseSettings _databaseSettings;
        private readonly RhetosBuildEnvironment _rhetosBuildEnvironment;

        public InitialDomCodeGenerator(
            PluginInfoCollection plugins,
            DatabaseSettings databaseSettings,
            RhetosBuildEnvironment rhetosBuildEnvironment)
        {
            _plugins = plugins;
            _databaseSettings = databaseSettings;
            _rhetosBuildEnvironment = rhetosBuildEnvironment;
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var addPlugins = new StringBuilder();
            foreach (var plugin in _plugins)
            {
                addPlugins.Append($"typeof({plugin.Type.FullName}),{Environment.NewLine}                ");
            }

            var rhetosHostBuilderCode =
$@"using Autofac;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;


namespace Rhetos
{{
    public static class RhetosHostBuilderAppConfiguration
    {{
        /// <summary>
        /// Configures <see cref=""IRhetosHostBuilder""/> with defaults for <i>{_rhetosBuildEnvironment.OutputAssemblyName}</i> Rhetos app.
        /// </summary>
        /// <remarks>
        /// The following defaults are applied:
        /// <list type=""bullet"">
        /// <item>Build-time configuration settings that are hard-coded in runtime environment:
        ///     <see cref=""RhetosAppOptions.RhetosAppAssemblyName""/>, <see cref=""RhetosAppOptions.RhetosHostFolder""/> and <see cref=""DatabaseSettings""/></item>.
        /// <item>Registers plugin types from referenced libraries, and the current assembly for additional plugin discovery.</item>
        /// <item>Various plugin packages may add additional configuration settings and components registration.</item>
        /// </list>
        /// </remarks>
        public static IRhetosHostBuilder ConfigureRhetosAppDefaults(this IRhetosHostBuilder hostBuilder)
        {{
            hostBuilder
                .ConfigureConfiguration(containerBuilder => containerBuilder
                    .AddKeyValue(
                        ConfigurationProvider.GetKey((RhetosAppOptions o) => o.RhetosAppAssemblyName),
                        typeof(RhetosHostBuilderAppConfiguration).Assembly.GetName().Name)
                    .AddKeyValue(
                        ConfigurationProvider.GetKey((RhetosAppOptions o) => o.RhetosHostFolder),
                        AppContext.BaseDirectory)
                    .AddOptions(new Rhetos.Utilities.DatabaseSettings
                        {{
                            DatabaseLanguage = {CsUtility.QuotedString(_databaseSettings.DatabaseLanguage)},
                        }})
                    )
                .AddPluginAssemblies(GetPluginAssemblies())
                .AddPluginTypes(GetPluginTypes());
            return hostBuilder;
        }}

        public static ContainerBuilder AddRhetosAppDefaults(this ContainerBuilder containerBuilder)
        {{
            RhetosContainerSetup(containerBuilder, new ConsoleLogProvider());
            containerBuilder.AddRhetosRuntime();
            containerBuilder.AddRhetosPluginModules();
            containerBuilder.RegisterType<ConsoleLogProvider>().As<ILogProvider>().SingleInstance();
            containerBuilder.Register(context => {{
                var logProvider = context.Resolve<ILogProvider>();
                var configureConfigurations = context.Resolve<IEnumerable<ConfigureConfiguration>>();
                return new ConfigurationBuilder(logProvider, configureConfigurations).Build();
            }}).As<IConfiguration>().SingleInstance();
            containerBuilder.RegisterInstance(new Rhetos.Utilities.ConfigureConfiguration(confiurationBuilder => {{
                confiurationBuilder
                    .AddKeyValue(
                        ConfigurationProvider.GetKey((RhetosAppOptions o) => o.RhetosAppAssemblyName),
                        typeof(RhetosHostBuilderAppConfiguration).Assembly.GetName().Name)
                    .AddKeyValue(
                        ConfigurationProvider.GetKey((RhetosAppOptions o) => o.RhetosHostFolder),
                        AppContext.BaseDirectory)
                    .AddOptions(new Rhetos.Utilities.DatabaseSettings
                    {{
                        DatabaseLanguage = @""MsSql"",
                    }});
            }}));
            {RhetosHostBuilderInitialConfigurationTag}
            return containerBuilder;
        }}

        private static ContainerBuilder RhetosContainerSetup(ContainerBuilder containerBuilder, ILogProvider logProvider)
        {{
            if (!containerBuilder.Properties.ContainsKey(nameof(IPluginScanner)))
            {{
                var pluginScanner = new RuntimePluginScanner(GetPluginAssemblies(), GetPluginTypes(), logProvider);

                // make properties accessible to modules which are provided with new/unique instance of ContainerBuilder
                containerBuilder.Properties.Add(nameof(IPluginScanner), pluginScanner);
                containerBuilder.Properties.Add(nameof(ILogProvider), logProvider);

                containerBuilder.RegisterBuildCallback(lifetimeScope => {{
                    LegacyUtilities.Initialize(lifetimeScope.Resolve<IConfiguration>());
                }});
            }}

            return containerBuilder;
        }}

        private static IEnumerable<Assembly> GetPluginAssemblies()
        {{
            return new Assembly[]
            {{
                typeof(RhetosHostBuilderAppConfiguration).Assembly,
                {RhetosHostBuilderPluginAssembliesTag}
            }};
        }}

        private static IEnumerable<Type> GetPluginTypes()
        {{
            #pragma warning disable CS0618 // (Type or member is obsolete) Obsolete plugins can be registered without a warning, their usage will show a warning.
            return new Type[]
            {{
                {addPlugins}{RhetosHostBuilderPluginTypesTag}
            }};
            #pragma warning restore CS0618
        }}
    }}
}}
";
            codeBuilder.InsertCodeToFile(rhetosHostBuilderCode, "RhetosHostBuilderAppConfiguration");
        }
    }
}
