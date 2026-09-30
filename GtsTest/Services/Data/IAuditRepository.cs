using System;
using System.Collections.Generic;

namespace GtsTest.Services.Data
{
    /// <summary>审计数据仓储接口</summary>
    public interface IAuditRepository
    {
        void EnsureTable();
        void Insert(long userId, string username, string actionType, string detail, DateTime timestamp);
        List<AuditLog> QueryRecent(int limit);
    }
}