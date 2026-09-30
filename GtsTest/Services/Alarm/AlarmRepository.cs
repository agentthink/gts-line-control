using Dapper;
using GtsTest.Data;
using GtsTest.Services.Data;
using GtsTest.Services.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace GtsTest.Services.Alarm
{
    /// <summary>
    /// 报警仓储统一实现（基于 Dapper）：
    /// 通过 IDbConnectionFactory 屏蔽 SQLite / SqlServer / MySQL 的差异。
    /// </summary>
    public class AlarmRepository : IAlarmRepository
    {
        private readonly IDbConnectionFactory _factory;
        private readonly ILogger _logger;
        private bool _tableEnsured;
        private readonly object _initLock = new object();

        public AlarmRepository(IDbConnectionFactory factory, ILogger logger)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _logger = logger ?? new AppLoggerWrapper();
        }

        // ================================================================
        // 建表
        // ================================================================
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
                        Id {SqlDialect.AutoIncrementIntPk},
                        DeviceId {SqlDialect.Text} NULL,
                        Message {SqlDialect.Text} NULL,
                        Severity {SqlDialect.Text} NULL,
                        Timestamp {SqlDialect.Text} NULL,
                        IsAcknowledged {SqlDialect.Bool} NOT NULL DEFAULT 0,
                        IsResolved {SqlDialect.Bool} NOT NULL DEFAULT 0,
                        AcknowledgedBy {SqlDialect.Text} NULL,
                        ResolvedBy {SqlDialect.Text} NULL,
                        AcknowledgedTime {SqlDialect.Text} NULL,
                        ResolvedTime {SqlDialect.Text} NULL";

                    conn.Execute(SqlDialect.CreateTableIfNotExists("AlarmRecords", columns));

                    TryCreateIndex(conn, "idx_alarm_timestamp", "AlarmRecords", "Timestamp DESC");
                    TryCreateIndex(conn, "idx_alarm_resolved", "AlarmRecords", "IsResolved");

                    _tableEnsured = true;
                    _logger.Info($"✅ AlarmRecords 表已就绪 ({SqlDialect.Provider})", "AlarmRepo");
                }
                catch (Exception ex)
                {
                    _logger.Error($"❌ 初始化 AlarmRecords 表失败: {ex.Message}", "AlarmRepo");
                    throw;
                }
            }
        }

        private void TryCreateIndex(IDbConnection conn, string indexName, string tableName, string columns)
        {
            try
            {
                conn.Execute(SqlDialect.CreateIndexIfNotExists(indexName, tableName, columns));
            }
            catch
            {
                // MySQL 已存在同名索引时会抛错，忽略即可
            }
        }

        // ================================================================
        // 插入
        // ================================================================
        public int Insert(AlarmRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            EnsureTable();

            using var conn = _factory.CreateConnection();
            conn.Open();

            const string insertSql = @"
                INSERT INTO AlarmRecords
                (DeviceId, Message, Severity, Timestamp, IsAcknowledged, IsResolved,
                 AcknowledgedBy, ResolvedBy, AcknowledgedTime, ResolvedTime)
                VALUES
                (@DeviceId, @Message, @Severity, @Timestamp, @IsAcknowledged, @IsResolved,
                 @AcknowledgedBy, @ResolvedBy, @AcknowledgedTime, @ResolvedTime);";

            var parameters = new
            {
                DeviceId = (object?)record.DeviceId ?? DBNull.Value,
                Message = (object?)record.Message ?? DBNull.Value,
                Severity = record.Severity.ToString(),
                Timestamp = record.Timestamp.ToString("o"),
                IsAcknowledged = record.IsAcknowledged ? 1 : 0,
                IsResolved = record.IsResolved ? 1 : 0,
                AcknowledgedBy = (object?)record.AcknowledgedBy ?? DBNull.Value,
                ResolvedBy = (object?)record.ResolvedBy ?? DBNull.Value,
                AcknowledgedTime = record.AcknowledgedTime?.ToString("o") ?? (object)DBNull.Value,
                ResolvedTime = record.ResolvedTime?.ToString("o") ?? (object)DBNull.Value
            };

            conn.Execute(insertSql, parameters);

            var id = conn.ExecuteScalar<long>(SqlDialect.LastInsertId);
            return (int)id;
        }

        // ================================================================
        // 更新
        // ================================================================
        public void Update(AlarmRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));

            using var conn = _factory.CreateConnection();
            conn.Open();

            conn.Execute(@"
                UPDATE AlarmRecords SET
                    IsAcknowledged = @IsAcknowledged,
                    IsResolved = @IsResolved,
                    AcknowledgedBy = @AcknowledgedBy,
                    ResolvedBy = @ResolvedBy,
                    AcknowledgedTime = @AcknowledgedTime,
                    ResolvedTime = @ResolvedTime
                WHERE Id = @Id;", new
            {
                Id = record.Id,
                IsAcknowledged = record.IsAcknowledged ? 1 : 0,
                IsResolved = record.IsResolved ? 1 : 0,
                AcknowledgedBy = (object?)record.AcknowledgedBy ?? DBNull.Value,
                ResolvedBy = (object?)record.ResolvedBy ?? DBNull.Value,
                AcknowledgedTime = record.AcknowledgedTime?.ToString("o") ?? (object)DBNull.Value,
                ResolvedTime = record.ResolvedTime?.ToString("o") ?? (object)DBNull.Value
            });
        }

        // ================================================================
        // 查询
        // ================================================================
        public List<AlarmRecord> QueryActive()
        {
            EnsureTable();
            using var conn = _factory.CreateConnection();
            conn.Open();
            var rows = conn.Query<AlarmRowDto>(@"
                SELECT Id, DeviceId, Message, Severity, Timestamp, IsAcknowledged, IsResolved,
                       AcknowledgedBy, ResolvedBy, AcknowledgedTime, ResolvedTime
                FROM AlarmRecords
                WHERE IsResolved = 0
                ORDER BY Timestamp DESC;").ToList();
            return rows.Select(MapDto).ToList();
        }

        public List<AlarmRecord> QueryAll()
        {
            EnsureTable();
            using var conn = _factory.CreateConnection();
            conn.Open();
            var rows = conn.Query<AlarmRowDto>(@"
                SELECT Id, DeviceId, Message, Severity, Timestamp, IsAcknowledged, IsResolved,
                       AcknowledgedBy, ResolvedBy, AcknowledgedTime, ResolvedTime
                FROM AlarmRecords
                ORDER BY Timestamp DESC;").ToList();
            return rows.Select(MapDto).ToList();
        }

        // ================================================================
        // DTO + 映射
        // ================================================================
        /// <summary>
        /// Dapper 映射用 DTO：字段类型用最普适的（long / int / string），
        /// 三个 provider 都能映射到这里，避免 dynamic 的类型不确定性。
        /// </summary>
        private class AlarmRowDto
        {
            public long Id { get; set; }
            public string? DeviceId { get; set; }
            public string? Message { get; set; }
            public string? Severity { get; set; }
            public string? Timestamp { get; set; }
            public int IsAcknowledged { get; set; }
            public int IsResolved { get; set; }
            public string? AcknowledgedBy { get; set; }
            public string? ResolvedBy { get; set; }
            public string? AcknowledgedTime { get; set; }
            public string? ResolvedTime { get; set; }
        }

        private static AlarmRecord MapDto(AlarmRowDto row)
        {
            return new AlarmRecord
            {
                Id = (int)row.Id,
                DeviceId = row.DeviceId,
                Message = row.Message ?? "",
                Severity = Enum.TryParse<AlarmSeverity>(row.Severity, out var sev) ? sev : AlarmSeverity.Info,
                Timestamp = ParseDateTime(row.Timestamp),
                IsAcknowledged = row.IsAcknowledged == 1,
                IsResolved = row.IsResolved == 1,
                AcknowledgedBy = row.AcknowledgedBy,
                ResolvedBy = row.ResolvedBy,
                AcknowledgedTime = ParseNullableDateTime(row.AcknowledgedTime),
                ResolvedTime = ParseNullableDateTime(row.ResolvedTime)
            };
        }

        private static DateTime ParseDateTime(string? s)
        {
            if (string.IsNullOrEmpty(s)) return DateTime.MinValue;
            return DateTime.TryParse(s, out var dt) ? dt : DateTime.MinValue;
        }

        private static DateTime? ParseNullableDateTime(string? s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            return DateTime.TryParse(s, out var dt) ? dt : (DateTime?)null;
        }
    }
}