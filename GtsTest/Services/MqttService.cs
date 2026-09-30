using GtsTest.Core;
using GtsTest.Diagnostics;            // 🆕 报文监视器
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using System;
using System.Text;                    // 🆕 Encoding.UTF8
using System.Threading.Tasks;

namespace GtsTest.Services
{
    public class MqttService : IMqttService
    {
        private IMqttClient _client;
        private bool _disposed;

        public event EventHandler<bool> ConnectionStateChanged;
        public event EventHandler<string> MessageReceived;  // "topic|payload"

        public bool IsConnected => _client != null && _client.IsConnected;
        public string BrokerAddress { get; private set; } = "";
        public int BrokerPort { get; private set; } = 1883;    // 🆕 供监视器显示端口

        // ================================================================
        // 🆕 报文监视器上报（零侵入）
        // ================================================================
        private void ReportFrame(FrameDirection dir, string summary, string payload,
                                 bool isError = false, string? errorMsg = null)
        {
            try
            {
                FrameMonitorHub.Instance.Publish(new FrameLogEntry
                {
                    Protocol = FrameProtocol.Mqtt,
                    DeviceId = string.IsNullOrEmpty(BrokerAddress) ? "" : $"{BrokerAddress}:{BrokerPort}",
                    Direction = dir,
                    Summary = summary,
                    Payload = payload,
                    IsError = isError,
                    ErrorMessage = errorMsg
                });
            }
            catch { /* 监视器异常不影响主业务 */ }
        }

        public async Task<bool> ConnectAsync(string brokerAddress, int port, string username = null, string password = null)
        {
            if (string.IsNullOrEmpty(brokerAddress))
                throw new ArgumentException("Broker 地址不能为空");

            ReportFrame(FrameDirection.Info, $"正在连接: {brokerAddress}:{port}", "");

            try
            {
                var factory = new MqttFactory();
                _client = factory.CreateMqttClient();

                // ✅ 修复①：先挂事件，再 ConnectAsync，避免连接瞬间的 retained / 订阅消息漏掉
                _client.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;

                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(brokerAddress, port)
                    .WithCleanSession()
                    .WithKeepAlivePeriod(TimeSpan.FromSeconds(60));

                if (!string.IsNullOrEmpty(username))
                    options.WithCredentials(username, password);

                await _client.ConnectAsync(options.Build());

                BrokerAddress = brokerAddress;
                BrokerPort = port;
                ConnectionStateChanged?.Invoke(this, true);
                AppLogger.Info($"MQTT 已连接到 {brokerAddress}:{port}", "MQTT");

                ReportFrame(FrameDirection.Info,
                    $"✅ 连接成功: {brokerAddress}:{port}",
                    string.IsNullOrEmpty(username) ? "匿名" : $"User={username}");

                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"MQTT 连接失败: {ex.Message}", "MQTT");

                ReportFrame(FrameDirection.Error,
                    $"连接失败: {ex.Message}", "", isError: true, errorMsg: ex.Message);

                // 连接失败时清理事件订阅，避免下次重连重复注册
                if (_client != null)
                {
                    try { _client.ApplicationMessageReceivedAsync -= OnMessageReceivedAsync; } catch { }
                    try { _client.Dispose(); } catch { }
                    _client = null;
                }
                return false;
            }
        }

        // ✅ 异步事件处理
        private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                var topic = e.ApplicationMessage.Topic;

                // ✅ 修复②：MQTTnet 4.x 用 PayloadSegment；5.x 请改成
                //    e.ApplicationMessage.Payload.ToArray()
                var payload = e.ApplicationMessage.PayloadSegment.Count > 0
                    ? Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment)
                    : string.Empty;

                MessageReceived?.Invoke(this, $"{topic}|{payload}");

                ReportFrame(FrameDirection.RX,
                    $"📨 收到消息 [{topic}]",
                    payload);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"MQTT 消息处理异常: {ex.Message}", "MQTT");

                ReportFrame(FrameDirection.Error,
                    $"消息处理异常: {ex.Message}", "", isError: true, errorMsg: ex.Message);
            }
            return Task.CompletedTask;
        }

        // ================================================================
        // ✅ 修复③：同步 Disconnect 内部走异步 + ConfigureAwait(false)，
        //     避免 UI 线程 .Wait() 死锁
        // ================================================================
        public void Disconnect()
        {
            try
            {
                DisconnectCoreAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                AppLogger.Error($"MQTT 断开异常: {ex.Message}", "MQTT");
            }
        }

        // 供新代码调用的公开异步版本
        public async Task DisconnectAsync()
        {
            await DisconnectCoreAsync().ConfigureAwait(false);
        }

        private async Task DisconnectCoreAsync()
        {
            var client = _client;
            if (client == null) return;

            if (client.IsConnected)
            {
                try
                {
                    await client.DisconnectAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    AppLogger.Warn($"MQTT DisconnectAsync 抛异常（忽略）: {ex.Message}", "MQTT");
                }

                try { client.ApplicationMessageReceivedAsync -= OnMessageReceivedAsync; } catch { }

                var addr = BrokerAddress;
                var port = BrokerPort;
                ConnectionStateChanged?.Invoke(this, false);
                AppLogger.Info("MQTT 已断开", "MQTT");

                ReportFrame(FrameDirection.Info, $"🔌 连接已断开: {addr}:{port}", "");
            }

            try { client.Dispose(); } catch { }

            _client = null;
            BrokerAddress = "";
        }

        public async Task PublishAsync(string topic, string payload, bool retain = false)
        {
            if (!IsConnected) throw new InvalidOperationException("MQTT 未连接");

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag(retain)
                .Build();

            await _client.PublishAsync(message);
            AppLogger.Info($"MQTT 发布: {topic} = {payload}", "MQTT");

            ReportFrame(FrameDirection.TX,
                $"📤 发布 [{topic}]{(retain ? " (retain)" : "")}",
                payload);
        }

        public async Task SubscribeAsync(string topic, int qos = 1)
        {
            if (!IsConnected) throw new InvalidOperationException("MQTT 未连接");

            var options = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(topic, (MqttQualityOfServiceLevel)qos)
                .Build();

            await _client.SubscribeAsync(options);
            AppLogger.Info($"MQTT 订阅主题: {topic}", "MQTT");

            ReportFrame(FrameDirection.Info,
                $"📡 订阅主题 [{topic}] QoS={qos}", "");
        }

        public async Task UnsubscribeAsync(string topic)
        {
            if (!IsConnected) return;
            var options = new MqttClientUnsubscribeOptionsBuilder()
                .WithTopicFilter(topic)
                .Build();

            await _client.UnsubscribeAsync(options);
            AppLogger.Info($"MQTT 取消订阅主题: {topic}", "MQTT");

            ReportFrame(FrameDirection.Info, $"🔕 取消订阅 [{topic}]", "");
        }

        public void UnsubscribeAll()
        {
            // 保持原行为（未实现）；如需真实实现，可在 SubscribeAsync 里维护 HashSet<string> _topics
            AppLogger.Warn("UnsubscribeAll 未实现详细取消，建议断开重连", "MQTT");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { Disconnect(); } catch { }
        }
    }
}