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

using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CSharp;
using Rhetos.Utilities;
using Rhetos.Logging;
using Rhetos.Dsl;
using System.Collections.Generic;

namespace Rhetos.Compiler
{
    public class AssemblyGenerator : IAssemblyGenerator
    {
        private readonly ILogger _performanceLogger;
        private readonly ILogger _logger;
        private readonly Lazy<int> _errorReportLimit;
        private readonly GeneratedFilesCache _filesCache;

        public AssemblyGenerator(ILogProvider logProvider, IConfiguration configuration, GeneratedFilesCache filesCache)
        {
            _performanceLogger = logProvider.GetLogger("Performance");
            _logger = logProvider.GetLogger("AssemblyGenerator");
            _errorReportLimit = configuration.GetInt("AssemblyGenerator.ErrorReportLimit", 5);
            _filesCache = filesCache;
        }

        public Assembly Generate(IAssemblySource assemblySource, CompilerParameters compilerParameters)
        {
            var stopwatch = Stopwatch.StartNew();

            // Prepare compiler parameters:

            compilerParameters.ReferencedAssemblies.AddRange(assemblySource.RegisteredReferences.ToArray());

            if (compilerParameters.WarningLevel == -1)
                compilerParameters.WarningLevel = 4;
            if (compilerParameters.GenerateInMemory)
                throw new FrameworkException("GenerateInMemory compiler parameter is not supported.");
            string dllName = Path.GetFileName(compilerParameters.OutputAssembly);

            // Save source file and it's hash value:

            string sourceCode = // The compiler parameters are included in the source, in order to invalidate the assembly cache when the parameters are changed.
                string.Concat(assemblySource.RegisteredReferences.Select(reference => $"// Reference: {reference}\r\n"))
                + $"// CompilerOptions: \"{compilerParameters.CompilerOptions}\"\r\n\r\n"
                + assemblySource.GeneratedCode;

            string sourceFile = Path.GetFullPath(Path.ChangeExtension(compilerParameters.OutputAssembly, ".cs"));
            var sourceHash = _filesCache.SaveSourceAndHash(sourceFile, sourceCode);
            _performanceLogger.Write(stopwatch, $"AssemblyGenerator: Save source and hash ({dllName}).");

            // Compile assembly or get from cache:

            Assembly generatedAssembly;

            var filesFromCache = _filesCache.RestoreCachedFiles(sourceFile, sourceHash, Path.GetDirectoryName(compilerParameters.OutputAssembly), new[] { ".dll", ".pdb" });
            if (filesFromCache != null)
            {
                _logger.Trace(() => "Restoring assembly from cache: " + dllName + ".");
                if (!File.Exists(compilerParameters.OutputAssembly))
                    throw new FrameworkException($"AssemblyGenerator: RestoreCachedFiles failed to create the assembly file ({dllName}).");

                generatedAssembly = Assembly.LoadFrom(compilerParameters.OutputAssembly);
                _performanceLogger.Write(stopwatch, $"AssemblyGenerator: Assembly from cache ({dllName}).");

                FailOnTypeLoadErrors(generatedAssembly, compilerParameters.OutputAssembly);
                _performanceLogger.Write(stopwatch, $"AssemblyGenerator: Report errors ({dllName}).");
            }
            else
            {
                _logger.Trace(() => "Compiling assembly: " + dllName + ".");
                CompilerResults compilerResults;
                using (CSharpCodeProvider codeProvider = new CSharpCodeProvider())
                    compilerResults = codeProvider.CompileAssemblyFromFile(compilerParameters, sourceFile);
                _performanceLogger.Write(stopwatch, $"AssemblyGenerator: CSharpCodeProvider.CompileAssemblyFromFile ({dllName}).");

                FailOnCompilerErrors(compilerResults, sourceCode, sourceFile, assemblySource);
                FailOnTypeLoadErrors(compilerResults.CompiledAssembly, compilerParameters.OutputAssembly);
                ReportWarnings(compilerResults, sourceFile);
                _performanceLogger.Write(stopwatch, $"AssemblyGenerator: Report errors ({dllName}).");
                generatedAssembly = compilerResults.CompiledAssembly;
            }

            return generatedAssembly;
        }

