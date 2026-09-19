using FluentAssertions;
using MatteiGrid.Modules.Billing.Contracts;
using MatteiGrid.Modules.Billing.Domain;
using MatteiGrid.Modules.Billing.Services;
using MatteiGrid.Modules.Inventory.Contracts;
using MatteiGrid.Modules.Inventory.Domain;
using MatteiGrid.Modules.Inventory.Services;
using MatteiGrid.Shared.Infrastructure.Persistence;
using MatteiGrid.Shared.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MatteiGrid.UnitTests;

public class EndToEndServiceTests
{
    private static CurrentTenantContext CreateTenantContext()
    {
        var tenant = new CurrentTenantContext();
        tenant.SetTenant(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            new[] { "Admin" });
        return tenant;
    }

    [Fact]
    public async Task InventoryService_RecordLabDeduction_ShouldSucceed()
    {
        var tenant = CreateTenantContext();
        var interceptor = new TenantSaveChangesInterceptor(tenant);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        using var db = new AppDbContext(options, tenant);
        var medId = Guid.NewGuid();
        var initialItem = new InventoryItem
        {
            ClinicId = tenant.ClinicId!.Value,
            MedicineId = medId,
            QuantityOnHand = 100m,
            ReorderLevel = 10m
        };
        db.Set<InventoryItem>().Add(initialItem);
        await db.SaveChangesAsync();

        var service = new InventoryService(db, tenant);
        var tx = await service.RecordLabDeductionAsync(medId, 5m, Guid.NewGuid());

        tx.Should().NotBeNull();
        tx.QuantityDelta.Should().Be(-5m);

        var updatedItem = await db.Set<InventoryItem>().FirstAsync(i => i.MedicineId == medId);
        updatedItem.QuantityOnHand.Should().Be(95m);
    }

    [Fact]
    public async Task BillingService_RecordManualPayment_ShouldSucceed()
    {
        var tenant = CreateTenantContext();
        var interceptor = new TenantSaveChangesInterceptor(tenant);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        using var db = new AppDbContext(options, tenant);
        var invoice = new Invoice
        {
            ClinicId = tenant.ClinicId!.Value,
            PatientId = Guid.NewGuid(),
            ConsultationId = Guid.NewGuid(),
            TotalAmount = 500m,
            Status = InvoiceStatus.Pending
        };
        db.Set<Invoice>().Add(invoice);
        await db.SaveChangesAsync();

        var service = new BillingService(db, tenant);
        var result = await service.RecordManualPaymentAsync(invoice.Id, new RecordManualPaymentRequest(500m, "Cash", tenant.UserId!.Value));

        result.Should().NotBeNull();
        result.Status.Should().Be(InvoiceStatus.Paid);
        result.RemainingBalance.Should().Be(0m);
    }
}
