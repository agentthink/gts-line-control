using System;
using System.IO;
using System.Text.Json;
using GtsTest.Core;

namespace GtsTest.Services.Mes
{
    /// <summary>
    /// MES 上报配置（支持 REST 和 SOAP 双协议）
    /// </summary>
    public class MesConfig
    {
        // ============================================================
        // 通用
        // ============================================================
        /// <summary>是否启用 MES 上报</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>协议类型：REST / SOAP</summary>
        public string Protocol { get; set; } = "REST";

        /// <summary>站点名称</summary>
        public string StationName { get; set; } = "Vision_Station_01";

        /// <summary>认证 Token（可选）</summary>
        public string Token { get; set; } = "";

        // ============================================================
        // REST 配置
        // ============================================================
        /// <summary>REST API 地址</summary>
        public string ApiUrl { get; set; } = "http://localhost:8888/api/mes";

        /// <summary>REST 请求超时（毫秒）</summary>
        public int RestTimeoutMs { get; set; } = 10000;

        // ============================================================
        // SOAP 配置
        // ============================================================
        /// <summary>WebService 端点地址</summary>
        public string SoapEndpoint { get; set; } = "http://localhost:8080/MesService.asmx";

        /// <summary>目标命名空间</summary>
        public string SoapTargetNamespace { get; set; } = "http://tempuri.org/";

        /// <summary>SOAP 版本：1.1 / 1.2</summary>
        public string SoapVersion { get; set; } = "1.1";

        /// <summary>SOAPAction（SOAP 1.1 必填）</summary>
        public string SoapAction { get; set; } = "http://tempuri.org/ReportProduction";

        /// <summary>方法名</summary>
        public string SoapMethodName { get; set; } = "ReportProduction";

        /// <summary>响应中结果节点名</summary>
        public string SoapResultNode { get; set; } = "ReportProductionResult";

        /// <summary>SOAP 请求超时（毫秒）</summary>
        public int SoapTimeoutMs { get; set; } = 10000;

        // ============================================================
        // 重试策略
        // ============================================================
        /// <summary>是否自动重传</summary>
        public bool AutoRetryEnabled { get; set; } = true;

        /// <summary>重试间隔（秒）</summary>
        public int AutoRetryIntervalSeconds { get; set; } = 30;

        /// <summary>积压阈值：待重传超过此值暂停自动重传</summary>
        public int PauseAutoRetryThreshold { get; set; } = 100;

        // ============================================================
        // 加载 / 保存
        // ============================================================
        private static string ConfigPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mes_config.json");

        /// <summary>加载配置（文件不存在则创建默认）</summary>
        public static MesConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    var config = JsonSerializer.Deserialize<MesConfig>(json);
                    if (config != null) return config;
                }
            }
            catch (Exception ex)
            {
                try { AppLogger.Warn($"加载 MES 配置失败: {ex.Message}", "MES"); } catch { }
            }

            var defaultConfig = new MesConfig();
            defaultConfig.Save();
            return defaultConfig;
        }

        /// <summary>保存配置</summary>
        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                try { AppLogger.Error($"保存 MES 配置失败: {ex.Message}", "MES"); } catch { }
            }
        }
    }
}