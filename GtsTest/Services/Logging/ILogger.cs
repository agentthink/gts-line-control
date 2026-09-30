// 文件: Services/ILogger.cs
using GtsTest.Core;

namespace GtsTest.Services.Logging
{
    public interface ILogger
    {
        void Info(string msg, string category = null);
        void Warn(string msg, string category = null);
        void Error(string msg, string category = null);
        void Debug(string msg, string category = null);
        void Fatal(string msg, string category = null);
        void Log(string msg, LogLevel level, string category = null);
    }

}