using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace GtsTest.Updater
{
    /// <summary>
    /// 下载更新包 + SHA256 校验
    /// </summary>
    public static class UpdateDownloader
    {
        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(10)
        };

        /// <summary>
        /// 下载更新包到临时目录，返回本地路径
        /// </summary>
        public static async Task<string?> DownloadAsync(
            string packageUrl, string expectedSha256,
            IProgress<int>? progress = null, CancellationToken ct = default)
        {
            try
            {
                var tempDir = Path.Combine(Path.GetTempPath(), "GtsTestUpdate");
                Directory.CreateDirectory(tempDir);

                // 清理旧文件
                foreach (var old in Directory.GetFiles(tempDir, "*.zip"))
                {
                    try { File.Delete(old); } catch { }
                }

                var destPath = Path.Combine(tempDir, $"package_{DateTime.Now:yyyyMMddHHmmss}.zip");

                if (packageUrl.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
                    || !packageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    // 本地/共享路径直接复制
                    var srcPath = packageUrl.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
                        ? new Uri(packageUrl).LocalPath
                        : packageUrl;
                    File.Copy(srcPath, destPath, true);
                    progress?.Report(100);
                }
                else
                {
                    // HTTP 下载
                    using var resp = await _http.GetAsync(packageUrl, HttpCompletionOption.ResponseHeadersRead, ct)
                        .ConfigureAwait(false);
                    resp.EnsureSuccessStatusCode();

                    var total = resp.Content.Headers.ContentLength ?? 0;
                    await using var src = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
                    await using var dst = File.Create(destPath);

                    var buffer = new byte[81920];
                    long read = 0;
                    int n;
                    while ((n = await src.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
                    {
                        await dst.WriteAsync(buffer.AsMemory(0, n), ct).ConfigureAwait(false);
                        read += n;
                        if (total > 0)
                            progress?.Report((int)(read * 100 / total));
                    }
                }

                // SHA256 校验
                if (!string.IsNullOrWhiteSpace(expectedSha256))
                {
                    var actual = ComputeSha256(destPath);
                    if (!string.Equals(actual, expectedSha256, StringComparison.OrdinalIgnoreCase))
                    {
                        Core.AppLogger.Error(
                            $"更新包 SHA256 校验失败: 期望={expectedSha256}, 实际={actual}", "Updater");
                        try { File.Delete(destPath); } catch { }
                        return null;
                    }
                    Core.AppLogger.Info("✅ 更新包 SHA256 校验通过", "Updater");
                }

                return destPath;
            }
            catch (OperationCanceledException)
            {
                Core.AppLogger.Warn("下载更新包已取消", "Updater");
                return null;
            }
            catch (Exception ex)
            {
                Core.AppLogger.Error($"下载更新包失败: {ex.Message}", "Updater");
                return null;
            }
        }

        public static string ComputeSha256(string filePath)
        {
            using var sha = SHA256.Create();
            using var fs = File.OpenRead(filePath);
            var hash = sha.ComputeHash(fs);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}