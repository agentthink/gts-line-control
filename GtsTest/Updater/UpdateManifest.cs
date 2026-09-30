using System;

namespace GtsTest.Updater
{
    /// <summary>
    /// 版本清单（服务器端 manifest.json 反序列化）
    /// </summary>
    public class UpdateManifest
    {
        /// <summary>最新版本号，如 "1.0.5"</summary>
        public string Version { get; set; } = "";

        /// <summary>发布日期</summary>
        public string ReleaseDate { get; set; } = "";

        /// <summary>更新说明（Markdown 或纯文本）</summary>
        public string ReleaseNotes { get; set; } = "";

        /// <summary>更新包下载地址（http:// 或 file:// 或相对路径）</summary>
        public string PackageUrl { get; set; } = "";

        /// <summary>更新包的 SHA256（小写十六进制）</summary>
        public string PackageSha256 { get; set; } = "";

        /// <summary>是否强制更新（用户无法跳过）</summary>
        public bool Mandatory { get; set; } = false;

        /// <summary>最低兼容版本（低于此版本必须先升级到此版本）</summary>
        public string MinCompatibleVersion { get; set; } = "";

        // ============================================================
        // ★ 用 System.Version 全限定名，避免与 UpdateManifest.Version 属性冲突
        // ============================================================

        public System.Version GetVersion()
            => System.Version.TryParse(Version, out var v)
                ? v
                : new System.Version(0, 0, 0, 0);

        public System.Version GetMinCompatible()
            => System.Version.TryParse(MinCompatibleVersion, out var v)
                ? v
                : new System.Version(0, 0, 0, 0);
    }

    /// <summary>检查结果</summary>
    public class UpdateCheckResult
    {
        public bool HasUpdate { get; set; }
        public UpdateManifest? Manifest { get; set; }
        public string? Error { get; set; }
    }
}