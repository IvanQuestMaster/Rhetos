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
using System.Diagnostics;
using System.Globalization;

namespace Rhetos
{
    public enum EventType2
    {
        /// <summary>
        /// Very detailed logs, which may include high-volume information such as protocol payloads. This log level is typically only enabled during development.
        /// </summary>
        Trace,
        /// <summary>
        /// Information messages and warnings, which are normally enabled in production environment.
        /// </summary>
        Info,
        /// <summary>
        /// Error messages, which are normally sent to administrator in production environment.
        /// </summary>
        Error
    };

    public interface ILogger2
    {
        void Write(EventType2 eventType, Func<string> logMessage);
    }

    public static class LoggerHelper
    {
        public static void Write(this ILogger2 logger, EventType2 eventType, string eventData, params object[] eventDataParams)
        {
            if (eventDataParams.Length == 0)
                logger.Write(eventType, () => eventData);
            else
                logger.Write(eventType, () => string.Format(CultureInfo.InvariantCulture, eventData, eventDataParams));
        }

        public static readonly TimeSpan SlowEvent = TimeSpan.FromSeconds(10);

        private static void PerformanceWrite(this ILogger2 performanceLogger, Stopwatch stopwatch, Func<string> fullMessage)
        {
            if (stopwatch.Elapsed >= SlowEvent)
                performanceLogger.Info(fullMessage);
            else
                performanceLogger.Trace(fullMessage);
            stopwatch.Restart();
        }

        /// <summary>
        /// Logs 'Trace' or 'Info' level, depending on the event duration.
        /// Restarts the stopwatch.
        /// </summary>
        public static void Write(this ILogger2 performanceLogger, Stopwatch stopwatch, Func<string> message)
        {
            PerformanceWrite(performanceLogger, stopwatch, () => stopwatch.Elapsed + " " + message());
        }

        /// <summary>
        /// Logs 'Trace' or 'Info' level, depending on the event duration.
        /// Restarts the stopwatch.
        /// </summary>
        public static void Write(this ILogger2 performanceLogger, Stopwatch stopwatch, string message)
        {
            PerformanceWrite(performanceLogger, stopwatch, () => stopwatch.Elapsed + " " + message);
        }

        public static void Error(this ILogger2 log, string eventData, params object[] eventDataParams)
        {
            log.Write(EventType2.Error, eventData, eventDataParams);
        }
        public static void Error(this ILogger2 log, Func<string> logMessage)
        {
            log.Write(EventType2.Error, logMessage);
        }
        public static void Info(this ILogger2 log, string eventData, params object[] eventDataParams)
        {
            log.Write(EventType2.Info, eventData, eventDataParams);
        }
        public static void Info(this ILogger2 log, Func<string> logMessage)
        {
            log.Write(EventType2.Info, logMessage);
        }
        public static void Trace(this ILogger2 log, string eventData, params object[] eventDataParams)
        {
            log.Write(EventType2.Trace, eventData, eventDataParams);
        }
        public static void Trace(this ILogger2 log, Func<string> logMessage)
        {
            log.Write(EventType2.Trace, logMessage);
        }
    }
}