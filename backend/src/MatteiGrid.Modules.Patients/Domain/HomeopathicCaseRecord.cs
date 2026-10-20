using MatteiGrid.Shared.Kernel;

namespace MatteiGrid.Modules.Patients.Domain;

public class HomeopathicCaseRecord : ClinicScopedEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    // Section 1: Socio-Demographic & Personal History (Organon Case Record)
    public string? MaritalStatus { get; set; }
    public string? Occupation { get; set; }
    public string? Diet { get; set; } // Veg, Non Veg, Mixed
    public int? FamilySize { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelation { get; set; }
    public string? ReferredBy { get; set; }
    public string? CaseType { get; set; } // Acute, Chronic, Gynaecological, Surgical, Epidemic, Others
    public string? PlaceOfBirth { get; set; }
    public string? Religion { get; set; }
    public string? Education { get; set; }
    public string? SocialStatus { get; set; }
    public string? NutritionalStatus { get; set; }
    public string? FamilyStatus { get; set; } // Nuclear / Joined / Extended

    // Section 2: Presenting Complaints & Modalities (Organon Standard)
    public string? OnsetTimeline { get; set; }
    public string? Location { get; set; } // Tissues, Organs, Systems, Extension, Direction
    public string? SensationAndPathology { get; set; }
    public string? Aggravations { get; set; } // < (Worse from)
    public string? Ameliorations { get; set; } // > (Better from)
    public string? AilmentsFrom { get; set; } // A/F (Causation / Etiological factors)
    public string? Concomitants { get; set; } // Concomitants / Accompaniments

    // Section 3: Physical Generals (Organon Page 5)
    // Thermals
    public string? Thermals { get; set; } // Chilly, Hot, Ambithermal, Weather / Draft reaction
    public string? ThermalState
    {
        get => Thermals;
        set => Thermals = value;
    }

    // Functional
    public string? Appetite { get; set; }
    public string? Thirst { get; set; }
    public string? Sex { get; set; } // Sexual desire, physical symptoms, marital relations
    public string? Sleep { get; set; }
    public string? SleepPattern
    {
        get => Sleep;
        set => Sleep = value;
    }
    public string? Dreams { get; set; }
    public string? CravingsAndAversions { get; set; } // Cravings, Desires & Aversions

    // Elimination
    public string? Stool { get; set; }
    public string? Urine { get; set; }
    public string? Sweat { get; set; }
    public string? Breath { get; set; }
    public string? Discharges { get; set; }
    public string? SweatAndElimination
    {
        get => Sweat ?? Stool ?? Urine;
        set
        {
            if (Sweat == null && value != null) Sweat = value;
        }
    }

    // Section 4: Mental Generals & Disposition (Organon Page 5: Will & emotions, Understanding & intellect, Memory)
    public string? WillAndEmotions { get; set; }
    public string? UnderstandingAndIntellect { get; set; }
    public string? Memory { get; set; }
    public string? IntellectAndMemory
    {
        get => UnderstandingAndIntellect ?? Memory;
        set => UnderstandingAndIntellect = value;
    }
    public string? Disposition { get; set; }

    // Section 5: Past & Family Medical History
    public string? PastIllnesses { get; set; }
    public string? FamilyHistory { get; set; }

    // Section 6: Physical Examination & Vitals (Organon Page 5)
    public string? ConsciousState { get; set; } // Conscious / Unconscious
    public string? GeneralAppearance { get; set; } // Expression, look, decubitus
    public string? BuildAndAppearance { get; set; }
    public string? BuiltAndNutrition { get; set; }
    public string? SystemicSigns { get; set; } // Anaemia, Jaundice, Cyanosis, Lymphadenopathy, Oedema, Clubbing, Nails, Gait, Skin
    public string? SkinAndNails { get; set; }
    public string? BloodPressure { get; set; }
    public int? PulseRate { get; set; }
    public decimal? TemperatureF { get; set; }
    public int? RespiratoryRate { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? BMI { get; set; }

    // Section 7: Allergies (Critical Clinical Safety)
    public bool AllergyAssessmentConfirmed { get; set; }
    public bool HasKnownAllergies { get; set; }
    public string? AllergyDetails { get; set; }

    // Completion Tracking
    public int DossierCompletionPercentage { get; set; }
    public bool IsMandatoryDossierComplete { get; set; }
    public string MissingRequiredFieldsJson { get; set; } = "[]";
    public Guid? LastRecordedByStaffId { get; set; }

    /// <summary>
    /// DPDP Act 2023 compliance: Mask identifiable personal details while retaining clinical totality.
    /// </summary>
    public void Anonymize()
    {
        Address = "Redacted";
        Email = null;
        EmergencyContactName = "Redacted";
        EmergencyContactPhone = "0000000000";
        EmergencyContactRelation = null;
        PlaceOfBirth = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
