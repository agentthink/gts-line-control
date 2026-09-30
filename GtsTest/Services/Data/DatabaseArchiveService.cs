using GtsTest.Core;
using GtsTest.Data;
using GtsTest.Services.Logging; // 🆕 补上 ILogger 的命名空间
using Dapper;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GtsTest.Services.Data
{
    /// <summary>
    /// 历史数据归档服务：定期将旧数据迁移到 History 表，并清理主表
    /// </summary>
    public class DatabaseArchiveService : IDisposable
    {
        private readonly IDbConnectionFactory _factory;
        private readonly ILogger _logger;
        private CancellationTokenSource? _cts;
        private Task? _archiveTask;

        public DatabaseArchiveService(IDbConnectionFactory factory, ILogger logger)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _logger = logger ?? new AppLoggerWrapper();
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            _archiveTask = Task.Run(() => ArchiveLoopAsync(_cts.Token));
            _logger.Info("✅ 数据库归档服务已启动 (保留最近3个月数据)", "Archive");
        }

        private async Task ArchiveLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    //await Task.Delay(TimeSpan.FromHours(24), ct);
                    await Task.Delay(TimeSpan.FromSeconds(30), ct);  // 测试用

                    _logger.Info("开始执行历史数据归档...", "Archive");
                    using var conn = _factory.CreateConnection();
                    conn.Open();

                    var columns = $@"Id {SqlDialect.AutoIncrementBigIntPk}, DeviceId {SqlDialect.Text}, Timestamp {SqlDialect.Text}, CurrentCount {SqlDialect.Int}, TargetCount {SqlDialect.Int}";
                    conn.Execute(SqlDialect.CreateTableIfNotExists("ProductionRecords_History", columns));

                    var cutoffDate = DateTime.Now.AddMonths(-3).ToString("o");

                    var affected = conn.Execute(@"
                        INSERT INTO ProductionRecords_History (DeviceId, Timestamp, CurrentCount, TargetCount)
                        SELECT DeviceId, Timestamp, CurrentCount, TargetCount 
                        FROM ProductionRecords 
                        WHERE Timestamp < @Cutoff", new { Cutoff = cutoffDate });

                    conn.Execute("DELETE FROM ProductionRecords WHERE Timestamp < @Cutoff", new { Cutoff = cutoffDate });

                    _logger.Info($"归档完成，迁移了 {affected} 条历史记录", "Archive");
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.Error($"归档异常: {ex.Message}", "Archive");
                }
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            try { _archiveTask?.Wait(1000); } catch { }
            _cts?.Dispose();
        }
    }
}