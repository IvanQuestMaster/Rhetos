﻿/*
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

using Rhetos.Deployment;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Rhetos.Dsl
{
    public class DiskDslScriptLoaderFromSource : IDslScriptsProvider
    {
        private readonly Lazy<IEnumerable<DslScript>> _scripts;
        private readonly FilesUtility _filesUtility;

        public DiskDslScriptLoaderFromSource(RhetosOptions options, FilesUtility filesUtility)
        {
            _scripts = new Lazy<IEnumerable<DslScript>>(() => LoadScripts(options.Sources));
            _filesUtility = filesUtility;
        }

        public IEnumerable<DslScript> DslScripts => _scripts.Value;

        const string DslScriptsSubfolder = "DslScripts";
        const string DslScriptsSubfolderPrefix = DslScriptsSubfolder + @"\";

        private List<DslScript> LoadScripts(string[] sources)
        {
            return sources.SelectMany(LoadPackageScripts).ToList();
        }

        private IEnumerable<DslScript> LoadPackageScripts(string package)
        {
            return Directory.GetFiles(package, "*.rhe", SearchOption.AllDirectories)
                .OrderBy(file => file)
                .Select(file =>
                    new DslScript
                    {
                        // Using package.Id instead of full package subfolder name, in order to keep the same script path between different versions of the package (the folder name will contain the version number).
                        Name = Directory.GetParent(package).Name,
                        Script = _filesUtility.ReadAllText(file),
                        Path = file
                    });
        }
    }
}