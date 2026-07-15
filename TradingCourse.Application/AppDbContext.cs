using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TradingCourse.Application.Models;

namespace TradingCourse.Application;

public class AppDbContext : IdentityDbContext<AdminUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Course> Courses { get; set; }
    public DbSet<Coupon> Coupons { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<HomepageBanner> HomepageBanners { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Course configurations
        builder.Entity<Course>()
            .HasIndex(c => c.Slug)
            .IsUnique();

        builder.Entity<Course>()
            .Property(c => c.Price)
            .HasPrecision(18, 2);

        builder.Entity<Course>()
            .Property(c => c.SalePrice)
            .HasPrecision(18, 2);

        // Coupon configurations
        builder.Entity<Coupon>()
            .HasIndex(c => c.Code)
            .IsUnique();

        builder.Entity<Coupon>()
            .Property(c => c.DiscountValue)
            .HasPrecision(18, 2);

        builder.Entity<Coupon>()
            .Property(c => c.MinPurchaseAmount)
            .HasPrecision(18, 2);

        builder.Entity<Coupon>()
            .HasOne(c => c.Course)
            .WithMany()
            .HasForeignKey(c => c.CourseId)
            .OnDelete(DeleteBehavior.SetNull);

        // Order configurations
        builder.Entity<Order>()
            .HasIndex(o => o.CustomerEmail);

        builder.Entity<Order>()
            .Property(o => o.AmountPaid)
            .HasPrecision(18, 2);

        builder.Entity<Order>()
            .HasOne(o => o.Course)
            .WithMany()
            .HasForeignKey(o => o.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
