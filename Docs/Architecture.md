# 系统架构说明

## 1. 概述

GTS 产线控制系统是一个基于 **.NET 8.0 WinForms** 的多设备并行控制与监控平台，采用 **MVP 架构 + 依赖注入**，通过分层设计实现 UI、业务逻辑、硬件驱动的完全解耦。

**主要能力**：

- 运动控制（固高 GTS 系列控制卡）
- 多协议通信（Modbus / OPC UA / MQTT / Siemens S7 / 三菱 MC / 串口）
- 工作流编排（JSON 命令序列）
- 机器视觉触发（Modbus 对接 HalconVisionServer）
- MES 上报（REST + SOAP 双协议）
- 用户权限审计、报警管理、黑匣子日志
- **冷热数据归档**（历史数据自动迁移）
- **IO 强制模拟**（现场调试无需硬件）
- **热加载差异报告**（配置变更可视化）

## 2. 分层架构

系统按职责分为 5 层，上层依赖下层，下层不感知上层：

### 2.1 UI 层（WinForms）

- `Form1` 主界面
- `OverviewControl` 产线监控
- `WorkflowExecutionControl` 生产执行
- `CommunicationControl` OPC UA 调试
- `MqttControl` MQTT 调试
- `DebugToolboxForm` 调试工具箱（6 个 Tab）
- `SystemConfigForm` 系统配置中心（6 个 Tab）
- `LoginForm` / `DeviceConfigForm` / `ModbusConfigForm` 各种对话框

### 2.2 Presenter 层（MVP）

- `GtsPresenter` 主界面调度
- `MqttPresenter` MQTT 调度

### 2.3 Service / Model 层

**设备与工作流**：

- `DeviceManager` 多设备并行调度
- `WorkflowEngine` 工作流执行引擎
- `DeviceStateMachine` 6 态状态机

**通信服务**：

- `PlcManager` 多 PLC 管理（西门子 S7 + 三菱 MC）
- `SerialPortManager` 多串口管理
- `ModbusClient` Modbus 客户端

**业务服务**：

- `AlarmManager` 报警管理
- `MesReportService` MES 上报
- `MesHealthMonitor` MES 健康监控
- `DatabaseArchiveService` 冷热数据归档
- `AuthenticationService` 认证授权
- `SessionServiceImpl` 会话管理

**基础设施**：

- `AppLogger` 异步日志（Channel）
- `CyclicMonitorBuffer` 黑匣子（30000 条环形缓冲）

### 2.4 通信层

- `ModbusClient` Modbus TCP/RTU
- `OpcUaClient` OPC UA 客户端
- `MqttService` MQTT 客户端
- `SiemensS7Client` 西门子 S7 PLC（S7NetPlus）
- `MitsubishiPlcClient` 三菱 MC PLC（HslCommunication）
- `SerialPortDriver` 串口驱动（4 种帧格式）

### 2.5 数据层

- `EfDataRepository` EF Core 仓储
- `GtsDbContext` 数据库上下文
- `SqlDialect` 跨数据库 SQL 方言（SQLite / SqlServer / MySQL）

### 2.6 依赖注入（v1.1.0 新增）

系统采用 `Microsoft.Extensions.DependencyInjection` 统一装配 30+ 服务：

```csharp
// ServiceCollectionExtensions.cs 关键注册
services.AddSingleton<ILogger, AppLoggerWrapper>();
services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
services.AddSingleton<IUserRepository, EfDataRepository>();
services.AddSingleton<IAlarmManager, AlarmManager>();
services.AddSingleton<DatabaseArchiveService>();    // 冷热归档
services.AddSingleton<PlcManager>();                 // 多 PLC 管理
services.AddSingleton<MesReportService>();           // MES 上报
services.AddSingleton<DeviceManager>();              // 设备调度
services.AddTransient<Form1>();                       // 主窗体
```

**优势**：

- 生命周期统一管理（Singleton / Transient）
- 单元测试友好（可注入 Mock）
- 依赖关系显式化

## 3. 模块职责

