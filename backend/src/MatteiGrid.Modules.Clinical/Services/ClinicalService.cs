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
            CaseType = !string.IsNullOrWhiteSpace(request.CaseType) ? request.CaseType : CaseTypes.Chronic,
            ProvisionalDiagnosis = request.ProvisionalDiagnosis ?? string.Empty,
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
            .Include(x => x.Observations)
            .FirstOrDefaultAsync(x => x.Id == consultationId, ct);

        return c is null ? null : MapDto(c);
    }

    public async Task<ConsultationDto?> GetActiveOrLatestConsultationForPatientAsync(Guid patientId, CancellationToken ct = default)
    {
        // First check if there is an in-progress consultation
        var active = await _dbContext.Set<Consultation>()
            .Include(x => x.ClinicalNotes)
            .Include(x => x.Observations)
            .Where(x => x.PatientId == patientId && x.Status == ConsultationStatus.InProgress)
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefaultAsync(ct);

        if (active != null) return MapDto(active);

        // Fallback to latest consultation
        var latest = await _dbContext.Set<Consultation>()
            .Include(x => x.ClinicalNotes)
            .Include(x => x.Observations)
            .Where(x => x.PatientId == patientId)
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefaultAsync(ct);

        return latest is null ? null : MapDto(latest);
    }

    public async Task<ConsultationDto?> UpdateClinicalDetailsAsync(Guid consultationId, UpdateConsultationClinicalDetailsRequest request, CancellationToken ct = default)
    {
        var c = await _dbContext.Set<Consultation>()
            .Include(x => x.ClinicalNotes)
            .Include(x => x.Observations)
            .FirstOrDefaultAsync(x => x.Id == consultationId, ct);

        if (c is null) return null;

        c.CaseType = request.CaseType;
        c.ProvisionalDiagnosis = request.ProvisionalDiagnosis;
        c.FinalDiagnosis = request.FinalDiagnosis;
        c.Outcome = request.Outcome;
        c.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(ct);
        return MapDto(c);
    }

    public async Task<ConsultationObservationDto> AddObservationAsync(Guid consultationId, AddConsultationObservationRequest request, CancellationToken ct = default)
    {
        var consultation = await _dbContext.Set<Consultation>()
            .FirstOrDefaultAsync(x => x.Id == consultationId, ct)
            ?? throw new InvalidOperationException("Consultation not found.");

        var obs = new ConsultationObservation
        {
            ClinicId = _tenantContext.ClinicId ?? Guid.Empty,
            ConsultationId = consultationId,
            StaffId = request.StaffId,
            ObservationDate = DateTimeOffset.UtcNow,
            ReactionType = request.ReactionType,
            KentsObservation = request.KentsObservation,
            SecondPrescriptionAction = request.SecondPrescriptionAction,
            Notes = request.Notes
        };

        _dbContext.Set<ConsultationObservation>().Add(obs);
        await _dbContext.SaveChangesAsync(ct);

        return new ConsultationObservationDto(
            obs.Id,
            obs.ConsultationId,
            obs.StaffId,
            obs.ObservationDate,
            obs.ReactionType,
            obs.KentsObservation,
            obs.SecondPrescriptionAction,
            obs.Notes,
            obs.CreatedAt
        );
    }

    public async Task<IReadOnlyList<ConsultationObservationDto>> GetObservationsAsync(Guid consultationId, CancellationToken ct = default)
    {
        var list = await _dbContext.Set<ConsultationObservation>()
            .Where(x => x.ConsultationId == consultationId)
            .OrderByDescending(x => x.ObservationDate)
            .ToListAsync(ct);

        return list.Select(obs => new ConsultationObservationDto(
            obs.Id,
            obs.ConsultationId,
            obs.StaffId,
            obs.ObservationDate,
            obs.ReactionType,
            obs.KentsObservation,
            obs.SecondPrescriptionAction,
            obs.Notes,
            obs.CreatedAt
        )).ToList();
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
            c.CaseType,
            c.ProvisionalDiagnosis,
            c.FinalDiagnosis,
            c.Outcome,
            c.StartedAt,
            c.CompletedAt,
            (c.ClinicalNotes ?? new List<ClinicalNote>()).Select(n => new ClinicalNoteDto(n.Id, n.ConsultationId, n.StaffId, n.Content, n.CreatedAt, n.UpdatedAt)).ToList(),
            (c.Observations ?? new List<ConsultationObservation>()).Select(o => new ConsultationObservationDto(o.Id, o.ConsultationId, o.StaffId, o.ObservationDate, o.ReactionType, o.KentsObservation, o.SecondPrescriptionAction, o.Notes, o.CreatedAt)).ToList()
        );
}