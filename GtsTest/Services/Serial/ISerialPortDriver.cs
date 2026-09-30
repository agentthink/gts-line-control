using System;

namespace GtsTest.Services.Serial
{
    /// <summary>
    /// 串口驱动接口
    /// </summary>
    public interface ISerialPortDriver : IDisposable
    {
        /// <summary>配置</summary>
        SerialPortConfig Config { get; }

        /// <summary>是否已打开</summary>
        bool IsOpen { get; }

        /// <summary>打开状态变化（true=已打开，false=已关闭）</summary>
        event EventHandler<bool>? ConnectionStateChanged;

        /// <summary>
        /// 收到完整一帧数据（已按 FrameType 分包）
        /// </summary>
        event EventHandler<byte[]>? FrameReceived;

        /// <summary>收到原始字节流（未分包，调试用）</summary>
        event EventHandler<byte[]>? RawDataReceived;

        /// <summary>错误事件</summary>
        event EventHandler<string>? ErrorOccurred;

        /// <summary>打开串口</summary>
        bool Open();

        /// <summary>关闭串口</summary>
        void Close();

        /// <summary>发送原始字节</summary>
        bool Send(byte[] data);

        /// <summary>发送字符串（自动编码）</summary>
        bool Send(string text, System.Text.Encoding? encoding = null);

        /// <summary>
        /// 发送并等待响应（同步阻塞，带超时）
        /// </summary>
        byte[]? SendAndWaitResponse(byte[] request, int timeoutMs,
            Func<byte[], bool>? frameMatcher = null);
    }
}