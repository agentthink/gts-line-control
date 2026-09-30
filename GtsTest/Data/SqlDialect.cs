namespace GtsTest.Data
{
    /// <summary>
    /// 跨数据库 SQL 语法工具：
    /// 根据 DbContextFactory.CurrentProvider 生成 Sqlite / SqlServer / MySql 各自方言的 SQL
    /// </summary>
    public static class SqlDialect
    {
        public static DatabaseProvider Provider => DbContextFactory.CurrentProvider;

        // ---------- 建表 ----------
        public static string CreateTableIfNotExists(string tableName, string columnsSql)
        {
            switch (Provider)
            {
                case DatabaseProvider.SqlServer:
                    return $@"
                        IF OBJECT_ID('{tableName}', 'U') IS NULL
                        BEGIN
                            CREATE TABLE {tableName} ({columnsSql});
                        END";
                case DatabaseProvider.MySql:
                default:
                    return $"CREATE TABLE IF NOT EXISTS {tableName} ({columnsSql});";
            }
        }

        // ---------- 建索引 ----------
        public static string CreateIndexIfNotExists(string indexName, string tableName, string columnsSql)
        {
            switch (Provider)
            {
                case DatabaseProvider.SqlServer:
                    return $@"
                        IF NOT EXISTS (SELECT * FROM sys.indexes 
                                       WHERE name = '{indexName}' 
                                         AND object_id = OBJECT_ID('{tableName}'))
                        BEGIN
                            CREATE INDEX {indexName} ON {tableName} ({columnsSql});
                        END";
                case DatabaseProvider.MySql:
                    return $"CREATE INDEX {indexName} ON {tableName} ({columnsSql});";
                default:
                    return $"CREATE INDEX IF NOT EXISTS {indexName} ON {tableName} ({columnsSql});";
            }
        }

        // ---------- 🆕 表补列（老库升级）----------
        /// <summary>
        /// 生成 "如果列不存在则新增列" 语句。
        /// SQLite / MySQL 没有 IF NOT EXISTS，调用方需 try-catch。
        /// </summary>
        public static string AlterTableAddColumnIfNotExists(string tableName, string columnName, string columnType)
        {
            switch (Provider)
            {
                case DatabaseProvider.SqlServer:
                    return $@"
                        IF NOT EXISTS (SELECT * FROM sys.columns 
                                       WHERE Name = '{columnName}' 
                                         AND Object_ID = OBJECT_ID('{tableName}'))
                        BEGIN
                            ALTER TABLE {tableName} ADD {columnName} {columnType};
                        END";
                case DatabaseProvider.MySql:
                    return $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnType};";
                default:
                    return $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnType};";
            }
        }

        // ---------- 类型 ----------
        public static string AutoIncrementIntPk => Provider switch
        {
            DatabaseProvider.SqlServer => "INT IDENTITY(1,1) PRIMARY KEY",
            DatabaseProvider.MySql => "INT AUTO_INCREMENT PRIMARY KEY",
            _ => "INTEGER PRIMARY KEY AUTOINCREMENT"
        };

        public static string AutoIncrementBigIntPk => Provider switch
        {
            DatabaseProvider.SqlServer => "BIGINT IDENTITY(1,1) PRIMARY KEY",
            DatabaseProvider.MySql => "BIGINT AUTO_INCREMENT PRIMARY KEY",
            _ => "INTEGER PRIMARY KEY AUTOINCREMENT"
        };

        public static string Int => "INT";
        public static string BigInt => "BIGINT";

        public static string Text => Provider switch
        {
            DatabaseProvider.SqlServer => "NVARCHAR(4000)",
            DatabaseProvider.MySql => "VARCHAR(4000)",
            _ => "TEXT"
        };

        public static string LongText => Provider switch
        {
            DatabaseProvider.SqlServer => "NVARCHAR(MAX)",
            DatabaseProvider.MySql => "LONGTEXT",
            _ => "TEXT"
        };

        public static string Real => Provider switch
        {
            DatabaseProvider.SqlServer => "FLOAT",
            DatabaseProvider.MySql => "DOUBLE",
            _ => "REAL"
        };

        public static string Bool => "INT";

        // ---------- 取最后插入 Id ----------
        public static string LastInsertId => Provider switch
        {
            DatabaseProvider.SqlServer => "SELECT CAST(SCOPE_IDENTITY() AS BIGINT)",
            DatabaseProvider.MySql => "SELECT LAST_INSERT_ID()",
            _ => "SELECT last_insert_rowid()"
        };
    }
}