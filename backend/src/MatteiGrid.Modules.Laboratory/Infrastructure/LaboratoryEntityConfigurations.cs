using MatteiGrid.Modules.Laboratory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatteiGrid.Modules.Laboratory.Infrastructure;

public class LaboratoryOrderConfiguration : IEntityTypeConfiguration<LaboratoryOrder>
{
    public void Configure(EntityTypeBuilder<LaboratoryOrder> builder)
    {
        builder.ToTable("LaboratoryOrders");
        builder.HasKey(lo => lo.Id);
        builder.HasIndex(lo => new { lo.ClinicId, lo.Id }).IsUnique();

        builder.Property(lo => lo.Status).IsRequired().HasMaxLength(50);
        builder.Property(lo => lo.RejectionComment).HasMaxLength(1000);

        builder.HasIndex(lo => new { lo.ClinicId, lo.PrescriptionId });
        builder.HasIndex(lo => new { lo.ClinicId, lo.Status });
    }
}
