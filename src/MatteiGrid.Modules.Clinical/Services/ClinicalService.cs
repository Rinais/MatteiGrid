using MatteiGrid.Modules.Clinical.Contracts;
using MatteiGrid.Modules.Clinical.Domain;
using MatteiGrid.Shared.Infrastructure.Persistence;
using MatteiGrid.Shared.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace MatteiGrid.Modules.Clinical.Services;

public class ClinicalService : IClinicalService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentTenantContext _tenantContext;

    public ClinicalService(AppDbContext dbContext, ICurrentTenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<ConsultationDto> StartConsultationAsync(StartConsultationRequest request, CancellationToken ct = default)
    {
        var consultation = new Consultation
        {
            ClinicId = _tenantContext.ClinicId ?? Guid.Empty,
            PatientId = request.PatientId,
            DoctorStaffId = request.DoctorStaffId,
            AppointmentId = request.AppointmentId,
            Status = ConsultationStatus.InProgress,
            StartedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Set<Consultation>().Add(consultation);
        await _dbContext.SaveChangesAsync(ct);

        return MapDto(consultation);
    }

    public async Task<ConsultationDto?> GetConsultationAsync(Guid consultationId, CancellationToken ct = default)
    {
        var c = await _dbContext.Set<Consultation>()
            .Include(x => x.ClinicalNotes)
            .FirstOrDefaultAsync(x => x.Id == consultationId, ct);

        return c is null ? null : MapDto(c);
    }

    public async Task<ClinicalNoteDto> AddOrUpdateNoteAsync(Guid consultationId, Guid staffId, string content, Guid? noteId = null, CancellationToken ct = default)
    {
        ClinicalNote note;
        if (noteId.HasValue)
        {
            note = await _dbContext.Set<ClinicalNote>()
                .FirstOrDefaultAsync(n => n.Id == noteId.Value && n.ConsultationId == consultationId, ct)
                ?? throw new InvalidOperationException("Clinical note not found.");

            note.Content = content;
            note.UpdatedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            note = new ClinicalNote
            {
                ClinicId = _tenantContext.ClinicId ?? Guid.Empty,
                ConsultationId = consultationId,
                StaffId = staffId,
                Content = content
            };
            _dbContext.Set<ClinicalNote>().Add(note);
        }

        await _dbContext.SaveChangesAsync(ct);
        return new ClinicalNoteDto(note.Id, note.ConsultationId, note.StaffId, note.Content, note.CreatedAt, note.UpdatedAt);
    }

    public async Task<bool> CompleteConsultationAsync(Guid consultationId, CancellationToken ct = default)
    {
        var c = await _dbContext.Set<Consultation>().FirstOrDefaultAsync(x => x.Id == consultationId, ct);
        if (c is null) return false;

        c.Status = ConsultationStatus.Completed;
        c.CompletedAt = DateTimeOffset.UtcNow;
        c.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    private static ConsultationDto MapDto(Consultation c) =>
        new(
            c.Id,
            c.ClinicId,
            c.PatientId,
            c.DoctorStaffId,
            c.AppointmentId,
            c.Status,
            c.StartedAt,
            c.CompletedAt,
            c.ClinicalNotes.Select(n => new ClinicalNoteDto(n.Id, n.ConsultationId, n.StaffId, n.Content, n.CreatedAt, n.UpdatedAt)).ToList()
        );
}