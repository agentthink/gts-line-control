using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GtsTest.Services.Data
{
    public enum AlarmSeverity { Info, Warning, Error, Critical }

    [Table("AlarmRecords")]
    public class AlarmRecord
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string? DeviceId { get; set; }
        public string Message { get; set; } = "";

        // ⭐ 保持原始类型（枚举、DateTime、bool），数据库映射交给 EF 转换器
        public AlarmSeverity Severity { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsAcknowledged { get; set; }
        public bool IsResolved { get; set; }
        public string? AcknowledgedBy { get; set; }
        public string? ResolvedBy { get; set; }
        public DateTime? AcknowledgedTime { get; set; }
        public DateTime? ResolvedTime { get; set; }
    }
}