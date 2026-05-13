using Microsoft.EntityFrameworkCore;
using Auctions.Domain.Entities;

namespace Auctions.Infrastructure.Data;

public class AuctionsDbContext : DbContext
{
    public AuctionsDbContext(DbContextOptions<AuctionsDbContext> options)
        : base(options) { }

    public DbSet<Lot> Lots => Set<Lot>();
    public DbSet<Bid> Bids => Set<Bid>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureLot(modelBuilder);
        ConfigureBid(modelBuilder);
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
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(l => l.CurrentPrice)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(l => l.Status)
                .HasConversion<string>()
                .IsRequired();

            entity.HasMany(l => l.Bids)
                .WithOne(b => b.Lot)
                .HasForeignKey(b => b.LotId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(l => l.Status);
            entity.HasIndex(l => l.EndTime);
        });
    }

    private void ConfigureBid(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Bid>(entity =>
        {
            entity.ToTable("Bids");

            entity.HasKey(b => b.Id);

            entity.Property(b => b.Amount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(b => b.PlacedAt)
                .IsRequired();

            entity.HasIndex(b => b.LotId);
            entity.HasIndex(b => b.PlacedAt);
        });
    }
}
