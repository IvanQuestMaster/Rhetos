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

using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rhetos
{
    /// <summary>
    /// An internal error of the Rhetos application occurred. Usually a result of incorrect configuration/setup or a platform bug.
    /// Web response HTTP status code on this exception is 500.
    /// </summary>
    [global::System.Serializable]
    public class FrameworkException : RhetosException
    {
        public FrameworkException() { }
        public FrameworkException(string message) : base(message) { }
        public FrameworkException(string message, Exception inner) : base(message, inner) { }
        protected FrameworkException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        public static string GetInternalServerErrorMessage(ILocalizer localizer, Exception exception)
        {
            // This message does not use SeeLogMessage for remove redundant substring,
            // to simplify string template extraction from source for localized error messages.
            return localizer[
                "Internal server error occurred. See RhetosServer.log for more information. ({0}, {1})",
                exception.GetType().Name,
                DateTime.Now.ToString("s")];
        }

        public static string SeeLogMessage(Exception exception)
        {
            return string.Format(
                "See RhetosServer.log for more information. ({0}, {1})",
                exception.GetType().Name,
                DateTime.Now.ToString("s"));
        }
    }
}
