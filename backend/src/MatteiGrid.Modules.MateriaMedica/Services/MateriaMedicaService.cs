using MatteiGrid.Modules.MateriaMedica.Contracts;
using MatteiGrid.Modules.MateriaMedica.Domain;
using MatteiGrid.Shared.Infrastructure.Persistence;
using MatteiGrid.Shared.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace MatteiGrid.Modules.MateriaMedica.Services;

public class MateriaMedicaService : IMateriaMedicaService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentTenantContext _tenantContext;

    public MateriaMedicaService(AppDbContext dbContext, ICurrentTenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<MateriaMedicaEntryDto>> SearchEntriesAsync(string? query, CancellationToken ct = default)
    {
        var q = _dbContext.Set<MateriaMedicaEntry>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var clean = query.Trim();
            q = q.Where(m =>
                EF.Functions.ILike(m.Name, $"%{clean}%") ||
                EF.Functions.ILike(m.Code, $"%{clean}%") ||
                EF.Functions.ILike(m.Family, $"%{clean}%") ||
                EF.Functions.ILike(m.Indications, $"%{clean}%"));
        }

        var list = await q.OrderBy(m => m.Name).Take(50).ToListAsync(ct);

        return list.Select(m => new MateriaMedicaEntryDto(
            m.Id,
            m.Code,
            m.Name,
            m.Family,
            m.Description,
            m.Indications,
            Array.Empty<PractitionerNoteDto>()
        )).ToList();
    }

    public async Task<MateriaMedicaEntryDto?> GetEntryByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entry = await _dbContext.Set<MateriaMedicaEntry>()
            .Include(m => m.PractitionerNotes)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        if (entry is null) return null;

        var notes = entry.PractitionerNotes
            .Select(n => new PractitionerNoteDto(n.Id, n.MateriaMedicaEntryId, n.StaffId, n.Content, n.CreatedAt))
            .ToList();

        return new MateriaMedicaEntryDto(entry.Id, entry.Code, entry.Name, entry.Family, entry.Description, entry.Indications, notes);
    }

    public async Task<PractitionerNoteDto> AddPractitionerNoteAsync(Guid entryId, Guid staffId, string content, CancellationToken ct = default)
    {
        var note = new PractitionerNote
        {
            ClinicId = _tenantContext.ClinicId ?? Guid.Empty,
            MateriaMedicaEntryId = entryId,
            StaffId = staffId,
            Content = content
        };

        _dbContext.Set<PractitionerNote>().Add(note);
        await _dbContext.SaveChangesAsync(ct);

        return new PractitionerNoteDto(note.Id, note.MateriaMedicaEntryId, note.StaffId, note.Content, note.CreatedAt);
    }
}