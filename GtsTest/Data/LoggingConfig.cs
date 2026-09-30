using GtsTest.Core;
using System;
using System.IO;
using System.Text.Json;

namespace GtsTest.Data
{
    /// <summary>
    /// 日志配置（从 appsettings.json 的 Logging 段读取）
    /// </summary>
    public class LoggingConfig
    {
        public string Level { get; set; } = "Info";
        public int MaxFileSizeMB { get; set; } = 10;
        public int RetentionDays { get; set; } = 30;

        private static string ConfigPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        /// <summary>加载日志配置</summary>
        public static LoggingConfig Load()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                    return new LoggingConfig();

                string json = File.ReadAllText(ConfigPath);
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("Logging", out var loggingSection))
                    return new LoggingConfig();

                var cfg = new LoggingConfig();

                if (loggingSection.TryGetProperty("Level", out var levelEl))
                    cfg.Level = levelEl.GetString() ?? "Info";

                if (loggingSection.TryGetProperty("MaxFileSizeMB", out var sizeEl))
                    cfg.MaxFileSizeMB = sizeEl.GetInt32();

                if (loggingSection.TryGetProperty("RetentionDays", out var daysEl))
                    cfg.RetentionDays = daysEl.GetInt32();

                return cfg;
            }
            catch (Exception ex)
            {
                try { AppLogger.Warn($"读取日志配置失败: {ex.Message}", "Logging"); } catch { }
                return new LoggingConfig();
            }
        }

        /// <summary>应用日志配置到 AppLogger</summary>
        public void Apply()
        {
            if (Enum.TryParse<LogLevel>(Level, true, out var level))
            {
                AppLogger.GlobalLogLevel = level;
                AppLogger.Info($"✅ 日志级别已应用: {level}", "Logging");
            }
            else
            {
                AppLogger.Warn($"⚠️ 无效的日志级别 '{Level}'，使用默认 Info", "Logging");
                AppLogger.GlobalLogLevel = LogLevel.Info;
            }

            AppLogger.MaxFileSizeMB = MaxFileSizeMB;
            AppLogger.RetentionDays = RetentionDays;
        }

        /// <summary>获取当前日志级别名称</summary>
        public static string GetCurrentLevelName()
        {
            return AppLogger.GlobalLogLevel.ToString();
        }

        /// <summary>动态设置日志级别（供 UI 下拉框使用）</summary>
        public static bool SetLevel(string levelName)
        {
            if (Enum.TryParse<LogLevel>(levelName, true, out var level))
            {
                AppLogger.GlobalLogLevel = level;
                AppLogger.Info($"🔄 日志级别已动态切换: {level}", "Logging");
                return true;
            }
            return false;
        }
    }
}