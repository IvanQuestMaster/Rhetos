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
using System.IO;
using System.Text;

namespace Rhetos
{
    public class RhetosBuildInputFile
    {
        public const string FileName = "RhetosBuildInput.json";

        public static string GetRhetosBuildInputPath(string projectRootFolder) => Path.Combine(projectRootFolder, "obj", FileName);

        public static RhetosBuildInput Load(string projectRootFolder)
        {
            if (!File.Exists(GetRhetosBuildInputPath(projectRootFolder)))
                return null;

            string serialized = File.ReadAllText(GetRhetosBuildInputPath(projectRootFolder), Encoding.UTF8);
            return JsonConvert.DeserializeObject<RhetosBuildInput>(serialized, _serializerSettings);
        }

        public static void Save(RhetosBuildInput rhetosBuildInput)
        {
            string serialized = JsonConvert.SerializeObject(rhetosBuildInput, _serializerSettings);
            File.WriteAllText(GetRhetosBuildInputPath(rhetosBuildInput.ProjectRootPath), serialized, Encoding.UTF8);
        }

        private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.All,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            Formatting = Formatting.Indented
        };
    }
}
