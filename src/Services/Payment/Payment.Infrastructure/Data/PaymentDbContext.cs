using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;
using AuHub.Shared.Converters;

namespace Payment.Infrastructure.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
        : base(options) { }

    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.ToTable("Wallets");
            entity.HasKey(w => w.Id);

            entity.Property(w => w.Balance)
                .HasConversion<MoneyConverter>()
                .IsRequired();

            entity.Property(w => w.FrozenBalance)
                .HasConversion<MoneyConverter>()
                .IsRequired();

            entity.HasIndex(w => w.UserId)
                .IsUnique();
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("Transactions");
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Amount)
                .HasConversion<MoneyConverter>()
                .IsRequired();

            entity.Property(t => t.Description)
                .HasMaxLength(500);

            entity.Property(t => t.Type)
                .HasConversion<int>()
                .IsRequired();

            entity.HasIndex(t => t.UserId);
            entity.HasIndex(t => t.CreatedAt);
            entity.HasIndex(t => t.ReferenceId);
        });
    }
}
