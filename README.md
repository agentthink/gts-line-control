# GTS 产线控制系统

[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey)]()
[![C%23](https://img.shields.io/badge/C%23-12.0-purple)]()
[![Version](https://img.shields.io/badge/Version-1.1.0-orange)]()

基于 **.NET 8.0 WinForms** 的多设备并行控制与监控平台，专为 **固高 GTS 系列运动控制卡** 设计。

集成 Modbus TCP/RTU、OPC UA、MQTT、**Siemens S7 PLC**、**三菱 MC 协议**、**RS232/485 串口** 通信，**MES 双协议上报（REST + SOAP）**，工作流编排、报警管理、用户权限审计、**冷热数据归档** 及黑匣子日志，适用于产线自动化与测试台架。

> 🆕 **v1.1.0 更新**：多品牌 PLC（三菱 MC）、IO 强制模拟、数据库冷热归档、热加载差异报告、DI 依赖注入。

---

## 📋 目录

- [界面预览](#-界面预览)
- [功能特性](#-功能特性)
- [技术架构](#-技术架构)
- [快速开始](#-快速开始)
- [工作流配置](#-工作流配置)
- [数据库设计](#-数据库设计)
- [项目结构](#-项目结构)
- [权限管理](#-权限管理)
- [通信协议](#-通信协议)
- [系统配置中心](#-系统配置中心)
- [配置文件说明](#-配置文件说明)
- [MES 上报服务](#-mes-上报服务)
- [Modbus 协议详解](#-modbus-协议详解)
- [v1.1.0 新增能力](#-v110-新增能力)
- [扩展点](#-扩展点)
- [测试报告摘要](#-测试报告摘要)
- [版本历史](#-版本历史)
- [待开发功能](#-待开发功能)
- [贡献指南](#-贡献指南)
- [许可证](#-许可证)

---

## 🖼️ 界面预览

### 主界面与登录

| 主操作界面 | 登录界面 |
|:---:|:---:|
| ![主界面](Images/main.png) | ![登录界面](Images/login.png) |
| 顶部标题栏、设备列表、产线监控/生产执行选项卡、操作日志/监控日志、状态栏 | 用户名/密码输入框，登录/取消按钮 |

**主界面说明：**

- **顶部栏**：标题、用户信息、登录/登出按钮、急停按钮、一键确认并解决所有活动报警按钮、系统管理按钮、MES 待重传数量、MES 手动重传按钮
- **左侧设备列表**：显示所有设备及在线状态（绿/红指示灯），支持添加/移除设备
- **中间区域**：
  - **产线监控**：4 张统计卡片（总产量/良品率/在线设备/未处理报警）、产量趋势折线图、在线/离线饼图、设备列表、底部设备详情栏
  - **生产执行**：设备选择、工作流选择、绑定/执行/停止/复位/产量清零按钮、步骤列表、执行状态、执行日志
- **底部日志**：操作日志（黑底绿字）和监控日志（黑底青字）
- **底部状态栏**：设备名、伺服状态、限位状态、看门狗状态、Modbus 状态、当前指令

### 设备配置

| 添加设备配置 | Modbus 高级配置 |
|:---:|:---:|
| ![添加设备](Images/DeviceSte.png) | ![Modbus配置](Images/ModbusSte.png) |
| 设备名称、IP 地址、Modbus 端口、起始地址、轴号、目标产量 | 协议选择(TCP/RTU)、从站地址、TCP/RTU 参数、地址类型、数据类型、显示格式、字节序、起始地址、寄存器数量 |

### 核心功能模块

| 工作流编辑（主界面） | 工作流编辑（系统配置中心） |
|:---:|:---:|
| ![工作流](Images/Workflow.png) | ![工作流编辑](Images/WorkSystemComfig.png) |
| 生产执行选项卡：工作流下拉选择、步骤列表、执行状态 | 系统配置中心工作流编辑器：新建/保存/运行/停止、步骤增删改/上移下移 |

| MQTT 通信 | OPC UA 通信 |
|:---:|:---:|
| ![MQTT](Images/MqttSystemComfig.png) | ![OPC UA](Images/OpcUaSystemComfig.png) |
| Broker 地址/端口/用户名/密码、连接/断开、主题订阅/取消订阅、消息发布 | 服务器地址输入、连接/断开、节点 ID 订阅/取消订阅、实时数据日志 |

| 系统工具 | 用户管理 |
|:---:|:---:|
| ![系统工具](Images/SystemComfigTool.png) | ![用户管理](Images/UserManagement.png) |
| 系统控制、模拟模式切换、运维工具、日志级别实时切换 | 用户列表、增删改查、密码重置、逻辑删除 |

### 调试工具箱

调试工具箱是独立窗体，通过 **系统配置中心 → 调试工具** 打开，包含 **6 个** 选项卡：

| 轴控制 | Modbus 调试 |
|:---:|:---:|
| ![轴控制](Images/DebugAxis.png) | ![Modbus调试](Images/DebugModbus.png) |
| 设备选择、轴号/轴状态/位置/速度显示、回零/定位/点动/停止/使能/复位报警 | 写寄存器、写线圈、读寄存器 |

| 设备控制 | 视觉触发调试 |
|:---:|:---:|
| ![设备控制](Images/DebugCtlDevice.png) | ![视觉触发](Images/DebugCamera.png) |
| 单设备控制（启动/停止/配置）、Modbus 连接/断开、IO 强制模拟 | 视觉服务器配置、触发拍照、状态显示 |

| 串口调试 | PLC 调试（含三菱） |
|:---:|:---:|
| ![串口调试](Images/DebugSerial.png) | ![PLC调试](Images/DebugPlc.png) |
| 串口配置、HEX/ASCII 发送、4 种帧格式、实时接收 | 支持模拟/西门子 S7/三菱 MC 全系列 PLC 读写 |

---

## 🚀 功能特性

| 模块 | 描述 | 状态 |
|------|------|------|
| **多设备管理** | 动态增删设备，每台独立配置（IP、端口、轴号、目标产量） | ✅ |
| **运动控制** | 回零、绝对定位、点动、伺服使能、急停、软限位、报警复位 | ✅ |
| **Modbus 通信** | TCP/RTU，读写线圈/寄存器/离散输入，支持 Int16/32、Float、Double 及字节序 | ✅ |
| **OPC UA 客户端** | 连接、读写节点、订阅数据变化、浏览节点树 | ✅ |
| **MQTT 客户端** | 连接 Broker、订阅/发布主题、保留消息、QoS | ✅ |
| **Siemens S7 PLC** | S7-1200/1500/300/400/200Smart，全类型读写，自动重连 | ✅ |
| **三菱 MC 协议** | 基于 HslCommunication，支持 FX / Q / L 系列，自动重连 | ✅ |
| **RS232/485 串口** | 4 种帧格式（原始流/定长/头+长度/分隔符），自动重连 | ✅ |
| **工作流引擎** | JSON 配置命令链，支持 7 种命令，动态加载，多设备复用 | ✅ |
| **视觉触发命令** | Modbus 触发 Halcon 视觉服务器拍照，等待结果并解析状态码 | ✅ |
| **跨设备信号交互** | 工作流中读写其他设备 Modbus 线圈，实现设备联锁 | ✅ |
| **设备-工作流绑定** | 设备预绑定工作流，支持"全部启动"一键运行 | ✅ |
| **实时监控** | 后台多线程轮询轴状态 + Modbus 数据，趋势图实时更新 | ✅ |
| **黑匣子缓冲区** | 内存循环存储 30000 条监控记录，一键导出 | ✅ |
| **双日志系统** | 操作日志 + 监控日志，按日期/大小滚动落盘，自动清理 | ✅ |
| **报警管理** | 触发/确认/解决报警，按严重等级分类，SQLite 持久化 | ⏳ UI 待完善 |
| **用户权限与审计** | Admin/Engineer/Operator 三级权限，PBKDF2 密码，操作审计 | ✅ |
| **产量记录服务** | 异步记录产量到数据库，为报表统计提供数据 | ✅ |
| **模拟/真实切换** | 无硬件完整运行，一键切换时自动检测固高卡 | ✅ |
| **设备看门狗** | 每设备独立看门狗，超时自动重连或报警 | ✅ |
| **配置导入导出** | 设备配置导出为 `devices.json`，支持导入恢复 | ✅ |
| **调试工具箱** | 6 个 Tab：轴控 / Modbus / 设备 / 视觉 / 串口 / PLC | ✅ |
| **EF Core 多数据库** | SQLite / SQL Server / MySQL 一键切换 | ✅ |
| **MES 双协议上报** | REST + SOAP，异步队列 + 离线缓存 + 分级重传 | ✅ |
| **MES 健康监控** | 10 秒探测 MES 端点，离线暂停、恢复重传 | ✅ |
| **配置热重载** | 运行时重载 JSON 配置，无需重启 | ✅ |
| **热加载差异报告** | 重载前 diff 对比，展示新增/移除/修改/未变设备清单 | ✅ |
| **日志级别动态切换** | UI 下拉框实时切换，无需重启 | ✅ |
| **IO 强制模拟** | 现场调试无需真实硬件，强制 DI 值让工作流秒过 | ✅ |
| **数据库冷热归档** | 定时归档 3 个月前数据到 History 表，保证主表查询性能 | ✅ |
| **DI 依赖注入** | 基于 `Microsoft.Extensions.DependencyInjection` 统一装配 | ✅ |

---

## 🏗️ 技术架构

### 技术栈

| 方面 | 实现 |
|------|------|
| **架构模式** | MVP（Model-View-Presenter）+ 被动视图 |
| **依赖注入** | `Microsoft.Extensions.DependencyInjection` 统一装配 |
| **设计模式** | 命令、模板方法、观察者、工厂、策略、状态机 |
| **多线程** | 独立后台轮询线程 + `CancellationToken` 工作流取消 |
| **硬件抽象** | `gts.cs` P/Invoke（需从固高 SDK 获取） |
| **配置驱动** | `devices.json` + `Workflows/*.json` + `mes_config.json` + `appsettings.json` |
| **ORM 框架** | Entity Framework Core 8.0 |
| **Modbus 集成** | NModbus |
| **OPC UA 集成** | OPC Foundation .NET Standard |
| **MQTT 集成** | MQTTnet |
| **PLC 集成** | S7NetPlus（西门子）+ HslCommunication（三菱） |
| **视觉对接** | Halcon（外部服务）+ Modbus TCP 握手协议 |
| **日志系统** | `AppLogger` 静态类 + `System.Threading.Channels` 异步写入 |
| **密码哈希** | PBKDF2-SHA256（15 万次迭代，支持 pepper 环境变量） |
| **异步队列** | `System.Threading.Channels` |
| **SOAP 客户端** | 基于 `HttpClient` 手写 SOAP 1.1/1.2 Envelope（不依赖 WCF） |

### 分层架构

系统按职责分为 5 层，上层依赖下层，下层不感知上层：

**1. UI 层（WinForms）**

- `Form1` 主界面
- `OverviewControl` 产线监控
- `WorkflowExecutionControl` 生产执行
- `CommunicationControl` OPC UA 调试
- `MqttControl` MQTT 调试
- `DebugToolboxForm` 调试工具箱
- `SystemConfigForm` 系统配置中心
- `LoginForm` / `DeviceConfigForm` / `ModbusConfigForm` 各种对话框

**2. Presenter 层（MVP）**

- `GtsPresenter` 主界面调度
- `MqttPresenter` MQTT 调度

**3. Service / Model 层**

- `DeviceManager` 多设备并行调度
- `WorkflowEngine` 工作流执行
- `AlarmManager` 报警管理
- `PlcManager` 多 PLC 管理
- `SerialPortManager` 多串口管理
- `MesReportService` MES 上报
- `DatabaseArchiveService` 冷热归档
- `AuthenticationService` 认证授权
- `AppLogger` 异步日志
- `CyclicMonitorBuffer` 黑匣子

**4. 通信层**

- `ModbusClient` Modbus TCP/RTU
- `OpcUaClient` OPC UA 客户端
- `MqttService` MQTT 客户端
- `SiemensS7Client` 西门子 S7 PLC
- `MitsubishiPlcClient` 三菱 MC PLC
- `SerialPortDriver` 串口驱动

**5. 数据层**

- `EfDataRepository` EF Core 仓储
- `GtsDbContext` 数据库上下文
- `SqlDialect` 跨数据库 SQL 方言

**基础设施**

- `gts.dll` 固高运动控制卡
- `NModbus` / `OPC UA SDK` / `MQTTnet` / `S7NetPlus` / `HslCommunication` / `System.IO.Ports`

### 关键设计模式

#### 命令模式（工作流命令链）

工作流的每个步骤是一个命令对象，通过 `SequenceCommand` 组合执行。

| 类型 | 名称 | 职责 |
|------|------|------|
| 接口 | `IMotionCommand` | 定义 `Execute()` / `Stop()` / 状态属性 / 日志事件 |
| 抽象基类 | `MotionCommandBase` | 模板方法（`Execute` 骨架 + `ExecuteCore` 抽象） |
| 具体命令 | `HomeCommand` | 轴回零 |
| 具体命令 | `MoveAbsCommand` | 绝对定位 |
| 具体命令 | `DelayCommand` | 延时 |
| 具体命令 | `WaitIOCommand` | 等待 DI 状态（支持 IO 强制） |
| 具体命令 | `TriggerVisionCommand` | 触发视觉拍照 |
| 具体命令 | `WriteSignalCommand` | 向其他设备写线圈 |
| 具体命令 | `WaitSignalCommand` | 等待其他设备线圈 |
| 组合命令 | `SequenceCommand` | 顺序执行一组命令，任一失败则停止 |

#### 状态机（设备生命周期）

设备采用 **6 态状态机** 管理，合法状态转换由 `DeviceStateMachine` 严格约束：

| 当前状态 | 可转换到 | 触发条件 |
|----------|----------|----------|
| **Idle**（空闲） | Running / Disconnected | 启动设备 / Modbus 断开 |
| **Running**（运行中） | Idle / Paused / Error / EmergencyStop / Disconnected | 停止 / 故障 / 急停 / 断开 |
| **Paused**（暂停） | Running / Idle / Error / EmergencyStop | 恢复 / 停止 / 故障 |
| **Error**（故障） | Idle / Paused | 复位 / 重试 |
| **EmergencyStop**（急停） | Idle / Disconnected | 急停复位 / 断开 |
| **Disconnected**（断开） | Idle / Error | 重连成功 / 重连失败 |

#### 策略模式（可替换实现）

| 抽象接口 | 实现类 |
|---------|--------|
| `ISerialFrameParser` | `FixedLengthParser` / `LengthFieldParser` / `DelimiterParser` |
| `IPlcClient` | `SiemensS7Client` / `MitsubishiPlcClient` / `SimulatedPlcClient` |
| `IDataRepository` | `EfDataRepository` / `SqliteRepository` |
| `IAuditService` | `AuditServiceImpl` + 静态门面 `AuditService` |
| `IProductionService` | `ProductionServiceImpl` + 静态门面 `ProductionService` |
| `ISessionService` | `SessionServiceImpl` + 静态门面 `SessionManager` |

### 线程模型

| 线程 | 职责 | 生命周期 |
|------|------|----------|
| **UI 线程** | 界面渲染、用户交互 | 程序全程 |
| **设备循环线程** | 每台设备独立 `Task.Run` 执行工作流 | StartDevice → StopDevice |
| **看门狗线程** | 每设备独立，检查通信健康 | 工作流运行期间 |
| **GrabLoop 线程** | 相机采集循环 | 相机连接期间 |
| **日志写线程** | `Channel` 消费 + 文件写入 | Initialize → Shutdown |
| **MES 消费线程** | `Channel` 消费 + 上报 | MesReportService 生命周期 |
| **MES 重传线程** | 定期重传待上报数据 | MesReportService 生命周期 |
| **MES 健康监控线程** | 每 10 秒探测 MES 端点 | MesHealthMonitor 生命周期 |
| **串口读取线程** | 事件 + 20ms 轮询双通道 | SerialPortDriver.Open() 期间 |
| **PLC 自动重连线程** | 连接失败时定时重试 | 各 PLC 客户端生命周期 |
| **归档定时线程** | 每天凌晨扫描并归档旧数据 | DatabaseArchiveService 生命周期 |

---

## 📦 快速开始

### 1. 获取固高 SDK（必须）

本仓库 **不包含** `gts.cs` 和 `gts.dll`（版权限制）。请从固高（Googol Technology）官网或配套光盘获取：

| 文件 | 位置 |
|------|------|
| `gts.cs` | 项目源代码根目录（与 `GtsModel.cs` 同级） |
| `gts.dll` | 编译输出目录或系统 PATH |

> 仅模拟模式可省略 `gts.dll`，但 `gts.cs` 仍需存在。

### 2. 环境要求

- Windows 10/11（x86/x64）
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 或 VS Code
- **可选**：SQL Server Express / LocalDB 或 MySQL/MariaDB（默认 SQLite 开箱即用）

### 3. NuGet 依赖

| 包名 | 版本 | 用途 |
|------|------|------|
| `NModbus` | 3.0.81+ | Modbus TCP/RTU |
| `MQTTnet` | 4.3+ | MQTT 客户端 |
| `OPCFoundation.NetStandard.Opc.Ua` | 1.5+ | OPC UA 客户端 |
| `S7NetPlus` | 0.20+ | 西门子 S7 PLC |
| `HslCommunication` | 12.0+ | 三菱 MC 协议 |
| `Microsoft.EntityFrameworkCore.Sqlite` | 8.0+ | SQLite |
| `Microsoft.EntityFrameworkCore.SqlServer` | 8.0+ | SQL Server |
| `Pomelo.EntityFrameworkCore.MySql` | 8.0+ | MySQL |
| `Dapper` | 2.1+ | 轻量仓储 |
| `Microsoft.Extensions.DependencyInjection` | 8.0+ | DI 容器 |

### 4. 克隆与编译

```bash
git clone https://github.com/Littleoldier/GtsTest.git
cd GtsTest
dotnet restore
dotnet build -c Release
dotnet run --project GtsTest.csproj
```

默认以 **模拟模式** 启动，无需硬件。

### 5. 首次登录

- 程序启动后自动创建 SQLite 数据库（`gts.db`）
- 默认管理员账号 `admin` 会在首次启动时自动生成，初始密码输出到日志中（同时显示在消息框）
- 建议登录后立即修改密码

---

## ⚙️ 工作流配置

工作流使用 **JSON** 格式定义，存放在程序运行目录下的 `Workflows/` 文件夹中。

### 文件结构

```json
{
  "Name": "工作流名称",
  "Description": "工作流描述（可选）",
  "Commands": [
    { "Type": "命令类型", "参数1": "值1", "参数2": "值2" }
  ]
}
```

### 完整示例

```json
{
  "Name": "焊接与视觉检测",
  "Description": "回零 → 定位 → 触发视觉 → 等待结果 → 延时",
  "Commands": [
    { "Type": "Home", "Axis": 1, "HomePos": 0 },
    { "Type": "MoveAbs", "Axis": 1, "TargetPos": 10000, "Vel": 20.0, "Acc": 10.0 },
    { "Type": "WriteSignal", "TargetDevice": "dev-002", "SignalAddress": 100, "SignalValue": true },
    { "Type": "WaitSignal", "TargetDevice": "dev-002", "SignalAddress": 101, "ExpectValue": true },
    { "Type": "TriggerVision", "VisionServerIp": "192.168.1.100", "VisionServerPort": 503 },
    { "Type": "Delay", "DelayMs": 500 }
  ]
}
```

### 命令类型详解

| 命令类型 | 参数 | 说明 |
|----------|------|------|
| **Home** | `Axis`、`HomePos` | 启动回零并等待完成（超时 10s） |
| **MoveAbs** | `Axis`、`TargetPos`、`Vel`、`Acc` | 绝对定位并等待到位（超时 5s） |
| **WaitIO** | `IoIndex`、`ExpectValue` | 等待 DI 状态（超时 5s，支持 IO 强制模拟） |
| **Delay** | `DelayMs` | 简单延时 |
| **WriteSignal** | `TargetDevice`、`SignalAddress`、`SignalValue` | 向目标设备线圈写值 |
| **WaitSignal** | `TargetDevice`、`SignalAddress`、`ExpectValue` | 等待目标设备线圈（超时 10s） |
| **TriggerVision** | `VisionServerIp`、`VisionServerPort`、触发/忙/结果线圈、结果码寄存器、超时 | 触发视觉拍照并解析结果 |

> ⚠️ `TargetDevice` 使用设备 `DeviceId`（GUID），可在 `devices.json` 中查看。

### 多设备复用

`Axis` 参数是**占位符**。工作流在某个设备上执行时，系统会自动将 `Axis` 替换为该设备配置的轴号，因此同一个工作流文件可应用于不同设备。

---

## 🗄️ 数据库设计

系统支持 **SQLite / SQL Server / MySQL** 三种数据库，通过 `appsettings.json` 配置切换。默认 SQLite（`gts.db`，自动创建）。

### 表结构

#### 1. Users（用户表）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | INTEGER | 主键，自增 |
| Username | TEXT UNIQUE | 登录用户名 |
| PasswordHash | TEXT | PBKDF2-SHA256 哈希密码 |
| Salt | TEXT | 密码盐值（兼容旧格式） |
| FullName | TEXT | 用户全名 |
| Role | TEXT | Admin / Engineer / Operator |
| IsActive | INTEGER | 是否启用 |
| CreatedTime | TEXT | 创建时间 |
| FailedAttempts | INTEGER | 连续失败次数 |
| LockoutUntil | TEXT | 锁定到期时间 |
| IsDeleted | INTEGER | 逻辑删除标记 |
| DeletedTime | TEXT | 删除时间 |
| DeletedBy | TEXT | 删除操作人 |

#### 2. AuditLogs（审计日志表）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | INTEGER | 主键，自增 |
| UserId | INTEGER | 操作用户 ID |
| Username | TEXT | 操作用户名 |
| ActionType | TEXT | 操作类型 |
| Detail | TEXT | 操作详情 |
| Timestamp | TEXT | 操作时间 |

#### 3. ProductionRecords（生产记录表 - 热数据）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | INTEGER | 主键，自增 |
| DeviceId | TEXT | 设备 ID |
| Timestamp | TEXT | 记录时间 |
| CurrentCount | INTEGER | 当前产量 |
| TargetCount | INTEGER | 目标产量 |

> 主表仅保留最近 **3 个月** 数据，由归档服务保证查询性能。

#### 4. ProductionRecords_History（生产记录归档表）

与 `ProductionRecords` 结构完全一致，存放 3 个月前的冷数据。

#### 5. AlarmRecords（报警记录表）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | INTEGER | 主键，自增 |
| DeviceId | TEXT | 关联设备 ID |
| Message | TEXT | 报警消息 |
| Severity | TEXT | Info / Warning / Error / Critical |
| Timestamp | TEXT | 触发时间 |
| IsAcknowledged | INTEGER | 是否已确认 |
| IsResolved | INTEGER | 是否已解决 |
| AcknowledgedBy / ResolvedBy | TEXT | 确认人 / 解决人 |
| AcknowledgedTime / ResolvedTime | TEXT | 确认时间 / 解决时间 |

#### 6. MesPendingRecords（MES 待重传表）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | INTEGER | 主键，自增 |
| Barcode | TEXT | 产品条码 |
| DeviceId | TEXT | 设备 ID |
| Station | TEXT | 工位名称 |
| Result | TEXT | 检测结果 |
| DiameterMm | REAL | 直径 |
| X / Y | REAL | 坐标 |
| ImagePath | TEXT | 图片路径 |
| Timestamp | TEXT | 记录时间 |
| RetryCount | INTEGER | 重试次数 |
| FailReason | TEXT | 失败原因 |
| NextRetryTime | TEXT | 下次重试时间 |

---

## 📁 项目结构

```text
GtsTest/
├── .gitignore
├── GtsTest.sln
├── README.md
├── LICENSE
├── Docs/
│   ├── Architecture.md
│   ├── Modbus_Protocol.md
│   ├── MES_Integration.md
│   └── Test_Report.md
├── Images/
│   └── （界面截图，见"界面预览"章节）
│
└── GtsTest/
    ├── GtsTest.csproj
    ├── Program.cs
    ├── ServiceCollectionExtensions.cs
    ├── appsettings.json
    ├── mes_config.json
    │
    ├── Data/                          # EF Core 数据层
    │   ├── GtsDbContext.cs
    │   ├── DbContextFactory.cs
    │   ├── DatabaseProvider.cs
    │   ├── EfDataRepository.cs
    │   ├── LoggingConfig.cs
    │   └── SqlDialect.cs
    │
    ├── Core/                          # 核心基础设施
    │   ├── AppLogger.cs
    │   ├── CyclicMonitorBuffer.cs
    │   ├── DeviceManager.cs
    │   ├── DeviceStateMachine.cs
    │   ├── GtsModel.cs
    │   ├── Watchdog.cs
    │   ├── WorkflowEngine.cs
    │   └── gts.cs                     # 需自行获取
    │
    ├── Commands/                      # 工作流命令
    │   ├── IMotionCommand.cs
    │   ├── MotionCommandBase.cs
    │   ├── CommandConfig.cs
    │   ├── CommandFactory.cs
    │   ├── HomeCommand.cs
    │   ├── MoveAbsCommand.cs
    │   ├── WaitIOCommand.cs
    │   ├── DelayCommand.cs
    │   ├── SequenceCommand.cs
    │   ├── TriggerVisionCommand.cs
    │   ├── WriteSignalCommand.cs
    │   ├── WaitSignalCommand.cs
    │   └── WorkflowConfig.cs
    │
    ├── Controls/                      # UI 用户控件
    │   ├── CommunicationControl.cs
    │   ├── MqttControl.cs
    │   ├── OverviewControl.cs
    │   ├── WorkflowControl.cs
    │   └── WorkflowExecutionControl.cs
    │
    ├── Forms/                         # WinForms 窗体
    │   ├── Form1.cs / Form1.Designer.cs
    │   ├── LoginForm.cs / .Designer.cs
    │   ├── DeviceConfigForm.cs / .Designer.cs
    │   ├── ModbusConfigForm.cs / .Designer.cs
    │   ├── DebugToolboxForm.cs / .Designer.cs
    │   ├── ResetPasswordDialog.cs
    │   ├── UserDialog.cs
    │   └── SystemConfigForm.cs / .Designer.cs
    │
    ├── Modbus/                        # Modbus 通信模块
    │   ├── ModbusClient.cs
    │   ├── ModbusConfig.cs
    │   ├── ModbusConnectionEventArgs.cs
    │   └── ModbusFormatter.cs
    │
    ├── Presenters/                    # MVP 表现层
    │   ├── IGtsView.cs
    │   ├── GtsPresenter.cs
    │   ├── IMqttView.cs
    │   └── MqttPresenter.cs
    │
    ├── Services/                      # 服务层
    │   ├── Alarm/                     # 报警管理
    │   ├── Authentication/            # 认证授权
    │   ├── Data/                      # 数据服务（含 DatabaseArchiveService）
    │   ├── Logging/                   # 日志包装
    │   ├── OpcUa/                     # OPC UA 客户端
    │   ├── Mes/                       # MES 上报（REST + SOAP）
    │   ├── Plc/                       # PLC 驱动（西门子 + 三菱）
    │   ├── Serial/                    # 串口驱动
    │   ├── MqttService.cs
    │   └── IMqttPublisher.cs
    │
    ├── Diagnostics/                   # 诊断工具
    │   ├── FrameMonitorForm.cs
    │   ├── FrameMonitorHub.cs
    │   ├── FrameLogEntry.cs
    │   ├── RingBuffer.cs
    │   └── DiagnosticPackageBuilder.cs
    │
    └── Models/                        # 数据模型
        ├── DeviceConfig.cs
        ├── DeviceRuntime.cs
        └── User.cs
```

---

## 🔐 权限管理

| 角色 | 权限 |
|------|------|
| **未登录** | 大部分操作禁用 |
| **Admin** | 全部权限（包括用户管理、审计查看） |
| **Engineer** | 查看/编辑配置、管理设备、运行/停止工作流 |
| **Operator** | 启动/停止设备、确认/解决报警、执行生产操作 |

所有关键操作写入审计日志（`AuditLogs` 表）。

---

## 📡 通信协议支持

| 协议 | 状态 | 说明 |
|------|------|------|
| **Modbus TCP** | ✅ | NModbus |
| **Modbus RTU** | ✅ | NModbus.Serial |
| **OPC UA** | ✅ | OPC Foundation 官方 SDK |
| **MQTT** | ✅ | MQTTnet |
| **Siemens S7** | ✅ | S7NetPlus，支持 S7-1200/1500/300/400/200Smart |
| **三菱 MC** | ✅ | HslCommunication，支持 FX / Q / L 系列 |
| **RS232/485** | ✅ | System.IO.Ports，4 种帧格式 |
| **MES REST** | ✅ | HttpClient |
| **MES SOAP** | ✅ | 手写 SOAP 1.1/1.2 Envelope |

### 使用示例

**OPC UA**

```csharp
await opcClient.ConnectAsync("opc.tcp://localhost:4840");
opcClient.Subscribe("ns=3;i=1001");
var value = await opcClient.ReadNodeValueAsync<double>("ns=3;i=1001");
```

**MQTT**

```csharp
await mqttService.ConnectAsync("broker.emqx.io", 1883);
await mqttService.SubscribeAsync("test/topic");
await mqttService.PublishAsync("test/topic", "Hello MQTT", retain: false);
```

**Siemens S7 PLC**

```csharp
var config = new PlcConfig
{
    Type = PlcType.SiemensS1200,
    IpAddress = "192.168.1.10",
    Rack = 0, Slot = 1, Name = "PLC1"
};
var plcClient = new SiemensS7Client(config);
plcClient.Connect();
plcClient.ReadFloat("DB1.DBD0", out float value);
plcClient.WriteInt("DB1.DBW2", 1234);
```

**三菱 MC PLC**

```csharp
var config = new PlcConfig
{
    Type = PlcType.MitsubishiMc,
    IpAddress = "192.168.1.20",
    Port = 6000, Name = "Mitsubishi1"
};
var plcClient = new MitsubishiPlcClient(config);
plcClient.Connect();
plcClient.ReadInt("D100", out int value);
plcClient.WriteBool("M100", true);
```

**RS232/485 串口**

```csharp
var config = new SerialPortConfig
{
    PortName = "COM3", BaudRate = 9600,
    DataBits = 8, StopBits = StopBits.One, Parity = Parity.None,
    FrameType = SerialFrameType.Delimiter,
    Delimiter = new byte[] { 0x0D, 0x0A }
};
var driver = new SerialPortDriver(config);
driver.FrameReceived += (s, frame) => Console.WriteLine($"RX: {BitConverter.ToString(frame)}");
driver.Open();
driver.Send("Hello\r\n");
```

---

## 🏛️ 系统配置中心

通过主界面顶部 **"🔧 系统管理"** 按钮进入，包含 **6 个** 选项卡：

### 1. 工作流

- 工作流列表下拉选择、步骤列表展示
- 新建/保存/运行/停止工作流
- 步骤增删改、上移/下移
- 命令类型选择及参数配置

### 2. OPC UA

- 服务器地址输入、连接/断开、节点 ID 订阅/取消订阅、实时数据日志

### 3. MQTT

- Broker 地址/端口/用户名/密码、连接/断开、主题订阅/发布

### 4. 调试工具（6 个子 Tab）

- **轴控制**：回零/定位/点动/使能/去使能/停止轴/复位报警
- **Modbus**：读写寄存器/线圈
- **设备控制**：启动/停止/配置 + IO 强制模拟
- **视觉触发**：手动测试 TriggerVision 命令
- **串口调试**：串口参数配置、HEX/ASCII 发送、帧格式、实时接收
- **PLC 调试**：支持模拟/西门子/三菱 MC 寄存器读写

### 5. 系统工具

- 系统控制：初始化运动控制卡、热加载配置（含差异报告）、保存配置
- 模拟模式切换（模拟 ↔ 真实）
- 运维工具：导出黑匣子、清空日志、系统诊断、一键诊断包
- **日志级别**：下拉框实时切换

### 6. 用户管理

- 用户列表（用户名/全名/角色/状态/创建时间/失败次数/删除标记）
- 添加/编辑/切换状态/删除/重置密码
- 显示已删除用户

---

## 📝 配置文件说明

| 文件/目录 | 位置 | 用途 |
|-----------|------|------|
| `devices.json` | 程序运行目录 | 设备配置，启动时自动加载 |
| `Workflows/*.json` | `Workflows/` | 工作流定义 |
| `appsettings.json` | 程序运行目录 | 数据库 + 日志配置 |
| `mes_config.json` | 程序运行目录 | MES 上报配置（REST/SOAP 双协议） |
| `gts.db` | 程序运行目录 | SQLite 数据库（默认） |
| `Logs/` | 程序运行目录 | 日志目录 |

### `appsettings.json` 示例

```json
{
  "Database": {
    "Provider": "Sqlite",
    "ConnectionString": "Data Source=gts.db"
  },
  "Logging": {
    "Level": "Info",
    "MaxFileSizeMB": 10,
    "RetentionDays": 30
  }
}
```

**SQL Server**

```json
"Provider": "SqlServer",
"ConnectionString": "Server=(localdb)\\MSSQLLocalDB;Database=GtsDb;Trusted_Connection=True;TrustServerCertificate=True;"
```

**MySQL / MariaDB**

```json
"Provider": "MySql",
"ConnectionString": "Server=localhost;Port=3306;Database=GtsDb;User Id=root;Password=YourPwd;CharSet=utf8mb4;"
```

### `mes_config.json` 示例

**REST 模式**

```json
{
  "Enabled": true,
  "Protocol": "REST",
  "StationName": "Vision_Station_01",
  "ApiUrl": "http://localhost:8888/api/mes",
  "RestTimeoutMs": 10000,
  "AutoRetryEnabled": true,
  "AutoRetryIntervalSeconds": 30,
  "PauseAutoRetryThreshold": 100
}
```

**SOAP 模式**

```json
{
  "Enabled": true,
  "Protocol": "SOAP",
  "StationName": "Vision_Station_01",
  "SoapEndpoint": "http://127.0.0.1:8080/",
  "SoapTargetNamespace": "http://tempuri.org/",
  "SoapVersion": "1.1",
  "SoapMethodName": "ReportProduction",
  "SoapResultNode": "ReportProductionResult",
  "AutoRetryEnabled": true
}
```

---

## 📤 MES 上报服务

| 特性 | 说明 |
|------|------|
| **双协议支持** | REST（JSON）+ SOAP（XML），通过 `Protocol` 字段一键切换 |
| **异步队列** | 基于 `System.Threading.Channels`，不阻塞主线程 |
| **离线缓存** | 上报失败自动缓存到 `MesPendingRecords` 表 |
| **分级重传** | 超时/连接失败：5 秒快速重试；5xx：30→60→120 秒指数退避；4xx：不自动重传 |
| **健康监控** | 独立看门狗每 10 秒探测，离线暂停重传，恢复自动重传 |
| **阈值保护** | 待重传积压超过阈值时暂停自动重传，避免雪崩 |
| **配置热重载** | 通过"热加载配置"按钮运行时切换协议 |
| **UI 集成** | 顶栏显示待重传数量 + 手动重传按钮 |

### 分级重传策略

| 失败类型 | 触发场景 | 首次重传延迟 | 最大重试次数 | 策略 |
|----------|----------|--------------|--------------|------|
| **Timeout** | 请求超时 | 5 秒 | 20 | 快速重试 |
| **ConnectionFailed** | 无法连接 MES | 5 秒 | 20 | 快速重试 |
| **ServerError** | HTTP 5xx | 30 秒 | 10 | 指数退避 |
| **ClientError** | HTTP 4xx | 不重传 | 0 | 人工介入 |

---

## 📡 Modbus 协议详解

### 视觉服务器寄存器映射

| 地址 | 名称 | R/W | 说明 |
|------|------|-----|------|
| **100** | 触发线圈 | W | 写 1 触发拍照；服务端处理完自动清 0 |
| **101** | 忙状态 | R | 1 = 处理中；0 = 空闲 |
| **102** | 整体结果 | R | 1 = OK；0 = NG |
| **1000** | 结果码 | R | 0=OK, 3=NG, 1=故障, 2=相机未连接, 99=系统错误 |
| **1003** | 直径 × 100 | R | 例如 2015 = 20.15 mm |
| **1004** | 缺陷数 | R | 缺陷计数 |
| **1005** | X 坐标 × 100 | R | Int16 有符号 |
| **1006** | Y 坐标 × 100 | R | Int16 有符号 |
| **1007** | 目标数量 | R | 检测到的目标个数 |

### 有符号数处理

Modbus 寄存器是**无符号 16 位**（0~65535），但坐标可能是负数。

**写入**：

```csharp
ushort raw = (ushort)(short)Math.Round(value * 100, MidpointRounding.AwayFromZero);
```

**读取**：

```csharp
short signed = (short)raw;
double value = signed / 100.0;
```

> ⚠️ **NModbus** 返回的 `ushort[]` **已是寄存器内原始值**，不要再做字节交换。

### 常见错误排查

| 现象 | 原因 | 解决方案 |
|------|------|----------|
| 连不上 503 端口 | 端口被占用 | 关闭 Modbus Slave |
| 读到全 0 | 未触发拍照 | 先写线圈 100 = 1 触发 |
| 读到 540.23 而非 20.03 | 客户端做了多余字节交换 | 去掉字节交换 |
| 坐标显示 62802 而非 -27.34 | 未做有符号转换 | 读取后 `(short)raw` |
| 值误差 0.01mm | 服务端截断而非四舍五入 | 用 `Math.Round` |

---

## 🆕 v1.1.0 新增能力

### 1. IO 强制模拟（现场调试神器）

**用途**：现场调试时，无需连接真实硬件，通过"强制" DI 值让工作流中的 `WaitIOCommand` 秒过。

**使用方式**：

1. 打开【系统管理】→【调试工具】→【设备控制】
2. 选择设备后，在【🔧 IO 强制模拟】区域：
   - IO 索引：填要强制的 IO 号
   - 勾选"强制为 True"
   - 点击【✅ 应用强制】

**代码集成**：

```csharp
// DeviceRuntime.cs
public Dictionary<int, bool> ForcedIOs { get; set; } = new();

// GtsModel.cs
public bool ReadDIWithForce(int ioIndex, DeviceRuntime? runtime)
{
    if (runtime != null && runtime.ForcedIOs.TryGetValue(ioIndex, out bool forcedValue))
        return forcedValue;
    return ReadDI(ioIndex);
}

// WaitIOCommand.cs 使用 ReadDIWithForce 替代 ReadDI
```

**测试验证**：50 次 Workflow 循环中，原本 5s 超时的 `WaitIO` 全部 65ms 秒过，成功率 100%。

### 2. 数据库冷热归档

**用途**：主表只保留 3 个月热数据，历史数据自动迁移到 `ProductionRecords_History`，保证查询性能。

**流程**：

- 主表 `ProductionRecords` 只保留 3 个月内数据
- 每天凌晨定时扫描
- 3 个月前的数据自动迁移到 `ProductionRecords_History`
- 冷热分离，主表查询性能始终保持在毫秒级

**核心代码**：

```csharp
INSERT INTO ProductionRecords_History (DeviceId, Timestamp, CurrentCount, TargetCount)
SELECT DeviceId, Timestamp, CurrentCount, TargetCount 
FROM ProductionRecords 
WHERE Timestamp < @Cutoff;

DELETE FROM ProductionRecords WHERE Timestamp < @Cutoff;
```

**测试验证**：手动插入 2 条 2024 年数据 + 1 条新数据，运行后：

- 主表剩余 1 条（新数据）
- History 表新增 2 条（旧数据）
- 幂等性正常（第二次运行迁移 0 条）

### 3. 三菱 MC 协议支持

**用途**：非标产线常混用多品牌 PLC，扩展支持三菱。

**依赖**：NuGet 包 `HslCommunication`。

**代码集成**：

```csharp
// PlcType.cs
public enum PlcType { ..., MitsubishiMc = 10 }

// PlcManager.cs
PlcType.MitsubishiMc => new MitsubishiPlcClient(config),

// MitsubishiPlcClient.cs
_plc = new MelsecMcNet(Config.IpAddress, Config.Port);
_plc.ConnectServer();
```

**测试验证**：连接 `192.168.1.10` 时优雅失败（弹出明确错误而非崩溃），用模拟 PLC 验证了接口契约完整性。

### 4. 热加载差异报告

**用途**：`devices.json` 重载前先 diff 对比，让工程师明确知道哪些设备被新增、移除、修改。

**预览效果**：

```text
📄 配置文件: devices.json
🕐 修改时间: 2026-09-26 17:35:44
──────────────────────────────
📊 变更总览: 新增 1 台 / 移除 1 台 / 修改 2 台 / 未变 1 台

【➕ 新增 1 台】
   • 设备4-包装 (dev-004)  →  192.168.1.13:502, 轴=4

【➖ 移除 1 台】
   • 设备3-旧料仓 (dev-003)  ⚠️ 当前产量: 47/100

【✏️ 修改 2 台】
   • 设备1-焊接 (dev-001)
        Modbus IP: 192.168.1.10 → 192.168.1.20
        目标产量: 100 → 200

【✔ 未变化 1 台】
   • 设备5-备用 (dev-005)
```

**关键保护**：应用变更时快照当前产量，避免 JSON 里的旧产量覆盖运行中的真实产量。

### 5. DI 依赖注入

**用途**：统一装配 30+ 服务，替代分散的 `new` 和静态类。

**核心位置**：`ServiceCollectionExtensions.cs`。

**关键注册**：

```csharp
services.AddSingleton<ILogger, AppLoggerWrapper>();
services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
services.AddSingleton<IUserRepository, EfDataRepository>();
services.AddSingleton<IAlarmManager, AlarmManager>();
services.AddSingleton<DatabaseArchiveService>();
services.AddSingleton<PlcManager>();
services.AddSingleton<MesReportService>();
services.AddSingleton<DeviceManager>();
services.AddTransient<Form1>();
```

---

## 🔧 扩展点

| 扩展点 | 位置 | 方法 |
|--------|------|------|
| **新增工作流命令** | `Commands/` | 实现 `IMotionCommand`，在 `CommandFactory` 注册 |
| **新增 PLC 品牌** | `Services/Plc/` | 实现 `IPlcClient`，在 `PlcManager.CreateClient` 注册 |
| **新增数据库** | `Data/DbContextFactory.cs` | 在 `ConfigureOptions` 添加 `case` |
| **新增 MES 协议** | `Services/Mes/MesReportService.cs` | 添加 `PostToMesByXxxAsync` 方法 |
| **新增串口帧格式** | `Services/Serial/Frames/` | 实现 `ISerialFrameParser` |
| **新增归档策略** | `Services/Data/DatabaseArchiveService.cs` | 修改 `ArchiveLoopAsync` |

---

## 🧪 测试报告摘要

### 数据库多适配测试

| 数据库 | 检查项 | 结果 |
|--------|--------|------|
| SQLite | 自动建库/建表/用户 CRUD | ✅ |
| SQL Server (LocalDB) | 连接/自动建表/数据隔离 | ✅ |
| MySQL / MariaDB | utf8mb4/中文读写/自动建表 | ✅ |

### IO 强制模拟测试

- **测试工作流**：`WaitIO(IoIndex=5, Expect=true)` → `Delay(1000)`
- **测试结果**：
  - 未强制 → 5s 超时失败
  - 应用强制后 → **65ms 秒过**，连续 6 个周期稳定
  - 清除强制后 → 恢复 5s 超时（说明强制是精确的，非全局替换）

### 数据库归档测试

- **测试数据**：手动插入 2 条 2024 年数据 + 1 条 2026-09 数据
- **归档结果**：
  - 第 1 次运行：`迁移了 2 条历史记录`
  - 主表剩余 1 条、History 表新增 2 条
  - 第 2 次运行：`迁移了 0 条`（幂等性正确）

### 三菱 PLC 测试

- **测试方式 1**：连接 `192.168.1.10:102`（无真实 PLC）
  - 结果：优雅失败，弹出 `PLC 连接失败: 192.168.1.10`
  - 意义：客户端正确创建、正确发起请求、错误可捕获
- **测试方式 2**：模拟 PLC 读写验证
  - `ReadShort("DB1.DBW0")` → 100 ✅
  - `WriteShort("DB1.DBW0", 200)` → 读回 200 ✅
  - 意义：接口契约完整

### RS232/485 串口驱动测试

- **虚拟串口对**：COM7 ↔ COM8（com0com）
- **测试工具**：UartAssist V5.0.14
- **帧格式**：原始流 / 分隔符（`\r\n`）/ 定长（8 字节）/ 头+长度（`AA 55` + 长度 + CRC16）
- **自动重连**：拔插虚拟串口 3 秒后自动重连 ✅

### Siemens S7 PLC 驱动测试

- **PLC 模拟器**：Snap7 Server Demo 1.4.2，监听 `0.0.0.0:102`
- **连接**：S7-1200 / Rack=0 / Slot=1 ✅
- **读写**：`DB1.DBW0` = 0 → 写 1234 → 读回 1234 ✅
- **类型覆盖**：Bool / Short / UShort / Int / Float / Double ✅
- **自动重连**：关闭/重启 Snap7 → 5 秒内自动重连 ✅

### MES 上报测试

- **REST 模式**：Python Flask `http://localhost:8888/api/mes`，连续 10 条上报全部成功 ✅
- **SOAP 模式**：Python 标准库 `http://127.0.0.1:8080/`，SOAP 1.1 Envelope 构造/解析正确 ✅
- **离线缓存**：关闭 MES → 5 条数据缓存 → 重启 MES → 健康监控判定在线后自动全量重传 ✅
- **配置热重载**：REST → SOAP 无需重启 ✅

### 视觉服务集成测试（HalconVisionServer）

- **测量对象**：20mm 金属圆片
- **算法演进**：
  - **V1**：Canny + 圆拟合，极差 42.7 μm，标准差 9.1 μm
  - **V2**：增加圆度筛选（≥0.65）+ 半径约束（83~98px），极差 30.4 μm
  - **V3**：增加拟合残差双重校验（平均残差 ≤1.2px，标准差 ≤0.8px），**极差 15.6 μm、标准差 3.8 μm** 🏆
- **判定**：达到工业级稳定标准（< 20 μm）
- **Modbus 数据一致性**：直径/坐标/缺陷数与视觉服务端完全一致 ✅
- **内存稳定性**：连续 1000 次图像迭代，内存增长 < 3MB ✅
- **线程安全修复**：
  - 修复 `GenImageInterleaved` 悬空指针（CopyData 缺失导致）
  - 修复跨线程 HObject 共享（改用 CopyImage 深拷贝）

### 综合性能

| 场景 | 性能指标 |
|------|----------|
| 单设备工作流循环（Home → MoveAbs → TriggerVision → Delay） | 约 1.2 秒/轮 |
| 3 设备并行循环 | 无相互阻塞，各设备独立线程 |
| MES 上报吞吐 | 约 2 条/秒（REST） |
| 内存占用（3 设备 + MES + 视觉） | 稳定在 150 MB 左右 |
| 归档服务单次耗时（100 万条） | < 5 秒 |

### 稳定性

- **连续运行 8 小时**：无内存泄漏，无崩溃
- **工作流循环 1000 轮**：全部成功，无漏拍
- **MES 断连/恢复 10 次**：全部自动恢复，无数据丢失
- **PLC 断连/恢复 5 次**：全部自动重连成功
- **IO 强制模拟 50 次测量**：100% 有效

---

## 📅 版本历史

### v1.1.0（2026-09-26）

**新增功能**：

- 三菱 MC 协议支持（HslCommunication）
- IO 强制模拟（现场调试无硬件干扰）
- 数据库冷热归档服务（主表只保留 3 个月）
- 热加载配置差异报告（新增/移除/修改/未变清单）
- DI 依赖注入（Microsoft.Extensions.DependencyInjection）
- 一键诊断包导出（日志+配置+数据库+截图）

**修复**：

- 修复 HALCON 视觉服务 `GenImageInterleaved` 悬空指针
- 修复跨线程 HObject 共享导致的内存泄漏
- 修复工作流暂停后仍自动重试的问题

**优化**：

- 视觉测量极差从 42.7 μm 优化到 15.6 μm
- 通过缓存 + 深拷贝减少图像转换开销

### v1.0.0（2026-08）

- 初版发布：多设备并行控制、GTS 运动控制卡、Modbus TCP/RTU、工作流引擎、用户权限、SQLite 持久化

---

## 🔧 待开发功能

| 功能 | 接口/类 | 优先级 | 说明 |
|------|---------|--------|------|
| **报警 UI 列表** | `Form1` 报警列表控件 | 高 | 报警后端已完成，需添加 UI 列表 |
| **MQTT 发布器（扩展）** | `IMqttPublisher` | 中 | 设备状态、报警、生产数据推送至云端 |
| **生产统计报表** | `Services/ReportService` | 低 | 按日/周/月生成产量报表 |
| **远程诊断** | `Services/RemoteDiagnostic` | 低 | TCP/WebSocket 实现远程日志查看 |
| **配方云端同步** | `Services/RecipeSync` | 低 | 多设备配方一键下发 |
| **HALCON 深度优化** | HalconVisionServer | 中 | `measure_pos` 卡尺算法替代 Canny，目标极差 < 10 μm |

---

## 🤝 贡献指南

### 新增命令类型

1. 在 `Commands/` 创建新类，实现 `IMotionCommand`
2. 继承 `MotionCommandBase` 并实现 `ExecuteCore`
3. 在 `CommandFactory.cs` 注册
4. 在 `CommandConfigDialog` 添加 UI 配置项

### 新增通信协议

参考 `Services/Plc/` 或 `Services/Serial/` 的目录结构，创建接口 + 实现 + 管理器三件套。

### 代码规范

- 遵循 C# 编码规范
- 使用 `AppLogger` 记录关键操作
- 所有公共 API 需包含 XML 文档注释
- **HALCON HObject 管理规范**：
  - 跨线程传递前必须 `CopyImage` 深拷贝
  - 每次生成都有对应的 `Dispose()`，且放在 `finally`
  - 禁用 `async void` 传递 HObject

---

## 📄 许可证

[MIT](LICENSE)

---

**Made with ❤️ for industrial automation**