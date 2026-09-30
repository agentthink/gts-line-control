using Microsoft.Data.Sqlite;
using System;
using System.Data;

namespace GtsTest.Data
{
    /// <summary>
    /// IDbConnectionFactory 的默认实现
    /// </summary>
    public class DbConnectionFactory : IDbConnectionFactory
    {
        public DatabaseProvider Provider => DbContextFactory.CurrentProvider;

        public IDbConnection CreateConnection()
        {
            var connStr = DbContextFactory.CurrentConnectionString;

            switch (Provider)
            {
                case DatabaseProvider.SqlServer:
                    return new Microsoft.Data.SqlClient.SqlConnection(connStr);

                case DatabaseProvider.MySql:
                    // 若将来要启用 MySQL：NuGet 引入 MySqlConnector，改为：
                    //   return new MySqlConnector.MySqlConnection(connStr);
                    throw new NotSupportedException(
                        "MySQL provider 尚未启用。请在 GtsTest.csproj 中引入 MySqlConnector 包后启用。");

                case DatabaseProvider.Sqlite:
                default:
                    return new SqliteConnection(connStr);
            }
        }
    }
}