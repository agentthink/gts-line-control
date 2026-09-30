using System;

namespace GtsTest.Diagnostics
{
    /// <summary>
    /// 通信报文总线：任何模块都可以发布报文，UI 订阅显示
    /// </summary>
    public interface IFrameMonitorHub
    {
        /// <summary>发布一条报文（线程安全，非阻塞）</summary>
        void Publish(FrameLogEntry entry);

        /// <summary>清空缓冲区</summary>
        void Clear();

        /// <summary>拉取快照（maxCount=0 表示全部）</summary>
        FrameLogEntry[] Snapshot(int maxCount = 0);

        /// <summary>获取统计</summary>
        FrameStats GetStats();

        /// <summary>重置统计</summary>
        void ResetStats();

        /// <summary>批量通知：最多每 100ms 触发一次（UI 订阅这个）</summary>
        event Action? FramesUpdated;
    }
}