        private void FailOnCompilerErrors(CompilerResults compilerResults, string sourceCode, string sourcePath, IAssemblySource assemblySource)
        {
            if (compilerResults.Errors.HasErrors)
            {
                var errors = (from System.CodeDom.Compiler.CompilerError error in compilerResults.Errors
                              where !error.IsWarning
                              select error).ToList();

                var report = new StringBuilder();
                report.Append(errors.Count + " errors while compiling '" + Path.GetFileName(sourcePath) + "'");

                if (errors.Count > _errorReportLimit.Value)
                    report.AppendLine(". The first " + _errorReportLimit.Value + " errors:");
                else
                    report.AppendLine(":");

                var newLineIndices = AllIndexesOf(sourceCode, Environment.NewLine);

                foreach (var error in errors.Take(_errorReportLimit.Value))
                {
                    var marker = GetNearestMarker(sourceCode, newLineIndices, error.Line, error.Column);
                    var conceptMarker = assemblySource.GetMarker(marker);
                    report.AppendLine();
                    if(conceptMarker == null)
                        report.AppendLine(error.ErrorText);
                    else
                        report.AppendLine($@"[Error] In the code snippet generated by the concept {conceptMarker.Concept.GetUserDescription()}");
                    if (error.Line > 0 || error.Column > 0)
                        report.AppendLine(ScriptPositionReporting.ReportPosition(sourceCode, error.Line, error.Column, sourcePath));
                }

                if (errors.Count() > _errorReportLimit.Value)
                {
                    report.AppendLine();
                    report.AppendLine("...");
                }

                throw new FrameworkException(report.ToString().Trim());
            }
        }

        private void FailOnTypeLoadErrors(Assembly assembly, string assemblyPath)
        {
            try
            {
                assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                throw new FrameworkException(CsUtility.ReportTypeLoadException(ex, "Error while compiling " + assemblyPath + "."), ex);
            }
        }

        private void ReportWarnings(CompilerResults results, string filePath)
        {
            var warnings = (from System.CodeDom.Compiler.CompilerError error in results.Errors
                            where error.IsWarning
                            select error).ToList();

            var warningGroups = warnings.GroupBy(warning =>
                {
                    string groupKey = warning.ErrorNumber;
                    if (groupKey == "CS0618")
                    {
                        const string obsoleteInfo = "is obsolete: ";
                        int obsoleteInfoStart = warning.ErrorText.IndexOf(obsoleteInfo);
                        if (obsoleteInfoStart != -1)
                            groupKey += " " + warning.ErrorText.Substring(obsoleteInfoStart + obsoleteInfo.Length);
                    }
                    return groupKey;
                });

            foreach (var warningGroup in warningGroups)
            {
                var warning = warningGroup.First();
                var report = new StringBuilder();

                if (warningGroup.Count() > 1)
                    report.AppendFormat("{0} warnings", warningGroup.Count());
                else
                    report.Append("Warning");

                report.AppendFormat(" {0}: {1}.", warning.ErrorNumber, warning.ErrorText);

                if (!string.IsNullOrEmpty(warning.FileName))
                    report.AppendFormat(" At line {0}, column {1}, file '{2}'.", warning.Line, warning.Column, warning.FileName);
                
                _logger.Info(report.ToString());
            }
        }

        private static List<int> AllIndexesOf(string str, string value)
        {
            if (String.IsNullOrEmpty(value))
                throw new ArgumentException("the string to find may not be empty", "value");
            List<int> indexes = new List<int>();
            for (int index = 0; ; index += value.Length)
            {
                index = str.IndexOf(value, index);
                if (index == -1)
                    return indexes;
                indexes.Add(index);
            }
        }


        private int GetNearestMarker(string sourceCode, List<int> newLineInidces, int row, int column)
        {
            var charPosition = newLineInidces[row] + column;
            Regex regex = new Regex("\\/\\*MrkStart(\\d+)\\*\\/", RegexOptions.RightToLeft);
            Match match = regex.Match(sourceCode, charPosition);
            if (match.Success)
            {
                return int.Parse(match.Groups[1].Value);
            }
            else
            {
                return -1;
            }
        }
    }
}