using System;
using System.Threading.Tasks;

namespace GtsTest.Services //MQTT 服务接口
{
    public interface IMqttService : IDisposable
    {
        event EventHandler<bool> ConnectionStateChanged;
        event EventHandler<string> MessageReceived;  // "topic|payload"

        bool IsConnected { get; }
        string BrokerAddress { get; }

        Task<bool> ConnectAsync(string brokerAddress, int port, string username = null, string password = null);
        void Disconnect();
        Task PublishAsync(string topic, string payload, bool retain = false);
        Task SubscribeAsync(string topic, int qos = 1);
        Task UnsubscribeAsync(string topic);
        void UnsubscribeAll();
    }
}