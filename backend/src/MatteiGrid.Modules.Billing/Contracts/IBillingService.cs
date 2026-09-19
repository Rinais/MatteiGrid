namespace MatteiGrid.Modules.Billing.Contracts;

public record CreateInvoiceLineItemRequest(Guid? MedicineId, string Description, decimal Quantity, decimal UnitPrice);
public record CreateInvoiceRequest(Guid PatientId, Guid ConsultationId, Guid? PrescriptionId, IReadOnlyList<CreateInvoiceLineItemRequest> Items);
public record RecordManualPaymentRequest(decimal Amount, string Mode, Guid StaffId);
public record ProcessRefundRequest(decimal Amount, string Reason, Guid StaffId, string? RazorpayRefundId = null);

public record InvoiceLineItemDto(Guid Id, Guid? MedicineId, string Description, decimal Quantity, decimal UnitPrice, decimal Amount);
public record PaymentDto(Guid Id, decimal Amount, string Source, string Mode, string Status, string? RazorpayPaymentId, Guid? RecordedByStaffId, DateTimeOffset CreatedAt);
public record PaymentLinkDto(Guid Id, string TokenHash, string Status, DateTimeOffset ExpiresAt, DateTimeOffset? ConsumedAt);
public record InvoiceDto(
    Guid Id,
    Guid ClinicId,
    Guid PatientId,
    Guid ConsultationId,
    Guid? PrescriptionId,
    string Status,
    decimal TotalAmount,
    decimal TotalPaidAmount,
    decimal RemainingBalance,
    DateTimeOffset CreatedAt,
    IReadOnlyList<InvoiceLineItemDto> LineItems,
    IReadOnlyList<PaymentDto> Payments,
    PaymentLinkDto? ActivePaymentLink);

public interface IBillingService
{
    Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken ct = default);
    Task<InvoiceDto?> GetInvoiceByIdAsync(Guid invoiceId, CancellationToken ct = default);
    Task<InvoiceDto> RecordManualPaymentAsync(Guid invoiceId, RecordManualPaymentRequest request, CancellationToken ct = default);
    Task<InvoiceDto> ProcessRefundAsync(Guid invoiceId, Guid paymentId, ProcessRefundRequest request, CancellationToken ct = default);
    Task<InvoiceDto> CancelInvoiceAsync(Guid invoiceId, CancellationToken ct = default);
    Task<PaymentLinkDto> GeneratePaymentLinkAsync(Guid invoiceId, CancellationToken ct = default);
    Task<PaymentLinkDto> RegeneratePaymentLinkAsync(Guid invoiceId, CancellationToken ct = default);
    Task<bool> HandleRazorpayWebhookAsync(string razorpayPaymentId, decimal amount, Guid invoiceId, CancellationToken ct = default);
}