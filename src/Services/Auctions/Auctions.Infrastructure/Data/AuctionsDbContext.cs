using Microsoft.EntityFrameworkCore;
using Auctions.Domain.Entities;
using AuHub.Shared.Converters;

namespace Auctions.Infrastructure.Data;

public class AuctionsDbContext : DbContext
{
    public AuctionsDbContext(DbContextOptions<AuctionsDbContext> options)
        : base(options) { }

    public DbSet<Lot> Lots => Set<Lot>();
    public DbSet<Bid> Bids => Set<Bid>();
    public DbSet<LotImage> LotImages => Set<LotImage>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureLot(modelBuilder);
        ConfigureBid(modelBuilder);
        ConfigureLotImage(modelBuilder);
        ConfigureOutboxMessage(modelBuilder);
    }

    private void ConfigureLot(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Lot>(entity =>
        {
            entity.ToTable("Lots");

            entity.HasKey(l => l.Id);

            entity.Property(l => l.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(l => l.Description)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(l => l.StartingPrice)
                .HasConversion<MoneyConverter>()
                .IsRequired();

            entity.Property(l => l.CurrentPrice)
                .HasConversion<MoneyConverter>()
                .IsRequired();

            entity.Property(l => l.Status)
                .HasConversion<string>()
                .IsRequired();

            entity.Property(l => l.Duration)
                .HasConversion<long>()
                .IsRequired();

            entity.Property(l => l.AdminComment)
                .HasMaxLength(500);

            entity.Property(l => l.TrackingNumber)
                .HasMaxLength(100);

            entity.Property(l => l.DeliveryAddress)
                .HasMaxLength(500);

            entity.Property(l => l.DisputeReason)
                .HasMaxLength(1000);

            entity.HasMany(l => l.Bids)
                .WithOne(b => b.Lot)
                .HasForeignKey(b => b.LotId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(l => l.Images)
                .WithOne()
                .HasForeignKey(i => i.LotId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(l => l.Status);
            entity.HasIndex(l => l.EndTime);

            entity.Property(l => l.RowVersion)
                .IsRowVersion();

            entity.Property(l => l.IsDeleted)
                .IsRequired();

            entity.HasQueryFilter(l => !l.IsDeleted);
        });
    }

    private void ConfigureBid(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Bid>(entity =>
        {
            entity.ToTable("Bids");

            entity.HasKey(b => b.Id);

            entity.Property(b => b.Amount)
                .HasConversion<MoneyConverter>()
                .IsRequired();

            entity.Property(b => b.PlacedAt)
                .IsRequired();

            entity.HasIndex(b => b.LotId);
            entity.HasIndex(b => b.PlacedAt);
            entity.HasIndex(b => b.IdempotencyKey)
                .IsUnique()
                .HasFilter("\"IdempotencyKey\" IS NOT NULL");
        });
    }

    private void ConfigureLotImage(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LotImage>(entity =>
        {
            entity.ToTable("LotImages");

            entity.HasKey(i => i.Id);

            entity.Property(i => i.FileName)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(i => i.ObjectName)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(i => i.ContentType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(i => i.Size)
                .IsRequired();

            entity.HasIndex(i => i.LotId);
        });
    }

    private void ConfigureOutboxMessage(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("OutboxMessages");

            entity.HasKey(o => o.Id);

            entity.Property(o => o.Type)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(o => o.Payload)
                .IsRequired()
                .HasColumnType("text");

            entity.Property(o => o.Error)
                .HasMaxLength(2000);

            entity.HasIndex(o => o.ProcessedAt);
            entity.HasIndex(o => o.CreatedAt);
        });
    }
}
