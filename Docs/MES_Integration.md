# MES 集成说明

## 1. 概述

系统支持 **REST** 和 **SOAP** 双协议对接 MES 系统，通过 `mes_config.json` 一键切换，无需修改代码。

**核心特性**：

- **异步队列**：上报数据入 Channel 队列，不阻塞工作流
- **离线缓存**：上报失败自动缓存到 SQLite
- **分级重传**：按失败类型采取不同重传策略
- **健康监控**：独立看门狗探测 MES 端点
- **配置热重载**：运行时切换协议，无需重启
- **阈值保护**：待重传积压超过阈值时暂停自动重传

## 2. 架构总览

系统由 4 个部分构成：

**产线侧**：

- `DeviceManager` 工作流循环
- 工作流执行完成 → 产量 +1 → 调用 `Enqueue`

**MES 服务侧**：

- `Channel Queue` 异步队列
- `MesReportService` 上报服务
- `MesHealthMonitor` 健康监控
- `MesPendingRecords` SQLite 表（离线缓存）

**MES 服务端**：

- REST API（HTTP POST JSON）
- SOAP WebService（HTTP POST XML）

**数据流**：

- 工作流 → Enqueue → Channel → MesReportService → MES 服务端
- 失败 → MesPendingRecords → 定时重传 → MES 服务端
- MesHealthMonitor 每 10 秒探测 MES 端点，离线时暂停重传

## 3. 配置说明

### 3.1 REST 模式（`mes_config.json`）

```json
{
  "Enabled": true,
  "Protocol": "REST",
  "StationName": "Vision_Station_01",
  "Token": "",
  "ApiUrl": "http://localhost:8888/api/mes",
  "RestTimeoutMs": 10000,
  "AutoRetryEnabled": true,
  "AutoRetryIntervalSeconds": 30,
  "PauseAutoRetryThreshold": 100
}
```

### 3.2 SOAP 模式

```json
{
  "Enabled": true,
  "Protocol": "SOAP",
  "StationName": "Vision_Station_01",
  "Token": "",
  "SoapEndpoint": "http://127.0.0.1:8080/",
  "SoapTargetNamespace": "http://tempuri.org/",
  "SoapVersion": "1.1",
  "SoapAction": "http://tempuri.org/ReportProduction",
  "SoapMethodName": "ReportProduction",
  "SoapResultNode": "ReportProductionResult",
  "SoapTimeoutMs": 10000,
  "AutoRetryEnabled": true,
  "AutoRetryIntervalSeconds": 30,
  "PauseAutoRetryThreshold": 100
}
```

