using System.Security.Cryptography;
using MatteiGrid.Modules.Billing.Contracts;
using MatteiGrid.Modules.Billing.Domain;
using MatteiGrid.Shared.Infrastructure.Persistence;
using MatteiGrid.Shared.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace MatteiGrid.Modules.Billing.Services;

public class BillingService : IBillingService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentTenantContext _tenantContext;

    public BillingService(AppDbContext dbContext, ICurrentTenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken ct = default)
    {
        var invoice = new Invoice
        {
            ClinicId = _tenantContext.ClinicId ?? Guid.Empty,
            PatientId = request.PatientId,
            ConsultationId = request.ConsultationId,
            PrescriptionId = request.PrescriptionId,
            Status = InvoiceStatus.Pending,
            TotalAmount = request.Items.Sum(i => i.Quantity * i.UnitPrice)
        };

        foreach (var item in request.Items)
        {
            invoice.LineItems.Add(new InvoiceLineItem
            {
                MedicineId = item.MedicineId,
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Amount = item.Quantity * item.UnitPrice,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        invoice.PaymentLinks.Add(new PaymentLink
        {
            ClinicId = _tenantContext.ClinicId ?? Guid.Empty,
            TokenHash = token,
            Status = PaymentLinkStatus.Active,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(48)
        });

        _dbContext.Set<Invoice>().Add(invoice);
        await _dbContext.SaveChangesAsync(ct);

        return (await GetInvoiceByIdAsync(invoice.Id, ct))!;
    }

    public async Task<InvoiceDto?> GetInvoiceByIdAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var i = await _dbContext.Set<Invoice>()
            .Include(x => x.LineItems)
            .Include(x => x.Payments)
            .Include(x => x.PaymentLinks)
            .FirstOrDefaultAsync(x => x.Id == invoiceId, ct);

        return i is null ? null : MapDto(i);
    }

    public async Task<InvoiceDto> RecordManualPaymentAsync(Guid invoiceId, RecordManualPaymentRequest request, CancellationToken ct = default)
    {
        var invoice = await _dbContext.Set<Invoice>()
            .Include(x => x.Payments)
            .Include(x => x.PaymentLinks)
            .FirstOrDefaultAsync(x => x.Id == invoiceId, ct)
            ?? throw new InvalidOperationException("Invoice not found.");

        var payment = new Payment
        {
            Amount = request.Amount,
            Source = PaymentSource.Manual,
            Mode = request.Mode,
            Status = PaymentStatus.Confirmed,
            RecordedByStaffId = request.StaffId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Set<Payment>().Add(payment);
        invoice.RecordPayment(payment);

        await _dbContext.SaveChangesAsync(ct);
        return (await GetInvoiceByIdAsync(invoiceId, ct))!;
    }

    public async Task<InvoiceDto> ProcessRefundAsync(Guid invoiceId, Guid paymentId, ProcessRefundRequest request, CancellationToken ct = default)
    {
        var invoice = await _dbContext.Set<Invoice>()
            .Include(x => x.Payments)
                .ThenInclude(p => p.Refunds)
            .FirstOrDefaultAsync(x => x.Id == invoiceId, ct)
            ?? throw new InvalidOperationException("Invoice not found.");

        var payment = invoice.Payments.FirstOrDefault(p => p.Id == paymentId)
            ?? throw new InvalidOperationException("Payment record not found.");

        if (payment.Status != PaymentStatus.Confirmed)
            throw new InvalidOperationException("Only confirmed payments can be refunded.");

        var totalRefundedOnPayment = payment.Refunds.Where(r => r.Status == RefundStatus.Processed).Sum(r => r.Amount);
        if (request.Amount > (payment.Amount - totalRefundedOnPayment))
            throw new InvalidOperationException("Refund amount exceeds remaining refundable payment balance.");

        var refund = new Refund
        {
            ClinicId = _tenantContext.ClinicId ?? Guid.Empty,
            PaymentId = payment.Id,
            StaffId = request.StaffId,
            Amount = request.Amount,
            Reason = request.Reason,
            Status = RefundStatus.Processed,
            RazorpayRefundId = request.RazorpayRefundId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Set<Refund>().Add(refund);
        payment.Refunds.Add(refund);

        invoice.MarkRefunded();

        await _dbContext.SaveChangesAsync(ct);
        return (await GetInvoiceByIdAsync(invoiceId, ct))!;
    }

    public async Task<InvoiceDto> CancelInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var invoice = await _dbContext.Set<Invoice>()
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == invoiceId, ct)
            ?? throw new InvalidOperationException("Invoice not found.");

        invoice.Cancel();

        await _dbContext.SaveChangesAsync(ct);
        return (await GetInvoiceByIdAsync(invoiceId, ct))!;
    }

    public async Task<PaymentLinkDto> GeneratePaymentLinkAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var link = new PaymentLink
        {
            ClinicId = _tenantContext.ClinicId ?? Guid.Empty,
            InvoiceId = invoiceId,
            TokenHash = token,
            Status = PaymentLinkStatus.Active,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(48)
        };

        _dbContext.Set<PaymentLink>().Add(link);
        await _dbContext.SaveChangesAsync(ct);

        return new PaymentLinkDto(link.Id, link.TokenHash, link.Status, link.ExpiresAt, link.ConsumedAt);
    }

    public async Task<PaymentLinkDto> RegeneratePaymentLinkAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var activeLinks = await _dbContext.Set<PaymentLink>()
            .Where(pl => pl.InvoiceId == invoiceId && pl.Status == PaymentLinkStatus.Active)
            .ToListAsync(ct);

        foreach (var l in activeLinks)
        {
            l.Status = PaymentLinkStatus.Expired;
        }

        return await GeneratePaymentLinkAsync(invoiceId, ct);
    }

    public async Task<bool> HandleRazorpayWebhookAsync(string razorpayPaymentId, decimal amount, Guid invoiceId, CancellationToken ct = default)
    {
        var existing = await _dbContext.Set<Payment>()
            .AnyAsync(p => p.RazorpayPaymentId == razorpayPaymentId, ct);

        if (existing) return true;

        var invoice = await _dbContext.Set<Invoice>()
            .Include(x => x.Payments)
            .Include(x => x.PaymentLinks)
            .FirstOrDefaultAsync(x => x.Id == invoiceId, ct);

        if (invoice is null) return false;

        var payment = new Payment
        {
            Amount = amount,
            Source = PaymentSource.Digital,
            Mode = PaymentMode.UPI,
            Status = PaymentStatus.Confirmed,
            RazorpayPaymentId = razorpayPaymentId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Set<Payment>().Add(payment);
        invoice.RecordPayment(payment);

        var activeLink = invoice.PaymentLinks.FirstOrDefault(l => l.Status == PaymentLinkStatus.Active);
        if (activeLink is not null)
        {
            activeLink.Status = PaymentLinkStatus.Consumed;
            activeLink.ConsumedAt = DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    private static InvoiceDto MapDto(Invoice i)
    {
        var activeLink = i.PaymentLinks
            .OrderByDescending(l => l.CreatedAt)
            .FirstOrDefault(l => l.Status == PaymentLinkStatus.Active);

        return new InvoiceDto(
            i.Id,
            i.ClinicId,
            i.PatientId,
            i.ConsultationId,
            i.PrescriptionId,
            i.Status,
            i.TotalAmount,
            i.TotalPaidAmount,
            i.RemainingBalance,
            i.CreatedAt,
            i.LineItems.Select(li => new InvoiceLineItemDto(li.Id, li.MedicineId, li.Description, li.Quantity, li.UnitPrice, li.Amount)).ToList(),
            i.Payments.Select(p => new PaymentDto(p.Id, p.Amount, p.Source, p.Mode, p.Status, p.RazorpayPaymentId, p.RecordedByStaffId, p.CreatedAt)).ToList(),
            activeLink is null ? null : new PaymentLinkDto(activeLink.Id, activeLink.TokenHash, activeLink.Status, activeLink.ExpiresAt, activeLink.ConsumedAt)
        );
    }
}