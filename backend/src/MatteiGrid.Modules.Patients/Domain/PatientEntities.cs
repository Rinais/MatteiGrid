using MatteiGrid.Shared.Kernel;

namespace MatteiGrid.Modules.Patients.Domain;

public class Patient : ClinicScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateOnly DOB { get; set; }
    public bool IsArchived { get; set; }
    public bool IsAnonymized { get; set; }

    public ICollection<ConsentRecord> ConsentRecords { get; set; } = new List<ConsentRecord>();

    /// <summary>
    /// DPDP Act 2023 compliance: On erasure request, PII is anonymized in-place.
    /// Underlying clinical/financial references remain intact for statutory retention.
    /// </summary>
    public void AnonymizePii()
    {
        Name = $"Anonymized-Patient-{Id.ToString("N")[..8]}";
        Phone = "0000000000";
        DOB = new DateOnly(1900, 1, 1);
        IsAnonymized = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public class ConsentRecord : IClinicScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClinicId { get; set; }
    public Guid PatientId { get; set; }
    public string ConsentType { get; set; } = string.Empty; // e.g., "DataProcessing", "Reminders"
    public string PolicyVersion { get; set; } = "1.0";
    public DateTimeOffset GrantedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAt { get; set; }

    public Patient? Patient { get; set; }
}
