using MatteiGrid.Shared.Kernel;

namespace MatteiGrid.Modules.Billing.Domain;

public static class InvoiceStatus
{
    public const string Pending = "Pending";
    public const string PartiallyPaid = "PartiallyPaid";
    public const string Paid = "Paid";
    public const string Cancelled = "Cancelled";
    public const string Refunded = "Refunded";
}

public static class PaymentSource
{
    public const string Digital = "Digital";
    public const string Manual = "Manual";
}

public static class PaymentMode
{
    public const string UPI = "UPI";
    public const string Cash = "Cash";
    public const string Card = "Card";
}

public static class PaymentStatus
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Failed = "Failed";
}

public static class PaymentLinkStatus
{
    public const string Active = "Active";
    public const string Expired = "Expired";
    public const string Consumed = "Consumed";
}

public static class RefundStatus
{
    public const string Initiated = "Initiated";
    public const string Processed = "Processed";
    public const string Failed = "Failed";
}

public class Invoice : ClinicScopedEntity
{
    public Guid PatientId { get; set; }
    public Guid ConsultationId { get; set; } // Mandatory anchor per ERD v4
    public Guid? PrescriptionId { get; set; } // Nullable — consultation-only billing supported
    public string Status { get; set; } = InvoiceStatus.Pending;
    public decimal TotalAmount { get; set; }

    public ICollection<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<PaymentLink> PaymentLinks { get; set; } = new List<PaymentLink>();

    public decimal TotalPaidAmount => Payments
        .Where(p => p.Status == PaymentStatus.Confirmed)
        .Sum(p => p.Amount);

    public decimal RemainingBalance => Math.Max(0, TotalAmount - TotalPaidAmount);

    public void Cancel()
    {
        if (Payments.Any(p => p.Status == PaymentStatus.Confirmed))
            throw new InvalidOperationException("Cannot cancel an invoice once any payment has been recorded. Issue a refund instead.");

        Status = InvoiceStatus.Cancelled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkRefunded()
    {
        if (Status != InvoiceStatus.Paid && Status != InvoiceStatus.PartiallyPaid)
            throw new InvalidOperationException("Only Paid or PartiallyPaid invoices can be transitioned to Refunded.");

        Status = InvoiceStatus.Refunded;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RecordPayment(Payment payment)
    {
        if (Status == InvoiceStatus.Cancelled || Status == InvoiceStatus.Refunded)
            throw new InvalidOperationException($"Cannot record payment against a {Status} invoice.");

        if (payment.Amount > RemainingBalance)
            throw new InvalidOperationException($"Payment amount {payment.Amount} exceeds remaining balance {RemainingBalance}. Overpayment rejected.");

        payment.InvoiceId = Id;
        payment.ClinicId = ClinicId;
        Payments.Add(payment);

        var newTotalPaid = TotalPaidAmount + (payment.Status == PaymentStatus.Confirmed ? payment.Amount : 0);
        if (newTotalPaid >= TotalAmount)
        {
            Status = InvoiceStatus.Paid;
        }
        else if (newTotalPaid > 0)
        {
            Status = InvoiceStatus.PartiallyPaid;
        }

        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public class InvoiceLineItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InvoiceId { get; set; }
    public Guid? MedicineId { get; set; } // Traceability only, never display source
    public string Description { get; set; } = string.Empty; // Frozen snapshot at invoice time
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow; // Immutable snapshot — no UpdatedAt

    public Invoice? Invoice { get; set; }
}

public class Payment : IClinicScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClinicId { get; set; }
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string Source { get; set; } = PaymentSource.Manual; // Digital or Manual
    public string Mode { get; set; } = PaymentMode.Cash;       // UPI, Cash, Card
    public string Status { get; set; } = PaymentStatus.Confirmed;
    public string? RazorpayPaymentId { get; set; }              // Nullable — partial unique index in DB
    public Guid? RecordedByStaffId { get; set; }                // Nullable, manual only
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public Invoice? Invoice { get; set; }
    public ICollection<Refund> Refunds { get; set; } = new List<Refund>();
}

public class Refund : IClinicScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClinicId { get; set; }
    public Guid PaymentId { get; set; }
    public Guid StaffId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = RefundStatus.Initiated;
    public string? RazorpayRefundId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public Payment? Payment { get; set; }
}

public class PaymentLink : IClinicScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClinicId { get; set; }
    public Guid InvoiceId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string Status { get; set; } = PaymentLinkStatus.Active;
    public DateTimeOffset ExpiresAt { get; set; } = DateTimeOffset.UtcNow.AddHours(48); // 48-hour TTL
    public DateTimeOffset? ConsumedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Invoice? Invoice { get; set; }

    public bool IsExpired => DateTimeOffset.UtcNow > ExpiresAt && Status == PaymentLinkStatus.Active;
}
