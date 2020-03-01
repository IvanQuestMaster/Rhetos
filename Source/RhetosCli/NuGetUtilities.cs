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

using NuGet.Frameworks;
using NuGet.ProjectModel;
using Rhetos.Deployment;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Rhetos
{
    internal class NuGetUtilities
    {
        const string ProjectAssetsJsonFileName = "project.assets.json";

        private readonly LockFile _lockFile;
        private readonly NuGetFramework _targetFramework;

        public string ProjectName { get { return _lockFile.PackageSpec.Name; } }

        public NuGetUtilities(string projectRootFolder, ILogProvider logProvider, string target)
        {
            var objFolderPath = Path.Combine(projectRootFolder, "obj");
            if (!Directory.Exists(objFolderPath))
                throw new FrameworkException($"Project object files folder '{objFolderPath}' does not exist. Please make sure that a valid project folder is specified, and run NuGet restore before build.");
            var path = Path.Combine(objFolderPath, ProjectAssetsJsonFileName);
            if (!File.Exists(path))
                throw new FrameworkException($"The {ProjectAssetsJsonFileName} file does not exist. Switch to NuGet's PackageReference format type for your project.");
            _lockFile = LockFileUtilities.GetLockFile(path, new NuGetLogger(logProvider));
            _targetFramework = ResolveTargetFramework(target);
        }

        private NuGetFramework ResolveTargetFramework(string target)
        {
            if (string.IsNullOrEmpty(target))
            {
                var targets = _lockFile.Targets.Select(x => x.TargetFramework).Distinct();
                if (targets.Count() > 1)
                {
                    //TODO: Add the option name with which the target framework should be pass to  RhetosCli after it is defined
                    throw new FrameworkException("There are multiple targets set. Pass the target version with the command line option.");
                }
                if (!targets.Any())
                    throw new FrameworkException("No target framework found for the selected project.");

                return targets.First();
            }
            else
            {
                return NuGetFramework.Parse(target);
            }
        }

        internal List<string> GetBuildAssembliesForNugetPackages()
        {
            return GetTargetFrameworkLibraries().Where(x => x.Type == "package")
                .Select(targetLibrary => new { PackageFolder = GetPackageFolderForLibrary(targetLibrary), targetLibrary.CompileTimeAssemblies })
                .SelectMany(targetLibrary => targetLibrary.CompileTimeAssemblies.Select(libFile => Path.Combine(targetLibrary.PackageFolder, GetNormalizedNugetPaths(libFile.Path))))
                .Where(libFile => Path.GetExtension(libFile) == ".dll")
                .ToList();
        }

        internal List<string> GetBuildAssembliesForReferencedProjects(IEnumerable<string> additionalAssemblies)
        {
            var projectOutputAssemblies = GetTargetFrameworkLibraries().Where(x => x.Type == "project")
                .Select(targetLibrary => new { ProjectName = targetLibrary.Name, PackageFolder = GetPackageFolderForLibrary(targetLibrary), targetLibrary.CompileTimeAssemblies })
                .SelectMany(targetLibrary => targetLibrary.CompileTimeAssemblies.Select(assemblyPartialPath =>  new { targetLibrary.ProjectName, targetLibrary.PackageFolder, AssemblyPartialPath = GetNormalizedNugetPaths(assemblyPartialPath.Path) }))
                .ToList();

            var projectAssemblies = new List<string>();
            foreach (var projectOutputAssembly in projectOutputAssemblies)
            {
                var assemblyFullPath = Path.Combine(projectOutputAssembly.PackageFolder, projectOutputAssembly.AssemblyPartialPath);
                if (!File.Exists(assemblyFullPath))
                {
                    var splits = projectOutputAssembly.AssemblyPartialPath.Split(new string []{ @"\placeholder\"}, StringSplitOptions.None);
                    if (splits.Length != 2)
                        throw new FrameworkException($"Unexpected output assembly path for project {projectOutputAssembly.ProjectName} found in {ProjectAssetsJsonFileName} file."); //This should never happen but if does happens what the user should do?
                    var beginPath = Path.Combine(projectOutputAssembly.PackageFolder, splits[0]);
                    var endPath = splits[1];
                    Func<string, bool> patternSearch = (file) => file.StartsWith(splits[0]) && file.EndsWith(splits[0]);
                    var validAssemblies = additionalAssemblies.Where(patternSearch).Distinct();
                    if (validAssemblies.Count() == 1)
                        projectAssemblies.Add(validAssemblies.First());
                    else if (validAssemblies.Count() > 1)
                        throw new FrameworkException($"Found multiple output assemblies for project {projectOutputAssembly.ProjectName}. Pass only one assembly to the option --assemblies argument that matches the following pattern {Path.Combine(projectOutputAssembly.PackageFolder, projectOutputAssembly.AssemblyPartialPath)}");

                    var outputAssemblyFileName = Path.GetFileName(projectOutputAssembly.AssemblyPartialPath);
                    validAssemblies = Directory.GetFiles(projectOutputAssembly.PackageFolder, "*/" + outputAssemblyFileName)
                        .Where(patternSearch).OrderByDescending(f => File.GetLastWriteTime(f));
                    if (validAssemblies.Count() == 1)
                        projectAssemblies.Add(validAssemblies.First());
                    else if (validAssemblies.Count() > 1)
                        throw new FrameworkException($"Found multiple output assemblies for project {projectOutputAssembly.ProjectName}. Please specify which assembly to use with the --assemblies switch.");
                    else
                        throw new FrameworkException($"No assembly found for project {projectOutputAssembly.ProjectName}. Please specify which assembly to use with the --assemblies switch.");
                }
                else
                {
                    projectAssemblies.Add(assemblyFullPath);
                }
            }

            return projectAssemblies;
        }

        internal List<InstalledPackage> GetInstalledPackages()
        {
            var installedPackages = new List<InstalledPackage>();
            var targetLibraries = GetTargetFrameworkLibraries().Where(x => x.Type == "package").ToList();
            foreach (var targetLibrary in targetLibraries)
            {
                var packageFolder = GetPackageFolderForLibrary(targetLibrary);
                var dependencies = targetLibrary.Dependencies.Select(x => new PackageRequest { Id = x.Id, VersionsRange = x.VersionRange.OriginalString });
                var library = _lockFile.GetLibrary(targetLibrary.Name, targetLibrary.Version);
                var contentFiles = library.Files.Select(x => new ContentFile { PhysicalPath = Path.Combine(packageFolder, GetNormalizedNugetPaths(x)), InPackagePath = GetNormalizedNugetPaths(x) }).ToList();
                installedPackages.Add(new InstalledPackage(library.Name, library.Version.Version.ToString(), dependencies, packageFolder, null, null, contentFiles));
            }

            return SortInstalledPackagesByDependencies(targetLibraries, installedPackages);
        }

        internal InstalledPackage GetProjectAsInstalledPackage(string projectRootPath)
        {
            //TODO: We should add the possibility to specify content files as an option
            //MSBuild knows which files are part of the project that it is building so we should use only those files
            var contentFiles = Directory.GetFiles(projectRootPath, "*", SearchOption.AllDirectories)
                .Select(f => new ContentFile { PhysicalPath = f, InPackagePath = FilesUtility.AbsoluteToRelativePath(projectRootPath, f) })
                .Where(c => (!c.InPackagePath.StartsWith("bin") && !c.InPackagePath.StartsWith("obj")))
                .ToList();
            var dependencies = _lockFile.PackageSpec.TargetFrameworks.Single(x => x.FrameworkName == _targetFramework).Dependencies.Select(x => new PackageRequest { Id = x.Name, VersionsRange = x.LibraryRange.VersionRange.OriginalString });
            return new InstalledPackage(ProjectName, "", dependencies, projectRootPath, null, null, contentFiles);
        }

        private IList<LockFileTargetLibrary> GetTargetFrameworkLibraries()
        {
            return _lockFile.Targets.Single(x => x.TargetFramework == _targetFramework && x.RuntimeIdentifier == null).Libraries.ToList();
        }

        private List<InstalledPackage> SortInstalledPackagesByDependencies(IList<LockFileTargetLibrary> targetLibraries, List<InstalledPackage> installedPackages)
        {
            var packages = targetLibraries.Select(x => x.Name).ToList();
            var dependencies = targetLibraries.SelectMany(x => x.Dependencies.Select(y => new Tuple<string, string>(x.Name, y.Id)));
            Graph.TopologicalSort(packages, dependencies);
            packages.Reverse();
            Graph.SortByGivenOrder(installedPackages, packages, x => x.Id);
            return installedPackages;
        }

        private string GetPackageFolderForLibrary(LockFileTargetLibrary targetLibrary)
        {
            var library = _lockFile.GetLibrary(targetLibrary.Name, targetLibrary.Version);
            var packageFolder = _lockFile.PackageFolders
                .Select(x => Path.Combine(x.Path, GetNormalizedNugetPaths(library.Path)))
                .FirstOrDefault(x => Directory.Exists(x));
            if (packageFolder == null)
                throw new FrameworkException($"Could not locate the folder for package '{library.Name}'.");
            return packageFolder;
        }

        private string GetNormalizedNugetPaths(string nugetPath) => nugetPath.Replace('/', '\\');
    }
}
