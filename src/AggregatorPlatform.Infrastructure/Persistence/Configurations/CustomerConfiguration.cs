using AggregatorPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AggregatorPlatform.Infrastructure.Persistence.Configurations;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> b)
    {
        b.ToTable("Clients");
        b.HasKey(x => x.ClientId);
        b.Property(x => x.BankAccountRoot)
            .IsRequired().HasMaxLength(500)
            .HasConversion(EncryptionValueConverter.ForString());
        b.HasIndex(x => x.BankAccountRoot)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Clients_BankAccountRoot_Unique");
        b.Property(x => x.FullName).IsRequired().HasMaxLength(300);
        b.Property(x => x.NationalId).HasMaxLength(500)
            .HasConversion(EncryptionValueConverter.ForNullableString());
        // PhoneNumber stocke en clair (identifiant metier). NationalId reste chiffre.
        b.Property(x => x.PhoneNumber).HasMaxLength(500);
        b.Property(x => x.Email).HasMaxLength(200);
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

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

        // Lien vers Client racine (nullable pour retro-compat).
        b.HasIndex(x => x.ClientId).HasDatabaseName("IX_Customers_ClientId");
        b.HasOne(x => x.Client).WithMany(c => c.Customers).HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);

        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> b)
    {
        b.ToTable("Subscriptions");
        b.HasKey(x => x.SubscriptionId);
        // BankAccount et PhoneNumber sont stockes en clair : leur valeur sert
        // d'identifiant metier (lookup par phone+bank, unicite du triplet). MaxLength(500)
        // conserve pour eviter une alteration de colonne et couvrir les valeurs
        // dechiffrees issues de l'historique via DecryptLegacySubscriptionColumnsHostedService.
        b.Property(x => x.BankAccount)
            .HasColumnName("BankAccountNumber")
            .IsRequired().HasMaxLength(500);
        b.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(500);
        b.Property(x => x.PhoneOperator).IsRequired().HasMaxLength(50);
        b.Property(x => x.Status).HasConversion<int>();

        // Regle metier : une souscription est unique par le triplet exact
        // (PartnerId, BankAccount, PhoneNumber). Le meme bank account peut etre
        // reutilise avec un autre phone et inversement, y compris chez le meme partenaire.
        // L'index est filtre sur IsDeleted = 0 pour permettre la reutilisation apres soft-delete.
        b.HasIndex(x => new { x.PartnerId, x.BankAccount, x.PhoneNumber })
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
