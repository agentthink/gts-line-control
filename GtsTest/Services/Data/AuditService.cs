using GtsTest.Services.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GtsTest.Services.Data
{
    /// <summary>
    /// 【静态兼容层】保留原有的静态调用入口（AuditService.Log(...) / AuditService.GetRecent(...)）
    /// 内部委托到 IAuditService 实例，默认指向 SQLite 实现。
    /// 应用启动时通过 SetDefault() 注入 DI 实例。
    ///
    /// 迁移期结束后可删除此类，把调用点全部改为注入 IAuditService。
    /// </summary>
    public static class AuditService
    {
        private static IAuditService _default = new AuditServiceImpl();

        /// <summary>
        /// 兼容旧代码的内嵌 DTO（字段与 AuditLog 完全一致）
        /// 保留它是为了让老调用点 AuditService.AuditRecord 依旧可用
        /// </summary>
        public class AuditRecord : AuditLog { }

        /// <summary>
        /// 设置默认实例（由 DI 容器装配时调用）
        /// </summary>
        public static void SetDefault(IAuditService service)
            => _default = service ?? throw new ArgumentNullException(nameof(service));

        /// <summary>写一条审计记录（非阻塞，fire-and-forget）</summary>
        public static void Log(long userId, string username, string actionType, string detail, IDataRepository? repo = null)
            => _default.Log(userId, username, actionType, detail, repo);

        /// <summary>异步写入（调用方希望等待完成时可使用）</summary>
        public static Task LogAsync(long userId, string username, string actionType, string detail, IDataRepository? repo = null)
            => _default.LogAsync(userId, username, actionType, detail, repo);

        /// <summary>读取最近的审计记录（按时间降序）</summary>
        public static List<AuditRecord> GetRecent(int limit = 200)
        {
            var logs = _default.GetRecent(limit);
            var result = new List<AuditRecord>(logs.Count);
            foreach (var log in logs)
            {
                result.Add(new AuditRecord
                {
                    Id = log.Id,
                    UserId = log.UserId,
                    Username = log.Username,
                    ActionType = log.ActionType,
                    Detail = log.Detail,
                    Timestamp = log.Timestamp
                });
            }
            return result;
        }
    }
}