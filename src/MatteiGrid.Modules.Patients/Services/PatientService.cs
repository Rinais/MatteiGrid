using MatteiGrid.Modules.Patients.Contracts;
using MatteiGrid.Modules.Patients.Domain;
using MatteiGrid.Shared.Infrastructure.Persistence;
using MatteiGrid.Shared.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace MatteiGrid.Modules.Patients.Services;

public class PatientService : IPatientService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentTenantContext _tenantContext;

    public PatientService(AppDbContext dbContext, ICurrentTenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<DuplicatePatientWarning> CheckDuplicateAsync(string name, string phone, DateOnly dob, CancellationToken ct = default)
    {
        var safeName = (name ?? string.Empty).Trim().ToLower();
        var safePhone = (phone ?? string.Empty).Trim();

        var candidates = await _dbContext.Set<Patient>()
            .Where(p => !p.IsArchived && (p.Phone == safePhone || (p.DOB == dob && p.Name.ToLower().Contains(safeName))))
            .Take(5)
            .ToListAsync(ct);

        if (candidates.Count == 0)
            return new DuplicatePatientWarning(false, null, Array.Empty<PatientDto>());

        var matches = candidates.Select(MapDto).ToList();
        return new DuplicatePatientWarning(true, $"Found {matches.Count} potential matching existing patient(s).", matches);
    }

    public async Task<(PatientDto Patient, DuplicatePatientWarning Warning)> RegisterPatientAsync(RegisterPatientRequest request, CancellationToken ct = default)
    {
        var safeName = (request.Name ?? string.Empty).Trim();
        var safePhone = (request.Phone ?? string.Empty).Trim();

        var warning = await CheckDuplicateAsync(safeName, safePhone, request.DOB, ct);

        var patient = new Patient
        {
            Name = safeName,
            Phone = safePhone,
            DOB = request.DOB,
            ClinicId = _tenantContext.ClinicId ?? Guid.Empty
        };

        if (!string.IsNullOrWhiteSpace(request.ConsentType))
        {
            patient.ConsentRecords.Add(new ConsentRecord
            {
                ClinicId = _tenantContext.ClinicId ?? Guid.Empty,
                ConsentType = request.ConsentType,
                PolicyVersion = "1.0",
                GrantedAt = DateTimeOffset.UtcNow
            });
        }

        _dbContext.Set<Patient>().Add(patient);
        await _dbContext.SaveChangesAsync(ct);

        return (MapDto(patient), warning);
    }

    public async Task<IReadOnlyList<PatientDto>> SearchPatientsAsync(string? query, CancellationToken ct = default)
    {
        var q = _dbContext.Set<Patient>().Where(p => !p.IsArchived);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var cleanQuery = query.Trim().ToLower();
            q = q.Where(p => p.Name.ToLower().Contains(cleanQuery) || p.Phone.Contains(cleanQuery));
        }

        var list = await q.OrderByDescending(p => p.CreatedAt).Take(50).ToListAsync(ct);
        return list.Select(MapDto).ToList();
    }

    public async Task<PatientDto?> GetPatientByIdAsync(Guid id, CancellationToken ct = default)
    {
        var p = await _dbContext.Set<Patient>().FirstOrDefaultAsync(x => x.Id == id, ct);
        return p is null ? null : MapDto(p);
    }

    public async Task<bool> ArchivePatientAsync(Guid id, CancellationToken ct = default)
    {
        var patient = await _dbContext.Set<Patient>().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (patient is null) return false;

        patient.IsArchived = true;
        patient.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ErasePatientPiiAsync(Guid id, CancellationToken ct = default)
    {
        var patient = await _dbContext.Set<Patient>().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (patient is null) return false;

        patient.AnonymizePii();
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    private static PatientDto MapDto(Patient p) =>
        new(p.Id, p.ClinicId, p.Name, p.Phone, p.DOB, p.IsArchived, p.IsAnonymized, p.CreatedAt);
}