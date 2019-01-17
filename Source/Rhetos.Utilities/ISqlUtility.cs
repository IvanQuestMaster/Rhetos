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
using System.Data.Common;

namespace Rhetos.Utilities
{
    public interface ISqlUtility
    {
        /// <summary>
        /// Checks the exception for database errors and attempts to transform it to a RhetosException.
        /// It the function returns null, the original exception should be used.
        /// </summary>
        RhetosException InterpretSqlException(Exception exception);

        /// <summary>
        /// Simplifies ORM exception by detecting the SQL exception that caused it.
        /// </summary>
        Exception ExtractSqlException(Exception exception);

        string ProviderName { get; }

        string UserContextInfoText(IUserInfo userInfo);

        IUserInfo ExtractUserInfo(string contextInfo);

        /// <summary>
        /// Throws an exception if 'name' is not a valid SQL database object name.
        /// Function returns given argument so it can be used as fluent interface.
        /// In some cases the function may change the identifier (limit identifier length to 30 on Oracle database, e.g.).
        /// </summary>
        string Identifier(string name);

        string QuoteText(string value);

        string QuoteIdentifier(string sqlIdentifier);

        string GetSchemaName(string fullObjectName);

        string GetShortName(string fullObjectName);

        string GetFullName(string objectName);

        /// <summary>
        /// Vendor-independent database reader.
        /// </summary>
        Guid ReadGuid(DbDataReader dataReader, int column);

        /// <summary>
        /// Vendor-independent database reader.
        /// </summary>
        int ReadInt(DbDataReader dataReader, int column);

        /// <summary>
        /// Returns empty string if the string value is null.
        /// This function is used for compatibility between MsSql and Oracle string behavior.
        /// </summary>
        string EmptyNullString(DbDataReader dataReader, int column);

        Guid StringToGuid(string guid);

        string QuoteGuid(Guid guid);

        string QuoteGuid(Guid? guid);

        string GuidToString(Guid? guid);

        string GuidToString(Guid guid);

        string QuoteDateTime(DateTime? dateTime);

        string DateTimeToString(DateTime? dateTime);

        string DateTimeToString(DateTime dateTime);

        string QuoteBool(bool? b);

        string BoolToString(bool? b);

        string BoolToString(bool b);

        DateTime GetDatabaseTime(ISqlExecuter sqlExecuter);

        /// <summary>
        /// Creates an SQL query that sets context_info connection variable to contain data about the user.
        /// The context_info variable can be used in SQL server to extract user info in certain situations such as logging trigger.
        /// </summary>
        string SetUserContextInfoQuery(IUserInfo userInfo);

        string LimitIdentifierLength(string name);

        bool IsUniqueError(RhetosException interpretedException, string table, string constraintName);

        bool IsReferenceErrorOnInsertUpdate(RhetosException interpretedException, string referencedTable, string referencedColumn, string constraintName);

        bool IsReferenceErrorOnDelete(RhetosException interpretedException, string dependentTable, string dependentColumn, string constraintName);

        void ThrowIfPrimaryKeyErrorOnInsert(RhetosException interpretedException, string tableName);

        string[] SplitBatches(string sql);
    }
}
