# Documentation:
# https://docs.microsoft.com/en-us/nuget/reference/ps-reference/ps-ref-get-project
# https://docs.microsoft.com/en-us/dotnet/api/envdte.dte
# https://docs.microsoft.com/en-us/dotnet/api/envdte.projectitems.addfromfilecopy?view=visualstudiosdk-2017

$sourceFolder = "$PSScriptRoot\projectFiles"
$project = (Get-Project)
$projectFolder = (Get-Item $project.FullName).DirectoryName
"Source folder: $sourceFolder"

Copy-Item -Path "$sourceFolder\Web.config" -Destination $projectFolder -Force
Copy-Item -Path "$sourceFolder\Rhetos Server DOM.linq" -Destination $projectFolder -Force
Copy-Item -Path "$sourceFolder\Rhetos Server SOAP.linq" -Destination $projectFolder -Force
Copy-Item -Path "$sourceFolder\Template.ConnectionStrings.config" -Destination $projectFolder -Force

$project.ProjectItems.AddFromFileCopy("$sourceFolder\RhetosService.svc") > $null
$project.ProjectItems.AddFromFileCopy("$sourceFolder\Global.asax") > $null
$project.ProjectItems.AddFromFileCopy("$sourceFolder\Default.aspx") > $null
$project.ProjectItems.AddFromFileCopy("$sourceFolder\RhetosRuntime.cs") > $null

$assemblyName = $project.Properties["AssemblyName"].Value
(Get-Content -Path "$projectFolder\RhetosService.svc" -Raw) -Replace ", Rhetos",", $assemblyName" | Set-Content -Path "$projectFolder\RhetosService.svc" -NoNewline
(Get-Content -Path "$projectFolder\RhetosRuntime.cs" -Raw) -Replace "AssetsFolder = Path.Combine\(assemblyFolder, ""Generated""\),","AssetsFolder = Path.Combine(assemblyFolder, ""RhetosAssets""),`r`n                    AssemblyName = GetType().Assembly.GetName().Name," | Set-Content -Path "$projectFolder\RhetosRuntime.cs" -NoNewline
(Get-Content -Path "$projectFolder\RhetosRuntime.cs" -Raw) -Replace "LegacyPluginsFolder = Path.Combine\(assemblyFolder, ""Plugins""\),","LegacyPluginsFolder = null," | Set-Content -Path "$projectFolder\RhetosRuntime.cs" -NoNewline
