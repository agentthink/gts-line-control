using System.Data;

namespace GtsTest.Data
{
    /// <summary>
    /// 数据库连接工厂：根据当前 Provider 返回对应的 IDbConnection
    /// 上层仓储只依赖此接口，不关心底层是 SQLite / SqlServer / MySQL
    /// </summary>
    public interface IDbConnectionFactory
    {
        DatabaseProvider Provider { get; }

        /// <summary>创建并返回一个未打开的连接（由调用方负责 Open + Dispose）</summary>
        IDbConnection CreateConnection();
    }
}