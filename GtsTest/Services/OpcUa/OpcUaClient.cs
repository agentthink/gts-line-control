using GtsTest.Core;
using GtsTest.Diagnostics;            // 🆕 报文监视器
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClientSession = Opc.Ua.Client.Session;
using ClientSubscription = Opc.Ua.Client.Subscription;
using ClientMonitoredItem = Opc.Ua.Client.MonitoredItem;

namespace GtsTest.Services.OpcUa
{
    public class OpcUaClient : IOpcUaClient
    {
        private Opc.Ua.Client.Subscription _subscription;
        private Dictionary<string, Opc.Ua.Client.MonitoredItem> _items = new();
        private ClientSession _session;
        private readonly object _lock = new object();
        private bool _disposed;

        public event EventHandler<bool> ConnectionStateChanged;
        public event EventHandler<string> DataValueChanged;
        public event EventHandler<string> ErrorOccurred;

        public bool IsConnected => _session != null && _session.Connected;
        public string ServerUrl { get; private set; } = "";

        // ================================================================
        // 🆕 报文监视器上报（零侵入，绝不抛异常）
        // ================================================================
        private void ReportFrame(FrameDirection dir, string summary, string payload,
                                 bool isError = false, string? errorMsg = null)
        {
            try
            {
                FrameMonitorHub.Instance.Publish(new FrameLogEntry
                {
                    Protocol = FrameProtocol.OpcUa,
                    DeviceId = string.IsNullOrEmpty(ServerUrl) ? "" : ServerUrl,
                    Direction = dir,
                    Summary = summary,
                    Payload = payload,
                    IsError = isError,
                    ErrorMessage = errorMsg
                });
            }
            catch { /* 监视器异常不影响主业务 */ }
        }

        // ---------- 连接管理 ----------
        public async Task<bool> ConnectAsync(string serverUrl)
        {
            if (string.IsNullOrEmpty(serverUrl))
            {
                ErrorOccurred?.Invoke(this, "服务器 URL 不能为空");
                return false;
            }

            ReportFrame(FrameDirection.Info, $"正在连接: {serverUrl}", "");

            try
            {
                var config = new ApplicationConfiguration
                {
                    ApplicationName = "GtsTest OPC UA Client",
                    ApplicationType = ApplicationType.Client,
                    ClientConfiguration = new ClientConfiguration(),
                    SecurityConfiguration = new SecurityConfiguration
                    {
                        ApplicationCertificate = new CertificateIdentifier(),
                        TrustedPeerCertificates = new CertificateTrustList(),
                        TrustedIssuerCertificates = new CertificateTrustList(),
                        RejectedCertificateStore = new CertificateStoreIdentifier(),
                        AutoAcceptUntrustedCertificates = true
                    },
                    TransportQuotas = new TransportQuotas
                    {
                        OperationTimeout = 15000,
                        MaxStringLength = 1048576,
                        MaxByteStringLength = 1048576,
                        MaxArrayLength = 65535,
                        MaxMessageSize = 4194304,
                        MaxBufferSize = 65535,
                        ChannelLifetime = 300000,
                        SecurityTokenLifetime = 3600000
                    }
                };

                var endpointDesc = CoreClientUtils.SelectEndpoint(serverUrl, false);
                var endpointConfiguration = EndpointConfiguration.Create(config);
                var endpoint = new ConfiguredEndpoint(null, endpointDesc, endpointConfiguration);
                var userIdentity = new UserIdentity();

                var session = await ClientSession.Create(
                    config,
                    endpoint,
                    false,
                    false,
                    "GtsTest Client",
                    60000,
                    userIdentity,
                    null
                );

                // 🆕 把 Close() 挪出锁外，避免持锁做阻塞网络操作
                ClientSession? oldSession;
                lock (_lock)
                {
                    oldSession = _session;
                    _session = session;
                }
                if (oldSession != null)
                {
                    try { oldSession.Close(); } catch { /* ignore */ }
                }

                ServerUrl = serverUrl;
                ConnectionStateChanged?.Invoke(this, true);
                AppLogger.Info($"OPC UA 已连接到 {serverUrl}", "OPC UA");

                ReportFrame(FrameDirection.Info,
                    $"✅ 连接成功: {endpointDesc.EndpointUrl}",
                    $"SecurityPolicy: {endpointDesc.SecurityPolicyUri}");

                CreateSubscription();
                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"连接失败: {ex.Message}");
                AppLogger.Error($"OPC UA 连接失败: {ex.Message}", "OPC UA");

                ReportFrame(FrameDirection.Error,
                    $"连接失败: {ex.Message}", "", isError: true, errorMsg: ex.Message);

                return false;
            }
        }

