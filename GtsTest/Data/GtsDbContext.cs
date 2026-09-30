using GtsTest.Models;
using GtsTest.Services.Data;
using Microsoft.EntityFrameworkCore;

namespace GtsTest.Data
{
    public class GtsDbContext : DbContext
    {
        public GtsDbContext(DbContextOptions<GtsDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<AlarmRecord> AlarmRecords => Set<AlarmRecord>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<ProductionRecord> ProductionRecords => Set<ProductionRecord>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============ User ============
            modelBuilder.Entity<User>(e =>
            {
                e.ToTable("Users");
                e.HasIndex(u => u.Username).IsUnique();
            });

            // ============ AlarmRecord ============
            modelBuilder.Entity<AlarmRecord>(e =>
            {
                e.ToTable("AlarmRecords");
                e.HasIndex(a => a.Timestamp);
                e.HasIndex(a => a.IsResolved);

                // ⭐ 枚举 → string
                e.Property(a => a.Severity)
                    .HasConversion<string>()
                    .HasMaxLength(20);

                // ⭐ DateTime → ISO 8601 字符串
                e.Property(a => a.Timestamp)
                    .HasConversion(
                        v => v.ToString("o", System.Globalization.CultureInfo.InvariantCulture),
                        v => ParseDateTime(v));

                // ⭐ DateTime? → ISO 8601 字符串（可空）
                e.Property(a => a.AcknowledgedTime)
                    .HasConversion(
                        v => v.HasValue ? v.Value.ToString("o", System.Globalization.CultureInfo.InvariantCulture) : null,
                        v => ParseNullableDateTime(v));

                e.Property(a => a.ResolvedTime)
                    .HasConversion(
                        v => v.HasValue ? v.Value.ToString("o", System.Globalization.CultureInfo.InvariantCulture) : null,
                        v => ParseNullableDateTime(v));

                // ⭐ bool 默认会映射成 INTEGER(0/1) 或 BIT，EF 自动处理，无需配置
            });

            // ============ AuditLog ============
            modelBuilder.Entity<AuditLog>(e =>
            {
                e.ToTable("AuditLogs");
                e.HasIndex(a => a.Timestamp);
            });

            // ============ ProductionRecord ============
            modelBuilder.Entity<ProductionRecord>(e =>
            {
                e.ToTable("ProductionRecords");
                e.HasIndex(p => p.DeviceId);
                e.HasIndex(p => p.Timestamp);
            });
        }

        // ============ 辅助：字符串与 DateTime 互转 ============
        private static System.DateTime ParseDateTime(string? s)
        {
            if (string.IsNullOrEmpty(s)) return System.DateTime.MinValue;
            return System.DateTime.TryParse(s, out var dt) ? dt : System.DateTime.MinValue;
        }

        private static System.DateTime? ParseNullableDateTime(string? s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            return System.DateTime.TryParse(s, out var dt) ? dt : (System.DateTime?)null;
        }
    }
}