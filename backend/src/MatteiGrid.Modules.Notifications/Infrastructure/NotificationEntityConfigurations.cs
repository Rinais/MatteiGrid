using MatteiGrid.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatteiGrid.Modules.Notifications.Infrastructure;

public class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("NotificationLogs");
        builder.HasKey(nl => nl.Id);

        builder.Property(nl => nl.Channel).IsRequired().HasMaxLength(50);
        builder.Property(nl => nl.Status).IsRequired().HasMaxLength(50);
        builder.Property(nl => nl.MessageContent).HasMaxLength(1000);

        builder.HasIndex(nl => new { nl.ClinicId, nl.PatientId });
        builder.HasIndex(nl => new { nl.ClinicId, nl.Status });
    }
}
