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

using Autofac;
using Rhetos.Configuration.Autofac;
using Rhetos.Logging;
using Rhetos.Security;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using Rhetos.Utilities.ApplicationConfiguration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonConcepts.Test
{
    /// <summary>
    /// These methods should be called before any object is resolved from the container.
    /// </summary>
    public static class RhetosTestContainerExtensions
    {
        public static void AddLogMonitor(this RhetosTestContainer container, List<string> log, EventType minLevel = EventType.Trace)
        {
            container.InitializeSession += builder =>
                builder.RegisterInstance(new ConsoleLogProvider((eventType, eventName, message) =>
                {
                    if (eventType >= minLevel)
                        log.Add("[" + eventType + "] " + (eventName != null ? (eventName + ": ") : "") + message());
                }))
                .As<ILogProvider>();
        }

        /// <summary>
        /// Turns off claim-based permissions check.
        /// </summary>
        public static void AddIgnoreClaims(this RhetosTestContainer container)
        {
            container.InitializeSession += builder => builder.RegisterType<IgnoreAuthorizationProvider>().As<IAuthorizationProvider>();
        }

        private class IgnoreAuthorizationProvider : IAuthorizationProvider
        {
            public IgnoreAuthorizationProvider() { }

            public IList<bool> GetAuthorizations(IUserInfo userInfo, IList<Claim> requiredClaims)
            {
                return requiredClaims.Select(c => true).ToList();
            }
        }

        public static void AddFakeUser(this RhetosTestContainer container, string username)
        {
            container.InitializeSession += builder =>
                builder.RegisterInstance(new TestUserInfo(username))
                .As<IUserInfo>();
        }

        public static void OverrideRhetosAppOptions(this RhetosTestContainer container, RhetosAppOptions newRhetosAppOptions)
        {
            container.InitializeSession += builder => builder.RegisterInstance(newRhetosAppOptions);
        }

        [Obsolete("Use OverrideRhetosAppOptions")]
        public static void OverrideConfiguration(this RhetosTestContainer container, params (string Key, object Value)[] settings)
        {
            var mockConfigurationBuilder = new ConfigurationBuilder()
                .AddConfigurationManagerConfiguration();
            
            foreach (var setting in settings)
            {
                Console.WriteLine($"{setting.Key} = {setting.Value}");
                mockConfigurationBuilder.AddKeyValue(setting.Key, setting.Value);
            }

            var mockConfiguration = mockConfigurationBuilder.Build();

            // register new instances of ConfigurationProvider and new resolved RhetosAppOptions from it
            // WARNING: other configured and registered option objects might contain old configuration values
            container.InitializeSession += builder => 
            {
                builder.RegisterInstance(mockConfiguration.GetOptions<RhetosAppOptions>());
                builder.RegisterInstance(mockConfiguration);
            };
        }
    }
}
