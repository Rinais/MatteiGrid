using FluentAssertions;
using MatteiGrid.Modules.Billing.Domain;
using Xunit;

namespace MatteiGrid.UnitTests;

public class InvoiceStateMachineTests
{
    [Fact]
    public void NewInvoice_ShouldDefaultTo_PendingStatus()
    {
        var invoice = new Invoice
        {
            TotalAmount = 500m
        };

        invoice.Status.Should().Be(InvoiceStatus.Pending);
        invoice.RemainingBalance.Should().Be(500m);
    }

    [Fact]
    public void RecordingPartialPayment_ShouldTransitionTo_PartiallyPaid()
    {
        var invoice = new Invoice
        {
            TotalAmount = 1000m
        };

        invoice.RecordPayment(new Payment
        {
            Amount = 400m,
            Status = PaymentStatus.Confirmed
        });

        invoice.Status.Should().Be(InvoiceStatus.PartiallyPaid);
        invoice.TotalPaidAmount.Should().Be(400m);
        invoice.RemainingBalance.Should().Be(600m);
    }

    [Fact]
    public void RecordingFullPayment_ShouldTransitionTo_Paid()
    {
        var invoice = new Invoice
        {
            TotalAmount = 500m
        };

        invoice.RecordPayment(new Payment
        {
            Amount = 500m,
            Status = PaymentStatus.Confirmed
        });

        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.RemainingBalance.Should().Be(0m);
    }

    [Fact]
    public void RecordingOverpayment_ShouldThrowInvalidOperationException()
    {
        var invoice = new Invoice
        {
            TotalAmount = 500m
        };

        var act = () => invoice.RecordPayment(new Payment
        {
            Amount = 550m,
            Status = PaymentStatus.Confirmed
        });

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*exceeds remaining balance*");
    }

    [Fact]
    public void CancellingInvoice_WithZeroPayments_ShouldSucceed()
    {
        var invoice = new Invoice
        {
            TotalAmount = 300m
        };

        invoice.Cancel();

        invoice.Status.Should().Be(InvoiceStatus.Cancelled);
    }

    [Fact]
    public void CancellingInvoice_WithRecordedPayment_ShouldThrowInvalidOperationException()
    {
        var invoice = new Invoice
        {
            TotalAmount = 500m
        };

        invoice.RecordPayment(new Payment
        {
            Amount = 200m,
            Status = PaymentStatus.Confirmed
        });

        var act = () => invoice.Cancel();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot cancel an invoice once any payment has been recorded*");
    }

    [Fact]
    public void RefundingInvoice_FromPaidStatus_ShouldSucceed()
    {
        var invoice = new Invoice
        {
            TotalAmount = 500m
        };

        invoice.RecordPayment(new Payment
        {
            Amount = 500m,
            Status = PaymentStatus.Confirmed
        });

        invoice.MarkRefunded();

        invoice.Status.Should().Be(InvoiceStatus.Refunded);
    }

    [Fact]
    public void RefundingInvoice_FromPendingStatus_ShouldThrowInvalidOperationException()
    {
        var invoice = new Invoice
        {
            TotalAmount = 500m
        };

        var act = () => invoice.MarkRefunded();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Only Paid or PartiallyPaid invoices can be transitioned to Refunded*");
    }
}
