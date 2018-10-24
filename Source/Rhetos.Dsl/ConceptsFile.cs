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

using Newtonsoft.Json;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Rhetos.Dsl
{
    public class ConceptsFile : IConceptsFile
    {
        private readonly ILogger _performanceLogger;
        private readonly ILogger _logger;

        public ConceptsFile(ILogProvider logProvider)
        {
            _performanceLogger = logProvider.GetLogger("Performance");
            _logger = logProvider.GetLogger(GetType().Name);
        }

        public void SaveConcepts(IEnumerable<IConceptInfo> concepts, string fileName)
        {
            var sw = Stopwatch.StartNew();

            var serializerSettings = new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.All,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                TypeNameHandling = TypeNameHandling.All,
            };

            CsUtility.Materialize(ref concepts);
            string serializedConcepts = JsonConvert.SerializeObject(concepts, serializerSettings);
            string path = Path.Combine(Paths.GeneratedFolder, fileName);
            File.WriteAllText(path, serializedConcepts, Encoding.UTF8);

            _performanceLogger.Write(sw, "ConceptsFile.Save.");
        }

        public IEnumerable<IConceptInfo> LoadConcepts(string fileName, ConceptsFileSource conceptsFileSource)
        {
            var sw = Stopwatch.StartNew();

            string path = null;
            if (conceptsFileSource == ConceptsFileSource.GeneratedFiles)
                path = Path.Combine(Paths.GeneratedFolder, fileName);
            else
                path = Path.Combine(Paths.GeneratedFilesCacheFolder, fileName);

            if (!File.Exists(path))
                return new IConceptInfo[0];

            string serializedConcepts = File.ReadAllText(path, Encoding.UTF8);

            var serializerSettings = new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.All,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                TypeNameHandling = TypeNameHandling.All,
            };

            var concepts = (IEnumerable<IConceptInfo>)JsonConvert.DeserializeObject(serializedConcepts, serializerSettings);
            _performanceLogger.Write(sw, "ConceptsFile.Load.");
            return concepts;
        }
    }
}
