using MatteiGrid.Shared.Kernel;

namespace MatteiGrid.Modules.Laboratory.Domain;

public static class LaboratoryOrderStatus
{
    public const string Received = "Received";
    public const string Accepted = "Accepted";
    public const string Preparing = "Preparing";
    public const string QualityCheck = "QualityCheck";
    public const string Prepared = "Prepared";
    public const string Ready = "Ready";
    public const string Collected = "Collected";
    public const string Rejected = "Rejected";

    public static readonly IReadOnlyList<string> SequentialOrder = new[]
    {
        Received, Accepted, Preparing, QualityCheck, Prepared, Ready, Collected
    };
}

public class LaboratoryOrder : ClinicScopedEntity
{
    public Guid PrescriptionId { get; set; }
    public Guid? PreparedByStaffId { get; set; }
    public string Status { get; set; } = LaboratoryOrderStatus.Received;
    public string? RejectionComment { get; set; }

    public void Reject(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection comment is mandatory per Section 5.7.", nameof(reason));

        if (Status == LaboratoryOrderStatus.Ready || Status == LaboratoryOrderStatus.Collected)
            throw new InvalidOperationException("Orders already marked Ready or Collected cannot be rejected.");

        Status = LaboratoryOrderStatus.Rejected;
        RejectionComment = reason;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
