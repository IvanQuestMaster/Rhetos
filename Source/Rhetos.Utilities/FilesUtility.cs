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

using Rhetos.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Rhetos.Utilities
{
    public class FilesUtility
    {
        private readonly ILogger _logger;
        private readonly Lazy<bool> _defaultEncodingWhenReadingFiles;

        public FilesUtility(ILogProvider logProvider)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _defaultEncodingWhenReadingFiles = new Lazy<bool>(GetDefaultEncodingOption);
        }

        private bool GetDefaultEncodingOption()
        {
            string value = ConfigUtility.GetAppSetting("Rhetos.Legacy.DefaultEncodingWhenReadingFiles");
            if (!string.IsNullOrEmpty(value))
                return bool.Parse(value);
            else
                return true;
        }

        private void Retry(Action action, Func<string> actionName)
        {
            const int maxTries = 10;
            for (int tries = maxTries; tries > 0; tries--)
            {
                try
                {
                    action();
                    break;
                }
                catch
                {
                    if (tries <= 1)
                        throw;

                    if (tries == maxTries - 1) // Logging the second retry instead of the first one, because first retries are too common.
                        _logger.Trace(() => "Waiting to " + actionName.Invoke() + ".");

                    if (Environment.UserInteractive)
                        System.Threading.Thread.Sleep(500);
                    continue;
                }
            }
        }

        public void SafeCreateDirectory(string path)
        {
            try
            {
                // When using TortoiseHg and the Rhetos folder is opened in Windows Explorer,
                // Directory.CreateDirectory() will stochastically fail with UnauthorizedAccessException or DirectoryNotFoundException.
                Retry(() => Directory.CreateDirectory(path), () => "create directory " + path);
            }
            catch (Exception ex)
            {
                throw new FrameworkException(String.Format("Can't create directory '{0}'. Check that it's not locked.", path), ex);
            }
        }

        public void SafeDeleteDirectory(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                    return;

                File.SetAttributes(path, FileAttributes.Normal);

                foreach (var dir in Directory.GetDirectories(path, "*", SearchOption.AllDirectories))
                    File.SetAttributes(dir, FileAttributes.Normal);

                foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                    File.SetAttributes(file, FileAttributes.Normal);

                Retry(() => Directory.Delete(path, true), () => "delete directory " + path);

                Retry(() => { if (Directory.Exists(path)) throw new FrameworkException("Failed to delete directory " + path); }, () => "check if directory deleted " + path);
            }
            catch (Exception ex)
            {
                throw new FrameworkException(String.Format("Can't delete directory '{0}'. Check that it's not locked.", path), ex);
            }
        }

        /// <summary>
        /// Creates the directory if it doesn't exists and deletes its content.
        /// This method will not delete the directory and create a new one; the existing directory is kept, in order to reduce locking issues if the folder is opened in command prompt or other application.
        /// </summary>
        public void EmptyDirectory(string path)
        {
            SafeCreateDirectory(path);

            foreach (var file in Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly))
                SafeDeleteFile(file);
            foreach (var folder in Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly))
                SafeDeleteDirectory(folder);
        }

        public void SafeMoveFile(string source, string destination)
        {
            try
            {
                SafeCreateDirectory(Path.GetDirectoryName(destination)); // Less problems with locked folders if the directory is created before moving the file. Locking may occur when using TortoiseHg and the Rhetos folder is opened in Windows Explorer.
                Retry(() => File.Move(source, destination), () => "move file " + source);
            }
            catch (Exception ex)
            {
                throw new FrameworkException(String.Format("Can't move file '{0}' to '{1}'. Check that destination file or folder is not locked.", source, destination), ex);
            }
        }

        public void SafeCopyFile(string sourceFile, string destinationFile)
        {
            try
            {
                SafeCreateDirectory(Path.GetDirectoryName(destinationFile));
                Retry(() => File.Copy(sourceFile, destinationFile), () => "copy file " + sourceFile);
            }
            catch (Exception ex)
            {
                throw new FrameworkException(String.Format("Can't copy file '{0}' to '{1}'. Check that destination folder is not locked.", sourceFile, destinationFile), ex);
            }
        }

        public string SafeCopyFileToFolder(string sourceFile, string destinationFolder)
        {
            string destinationFile = Path.Combine(destinationFolder, Path.GetFileName(sourceFile));
            SafeCopyFile(sourceFile, destinationFile);
            return destinationFile;
        }

        public void SafeDeleteFile(string path)
        {
            if (File.Exists(path))
            {
                File.SetAttributes(path, FileAttributes.Normal);
                Retry(() => File.Delete(path), () => "delete file " + path);
            }
        }

        public string[] SafeGetFiles(string directory, string pattern, SearchOption searchOption)
        {
            if (Directory.Exists(directory))
                return Directory.GetFiles(directory, pattern, searchOption);
            else
                return new string[] { };
        }

        public string ReadAllText(string path)
        {
            if (_defaultEncodingWhenReadingFiles.Value)
            {
                return File.ReadAllText(path, Encoding.Default);
            }
            else
            {
                var text = File.ReadAllText(path);
                //Occurrence of the character � is interpreted as invalid UTF-8
                var inavlidCharIndex = text.IndexOf((char)65533);
                if (inavlidCharIndex != -1)
                    _logger.Info($@"WARNING: File '{path}' contains invalid UTF-8 character at line {ScriptPositionReporting.Line(text, inavlidCharIndex)}. Save text file as UTF-8.");
                return text;
            }
        }

        public static string RelativeToAbsolutePath(string baseFolder, string path)
        {
            return Path.GetFullPath(Path.Combine(baseFolder, path));
        }

        public static string AbsoluteToRelativePath(string baseFolder, string target)
        {
            var baseParts = Path.GetFullPath(baseFolder).Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            var targetParts = Path.GetFullPath(target).Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

            int common = 0;
            while (common < baseParts.Length && common < targetParts.Length
                && string.Equals(baseParts[common], targetParts[common], StringComparison.OrdinalIgnoreCase))
                common++;

            if (common == 0)
                return target;

            var resultParts = Enumerable.Repeat(@"..", baseParts.Length - common)
                .Concat(targetParts.Skip(common));

            var resultPath = string.Join(@"\", resultParts);
            if (resultPath != "")
                return resultPath;
            else
                return ".";
        }

        public static bool SafeTouch(string path)
        {
            var file = new FileInfo(path);
            if (file.Exists)
            {
                var isReadOnly = file.IsReadOnly;
                file.IsReadOnly = false;
                file.LastWriteTime = DateTime.Now;
                file.IsReadOnly = isReadOnly;
                return true;
            }
            else
                return false;
        }
    }
}
