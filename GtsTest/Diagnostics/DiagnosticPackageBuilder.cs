using Dapper;
using GtsTest.Core;
using GtsTest.Data;
using GtsTest.Models;
using GtsTest.Services.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace GtsTest.Diagnostics
{
    /// <summary>
    /// 一键诊断包构建器
    /// 收集运行现场的所有关键信息，打包成 zip，供现场人员一键发给研发
    /// </summary>
    public static class DiagnosticPackageBuilder
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>日志打包天数：默认最近 3 天</summary>
        public static int LogDaysToInclude { get; set; } = 3;

        /// <summary>审计记录打包条数：默认 200 条</summary>
        public static int AuditRecordsToInclude { get; set; } = 200;

        /// <summary>
        /// 构建诊断包并返回 zip 路径；失败返回 null
        /// </summary>
        /// <param name="deviceManager">设备管理器（用于抓设备快照）</param>
        /// <param name="mainForm">主窗体（用于截图，可为 null）</param>
        /// <param name="customOutputDir">自定义输出目录（默认桌面）</param>
        public static string? Build(DeviceManager? deviceManager, Form? mainForm = null, string? customOutputDir = null)
        {
            string? tempDir = null;
            try
            {
                // ---- 0. 准备临时目录 ----
                string sessionId = Guid.NewGuid().ToString("N").Substring(0, 8);
                tempDir = Path.Combine(Path.GetTempPath(), $"GtsTestDiag_{sessionId}");
                Directory.CreateDirectory(tempDir);

                var configDir = Path.Combine(tempDir, "config");
                var logsDir = Path.Combine(tempDir, "logs");
                Directory.CreateDirectory(configDir);
                Directory.CreateDirectory(logsDir);

                // ---- 1. 收集系统信息 ----
                File.WriteAllText(Path.Combine(tempDir, "system-info.txt"), BuildSystemInfo(), Encoding.UTF8);
                File.WriteAllText(Path.Combine(tempDir, "process-info.txt"), BuildProcessInfo(), Encoding.UTF8);

                // ---- 2. 设备快照 ----
                if (deviceManager != null)
                {
                    var snapshot = BuildDeviceSnapshot(deviceManager);
                    File.WriteAllText(Path.Combine(tempDir, "devices-snapshot.json"),
                        JsonSerializer.Serialize(snapshot, _jsonOptions), Encoding.UTF8);

                    // 报警 CSV
                    try
                    {
                        var alarmManager = deviceManager.AlarmManager;
                        var alarms = alarmManager?.GetActiveAlarms()?.ToList() ?? new List<AlarmRecord>();
                        File.WriteAllText(Path.Combine(tempDir, "alarms-active.csv"),
                            BuildAlarmsCsv(alarms), Encoding.UTF8);
                    }
                    catch (Exception ex)
                    {
                        File.WriteAllText(Path.Combine(tempDir, "alarms-active.csv"),
                            $"# 收集报警失败: {ex.Message}", Encoding.UTF8);
                    }
                }

                // ---- 3. 审计记录 ----
                try
                {
                    var auditService = new AuditServiceImpl();
                    var recent = auditService.GetRecent(AuditRecordsToInclude);
                    File.WriteAllText(Path.Combine(tempDir, "audit-recent.csv"),
                        BuildAuditCsv(recent), Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    File.WriteAllText(Path.Combine(tempDir, "audit-recent.csv"),
                        $"# 收集审计失败: {ex.Message}", Encoding.UTF8);
                }

                // ---- 4. 数据库状态 ----
                File.WriteAllText(Path.Combine(tempDir, "database-status.txt"),
                    BuildDatabaseStatus(), Encoding.UTF8);

                // ---- 5. 复制配置文件（脱敏） ----
                CopyConfigFiles(configDir);

                // ---- 6. 复制日志文件 ----
                CopyLogFiles(logsDir);

                // ---- 7. 截图 ----
                if (mainForm != null && !mainForm.IsDisposed)
                {
                    try
                    {
                        CaptureScreenshot(mainForm, Path.Combine(tempDir, "screenshot.png"));
                    }
                    catch { /* 截图失败不影响整体 */ }
                }

                // ---- 8. 生成 summary.txt ----
                File.WriteAllText(Path.Combine(tempDir, "summary.txt"),
                    BuildSummary(deviceManager), Encoding.UTF8);

                // ---- 9. 打包 zip ----
                string outputDir = customOutputDir ?? GetDefaultOutputDir();
                if (!Directory.Exists(outputDir))
                    Directory.CreateDirectory(outputDir);

                string zipName = $"GtsTest_Diag_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
                string zipPath = Path.Combine(outputDir, zipName);

                if (File.Exists(zipPath))
                    File.Delete(zipPath);

                ZipFile.CreateFromDirectory(tempDir, zipPath, CompressionLevel.Optimal, false);

                AppLogger.Info($"✅ 诊断包已生成: {zipPath} ({new FileInfo(zipPath).Length / 1024} KB)", "Diagnostic");
                return zipPath;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"❌ 诊断包构建失败: {ex}", "Diagnostic");
                return null;
            }
            finally
            {
                // 清理临时目录
                if (tempDir != null && Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, recursive: true); } catch { }
                }
            }
        }

        // ================================================================
        // 系统信息
        // ================================================================
        private static string BuildSystemInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("========== 系统信息 ==========");
            sb.AppendLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"机器名: {Environment.MachineName}");
            sb.AppendLine($"用户名: {Environment.UserName}");
            sb.AppendLine($"OS 版本: {Environment.OSVersion}");
            sb.AppendLine($"64 位系统: {Environment.Is64BitOperatingSystem}");
            sb.AppendLine($"64 位进程: {Environment.Is64BitProcess}");
            sb.AppendLine($"处理器数: {Environment.ProcessorCount}");
            sb.AppendLine($".NET 版本: {Environment.Version}");
            sb.AppendLine($"系统目录: {Environment.SystemDirectory}");
            sb.AppendLine();

            // 磁盘空间
            sb.AppendLine("========== 磁盘空间 ==========");
            try
            {
                foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
                {
                    sb.AppendLine($"{drive.Name} [{drive.DriveType}] 总 {FormatBytes(drive.TotalSize)}，剩余 {FormatBytes(drive.AvailableFreeSpace)}");
                }
            }
            catch (Exception ex) { sb.AppendLine($"获取磁盘信息失败: {ex.Message}"); }
            sb.AppendLine();

            // 程序集版本
            sb.AppendLine("========== 程序集版本 ==========");
            try
            {
                var assembly = Assembly.GetEntryAssembly();
                sb.AppendLine($"产品: {assembly?.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "N/A"}");
                sb.AppendLine($"版本: {assembly?.GetName().Version}");
                sb.AppendLine($"文件名: {assembly?.Location}");
                sb.AppendLine($"编译时间: {File.GetLastWriteTime(assembly?.Location ?? "")}");
            }
            catch { }

            return sb.ToString();
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        // ================================================================
        // 进程信息
        // ================================================================
        private static string BuildProcessInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("========== 进程信息 ==========");
            try
            {
                using var proc = Process.GetCurrentProcess();
                sb.AppendLine($"进程名: {proc.ProcessName}");
                sb.AppendLine($"PID: {proc.Id}");
                sb.AppendLine($"启动时间: {proc.StartTime:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"运行时长: {(DateTime.Now - proc.StartTime):dd\\.hh\\:mm\\:ss}");
                sb.AppendLine($"工作集: {FormatBytes(proc.WorkingSet64)}");
                sb.AppendLine($"私有内存: {FormatBytes(proc.PrivateMemorySize64)}");
                sb.AppendLine($"虚拟内存: {FormatBytes(proc.VirtualMemorySize64)}");
                sb.AppendLine($"线程数: {proc.Threads.Count}");
                sb.AppendLine($"句柄数: {proc.HandleCount}");
                sb.AppendLine($"GC 已用内存: {FormatBytes(GC.GetTotalMemory(false))}");
                sb.AppendLine($"GC 代数: {GC.MaxGeneration}");
            }
            catch (Exception ex) { sb.AppendLine($"获取进程信息失败: {ex.Message}"); }
            return sb.ToString();
        }

        // ================================================================
        // 设备快照
        // ================================================================
        private static object BuildDeviceSnapshot(DeviceManager dm)
        {
            var devices = dm.GetAllDevices();
            return new
            {
                Timestamp = DateTime.Now,
                DeviceCount = devices.Count,
                Devices = devices.Select(d => new
                {
                    d.Config.DeviceId,
                    d.Config.Name,
                    d.Config.Enabled,
                    Axis = d.Config.Axis,
                    TargetCount = d.Config.TargetCount,
                    CurrentCount = d.Config.CurrentCount,
                    IsOnline = d.IsOnline,
                    State = d.StateMachine.CurrentState.ToString(),
                    CurrentStep = d.CurrentStep,
                    LastError = d.LastError,
                    ModbusConnected = d.ModbusClient?.IsConnected ?? false,
                    WorkflowName = d.CurrentWorkflowName,
                    BoundWorkflowName = d.BoundWorkflowName,
                    CurrentStepIndex = d.CurrentStepIndex,
                    Watchdog = new
                    {
                        d.Watchdog.IsTimeout,
                        d.Watchdog.RemainingMs,
                        LastFeedTime = d.Watchdog.LastFeedTime
                    },
                    ModbusConfig = new
                    {
                        Protocol = d.Config.Modbus.Protocol.ToString(),
                        d.Config.Modbus.IpAddress,
                        d.Config.Modbus.Port,
                        d.Config.Modbus.SlaveAddress
                    }
                }).ToList()
            };
        }

        // ================================================================
        // 报警 CSV
        // ================================================================
        private static string BuildAlarmsCsv(List<AlarmRecord> alarms)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Id,DeviceId,Severity,Timestamp,IsAcknowledged,IsResolved,Message,AcknowledgedBy,ResolvedBy");
            foreach (var a in alarms)
            {
                sb.AppendLine(string.Join(",",
                    a.Id,
                    Csv(a.DeviceId),
                    Csv(a.Severity.ToString()),
                    Csv(a.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")),
                    a.IsAcknowledged ? 1 : 0,
                    a.IsResolved ? 1 : 0,
                    Csv(a.Message),
                    Csv(a.AcknowledgedBy),
                    Csv(a.ResolvedBy)
                ));
            }
            return sb.ToString();
        }

        // ================================================================
        // 审计 CSV
        // ================================================================
        private static string BuildAuditCsv(List<AuditLog> logs)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Id,UserId,Username,ActionType,Timestamp,Detail");
            foreach (var l in logs)
            {
                sb.AppendLine(string.Join(",",
                    l.Id,
                    l.UserId,
                    Csv(l.Username),
                    Csv(l.ActionType),
                    Csv(l.Timestamp),
                    Csv(l.Detail)
                ));
            }
            return sb.ToString();
        }

        /// <summary>CSV 字段转义</summary>
        private static string Csv(string? s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r'))
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }

        // ================================================================
        // 数据库状态
        // ================================================================
        private static string BuildDatabaseStatus()
        {
            var sb = new StringBuilder();
            sb.AppendLine("========== 数据库状态 ==========");
            sb.AppendLine($"Provider: {DbContextFactory.CurrentProvider}");
            sb.AppendLine($"连接串: {MaskConnectionString(DbContextFactory.CurrentConnectionString)}");
            sb.AppendLine();

            try
            {
                var factory = new DbConnectionFactory();
                using var conn = factory.CreateConnection();
                conn.Open();

                string[] tables = { "Users", "AlarmRecords", "AuditLogs", "ProductionRecords", "MesPendingRecords" };
                sb.AppendLine("表名                          行数");
                sb.AppendLine("---------------------------------");
                foreach (var table in tables)
                {
                    try
                    {
                        var count = conn.ExecuteScalar<int>($"SELECT COUNT(*) FROM {table}");
                        sb.AppendLine($"{table,-30} {count}");
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine($"{table,-30} 查询失败: {ex.Message}");
                    }
                }
                sb.AppendLine();
                sb.AppendLine("查询时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            catch (Exception ex)
            {
                sb.AppendLine($"连接数据库失败: {ex.Message}");
            }

            return sb.ToString();
        }

        private static string MaskConnectionString(string connStr)
        {
            if (string.IsNullOrEmpty(connStr)) return "";
            return System.Text.RegularExpressions.Regex.Replace(
                connStr, @"(password|pwd)\s*=\s*[^;]*", "$1=***",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        // ================================================================
        // 复制配置文件（脱敏）
        // ================================================================
        private static void CopyConfigFiles(string targetDir)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // appsettings.json（脱敏 password）
            CopyAndMask(Path.Combine(baseDir, "appsettings.json"),
                Path.Combine(targetDir, "appsettings.json"),
                @"(password|pwd)""?\s*[:=]\s*""[^""]*""");

            // devices.json（不脱敏，只是设备列表）
            CopyPlain(Path.Combine(baseDir, "devices.json"),
                Path.Combine(targetDir, "devices.json"));

            // mes_config.json（脱敏 Token）
            CopyAndMask(Path.Combine(baseDir, "mes_config.json"),
                Path.Combine(targetDir, "mes_config.json"),
                @"""Token""\s*:\s*""[^""]*""");
        }

        private static void CopyPlain(string src, string dst)
        {
            try
            {
                if (File.Exists(src))
                    File.Copy(src, dst, overwrite: true);
            }
            catch { }
        }

        private static void CopyAndMask(string src, string dst, string pattern)
        {
            try
            {
                if (!File.Exists(src)) return;
                string text = File.ReadAllText(src);
                // 把匹配到的敏感字段替换为 ***
                text = System.Text.RegularExpressions.Regex.Replace(
                    text,
                    pattern,
                    m => m.Value.Contains("\"")
                        ? System.Text.RegularExpressions.Regex.Replace(m.Value, @":\s*""[^""]*""", ": \"***\"")
                        : System.Text.RegularExpressions.Regex.Replace(m.Value, @"=[^;]*", "=***"),
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                File.WriteAllText(dst, text, Encoding.UTF8);
            }
            catch { }
        }

        // ================================================================
        // 复制日志文件
        // ================================================================
        private static void CopyLogFiles(string targetDir)
        {
            try
            {
                string logDir = AppLogger.LogDirectory;
                if (string.IsNullOrEmpty(logDir) || !Directory.Exists(logDir)) return;

                var cutoff = DateTime.Now.AddDays(-LogDaysToInclude);
                var files = Directory.GetFiles(logDir, "AppLog_*.txt")
                    .Select(f => new FileInfo(f))
                    .Where(fi => fi.LastWriteTime >= cutoff)
                    .OrderByDescending(fi => fi.LastWriteTime)
                    .ToList();

                foreach (var fi in files)
                {
                    try
                    {
                        File.Copy(fi.FullName, Path.Combine(targetDir, fi.Name), overwrite: true);
                    }
                    catch { }
                }
            }
            catch { }
        }

        // ================================================================
        // 截图
        // ================================================================
        private static void CaptureScreenshot(Form form, string filePath)
        {
            if (form == null || form.IsDisposed || form.Width <= 0 || form.Height <= 0) return;

            using var bmp = new Bitmap(form.Width, form.Height);
            form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
            bmp.Save(filePath, ImageFormat.Png);
        }

        // ================================================================
        // summary.txt
        // ================================================================
        private static string BuildSummary(DeviceManager? dm)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=================================================");
            sb.AppendLine("    GtsTest 诊断包 - 快速总览");
            sb.AppendLine("=================================================");
            sb.AppendLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            if (dm != null)
            {
                var devices = dm.GetAllDevices();
                sb.AppendLine($"设备总数: {devices.Count}");
                sb.AppendLine($"在线设备: {devices.Count(d => d.IsOnline)}");
                sb.AppendLine($"运行设备: {devices.Count(d => d.StateMachine.CurrentState == DeviceState.Running)}");
                sb.AppendLine($"故障设备: {devices.Count(d => d.StateMachine.CurrentState == DeviceState.Error)}");
                sb.AppendLine();

                sb.AppendLine("---- 设备明细 ----");
                foreach (var d in devices)
                {
                    sb.AppendLine($"[{d.Config.Name}]");
                    sb.AppendLine($"  状态: {d.StateMachine.CurrentState}");
                    sb.AppendLine($"  在线: {(d.IsOnline ? "是" : "否")}");
                    sb.AppendLine($"  产量: {d.Config.CurrentCount}/{d.Config.TargetCount}");
                    sb.AppendLine($"  步骤: {d.CurrentStep}");
                    if (!string.IsNullOrEmpty(d.LastError))
                        sb.AppendLine($"  最后错误: {d.LastError}");
                    sb.AppendLine();
                }
            }

            sb.AppendLine("---- 包内文件 ----");
            sb.AppendLine("summary.txt              本文件");
            sb.AppendLine("system-info.txt          系统信息");
            sb.AppendLine("process-info.txt         进程信息");
            sb.AppendLine("devices-snapshot.json    设备快照");
            sb.AppendLine("alarms-active.csv        活动报警");
            sb.AppendLine("audit-recent.csv         最近审计记录");
            sb.AppendLine("database-status.txt      数据库状态");
            sb.AppendLine("config/                  配置文件（已脱敏）");
            sb.AppendLine("logs/                    日志文件");
            sb.AppendLine("screenshot.png           主界面截图（若采集成功）");

            return sb.ToString();
        }

        // ================================================================
        // 输出目录
        // ================================================================
        private static string GetDefaultOutputDir()
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                if (!string.IsNullOrEmpty(desktop) && Directory.Exists(desktop))
                    return Path.Combine(desktop, "GtsTest_Diagnostics");
            }
            catch { }

            // fallback：程序目录
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Diagnostics");
        }
    }
}