### 3.3 字段说明

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Enabled` | bool | `true` | 是否启用 MES 上报 |
| `Protocol` | string | `"REST"` | `"REST"` 或 `"SOAP"` |
| `StationName` | string | `"Vision_Station_01"` | 工位名称 |
| `Token` | string | `""` | Bearer Token（可选） |
| `ApiUrl` | string | - | REST 接口地址 |
| `RestTimeoutMs` | int | 10000 | REST 请求超时 |
| `SoapEndpoint` | string | - | SOAP 端点 URL |
| `SoapTargetNamespace` | string | - | SOAP 命名空间 |
| `SoapVersion` | string | `"1.1"` | `"1.1"` 或 `"1.2"` |
| `SoapAction` | string | - | SOAP 1.1 必填 |
| `SoapMethodName` | string | - | 方法名 |
| `SoapResultNode` | string | - | 结果节点名 |
| `SoapTimeoutMs` | int | 10000 | SOAP 请求超时 |
| `AutoRetryEnabled` | bool | `true` | 是否自动重传 |
| `AutoRetryIntervalSeconds` | int | 30 | 自动重传检查间隔（秒） |
| `PauseAutoRetryThreshold` | int | 100 | 积压阈值，超过则暂停 |

## 4. 上报数据结构

```csharp
public class MesReportData
{
    public long Id { get; set; }              // 数据库主键（重传时使用）
    public string Barcode { get; set; }       // 产品条码
    public string DeviceId { get; set; }      // 设备 ID
    public string Station { get; set; }       // 工位名称
    public string Result { get; set; }        // "OK" / "NG"
    public double DiameterMm { get; set; }    // 直径（mm）
    public double X { get; set; }             // X 坐标（mm）
    public double Y { get; set; }             // Y 坐标（mm）
    public string? ImagePath { get; set; }    // 图片路径
    public DateTime Timestamp { get; set; }   // 时间戳
    public int RetryCount { get; set; }       // 重试次数
    public string? FailReason { get; set; }   // 失败原因类型
    public string? NextRetryTime { get; set; } // 下次重试时间
}
```

## 5. 分级重传策略

### 5.1 失败类型分类

```csharp
public enum MesFailureType
{
    None = 0,              // 成功
    Timeout = 1,           // 请求超时
    ConnectionFailed = 2,  // 网络连接失败
    ServerError = 3,       // 服务端 5xx 错误
    ClientError = 4,       // 客户端 4xx 错误（不自动重传）
    Unknown = 99
}
```

### 5.2 重传策略表

| 失败类型 | 触发场景 | 首次重传延迟 | 最大重试次数 | 策略 |
|----------|----------|--------------|--------------|------|
| **Timeout** | 请求超时 | 5 秒 | 20 | 快速重试 |
| **ConnectionFailed** | 无法连接 MES | 5 秒 | 20 | 快速重试 |
| **ServerError** | HTTP 5xx | 30 秒 | 10 | 指数退避（30→60→120→240→300 秒封顶） |
| **ClientError** | HTTP 4xx | 不重传 | 0 | 需人工介入（配置错误） |

### 5.3 重传流程

1. 启动重传循环
2. 检查 `Enabled` 是否为 true
3. 检查 MES 是否在线（健康监控状态）
4. 加载待重传记录（最多 200 条）
5. 检查待重传数量是否超过阈值
   - 超过 → 暂停自动重传，等待用户处理
   - 未超过 → 继续
6. 逐条处理待重传数据：
   - 若重试次数超限 → 跳过
   - 若未到下次重试时间 → 跳过
   - 否则执行上报
     - 成功 → 从数据库删除
     - 失败 → 更新重试次数和下次重试时间
7. 等待下次循环

### 5.4 阈值保护

当待重传积压 **超过阈值**（默认 100 条）时：

1. 自动**暂停**自动重传
2. 记录警告日志
3. UI 顶部显示"⚠️ MES 待重传: N (已暂停)"（红色）
4. 网络恢复或用户手动触发重传后，自动**解除暂停**

**目的**：防止 MES 长期宕机导致本地数据库无限增长 + 反复重传冲击网络。

## 6. 健康监控看门狗

### 6.1 工作原理

1. 每 10 秒执行一次健康检查
2. 向 MES 端点发送 HTTP HEAD 请求
3. 判定逻辑：
   - 响应码 < 500 → 判定成功
   - 响应超时或 5xx → 判定失败
   - 连续成功 2 次 → 判定"在线"
   - 连续失败 3 次 → 判定"离线"
4. 状态变化时触发事件 `HealthChanged`

### 6.2 关键参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `IntervalSeconds` | 10 | 检查间隔 |
| `OfflineThreshold` | 3 | 连续失败 N 次判定离线 |
| `OnlineThreshold` | 2 | 连续成功 N 次判定恢复 |

### 6.3 状态变化响应

**离线时**：

- 暂停自动重传
- 上报直接走"离线缓存"分支，不尝试网络请求
- UI 显示 🔴 MES 离线

**恢复时**：

- 立即触发一次全量重传（不等 30 秒周期）
- UI 显示 🟢 MES 在线

## 7. 离线缓存

失败数据存入 SQLite 表 `MesPendingRecords`：

```sql
CREATE TABLE IF NOT EXISTS MesPendingRecords (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Barcode TEXT,
    DeviceId TEXT,
    Station TEXT,
    Result TEXT,
    DiameterMm REAL,
    X REAL,
    Y REAL,
    ImagePath TEXT,
    Timestamp TEXT,
    RetryCount INTEGER DEFAULT 0,
    FailReason TEXT,
    NextRetryTime TEXT
);
CREATE INDEX idx_mes_barcode ON MesPendingRecords(Barcode);
CREATE INDEX idx_mes_retry ON MesPendingRecords(NextRetryTime);
```

**优势**：

- 断电/崩溃后重传数据不丢失
- 可跨程序重启继续重传
- 查询/清理方便

## 8. SOAP 客户端实现

### 8.1 请求构造

```xml
<?xml version="1.0" encoding="utf-8"?>
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
  <soap:Body>
    <ReportProduction xmlns="http://tempuri.org/">
      <barcode>SN20260922123456</barcode>
      <deviceId>dev-002</deviceId>
      <station>Vision_Station_01</station>
      <result>OK</result>
      <diameterMm>20.15</diameterMm>
      <x>-27.34</x>
      <y>-15.76</y>
      <imagePath></imagePath>
      <timestamp>2026-09-22T12:34:56.789</timestamp>
    </ReportProduction>
  </soap:Body>
