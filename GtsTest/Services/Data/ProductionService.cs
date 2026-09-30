using System;

namespace GtsTest.Services.Data
{
    /// <summary>
    /// 【静态兼容层】保留原有的静态调用入口（ProductionService.RecordProduction(...)）
    /// 内部委托到 IProductionService 实例，默认指向 SQLite 实现。
    /// 应用启动时通过 SetDefault() 注入 DI 实例。
    /// </summary>
    public static class ProductionService
    {
        private static IProductionService _default = new ProductionServiceImpl();

        /// <summary>
        /// 设置默认实例（由 DI 容器装配时调用）
        /// </summary>
        public static void SetDefault(IProductionService service)
            => _default = service ?? throw new ArgumentNullException(nameof(service));

        /// <summary>异步记录产量更新（非阻塞）</summary>
        public static void RecordProduction(string deviceId, int current, int target)
            => _default.RecordProduction(deviceId, current, target);
    }
}