        private void CreateSubscription()
        {
            if (_subscription != null)
            {
                AppLogger.Warn("订阅已存在，跳过创建", "OPC UA");
                return;
            }

            if (_session == null || !_session.Connected)
            {
                AppLogger.Error("会话未连接，无法创建订阅", "OPC UA");
                return;
            }

            try
            {
                var subscription = new Opc.Ua.Client.Subscription
                {
                    PublishingInterval = 1000,
                    KeepAliveCount = 10,
                    LifetimeCount = 20,
                    MaxNotificationsPerPublish = 1000,
                    Priority = 0,
                    PublishingEnabled = true,
                    TimestampsToReturn = TimestampsToReturn.Both
                };

                _session.AddSubscription(subscription);
                subscription.Create();

                if (subscription.Id == 0)
                {
                    AppLogger.Error("❌ 订阅创建失败：服务器返回 ID=0", "OPC UA");
                    return;
                }

                AppLogger.Info($"✅ OPC UA 订阅已创建 (ID={subscription.Id})", "OPC UA");

                ReportFrame(FrameDirection.Info,
                    $"📡 订阅已创建 (ID={subscription.Id})",
                    $"PublishingInterval={subscription.PublishingInterval}ms");

                _subscription = subscription;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"❌ 订阅创建异常: {ex.Message}", "OPC UA");
                _subscription = null;
                ErrorOccurred?.Invoke(this, $"订阅创建失败: {ex.Message}");

                ReportFrame(FrameDirection.Error,
                    $"订阅创建失败: {ex.Message}", "", isError: true, errorMsg: ex.Message);
            }
        }

        public void Disconnect()
        {
            var url = ServerUrl;

            // 先在锁外清理订阅（避免持锁做网络操作）
            try { UnsubscribeAll(); } catch { /* ignore */ }

            ClientSession? oldSession;
            ClientSubscription? oldSub;
            lock (_lock)
            {
                oldSub = _subscription;
                oldSession = _session;
                _subscription = null;
                _session = null;
                ServerUrl = "";
            }

            // 🆕 网络清理全部在锁外
            try { oldSub?.Delete(true); } catch { /* ignore */ }
            try { oldSession?.Close(); } catch { /* ignore */ }

            ConnectionStateChanged?.Invoke(this, false);
            AppLogger.Info("OPC UA 已断开", "OPC UA");
            ReportFrame(FrameDirection.Info, $"🔌 连接已断开: {url}", "");
        }

        // ---------- 数据操作 ----------
        public async Task<T> ReadNodeValueAsync<T>(string nodeId)
        {
            if (!IsConnected)
                throw new InvalidOperationException("未连接到 OPC UA 服务器");

            try
            {
                var node = new NodeId(nodeId);
                var result = await _session.ReadValueAsync(node);

                if (result.StatusCode == StatusCodes.Good)
                {
                    ReportFrame(FrameDirection.RX,
                        $"📥 读节点 {nodeId} = {result.Value}",
                        $"StatusCode=Good");

                    // 🆕 泛型类型不匹配时给出明确提示，而不是裸 InvalidCastException
                    if (result.Value is T typed)
                        return typed;

                    throw new InvalidCastException(
                        $"读取值类型不匹配: 期望 {typeof(T).Name}, 实际 {result.Value?.GetType().Name ?? "null"}");
                }

                throw new Exception($"读取失败: {result.StatusCode}");
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"读取节点 {nodeId} 失败: {ex.Message}");

                ReportFrame(FrameDirection.Error,
                    $"读取节点 {nodeId} 失败: {ex.Message}", "",
                    isError: true, errorMsg: ex.Message);

                throw;
            }
        }

        public async Task WriteNodeValueAsync<T>(string nodeId, T value)
        {
            if (!IsConnected)
                throw new InvalidOperationException("未连接到 OPC UA 服务器");

            try
            {
                var node = new NodeId(nodeId);
                var dataValue = new DataValue(new Variant(value));
                var writeValue = new WriteValue
                {
                    NodeId = node,
                    AttributeId = Attributes.Value,
                    Value = dataValue
                };
                var collection = new WriteValueCollection { writeValue };
                var response = await _session.WriteAsync(null, collection, CancellationToken.None);
                var result = response.Results?[0] ?? StatusCodes.BadUnexpectedError;

                if (result != StatusCodes.Good)
                    throw new Exception($"写入失败: {result}");

                AppLogger.Info($"OPC UA 写入节点 {nodeId} = {value}", "OPC UA");

                ReportFrame(FrameDirection.TX,
                    $"📤 写节点 {nodeId} = {value}",
                    $"StatusCode=Good");
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"写入节点 {nodeId} 失败: {ex.Message}");

                ReportFrame(FrameDirection.Error,
                    $"写入节点 {nodeId} 失败: {ex.Message}", "",
                    isError: true, errorMsg: ex.Message);

                throw;
            }
        }

        public async Task<List<string>> BrowseNodesAsync(string nodeId = null)
        {
            if (!IsConnected)
                throw new InvalidOperationException("未连接到 OPC UA 服务器");

            var results = new List<string>();
            try
            {
                var browseId = string.IsNullOrEmpty(nodeId) ? ObjectIds.RootFolder : new NodeId(nodeId);
                var description = new BrowseDescription
                {
                    NodeId = browseId,
                    BrowseDirection = BrowseDirection.Forward,
                    IncludeSubtypes = true,
                    NodeClassMask = (uint)(NodeClass.Object | NodeClass.Variable | NodeClass.Method),
                    ResultMask = (uint)BrowseResultMask.All
                };
                var collection = new BrowseDescriptionCollection { description };
                var response = await _session.BrowseAsync(null, null, 0, collection, CancellationToken.None);
                var references = response.Results?[0]?.References;

                if (references != null)
                {
                    foreach (var refs in references)
                        results.Add($"{refs.DisplayName.Text} ({refs.NodeId})");
                }

                ReportFrame(FrameDirection.RX,
                    $"📥 浏览节点 {nodeId ?? "Root"} 共 {results.Count} 项",
                    "");

                return results;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"浏览节点失败: {ex.Message}");

                ReportFrame(FrameDirection.Error,
                    $"浏览节点失败: {ex.Message}", "", isError: true, errorMsg: ex.Message);

                throw;
            }
        }

        // ---------- 订阅 ----------
        public void Subscribe(string nodeId, int samplingInterval = 1000)
        {
            if (!IsConnected || _subscription == null) return;

            try
            {
                if (_items.ContainsKey(nodeId))
                    Unsubscribe(nodeId);

                var node = ParseNodeId(nodeId);
                var item = new Opc.Ua.Client.MonitoredItem
                {
                    StartNodeId = node,
                    AttributeId = Attributes.Value,
                    SamplingInterval = samplingInterval,
                    QueueSize = 10,
                    DiscardOldest = true,
                    MonitoringMode = MonitoringMode.Reporting
                };

                item.Notification += OnMonitoredItemNotification;

                _subscription.AddItem(item);
                _items[nodeId] = item;

                _subscription.CreateItems();

                if (item.Status == null || item.Status.Id == 0)
                {
                    AppLogger.Error($"❌ 监控项创建失败：服务器未分配有效 ID (Status.Id={item.Status?.Id})", "OPC UA");

                    ReportFrame(FrameDirection.Error,
                        $"监控项创建失败: {nodeId}", "", isError: true);

                    _items.Remove(nodeId);
                    return;
                }

                AppLogger.Info($"✅ 监控项创建成功, ID={item.Status.Id}, SamplingInterval={item.SamplingInterval}", "OPC UA");

                ReportFrame(FrameDirection.Info,
                    $"📡 订阅节点 {nodeId} (ID={item.Status.Id}, {samplingInterval}ms)",
                    "");
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"订阅节点 {nodeId} 失败: {ex.Message}");
                AppLogger.Error($"❌ 订阅节点 {nodeId} 失败: {ex.Message}", "OPC UA");

                ReportFrame(FrameDirection.Error,
                    $"订阅节点 {nodeId} 失败: {ex.Message}", "", isError: true, errorMsg: ex.Message);
            }
        }

        private NodeId ParseNodeId(string nodeIdString)
        {
            if (string.IsNullOrEmpty(nodeIdString))
                throw new ArgumentException("节点 ID 不能为空");

            if (int.TryParse(nodeIdString, out int numericId))
                return new NodeId((uint)numericId);

            var parts = nodeIdString.Split(';');
            int ns = 0;
            string identifier = "";
            foreach (var part in parts)
            {
                if (part.StartsWith("ns="))
                    ns = int.Parse(part.Substring(3));
                else if (part.StartsWith("i="))
                    identifier = part.Substring(2);
                else if (part.StartsWith("s="))
                    identifier = part.Substring(2);
                else
                    identifier = part;
            }

            if (int.TryParse(identifier, out int id))
                return new NodeId((uint)id, (ushort)ns);

            return new NodeId(identifier, (ushort)ns);
        }

        // 回调
        private void OnMonitoredItemNotification(Opc.Ua.Client.MonitoredItem item, MonitoredItemNotificationEventArgs e)
        {
            AppLogger.Info($"📥 收到数据变化通知, NodeId: {item.StartNodeId}", "OPC UA");

            try
            {
                var notification = e.NotificationValue as MonitoredItemNotification;
                if (notification != null)
                {
                    var value = notification.Value;
                    if (value != null && value.Value != null)
                    {
                        var nodeId = item.StartNodeId.ToString();
                        DataValueChanged?.Invoke(this, $"{nodeId}|{value.Value}");
                        AppLogger.Info($"📤 触发 DataValueChanged 事件, 值: {value.Value}", "OPC UA");

                        ReportFrame(FrameDirection.RX,
                            $"📨 数据变化 {nodeId} = {value.Value}",
                            $"Timestamp={value.SourceTimestamp:HH:mm:ss.fff}");
                    }
                    else
                    {
                        AppLogger.Warn($"⚠️ 通知值为空或无效", "OPC UA");
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"❌ 数据回调异常: {ex.Message}", "OPC UA");

                ReportFrame(FrameDirection.Error,
                    $"数据回调异常: {ex.Message}", "", isError: true, errorMsg: ex.Message);
            }
        }

        public void Unsubscribe(string nodeId)
        {
            if (_items.TryGetValue(nodeId, out var item))
            {
                item.Notification -= OnMonitoredItemNotification;
                _subscription?.RemoveItem(item);
                _items.Remove(nodeId);
                _subscription?.ApplyChanges();
                AppLogger.Info($"OPC UA 取消订阅节点 {nodeId}", "OPC UA");

                ReportFrame(FrameDirection.Info, $"🔕 取消订阅 {nodeId}", "");
            }
        }

        public void UnsubscribeAll()
        {
            foreach (var kv in _items.ToList())
                Unsubscribe(kv.Key);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Disconnect();
        }
    }
}