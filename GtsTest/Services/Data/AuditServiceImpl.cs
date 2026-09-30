using GtsTest.Core;
using GtsTest.Data;
using GtsTest.Services.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GtsTest.Services.Data
{
    /// <summary>
    /// 审计服务实例实现：通过 IAuditRepository 屏蔽数据库差异
    /// </summary>
    public class AuditServiceImpl : IAuditService
    {
        private readonly IAuditRepository _repository;
        private readonly ILogger _logger;

        /// <summary>无参兜底构造（静态兼容层 / 单元测试使用，内部自组装依赖）</summary>
        public AuditServiceImpl()
            : this(new AuditRepository(new DbConnectionFactory(), new AppLoggerWrapper()),
                   new AppLoggerWrapper())
        { }

        /// <summary>DI 注入构造</summary>
        public AuditServiceImpl(IAuditRepository repository, ILogger logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? new AppLoggerWrapper();
        }

        public void Log(long userId, string username, string actionType, string detail, IDataRepository? repo = null)
        {
            Task.Run(() =>
            {
                try
                {
                    _repository.Insert(userId, username, actionType, detail, DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    try { _logger.Warn($"无法写入审计日志: {ex.Message}", "AuditService"); } catch { }
                }
            });
        }

        public async Task LogAsync(long userId, string username, string actionType, string detail, IDataRepository? repo = null)
        {
            try
            {
                await Task.Run(() => _repository.Insert(userId, username, actionType, detail, DateTime.UtcNow))
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                try { _logger.Warn($"无法写入审计日志(Async): {ex.Message}", "AuditService"); } catch { }
            }
        }

        public List<AuditLog> GetRecent(int limit = 200)
        {
            try
            {
                return _repository.QueryRecent(limit);
            }
            catch (Exception ex)
            {
                try { _logger.Warn($"读取审计日志失败: {ex.Message}", "AuditService"); } catch { }
                return new List<AuditLog>();
            }
        }
    }
}