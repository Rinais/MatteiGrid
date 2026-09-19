using MatteiGrid.Modules.MateriaMedica.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatteiGrid.Modules.MateriaMedica.Infrastructure;

public class MateriaMedicaEntryConfiguration : IEntityTypeConfiguration<MateriaMedicaEntry>
{
    public void Configure(EntityTypeBuilder<MateriaMedicaEntry> builder)
    {
        builder.ToTable("MateriaMedicaEntries");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(m => m.Code).IsUnique();

        builder.Property(m => m.Name).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Family).HasMaxLength(100);
        builder.Property(m => m.Description).IsRequired();
        builder.Property(m => m.Indications).IsRequired();
    }
}

public class PractitionerNoteConfiguration : IEntityTypeConfiguration<PractitionerNote>
{
    public void Configure(EntityTypeBuilder<PractitionerNote> builder)
    {
        builder.ToTable("PractitionerNotes");
        builder.HasKey(n => n.Id);
        builder.HasIndex(n => new { n.ClinicId, n.Id }).IsUnique();

        builder.Property(n => n.Content).IsRequired();

        builder.HasOne(n => n.MateriaMedicaEntry)
            .WithMany(m => m.PractitionerNotes)
            .HasForeignKey(n => n.MateriaMedicaEntryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
