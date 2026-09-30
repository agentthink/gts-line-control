using System.Collections.Generic;
using System.Threading.Tasks;

namespace GtsTest.Services.Data
{
    /// <summary>
    /// 审计服务接口（数据访问抽象层）
    /// 未来可切换 Sqlite / SqlServer / MySQL 实现
    /// </summary>
    public interface IAuditService
    {
        void Log(long userId, string username, string actionType, string detail, IDataRepository? repo = null);
        Task LogAsync(long userId, string username, string actionType, string detail, IDataRepository? repo = null);
        List<AuditLog> GetRecent(int limit = 200);
    }
}