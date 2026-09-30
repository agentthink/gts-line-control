using GtsTest.Core;
using GtsTest.Services.Authentication;
using GtsTest.Services.Data;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Windows.Forms;

namespace GtsTest
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                try
                {
                    // ---- 1. 日志初始化 ----
                    AppLogger.Initialize();
                    AppLogger.GlobalLogLevel = LogLevel.Info;
                    GtsModel.UseSimulation = true;

                    // ---- 2. WinForms 初始化 ----
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);

                    // ---- 3. 检查更新 ----
                    CheckForUpdatesAtStartup();

                    // ---- 4. DI 装配 ----
                    var services = new ServiceCollection();
                    services.AddGtsTestServices();
                    var provider = services.BuildServiceProvider();

                    // ---- 5. 静态兼容层指向 DI 实例 ----
                    AuditService.SetDefault(provider.GetRequiredService<IAuditService>());
                    ProductionService.SetDefault(provider.GetRequiredService<IProductionService>());
                    SessionManager.SetDefault(provider.GetRequiredService<ISessionService>());

                    // ---- 🆕 6. 启动数据库归档后台服务 ----
                    var archiveService = provider.GetRequiredService<DatabaseArchiveService>();
                    archiveService.Start();

                    // ---- 7. 初始化默认管理员 ----
                    var authService = provider.GetRequiredService<IAuthenticationService>();
                    var defaultPwd = authService.EnsureDefaultAdmin();
                    if (!string.IsNullOrEmpty(defaultPwd))
                    {
                        MessageBox.Show(
                            $"默认管理员账号已创建！\n\n用户名: admin\n密码: {defaultPwd}\n\n请妥善保管此密码，登录后立即修改。",
                            "⚠️ 初始管理员密码",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                        AppLogger.Info($"初始管理员密码: {defaultPwd}", "Auth");
                    }
                    else
                    {
                        AppLogger.Info("管理员账号已存在，无需创建", "Auth");
                    }

                    // ---- 8. 从 DI 容器解析主窗体并运行 ----
                    Application.Run(provider.GetRequiredService<Form1>());
                }
                finally
                {
                    AppLogger.Shutdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"程序启动失败: {ex.Message}\n\n{ex.StackTrace}",
                    "错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static void CheckForUpdatesAtStartup()
        {
            try
            {
                var config = GtsTest.Updater.UpdateConfig.Load();
                if (!config.Enabled || !config.AutoCheckOnStartup)
                {
                    AppLogger.Info("自动更新未启用，跳过检查", "Updater");
                    return;
                }

                AppLogger.Info($"开始检查更新: {config.ManifestUrl}", "Updater");

                var task = GtsTest.Updater.UpdateChecker.CheckAsync(config);
                if (!task.Wait(TimeSpan.FromSeconds(8)))
                {
                    AppLogger.Warn("检查更新超时，跳过", "Updater");
                    return;
                }

                var result = task.Result;
                if (!result.HasUpdate || result.Manifest == null)
                {
                    AppLogger.Info($"无新版本 (当前 {GtsTest.Updater.UpdateConfig.GetCurrentVersion()})", "Updater");
                    return;
                }

                using var dlg = new GtsTest.Updater.UpdateDialog(result.Manifest);
                var dialogResult = dlg.ShowDialog();

                if (dialogResult == DialogResult.OK && !string.IsNullOrEmpty(dlg.DownloadedPackagePath))
                {
                    if (GtsTest.Updater.UpdateLauncher.Launch(dlg.DownloadedPackagePath, restartAfter: true))
                    {
                        AppLogger.Info("即将退出主程序以完成更新...", "Updater");
                        Thread.Sleep(500);
                        Environment.Exit(0);
                    }
                    else
                    {
                        MessageBox.Show("启动更新程序失败，请手动更新。", "更新失败",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else if (dialogResult == DialogResult.Abort)
                {
                    AppLogger.Info("用户选择退出（强制更新）", "Updater");
                    Environment.Exit(0);
                }
                else
                {
                    AppLogger.Info("用户跳过了本次更新", "Updater");
                }
            }
            catch (Exception ex)
            {
                AppLogger.Warn($"检查更新异常（已忽略）: {ex.Message}", "Updater");
            }
        }
    }
}