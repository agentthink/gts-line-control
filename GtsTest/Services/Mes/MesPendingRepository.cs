using Dapper;
using GtsTest.Data;
using GtsTest.Services.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace GtsTest.Services.Mes
{
    /// <summary>MES 待重传缓存仓储统一实现（基于 Dapper）</summary>
    public class MesPendingRepository : IMesPendingRepository
    {
        private readonly IDbConnectionFactory _factory;
        private readonly ILogger _logger;
        private bool _tableEnsured;
        private readonly object _initLock = new object();

        public MesPendingRepository(IDbConnectionFactory factory, ILogger logger)
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

                    // 1. 建表（新库直接包含所有列）
                    var columns = $@"
                        Id {SqlDialect.AutoIncrementBigIntPk},
                        Barcode {SqlDialect.Text} NULL,
                        DeviceId {SqlDialect.Text} NULL,
                        Station {SqlDialect.Text} NULL,
                        Result {SqlDialect.Text} NULL,
                        DiameterMm {SqlDialect.Real} NULL,
                        X {SqlDialect.Real} NULL,
                        Y {SqlDialect.Real} NULL,
                        ImagePath {SqlDialect.Text} NULL,
                        Timestamp {SqlDialect.Text} NULL,
                        RetryCount {SqlDialect.Int} NOT NULL DEFAULT 0,
                        FailReason {SqlDialect.Text} NULL,
                        NextRetryTime {SqlDialect.Text} NULL";

                    conn.Execute(SqlDialect.CreateTableIfNotExists("MesPendingRecords", columns));

                    // 2. 老库升级：尝试补列（各 provider 用各自方言）
                    TryAddColumn(conn, "MesPendingRecords", "Station", SqlDialect.Text);
                    TryAddColumn(conn, "MesPendingRecords", "RetryCount", $"{SqlDialect.Int} NOT NULL DEFAULT 0");
                    TryAddColumn(conn, "MesPendingRecords", "FailReason", SqlDialect.Text);
                    TryAddColumn(conn, "MesPendingRecords", "NextRetryTime", SqlDialect.Text);

                    // 3. 建索引（必须放在补列之后，否则缺列会报错）
                    TryCreateIndex(conn, "idx_mes_barcode", "MesPendingRecords", "Barcode");
                    TryCreateIndex(conn, "idx_mes_retry", "MesPendingRecords", "NextRetryTime");

                    _tableEnsured = true;
                    _logger.Info($"✅ MesPendingRecords 表已就绪 ({SqlDialect.Provider})", "MesRepo");
                }
                catch (Exception ex)
                {
                    _logger.Error($"❌ 初始化 MesPendingRecords 表失败: {ex.Message}", "MesRepo");
                    throw;
                }
            }
        }

        private void TryAddColumn(IDbConnection conn, string tableName, string columnName, string columnType)
        {
            try
            {
                conn.Execute(SqlDialect.AlterTableAddColumnIfNotExists(tableName, columnName, columnType));
            }
            catch
            {
                // SQLite / MySQL 列已存在时抛错，忽略
            }
        }

        private void TryCreateIndex(IDbConnection conn, string indexName, string tableName, string columns)
        {
            try { conn.Execute(SqlDialect.CreateIndexIfNotExists(indexName, tableName, columns)); }
            catch { }
        }

        public long Insert(MesReportData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            EnsureTable();

            using var conn = _factory.CreateConnection();
            conn.Open();

            conn.Execute(@"
                INSERT INTO MesPendingRecords
                (Barcode, DeviceId, Station, Result, DiameterMm, X, Y, ImagePath, Timestamp,
                 RetryCount, FailReason, NextRetryTime)
                VALUES
                (@Barcode, @DeviceId, @Station, @Result, @DiameterMm, @X, @Y, @ImagePath, @Timestamp,
                 @RetryCount, @FailReason, @NextRetryTime);", new
            {
                Barcode = (object?)data.Barcode ?? DBNull.Value,
                DeviceId = (object?)data.DeviceId ?? DBNull.Value,
                Station = (object?)data.Station ?? DBNull.Value,
                Result = (object?)data.Result ?? DBNull.Value,
                DiameterMm = data.DiameterMm,
                X = data.X,
                Y = data.Y,
                ImagePath = (object?)data.ImagePath ?? DBNull.Value,
                Timestamp = data.Timestamp.ToString("o"),
                RetryCount = data.RetryCount,
                FailReason = (object?)data.FailReason ?? DBNull.Value,
                NextRetryTime = (object?)data.NextRetryTime ?? DBNull.Value
            });

            return conn.ExecuteScalar<long>(SqlDialect.LastInsertId);
        }

        public List<MesReportData> QueryForRetry(int limit)
        {
            EnsureTable();
            using var conn = _factory.CreateConnection();
            conn.Open();
            var rows = conn.Query<MesPendingRowDto>(@"
                SELECT Id, Barcode, DeviceId, Station, Result, DiameterMm, X, Y, ImagePath, Timestamp,
                       RetryCount, FailReason, NextRetryTime
                FROM MesPendingRecords
                ORDER BY Id ASC
                LIMIT @Limit;", new { Limit = limit }).ToList();

            return rows.Select(MapDto).ToList();
        }

        public void UpdateRetry(MesReportData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            using var conn = _factory.CreateConnection();
            conn.Open();
            conn.Execute(@"
                UPDATE MesPendingRecords
                SET RetryCount = @RetryCount,
                    FailReason = @FailReason,
                    NextRetryTime = @NextRetryTime
                WHERE Id = @Id;", new
            {
                Id = data.Id,
                RetryCount = data.RetryCount,
                FailReason = (object?)data.FailReason ?? DBNull.Value,
                NextRetryTime = (object?)data.NextRetryTime ?? DBNull.Value
            });
        }

        public void Delete(long id)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            conn.Execute("DELETE FROM MesPendingRecords WHERE Id = @Id;", new { Id = id });
        }

        public int Count()
        {
            try
            {
                EnsureTable();
                using var conn = _factory.CreateConnection();
                conn.Open();
                return conn.ExecuteScalar<int>("SELECT COUNT(*) FROM MesPendingRecords;");
            }
            catch { return 0; }
        }

        private class MesPendingRowDto
        {
            public long Id { get; set; }
            public string? Barcode { get; set; }
            public string? DeviceId { get; set; }
            public string? Station { get; set; }
            public string? Result { get; set; }
            public double DiameterMm { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public string? ImagePath { get; set; }
            public string? Timestamp { get; set; }
            public int RetryCount { get; set; }
            public string? FailReason { get; set; }
            public string? NextRetryTime { get; set; }
        }

        private static MesReportData MapDto(MesPendingRowDto row)
        {
            return new MesReportData
            {
                Id = row.Id,
                Barcode = row.Barcode ?? "",
                DeviceId = row.DeviceId ?? "",
                Station = row.Station ?? "",
                Result = row.Result ?? "",
                DiameterMm = row.DiameterMm,
                X = row.X,
                Y = row.Y,
                ImagePath = row.ImagePath,
                Timestamp = DateTime.TryParse(row.Timestamp, out var dt) ? dt : DateTime.Now,
                RetryCount = row.RetryCount,
                FailReason = row.FailReason,
                NextRetryTime = row.NextRetryTime
            };
        }
    }
}