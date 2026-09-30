namespace GtsTest.Services.Data
{
    /// <summary>
    /// 生产记录服务接口
    /// </summary>
    public interface IProductionService
    {
        void RecordProduction(string deviceId, int current, int target);
    }
}