using System.Collections.Generic;

namespace GtsTest.Services.Mes
{
    /// <summary>MES 待重传缓存仓储接口</summary>
    public interface IMesPendingRepository
    {
        void EnsureTable();

        /// <summary>插入一条待重传记录，返回新生成的 Id</summary>
        long Insert(MesReportData data);

        /// <summary>读取最多 limit 条待重传记录（按 Id 升序，保证 FIFO 顺序）</summary>
        List<MesReportData> QueryForRetry(int limit);

        /// <summary>更新重试次数 / 失败原因 / 下次重试时间</summary>
        void UpdateRetry(MesReportData data);

        /// <summary>删除指定 Id（重传成功后调用）</summary>
        void Delete(long id);

        /// <summary>当前积压数量</summary>
        int Count();
    }
}