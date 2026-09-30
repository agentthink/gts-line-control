using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GtsTest.Services.Data
{
    [Table("AuditLogs")]
    public class AuditLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long UserId { get; set; }
        public string Username { get; set; } = "";
        public string ActionType { get; set; } = "";
        public string Detail { get; set; } = "";
        public string Timestamp { get; set; } = "";
    }
}