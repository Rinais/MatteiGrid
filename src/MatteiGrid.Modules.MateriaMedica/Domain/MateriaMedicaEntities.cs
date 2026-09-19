using MatteiGrid.Shared.Kernel;

namespace MatteiGrid.Modules.MateriaMedica.Domain;

public class MateriaMedicaEntry : IAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Family { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Indications { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public ICollection<PractitionerNote> PractitionerNotes { get; set; } = new List<PractitionerNote>();
}

public class PractitionerNote : ClinicScopedEntity
{
    public Guid MateriaMedicaEntryId { get; set; }
    public Guid StaffId { get; set; }
    public string Content { get; set; } = string.Empty;

    public MateriaMedicaEntry? MateriaMedicaEntry { get; set; }
}
