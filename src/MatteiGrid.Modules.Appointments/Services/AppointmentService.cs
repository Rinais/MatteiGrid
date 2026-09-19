using MatteiGrid.Modules.Appointments.Contracts;
using MatteiGrid.Modules.Appointments.Domain;
using MatteiGrid.Shared.Infrastructure.Persistence;
using MatteiGrid.Shared.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace MatteiGrid.Modules.Appointments.Services;

public class AppointmentService : IAppointmentService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentTenantContext _tenantContext;

    public AppointmentService(AppDbContext dbContext, ICurrentTenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<OverlapCheckResult> CheckOverlapAsync(Guid doctorStaffId, DateTimeOffset startAt, DateTimeOffset endAt, Guid? excludeAppointmentId = null, CancellationToken ct = default)
    {
        var query = _dbContext.Set<Appointment>()
            .Where(a => a.DoctorStaffId == doctorStaffId &&
                        a.Status != AppointmentStatus.Cancelled &&
                        a.StartAt < endAt &&
                        a.EndAt > startAt);

        if (excludeAppointmentId.HasValue)
        {
            query = query.Where(a => a.Id != excludeAppointmentId.Value);
        }

        var conflicting = await query.FirstOrDefaultAsync(ct);
        if (conflicting is null)
            return new OverlapCheckResult(false, null);

        return new OverlapCheckResult(true, $"Doctor is already booked between {conflicting.StartAt:t} and {conflicting.EndAt:t}.");
    }

    public async Task<(AppointmentDto Appointment, OverlapCheckResult Overlap)> BookAppointmentAsync(BookAppointmentRequest request, CancellationToken ct = default)
    {
        var overlap = await CheckOverlapAsync(request.DoctorStaffId, request.StartAt, request.EndAt, null, ct);

        if (overlap.HasOverlap && string.IsNullOrWhiteSpace(request.OverrideReason))
        {
            throw new InvalidOperationException("Double-booking detected. An override reason is required to proceed.");
        }

        var appointment = new Appointment
        {
            ClinicId = _tenantContext.ClinicId ?? Guid.Empty,
            PatientId = request.PatientId,
            DoctorStaffId = request.DoctorStaffId,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            Status = AppointmentStatus.Scheduled,
            OverrideReason = request.OverrideReason
        };

        _dbContext.Set<Appointment>().Add(appointment);
        await _dbContext.SaveChangesAsync(ct);

        return (MapDto(appointment), overlap);
    }

    public async Task<IReadOnlyList<AppointmentDto>> GetAppointmentsAsync(DateTimeOffset? from, DateTimeOffset? to, Guid? doctorStaffId, CancellationToken ct = default)
    {
        var query = _dbContext.Set<Appointment>().AsQueryable();

        if (from.HasValue) query = query.Where(a => a.StartAt >= from.Value);
        if (to.HasValue) query = query.Where(a => a.EndAt <= to.Value);
        if (doctorStaffId.HasValue) query = query.Where(a => a.DoctorStaffId == doctorStaffId.Value);

        var list = await query.OrderBy(a => a.StartAt).ToListAsync(ct);
        return list.Select(MapDto).ToList();
    }

    public async Task<bool> UpdateStatusAsync(Guid appointmentId, string status, CancellationToken ct = default)
    {
        var appt = await _dbContext.Set<Appointment>().FirstOrDefaultAsync(a => a.Id == appointmentId, ct);
        if (appt is null) return false;

        appt.Status = status;
        appt.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    private static AppointmentDto MapDto(Appointment a) =>
        new(a.Id, a.ClinicId, a.PatientId, a.DoctorStaffId, a.StartAt, a.EndAt, a.Status, a.OverrideReason, a.CreatedAt);
}