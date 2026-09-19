using MatteiGrid.Shared.Kernel;

namespace MatteiGrid.Modules.Notifications.Domain;

public static class NotificationChannel
{
    public const string SMS = "SMS";
    public const string WhatsApp = "WhatsApp";
    public const string Email = "Email";
}

public static class NotificationStatus
{
    public const string Pending = "Pending";
    public const string Sent = "Sent";
    public const string Failed = "Failed";
}

public class NotificationLog : IClinicScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClinicId { get; set; }
    public Guid PatientId { get; set; }
    public Guid? TriggeredByStaffId { get; set; } // Nullable — system-triggered
    public string Channel { get; set; } = NotificationChannel.SMS;
    public string Status { get; set; } = NotificationStatus.Pending;
    public string? MessageContent { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SentAt { get; set; }
}
