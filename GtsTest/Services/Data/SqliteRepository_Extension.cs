using GtsTest.Core;
using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace GtsTest.Services.Data
{
    public static class DatabaseExtension
    {
        public static void EnsureMesPendingTable()
        {
            try
            {
                var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gts.db");
                using var conn = new SqliteConnection($"Data Source={dbPath}");
                conn.Open();
                using var cmd = conn.CreateCommand();

                // 建表（含新增字段）
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS MesPendingRecords (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Barcode TEXT,
                        DeviceId TEXT,
                        Result TEXT,
                        DiameterMm REAL,
                        X REAL,
                        Y REAL,
                        ImagePath TEXT,
                        Timestamp TEXT,
                        JsonData TEXT,
                        FailReason TEXT,
                        RetryCount INTEGER DEFAULT 0,
                        NextRetryTime TEXT
                    );
                    CREATE INDEX IF NOT EXISTS idx_mes_pending_ts ON MesPendingRecords(Timestamp);
                    CREATE INDEX IF NOT EXISTS idx_mes_next_retry ON MesPendingRecords(NextRetryTime);";
                cmd.ExecuteNonQuery();

                // ⭐ 兼容老版本数据库：尝试新增字段（如果已存在会忽略错误）
                TryAddColumn(conn, "MesPendingRecords", "FailReason", "TEXT");
                TryAddColumn(conn, "MesPendingRecords", "RetryCount", "INTEGER DEFAULT 0");
                TryAddColumn(conn, "MesPendingRecords", "NextRetryTime", "TEXT");

                AppLogger.Info("✅ MesPendingRecords 表已就绪", "DB");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"创建 MES 待重传表失败: {ex.Message}", "DB");
            }
        }

        private static void TryAddColumn(SqliteConnection conn, string table, string column, string type)
        {
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {type};";
                cmd.ExecuteNonQuery();
            }
            catch
            {
                // 字段已存在，忽略
            }
        }
    }
}