using Dapper;
using GtsTest.Data;
using GtsTest.Services.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace GtsTest.Services.Data
{
    /// <summary>审计仓储统一实现（基于 Dapper）</summary>
    public class AuditRepository : IAuditRepository
    {
        private readonly IDbConnectionFactory _factory;
        private readonly ILogger _logger;
        private bool _tableEnsured;
        private readonly object _initLock = new object();

        public AuditRepository(IDbConnectionFactory factory, ILogger logger)
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
                        UserId {SqlDialect.BigInt} NULL,
                        Username {SqlDialect.Text} NULL,
                        ActionType {SqlDialect.Text} NULL,
                        Detail {SqlDialect.LongText} NULL,
                        Timestamp {SqlDialect.Text} NULL";

                    conn.Execute(SqlDialect.CreateTableIfNotExists("AuditLogs", columns));
                    TryCreateIndex(conn, "idx_audit_timestamp", "AuditLogs", "Timestamp DESC");

                    _tableEnsured = true;
                    _logger.Info($"✅ AuditLogs 表已就绪 ({SqlDialect.Provider})", "AuditRepo");
                }
                catch (Exception ex)
                {
                    _logger.Error($"❌ 初始化 AuditLogs 表失败: {ex.Message}", "AuditRepo");
                    throw;
                }
            }
        }

        private void TryCreateIndex(IDbConnection conn, string indexName, string tableName, string columns)
        {
            try { conn.Execute(SqlDialect.CreateIndexIfNotExists(indexName, tableName, columns)); }
            catch { /* MySQL 已存在索引时抛错，忽略 */ }
        }

        public void Insert(long userId, string username, string actionType, string detail, DateTime timestamp)
        {
            EnsureTable();
            using var conn = _factory.CreateConnection();
            conn.Open();

            conn.Execute(@"
                INSERT INTO AuditLogs (UserId, Username, ActionType, Detail, Timestamp)
                VALUES (@UserId, @Username, @ActionType, @Detail, @Timestamp);", new
            {
                UserId = userId,
                Username = username ?? "",
                ActionType = actionType ?? "",
                Detail = detail ?? "",
                Timestamp = timestamp.ToString("o")
            });
        }

        public List<AuditLog> QueryRecent(int limit)
        {
            EnsureTable();
            using var conn = _factory.CreateConnection();
            conn.Open();
            var rows = conn.Query<AuditRowDto>(@"
                SELECT Id, UserId, Username, ActionType, Detail, Timestamp
                FROM AuditLogs
                ORDER BY Timestamp DESC
                LIMIT @Limit;", new { Limit = limit }).ToList();
            return rows.Select(r => new AuditLog
            {
                Id = r.Id,
                UserId = r.UserId,
                Username = r.Username ?? "",
                ActionType = r.ActionType ?? "",
                Detail = r.Detail ?? "",
                Timestamp = r.Timestamp ?? ""
            }).ToList();
        }

        private class AuditRowDto
        {
            public long Id { get; set; }
            public long UserId { get; set; }
            public string? Username { get; set; }
            public string? ActionType { get; set; }
            public string? Detail { get; set; }
            public string? Timestamp { get; set; }
        }
    }
}