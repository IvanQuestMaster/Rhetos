using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Rhetos.Utilities
{
    public class MsSqlUtility2 : ISqlUtility
    {
        private static readonly string[] _referenceConstraintTypes = new string[] { "REFERENCE", "SAME TABLE REFERENCE", "FOREIGN KEY", "COLUMN FOREIGN KEY" };

        private const string InsertingDuplicateIdMessage = "Inserting a record that already exists in database.";

        private readonly IConfiguration _configuration;

        private readonly IConnectionStringConfiguration _connectionStringConfiguration;

        int? _sqlCommandTimeout;

        public MsSqlUtility2(IConfiguration configuration, IConnectionStringConfiguration connectionStringConfiguration)
        {
            _configuration = configuration;
            _connectionStringConfiguration = connectionStringConfiguration;
        }

        public int SqlCommandTimeout
        {
            get
            {
                if (!_sqlCommandTimeout.HasValue)
                {
                    _sqlCommandTimeout = _configuration.GetInt("SqlCommandTimeout", 30).Value;
                }
                return _sqlCommandTimeout.Value;
            }
        }

        public string ProviderName
        {
            get
            {
                return "System.Data.SqlClient";
            }
        }

        public string BoolToString(bool? b)
        {
            return b.HasValue ? BoolToString(b.Value) : null;
        }

        public string BoolToString(bool b)
        {
            return b ? "0" : "1";
        }

        public string DateTimeToString(DateTime? dateTime)
        {
            return dateTime.HasValue ? DateTimeToString(dateTime.Value) : null;
        }

        public string DateTimeToString(DateTime dateTime)
        {
            return dateTime.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff");
        }

        public string EmptyNullString(DbDataReader dataReader, int column)
        {
            return dataReader.GetString(column) ?? "";
        }

        public Exception ExtractSqlException(Exception exception)
        {
            if (exception is SqlException)
                return (SqlException)exception;
            if (exception.InnerException != null)
                return ExtractSqlException(exception.InnerException);
            return null;
        }

        private class ReconstructedUserInfo : IUserInfo
        {
            public bool IsUserRecognized { get; set; }
            public string UserName { get; set; }
            public string Workstation { get; set; }
            public string Report() { return UserName + "," + Workstation; }
        }

        public IUserInfo ExtractUserInfo(string contextInfo)
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

        public DateTime GetDatabaseTime(ISqlExecuter sqlExecuter)
        {
            return DatabaseTimeCache.GetDatabaseTimeCached(() =>
            {
                DateTime databaseTime = DateTime.MinValue;
                sqlExecuter.ExecuteReader("SELECT GETDATE()",
                    reader => databaseTime = reader.GetDateTime(0));
                if (databaseTime == DateTime.MinValue)
                    throw new ApplicationException("Cannot read database server time.");
                return DateTime.SpecifyKind(databaseTime, DateTimeKind.Local);
            }, () => DateTime.Now);
        }

        public string GetFullName(string objectName)
        {
            var schema = GetSchemaName(objectName);
            var name = GetShortName(objectName);
            return schema + "." + name;
        }

        public string GetSchemaName(string fullObjectName)
        {
            int dotPosition = fullObjectName.IndexOf('.');
            if (dotPosition == -1)
                return "dbo";
            var schema = fullObjectName.Substring(0, dotPosition);
            return Identifier(schema);
        }

        public string GetShortName(string fullObjectName)
        {
            int dotPosition = fullObjectName.IndexOf('.');
            if (dotPosition == -1)
                return fullObjectName;

            var shortName = fullObjectName.Substring(dotPosition + 1);

            int secondDot = shortName.IndexOf('.');
            if (secondDot != -1 || string.IsNullOrEmpty(shortName))
                throw new FrameworkException("Invalid database object name: '" + fullObjectName + "'. Expected format is 'schema.name' or 'name'.");
            return Identifier(shortName);
        }

 

        public string GuidToString(Guid? guid)
        {
            return guid.HasValue ? GuidToString(guid.Value) : null;
        }

        public string GuidToString(Guid guid)
        {
            return guid.ToString().ToUpper();
        }

        public string Identifier(string name)
        {
            string error = CsUtility.GetIdentifierError(name);
            if (error != null)
                throw new FrameworkException("Invalid database object name: " + error);

            name = LimitIdentifierLength(name);

            return name;
        }

        public RhetosException InterpretSqlException(Exception exception)
        {
            if (exception == null || exception is RhetosException)
                return null;

            var sqlException = (SqlException)ExtractSqlException(exception);
            if (sqlException == null)
                return null;

            //=========================
            // Detect user message in SQL error:

            const int userErrorCode = 101; // Rhetos convention for an error raised in SQL that is intended as a message to the end user.

            if (sqlException.State == userErrorCode)
                return new UserException(sqlException.Message, exception);

            if (sqlException.Errors != null)
                foreach (var sqlError in sqlException.Errors.Cast<SqlError>().OrderBy(e => e.LineNumber))
                    if (sqlError.State == userErrorCode)
                        return new UserException(sqlError.Message, exception);

            //=========================
            // Detect system errors:

            if (sqlException.Number == 229 || sqlException.Number == 230)
                if (sqlException.Message.Contains("permission was denied"))
                    return new FrameworkException("Rhetos server lacks sufficient database permissions for this operation. Please make sure that Rhetos Server process has db_owner role for the database.", exception);

            //=========================
            // Detect UNIQUE constraint:

            if (sqlException.Number == 2601)
            {
                // See the InterpretUniqueConstraint unit test for regex coverage.
                Regex messageParser = new Regex(@"^Cannot insert duplicate key row in object '(.+)' with unique index '(.+)'\.( The duplicate key value is \((.+)\)\.)?");
                var parts = messageParser.Match(sqlException.Message).Groups;

                var interpretedException = new UserException("It is not allowed to enter a duplicate record.", exception);

                interpretedException.Info["Constraint"] = "Unique";
                if (parts[1].Success)
                    interpretedException.Info["Table"] = parts[1].Value;
                if (parts[2].Success)
                    interpretedException.Info["ConstraintName"] = parts[2].Value;
                if (parts[4].Success)
                    interpretedException.Info["DuplicateValue"] = parts[4].Value;

                return interpretedException;
            }

            //=========================
            // Detect REFERENCE constraint:

            if (sqlException.Number == 547)
            {
                // See the InterpretReferenceConstraint unit test for regex coverage.
                Regex messageParser = new Regex(@"^(The )?(.+) statement conflicted with (the )?(.+) constraint [""'](.+)[""']. The conflict occurred in database [""'](.+)[""'], table [""'](.+?)[""'](, column [""'](.+?)[""'])?");
                var parts = messageParser.Match(sqlException.Message).Groups;
                string action = parts[2].Value ?? "";
                string constraintType = parts[4].Value ?? "";

                if (_referenceConstraintTypes.Contains(constraintType))
                {
                    UserException interpretedException = null;
                    if (action == "DELETE")
                        interpretedException = new UserException("It is not allowed to delete a record that is referenced by other records.", new string[] { parts[7].Value, parts[9].Value }, null, exception);
                    else if (action == "INSERT")
                        interpretedException = new UserException("It is not allowed to enter the record. The entered value references nonexistent record.", new string[] { parts[7].Value, parts[9].Value }, null, exception);
                    else if (action == "UPDATE")
                        interpretedException = new UserException("It is not allowed to edit the record. The entered value references nonexistent record.", new string[] { parts[7].Value, parts[9].Value }, null, exception);

                    if (interpretedException != null)
                    {
                        interpretedException.Info["Constraint"] = "Reference";
                        interpretedException.Info["Action"] = action;
                        if (parts[5].Success)
                            interpretedException.Info["ConstraintName"] = parts[5].Value; // The FK constraint name is ambiguous: The error does not show the schema name and the base table that the INSERT or UPDATE actually happened.
                        if (parts[7].Success)
                            interpretedException.Info[action == "DELETE" ? "DependentTable" : "ReferencedTable"] = parts[7].Value;
                        if (parts[9].Success)
                            interpretedException.Info[action == "DELETE" ? "DependentColumn" : "ReferencedColumn"] = parts[9].Value;

                        return interpretedException;
                    }
                }
            }

            //=========================
            // Detect PRIMARY KEY constraint:

            if (sqlException.Number == 2627 && sqlException.Message.StartsWith("Violation of PRIMARY KEY constraint"))
            {
                Regex messageParser = new Regex(@"^Violation of PRIMARY KEY constraint '(.+)'\. Cannot insert duplicate key in object '(.+)'\.( The duplicate key value is \((.+)\)\.)?");
                var parts = messageParser.Match(sqlException.Message).Groups;

                var interpretedException = new FrameworkException(InsertingDuplicateIdMessage, exception);

                interpretedException.Info["Constraint"] = "Primary key";
                if (parts[1].Success)
                    interpretedException.Info["ConstraintName"] = parts[1].Value;
                if (parts[2].Success)
                    interpretedException.Info["Table"] = parts[2].Value;
                if (parts[4].Success)
                    interpretedException.Info["DuplicateValue"] = parts[4].Value;

                return interpretedException;
            }

            return null;
        }

        public bool IsReferenceErrorOnDelete(RhetosException interpretedException, string dependentTable, string dependentColumn, string constraintName)
        {
            if (interpretedException == null)
                return false;
            var info = interpretedException.Info;
            return
                info != null
                && (info.GetValueOrDefault("Constraint") as string) == "Reference"
                && (info.GetValueOrDefault("Action") as string) == "DELETE"
                && (info.GetValueOrDefault("DependentTable") as string) == dependentTable
                && (info.GetValueOrDefault("DependentColumn") as string) == dependentColumn
                && (info.GetValueOrDefault("ConstraintName") as string) == constraintName;
        }

        public bool IsReferenceErrorOnInsertUpdate(RhetosException interpretedException, string referencedTable, string referencedColumn, string constraintName)
        {
            if (interpretedException == null)
                return false;
            var info = interpretedException.Info;
            return
                info != null
                && (info.GetValueOrDefault("Constraint") as string) == "Reference"
                && ((info.GetValueOrDefault("Action") as string) == "INSERT" || (info.GetValueOrDefault("Action") as string) == "UPDATE")
                && (info.GetValueOrDefault("ReferencedTable") as string) == referencedTable
                && (info.GetValueOrDefault("ReferencedColumn") as string) == referencedColumn
                && (info.GetValueOrDefault("ConstraintName") as string) == constraintName;
        }

        public bool IsUniqueError(RhetosException interpretedException, string table, string constraintName)
        {
            if (interpretedException == null)
                return false;
            var info = interpretedException.Info;
            return
                info != null
                && (info.GetValueOrDefault("Constraint") as string) == "Unique"
                && (info.GetValueOrDefault("Table") as string) == table
                && (info.GetValueOrDefault("ConstraintName") as string) == constraintName;
        }

        public string LimitIdentifierLength(string name)
        {
            string error = CsUtility.GetIdentifierError(name);
            if (error != null)
                throw new FrameworkException("Invalid database object name: " + error);

            const int MaxLength = 128;
            if (name.Length > MaxLength)
            {
                var hashErasedPart = name.Substring(MaxLength - 9).GetHashCode().ToString("X");
                return name.Substring(0, MaxLength - 9) + "_" + hashErasedPart;
            }
            return name;
        }

        public string QuoteBool(bool? b)
        {
            return b.HasValue ? BoolToString(b.Value) : "NULL";
        }

        public string QuoteDateTime(DateTime? dateTime)
        {
            return dateTime.HasValue
                ? "'" + DateTimeToString(dateTime.Value) + "'"
                : "NULL";
        }

        public string QuoteGuid(Guid guid)
        {
            return "'" + GuidToString(guid) + "'";
        }

        public string QuoteGuid(Guid? guid)
        {
            return guid.HasValue
                ? "'" + GuidToString(guid.Value) + "'"
                : "NULL";
        }

        public string QuoteIdentifier(string sqlIdentifier)
        {
                sqlIdentifier = sqlIdentifier.Replace("]", "]]");
                return "[" + sqlIdentifier + "]";
        }

        public string QuoteText(string value)
        {
            return value != null
                ? "'" + value.Replace("'", "''") + "'"
                : "NULL";
        }

        public Guid ReadGuid(DbDataReader dataReader, int column)
        {
            return dataReader.GetGuid(column);
        }

        public int ReadInt(DbDataReader dataReader, int column)
        {
            return dataReader.GetInt32(column);
        }

        public string SetUserContextInfoQuery(IUserInfo userInfo)
        {
            string text = UserContextInfoText(userInfo);
            if (string.IsNullOrEmpty(text))
                return "";

            if (text.Length > 128)
                text = text.Substring(1, 128);

            var query = new StringBuilder(text.Length * 2 + 2);
            query.Append("SET CONTEXT_INFO 0x");
            foreach (char c in text)
            {
                int i = c;
                if (i > 255) i = '?';
                query.Append(i.ToString("x2"));
            }

            return query.ToString();
        }

        public Guid StringToGuid(string guid)
        {
            return Guid.Parse(guid);
        }

        public void ThrowIfPrimaryKeyErrorOnInsert(RhetosException interpretedException, string tableName)
        {
            if (interpretedException != null
                && interpretedException.Info != null
                && (interpretedException.Info.GetValueOrDefault("Constraint") as string) == "Primary key"
                && (interpretedException.Info.GetValueOrDefault("Table") as string) == tableName)
            {
                string pkValue = interpretedException.Info.GetValueOrDefault("DuplicateValue") as string;
                throw new ClientException(InsertingDuplicateIdMessage + (pkValue != null ? " ID=" + pkValue : ""));
            }
        }

        public string UserContextInfoText(IUserInfo userInfo)
        {
            if (!userInfo.IsUserRecognized)
                return "";

            return "Rhetos:" + userInfo.Report();
        }

        /// <summary>
        /// Splits the script to multiple batches, separated by the GO command.
        /// It emulates the behavior of Microsoft SQL Server utilities, sqlcmd and osql,
        /// but it does not work perfectly: comments near GO, strings containing GO and the repeat count are currently not supported.
        /// </summary>
        public string[] SplitBatches(string sql)
        {
            return batchSplitter.Split(sql).Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
        }

        private static readonly Regex batchSplitter = new Regex(@"^\s*GO\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
    }
}
