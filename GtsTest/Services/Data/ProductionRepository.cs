using Dapper;
using GtsTest.Data;
using GtsTest.Services.Logging;
using System;
using System.Data;

namespace GtsTest.Services.Data
{
    /// <summary>生产记录仓储统一实现（基于 Dapper）</summary>
    public class ProductionRepository : IProductionRepository
    {
        private readonly IDbConnectionFactory _factory;
        private readonly ILogger _logger;
        private bool _tableEnsured;
        private readonly object _initLock = new object();

        public ProductionRepository(IDbConnectionFactory factory, ILogger logger)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _logger = logger ?? new AppLoggerWrapper();
        }

        public void EnsureTable()
        {
            if (_tableEnsured) return;
            lock (_initLock)
            {
                if (_tableEnsured) return;
                try
                {
                    using var conn = _factory.CreateConnection();
                    conn.Open();

                    var columns = $@"
                        Id {SqlDialect.AutoIncrementBigIntPk},
                        DeviceId {SqlDialect.Text} NULL,
                        Timestamp {SqlDialect.Text} NULL,
                        CurrentCount {SqlDialect.Int} NULL,
                        TargetCount {SqlDialect.Int} NULL";

                    conn.Execute(SqlDialect.CreateTableIfNotExists("ProductionRecords", columns));
                    TryCreateIndex(conn, "idx_prod_device", "ProductionRecords", "DeviceId");
                    TryCreateIndex(conn, "idx_prod_time", "ProductionRecords", "Timestamp DESC");

                    _tableEnsured = true;
                    _logger.Info($"✅ ProductionRecords 表已就绪 ({SqlDialect.Provider})", "ProdRepo");
                }
                catch (Exception ex)
                {
                    _logger.Error($"❌ 初始化 ProductionRecords 表失败: {ex.Message}", "ProdRepo");
                    throw;
                }
            }
        }

        private void TryCreateIndex(IDbConnection conn, string indexName, string tableName, string columns)
        {
            try { conn.Execute(SqlDialect.CreateIndexIfNotExists(indexName, tableName, columns)); }
            catch { }
        }

        public void Insert(string deviceId, int currentCount, int targetCount, DateTime timestamp)
        {
            EnsureTable();
            using var conn = _factory.CreateConnection();
            conn.Open();

            conn.Execute(@"
                INSERT INTO ProductionRecords (DeviceId, Timestamp, CurrentCount, TargetCount)
                VALUES (@DeviceId, @Timestamp, @CurrentCount, @TargetCount);", new
            {
                DeviceId = deviceId ?? "",
                Timestamp = timestamp.ToString("o"),
                CurrentCount = currentCount,
                TargetCount = targetCount
            });
        }
    }
}