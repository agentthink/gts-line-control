using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace GtsTest.Core
{
    /// <summary>
    /// 日志级别（数值越大越严重）
    /// </summary>
    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warn = 3,
        Error = 4,
        Fatal = 5
    }

    /// <summary>
    /// 日志条目：在队列中传递完整信息
    /// </summary>
    internal sealed class LogEntry
    {
        public LogLevel Level { get; }
        public string Line { get; }
        public string? Category { get; }

        public LogEntry(LogLevel level, string line, string? category)
        {
            Level = level;
            Line = line;
            Category = category;
        }
    }

    /// <summary>
    /// 工业级日志管理器（单例、后台写入）。
    /// OnLogReceived 在日志实际写入文件之后触发（写线程中同步调用）。
    /// 订阅者若要更新 UI，请在处理器中 Marshal 回 UI 线程（BeginInvoke / SynchronizationContext.Post）。
    /// </summary>
    public static class AppLogger
    {
        // ---------- 配置参数（支持运行时修改） ----------
        public static LogLevel GlobalLogLevel { get; set; } = LogLevel.Info; // 运行时动态调整
        public static int MaxFileSizeMB { get; set; } = 10;                  // 单文件最大 10MB
        public static int RetentionDays { get; set; } = 30;                 // 保留最近 30 天
        public static string LogDirectory { get; private set; } = "";

        // ---------- 事件（推送到 UI） ----------
        // 现在在“写盘成功后”触发，调用发生在写线程上（同步）。订阅者应尽快返回或自行在内部 Marshal 到 UI 线程。
        public static event Action<LogLevel, string, string?>? OnLogReceived; // 参数：级别, 消息, 类别

        // 内部实现
        private static readonly Channel<LogEntry> _channel = Channel.CreateUnbounded<LogEntry>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
        private static CancellationTokenSource? _writerCts;
        private static Task? _writerTask;
        private static readonly Encoding _encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        private static StreamWriter? _writer;
        private static string _currentLogFile = "";
        private static long _currentFileBytes = 0;
        private static readonly object _fileLock = new();
        private static bool _initialized = false;

        /// <summary>
        /// 当前用户（可选，供日志标注）
        /// </summary>
        public static string CurrentUser { get; set; } = "未登录";

        /// <summary>
        /// 初始化日志系统（可多次调用，但只会启动一次 writer）
        /// </summary>
        public static void Initialize(string logDirectory = "Logs")
        {
            if (_initialized) return;

            lock (_fileLock)
            {
                if (_initialized) return;
                LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logDirectory);
                if (!Directory.Exists(LogDirectory))
                    Directory.CreateDirectory(LogDirectory);

                CleanupOldLogs();

                PrepareLogFile();

                _writerCts = new CancellationTokenSource();
                _writerTask = Task.Run(() => LogWriterLoopAsync(_writerCts.Token));

                _initialized = true;
            }
        }

        /// <summary>
        /// 记录一条日志（线程安全、非阻塞）：只入队，不触发事件。
        /// </summary>
        public static void Log(string message, LogLevel level = LogLevel.Info, string? category = null)
        {
            if (level < GlobalLogLevel) return;

            string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string levelStr = level.ToString().ToUpper();
            string categoryTag = string.IsNullOrEmpty(category) ? "" : $"[{category}] ";
            string userTag = string.IsNullOrEmpty(CurrentUser) ? "" : $" [User:{CurrentUser}]";
            string logLine = $"[{timeStamp}] [{levelStr}] {categoryTag}{message}{userTag}";

            // 非阻塞地写入队列（Channel 是无界的，TryWrite 应当成功）
            // 若你希望有回压/有界队列策略，可改为有界 Channel 并在 TryWrite 失败时采取降级措施。
            _channel.Writer.TryWrite(new LogEntry(level, logLine, category));
        }

        // 便捷方法
        public static void Info(string msg, string? category = null) => Log(msg, LogLevel.Info, category);
        public static void Warn(string msg, string? category = null) => Log(msg, LogLevel.Warn, category);
        public static void Error(string msg, string? category = null) => Log(msg, LogLevel.Error, category);
        public static void Debug(string msg, string? category = null) => Log(msg, LogLevel.Debug, category);
        public static void Fatal(string msg, string? category = null) => Log(msg, LogLevel.Fatal, category);
        public static void Trace(string msg, string? category = null) => Log(msg, LogLevel.Trace, category);

        /// <summary>
        /// 后台写入循环：从 Channel 读取并写入文件，负责滚动和“写盘后触发事件”。
        /// 事件触发在这里（写成功后同步调用），因此订阅者的处理必须快或内部 Marshal 到 UI 线程。
        /// </summary>
        private static async Task LogWriterLoopAsync(CancellationToken ct)
        {
            try
            {
                // 打开初始文件
                lock (_fileLock)
                {
                    var fs = new FileStream(_currentLogFile, FileMode.Append, FileAccess.Write, FileShare.Read);
                    _writer = new StreamWriter(fs, _encoding) { AutoFlush = true };
                    _currentFileBytes = fs.Length;
                }

                await foreach (var entry in _channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
                {
                    try
                    {
                        var line = entry.Line;
                        var bytesLength = _encoding.GetByteCount(line + Environment.NewLine);

                        // 检查是否需要滚动（按字节）
                        if (_currentFileBytes + bytesLength > MaxFileSizeMB * 1024L * 1024L)
                        {
                            // 进行滚动（同步执行，写线程上）
                            RolloverLogFile();
                        }

                        try
                        {
                            // 写入到文件（同步 await）
                            await _writer!.WriteLineAsync(line).ConfigureAwait(false);
                            _currentFileBytes += bytesLength;
                        }
                        catch (Exception writeEx)
                        {
                            // 尝试重建 writer 一次并重试写入
                            try
                            {
                                lock (_fileLock)
                                {
                                    _writer?.Dispose();
                                    var fs = new FileStream(_currentLogFile, FileMode.Append, FileAccess.Write, FileShare.Read);
                                    _writer = new StreamWriter(fs, _encoding) { AutoFlush = true };
                                }
                                await _writer.WriteLineAsync(line).ConfigureAwait(false);
                                _currentFileBytes = new FileInfo(_currentLogFile).Length;
                            }
                            catch (Exception retryEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"日志写入失败: {writeEx}; 重试失败: {retryEx}");
                            }
                        }

                        // ===== 在写入成功后触发事件（同步在写线程上） =====
                        var evt = OnLogReceived;
                        if (evt != null)
                        {
                            try
                            {
                                evt.Invoke(entry.Level, line, entry.Category);
                            }
                            catch (Exception evEx)
                            {
                                // 事件处理异常不应影响日志系统
                                System.Diagnostics.Debug.WriteLine($"OnLogReceived 处理异常: {evEx}");
                            }
                        }
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"日志循环处理异常: {ex}");
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"日志写入任务异常: {ex}");
            }
            finally
            {
                try { _writer?.Dispose(); } catch { }
            }
        }

        /// <summary>
        /// 准备当前日志文件路径并设置 _currentLogFile
        /// </summary>
        private static void PrepareLogFile()
        {
            DateTime today = DateTime.Today;
            string dateStr = today.ToString("yyyy-MM-dd");
            _currentLogFile = Path.Combine(LogDirectory, $"AppLog_{dateStr}.txt");
            if (!File.Exists(_currentLogFile))
            {
                // 创建空文件并设置初始大小为 0
                try { File.WriteAllText(_currentLogFile, ""); } catch { }
                _currentFileBytes = 0;
            }
            else
            {
                try { _currentFileBytes = new FileInfo(_currentLogFile).Length; } catch { _currentFileBytes = 0; }
            }
        }

        /// <summary>
        /// 执行日志滚动（重命名旧文件），线程安全（应在单一读者上下文调用）
        /// </summary>
        private static void RolloverLogFile()
        {
            lock (_fileLock)
            {
                try
                {
                    _writer?.Dispose();
                }
                catch { }

                string baseName = Path.GetFileNameWithoutExtension(_currentLogFile); // AppLog_2026-08-08
                string dir = Path.GetDirectoryName(_currentLogFile) ?? LogDirectory;
                int index = 1;
                string newFile;
                do
                {
                    newFile = Path.Combine(dir, $"{baseName}_{index:D3}.txt");
                    index++;
                } while (File.Exists(newFile));

                try
                {
                    File.Move(_currentLogFile, newFile);
                }
                catch (Exception ex)
                {
                    // 如果移动失败，尝试以时间戳命名
                    try
                    {
                        var alt = Path.Combine(dir, $"{baseName}_{DateTime.Now:HHmmss}.txt");
                        File.Move(_currentLogFile, alt);
                    }
                    catch
                    {
                        System.Diagnostics.Debug.WriteLine($"日志滚动移动失败: {ex}");
                    }
                }

                // 重新创建当前日志文件并打开 writer
                try
                {
                    var fs = new FileStream(_currentLogFile, FileMode.Create, FileAccess.Write, FileShare.Read);
                    _writer = new StreamWriter(fs, _encoding) { AutoFlush = true };
                    _currentFileBytes = 0;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"日志滚动后打开新文件失败: {ex}");
                    _writer = null;
                    _currentFileBytes = 0;
                }
            }
        }

        /// <summary>
        /// 清理过期日志（按创建时间）
        /// </summary>
        private static void CleanupOldLogs()
        {
            try
            {
                var cutoff = DateTime.Now.AddDays(-RetentionDays);
                var files = Directory.GetFiles(LogDirectory, "AppLog_*.txt", SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    try
                    {
                        if (File.GetCreationTime(file) < cutoff)
                        {
                            File.Delete(file);
                        }
                    }
                    catch { /* 忽略单文件删除失败 */ }
                }
            }
            catch { /* 清理失败不影响主程序 */ }
        }

        /// <summary>
        /// 关闭日志系统并等待后台任务结束（在应用退出时调用）
        /// </summary>
        public static void Shutdown(int waitMs = 5000)
        {
            if (!_initialized) return;

            try
            {
                _channel.Writer.Complete();

                try { _writerCts?.Cancel(); } catch { }

                if (_writerTask != null)
                {
                    try
                    {
                        _writerTask.Wait(waitMs);
                    }
                    catch { /* ignore */ }
                }

                // ✅ 修复：清空静态事件（防止引用泄漏）
                OnLogReceived = null;
            }
            catch { /* ignore */ }
            finally
            {
                try { _writer?.Dispose(); } catch { }
                try { _writerCts?.Dispose(); } catch { }
                _initialized = false;
            }
        }
    }
}