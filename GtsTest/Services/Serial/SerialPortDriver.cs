using GtsTest.Core;
using GtsTest.Services.Serial.Frames;
using System;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GtsTest.Services.Serial
{
    /// <summary>
    /// 通用串口驱动（232/485）
    /// 采用「事件 + 轮询」双通道读取，兼容各种虚拟串口和 USB 转串口驱动
    /// </summary>
    public class SerialPortDriver : ISerialPortDriver
    {
        private readonly object _lock = new();
        private readonly object _sendLock = new();
        private readonly object _readLock = new();   // ⭐ 保护读取操作

        private SerialPort? _port;
        private ISerialFrameParser? _parser;
        private CancellationTokenSource? _cts;
        private Task? _pollTask;
        private Task? _reconnectTask;
        private volatile bool _isOpen;
        private volatile bool _disposed;

        // 用于 SendAndWaitResponse 的响应队列
        private readonly BlockingCollection<byte[]> _responseQueue = new();

        public SerialPortConfig Config { get; }
        public bool IsOpen => _isOpen;

        public event EventHandler<bool>? ConnectionStateChanged;
        public event EventHandler<byte[]>? FrameReceived;
        public event EventHandler<byte[]>? RawDataReceived;
        public event EventHandler<string>? ErrorOccurred;

        public SerialPortDriver(SerialPortConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
        }

        // ============================================================
        // 打开 / 关闭
        // ============================================================
        public bool Open()
        {
            lock (_lock)
            {
                if (_isOpen) return true;
                if (_disposed) return false;

                try
                {
                    _port = new SerialPort(Config.PortName, Config.BaudRate,
                        Config.Parity, Config.DataBits, Config.StopBits)
                    {
                        ReadTimeout = Config.ReadTimeoutMs,
                        WriteTimeout = Config.WriteTimeoutMs,
                        ReadBufferSize = 4096,
                        WriteBufferSize = 4096,
                        Handshake = Handshake.None,
                        DtrEnable = false,   // ⭐ 某些虚拟串口对 DTR/RTS 敏感，改为 false
                        RtsEnable = false,
                    };

                    _port.DataReceived += OnDataReceived;
                    _port.ErrorReceived += OnErrorReceived;
                    _port.Open();

                    // 创建帧解析器
                    _parser = CreateParser();

                    _cts = new CancellationTokenSource();
                    _isOpen = true;

                    ConnectionStateChanged?.Invoke(this, true);
                    AppLogger.Info($"✅ 串口已打开: {Config.PortName} @ {Config.BaudRate}, " +
                        $"数据位={Config.DataBits}, 停止位={Config.StopBits}, 校验={Config.Parity}, " +
                        $"帧格式={Config.FrameType}",
                        "SerialPort");

                    // ⭐ 启动兜底轮询线程（防止 DataReceived 事件丢失）
                    _pollTask = Task.Run(() => PollReadLoop(_cts.Token));

                    return true;
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"❌ 串口打开失败 [{Config.PortName}]: {ex.Message}", "SerialPort");
                    ErrorOccurred?.Invoke(this, ex.Message);
                    CleanupPort();
                    return false;
                }
            }
        }

        public void Close()
        {
            lock (_lock)
            {
                if (!_isOpen && _port == null) return;

                try { _cts?.Cancel(); } catch { }

                // 等待轮询线程退出
                try { _pollTask?.Wait(500); } catch { }

                _isOpen = false;
                CleanupPort();

                // 停止自动重连
                try { _reconnectTask?.Wait(500); } catch { }

                ConnectionStateChanged?.Invoke(this, false);
                AppLogger.Info($"🔌 串口已关闭: {Config.PortName}", "SerialPort");
            }
        }

        private void CleanupPort()
        {
            if (_port != null)
            {
                try { _port.DataReceived -= OnDataReceived; } catch { }
                try { _port.ErrorReceived -= OnErrorReceived; } catch { }
                try { if (_port.IsOpen) _port.Close(); } catch { }
                try { _port.Dispose(); } catch { }
                _port = null;
            }
            _parser?.Reset();
        }

        // ============================================================
        // ⭐ 统一读取：事件和轮询都走这里，通过锁避免重复读取
        // ============================================================
        private void TryReadAvailable(string source)
        {
            lock (_readLock)
            {
                var port = _port;
                if (port == null || !port.IsOpen) return;

                try
                {
                    int totalRead = 0;

                    // 循环读取直到缓冲区为空
                    while (true)
                    {
                        int avail = port.BytesToRead;
                        if (avail <= 0) break;

                        byte[] buf = new byte[avail];
                        int read = port.Read(buf, 0, avail);
                        if (read <= 0) break;

                        if (read < buf.Length)
                            buf = buf.Take(read).ToArray();

                        ProcessReceivedBytes(buf, source);
                        totalRead += read;
                    }

                    if (totalRead > 0)
                        AppLogger.Debug($"📥 [{Config.PortName}] 本次从 {source} 读取 {totalRead} 字节", "SerialPort");
                }
                catch (InvalidOperationException)
                {
                    // 端口关闭，忽略
                }
                catch (Exception ex)
                {
                    AppLogger.Warn($"⚠️ 串口读取异常 [{Config.PortName}]: {ex.Message}", "SerialPort");
                    HandlePortError(ex);
                }
            }
        }

        // ============================================================
        // ⭐ 事件回调（可能不触发，交给轮询兜底）
        // ============================================================
        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            TryReadAvailable("事件");
        }

        // ============================================================
        // ⭐ 轮询兜底线程（20ms 一次）
        // ============================================================
        private async Task PollReadLoop(CancellationToken ct)
        {
            AppLogger.Debug($"🔄 启动轮询读取线程 [{Config.PortName}]", "SerialPort");

            while (!ct.IsCancellationRequested && _isOpen)
            {
                try
                {
                    await Task.Delay(20, ct);
                    if (!_isOpen) break;

                    TryReadAvailable("轮询");
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    if (_isOpen)
                        AppLogger.Debug($"轮询线程异常: {ex.Message}", "SerialPort");
                }
            }

            AppLogger.Debug($"🛑 轮询读取线程退出 [{Config.PortName}]", "SerialPort");
        }

        // ============================================================
        // 处理接收到的字节
        // ============================================================
        private void ProcessReceivedBytes(byte[] buf, string source)
        {
            if (buf == null || buf.Length == 0) return;

            // 原始流事件
            try
            {
                RawDataReceived?.Invoke(this, buf);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"⚠️ RawDataReceived 处理异常: {ex.Message}", "SerialPort");
            }

            AppLogger.Debug($"📥 [{Config.PortName}] RX({source}): {ToHex(buf)}", "SerialPort");

            // 分包
            if (_parser != null)
            {
                try
                {
                    var frames = _parser.Push(buf);
                    foreach (var frame in frames)
                    {
                        try
                        {
                            FrameReceived?.Invoke(this, frame);
                        }
                        catch (Exception ex)
                        {
                            AppLogger.Error($"⚠️ FrameReceived 处理异常: {ex.Message}", "SerialPort");
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"⚠️ 帧解析异常: {ex.Message}", "SerialPort");
                }
            }
            else
            {
                // Raw 模式，直接抛出一整块
                try
                {
                    FrameReceived?.Invoke(this, buf);
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"⚠️ FrameReceived 处理异常: {ex.Message}", "SerialPort");
                }
            }
        }

        private void OnErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            AppLogger.Warn($"⚠️ 串口错误 [{Config.PortName}]: {e.EventType}", "SerialPort");
            HandlePortError(new Exception(e.EventType.ToString()));
        }

        // ============================================================
        // 发送
        // ============================================================
        public bool Send(byte[] data)
        {
            if (!_isOpen || _port == null || data == null || data.Length == 0)
                return false;

            lock (_sendLock)
            {
                try
                {
                    _port.Write(data, 0, data.Length);
                    AppLogger.Debug($"📤 [{Config.PortName}] TX: {ToHex(data)}", "SerialPort");
                    return true;
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"❌ 串口发送失败 [{Config.PortName}]: {ex.Message}", "SerialPort");
                    HandlePortError(ex);
                    return false;
                }
            }
        }

        public bool Send(string text, Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;
            return Send(encoding.GetBytes(text));
        }

        // ============================================================
        // 发送并等待响应
        // ============================================================
        public byte[]? SendAndWaitResponse(byte[] request, int timeoutMs,
            Func<byte[], bool>? frameMatcher = null)
        {
            if (!_isOpen) return null;

            // 清空旧响应
            while (_responseQueue.TryTake(out _)) { }

            EventHandler<byte[]> handler = (s, frame) =>
            {
                if (frameMatcher == null || frameMatcher(frame))
                    _responseQueue.TryAdd(frame);
            };
            FrameReceived += handler;

            try
            {
                if (!Send(request)) return null;

                if (_responseQueue.TryTake(out var resp, timeoutMs))
                    return resp;

                AppLogger.Warn($"⚠️ [{Config.PortName}] 等待响应超时 ({timeoutMs}ms)", "SerialPort");
                return null;
            }
            finally
            {
                FrameReceived -= handler;
            }
        }

        // ============================================================
        // 错误处理 + 自动重连
        // ============================================================
        private void HandlePortError(Exception ex)
        {
            if (!_isOpen) return;

            lock (_lock)
            {
                _isOpen = false;
                CleanupPort();
                ConnectionStateChanged?.Invoke(this, false);
            }

            if (Config.AutoReconnectIntervalMs > 0 && !_disposed)
            {
                StartReconnectLoop();
            }
        }

        private void StartReconnectLoop()
        {
            if (_reconnectTask != null && !_reconnectTask.IsCompleted) return;

            _reconnectTask = Task.Run(async () =>
            {
                while (!_disposed && !_isOpen)
                {
                    await Task.Delay(Config.AutoReconnectIntervalMs);
                    if (_disposed) return;

                    AppLogger.Info($"🔄 尝试重连串口 {Config.PortName}...", "SerialPort");
                    if (Open())
                    {
                        AppLogger.Info($"✅ 串口重连成功: {Config.PortName}", "SerialPort");
                        return;
                    }
                }
            });
        }

        // ============================================================
        // 帧解析器工厂
        // ============================================================
        private ISerialFrameParser CreateParser()
        {
            switch (Config.FrameType)
            {
                case SerialFrameType.FixedLength:
                    return new FixedLengthParser(Config.FixedFrameLength);

                case SerialFrameType.LengthField:
                    return new LengthFieldParser(
                        Config.FrameHeader,
                        Config.LengthFieldOffset,
                        Config.LengthFieldSize,
                        Config.LengthIncludesHeader,
                        Config.UseCrc16);

                case SerialFrameType.Delimiter:
                    return new DelimiterParser(Config.Delimiter);

                case SerialFrameType.Raw:
                default:
                    return null!;   // 不分包
            }
        }

        // ============================================================
        // 工具
        // ============================================================
        private static string ToHex(byte[] data)
        {
            if (data == null || data.Length == 0) return "";
            return BitConverter.ToString(data).Replace("-", " ");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Close();
            _responseQueue?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}