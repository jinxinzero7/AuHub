using Microsoft.EntityFrameworkCore;
using Auctions.Domain.Entities;

namespace Auctions.Infrastructure.Data;

public class AuctionsDbContext : DbContext
{
    public AuctionsDbContext(DbContextOptions<AuctionsDbContext> options)
        : base(options) { }

    public DbSet<Lot> Lots => Set<Lot>();
    public DbSet<Bid> Bids => Set<Bid>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureLot(modelBuilder);
        ConfigureBid(modelBuilder);
        ConfigureUser(modelBuilder);
        ConfigureRefreshToken(modelBuilder);
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

    private void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(u => u.Id);

            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(u => u.PasswordHash)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(u => u.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(u => u.Role)
                .HasConversion<string>()
                .IsRequired();

            entity.HasIndex(u => u.Email)
                .IsUnique();
        });
    }

    private void ConfigureRefreshToken(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");

            entity.HasKey(rt => rt.Id);

            entity.Property(rt => rt.Token)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(rt => rt.ExpiresAt)
                .IsRequired();

            entity.Property(rt => rt.IsRevoked)
                .IsRequired();

            entity.HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(rt => rt.Token)
                .IsUnique();
            entity.HasIndex(rt => rt.UserId);
        });
    }
}
