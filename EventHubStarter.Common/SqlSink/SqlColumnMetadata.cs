namespace EventHubStarter.Common.SqlSink
{
    public class SqlColumnMetadata
    {
        public string ColumnName { get; set; } = string.Empty;

        public string DataType { get; set; } = string.Empty;

        public int MaxLength { get; set; }

        public bool IsNullable { get; set; }

        public bool IsPrimaryKey { get; set; }
    }
}
