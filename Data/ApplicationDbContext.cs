
using BE_TRELLO.Entities.Auth;

using Microsoft.EntityFrameworkCore;

namespace BE_TRELLO.Data;


public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Users> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Có thể cấu hình thêm các quan hệ giữa các bảng tại đây nếu cần

        _ = builder.Entity<Users>(entity =>
        {
            _ = entity.HasKey(u => u.UserId);

            _ = entity.Property(u => u.UserId)
                .HasDefaultValueSql("NEWID()");

            _ = entity.Property(u => u.UserName).HasMaxLength(100).IsRequired(true);
            _ = entity.HasIndex(u => u.UserName).IsUnique();



            _ = entity.Property(u => u.Email).HasMaxLength(100).IsRequired(false);
            _ = entity.HasIndex(u => u.Email).IsUnique().HasFilter("[Email] IS NOT NULL"); ;

            _ = entity.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired(false);
            _ = entity.Property(u => u.GoogleId).HasMaxLength(100).IsRequired(false);
            _ = entity.Property(u => u.IsActived).HasDefaultValue(true);
        });


    }
}