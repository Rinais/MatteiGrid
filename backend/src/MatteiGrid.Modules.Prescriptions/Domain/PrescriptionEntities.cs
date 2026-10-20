using MatteiGrid.Shared.Kernel;

namespace MatteiGrid.Modules.Prescriptions.Domain;

public static class PrescriptionStatus
{
    public const string Draft = "Draft";
    public const string Finalized = "Finalized";
}

public static class Electricities
{
    public const string Red = "Red";
    public const string Blue = "Blue";
    public const string Green = "Green";
    public const string Yellow = "Yellow";
    public const string White = "White";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Red, Blue, Green, Yellow, White
    };
}

public class Medicine : ClinicScopedEntity
{
    public Guid? MateriaMedicaEntryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class Dilution : ClinicScopedEntity
{
    public string Label { get; set; } = string.Empty; // e.g. 6X, 30C
}

public class Vehicle : ClinicScopedEntity
{
    public string Name { get; set; } = string.Empty;
}

public static class PrescriptionSelectionMethod
{
    public const string Repertorial = "Repertorial";
    public const string NonRepertorial = "NonRepertorial";

    public static readonly IReadOnlyList<string> All = new[] { Repertorial, NonRepertorial };
}

public class Prescription : IClinicScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClinicId { get; set; }
    public Guid ConsultationId { get; set; }
    public Guid? PreviousVersionId { get; set; } // Nullable, UNIQUE — enforces linear v1-v2-v3 chain
    public int Version { get; set; } = 1;
    public string Status { get; set; } = PrescriptionStatus.Draft;
    public Guid? FinalizedByStaffId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FinalizedAt { get; set; } // Immutable once finalized — no UpdatedAt per ERD v4

    // Organon Case Sheet Page 6: Diet & Regimen instructions
    public string? DietAndRegimenInstructions { get; set; }
    public string? GeneralInstructions { get; set; }

    public Prescription? PreviousVersion { get; set; }
    public ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
}

public class PrescriptionItem : IClinicScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClinicId { get; set; } // Defense-in-depth tenant isolation
    public Guid PrescriptionId { get; set; }
    public Guid MedicineId { get; set; }
    public Guid DilutionId { get; set; }
    public Guid VehicleId { get; set; }
    public string ElectricityPolarity { get; set; } = Electricities.Red;
    public decimal Quantity { get; set; }
    public string Dosage { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;

    // Organon Page 6 & Clinical Prescription Grid Enhancements
    public string SelectionMethod { get; set; } = PrescriptionSelectionMethod.Repertorial;
    public string Frequency { get; set; } = "TDS";
    public string Instructions { get; set; } = string.Empty;

    public Prescription? Prescription { get; set; }
    public Medicine? Medicine { get; set; }
    public Dilution? Dilution { get; set; }
    public Vehicle? Vehicle { get; set; }
}
