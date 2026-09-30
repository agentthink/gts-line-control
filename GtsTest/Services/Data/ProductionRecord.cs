using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GtsTest.Services.Data
{
    [Table("ProductionRecords")]
    public class ProductionRecord
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string DeviceId { get; set; } = "";
        public string Timestamp { get; set; } = "";
        public int CurrentCount { get; set; }
        public int TargetCount { get; set; }
    }
}