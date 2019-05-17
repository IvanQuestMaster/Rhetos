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

using Newtonsoft.Json;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Rhetos.Deployment
{
    public class InstalledPackages : IInstalledPackages
    {
        private readonly ILogger _logger;

        public InstalledPackages(ILogProvider logProvider)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _packages = new Lazy<IEnumerable<InstalledPackage>>(Load);
        }

        public IEnumerable<InstalledPackage> Packages => _packages.Value;

        private Lazy<IEnumerable<InstalledPackage>> _packages;

        private const string PackagesFileName = "InstalledPackages.json";

        private static string PackagesFilePath => Path.Combine(Paths.GeneratedFolder, PackagesFileName);

        private IEnumerable<InstalledPackage> Load()
        {
            List<InstalledPackage> packages;
            if (Paths.ProjectFolder != null)
            {
                packages = new List<InstalledPackage>(Paths.PackagesFolder.Select(x => new InstalledPackage(null, null, null, x, null, null)));
                packages.Add(new InstalledPackage(null, null, null, Paths.ProjectFolder, null, null));
            }
            else
            {
                string serialized = File.ReadAllText(PackagesFilePath, Encoding.UTF8);
                packages = new List<InstalledPackage>((IEnumerable<InstalledPackage>)JsonConvert.DeserializeObject(serialized, _serializerSettings));

                // Package folder is saved as relative path, to allow moving the deployed folder.
                foreach (var package in packages)
                    package.SetAbsoluteFolderPath();

                foreach (var package in packages)
                    _logger.Trace(() => package.Report());
            }
            return packages;
        }

        public static void Save(IEnumerable<InstalledPackage> packages)
        {
            CsUtility.Materialize(ref packages);

            // Package folder is saved as relative path, to allow moving the deployed folder.
            foreach (var package in packages)
                package.SetRelativeFolderPath();

            string serialized = JsonConvert.SerializeObject(packages, _serializerSettings);

            foreach (var package in packages)
                package.SetAbsoluteFolderPath();

            File.WriteAllText(PackagesFilePath, serialized, Encoding.UTF8);
        }

        private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.All,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            TypeNameHandling = TypeNameHandling.All,
            Formatting = Formatting.Indented
        };
    }
}
