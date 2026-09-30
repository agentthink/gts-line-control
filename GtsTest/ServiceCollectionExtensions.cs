using GtsTest.Core;
using GtsTest.Data;
using GtsTest.Forms; // 🆕 引入 Forms 以支持 DebugToolboxForm 注册
using GtsTest.Services.Alarm;
using GtsTest.Services.Authentication;
using GtsTest.Services.Data;
using GtsTest.Services.Logging;
using GtsTest.Services.Mes;
using GtsTest.Services.Plc; // 🆕 引入 Plc 以支持 PlcManager
using Microsoft.Extensions.DependencyInjection;

namespace GtsTest
{
    /// <summary>
    /// DI 装配中心：统一管理所有服务生命周期
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGtsTestServices(this IServiceCollection services)
        {
            // ============================================================
            // 1. 数据库初始化（启动时执行一次）
            // ============================================================
            DbContextFactory.ConfigureFromAppSettings();
            DbContextFactory.EnsureDatabase();

            // ============================================================
            // 2. 基础设施 (Infrastructure)
            // ============================================================
            services.AddSingleton<ILogger, AppLoggerWrapper>();
            services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

            // ============================================================
            // 3. 数据仓储层 (Repositories)
            // ============================================================
            // 用户仓储（无状态，单例安全）
            services.AddSingleton<EfDataRepository>();
            services.AddSingleton<IUserRepository>(sp => sp.GetRequiredService<EfDataRepository>());
            services.AddSingleton<IDataRepository>(sp => sp.GetRequiredService<EfDataRepository>());

            // 业务仓储
            services.AddSingleton<IAuditRepository, AuditRepository>();
            services.AddSingleton<IProductionRepository, ProductionRepository>();
            services.AddSingleton<IMesPendingRepository, MesPendingRepository>();
            services.AddSingleton<IAlarmRepository, AlarmRepository>();

            // ============================================================
            // 4. 核心业务服务 (Core Services)
            // ============================================================
            // 数据服务
            services.AddSingleton<IAuditService, AuditServiceImpl>();
            services.AddSingleton<IProductionService, ProductionServiceImpl>();

            // 认证与会话
            services.AddSingleton<IAuthenticationService, AuthenticationService>();
            services.AddSingleton<ISessionService, SessionServiceImpl>();

            // 🆕 数据库归档服务（冷热数据分离，保障百万级数据查询性能）
            services.AddSingleton<DatabaseArchiveService>();

            // 报警管理（注入仓储和日志）
            services.AddSingleton<IAlarmManager>(sp =>
                new AlarmManager(
                    sp.GetRequiredService<IAlarmRepository>(),
                    sp.GetRequiredService<ILogger>()));

            // ============================================================
            // 5. 硬件与通信层 (Hardware & Communication)
            // ============================================================
            // 运动控制模型
            services.AddSingleton<GtsModel>();

            // 🆕 多品牌 PLC 管理器（替代原有的手动 new PlcManager）
            services.AddSingleton<PlcManager>();

            // ============================================================
            // 6. MES 上报服务 (MES Integration)
            // ============================================================
            services.AddSingleton<MesReportService>(sp =>
            {
                var config = MesConfig.Load();
                return new MesReportService(
                    config,
                    sp.GetRequiredService<IMesPendingRepository>(),
                    sp.GetRequiredService<ILogger>());
            });

            // ============================================================
            // 7. 设备管理与工作流 (Device Management)
            // ============================================================
            services.AddSingleton<DeviceManager>(sp =>
            {
                var mgr = new DeviceManager(
                    sp.GetRequiredService<GtsModel>(),
                    sp.GetRequiredService<IDataRepository>(),
                    sp.GetRequiredService<IAlarmManager>(),
                    sp.GetRequiredService<ILogger>());

                // 注入 MES 服务
                mgr.MesService = sp.GetRequiredService<MesReportService>();
                return mgr;
            });

            // ============================================================
            // 8. 窗体与视图 (UI Forms)
            // ============================================================
            // 🆕 注意：Form1 构造函数增加了 PlcManager 参数
            services.AddTransient<Form1>(sp => new Form1(
                sp.GetRequiredService<DeviceManager>(),
                sp.GetRequiredService<IDataRepository>(),
                sp.GetRequiredService<IAuthenticationService>(),
                sp.GetRequiredService<IAlarmManager>(),
                sp.GetRequiredService<ILogger>(),
                sp.GetRequiredService<PlcManager>() // 传入 PLC 管理器
            ));

            services.AddTransient<LoginForm>();

            // 🆕 调试工具箱支持 DI 注入（如果未来在别的窗体中需要直接由 DI 创建）
            services.AddTransient<DebugToolboxForm>(sp => new DebugToolboxForm(
                sp.GetRequiredService<DeviceManager>(),
                sp.GetRequiredService<GtsModel>(),
                sp.GetRequiredService<IAlarmManager>(),
                sp.GetRequiredService<IDataRepository>(),
                sp.GetRequiredService<IAuthenticationService>(),
                sp.GetRequiredService<PlcManager>()
            ));

            return services;
        }
    }
}