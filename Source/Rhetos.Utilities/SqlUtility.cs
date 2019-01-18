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
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Data.SqlClient;

namespace Rhetos.Utilities
{
    public static class SqlUtility
    {
        private static ISqlUtility _sqlUtility;
        private static SqlCommandConfig _sqlCommandConfig;
        private static IConnectionStringSettings _connectionStringSettings;

        public static void Initialize(ISqlUtility sqlUtility,
            SqlCommandConfig sqlCommandConfig,
            IConnectionStringSettings connectionStringSettings)
        {
            if (_sqlUtility != null || _sqlCommandConfig != null || _connectionStringSettings != null)
                throw new FrameworkException("SqlUtility is already initialized.");

            _sqlUtility = sqlUtility;
            _sqlCommandConfig = sqlCommandConfig;
            _connectionStringSettings = connectionStringSettings;
        }

        private static ISqlUtility GetISqlUtility()
        {
            if (_sqlUtility == null)
                throw new FrameworkException("SqlUtility is not initlaized.");

            return _sqlUtility;
        }

        private static SqlCommandConfig GetSqlCommandConfig()
        {
            if (_sqlCommandConfig == null)
                throw new FrameworkException("SqlUtility is not initlaized.");

            return _sqlCommandConfig;
        }

        private static IConnectionStringSettings GetConnectionStringSettings()
        {
            if (_connectionStringSettings == null)
                throw new FrameworkException("SqlUtility is not initlaized.");

            return _connectionStringSettings;
        }

        /// <summary>
        /// In seconds.
        /// </summary>
        public static int SqlCommandTimeout
        {
            get { return GetSqlCommandConfig().CommandTimeout; }
        }

        public static string DatabaseLanguage
        {
            get { return GetConnectionStringSettings().DatabaseLanguage; }
        }

        public static string NationalLanguage
        {
            get { return GetConnectionStringSettings().NationalLanguage; }
        }


        public static string ConnectionString
        {
            get { return GetConnectionStringSettings().ConnectionString; }
        }

        public static string ProviderName
        {
            get { return GetISqlUtility().ProviderName; }
        }

        public static string UserContextInfoText(IUserInfo userInfo)
        {
            if (!userInfo.IsUserRecognized)
                return "";

            return "Rhetos:" + userInfo.Report();
        }

        public static IUserInfo ExtractUserInfo(string contextInfo)
        {
            if (contextInfo == null)
                return new ReconstructedUserInfo { IsUserRecognized = false, UserName = null, Workstation = null };

            string prefix1 = "Rhetos:";
            string prefix2 = "Alpha:";

            int positionUser;
            if (contextInfo.StartsWith(prefix1))
                positionUser = prefix1.Length;
            else if (contextInfo.StartsWith(prefix2))
                positionUser = prefix2.Length;
            else
                return new ReconstructedUserInfo { IsUserRecognized = false, UserName = null, Workstation = null };

            var result = new ReconstructedUserInfo();

            int positionWorkstation = contextInfo.IndexOf(',', positionUser);
            if (positionWorkstation > -1)
            {
                result.UserName = contextInfo.Substring(positionUser, positionWorkstation - positionUser);
                result.Workstation = contextInfo.Substring(positionWorkstation + 1);
            }
            else
            {
                result.UserName = contextInfo.Substring(positionUser);
                result.Workstation = "";
            }

            result.UserName = result.UserName.Trim();
            if (result.UserName == "") result.UserName = null;
            result.Workstation = result.Workstation.Trim();
            if (result.Workstation == "") result.Workstation = null;

            result.IsUserRecognized = result.UserName != null;
            return result;
        }

        private class ReconstructedUserInfo : IUserInfo
        {
            public bool IsUserRecognized { get; set; }
            public string UserName { get; set; }
            public string Workstation { get; set; }
            public string Report() { return UserName + "," + Workstation; }
        }

        /// <summary>
        /// Throws an exception if 'name' is not a valid SQL database object name.
        /// Function returns given argument so it can be used as fluent interface.
        /// In some cases the function may change the identifier (limit identifier length to 30 on Oracle database, e.g.).
        /// </summary>
        public static string Identifier(string name)
        {
            return GetISqlUtility().Identifier(name);
        }

