using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GtsTest.Updater
{
    /// <summary>
    /// 检查更新：支持 http(s):// 和 file:// 两种协议
    /// </summary>
    public static class UpdateChecker
    {
        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        public static async Task<UpdateCheckResult> CheckAsync(UpdateConfig config, CancellationToken ct = default)
        {
            var result = new UpdateCheckResult();

            try
            {
                if (!config.Enabled || string.IsNullOrWhiteSpace(config.ManifestUrl))
                {
                    result.Error = "更新功能未启用";
                    return result;
                }

                string json = await FetchManifestJsonAsync(config.ManifestUrl, config.CheckTimeoutMs, ct)
                    .ConfigureAwait(false);

                var manifest = JsonSerializer.Deserialize<UpdateManifest>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (manifest == null || string.IsNullOrWhiteSpace(manifest.Version))
                {
                    result.Error = "manifest 格式错误";
                    return result;
                }

                // 解析相对路径 → 相对 manifest 所在目录
                if (!string.IsNullOrEmpty(manifest.PackageUrl)
                    && !manifest.PackageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    && !manifest.PackageUrl.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
                {
                    var baseUri = new Uri(config.ManifestUrl);
                    manifest.PackageUrl = new Uri(baseUri, manifest.PackageUrl).ToString();
                }

                var current = UpdateConfig.GetCurrentVersion();
                var latest = manifest.GetVersion();
                var minCompat = manifest.GetMinCompatible();

                // 版本比较
                result.HasUpdate = latest > current;
                result.Manifest = manifest;

                // 最低兼容检查：如果当前版本太低，即便有更新也提示先升到 min
                if (result.HasUpdate && minCompat > new Version(0, 0, 0, 0) && current < minCompat)
                {
                    Core.AppLogger.Warn(
                        $"当前版本 {current} 低于最低兼容版本 {minCompat}，需要完整安装包升级", "Updater");
                }

                Core.AppLogger.Info(
                    $"版本检查: 当前={current}, 最新={latest}, 有更新={result.HasUpdate}", "Updater");

                return result;
            }
            catch (OperationCanceledException)
            {
                result.Error = "检查超时/取消";
                return result;
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                Core.AppLogger.Warn($"检查更新失败: {ex.Message}", "Updater");
                return result;
            }
        }

        private static async Task<string> FetchManifestJsonAsync(string url, int timeoutMs, CancellationToken ct)
        {
            if (url.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            {
                // file:// 本地/共享路径
                var localPath = new Uri(url).LocalPath;
                return await File.ReadAllTextAsync(localPath, ct).ConfigureAwait(false);
            }
            else if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
                linked.CancelAfter(timeoutMs);
                return await _http.GetStringAsync(url, linked.Token).ConfigureAwait(false);
            }
            else
            {
                // 当作本地路径处理
                return await File.ReadAllTextAsync(url, ct).ConfigureAwait(false);
            }
        }
    }
}