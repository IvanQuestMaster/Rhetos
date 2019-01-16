using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Globalization;

namespace Rhetos.Utilities
{
    public class ManifestTokenProvider
    {
        const string TokenSql8 = "2000";
        const string TokenSql9 = "2005";
        const string TokenSql10 = "2008";
        const string TokenSql11 = "2012";
        const string TokenAzure11 = "2012.Azure";

        enum SqlVersion
        {
            /// <summary>
            ///     SQL Server 8 (2000).
            /// </summary>
            Sql8 = 80,

            /// <summary>
            ///     SQL Server 9 (2005).
            /// </summary>
            Sql9 = 90,

            /// <summary>
            ///     SQL Server 10 (2008).
            /// </summary>
            Sql10 = 100,

            /// <summary>
            ///     SQL Server 11 (2012).
            /// </summary>
            Sql11 = 110,

            // Higher versions go here
        }

        enum ServerType
        {
            OnPremises,
            Cloud,
        }

        ConnectionString _connectionString;

        public ManifestTokenProvider(ConnectionString connectionString)
        {
            _connectionString = connectionString;
        }

        public string GetProviderManifestToken()
        {
            using (SqlConnection sc = new SqlConnection(_connectionString))
            {
                sc.Open();
                string providerManifestToken = QueryForManifestToken(sc);
                sc.Close();
                return providerManifestToken;
            }
        }

        static SqlVersion GetSqlVersion(DbConnection connection)
        {
            var majorVersion = Int32.Parse(connection.ServerVersion.Substring(0, 2), CultureInfo.InvariantCulture);

            if (majorVersion >= 11)
            {
                return SqlVersion.Sql11;
            }

            if (majorVersion == 10)
            {
                return SqlVersion.Sql10;
            }

            if (majorVersion == 9)
            {
                return SqlVersion.Sql9;
            }
            return SqlVersion.Sql8;
        }

        static ServerType GetServerType(DbConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = "SELECT serverproperty('EngineEdition')";
            var reader = command.ExecuteReader();
            int engineEdition = 0;
            while (reader.Read())
            {
                engineEdition = reader.GetInt32(0);
            }
            const int sqlAzureEngineEdition = 5;
            return engineEdition == sqlAzureEngineEdition ? ServerType.Cloud : ServerType.OnPremises;
        }

        static string GetVersionHint(SqlVersion version, ServerType serverType)
        {
            if (serverType == ServerType.Cloud)
            {
                return TokenAzure11;
            }

            switch (version)
            {
                case SqlVersion.Sql8:
                    return TokenSql8;

                case SqlVersion.Sql9:
                    return TokenSql9;

                case SqlVersion.Sql10:
                    return TokenSql10;

                case SqlVersion.Sql11:
                    return TokenSql11;

                default:
                    throw new ArgumentException("Could not determine storage version; a valid storage connection or a version hint is required.");
            }
        }

        string QueryForManifestToken(DbConnection conn)
        {
            var sqlVersion = GetSqlVersion(conn);
            var serverType = sqlVersion >= SqlVersion.Sql11 ? GetServerType(conn) : ServerType.OnPremises;
            return GetVersionHint(sqlVersion, serverType);
        }
    }
}