        public static string QuoteText(string value)
        {
            return GetISqlUtility().QuoteText(value);
        }

        public static string QuoteIdentifier(string sqlIdentifier)
        {
            return GetISqlUtility().QuoteIdentifier(sqlIdentifier);
        }

        public static string GetSchemaName(string fullObjectName)
        {
            return GetISqlUtility().GetSchemaName(fullObjectName);
        }

        public static string GetShortName(string fullObjectName)
        {
            return GetISqlUtility().GetShortName(fullObjectName);
        }

        public static string GetFullName(string objectName)
        {
            return GetISqlUtility().GetFullName(objectName);
        }

        /// <summary>
        /// Vendor-independent database reader.
        /// </summary>
        public static Guid ReadGuid(DbDataReader dataReader, int column)
        {
            return GetISqlUtility().ReadGuid(dataReader, column);
        }

        /// <summary>
        /// Vendor-independent database reader.
        /// </summary>
        public static int ReadInt(DbDataReader dataReader, int column)
        {
            return GetISqlUtility().ReadInt(dataReader, column);
        }

        public static Guid StringToGuid(string guid)
        {
            return GetISqlUtility().StringToGuid(guid);
        }

        public static string QuoteGuid(Guid guid)
        {
            return GetISqlUtility().QuoteGuid(guid);
        }

        public static string QuoteGuid(Guid? guid)
        {
            return GetISqlUtility().QuoteGuid(guid);
        }

        public static string GuidToString(Guid? guid)
        {
            return GetISqlUtility().GuidToString(guid);
        }

        public static string GuidToString(Guid guid)
        {
            return GetISqlUtility().GuidToString(guid);
        }

        public static string QuoteDateTime(DateTime? dateTime)
        {
            return GetISqlUtility().QuoteDateTime(dateTime);
        }

        public static string DateTimeToString(DateTime? dateTime)
        {
            return GetISqlUtility().DateTimeToString(dateTime);
        }

        public static string DateTimeToString(DateTime dateTime)
        {
            return GetISqlUtility().DateTimeToString(dateTime);
        }

        public static string QuoteBool(bool? b)
        {
            return GetISqlUtility().QuoteBool(b);
        }

        public static string BoolToString(bool? b)
        {
            return GetISqlUtility().BoolToString(b);
        }

        public static string BoolToString(bool b)
        {
            return GetISqlUtility().BoolToString(b);
        }

        /// <summary>
        /// Returns empty string if the string value is null.
        /// This function is used for compatibility between MsSql and Oracle string behavior.
        /// </summary>
        public static string EmptyNullString(DbDataReader dataReader, int column)
        {
            return GetISqlUtility().EmptyNullString(dataReader, column);
        }

        public static string SqlConnectionInfo(string connectionString)
        {
            return GetConnectionStringSettings().SqlConnectionInfo(connectionString);
        }

        /// <summary>
        /// Used in DatabaseGenerator to split SQL script generated by IConceptDatabaseDefinition plugins.
        /// </summary>
        public const string ScriptSplitterTag = "/* database generator splitter */";

        /// <summary>
        /// Add this tag to the beginning of the DatabaseGenerator SQL script to execute it without transaction.
        /// Used for special database changes that must be executed without transaction, for example
        /// creating full-text search index.
        /// </summary>
        public const string NoTransactionTag = "/*DatabaseGenerator:NoTransaction*/";

        public static bool ScriptSupportsTransaction(string sql) => !sql.StartsWith(NoTransactionTag);

        public static DateTime GetDatabaseTime(ISqlExecuter sqlExecuter)
        {
            return GetISqlUtility().GetDatabaseTime(sqlExecuter);
        }

        /// <summary>
        /// Splits the script to multiple batches, separated by the GO command.
        /// It emulates the behavior of Microsoft SQL Server utilities, sqlcmd and osql,
        /// but it does not work perfectly: comments near GO, strings containing GO and the repeat count are currently not supported.
        /// </summary>
        public static string[] SplitBatches(string sql)
        {
            return batchSplitter.Split(sql).Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
        }

        private static readonly Regex batchSplitter = new Regex(@"^\s*GO\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
    }
}
