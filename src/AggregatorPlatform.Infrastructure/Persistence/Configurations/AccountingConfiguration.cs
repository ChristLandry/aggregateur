using AggregatorPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AggregatorPlatform.Infrastructure.Persistence.Configurations;

public class AccountingSchemaConfiguration : IEntityTypeConfiguration<AccountingSchema>
{
    public void Configure(EntityTypeBuilder<AccountingSchema> b)
    {
        b.ToTable("AccountingSchemas");
        b.HasKey(x => x.SchemaId);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.TransactionType).HasConversion<int>();
        b.Property(x => x.TransactionSide).HasConversion<int>();
        b.Property(x => x.Channel).HasConversion<int>();

        b.HasOne(x => x.Partner).WithMany(p => p.AccountingSchemas).HasForeignKey(x => x.PartnerId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Lines).WithOne(l => l.Schema!).HasForeignKey(l => l.SchemaId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.TransactionType, x.TransactionSide, x.Channel, x.PartnerId, x.IsActive, x.Priority });
    }
}

public class AccountingSchemaLineConfiguration : IEntityTypeConfiguration<AccountingSchemaLine>
{
    public void Configure(EntityTypeBuilder<AccountingSchemaLine> b)
    {
        b.ToTable("AccountingSchemaLines");
        b.HasKey(x => x.LineId);
        b.Property(x => x.AccountCode).IsRequired().HasMaxLength(50);
        b.Property(x => x.AccountExpression).HasMaxLength(500);
        b.Property(x => x.AmountFormula).IsRequired().HasMaxLength(500);
        b.Property(x => x.Label).IsRequired().HasMaxLength(200);
        b.Property(x => x.Condition).HasMaxLength(500);
        b.Property(x => x.AccountType).HasConversion<int>();
        b.Property(x => x.Side).HasConversion<int>();
        b.Property(x => x.Code).HasMaxLength(50);
        b.Property(x => x.Exploitant).HasMaxLength(50);
    }
}

public class PartnerEndpointConfiguration : IEntityTypeConfiguration<PartnerEndpoint>
{
    public void Configure(EntityTypeBuilder<PartnerEndpoint> b)
    {
        b.ToTable("PartnerEndpoints");
        b.HasKey(x => x.PartnerEndpointId);
        b.Property(x => x.EndpointKey).HasConversion<int>();

        // Unicite : un partenaire ne peut avoir qu'un seul lien par endpoint
        // (filtre IsDeleted=0 pour permettre la reutilisation apres soft-delete).
        b.HasIndex(x => new { x.PartnerId, x.EndpointKey })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_PartnerEndpoints_Partner_Key_Unique");

        b.HasOne(x => x.Partner)
            .WithMany()
            .HasForeignKey(x => x.PartnerId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Schema)
            .WithMany()
            .HasForeignKey(x => x.SchemaId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class MovementConfiguration : IEntityTypeConfiguration<Movement>
{
    public void Configure(EntityTypeBuilder<Movement> b)
    {
        b.ToTable("Movements");
        b.HasKey(x => x.MovementId);
        b.Property(x => x.Account).IsRequired().HasMaxLength(100);
        b.Property(x => x.Amount).HasPrecision(18, 4);
        b.Property(x => x.Side).HasConversion<int>();
        b.Property(x => x.Label).IsRequired().HasMaxLength(200);
        b.Property(x => x.Code).HasMaxLength(50);
        b.Property(x => x.Exploitant).HasMaxLength(50);
        b.Property(x => x.Reference).HasMaxLength(200);

        b.HasOne(x => x.Transaction).WithMany(t => t.Movements).HasForeignKey(x => x.TransactionId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Schema).WithMany().HasForeignKey(x => x.SchemaId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.TransactionId);
        b.HasIndex(x => new { x.Account, x.TransactionDate });
        b.HasIndex(x => x.TransactionDate);
    }
}
