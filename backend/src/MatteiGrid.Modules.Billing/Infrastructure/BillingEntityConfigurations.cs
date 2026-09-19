using MatteiGrid.Modules.Billing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatteiGrid.Modules.Billing.Infrastructure;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(i => i.Id);
        builder.HasIndex(i => new { i.ClinicId, i.Id }).IsUnique();

        builder.Property(i => i.Status).IsRequired().HasMaxLength(50);
        builder.Property(i => i.TotalAmount).HasPrecision(18, 2);

        builder.HasIndex(i => new { i.ClinicId, i.PatientId });
        builder.HasIndex(i => new { i.ClinicId, i.ConsultationId });
        builder.HasIndex(i => new { i.ClinicId, i.Status });
    }
}

public class InvoiceLineItemConfiguration : IEntityTypeConfiguration<InvoiceLineItem>
{
    public void Configure(EntityTypeBuilder<InvoiceLineItem> builder)
    {
        builder.ToTable("InvoiceLineItems");
        builder.HasKey(li => li.Id);

        builder.Property(li => li.Description).IsRequired().HasMaxLength(500);
        builder.Property(li => li.Quantity).HasPrecision(18, 2);
        builder.Property(li => li.UnitPrice).HasPrecision(18, 2);
        builder.Property(li => li.Amount).HasPrecision(18, 2);

        builder.HasOne(li => li.Invoice)
            .WithMany(i => i.LineItems)
            .HasForeignKey(li => li.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => new { p.ClinicId, p.Id }).IsUnique();

        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.Source).IsRequired().HasMaxLength(20);
        builder.Property(p => p.Mode).IsRequired().HasMaxLength(20);
        builder.Property(p => p.Status).IsRequired().HasMaxLength(20);
        builder.Property(p => p.RazorpayPaymentId).HasMaxLength(100);

        // ERD v4 Sec 1: partial unique index on Payment.RazorpayPaymentId WHERE NOT NULL — idempotency
        builder.HasIndex(p => p.RazorpayPaymentId)
            .IsUnique()
            .HasFilter("\"RazorpayPaymentId\" IS NOT NULL");

        builder.HasOne(p => p.Invoice)
            .WithMany(i => i.Payments)
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable("Refunds");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Amount).HasPrecision(18, 2);
        builder.Property(r => r.Reason).IsRequired().HasMaxLength(500);
        builder.Property(r => r.Status).IsRequired().HasMaxLength(50);
        builder.Property(r => r.RazorpayRefundId).HasMaxLength(100);

        builder.HasOne(r => r.Payment)
            .WithMany(p => p.Refunds)
            .HasForeignKey(r => new { r.ClinicId, r.PaymentId })
            .HasPrincipalKey(p => new { p.ClinicId, p.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PaymentLinkConfiguration : IEntityTypeConfiguration<PaymentLink>
{
    public void Configure(EntityTypeBuilder<PaymentLink> builder)
    {
        builder.ToTable("PaymentLinks");
        builder.HasKey(pl => pl.Id);

        builder.Property(pl => pl.TokenHash).IsRequired().HasMaxLength(256);
        builder.Property(pl => pl.Status).IsRequired().HasMaxLength(50);

        builder.HasIndex(pl => pl.TokenHash).IsUnique();

        builder.HasOne(pl => pl.Invoice)
            .WithMany(i => i.PaymentLinks)
            .HasForeignKey(pl => new { pl.ClinicId, pl.InvoiceId })
            .HasPrincipalKey(i => new { i.ClinicId, i.Id })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
