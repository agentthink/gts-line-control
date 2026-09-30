using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GtsTest.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required, MaxLength(64)]
        public string Username { get; set; } = "";

        [Required]
        public string PasswordHash { get; set; } = "";

        public string? Salt { get; set; }

        [MaxLength(128)]
        public string FullName { get; set; } = "";

        [MaxLength(32)]
        public string Role { get; set; } = "Operator";

        public int IsActive { get; set; } = 1;
        public string CreatedTime { get; set; } = DateTime.UtcNow.ToString("o");
        public int FailedAttempts { get; set; } = 0;
        public string? LockoutUntil { get; set; }

        public int IsDeleted { get; set; } = 0;
        public string? DeletedTime { get; set; }
        public string? DeletedBy { get; set; }
    }
}