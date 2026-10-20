namespace MatteiGrid.Modules.Patients.Contracts;

public record RegisterPatientRequest(
    string Name,
    string Phone,
    DateOnly DOB,
    string? Gender = null,
    string? PrimaryChiefComplaint = null,
    string? ConsentType = null
);

public record PatientDto(
    Guid Id,
    Guid ClinicId,
    string Name,
    string Phone,
    DateOnly DOB,
    string Gender,
    string? PrimaryChiefComplaint,
    string ProfileStatus,
    int CompletionPercentage,
    IReadOnlyList<string> MissingRequiredFields,
    bool IsArchived,
    bool IsAnonymized,
    DateTimeOffset CreatedAt
);

public record DuplicatePatientWarning(bool HasPotentialDuplicate, string? Message, IReadOnlyList<PatientDto> Matches);

public record HomeopathicCaseRecordDto(
    Guid Id,
    Guid ClinicId,
    Guid PatientId,
    string? MaritalStatus,
    string? Occupation,
    string? Diet,
    int? FamilySize,
    string? Address,
    string? Email,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? EmergencyContactRelation,
    string? ReferredBy,
    string? CaseType,
    string? PlaceOfBirth,
    string? Religion,
    string? Education,
    string? SocialStatus,
    string? NutritionalStatus,
    string? FamilyStatus,
    string? OnsetTimeline,
    string? Location,
    string? SensationAndPathology,
    string? Aggravations,
    string? Ameliorations,
    string? AilmentsFrom,
    string? Concomitants,
    string? Thermals,
    string? ThermalState,
    // Functional
    string? Appetite,
    string? Thirst,
    string? Sex,
    string? Sleep,
    string? SleepPattern,
    string? Dreams,
    string? CravingsAndAversions,
    // Elimination
    string? Stool,
    string? Urine,
    string? Sweat,
    string? Breath,
    string? Discharges,
    string? SweatAndElimination,
    // Mental Generals
    string? WillAndEmotions,
    string? UnderstandingAndIntellect,
    string? Memory,
    string? IntellectAndMemory,
    string? Disposition,
    // Medical History
    string? PastIllnesses,
    string? FamilyHistory,
    // General Examination & Vitals
    string? ConsciousState,
    string? GeneralAppearance,
    string? BuildAndAppearance,
    string? BuiltAndNutrition,
    string? SystemicSigns,
    string? SkinAndNails,
    string? BloodPressure,
    int? PulseRate,
    decimal? TemperatureF,
    int? RespiratoryRate,
    decimal? HeightCm,
    decimal? WeightKg,
    decimal? BMI,
    // Allergies
    bool AllergyAssessmentConfirmed,
    bool HasKnownAllergies,
    string? AllergyDetails,
    int DossierCompletionPercentage,
    bool IsMandatoryDossierComplete,
    IReadOnlyList<string> MissingRequiredFields,
    DateTimeOffset? UpdatedAt
);

public record UpdateHomeopathicCaseRecordRequest(
    string? MaritalStatus = null,
    string? Occupation = null,
    string? Diet = null,
    int? FamilySize = null,
    string? Address = null,
    string? Email = null,
    string? EmergencyContactName = null,
    string? EmergencyContactPhone = null,
    string? EmergencyContactRelation = null,
    string? ReferredBy = null,
    string? CaseType = null,
    string? PlaceOfBirth = null,
    string? Religion = null,
    string? Education = null,
    string? SocialStatus = null,
    string? NutritionalStatus = null,
    string? FamilyStatus = null,
    string? OnsetTimeline = null,
    string? Location = null,
    string? SensationAndPathology = null,
    string? Aggravations = null,
    string? Ameliorations = null,
    string? AilmentsFrom = null,
    string? Concomitants = null,
    string? Thermals = null,
    string? ThermalState = null,
    // Functional
    string? Appetite = null,
    string? Thirst = null,
    string? Sex = null,
    string? Sleep = null,
    string? SleepPattern = null,
    string? Dreams = null,
    string? CravingsAndAversions = null,
    // Elimination
    string? Stool = null,
    string? Urine = null,
    string? Sweat = null,
    string? Breath = null,
    string? Discharges = null,
    string? SweatAndElimination = null,
    // Mental Generals
    string? WillAndEmotions = null,
    string? UnderstandingAndIntellect = null,
    string? Memory = null,
    string? IntellectAndMemory = null,
    string? Disposition = null,
    // Medical History
    string? PastIllnesses = null,
    string? FamilyHistory = null,
    // General Examination & Vitals
    string? ConsciousState = null,
    string? GeneralAppearance = null,
    string? BuildAndAppearance = null,
    string? BuiltAndNutrition = null,
    string? SystemicSigns = null,
    string? SkinAndNails = null,
    string? BloodPressure = null,
    int? PulseRate = null,
    decimal? TemperatureF = null,
    int? RespiratoryRate = null,
    decimal? HeightCm = null,
    decimal? WeightKg = null,
    decimal? BMI = null,
    bool? AllergyAssessmentConfirmed = null,
    bool? HasKnownAllergies = null,
    string? AllergyDetails = null,
    Guid? StaffId = null
);

public record PatientDetailedProfileDto(
    PatientDto Summary,
    HomeopathicCaseRecordDto? CaseRecord
);

public interface IPatientService
{
    Task<(PatientDto Patient, DuplicatePatientWarning Warning)> RegisterPatientAsync(RegisterPatientRequest request, CancellationToken ct = default);
    Task<DuplicatePatientWarning> CheckDuplicateAsync(string name, string phone, DateOnly dob, CancellationToken ct = default);
    Task<IReadOnlyList<PatientDto>> SearchPatientsAsync(string? query, CancellationToken ct = default);
    Task<PatientDto?> GetPatientByIdAsync(Guid id, CancellationToken ct = default);
    Task<PatientDetailedProfileDto?> GetDetailedProfileAsync(Guid id, CancellationToken ct = default);
    Task<PatientDetailedProfileDto?> UpdateDetailedProfileAsync(Guid id, UpdateHomeopathicCaseRecordRequest request, CancellationToken ct = default);
    Task<bool> ArchivePatientAsync(Guid id, CancellationToken ct = default);
    Task<bool> ErasePatientPiiAsync(Guid id, CancellationToken ct = default);
}