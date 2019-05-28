using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Autofac;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json.Linq;

namespace Rhetos.MSBuildExtension
{
    public class RhetosGenerateTask : Task
    {
        [Required]
        public string NugetFolder { get; set; }

        [Required]
        public string ResolvedPackagesFile { get; set; }

        [Required]
        public ITaskItem[] References { get; set; }

        [Required]
        public string ProjectFullPath { get; set; }

        [Required]
        public string OutputFolder { get; set; }

        private List<string> references;

        private List<string> packagesPaths;

        private string projectFolderFullPath;

        private string generatedFolderFullPath;

        public override bool Execute()
        {
            projectFolderFullPath = Path.GetDirectoryName(ProjectFullPath);
            generatedFolderFullPath = Path.Combine(projectFolderFullPath, OutputFolder);
            references = References.Select(x => x.ToString()).ToList();
            var resolvedPackages = JObject.Parse(File.ReadAllText(ResolvedPackagesFile));
            packagesPaths = new List<string>();
            foreach (var library in resolvedPackages["libraries"])
            {
                var libraryValue = library.Value<JProperty>().Value as JObject;
                packagesPaths.Add(Path.Combine(NugetFolder, (string)libraryValue["path"]));
            }

            try
            {
                using (Process myProcess = new Process())
                {
                    myProcess.StartInfo.UseShellExecute = false;
                    myProcess.StartInfo.FileName = @"..\..\..\RhetosCLI\bin\Debug";
                    myProcess.StartInfo.CreateNoWindow = true;
                    myProcess.StartInfo.Arguments = "generate" + " " + string.Join(" ", references.Select(x => "--reference \"" + x + "\"").ToArray()) +
                         " " + string.Join(" ", packagesPaths.Select(x => "--package \"" + x + "\"").ToArray()) + 
                        " --output-folder " + generatedFolderFullPath + " --project-folder " + projectFolderFullPath;
                    Log.LogMessage(MessageImportance.High, "Command line arguments: " + myProcess.StartInfo.Arguments);
                    myProcess.OutputDataReceived += MyProcess_OutputDataReceived;
                    myProcess.Start();
                }
            }
            catch (Exception e)
            {
                Log.LogMessage(MessageImportance.High, "Rhetos gneretae task: " + e.Message);
                return false;
            }

            return true;
        }

        private void MyProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Log.LogMessage(MessageImportance.High, e.Data);
        }
    }
}