using System;

namespace GtsTest.Services.Mes
{
    /// <summary>
    /// MES 上报数据
    /// </summary>
    public class MesReportData
    {
        /// <summary>数据库主键（重传时使用）</summary>
        public long Id { get; set; }

        /// <summary>产品条码</summary>
        public string Barcode { get; set; } = "";

        /// <summary>设备 ID</summary>
        public string DeviceId { get; set; } = "";

        /// <summary>工位</summary>
        public string Station { get; set; } = "";

        /// <summary>结果（OK / NG）</summary>
        public string Result { get; set; } = "OK";

        /// <summary>直径（mm）</summary>
        public double DiameterMm { get; set; }

        /// <summary>X 坐标</summary>
        public double X { get; set; }

        /// <summary>Y 坐标</summary>
        public double Y { get; set; }

        /// <summary>图片路径</summary>
        public string? ImagePath { get; set; }

        /// <summary>时间戳</summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>重试次数（用于分级重传）</summary>
        public int RetryCount { get; set; }

        /// <summary>失败原因类型</summary>
        public string? FailReason { get; set; }

        /// <summary>下次重试时间（ISO 8601）</summary>
        public string? NextRetryTime { get; set; }
    }
}