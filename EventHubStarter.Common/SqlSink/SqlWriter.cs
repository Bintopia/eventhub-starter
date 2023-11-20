

using System.Data;
using System.Text;

using EventHubStarter.Common.Events;

using Microsoft.Data.SqlClient;

namespace EventHubStarter.Common.SqlSink
{
    public class SqlWriter(string connection)
    {
        //const string QUERY_TABLE = "SELECT TABLE_CATALOG, TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA=N'production' AND TABLE_NAME = N'product'";

        const string QUERY_TABLE_COLUMNS = @"USE AdventureworksReplicate;
                                             SELECT
                                                C.NAME 'COLUMN NAME',
                                                T.NAME 'DATA TYPE',
                                                C.MAX_LENGTH 'MAX LENGTH',
                                                C.IS_NULLABLE 'IS NULLABLE',
                                                ISNULL(I.IS_PRIMARY_KEY, 0) 'PRIMARY KEY'
                                            FROM
                                                SYS.COLUMNS C
                                            INNER JOIN
                                                SYS.TYPES T ON C.USER_TYPE_ID = T.USER_TYPE_ID
                                            LEFT OUTER JOIN
                                                SYS.INDEX_COLUMNS IC ON IC.OBJECT_ID = C.OBJECT_ID AND IC.COLUMN_ID = C.COLUMN_ID
                                            LEFT OUTER JOIN
                                                SYS.INDEXES I ON IC.OBJECT_ID = I.OBJECT_ID AND IC.INDEX_ID = I.INDEX_ID
                                            WHERE
                                                C.OBJECT_ID = OBJECT_ID('[production].[product]')";

        private readonly string _connectionString = connection;

        private readonly static Dictionary<string, SqlColumnMetadata> PrimaryKeys = [];

        public async Task<int> WriteAsync(RecordChangedEvent recordChanged)
        {
            Console.WriteLine("MSSQL Writer: write data...");

            var query = string.Empty;

            var primaryKey = await GetPrimaryKey(recordChanged);

            switch (recordChanged.Action.ToUpper())
            {
                case "INSERT":
                    query = BuildInsertQuery(recordChanged.Table, [.. recordChanged.Data.Keys], recordChanged.Schema);
                    break;
                case "UPDATE":
                    var keys = recordChanged.Data.Keys.Where(k => !k.Equals(primaryKey.ColumnName, StringComparison.OrdinalIgnoreCase)).ToList();
                    query = BuildUpdateQuery(recordChanged.Table, primaryKey, keys, recordChanged.Schema);
                    break;
                case "DELETE":
                    query = BuildDeleteQuery(recordChanged.Table, primaryKey, recordChanged.Schema);
                    break;
                default:
                    throw new Exception($"Unsupported SQL query action '{recordChanged.Action}'");
            }

            var connection = new SqlConnection(_connectionString);
            var command = BuildSqlCommand(query, connection, recordChanged.Data);
            await connection.OpenAsync();
            var affectedRows = await command.ExecuteNonQueryAsync();

            Console.WriteLine($"MSSQL Writer: Write completed, {affectedRows} row(s) affected.");

            return affectedRows;
        }

        private async Task<SqlColumnMetadata> GetPrimaryKey(RecordChangedEvent recordChanged)
        {
            var indexKeyName = $"{recordChanged.Schema}_{recordChanged.Table}";
            if (!PrimaryKeys.TryGetValue(indexKeyName, out SqlColumnMetadata? primaryKey))
            {
                var columns = await GetTableColumns(recordChanged.Table, recordChanged.Schema);
                var pk = columns.FirstOrDefault(x => x.IsPrimaryKey) ?? throw new Exception($"Unable to find the primary key for table [recordChanged.Schema].[recordChanged.Table]");
                primaryKey = pk;
                PrimaryKeys.Add(indexKeyName, primaryKey);
            }

            return primaryKey;
        }

        public async Task<List<SqlColumnMetadata>> GetTableColumns(string table, string schema = "dbo")
        {
            using var connection = new SqlConnection(_connectionString);
            var command = new SqlCommand(QUERY_TABLE_COLUMNS, connection);
            //command.Parameters.AddWithValue("@schemaName", schema);
            //command.Parameters.AddWithValue("@tableName", table);

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();

            var columns = new List<SqlColumnMetadata>();

            if (reader != null && reader.HasRows)
            {
                while (reader.Read())
                {
                    var metadata = new SqlColumnMetadata();
                    metadata.ColumnName = reader.GetString("COLUMN NAME");
                    metadata.DataType = reader.GetString("DATA TYPE");
                    metadata.MaxLength = reader.GetInt16("MAX LENGTH");
                    metadata.IsNullable = reader.GetBoolean("IS NULLABLE");
                    metadata.IsPrimaryKey = reader.GetBoolean("PRIMARY KEY");
                    columns.Add(metadata);
                }
            }

            return columns;
        }

        private static SqlCommand BuildSqlCommand(string query, SqlConnection connection, Dictionary<string, object> data)
        {
            var command = new SqlCommand(query, connection);

            foreach (var item in data)
            {
                if (item.Value == null)
                {
                    command.Parameters.AddWithValue($"@{item.Key}", DBNull.Value);
                }
                else
                {
                    command.Parameters.AddWithValue($"@{item.Key}", item.Value);
                }
            }

            return command;
        }

        private static string BuildDeleteQuery(string table, SqlColumnMetadata primaryKey, string schema = "dbo")
        {
            var builder = new StringBuilder();
            builder.AppendFormat("DELETE FROM [0].[1]", schema, table);

            if (primaryKey.DataType.Contains("char", StringComparison.OrdinalIgnoreCase))
            {
                builder.AppendFormat(" WHERE [{0}]='@{0}'", primaryKey.ColumnName);
            }
            else
            {
                builder.AppendFormat(" WHERE [{0}]=@{0}", primaryKey.ColumnName);
            }

            return builder.ToString();
        }

        private static string BuildInsertQuery(string table, List<string> keys, string schema = "dbo")
        {
            var builder = new StringBuilder();
            builder.AppendFormat("INSERT INTO [{0}].[{1}] (", schema, table);

            var valuesStatementBuilder = new StringBuilder("VALUES(");

            foreach (var key in keys)
            {
                builder.AppendFormat("[{0}], ", key);
                valuesStatementBuilder.AppendFormat("@{0},", key);
            }

            builder.Append(')');
            valuesStatementBuilder.Append(')');

            builder.Append(valuesStatementBuilder);

            return builder.ToString();
        }

        private static string BuildUpdateQuery(string table, SqlColumnMetadata primaryKey, List<string> keys, string schema = "dbo")
        {
            var builder = new StringBuilder();
            builder.AppendFormat("UPDATE [{0}].[{1}] SET [{2}]=@{2}", schema, table, keys[0]);

            for (var i = 1; i < keys.Count - 1; i++)
            {
                builder.AppendFormat(", [{0}]=@{0}", keys[i]);
            }

            if (primaryKey.DataType.Contains("char", StringComparison.OrdinalIgnoreCase))
            {
                builder.AppendFormat(" WHERE [{0}]='@{0}'", primaryKey.ColumnName);
            }
            else
            {
                builder.AppendFormat(" WHERE [{0}]=@{0}", primaryKey.ColumnName);
            }

            return builder.ToString();
        }
    }
}
