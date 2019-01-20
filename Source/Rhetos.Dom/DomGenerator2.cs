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


using System.IO;
using Rhetos.Compiler;
using System.Reflection;
using Rhetos.Extensibility;
using Rhetos.Logging;
using ICodeGenerator = Rhetos.Compiler.ICodeGenerator;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using Rhetos.Dsl;
using System;

namespace Rhetos.Dom
{
    [Export(typeof(IGenerator))]
    public class DomGenerator2 : IGenerator
    {
        private readonly IPlugins<IConceptCodeGenerator> _pluginRepository;
        private readonly ILogProvider _log;
        private readonly ILogger _logger;
        private readonly IDslModel _dslModel;

        private List<Assembly> _assemblies;

        /// <summary>
        /// If assemblyName is not null, the assembly will be saved on disk.
        /// If assemblyName is null, the assembly will be generated in memory.
        /// </summary>
        public DomGenerator2(
            IPlugins<IConceptCodeGenerator> plugins,
            ILogProvider logProvider,
            IDslModel dslModel)
        {
            _pluginRepository = plugins;
            _log = logProvider;
            _logger = logProvider.GetLogger("CodeGenerator");
            _dslModel = dslModel;
        }


        public IEnumerable<string> Dependencies {
            get { return new List<string>(); }
        }

        public void Generate(string folderPath)
        {
            IAssemblySource assemblySource = ExecutePlugins(_pluginRepository, "/*", "*/", null);
            File.WriteAllText(Path.Combine(folderPath, "ServerDom.cs"), assemblySource.GeneratedCode);
        }

        public IAssemblySource ExecutePlugins<TPlugin>(IPlugins<TPlugin> plugins, string tagOpen, string tagClose, IConceptCodeGenerator initialCodeGenerator)
            where TPlugin : IConceptCodeGenerator
        {
            var stopwatch = Stopwatch.StartNew();

            var codeBuilder = new CodeBuilder(tagOpen, tagClose);

            if (initialCodeGenerator != null)
                initialCodeGenerator.GenerateCode(null, codeBuilder);

            foreach (var conceptInfo in _dslModel.Concepts)
                foreach (var plugin in plugins.GetImplementations(conceptInfo.GetType()))
                {
                    try
                    {
                        plugin.GenerateCode(conceptInfo, codeBuilder);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex.ToString());
                        _logger.Error("Part of the source code that was generated before the exception was thrown is written in the trace log.");
                        _logger.Trace(codeBuilder.GeneratedCode);
                        throw;
                    }
                }

            _logger.Write(stopwatch, "CodeGenerator: Code generated.");

            return codeBuilder;
        }
    }
}
