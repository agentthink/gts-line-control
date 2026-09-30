using System;

namespace GtsTest.Presenters
{
    public interface IMqttView
    {
        event Action ConnectClicked;
        event Action DisconnectClicked;
        event Action SubscribeClicked;
        event Action UnsubscribeClicked;
        event Action PublishClicked;

        void UpdateConnectionStatus(bool connected, string brokerInfo);
        void AppendMessage(string topic, string payload);
        void AppendLog(string message);
        void ShowMessage(string text, string caption, MessageType type);

        string GetBrokerAddress();
        int GetBrokerPort();
        string GetUsername();
        string GetPassword();
        string GetSubscribeTopic();
        string GetPublishTopic();
        string GetPublishPayload();
        bool GetRetainFlag();
    }
}