| 模块 | 位置 | 职责 |
|------|------|------|
| `GtsPresenter` | `Presenters/` | MVP 核心调度，桥接 UI 与 Service |
| `DeviceManager` | `Core/` | 多设备并行调度、工作流循环、MES 触发 |
| `WorkflowEngine` | `Core/` | 加载工作流、组装命令链、执行、收集结果 |
| `DeviceStateMachine` | `Core/` | 6 态状态机，合法状态转换校验 |
| `GtsModel` | `Core/` | 固高 GTS SDK 封装，支持模拟/真实切换 |
| `Watchdog` | `Core/` | 通信健康监控看门狗 |
| `AppLogger` | `Core/` | 基于 Channel 的异步日志系统 |
| `CyclicMonitorBuffer` | `Core/` | 30000 条循环内存缓冲区（黑匣子） |
| `ModbusClient` | `Modbus/` | Modbus TCP/RTU 客户端 |
| `OpcUaClient` | `Services/OpcUa/` | OPC UA 客户端（连接/读写/订阅） |
| `MqttService` | `Services/` | MQTT 客户端 |
| `PlcManager` | `Services/Plc/` | 多 PLC 管理 + 西门子 S7 + 三菱 MC |
| `SerialPortManager` | `Services/Serial/` | 多串口管理 + 4 种帧格式解析 |
| `MesReportService` | `Services/Mes/` | MES 双协议上报 + 异步队列 + 离线缓存 |
| `MesHealthMonitor` | `Services/Mes/` | MES 端点健康监控 |
| `DatabaseArchiveService` | `Services/Data/` | 冷热数据归档（主表 → History 表） |
| `EfDataRepository` | `Data/` | EF Core 多数据库仓储 |
| `DbContextFactory` | `Data/` | 数据库工厂，支持 SQLite/SqlServer/MySQL |
| `ServiceCollectionExtensions` | 根目录 | DI 装配中心 |

## 4. 关键设计模式

### 4.1 MVP（Model-View-Presenter）

**执行流程**：

1. 用户点击主界面"启动设备"按钮
2. View（Form1）触发 `StartSelectedClicked` 事件
3. Presenter（GtsPresenter）接收事件 → 校验权限 → 调用 `DeviceManager.StartDevice(deviceId)`
4. Service 返回结果 → Presenter 调用 `View.ShowMessage("启动成功")` 和 `View.UpdateDeviceList()`

**优点**：

- View 层只负责 UI，不含业务逻辑
- Presenter 可以独立测试
- 更换 UI 框架（WinForms → WPF）不需要改动 Presenter

### 4.2 命令模式（Command Pattern）

工作流中的每个步骤是一个 `IMotionCommand` 实现：

| 类型 | 名称 | 职责 |
|------|------|------|
| 接口 | `IMotionCommand` | 定义 `Execute()` / `Stop()` / 状态属性 / 日志事件 |
| 抽象基类 | `MotionCommandBase` | 模板方法（`Execute` 骨架 + `ExecuteCore` 抽象） |
| 具体命令 | `HomeCommand` | 轴回零 |
| 具体命令 | `MoveAbsCommand` | 绝对定位 |
| 具体命令 | `DelayCommand` | 延时 |
| 具体命令 | `WaitIOCommand` | 等待 DI 状态（**支持 IO 强制**） |
| 具体命令 | `TriggerVisionCommand` | 触发视觉拍照 |
| 具体命令 | `WriteSignalCommand` | 向其他设备写线圈 |
| 具体命令 | `WaitSignalCommand` | 等待其他设备线圈 |
| 组合命令 | `SequenceCommand` | 顺序执行一组命令，任一失败则停止 |

### 4.3 工厂模式（Factory Pattern）

`CommandFactory.Create()` 根据 `CommandConfig.Type` 动态创建命令对象：

