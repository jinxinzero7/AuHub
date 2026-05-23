using Microsoft.EntityFrameworkCore;
using Notifications.Domain.Entities;
using Notifications.Domain.Enums;

namespace Notifications.Infrastructure.Data;

public class NotificationsDbContext : DbContext
{
    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : base(options) { }

    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Notification>(builder =>
        {
            builder.HasKey(n => n.Id);

            builder.Property(n => n.UserId)
                .IsRequired();

            builder.Property(n => n.Title)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(n => n.Message)
                .HasMaxLength(1000)
                .IsRequired();

            builder.Property(n => n.Type)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(n => n.IsRead)
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(n => n.CreatedAt)
                .IsRequired();

            builder.HasIndex(n => n.UserId);
            builder.HasIndex(n => n.IsRead);
            builder.HasIndex(n => n.CreatedAt);
        });
    }
}