</soap:Envelope>
```

### 8.2 HTTP 请求头

**SOAP 1.1**：

```text
POST / HTTP/1.1
Host: 127.0.0.1:8080
Content-Type: text/xml; charset=utf-8
SOAPAction: "http://tempuri.org/ReportProduction"
```

**SOAP 1.2**：

```text
POST / HTTP/1.1
Host: 127.0.0.1:8080
Content-Type: application/soap+xml; charset=utf-8; action="http://tempuri.org/ReportProduction"
```

### 8.3 响应解析

```xml
<?xml version="1.0" encoding="utf-8"?>
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
  <soap:Body>
    <ReportProductionResponse xmlns="http://tempuri.org/">
      <ReportProductionResult>OK:SN20260922123456</ReportProductionResult>
    </ReportProductionResponse>
  </soap:Body>
</soap:Envelope>
```

解析出 `ReportProductionResult` 节点的文本作为返回结果。

## 9. 测试方法

### 9.1 REST 测试服务端（Python Flask）

```python
from flask import Flask, request, jsonify
from datetime import datetime

app = Flask(__name__)

@app.route('/api/mes', methods=['POST'])
def receive():
    data = request.get_json()
    print(f"[MES-REST] {datetime.now().strftime('%H:%M:%S')} "
          f"收到: {data['barcode']} | {data['deviceId']} | {data['result']}")
    return jsonify({"code": 0, "message": "success"})

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=8888, threaded=True)
```

**运行**：

```bash
pip install flask
python mes_rest_server.py
```

### 9.2 SOAP 测试服务端（Python 标准库）

```python
import http.server
import socketserver
import xml.etree.ElementTree as ET

SOAP_NS = "http://schemas.xmlsoap.org/soap/envelope/"
TNS = "http://tempuri.org/"
PORT = 8080

class SoapHandler(http.server.BaseHTTPRequestHandler):
    def do_POST(self):
        content_length = int(self.headers.get('Content-Length', 0))
        body = self.rfile.read(content_length).decode('utf-8')
        root = ET.fromstring(body)
        body_node = root.find(f'{{{SOAP_NS}}}Body')
        method_node = body_node[0]
        params = {child.tag.split('}')[-1]: child.text for child in method_node}

        barcode = params.get('barcode', '')
        print(f"[MES SOAP] 收到上报: {barcode}")

        response_xml = f'''<?xml version="1.0" encoding="utf-8"?>
<soap:Envelope xmlns:soap="{SOAP_NS}">
  <soap:Body>
    <ReportProductionResponse xmlns="{TNS}">
      <ReportProductionResult>OK:{barcode}</ReportProductionResult>
    </ReportProductionResponse>
  </soap:Body>
</soap:Envelope>'''

        response_bytes = response_xml.encode('utf-8')
        self.send_response(200)
        self.send_header('Content-Type', 'text/xml; charset=utf-8')
        self.send_header('Content-Length', str(len(response_bytes)))
        self.end_headers()
        self.wfile.write(response_bytes)

    def log_message(self, format, *args):
        pass

if __name__ == '__main__':
    with socketserver.TCPServer(("", PORT), SoapHandler) as httpd:
        print(f"MES SOAP 服务端已启动: http://0.0.0.0:{PORT}/")
        httpd.serve_forever()
```

## 10. 故障排查

| 现象 | 可能原因 | 解决方案 |
|------|----------|----------|
| **完全不发** | `Enabled = false` | 改配置为 `true`，热加载 |
| **一直失败但缓存成功** | MES 服务器未启动 | 启动服务端，健康监控会自动恢复 |
| **一直缓存在离线队列** | MES 端点 URL 错误 | 用 `curl` 验证端点可访问 |
| **4xx 错误** | 字段名/格式不匹配 | 检查 MES 服务端的接口定义 |
| **字段名不匹配** | SOAP 参数名不同 | 修改 `PostToMesBySoapAsync` 中的参数字典 |
| **待重传数一直涨** | MES 宕机超过阈值 | 修复 MES，点击"重传 MES"手动触发 |

## 11. 简历亮点表述

> **MES 双协议对接**：基于 `HttpClient` 手写 SOAP 1.1/1.2 客户端（不依赖 WCF），支持 SOAP Fault 解析；通过 `mes_config.json` 一键切换 REST/SOAP 协议；配合**异步队列（Channel）+ 离线缓存（SQLite）+ 分级重传（超时/5xx/4xx 差异化策略）+ 健康监控看门狗**机制，保证产线数据零丢失。