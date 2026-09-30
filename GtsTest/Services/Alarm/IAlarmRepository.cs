using GtsTest.Services.Data;
using System.Collections.Generic;

namespace GtsTest.Services.Alarm
{
    /// <summary>
    /// 报警数据仓储接口。
    /// 上层业务（AlarmManager）只关心"存/取报警"，不关心底层是 SQLite 还是 SqlServer。
    /// </summary>
    public interface IAlarmRepository
    {
        /// <summary>建表（幂等，可重复调用）</summary>
        void EnsureTable();

        /// <summary>插入一条报警，返回新生成的 Id</summary>
        int Insert(AlarmRecord record);

        /// <summary>更新报警记录（确认/解决时用）</summary>
        void Update(AlarmRecord record);

        /// <summary>查询所有未解决的报警（按时间降序）</summary>
        List<AlarmRecord> QueryActive();

        /// <summary>查询所有报警（含已解决，按时间降序）</summary>
        List<AlarmRecord> QueryAll();
    }
}