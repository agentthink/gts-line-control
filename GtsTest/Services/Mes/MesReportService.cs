using GtsTest.Core;
using GtsTest.Data;
using GtsTest.Services.Logging;
using GtsTest.Services.Mes.WebService;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace GtsTest.Services.Mes
{
    /// <summary>
    /// MES 上报服务
    /// 支持 REST / SOAP 双协议
    /// 异步队列 + 离线缓存（通过 IMesPendingRepository）+ 分级重传 + 健康监控 + 热重载
    /// </summary>
    public class MesReportService : IDisposable
    {
        private readonly MesConfig _config;
        private readonly Channel<MesReportData> _queue;
        private readonly CancellationTokenSource _cts = new();
        private Task? _consumeTask;
        private Task? _retryTask;
        private readonly HttpClient _restHttpClient;
        private readonly IMesPendingRepository _pendingRepository;
        private readonly ILogger _logger;
        private bool _disposed;

        private int _lastPendingCount = 0;
        private bool _isAutoRetryPaused = false;
        private int _retryGate = 0;

        public MesHealthMonitor? HealthMonitor { get; private set; }

        // ============================================================
        // 事件
        // ============================================================
        public event Action<int>? PendingCountChanged;
        public event Action<bool>? AutoRetryPausedChanged;
        public event Action<MesFailureType, string>? ReportFailed;

        // ============================================================
        // 公共属性
        // ============================================================
        public string StationName => _config.StationName;
        public bool IsEnabled => _config.Enabled;
        public string Protocol => _config.Protocol;

        public string ApiUrl =>
            string.Equals(_config.Protocol, "SOAP", StringComparison.OrdinalIgnoreCase)
                ? _config.SoapEndpoint
                : _config.ApiUrl;

        public int PendingCount => _pendingRepository.Count();
        public bool IsAutoRetryPaused => _isAutoRetryPaused;
        public bool IsMesOnline => HealthMonitor?.IsOnline ?? false;

        // ============================================================
        // 构造函数
        // ============================================================
        public MesReportService(MesConfig config, IMesPendingRepository? pendingRepository = null, ILogger? logger = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? new AppLoggerWrapper();

            _pendingRepository = pendingRepository
                ?? new MesPendingRepository(new DbConnectionFactory(), _logger);

            _queue = Channel.CreateUnbounded<MesReportData>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

            _restHttpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMilliseconds(_config.RestTimeoutMs)
            };

            _pendingRepository.EnsureTable();

            _consumeTask = Task.Run(() => ConsumeLoopAsync(_cts.Token));
            _retryTask = Task.Run(() => RetryLoopAsync(_cts.Token));

            HealthMonitor = new MesHealthMonitor(_config);
            HealthMonitor.HealthChanged += OnMesHealthChanged;
            HealthMonitor.Start();

            _logger.Info($"✅ MES 上报服务已启用: Protocol={_config.Protocol}, URL={ApiUrl}", "MES");
            //_logger.Info($"✅ MES 健康监控已启动，间隔 {HealthMonitor.IntervalSeconds}s", "MES");

            NotifyPendingCountChanged();
        }

        // ============================================================
        // 运行时热重载配置
        // ============================================================
        public void ReloadConfig(MesConfig newConfig)
        {
            if (newConfig == null) return;
            try
            {
                _config.Enabled = newConfig.Enabled;
                _config.Protocol = newConfig.Protocol;
                _config.StationName = newConfig.StationName;
                _config.Token = newConfig.Token;

                _config.ApiUrl = newConfig.ApiUrl;
                _config.RestTimeoutMs = newConfig.RestTimeoutMs;

                _config.SoapEndpoint = newConfig.SoapEndpoint;
                _config.SoapTargetNamespace = newConfig.SoapTargetNamespace;
                _config.SoapVersion = newConfig.SoapVersion;
                _config.SoapAction = newConfig.SoapAction;
                _config.SoapMethodName = newConfig.SoapMethodName;
                _config.SoapResultNode = newConfig.SoapResultNode;
                _config.SoapTimeoutMs = newConfig.SoapTimeoutMs;

                _config.AutoRetryEnabled = newConfig.AutoRetryEnabled;
                _config.AutoRetryIntervalSeconds = newConfig.AutoRetryIntervalSeconds;
                _config.PauseAutoRetryThreshold = newConfig.PauseAutoRetryThreshold;

                try { _restHttpClient.Timeout = TimeSpan.FromMilliseconds(_config.RestTimeoutMs); } catch { }

                HealthMonitor?.ReloadConfig(_config);
                _logger.Info($"🔄 MES 上报服务配置已重载: Protocol={_config.Protocol}, URL={ApiUrl}", "MES");
                NotifyPendingCountChanged();
            }
            catch (Exception ex)
            {
                _logger.Error($"❌ MES 配置重载失败: {ex.Message}", "MES");
            }
        }

        private void OnMesHealthChanged(bool online, string msg)
        {
            if (online)
            {
                _logger.Info($"🔄 MES 恢复在线，立即触发一次重传", "MES");
                _ = Task.Run(async () =>
                {
                    try { await RetryPendingAsync().ConfigureAwait(false); }
                    catch (Exception ex) { _logger.Warn($"恢复后重传异常: {ex.Message}", "MES"); }
                });
            }
        }

        // ============================================================
        // 入队
        // ============================================================
        public void Enqueue(MesReportData data)
        {
            if (!_config.Enabled) return;
            _queue.Writer.TryWrite(data);
        }

        // ============================================================
        // 手动重传
        // ============================================================
        public async Task<int> ForceRetryAsync()
        {
            if (Interlocked.CompareExchange(ref _retryGate, 1, 0) != 0)
            {
                _logger.Warn("⚠️ 已有重传任务在进行，跳过", "MES");
                return -1;
            }
            try
            {
                _logger.Info("🔄 手动触发 MES 重传...", "MES");
                _isAutoRetryPaused = false;
                AutoRetryPausedChanged?.Invoke(false);
                return await RetryPendingAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Error($"❌ 手动重传异常: {ex.Message}", "MES");
                return 0;
            }
            finally { Interlocked.Exchange(ref _retryGate, 0); }
        }

        // ============================================================
        // 消费循环
        // ============================================================
        private async Task ConsumeLoopAsync(CancellationToken ct)
        {
            try
            {
                await foreach (var data in _queue.Reader.ReadAllAsync(ct).ConfigureAwait(false))
                {
                    var failType = await PostToMesAsync(data, ct).ConfigureAwait(false);
                    if (failType == MesFailureType.None) continue;

                    data.FailReason = failType.ToString();
                    data.RetryCount = 0;
                    data.NextRetryTime = CalculateNextRetryTime(failType, 0).ToString("o");

                    try
                    {
                        data.Id = _pendingRepository.Insert(data);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"❌ MES 缓存写入失败: {ex.Message}", "MES");
                        continue;
                    }

                    _logger.Warn($"⚠️ MES 上报失败({failType})，已缓存本地: {data.Barcode}", "MES");
                    ReportFailed?.Invoke(failType, $"MES 上报失败: {data.Barcode}, 原因: {failType}");
                    NotifyPendingCountChanged();
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.Error($"❌ MES 消费循环异常: {ex.Message}", "MES");
            }
        }

        // ============================================================
        // 定期重传
        // ============================================================
        private async Task RetryLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_config.AutoRetryIntervalSeconds), ct);

                    if (!_config.Enabled || !_config.AutoRetryEnabled) continue;
                    if (HealthMonitor != null && !HealthMonitor.IsOnline) continue;

                    var pendingCount = _pendingRepository.Count();
                    if (pendingCount == 0)
                    {
                        if (_isAutoRetryPaused)
                        {
                            _isAutoRetryPaused = false;
                            AutoRetryPausedChanged?.Invoke(false);
                        }
                        continue;
                    }

                    if (pendingCount > _config.PauseAutoRetryThreshold)
                    {
                        if (!_isAutoRetryPaused)
                        {
                            _isAutoRetryPaused = true;
                            AutoRetryPausedChanged?.Invoke(true);
                            _logger.Warn($"⚠️ 待重传积压 {pendingCount} 条，暂停自动重传", "MES");
                        }
                        continue;
                    }

                    if (_isAutoRetryPaused)
                    {
                        _isAutoRetryPaused = false;
                        AutoRetryPausedChanged?.Invoke(false);
                        _logger.Info($"✅ 积压恢复 ({pendingCount} 条)，重新开始自动重传", "MES");
                    }

                    await RetryPendingAsync().ConfigureAwait(false);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.Error($"❌ MES 重传循环异常: {ex.Message}", "MES");
                    await Task.Delay(5000, ct);
                }
            }
        }

        public async Task<int> RetryPendingAsync()
        {
            int totalSuccess = 0;
            if (!_config.Enabled) return 0;

            for (int round = 0; round < 10; round++)
            {
                var pending = _pendingRepository.QueryForRetry(200);
                if (pending.Count == 0)
                {
                    NotifyPendingCountChanged();
                    return totalSuccess;
                }

                int successCount = 0, failCount = 0, skippedCount = 0;

                foreach (var data in pending)
                {
                    if (data.FailReason == MesFailureType.ClientError.ToString()) { skippedCount++; continue; }
                    if (data.RetryCount >= GetMaxRetryForType(data.FailReason)) { skippedCount++; continue; }

                    if (!string.IsNullOrEmpty(data.NextRetryTime))
                    {
                        if (DateTime.TryParse(data.NextRetryTime, out var nextTime) && nextTime > DateTime.Now)
                        { skippedCount++; continue; }
                    }

                    var failType = await PostToMesAsync(data, CancellationToken.None).ConfigureAwait(false);
                    if (failType == MesFailureType.None)
                    {
                        _pendingRepository.Delete(data.Id);
                        successCount++;
                    }
                    else
                    {
                        data.RetryCount++;
                        data.FailReason = failType.ToString();
                        data.NextRetryTime = CalculateNextRetryTime(failType, data.RetryCount).ToString("o");
                        _pendingRepository.UpdateRetry(data);
                        failCount++;
                    }
                }

                _logger.Info($"📊 第 {round + 1} 轮重传：成功 {successCount}，失败 {failCount}，跳过 {skippedCount}", "MES");
                totalSuccess += successCount;
                NotifyPendingCountChanged();

                if (failCount == 0) return totalSuccess;
                await Task.Delay(3000);
            }
            return totalSuccess;
        }

        // ============================================================
        // 上报（REST / SOAP）
        // ============================================================
        private async Task<MesFailureType> PostToMesAsync(MesReportData data, CancellationToken ct)
        {
            if (!_config.Enabled) return MesFailureType.None;

            try
            {
                if (string.Equals(_config.Protocol, "SOAP", StringComparison.OrdinalIgnoreCase))
                    return await PostToMesBySoapAsync(data, ct).ConfigureAwait(false);
                else
                    return await PostToMesByRestAsync(data, ct).ConfigureAwait(false);
            }
            catch (TaskCanceledException) { return MesFailureType.Timeout; }
            catch (HttpRequestException httpEx)
            {
                if (httpEx.Message.Contains("HTTP 4")) return MesFailureType.ClientError;
                return MesFailureType.ConnectionFailed;
            }
            catch (Exception ex)
            {
                _logger.Error($"❌ MES 上报异常: {ex.Message}", "MES");
                return MesFailureType.ConnectionFailed;
            }
        }

        private async Task<MesFailureType> PostToMesByRestAsync(MesReportData data, CancellationToken ct)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, _config.ApiUrl) { Content = content };
            if (!string.IsNullOrEmpty(_config.Token))
                request.Headers.Add("Authorization", $"Bearer {_config.Token}");

            var response = await _restHttpClient.SendAsync(request, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.Info($"✅ MES 上报成功(REST): {data.Barcode} | {data.Result}", "MES");
                return MesFailureType.None;
            }

            if ((int)response.StatusCode >= 500) return MesFailureType.ServerError;
            return MesFailureType.ClientError;
        }

        private async Task<MesFailureType> PostToMesBySoapAsync(MesReportData data, CancellationToken ct)
        {
            using var soapClient = new SoapHttpClient(
                _config.SoapEndpoint,
                _config.SoapTargetNamespace,
                _config.SoapAction,
                _config.SoapVersion,
                _config.SoapTimeoutMs);

            if (!string.IsNullOrEmpty(_config.Token))
                soapClient.AddHeader("Authorization", $"Bearer {_config.Token}");

            var parameters = new Dictionary<string, object?>
            {
                { "barcode", data.Barcode }, { "deviceId", data.DeviceId },
                { "station", data.Station }, { "result", data.Result },
                { "diameterMm", data.DiameterMm }, { "x", data.X }, { "y", data.Y },
                { "imagePath", data.ImagePath ?? "" },
                { "timestamp", data.Timestamp.ToString("o") },
            };

            var result = await soapClient.InvokeAsync(
                _config.SoapMethodName, parameters, _config.SoapResultNode, ct).ConfigureAwait(false);

            _logger.Info($"✅ MES 上报成功(SOAP): {data.Barcode} | 返回: {result}", "MES");
            return MesFailureType.None;
        }

        // ============================================================
        // 分级重传策略
        // ============================================================
        private DateTime CalculateNextRetryTime(MesFailureType type, int retryCount)
        {
            int seconds = type switch
            {
                MesFailureType.Timeout => 5,
                MesFailureType.ConnectionFailed => 5,
                MesFailureType.ServerError => Math.Min(30 * (int)Math.Pow(2, retryCount), 300),
                MesFailureType.ClientError => 999999,
                _ => 30
            };
            return DateTime.Now.AddSeconds(seconds);
        }

        private int GetMaxRetryForType(string? failReason)
        {
            if (failReason == MesFailureType.ClientError.ToString()) return 0;
            if (failReason == MesFailureType.ServerError.ToString()) return 10;
            return 20;
        }

        private void NotifyPendingCountChanged()
        {
            try
            {
                var count = _pendingRepository.Count();
                if (count != _lastPendingCount)
                {
                    _lastPendingCount = count;
                    PendingCountChanged?.Invoke(count);
                }
            }
            catch { }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try { HealthMonitor?.Dispose(); } catch { }
            try { _queue.Writer.Complete(); } catch { }
            try { _cts.Cancel(); } catch { }
            try { _consumeTask?.Wait(2000); } catch { }
            try { _retryTask?.Wait(2000); } catch { }
            try { _restHttpClient?.Dispose(); } catch { }
            try { _cts.Dispose(); } catch { }

            GC.SuppressFinalize(this);
        }
    }
}