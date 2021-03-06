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
using Rhetos.Persistence;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Rhetos.Dom.DefaultConcepts.Persistence
{
    public class EntityFrameworkMetadata
    {
        private readonly ILogger _performanceLogger;
        private MetadataWorkspace _metadataWorkspace;
        private bool _initialized;
        private object _initializationLock = new object();

        public EntityFrameworkMetadata(ILogProvider logProvider)
        {
            _performanceLogger = logProvider.GetLogger("Performance");
        }

        public MetadataWorkspace MetadataWorkspace
        {
            get
            {
                if (!_initialized)
                    lock (_initializationLock)
                        if (!_initialized)
                        {
                            var sw = Stopwatch.StartNew();

                            var filesFromCode = SegmentsFromCode.Select(segment => segment.FileName)
                                .Select(fileName => Path.Combine(Paths.GeneratedFolder, fileName))
                                .ToList();

                            if (File.Exists(filesFromCode.First()))
                            {
                                var filesFromGenerator = EntityFrameworkMapping.ModelFiles
                                    .Select(fileName => Path.Combine(Paths.GeneratedFolder, fileName));
                                var loadFiles = filesFromGenerator.Concat(filesFromCode)
                                    .Where(file => File.Exists(file))
                                    .ToList();
                                _metadataWorkspace = new MetadataWorkspace(loadFiles, new Assembly[] { });
                                _performanceLogger.Write(sw, "EntityFrameworkMetadata: Load EDM files.");
                            }
                            else
                                throw new FrameworkException("Entity Framework metadata files are not yet generated.");

                            _initialized = true;
                        }

                return _metadataWorkspace;
            }
        }

        internal class Segment
        {
            public string TagName;
            public string FileName;
        }

        internal static readonly Segment[] SegmentsFromCode = new Segment[]
        {
            new Segment { FileName = "ServerDomEdmFromCode.csdl", TagName = "ConceptualModels" },
            new Segment { FileName = "ServerDomEdmFromCode.msl", TagName = "Mappings" },
            new Segment { FileName = "ServerDomEdmFromCode.ssdl", TagName = "StorageModels" },
        };
    }
}
