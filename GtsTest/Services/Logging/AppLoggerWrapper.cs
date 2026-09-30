using GtsTest.Core;

namespace GtsTest.Services.Logging
{
    public class AppLoggerWrapper : ILogger
    {
        public void Info(string msg, string category = null) => AppLogger.Info(msg, category);
        public void Warn(string msg, string category = null) => AppLogger.Warn(msg, category);
        public void Error(string msg, string category = null) => AppLogger.Error(msg, category);
        public void Debug(string msg, string category = null) => AppLogger.Debug(msg, category);
        public void Fatal(string msg, string category = null) => AppLogger.Fatal(msg, category);

        public void Log(string msg, LogLevel level, string category = null)
        {
            // 直接调用，无需转换
            AppLogger.Log(msg, level, category);
        }
    }
}