using MatteiGrid.Shared.Infrastructure.Tenancy;
using MatteiGrid.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MatteiGrid.Api.Hubs;

public interface ILabQueueClient
{
    Task OrderReceived(Guid orderId, Guid prescriptionId);
    Task OrderStatusChanged(Guid orderId, string newStatus);
    Task OrderRejected(Guid orderId, string reason);
}

[Authorize]
public class LabQueueHub : Hub<ILabQueueClient>
{
    private readonly ICurrentTenantContext _tenantContext;

    public LabQueueHub(ICurrentTenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public async Task JoinClinicGroup(string clinicId)
    {
        // Enforce that user can only join group of their authenticated clinic
        if (!Guid.TryParse(clinicId, out var targetClinic) || targetClinic != _tenantContext.ClinicId)
        {
            throw new HubException("Cross-tenant group access denied.");
        }

        // Lab Techs, Doctors, and Administrators can listen to lab events
        var allowedRoles = new[] { Roles.LabTechnician, Roles.Doctor, Roles.Administrator, Roles.SuperAdmin };
        if (!allowedRoles.Contains(_tenantContext.Role))
        {
            throw new HubException("Role not authorized for lab queue telemetry.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"clinic-{clinicId}:lab");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"clinic-{clinicId}");
    }

    public async Task LeaveClinicGroup(string clinicId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"clinic-{clinicId}:lab");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"clinic-{clinicId}");
    }
}

public interface IDashboardClient
{
    Task AppointmentBooked(Guid appointmentId);
    Task PaymentReceived(Guid invoiceId, decimal amount);
    Task LowStockAlert(Guid medicineId, decimal remainingQuantity);
}

[Authorize]
public class DashboardHub : Hub<IDashboardClient>
{
    private readonly ICurrentTenantContext _tenantContext;

    public DashboardHub(ICurrentTenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public async Task JoinClinicGroup(string clinicId)
    {
        if (!Guid.TryParse(clinicId, out var targetClinic) || targetClinic != _tenantContext.ClinicId)
        {
            throw new HubException("Cross-tenant group access denied.");
        }

        // Join clinic general dashboard and role-specific channel
        await Groups.AddToGroupAsync(Context.ConnectionId, $"clinic-{clinicId}");
        if (!string.IsNullOrEmpty(_tenantContext.Role))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"clinic-{clinicId}:{_tenantContext.Role.ToLowerInvariant()}");
        }
    }
}
