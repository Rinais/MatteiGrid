namespace MatteiGrid.Shared.Kernel;

public interface IPaymentLockChecker
{
    Task<bool> HasConfirmedPaymentForPrescriptionAsync(Guid prescriptionId, CancellationToken ct = default);
}
