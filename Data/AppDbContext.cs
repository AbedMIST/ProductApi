using Microsoft.EntityFrameworkCore;
using ProductApi.Models;

namespace ProductApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(e =>
        {
            e.Property(p => p.Name).HasMaxLength(100).IsRequired();
            e.Property(p => p.Description).HasMaxLength(500);
            e.Property(p => p.Price).HasPrecision(18, 2);
        });

        modelBuilder.Entity<User>(e =>
        {
            e.Property(u => u.Username).HasMaxLength(50).IsRequired();
            e.HasIndex(u => u.Username).IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.Property(t => t.TokenHash).HasMaxLength(64).IsRequired(); // SHA-256 hex
            e.HasIndex(t => t.TokenHash).IsUnique();
            e.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
