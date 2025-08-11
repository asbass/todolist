using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using todolist.model;

namespace todolist.data
{
    public class AppDbContext : IdentityDbContext<User>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<WeeklyQuota> WeeklyQuotas { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Cấu hình quan hệ tránh vòng lặp khi delete
            builder.Entity<TaskItem>()
                .HasOne(t => t.AssignedTo)
                .WithMany() // nếu bạn không cần navigation từ User về các task
                .HasForeignKey(t => t.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict); // hoặc NoAction
        }
    }
   
}
