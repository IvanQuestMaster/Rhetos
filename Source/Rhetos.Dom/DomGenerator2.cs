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

namespace Rhetos.Dom
{
    [Export(typeof(IGenerator))]
    public class DomGenerator2 : IGenerator
    {
        private readonly IPluginsContainer<IConceptCodeGenerator> _pluginRepository;
        private readonly ICodeGenerator _codeGenerator;
        private readonly ILogProvider _log;

        private List<Assembly> _assemblies;

        /// <summary>
        /// If assemblyName is not null, the assembly will be saved on disk.
        /// If assemblyName is null, the assembly will be generated in memory.
        /// </summary>
        public DomGenerator2(
            IPluginsContainer<IConceptCodeGenerator> plugins,
            ICodeGenerator codeGenerator,
            ILogProvider logProvider)
        {
            _pluginRepository = plugins;
            _codeGenerator = codeGenerator;
            _log = logProvider;
        }


        public IEnumerable<string> Dependencies {
            get { return new List<string>(); }
        }

        public void Generate(string folderPath)
        {
            IAssemblySource assemblySource = _codeGenerator.ExecutePlugins(_pluginRepository, "/*", "*/", null);
            File.WriteAllText(Path.Combine(folderPath, "ServerDom.cs"), assemblySource.GeneratedCode);
        }
    }
}
