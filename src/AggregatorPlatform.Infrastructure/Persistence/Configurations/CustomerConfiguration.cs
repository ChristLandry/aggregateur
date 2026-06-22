using AggregatorPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AggregatorPlatform.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.ToTable("Customers");
        b.HasKey(x => x.CustomerId);
        b.Property(x => x.ExternalCustomerId).HasMaxLength(100);
        b.HasIndex(x => x.ExternalCustomerId);
        b.Property(x => x.FullName).IsRequired().HasMaxLength(300);
        b.Property(x => x.NationalId)
            .HasMaxLength(500)
            .HasConversion(EncryptionValueConverter.ForNullableString());
        b.Property(x => x.Email).HasMaxLength(200);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.KycStatus).HasConversion<int>();
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> b)
    {
        b.ToTable("Subscriptions");
        b.HasKey(x => x.SubscriptionId);
        b.Property(x => x.BankAccountNumber)
            .IsRequired().HasMaxLength(500)
            .HasConversion(EncryptionValueConverter.ForString());
        b.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(500)
            .HasConversion(EncryptionValueConverter.ForString());
        b.Property(x => x.PhoneOperator).IsRequired().HasMaxLength(50);
        b.Property(x => x.Status).HasConversion<int>();

        // Regle metier : une souscription est unique par le triplet exact
        // (PartnerId, BankAccountNumber, PhoneNumber). Le meme bank account peut etre
        // reutilise avec un autre phone et inversement, y compris chez le meme partenaire.
        // L'index est filtre sur IsDeleted = 0 pour permettre la reutilisation apres soft-delete.
        b.HasIndex(x => new { x.PartnerId, x.BankAccountNumber, x.PhoneNumber })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Subscriptions_Partner_Bank_Phone_Unique");

        // Index non-unique pour les listings par client
        b.HasIndex(x => x.CustomerId).HasDatabaseName("IX_Subscriptions_CustomerId");

        b.HasOne(x => x.Customer).WithMany(c => c.Subscriptions).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Partner).WithMany(p => p.Subscriptions).HasForeignKey(x => x.PartnerId).OnDelete(DeleteBehavior.Restrict);
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
