using GtsTest.Core;
using GtsTest.Data;
using GtsTest.Services.Logging;
using System;
using System.Threading.Tasks;

namespace GtsTest.Services.Data
{
    /// <summary>
    /// 生产记录实例实现：通过 IProductionRepository 屏蔽数据库差异
    /// </summary>
    public class ProductionServiceImpl : IProductionService
    {
        private readonly IProductionRepository _repository;
        private readonly ILogger _logger;

        /// <summary>无参兜底构造（静态兼容层 / 单元测试使用，内部自组装依赖）</summary>
        public ProductionServiceImpl()
            : this(new ProductionRepository(new DbConnectionFactory(), new AppLoggerWrapper()),
                   new AppLoggerWrapper())
        { }

        /// <summary>DI 注入构造</summary>
        public ProductionServiceImpl(IProductionRepository repository, ILogger logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? new AppLoggerWrapper();
        }

        public void RecordProduction(string deviceId, int current, int target)
        {
            Task.Run(() =>
            {
                try
                {
                    _repository.Insert(deviceId, current, target, DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    try { _logger.Warn($"记录产量失败: {ex.Message}", "Production"); } catch { }
                }
            });
        }
    }
}