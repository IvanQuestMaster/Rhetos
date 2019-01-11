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
        // TODO: Move most of the methods to ISqlUtility.

        private static ISqlUtility _sqlUtility;

        public static void Initialize(ISqlUtility sqlUtility)
        {
            if (_sqlUtility != null)
                throw new Exception("SqlUtility already initialized");
        }

        private static ISqlUtility GetSqlUtilityOrException()
        {
            if (_sqlUtility == null)
                throw new Exception("SqlUtility is not initialized");

            return _sqlUtility;
        }

        /// <summary>
        /// In seconds.
        /// </summary>
        public static int SqlCommandTimeout
        {
            get
            {
                return GetSqlUtilityOrException().SqlCommandTimeout;
            }
        }

        public static string DatabaseLanguage
        {
            get
            {
                return GetSqlUtilityOrException().DatabaseLanguage;
            }
        }

        public static string NationalLanguage
        {
            get
            {
                return GetSqlUtilityOrException().NationalLanguage;
            }
        }


        public static string ConnectionString
        {
            get
            {
                return GetSqlUtilityOrException().ConnectionString;
            }
        }

        public static string ProviderName
        {
            get
            {
                return GetSqlUtilityOrException().ProviderName;
            }
        }

        /// <summary>
        /// Throws an exception if 'name' is not a valid SQL database object name.
        /// Function returns given argument so it can be used as fluent interface.
        /// In some cases the function may change the identifier (limit identifier length to 30 on Oracle database, e.g.).
        /// </summary>
        public static string Identifier(string name)
        {
            return GetSqlUtilityOrException().Identifier(name);
        }

        public static string QuoteText(string value)
        {
            return GetSqlUtilityOrException().QuoteText(value);
        }

        public static string QuoteIdentifier(string sqlIdentifier)
        {
            return GetSqlUtilityOrException().QuoteIdentifier(sqlIdentifier);
        }

        public static string GetSchemaName(string fullObjectName)
        {
            return GetSqlUtilityOrException().GetSchemaName(fullObjectName);
        }

        public static string GetShortName(string fullObjectName)
        {
            return GetSqlUtilityOrException().GetShortName(fullObjectName);
        }

        public static string GetFullName(string objectName)
        {
            return GetSqlUtilityOrException().GetFullName(objectName);
        }

        /// <summary>
        /// Vendor-independent database reader.
        /// </summary>
        public static Guid ReadGuid(DbDataReader dataReader, int column)
        {
            return GetSqlUtilityOrException().ReadGuid(dataReader, column);
        }

        /// <summary>
        /// Vendor-independent database reader.
        /// </summary>
        public static int ReadInt(DbDataReader dataReader, int column)
        {
            return GetSqlUtilityOrException().ReadInt(dataReader, column);
        }

        public static Guid StringToGuid(string guid)
        {
            return GetSqlUtilityOrException().StringToGuid(guid);
        }

        public static string QuoteGuid(Guid guid)
        {
            return GetSqlUtilityOrException().QuoteGuid(guid);
        }

        public static string QuoteGuid(Guid? guid)
        {
            return GetSqlUtilityOrException().QuoteGuid(guid);
        }

        public static string GuidToString(Guid? guid)
        {
            return GetSqlUtilityOrException().GuidToString(guid);
        }

        public static string GuidToString(Guid guid)
        {
            return GetSqlUtilityOrException().GuidToString(guid);
        }

        public static string QuoteDateTime(DateTime? dateTime)
        {
            return GetSqlUtilityOrException().QuoteDateTime(dateTime);
        }

        public static string DateTimeToString(DateTime? dateTime)
        {
            return GetSqlUtilityOrException().DateTimeToString(dateTime);
        }

        public static string DateTimeToString(DateTime dateTime)
        {
            return GetSqlUtilityOrException().DateTimeToString(dateTime);
        }

        public static string QuoteBool(bool? b)
        {
            return GetSqlUtilityOrException().QuoteBool(b);
        }

        public static string BoolToString(bool? b)
        {
            return GetSqlUtilityOrException().BoolToString(b);
        }

        public static string BoolToString(bool b)
        {
            return GetSqlUtilityOrException().BoolToString(b);
        }

        /// <summary>
        /// Returns empty string if the string value is null.
        /// This function is used for compatibility between MsSql and Oracle string behavior.
        /// </summary>
        public static string EmptyNullString(DbDataReader dataReader, int column)
        {
            return GetSqlUtilityOrException().EmptyNullString(dataReader, column);
        }

        public static string SqlConnectionInfo(string connectionString)
        {
            return GetSqlUtilityOrException().SqlConnectionInfo(connectionString);
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

        public static bool ScriptSupportsTransaction(string sql)
        {
            return GetSqlUtilityOrException().ScriptSupportsTransaction(sql);
        }

        public static DateTime GetDatabaseTime(ISqlExecuter sqlExecuter)
        {
            return GetSqlUtilityOrException().GetDatabaseTime(sqlExecuter);
        }

        /// <summary>
        /// Splits the script to multiple batches, separated by the GO command.
        /// It emulates the behavior of Microsoft SQL Server utilities, sqlcmd and osql,
        /// but it does not work perfectly: comments near GO, strings containing GO and the repeat count are currently not supported.
        /// </summary>
        public static string[] SplitBatches(string sql)
        {
            return GetSqlUtilityOrException().SplitBatches(sql);
        }
    }
}
