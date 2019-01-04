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

using Rhetos.Dsl;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RhetosBuild
{
    public class DiskDslScriptLoader : IDslScriptsProvider
    {
        private List<DslScript> _scripts = null;
        private readonly object _scriptsLock = new object();

        const string DslScriptsSubfolder = "DslScripts";

        public IEnumerable<DslScript> DslScripts
        {
            get
            {
                if (_scripts == null)
                    lock (_scriptsLock)
                        if (_scripts == null)
                        {
                            _scripts = new List<DslScript>();

                            throw new NotImplementedException();
                        }

                return _scripts;
            }
        }
    }
}