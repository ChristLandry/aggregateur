using AggregatorPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AggregatorPlatform.Infrastructure.Persistence.Configurations;

public class PartnerConfiguration : IEntityTypeConfiguration<Partner>
{
    public void Configure(EntityTypeBuilder<Partner> b)
    {
        b.ToTable("Partners");
        b.HasKey(x => x.PartnerId);
        b.Property(x => x.PartnerCode).IsRequired().HasMaxLength(50);
        b.HasIndex(x => x.PartnerCode).IsUnique();
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.BaseUrl).IsRequired().HasMaxLength(500);
        b.Property(x => x.ApiKey).IsRequired().HasMaxLength(500);
        b.Property(x => x.AccountCode).HasMaxLength(50);
        b.Property(x => x.Currency).IsRequired().HasMaxLength(3);
        b.Property(x => x.WebhookUrl).HasMaxLength(500);
        b.Property(x => x.IpWhitelist).HasMaxLength(1000);
        b.Property(x => x.Status).HasConversion<int>();
        b.HasQueryFilter(x => !x.IsDeleted);

        b.HasOne(x => x.PartnerAccount)
            .WithOne(x => x.Partner!)
            .HasForeignKey<PartnerAccount>(x => x.PartnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PartnerAccountConfiguration : IEntityTypeConfiguration<PartnerAccount>
{
    public void Configure(EntityTypeBuilder<PartnerAccount> b)
    {
        b.ToTable("PartnerAccounts");
        b.HasKey(x => x.AccountId);
        b.HasIndex(x => x.PartnerId).IsUnique();
        b.Property(x => x.Balance).HasPrecision(18, 4);
        b.Property(x => x.Currency).IsRequired().HasMaxLength(3);

        // Numero de compte bancaire du partenaire — chiffre AES-256 au repos (donnee sensible).
        b.Property(x => x.PartnerBankAccount)
            .IsRequired()
            .HasMaxLength(500)
            .HasConversion(EncryptionValueConverter.ForString());
    }
}

public class PartnerAccountMovementConfiguration : IEntityTypeConfiguration<PartnerAccountMovement>
{
    public void Configure(EntityTypeBuilder<PartnerAccountMovement> b)
    {
        b.ToTable("PartnerAccountMovements");
        b.HasKey(x => x.MovementId);
        b.Property(x => x.Amount).HasPrecision(18, 4);
        b.Property(x => x.BalanceBefore).HasPrecision(18, 4);
        b.Property(x => x.BalanceAfter).HasPrecision(18, 4);
        b.Property(x => x.MovementType).HasConversion<int>();
        b.Property(x => x.Description).HasMaxLength(500);
        b.HasIndex(x => new { x.PartnerId, x.MovementDate });

        b.HasOne(x => x.Partner).WithMany().HasForeignKey(x => x.PartnerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Transaction).WithMany().HasForeignKey(x => x.TransactionId).OnDelete(DeleteBehavior.SetNull);
    }
}
