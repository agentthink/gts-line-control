using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GtsTest.Data
{
    public static class DbContextFactory
    {
        public static DatabaseProvider CurrentProvider { get; private set; } = DatabaseProvider.Sqlite;
        public static string CurrentConnectionString { get; private set; } = "";

        /// <summary>
        /// 从 appsettings.json 读取配置；失败则回退 SQLite
        /// </summary>
        public static void ConfigureFromAppSettings()
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    using var doc = JsonDocument.Parse(json);
                    var dbSection = doc.RootElement.GetProperty("Database");

                    var providerStr = dbSection.GetProperty("Provider").GetString() ?? "Sqlite";
                    var connStr = dbSection.GetProperty("ConnectionString").GetString() ?? "";

                    if (Enum.TryParse<DatabaseProvider>(providerStr, true, out var provider))
                    {
                        CurrentProvider = provider;
                        CurrentConnectionString = connStr;
                        Core.AppLogger.Info(
                            $"✅ 数据库配置: Provider={provider}, ConnStr={Mask(connStr)}",
                            "DbFactory");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Core.AppLogger.Warn($"⚠️ 读取 appsettings.json 失败: {ex.Message}", "DbFactory");
            }

            // 兜底：默认 SQLite
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gts.db");
            CurrentProvider = DatabaseProvider.Sqlite;
            CurrentConnectionString = $"Data Source={dbPath}";
            Core.AppLogger.Info($"⚠️ 使用默认 SQLite: {dbPath}", "DbFactory");
        }

        private static string Mask(string connStr)
        {
            if (string.IsNullOrEmpty(connStr)) return "";
            return Regex.Replace(connStr, @"(password|pwd)=[^;]*", "$1=***", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// 创建 DbContext（每次调用返回新实例，由调用方 using 释放）
        /// </summary>
        public static GtsDbContext Create()
        {
            var builder = new DbContextOptionsBuilder<GtsDbContext>();
            ConfigureOptions(builder);
            return new GtsDbContext(builder.Options);
        }

        public static void ConfigureOptions(DbContextOptionsBuilder builder)
        {
            switch (CurrentProvider)
            {
                case DatabaseProvider.SqlServer:
                    builder.UseSqlServer(CurrentConnectionString, opt =>
                    {
                        opt.EnableRetryOnFailure(3);
                        opt.CommandTimeout(30);
                    });
                    break;

                case DatabaseProvider.MySql:
                    builder.UseMySql(CurrentConnectionString,
                        ServerVersion.AutoDetect(CurrentConnectionString),
                        opt =>
                        {
                            opt.EnableRetryOnFailure(3);
                            opt.CommandTimeout(30);
                        });
                    break;

                case DatabaseProvider.Sqlite:
                default:
                    builder.UseSqlite(CurrentConnectionString);
                    break;
            }
        }

        /// <summary>
        /// 确保数据库存在（首次运行时自动建表）
        /// </summary>
        public static void EnsureDatabase()
        {
            try
            {
                using var db = Create();
                db.Database.EnsureCreated();
                Core.AppLogger.Info($"✅ 数据库已就绪 ({CurrentProvider})", "DbFactory");
            }
            catch (Exception ex)
            {
                Core.AppLogger.Error($"❌ 数据库初始化失败: {ex.Message}", "DbFactory");
                throw;
            }
        }
    }
}