| Type | 创建的命令 |
|------|-----------|
| `"Home"` | `new HomeCommand(...)` |
| `"MoveAbs"` | `new MoveAbsCommand(...)` |
| `"Delay"` | `new DelayCommand(...)` |
| `"WaitIO"` | `new WaitIOCommand(...)` |
| `"TriggerVision"` | `new TriggerVisionCommand(...)` |
| `"WriteSignal"` | `new WriteSignalCommand(...)` |
| `"WaitSignal"` | `new WaitSignalCommand(...)` |

### 4.4 策略模式（Strategy Pattern）

| 抽象接口 | 实现类 |
|---------|--------|
| `ISerialFrameParser` | `FixedLengthParser` / `LengthFieldParser` / `DelimiterParser` |
| `IPlcClient` | `SiemensS7Client` / `MitsubishiPlcClient` / `SimulatedPlcClient` |
| `IDataRepository` | `EfDataRepository` / `SqliteRepository` |
| `IAuditService` | `AuditServiceImpl` + 静态门面 `AuditService` |
| `IProductionService` | `ProductionServiceImpl` + 静态门面 `ProductionService` |
| `ISessionService` | `SessionServiceImpl` + 静态门面 `SessionManager` |

### 4.5 状态机（State Machine）

设备采用 **6 态状态机** 管理，由 `DeviceStateMachine` 严格约束转换：

| 当前状态 | 可转换到 | 触发条件 |
|----------|----------|----------|
| **Idle**（空闲） | Running / Disconnected | 启动设备 / Modbus 断开 |
| **Running**（运行中） | Idle / Paused / Error / EmergencyStop / Disconnected | 停止 / 故障 / 急停 / 断开 |
| **Paused**（暂停） | Running / Idle / Error / EmergencyStop | 恢复 / 停止 / 故障 |
| **Error**（故障） | Idle / Paused | 复位 / 重试 |
| **EmergencyStop**（急停） | Idle / Disconnected | 急停复位 / 断开 |
| **Disconnected**（断开） | Idle / Error | 重连成功 / 重连失败 |

### 4.6 归档模式（v1.1.0 新增）

`DatabaseArchiveService` 实现**冷热数据分离**：

- 热数据（主表）：最近 3 个月
- 冷数据（History 表）：3 个月前
- 每天凌晨定时扫描迁移
- 保证主表查询性能始终在毫秒级

## 5. 线程模型

| 线程 | 职责 | 生命周期 |
|------|------|----------|
| **UI 线程** | 界面渲染、用户交互 | 程序全程 |
| **设备循环线程** | 每台设备独立 `Task.Run` 执行工作流 | `StartDevice` → `StopDevice` |
| **看门狗线程** | 每设备独立，检查通信健康 | 工作流运行期间 |
| **GrabLoop 线程** | 相机采集循环 | 相机连接期间 |
| **日志写线程** | Channel 消费 + 文件写入 | `Initialize` → `Shutdown` |
| **MES 消费线程** | Channel 消费 + 上报 | `MesReportService` 生命周期 |
| **MES 重传线程** | 定期重传待上报数据 | `MesReportService` 生命周期 |
| **MES 健康监控线程** | 每 10 秒探测 MES 端点 | `MesHealthMonitor` 生命周期 |
| **串口读取线程** | 事件 + 20ms 轮询双通道 | `SerialPortDriver.Open()` 期间 |
| **PLC 自动重连线程** | 连接失败时定时重试 | 各 PLC 客户端生命周期 |
| **归档定时线程**（v1.1.0 新增） | 每天凌晨扫描并归档旧数据 | `DatabaseArchiveService` 生命周期 |

## 6. 配置驱动

系统完全由配置文件驱动：

| 配置文件 | 作用 | 热加载 |
|----------|------|:---:|
| `appsettings.json` | 数据库 + 日志配置 | ✅ |
| `mes_config.json` | MES 上报配置（REST/SOAP） | ✅ |
| `devices.json` | 设备列表 | ✅（含差异报告） |
| `Workflows/*.json` | 工作流定义 | ✅ |

**热加载按钮**（系统工具页）：一键重载所有配置，无需重启程序。

**v1.1.0 新增差异报告**：`devices.json` 热加载前自动 diff 对比，展示新增/移除/修改/未变设备清单，用户确认后再应用。

