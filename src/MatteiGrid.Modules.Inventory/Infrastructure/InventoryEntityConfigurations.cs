using MatteiGrid.Modules.Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatteiGrid.Modules.Inventory.Infrastructure;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("InventoryItems");
        builder.HasKey(ii => ii.Id);
        builder.HasIndex(ii => new { ii.ClinicId, ii.Id }).IsUnique();

        builder.Property(ii => ii.QuantityOnHand).HasPrecision(18, 2);
        builder.Property(ii => ii.ReorderLevel).HasPrecision(18, 2);

        builder.HasIndex(ii => new { ii.ClinicId, ii.MedicineId }).IsUnique();
    }
}

public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.ToTable("InventoryTransactions", t =>
        {
            // ERD v4 Sec 1 & 4 CHECK constraint:
            // CHECK (TransactionType = 'LabDeduction' ⟺ LaboratoryOrderId IS NOT NULL)
            t.HasCheckConstraint("CK_InventoryTransaction_LabOrderId",
                "(\"TransactionType\" = 'LabDeduction' AND \"LaboratoryOrderId\" IS NOT NULL) OR (\"TransactionType\" <> 'LabDeduction' AND \"LaboratoryOrderId\" IS NULL)");
        });

        builder.HasKey(it => it.Id);
        builder.Property(it => it.TransactionType).IsRequired().HasMaxLength(50);
        builder.Property(it => it.QuantityDelta).HasPrecision(18, 2);
        builder.Property(it => it.Reason).HasMaxLength(500);

        builder.HasOne(it => it.InventoryItem)
            .WithMany(ii => ii.Transactions)
            .HasForeignKey(it => it.InventoryItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(it => new { it.ClinicId, it.InventoryItemId });
        builder.HasIndex(it => new { it.ClinicId, it.LaboratoryOrderId });
    }
}
