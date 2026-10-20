using MatteiGrid.Modules.Patients.Contracts;
using MatteiGrid.Modules.Patients.Domain;
using MatteiGrid.Shared.Infrastructure.Persistence;
using MatteiGrid.Shared.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MatteiGrid.Modules.Patients.Services;

public class PatientService : IPatientService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly ILogger<PatientService>? _logger;

    public PatientService(AppDbContext dbContext, ICurrentTenantContext tenantContext, ILogger<PatientService>? logger = null)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _logger = logger;
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
        _logger?.LogWarning("[WARN] [DUPLICATE_CHECK] Potential duplicate patient match detected: Found {Count} existing patient(s) matching Phone '{Phone}' or Name '{Name}' and DOB '{DOB}' in Clinic {ClinicId}",
            matches.Count, safePhone, safeName, dob, _tenantContext.ClinicId);

        return new DuplicatePatientWarning(true, $"Found {matches.Count} potential matching existing patient(s).", matches);
    }

    public async Task<(PatientDto Patient, DuplicatePatientWarning Warning)> RegisterPatientAsync(RegisterPatientRequest request, CancellationToken ct = default)
    {
        var safeName = (request.Name ?? string.Empty).Trim();
        var safePhone = (request.Phone ?? string.Empty).Trim();
        var safeGender = string.IsNullOrWhiteSpace(request.Gender) ? "Undisclosed" : request.Gender.Trim();
        var safeComplaint = string.IsNullOrWhiteSpace(request.PrimaryChiefComplaint) ? null : request.PrimaryChiefComplaint.Trim();

        var warning = await CheckDuplicateAsync(safeName, safePhone, request.DOB, ct);
        if (warning.HasPotentialDuplicate)
        {
            _logger?.LogWarning("[WARN] [PATIENT_REGISTRATION] Proceeding to register patient '{Name}' with {Count} potential duplicate(s) flagged",
                safeName, warning.Matches.Count);
        }

        var patient = new Patient
        {
            Name = safeName,
            Phone = safePhone,
            DOB = request.DOB,
            Gender = safeGender,
            PrimaryChiefComplaint = safeComplaint,
            ClinicId = _tenantContext.ClinicId ?? Guid.Empty
        };

        // Calculate initial dynamic profile completion
        var completion = ProfileCompletionCalculator.Calculate(patient, null);
        patient.ProfileStatus = completion.Status;
        patient.CompletionPercentage = completion.Score;
        patient.MissingFieldsJson = ProfileCompletionCalculator.SerializeMissingFields(completion.MissingFields);

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

    public async Task<PatientDetailedProfileDto?> GetDetailedProfileAsync(Guid id, CancellationToken ct = default)
    {
        var p = await _dbContext.Set<Patient>()
            .Include(x => x.CaseRecord)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (p is null) return null;

        return new PatientDetailedProfileDto(
            MapDto(p),
            p.CaseRecord is null ? null : MapCaseRecordDto(p.CaseRecord)
        );
    }

    public async Task<PatientDetailedProfileDto?> UpdateDetailedProfileAsync(Guid id, UpdateHomeopathicCaseRecordRequest request, CancellationToken ct = default)
    {
        var patient = await _dbContext.Set<Patient>()
            .Include(x => x.CaseRecord)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (patient is null) return null;

        var clinicId = _tenantContext.ClinicId ?? patient.ClinicId;
        var caseRecord = patient.CaseRecord;
        if (caseRecord is null)
        {
            caseRecord = new HomeopathicCaseRecord
            {
                ClinicId = clinicId,
                PatientId = patient.Id
            };
            _dbContext.Set<HomeopathicCaseRecord>().Add(caseRecord);
            patient.CaseRecord = caseRecord;
        }

        // Apply Section Updates
        if (request.MaritalStatus != null) caseRecord.MaritalStatus = request.MaritalStatus;
        if (request.Occupation != null) caseRecord.Occupation = request.Occupation;
        if (request.Diet != null) caseRecord.Diet = request.Diet;
        if (request.FamilySize.HasValue) caseRecord.FamilySize = request.FamilySize;
        if (request.Address != null) caseRecord.Address = request.Address;
        if (request.Email != null) caseRecord.Email = request.Email;
        if (request.EmergencyContactName != null) caseRecord.EmergencyContactName = request.EmergencyContactName;
        if (request.EmergencyContactPhone != null) caseRecord.EmergencyContactPhone = request.EmergencyContactPhone;
        if (request.EmergencyContactRelation != null) caseRecord.EmergencyContactRelation = request.EmergencyContactRelation;
        if (request.ReferredBy != null) caseRecord.ReferredBy = request.ReferredBy;
        if (request.CaseType != null) caseRecord.CaseType = request.CaseType;
        if (request.PlaceOfBirth != null) caseRecord.PlaceOfBirth = request.PlaceOfBirth;
        if (request.Religion != null) caseRecord.Religion = request.Religion;
        if (request.Education != null) caseRecord.Education = request.Education;
        if (request.SocialStatus != null) caseRecord.SocialStatus = request.SocialStatus;
        if (request.NutritionalStatus != null) caseRecord.NutritionalStatus = request.NutritionalStatus;
        if (request.FamilyStatus != null) caseRecord.FamilyStatus = request.FamilyStatus;

        if (request.OnsetTimeline != null) caseRecord.OnsetTimeline = request.OnsetTimeline;
        if (request.Location != null) caseRecord.Location = request.Location;
        if (request.SensationAndPathology != null) caseRecord.SensationAndPathology = request.SensationAndPathology;
        if (request.Aggravations != null) caseRecord.Aggravations = request.Aggravations;
        if (request.Ameliorations != null) caseRecord.Ameliorations = request.Ameliorations;
        if (request.AilmentsFrom != null) caseRecord.AilmentsFrom = request.AilmentsFrom;
        if (request.Concomitants != null) caseRecord.Concomitants = request.Concomitants;

        if (request.Thermals != null) caseRecord.Thermals = request.Thermals;
        else if (request.ThermalState != null) caseRecord.ThermalState = request.ThermalState;

        // Functional
        if (request.Appetite != null) caseRecord.Appetite = request.Appetite;
        if (request.Thirst != null) caseRecord.Thirst = request.Thirst;
        if (request.Sex != null) caseRecord.Sex = request.Sex;
        if (request.Sleep != null) caseRecord.Sleep = request.Sleep;
        else if (request.SleepPattern != null) caseRecord.SleepPattern = request.SleepPattern;
        if (request.Dreams != null) caseRecord.Dreams = request.Dreams;
        if (request.CravingsAndAversions != null) caseRecord.CravingsAndAversions = request.CravingsAndAversions;

        // Elimination
        if (request.Stool != null) caseRecord.Stool = request.Stool;
        if (request.Urine != null) caseRecord.Urine = request.Urine;
        if (request.Sweat != null) caseRecord.Sweat = request.Sweat;
        if (request.Breath != null) caseRecord.Breath = request.Breath;
        if (request.Discharges != null) caseRecord.Discharges = request.Discharges;
        if (request.SweatAndElimination != null && caseRecord.Sweat == null) caseRecord.Sweat = request.SweatAndElimination;

        // Mental Generals
        if (request.WillAndEmotions != null) caseRecord.WillAndEmotions = request.WillAndEmotions;
        if (request.UnderstandingAndIntellect != null) caseRecord.UnderstandingAndIntellect = request.UnderstandingAndIntellect;
        if (request.Memory != null) caseRecord.Memory = request.Memory;
        if (request.IntellectAndMemory != null && caseRecord.UnderstandingAndIntellect == null) caseRecord.UnderstandingAndIntellect = request.IntellectAndMemory;
        if (request.Disposition != null) caseRecord.Disposition = request.Disposition;

        if (request.PastIllnesses != null) caseRecord.PastIllnesses = request.PastIllnesses;
        if (request.FamilyHistory != null) caseRecord.FamilyHistory = request.FamilyHistory;

        // General Examination & Vitals
        if (request.ConsciousState != null) caseRecord.ConsciousState = request.ConsciousState;
        if (request.GeneralAppearance != null) caseRecord.GeneralAppearance = request.GeneralAppearance;
        if (request.BuildAndAppearance != null) caseRecord.BuildAndAppearance = request.BuildAndAppearance;
        if (request.BuiltAndNutrition != null) caseRecord.BuiltAndNutrition = request.BuiltAndNutrition;
        if (request.SystemicSigns != null) caseRecord.SystemicSigns = request.SystemicSigns;
        if (request.SkinAndNails != null) caseRecord.SkinAndNails = request.SkinAndNails;
        if (request.BloodPressure != null) caseRecord.BloodPressure = request.BloodPressure;
        if (request.PulseRate.HasValue) caseRecord.PulseRate = request.PulseRate;
        if (request.TemperatureF.HasValue) caseRecord.TemperatureF = request.TemperatureF;
        if (request.RespiratoryRate.HasValue) caseRecord.RespiratoryRate = request.RespiratoryRate;
        if (request.HeightCm.HasValue) caseRecord.HeightCm = request.HeightCm;
        if (request.WeightKg.HasValue) caseRecord.WeightKg = request.WeightKg;
        if (request.BMI.HasValue) caseRecord.BMI = request.BMI;

        if (request.AllergyAssessmentConfirmed.HasValue) caseRecord.AllergyAssessmentConfirmed = request.AllergyAssessmentConfirmed.Value;
        if (request.HasKnownAllergies.HasValue) caseRecord.HasKnownAllergies = request.HasKnownAllergies.Value;
        if (request.AllergyDetails != null) caseRecord.AllergyDetails = request.AllergyDetails;

        if (request.StaffId.HasValue) caseRecord.LastRecordedByStaffId = request.StaffId;
        caseRecord.UpdatedAt = DateTimeOffset.UtcNow;
        patient.UpdatedAt = DateTimeOffset.UtcNow;

        // Dynamic Real-time Recalculation
        var completion = ProfileCompletionCalculator.Calculate(patient, caseRecord);
        patient.ProfileStatus = completion.Status;
        patient.CompletionPercentage = completion.Score;
        patient.MissingFieldsJson = ProfileCompletionCalculator.SerializeMissingFields(completion.MissingFields);

        caseRecord.DossierCompletionPercentage = completion.Score;
        caseRecord.IsMandatoryDossierComplete = completion.IsComplete;
        caseRecord.MissingRequiredFieldsJson = patient.MissingFieldsJson;

        await _dbContext.SaveChangesAsync(ct);

        _logger?.LogInformation("[INFO] [CASE_DOSSIER_UPDATED] Patient {PatientId} case dossier updated. Score: {Score}%, Status: {Status}, Missing: {MissingCount}",
            patient.Id, completion.Score, completion.Status, completion.MissingFields.Count);

        return new PatientDetailedProfileDto(MapDto(patient), MapCaseRecordDto(caseRecord));
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
        var patient = await _dbContext.Set<Patient>()
            .Include(x => x.CaseRecord)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (patient is null) return false;

        patient.AnonymizePii();
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    private static PatientDto MapDto(Patient p)
    {
        var missing = ProfileCompletionCalculator.DeserializeMissingFields(p.MissingFieldsJson);
        return new PatientDto(
            p.Id,
            p.ClinicId,
            p.Name,
            p.Phone,
            p.DOB,
            p.Gender,
            p.PrimaryChiefComplaint,
            p.ProfileStatus,
            p.CompletionPercentage,
            missing,
            p.IsArchived,
            p.IsAnonymized,
            p.CreatedAt
        );
    }

    private static HomeopathicCaseRecordDto MapCaseRecordDto(HomeopathicCaseRecord r)
    {
        var missing = ProfileCompletionCalculator.DeserializeMissingFields(r.MissingRequiredFieldsJson);
        return new HomeopathicCaseRecordDto(
            r.Id,
            r.ClinicId,
            r.PatientId,
            r.MaritalStatus,
            r.Occupation,
            r.Diet,
            r.FamilySize,
            r.Address,
            r.Email,
            r.EmergencyContactName,
            r.EmergencyContactPhone,
            r.EmergencyContactRelation,
            r.ReferredBy,
            r.CaseType,
            r.PlaceOfBirth,
            r.Religion,
            r.Education,
            r.SocialStatus,
            r.NutritionalStatus,
            r.FamilyStatus,
            r.OnsetTimeline,
            r.Location,
            r.SensationAndPathology,
            r.Aggravations,
            r.Ameliorations,
            r.AilmentsFrom,
            r.Concomitants,
            r.Thermals ?? r.ThermalState,
            r.ThermalState ?? r.Thermals,
            // Functional
            r.Appetite,
            r.Thirst,
            r.Sex,
            r.Sleep ?? r.SleepPattern,
            r.SleepPattern ?? r.Sleep,
            r.Dreams,
            r.CravingsAndAversions,
            // Elimination
            r.Stool,
            r.Urine,
            r.Sweat,
            r.Breath,
            r.Discharges,
            r.SweatAndElimination,
            // Mental Generals
            r.WillAndEmotions,
            r.UnderstandingAndIntellect,
            r.Memory,
            r.IntellectAndMemory,
            r.Disposition,
            // Medical History
            r.PastIllnesses,
            r.FamilyHistory,
            // General Examination & Vitals
            r.ConsciousState,
            r.GeneralAppearance,
            r.BuildAndAppearance,
            r.BuiltAndNutrition,
            r.SystemicSigns,
            r.SkinAndNails,
            r.BloodPressure,
            r.PulseRate,
            r.TemperatureF,
            r.RespiratoryRate,
            r.HeightCm,
            r.WeightKg,
            r.BMI,
            // Allergies
            r.AllergyAssessmentConfirmed,
            r.HasKnownAllergies,
            r.AllergyDetails,
            r.DossierCompletionPercentage,
            r.IsMandatoryDossierComplete,
            missing,
            r.UpdatedAt
        );
    }
}