using Microsoft.EntityFrameworkCore;
using Identity.Domain.Entities;

namespace Identity.Infrastructure.Data;

public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUser(modelBuilder);
        ConfigureRefreshToken(modelBuilder);
        ConfigureEmailVerificationToken(modelBuilder);
        ConfigureAdminAuditLog(modelBuilder);
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

            entity.Property(u => u.PhoneNumber)
                .IsRequired()
                .HasMaxLength(32);

            entity.Property(u => u.Nickname)
                .IsRequired()
                .HasMaxLength(32);

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

            entity.HasIndex(u => u.PhoneNumber)
                .IsUnique();

            entity.HasIndex(u => u.Nickname)
                .IsUnique();

            entity.Property(u => u.IsEmailVerified)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(u => u.EmailVerifiedAt);

            entity.Property(u => u.IsPhoneVerified)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(u => u.PhoneVerifiedAt);

            entity.Property(u => u.IsBanned)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(u => u.BannedAt);

            entity.Property(u => u.BanReason)
                .HasMaxLength(500);
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

            entity.Property(rt => rt.FamilyId);
            entity.Property(rt => rt.ReplacedByTokenId);
            entity.Property(rt => rt.RevokedAt);

            entity.HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(rt => rt.Token)
                .IsUnique();
            entity.HasIndex(rt => rt.UserId);
            entity.HasIndex(rt => rt.FamilyId);
        });
    }

    private void ConfigureAdminAuditLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminAuditLog>(entity =>
        {
            entity.ToTable("AdminAuditLogs");

            entity.HasKey(log => log.Id);

            entity.Property(log => log.Action)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(log => log.TargetType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(log => log.Details)
                .HasMaxLength(1000);

            entity.Property(log => log.CreatedAt)
                .IsRequired();

            entity.HasIndex(log => log.ActorUserId);
            entity.HasIndex(log => new { log.TargetType, log.TargetId });
            entity.HasIndex(log => log.CreatedAt);
        });
    }

    private void ConfigureEmailVerificationToken(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmailVerificationToken>(entity =>
        {
            entity.ToTable("EmailVerificationTokens");

            entity.HasKey(token => token.Id);

            entity.Property(token => token.TokenHash)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(token => token.ExpiresAt)
                .IsRequired();

            entity.Property(token => token.CreatedAt)
                .IsRequired();

            entity.Property(token => token.UsedAt);

            entity.HasOne(token => token.User)
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(token => token.TokenHash)
                .IsUnique();
            entity.HasIndex(token => token.UserId);
        });
    }
}
