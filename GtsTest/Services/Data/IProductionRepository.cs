using System;

namespace GtsTest.Services.Data
{
    /// <summary>生产记录仓储接口</summary>
    public interface IProductionRepository
    {
        void EnsureTable();
        void Insert(string deviceId, int currentCount, int targetCount, DateTime timestamp);
    }
}