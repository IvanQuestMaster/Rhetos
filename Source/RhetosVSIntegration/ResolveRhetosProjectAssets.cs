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

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Rhetos;
using Rhetos.Deployment;
using Rhetos.Utilities;
using System.Linq;

namespace RhetosVSIntegration
{
    public class ResolveRhetosProjectAssets : Task
    {
        [Required]
        public string ProjectDirectory { get; set; }

        [Required]
        public string AssemblyName { get; set; }

        [Required]
        public ITaskItem[] Assemblies { get; set; }

        [Required]
        public ITaskItem[] ProjectContentFiles { get; set; }

        public override bool Execute()
        {
            var resolvedProjectContentFiles = ProjectContentFiles.Select(x => new { x.ItemSpec, FullPath = x.GetMetadata("FullPath") });
            var invalidProjectContentFiles = resolvedProjectContentFiles.Where(x => string.IsNullOrEmpty(x.FullPath));
            if (invalidProjectContentFiles.Any())
                throw new FrameworkException("Could not resolve the full path for the Rhetos input files " + string.Join(", ", invalidProjectContentFiles.Select(x => x.ItemSpec)).Limit(1000));

            var assembliesInReferencedProjects = Assemblies.Where(x => !string.IsNullOrEmpty(x.GetMetadata("Project"))).Select(x => new { x.ItemSpec, FullPath = x.GetMetadata("FullPath") });
            var invalidAssembliesInReferencedProjects = assembliesInReferencedProjects.Where(x => string.IsNullOrEmpty(x.FullPath));
            if (invalidAssembliesInReferencedProjects.Any())
                throw new FrameworkException("Could not resolve the full path for the referenced assemblies " + string.Join(", ", invalidAssembliesInReferencedProjects.Select(x => x.ItemSpec)).Limit(1000));

            var nuget = new NuGetUtilities(ProjectDirectory, resolvedProjectContentFiles.Select(x => x.FullPath), new NuGetLogger(Log), null);
            var packagesAssemblies = nuget.GetRuntimeAssembliesFromPackages();

            var rhetosProjectAssets = new RhetosProjectAssets
            {
                InstalledPackages = new InstalledPackages { Packages = nuget.GetInstalledPackages() },
                Assemblies = packagesAssemblies.Union(assembliesInReferencedProjects.Select(x => x.FullPath)),
                OutputAssemblyName = AssemblyName
            };

            var rhetosProjectAssetsFileProvider = new RhetosProjectAssetsFileProvider(ProjectDirectory, new VSLogProvider(Log));
            rhetosProjectAssetsFileProvider.Save(rhetosProjectAssets);
            //The file touch is added to notify the language server that something has happened even if the file has not been changed.
            //This is a problem when in a referenced project we implement a new concept, the RhetosProjectAssetsFile remains the same but the language server
            //must be restarted to take into account the new concept
            FilesUtility.SafeTouch(rhetosProjectAssetsFileProvider.ProjectAssetsFilePath);

            return true;
        }
    }
}
