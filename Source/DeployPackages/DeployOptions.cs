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

namespace DeployPackages
{
    /// <summary>
    /// Options specific to DeployPackages utility. Should not be used outside of that scope.
    /// </summary>
    public class DeployOptions
    {
        public bool StartPaused { get; set; }
        /// <summary>
        /// No pause on error.
        /// </summary>
        public bool NoPause { get; set; }
        /// <summary>
        /// Do not stop deployment if a NuGet package dependency has an incompatible version.
        /// </summary>
        public bool IgnoreDependencies { get; set; }
        public bool DatabaseOnly { get; set; }
    }
}
