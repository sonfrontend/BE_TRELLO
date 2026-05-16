
using BE_ECOMMERCE.Entities.Auth;
using BE_ECOMMERCE.Entities.Product;
using BE_ECOMMERCE.Entities.Transaction;
using BE_ECOMMERCE.Entities.Category;

using Microsoft.EntityFrameworkCore;

namespace BE_ECOMMERCE.Data;


public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Category> Categories { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Có thể cấu hình thêm các quan hệ giữa các bảng tại đây nếu cần

        _ = builder.Entity<User>(entity =>
        {
            _ = entity.HasKey(u => u.UserId);

            _ = entity.Property(u => u.UserId)
                .HasDefaultValueSql("NEWID()");

            _ = entity.Property(u => u.UserName).HasMaxLength(100).IsRequired(true);
            _ = entity.HasIndex(u => u.UserName).IsUnique();



            _ = entity.Property(u => u.Email).HasMaxLength(100).IsRequired(false);
            _ = entity.HasIndex(u => u.Email).IsUnique().HasFilter("[Email] IS NOT NULL");

            _ = entity.Property(u => u.FullName).HasMaxLength(500).IsRequired(false);
            _ = entity.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired(false);
            _ = entity.Property(u => u.PhoneNumber).HasMaxLength(15).IsRequired(false);
            _ = entity.Property(u => u.RefreshToken).HasMaxLength(500).IsRequired(false);
            _ = entity.Property(u => u.RefreshTokenExpiryTime).IsRequired(false);
            _ = entity.Property(u => u.AvatarUrl).HasMaxLength(500).IsRequired(false);
            _ = entity.Property(u => u.GoogleId).HasMaxLength(100).IsRequired(false);
            _ = entity.Property(u => u.IsActived).HasDefaultValue(true);
        });

        _ = builder.Entity<Role>(entity =>
       {
           _ = entity.HasKey(u => u.RoleId);

           _ = entity.Property(u => u.RoleId)
               .HasDefaultValueSql("NEWID()");

           _ = entity.Property(u => u.RoleName).HasMaxLength(200).IsRequired(true);

       });

        _ = builder.Entity<Permission>(entity =>
       {
           _ = entity.HasKey(u => u.PermissionId);

           _ = entity.Property(u => u.PermissionId)
               .HasDefaultValueSql("NEWID()");

           _ = entity.Property(u => u.PermissionName).HasMaxLength(200).IsRequired(true);
           _ = entity.Property(u => u.Description).HasMaxLength(500).IsRequired(true);

       });

        _ = builder.Entity<RolePermission>(entity =>
       {
           _ = entity.HasKey(u => u.RolePermissionId);

           _ = entity.Property(u => u.RolePermissionId)
               .HasDefaultValueSql("NEWID()");

           _ = entity.Property(e => e.RoleId).IsRequired(true);
           _ = entity.Property(e => e.PermissionId).IsRequired(true);

           entity.HasOne(rp => rp.Role)
          .WithMany(r => r.RolePermissions)
          .HasForeignKey(rp => rp.RoleId);

           entity.HasOne(rp => rp.Permission)
          .WithMany(r => r.RolePermissions)
          .HasForeignKey(rp => rp.PermissionId);
           // Chống trùng key
           entity.HasIndex(rp => new { rp.RoleId, rp.PermissionId })
         .IsUnique();

       });

        _ = builder.Entity<UserRole>(entity =>
       {
           _ = entity.HasKey(u => u.UserRoleId);

           _ = entity.Property(u => u.UserRoleId)
               .HasDefaultValueSql("NEWID()");

           _ = entity.Property(e => e.UserId).IsRequired(true);
           _ = entity.Property(e => e.RoleId).IsRequired(true);

           entity.HasOne(ur => ur.User)
          .WithMany(u => u.UserRoles)
          .HasForeignKey(ur => ur.UserId);

           entity.HasOne(ur => ur.Role)
          .WithMany(r => r.UserRoles)
          .HasForeignKey(ur => ur.RoleId);

           // Chống trùng key
           entity.HasIndex(ur => new { ur.UserId, ur.RoleId })
         .IsUnique();
       });

        _ = builder.Entity<Category>(entity =>
        {
            _ = entity.HasKey(c => c.Id);

            _ = entity.HasOne(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        _ = builder.Entity<Product>(entity =>
        {
            _ = entity.HasKey(p => p.ArticleId);

            _ = entity.Property(p => p.ArticleId).IsRequired(true);
            _ = entity.Property(p => p.ProductCode).HasMaxLength(255).IsRequired(false);
            _ = entity.Property(p => p.ProductName).HasMaxLength(255).IsRequired(false);
            _ = entity.Property(p => p.Color).HasMaxLength(255).IsRequired(false);
            _ = entity.Property(p => p.Size).HasMaxLength(255).IsRequired(false);
            _ = entity.Property(p => p.Price).HasColumnType("decimal(18,2)");
            _ = entity.Property(p => p.ImageUrl).HasMaxLength(500).IsRequired(false);
            _ = entity.Property(p => p.Description).HasMaxLength(4000).IsRequired(false);

            // Configure relationship with Category
            _ = entity.HasOne(p => p.Categories)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        _ = builder.Entity<Transaction>(entity =>
        {
            _ = entity.HasKey(t => t.Id);

            _ = entity.HasOne<Product>()
                .WithMany()
                .HasForeignKey(t => t.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}