using AzureOmsDemo.Models;
using Microsoft.EntityFrameworkCore;

namespace AzureOmsDemo.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.OrderCode)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(x => x.OrderCode)
                .IsUnique();

            entity.Property(x => x.TotalAmount)
                .HasPrecision(18, 2);

            entity.Property(x => x.Status)
                .HasMaxLength(30)
                .IsRequired();
        });
    }
}
