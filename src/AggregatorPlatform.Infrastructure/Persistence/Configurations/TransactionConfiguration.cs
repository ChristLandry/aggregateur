using AggregatorPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AggregatorPlatform.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> b)
    {
        b.ToTable("Transactions");
        b.HasKey(x => x.TransactionId);
        b.Property(x => x.PartnerTransactionRef).IsRequired().HasMaxLength(100);
        b.HasIndex(x => new { x.PartnerId, x.PartnerTransactionRef }).IsUnique();
        b.Property(x => x.Amount).HasPrecision(18, 4);
        b.Property(x => x.FeeAmount).HasPrecision(18, 4);
        b.Property(x => x.NetAmount).HasPrecision(18, 4);
        b.Property(x => x.Currency).IsRequired().HasMaxLength(3);
        b.Property(x => x.TransactionType).HasConversion<int>();
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.AccountingStatus).HasConversion<int>();
        b.Property(x => x.FailureReason).HasMaxLength(500);
        b.Property(x => x.ExternalRef).HasMaxLength(200);

        // Champs ajoutes au payload : chiffres au repos
        b.Property(x => x.BankAccount)
            .HasMaxLength(500)
            .HasConversion(EncryptionValueConverter.ForNullableString());
        b.Property(x => x.PhoneNumber)
            .HasMaxLength(500)
            .HasConversion(EncryptionValueConverter.ForNullableString());
        b.Property(x => x.ExtraData)
            .HasColumnType("nvarchar(max)");

        b.HasOne(x => x.Partner).WithMany(p => p.Transactions).HasForeignKey(x => x.PartnerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Subscription).WithMany(s => s.Transactions).HasForeignKey(x => x.SubscriptionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Schema).WithMany().HasForeignKey(x => x.SchemaId).OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.InitiatedAt);
    }
}
