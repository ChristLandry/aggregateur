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
    }
}

public class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> b)
    {
        b.ToTable("JournalEntries");
        b.HasKey(x => x.EntryId);
        b.Property(x => x.TotalDebit).HasPrecision(18, 4);
        b.Property(x => x.TotalCredit).HasPrecision(18, 4);
        b.HasMany(x => x.Lines).WithOne(l => l.Entry!).HasForeignKey(l => l.EntryId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Transaction).WithMany(t => t.JournalEntries).HasForeignKey(x => x.TransactionId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.EntryDate);
    }
}

public class JournalLineConfiguration : IEntityTypeConfiguration<JournalLine>
{
    public void Configure(EntityTypeBuilder<JournalLine> b)
    {
        b.ToTable("JournalLines");
        b.HasKey(x => x.LineId);
        b.Property(x => x.AccountCode).IsRequired().HasMaxLength(50);
        b.Property(x => x.Label).IsRequired().HasMaxLength(200);
        b.Property(x => x.Amount).HasPrecision(18, 4);
        b.Property(x => x.Side).HasConversion<int>();
        b.HasIndex(x => x.AccountCode);
    }
}
