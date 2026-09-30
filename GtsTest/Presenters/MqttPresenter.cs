using GtsTest.Services;
using System;
using System.Threading.Tasks;

namespace GtsTest.Presenters
{
    public class MqttPresenter : IDisposable
    {
        private readonly IMqttView _view;
        private readonly IMqttService _service;
        private bool _disposed;

        public MqttPresenter(IMqttView view, IMqttService service)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _service = service ?? throw new ArgumentNullException(nameof(service));

            _view.ConnectClicked += OnConnect;
            _view.DisconnectClicked += OnDisconnect;
            _view.SubscribeClicked += OnSubscribe;
            _view.UnsubscribeClicked += OnUnsubscribe;
            _view.PublishClicked += OnPublish;

            _service.ConnectionStateChanged += OnConnectionChanged;
            _service.MessageReceived += OnMessageReceived;

            _view.UpdateConnectionStatus(false, "");
        }

        private async void OnConnect()
        {
            string broker = _view.GetBrokerAddress();
            int port = _view.GetBrokerPort();
            string user = _view.GetUsername();
            string pass = _view.GetPassword();

            if (string.IsNullOrEmpty(broker))
            {
                _view.ShowMessage("请输入 Broker 地址", "提示", MessageType.Warning);
                return;
            }

            _view.AppendLog($"正在连接 {broker}:{port} ...");
            bool success = await _service.ConnectAsync(broker, port, user, pass);
            if (!success)
            {
                _view.ShowMessage("MQTT 连接失败，请检查地址和网络", "错误", MessageType.Error);
                _view.AppendLog("连接失败");
            }
            else
            {
                _view.AppendLog("连接成功");
            }
        }

        private void OnDisconnect()
        {
            _service.Disconnect();
            _view.AppendLog("手动断开连接");
        }

        private async void OnSubscribe()
        {
            string topic = _view.GetSubscribeTopic();
            if (string.IsNullOrEmpty(topic))
            {
                _view.ShowMessage("请输入订阅主题", "提示", MessageType.Warning);
                return;
            }
            try
            {
                await _service.SubscribeAsync(topic);
                _view.AppendLog($"订阅主题: {topic}");
            }
            catch (Exception ex)
            {
                _view.AppendLog($"订阅失败: {ex.Message}");
                _view.ShowMessage($"订阅失败: {ex.Message}", "错误", MessageType.Error);
            }
        }

        private async void OnUnsubscribe()
        {
            string topic = _view.GetSubscribeTopic();
            if (string.IsNullOrEmpty(topic))
            {
                _view.ShowMessage("请输入取消订阅的主题", "提示", MessageType.Warning);
                return;
            }
            try
            {
                await _service.UnsubscribeAsync(topic);
                _view.AppendLog($"取消订阅: {topic}");
            }
            catch (Exception ex)
            {
                _view.AppendLog($"取消订阅失败: {ex.Message}");
                _view.ShowMessage($"取消订阅失败: {ex.Message}", "错误", MessageType.Error);
            }
        }

        private async void OnPublish()
        {
            string topic = _view.GetPublishTopic();
            string payload = _view.GetPublishPayload();
            bool retain = _view.GetRetainFlag();

            if (string.IsNullOrEmpty(topic))
            {
                _view.ShowMessage("请输入发布主题", "提示", MessageType.Warning);
                return;
            }
            if (string.IsNullOrEmpty(payload))
            {
                _view.ShowMessage("请输入发布内容", "提示", MessageType.Warning);
                return;
            }

            try
            {
                await _service.PublishAsync(topic, payload, retain);
                _view.AppendLog($"发布消息: {topic} = {payload}");
            }
            catch (Exception ex)
            {
                _view.AppendLog($"发布失败: {ex.Message}");
                _view.ShowMessage($"发布失败: {ex.Message}", "错误", MessageType.Error);
            }
        }

        private void OnConnectionChanged(object sender, bool connected)
        {
            string info = connected ? $"{_service.BrokerAddress}" : "";
            _view.UpdateConnectionStatus(connected, info);
            if (connected)
                _view.AppendLog("MQTT 已连接");
            else
                _view.AppendLog("MQTT 已断开");
        }

        private void OnMessageReceived(object sender, string data)
        {
            var parts = data.Split('|');
            if (parts.Length == 2)
                _view.AppendMessage(parts[0], parts[1]);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _view.ConnectClicked -= OnConnect;
            _view.DisconnectClicked -= OnDisconnect;
            _view.SubscribeClicked -= OnSubscribe;
            _view.UnsubscribeClicked -= OnUnsubscribe;
            _view.PublishClicked -= OnPublish;
            _service.ConnectionStateChanged -= OnConnectionChanged;
            _service.MessageReceived -= OnMessageReceived;
            _service.Dispose();
        }
    }
}