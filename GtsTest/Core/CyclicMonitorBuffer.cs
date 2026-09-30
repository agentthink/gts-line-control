using System.Collections.Concurrent;
using System.IO;

namespace GtsTest.Core
{
    /// <summary>
    /// 循环监控缓冲区（黑匣子）
    /// 仅内存操作，不写硬盘，达到最大容量后自动覆盖最旧数据
    /// </summary>
    public static class CyclicMonitorBuffer
    {
        private static readonly ConcurrentQueue<string> _buffer = new();
        private static int _maxSize = 30000; // 默认3万条
        private static readonly object _lock = new();

        /// <summary>
        /// 设置最大缓存条数（建议在程序启动时设置一次）
        /// </summary>
        public static void SetMaxSize(int maxSize)
        {
            lock (_lock)
            {
                _maxSize = maxSize;
            }
        }

        /// <summary>
        /// 添加一条监控记录（线程安全，纯内存操作）
        /// </summary>
        public static void Log(string message)
        {
            _buffer.Enqueue(message);
            if (_buffer.Count > _maxSize)
            {
                _buffer.TryDequeue(out _);
            }
        }

        /// <summary>
        /// 获取当前缓冲区的所有记录（快照）
        /// </summary>
        public static string[] Snapshot()
        {
            return _buffer.ToArray();
        }

        /// <summary>
        /// 清空缓冲区
        /// </summary>
        public static void Clear()
        {
            _buffer.Clear();
        }

        /// <summary>
        /// 将当前缓冲区内容转储到文件（带时间戳），并记录审计日志
        /// </summary>
        /// <param name="reason">触发原因（如报警、手动导出）</param>
        /// <returns>转储文件路径，若失败返回 null</returns>
        public static string? DumpToFile(string reason)
        {
            try
            {
                var snapshot = Snapshot();
                if (snapshot.Length == 0)
                {
                    AppLogger.Warn("监控缓冲区为空，跳过转储", "Monitor");
                    return null;
                }

                string dir = AppLogger.LogDirectory; // 复用日志目录
                string fileName = $"BlackBox_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string filePath = Path.Combine(dir, fileName);

                var lines = new List<string>
                {
                    $"=== 黑匣子转储 触发原因: {reason} 时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ===",
                    $"=== 共 {snapshot.Length} 条记录 ===",
                    "========================================"
                };
                lines.AddRange(snapshot);
                File.WriteAllLines(filePath, lines);

                // 通过审计日志记录转储事件
                AppLogger.Info($"黑匣子已转储: {filePath} (条数: {snapshot.Length})", "Operation");
                return filePath;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"黑匣子转储失败: {ex.Message}", "Operation ");
                return null;
            }
        }
    }
}