## 7. 数据流

### 7.1 工作流执行流程

1. 用户点击"执行工作流"
2. Presenter 检查权限
3. 若设备在运行，先停止
4. 设置工作流（`SetDeviceWorkflow`）
5. 启动设备（`StartDevice`）
6. `DeviceManager` 启动 `DeviceLoopAsync`
7. 循环执行工作流：
   - 加载命令序列
   - 逐个 `Execute(ct)`
   - 全部成功 → 产量 +1 → 触发 MES 上报
   - 任何失败 → 状态变 `Paused` → 触发报警

### 7.2 MES 上报流程

1. 工作流完成 → `DeviceManager` 调用 `MesReportService.Enqueue(data)`
2. 数据进入 `Channel` 队列
3. `ConsumeLoopAsync` 从队列消费
4. 调用 `PostToMesAsync`（REST 或 SOAP）
5. 成功 → 完成
6. 失败 → 缓存到 `MesPendingRecords` 表，计算下次重试时间
7. `RetryLoopAsync` 每 30 秒扫描待重传数据
8. 按失败类型采用不同重传策略

### 7.3 归档数据流（v1.1.0 新增）

1. `DatabaseArchiveService.Start()` 启动后台任务
2. 每天凌晨 2 点执行
3. 扫描 `ProductionRecords` 表
4. 找出 `Timestamp < 3个月前` 的数据
5. `INSERT INTO ProductionRecords_History` 迁移
6. `DELETE FROM ProductionRecords` 清理
7. 记录日志：`归档完成，迁移了 N 条历史记录`

## 8. 目录结构对照

| 目录 | 说明 |
|------|------|
| `Core/` | 核心基础设施（日志、看门狗、设备管理、状态机、工作流引擎） |
| `Commands/` | 工作流命令（命令模式，7 种命令） |
| `Controls/` | UI 用户控件 |
| `Data/` | EF Core 数据层（DbContext + Repository + SqlDialect） |
| `Forms/` | WinForms 窗体 |
| `Modbus/` | Modbus 通信模块 |
| `Models/` | 数据模型 |
| `Presenters/` | MVP Presenter |
| `Services/` | 业务服务（含 Alarm / Authentication / Data / Mes / Plc / Serial） |
| `Diagnostics/` | 诊断工具（报文监视器 + 诊断包） |
| `Utils/` | 工具类 |

## 9. 扩展点

| 扩展点 | 位置 | 方法 |
|--------|------|------|
| **新增工作流命令** | `Commands/` | 实现 `IMotionCommand`，在 `CommandFactory` 注册 |
| **新增 PLC 品牌** | `Services/Plc/` | 实现 `IPlcClient`，在 `PlcManager.CreateClient` 注册 |
| **新增数据库** | `Data/DbContextFactory.cs` | 在 `ConfigureOptions` 添加 `case` |
| **新增 MES 协议** | `Services/Mes/MesReportService.cs` | 添加 `PostToMesByXxxAsync` 方法 |
| **新增串口帧格式** | `Services/Serial/Frames/` | 实现 `ISerialFrameParser` |
| **新增归档策略** | `Services/Data/DatabaseArchiveService.cs` | 修改 `ArchiveLoopAsync` |
| **新增 DI 服务** | `ServiceCollectionExtensions.cs` | 添加 `services.AddSingleton<...>()` |

## 10. v1.1.0 关键改进总结

| 改进 | 影响 |
|------|------|
| **三菱 MC 协议** | 支持多品牌 PLC 混用场景 |
| **IO 强制模拟** | 现场调试无需真实硬件，工作流秒过 |
| **冷热数据归档** | 主表查询性能不再随数据量下降 |
| **热加载差异报告** | 工程师明确知道配置变更内容 |
| **DI 依赖注入** | 生命周期统一管理，测试友好 |
| **HALCON 精度优化** | 视觉测量极差 42.7 μm → 15.6 μm |
| **HALCON 内存安全** | 修复 `GenImageInterleaved` 悬空指针 + 跨线程 HObject 共享 |