using System;
using System.Diagnostics;
using System.IO;

namespace GtsTest.Updater
{
    /// <summary>
    /// 启动 GtsUpdater.exe 并传参
    /// </summary>
    public static class UpdateLauncher
    {
        /// <summary>
        /// 启动 Updater，调用方随后应立即退出主程序
        /// </summary>
        public static bool Launch(string packagePath, bool restartAfter = true)
        {
            try
            {
                var appDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
                var updaterPath = Path.Combine(appDir, "GtsUpdater.exe");

                if (!File.Exists(updaterPath))
                {
                    Core.AppLogger.Error($"找不到 GtsUpdater.exe: {updaterPath}", "Updater");
                    return false;
                }

                var mainExe = Process.GetCurrentProcess().MainModule?.FileName
                    ?? Path.Combine(appDir, "GtsTest.exe");
                var currentPid = Process.GetCurrentProcess().Id;

                var args = $"--pid {currentPid} " +
                           $"--app-dir \"{appDir}\" " +
                           $"--package \"{packagePath}\" " +
                           $"--main-exe \"{mainExe}\"" +
                           (restartAfter ? " --restart" : "");

                Core.AppLogger.Info($"🚀 启动更新程序: {updaterPath} {args}", "Updater");

                Process.Start(new ProcessStartInfo
                {
                    FileName = updaterPath,
                    Arguments = args,
                    UseShellExecute = true,
                    WorkingDirectory = appDir
                });

                return true;
            }
            catch (Exception ex)
            {
                Core.AppLogger.Error($"启动更新程序失败: {ex.Message}", "Updater");
                return false;
            }
        }
    }
}