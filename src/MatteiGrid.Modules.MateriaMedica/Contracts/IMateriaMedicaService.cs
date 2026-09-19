namespace MatteiGrid.Modules.MateriaMedica.Contracts;

public record MateriaMedicaEntryDto(Guid Id, string Code, string Name, string Family, string Description, string Indications, IReadOnlyList<PractitionerNoteDto> Notes);
public record PractitionerNoteDto(Guid Id, Guid MateriaMedicaEntryId, Guid StaffId, string Content, DateTimeOffset CreatedAt);

public interface IMateriaMedicaService
{
    Task<IReadOnlyList<MateriaMedicaEntryDto>> SearchEntriesAsync(string? query, CancellationToken ct = default);
    Task<MateriaMedicaEntryDto?> GetEntryByIdAsync(Guid id, CancellationToken ct = default);
    Task<PractitionerNoteDto> AddPractitionerNoteAsync(Guid entryId, Guid staffId, string content, CancellationToken ct = default);
}