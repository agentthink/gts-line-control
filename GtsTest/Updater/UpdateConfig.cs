using System;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace GtsTest.Updater
{
    /// <summary>
    /// 自动更新配置（从 appsettings.json 的 Updater 段读取）
    /// </summary>
    public class UpdateConfig
    {
        public bool Enabled { get; set; } = false;
        public string ManifestUrl { get; set; } = "";
        public bool AutoCheckOnStartup { get; set; } = true;
        public int CheckTimeoutMs { get; set; } = 5000;
        public bool ShowReleaseNotes { get; set; } = true;

        private static string ConfigPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        /// <summary>当前程序版本（从 Assembly 读）</summary>
        public static Version GetCurrentVersion()
        {
            try
            {
                var asm = Assembly.GetEntryAssembly();
                return asm?.GetName().Version ?? new Version(1, 0, 0, 0);
            }
            catch { return new Version(1, 0, 0, 0); }
        }

        public static UpdateConfig Load()
        {
            try
            {
                if (!File.Exists(ConfigPath)) return new UpdateConfig();

                var json = File.ReadAllText(ConfigPath);
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("Updater", out var section))
                    return new UpdateConfig();

                var cfg = new UpdateConfig();
                if (section.TryGetProperty("Enabled", out var e)) cfg.Enabled = e.GetBoolean();
                if (section.TryGetProperty("ManifestUrl", out var m)) cfg.ManifestUrl = m.GetString() ?? "";
                if (section.TryGetProperty("AutoCheckOnStartup", out var a)) cfg.AutoCheckOnStartup = a.GetBoolean();
                if (section.TryGetProperty("CheckTimeoutMs", out var t)) cfg.CheckTimeoutMs = t.GetInt32();
                if (section.TryGetProperty("ShowReleaseNotes", out var s)) cfg.ShowReleaseNotes = s.GetBoolean();
                return cfg;
            }
            catch (Exception ex)
            {
                Core.AppLogger.Warn($"读取更新配置失败: {ex.Message}", "Updater");
                return new UpdateConfig();
            }
        }
    }
}