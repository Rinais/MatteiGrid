using Microsoft.AspNetCore.SignalR;

namespace MatteiGrid.Api.Hubs;

public interface ILabQueueClient
{
    Task OrderReceived(Guid orderId, Guid prescriptionId);
    Task OrderStatusChanged(Guid orderId, string newStatus);
    Task OrderRejected(Guid orderId, string reason);
}

public class LabQueueHub : Hub<ILabQueueClient>
{
    public async Task JoinClinicGroup(string clinicId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"clinic-{clinicId}");
    }

    public async Task LeaveClinicGroup(string clinicId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"clinic-{clinicId}");
    }
}

public interface IDashboardClient
{
    Task AppointmentBooked(Guid appointmentId);
    Task PaymentReceived(Guid invoiceId, decimal amount);
    Task LowStockAlert(Guid medicineId, decimal remainingQuantity);
}

public class DashboardHub : Hub<IDashboardClient>
{
    public async Task JoinClinicGroup(string clinicId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"clinic-{clinicId}");
    }
}
