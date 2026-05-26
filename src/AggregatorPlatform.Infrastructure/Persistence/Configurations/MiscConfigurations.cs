using AggregatorPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AggregatorPlatform.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("AuditLogs");
        b.HasKey(x => x.LogId);
        b.Property(x => x.EntityType).IsRequired().HasMaxLength(100);
        b.Property(x => x.EntityId).IsRequired().HasMaxLength(100);
        b.Property(x => x.Action).IsRequired().HasMaxLength(50);
        b.Property(x => x.PerformedBy).IsRequired().HasMaxLength(100);
        b.Property(x => x.IpAddress).HasMaxLength(50);
        b.Property(x => x.UserAgent).HasMaxLength(500);
        b.HasIndex(x => new { x.EntityType, x.EntityId });
        b.HasIndex(x => x.PerformedAt);
    }
}

public class SystemParameterConfiguration : IEntityTypeConfiguration<SystemParameter>
{
    public void Configure(EntityTypeBuilder<SystemParameter> b)
    {
        b.ToTable("SystemParameters");
        b.HasKey(x => x.Key);
        b.Property(x => x.Key).HasMaxLength(100);
        b.Property(x => x.Value).IsRequired().HasMaxLength(1000);
        b.Property(x => x.Description).HasMaxLength(500);
    }
}

public class WebhookLogConfiguration : IEntityTypeConfiguration<WebhookLog>
{
    public void Configure(EntityTypeBuilder<WebhookLog> b)
    {
        b.ToTable("WebhookLogs");
        b.HasKey(x => x.LogId);
        b.Property(x => x.EventType).IsRequired().HasMaxLength(100);
        b.Property(x => x.TargetUrl).IsRequired().HasMaxLength(500);
        b.Property(x => x.Payload).IsRequired();
        b.Property(x => x.Status).HasConversion<int>();
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.NextAttemptAt);
    }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users");
        b.HasKey(x => x.UserId);
        b.Property(x => x.Username).IsRequired().HasMaxLength(100);
        b.HasIndex(x => x.Username).IsUnique();
        b.Property(x => x.Email).IsRequired().HasMaxLength(200);
        b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.PasswordHash).IsRequired().HasMaxLength(500);
        b.Property(x => x.Role).HasConversion<int>();
        b.Property(x => x.TwoFactorSecret).HasMaxLength(200);
        b.HasOne(x => x.Partner).WithMany().HasForeignKey(x => x.PartnerId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("RefreshTokens");
        b.HasKey(x => x.TokenId);
        b.Property(x => x.Token).IsRequired().HasMaxLength(500);
        b.HasIndex(x => x.Token);
        b.Property(x => x.ReplacedByToken).HasMaxLength(500);
        b.HasOne(x => x.User).WithMany(u => u.RefreshTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
