using GtsTest.Core;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GtsTest.Services.Mes
{
    /// <summary>
    /// MES 健康监控看门狗
    /// 每 N 秒 ping 一次 MES 端点，判定在线/离线
    /// 支持运行时热重载配置
    /// </summary>
    public class MesHealthMonitor : IDisposable
    {
        private MesConfig _config;   // ⭐ 非 readonly，支持热重载
        private readonly HttpClient _httpClient;
        private readonly CancellationTokenSource _cts = new();
        private Task? _monitorTask;

        private int _consecutiveFailures = 0;
        private int _consecutiveSuccesses = 0;
        private bool _isOnline = false;
        private bool _disposed;

        public int IntervalSeconds { get; set; } = 10;
        public int OfflineThreshold { get; set; } = 3;
        public int OnlineThreshold { get; set; } = 2;

        public event Action<bool, string>? HealthChanged;

        public bool IsOnline => _isOnline;
        public int LastLatencyMs { get; private set; }
        public DateTime LastCheckTime { get; private set; } = DateTime.MinValue;

        public MesHealthMonitor(MesConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        }

        /// <summary>⭐ 运行时热重载配置</summary>
        public void ReloadConfig(MesConfig newConfig)
        {
            if (newConfig == null) return;

            // 原地更新配置
            _config.Enabled = newConfig.Enabled;
            _config.Protocol = newConfig.Protocol;
            _config.ApiUrl = newConfig.ApiUrl;
            _config.SoapEndpoint = newConfig.SoapEndpoint;

            // 重置探测计数
            _consecutiveFailures = 0;
            _consecutiveSuccesses = 0;

            AppLogger.Info($"🔄 MES 健康监控配置已重载: Protocol={_config.Protocol}", "MES");
        }

        public void Start()
        {
            if (_monitorTask != null) return;
            _monitorTask = Task.Run(() => MonitorLoop(_cts.Token));
            AppLogger.Info($"✅ MES 健康监控已启动，间隔 {IntervalSeconds}s", "MES");
        }

        public void Stop()
        {
            try { _cts.Cancel(); } catch { }
            try { _monitorTask?.Wait(2000); } catch { }
        }

        private async Task MonitorLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await CheckOnceAsync(ct).ConfigureAwait(false);
                    await Task.Delay(TimeSpan.FromSeconds(IntervalSeconds), ct);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    AppLogger.Debug($"健康检查异常: {ex.Message}", "MES");
                }
            }
        }

        private async Task CheckOnceAsync(CancellationToken ct)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            bool ok = false;
            string errorMsg = "";

            try
            {
                var targetUrl = _config.Protocol.Equals("SOAP", StringComparison.OrdinalIgnoreCase)
                    ? _config.SoapEndpoint
                    : _config.ApiUrl;

                using var req = new HttpRequestMessage(HttpMethod.Head, targetUrl);
                using var resp = await _httpClient.SendAsync(req, ct).ConfigureAwait(false);

                if ((int)resp.StatusCode < 500)
                    ok = true;
                else
                    errorMsg = $"HTTP {(int)resp.StatusCode}";
            }
            catch (TaskCanceledException)
            {
                errorMsg = "超时";
            }
            catch (HttpRequestException httpEx)
            {
                if (httpEx.Message.Contains("HTTP 4"))
                    ok = true;
                else
                    errorMsg = httpEx.Message;
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
            }

            sw.Stop();
            LastLatencyMs = (int)sw.ElapsedMilliseconds;
            LastCheckTime = DateTime.Now;

            if (ok)
            {
                _consecutiveFailures = 0;
                _consecutiveSuccesses++;

                if (!_isOnline && _consecutiveSuccesses >= OnlineThreshold)
                {
                    _isOnline = true;
                    AppLogger.Info($"🟢 MES 已恢复在线 (延迟 {LastLatencyMs}ms)", "MES");
                    HealthChanged?.Invoke(true, $"在线，延迟 {LastLatencyMs}ms");
                }
            }
            else
            {
                _consecutiveSuccesses = 0;
                _consecutiveFailures++;

                if (_isOnline && _consecutiveFailures >= OfflineThreshold)
                {
                    _isOnline = false;
                    AppLogger.Warn($"🔴 MES 已离线 (连续失败 {_consecutiveFailures} 次，原因: {errorMsg})", "MES");
                    HealthChanged?.Invoke(false, $"离线: {errorMsg}");
                }
            }
        }

        public Task ForceCheckAsync() => CheckOnceAsync(_cts.Token);

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
            _httpClient?.Dispose();
            _cts?.Dispose();
        }
    }
}