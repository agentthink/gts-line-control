using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GtsTest.Services
{
    public interface IOpcUaClient : IDisposable
    {
        event EventHandler<bool> ConnectionStateChanged;
        event EventHandler<string> DataValueChanged;
        event EventHandler<string> ErrorOccurred;

        bool IsConnected { get; }
        string ServerUrl { get; }

        Task<bool> ConnectAsync(string serverUrl);
        void Disconnect();
        Task<T> ReadNodeValueAsync<T>(string nodeId);
        Task WriteNodeValueAsync<T>(string nodeId, T value);
        Task<List<string>> BrowseNodesAsync(string nodeId = null);
        void Subscribe(string nodeId, int samplingInterval = 1000);
        void Unsubscribe(string nodeId);
        void UnsubscribeAll();
    }
}