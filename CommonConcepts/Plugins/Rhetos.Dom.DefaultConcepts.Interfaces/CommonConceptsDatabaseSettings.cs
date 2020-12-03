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

namespace Rhetos.Dom.DefaultConcepts
{
    /// <summary>
    /// This class is available at both build-time and run-time.
    /// In build-time it is configurable by build settings (see <see cref="Rhetos.Utilities.RhetosBuildEnvironment.ConfigurationFileName"/>).
    /// In run-time it is hardcoded, returning settings from the build.
    /// </summary>
    [Options("CommonConcepts")]
    public class CommonConceptsDatabaseSettings
    {
        /// <summary>
        /// For backward compatibility, the DateTime properties will generate 'datetime' columns in database, instead of 'datetime2'.
        /// The datetime2 column type is recommended for all new applications.
        /// Note that the legacy datetime column type has a rounding error with comparison operators on Entity Framework,
        /// see unit test DateTimeConsistencyTest() in CommonConcepts.Test.
        /// </summary>
        public bool UseLegacyMsSqlDateTime { get; set; } = true;

        /// <summary>
        /// It is recommended to use precision 3 to avoid round-trip issues with front end
        /// (for example, JavaScript usually works with time in milliseconds).
        /// For specific high-precision measurement, a new DSL property concept could be created.
        /// Also note that SYSDATETIME() in SQL Server typically does not provide higher accuracy then 1 ms.
        /// </summary>
        public int DateTimePrecision { get; set; } = 3;
    }
}
