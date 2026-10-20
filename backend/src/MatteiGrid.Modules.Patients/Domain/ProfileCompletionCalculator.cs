using System.Text.Json;

namespace MatteiGrid.Modules.Patients.Domain;

public record ProfileCompletionResult(
    int Score,
    bool IsComplete,
    string Status,
    IReadOnlyList<string> MissingFields
);

public static class ProfileCompletionCalculator
{
    private static readonly (string Code, string Label, Func<Patient, HomeopathicCaseRecord?, bool> IsSatisfied, int Weight, bool IsMandatory)[] Rules =
    {
        // Tier 1: Quick Intake Baseline (Weight: 40%)
        ("Quick.Name", "Full Name", (p, _) => !string.IsNullOrWhiteSpace(p.Name), 10, true),
        ("Quick.Phone", "Contact Phone", (p, _) => !string.IsNullOrWhiteSpace(p.Phone) && p.Phone.Length >= 10, 10, true),
        ("Quick.DOB", "Date of Birth", (p, _) => p.DOB != default && p.DOB < DateOnly.FromDateTime(DateTime.UtcNow), 10, true),
        ("Quick.Gender", "Gender", (p, _) => !string.IsNullOrWhiteSpace(p.Gender) && !string.Equals(p.Gender, "Undisclosed", StringComparison.OrdinalIgnoreCase), 5, true),
        ("Quick.Complaint", "Chief Complaint", (p, _) => !string.IsNullOrWhiteSpace(p.PrimaryChiefComplaint), 5, true),

        // Tier 2: Detailed Case Dossier Baseline (Weight: 60%)
        ("Case.Onset", "Chief Complaint Timeline / Onset", (_, r) => !string.IsNullOrWhiteSpace(r?.OnsetTimeline), 12, true),
        ("Case.Thermal", "Thermals", (_, r) => !string.IsNullOrWhiteSpace(r?.Thermals ?? r?.ThermalState), 12, true),
        ("Case.ThirstAppetite", "Appetite & Thirst Characteristics", (_, r) => !string.IsNullOrWhiteSpace(r?.Appetite) || !string.IsNullOrWhiteSpace(r?.Thirst), 12, true),
        ("Case.Sleep", "Sleep Quality & Rest Patterns", (_, r) => !string.IsNullOrWhiteSpace(r?.Sleep ?? r?.SleepPattern), 12, true),
        ("Case.Allergies", "Known Drug / Substance Allergies", (_, r) => r?.AllergyAssessmentConfirmed == true, 12, true)
    };

    public static ProfileCompletionResult Calculate(Patient patient, HomeopathicCaseRecord? caseRecord)
    {
        var missingMandatory = new List<string>();
        int score = 0;

        foreach (var (code, label, isSatisfied, weight, isMandatory) in Rules)
        {
            if (isSatisfied(patient, caseRecord))
            {
                score += weight;
            }
            else if (isMandatory)
            {
                missingMandatory.Add(label);
            }
        }

        // Clamp score between 0 and 100
        score = Math.Clamp(score, 0, 100);
        bool isComplete = missingMandatory.Count == 0;

        return new ProfileCompletionResult(
            Score: score,
            IsComplete: isComplete,
            Status: isComplete ? "Complete" : "Incomplete",
            MissingFields: missingMandatory
        );
    }

    public static string SerializeMissingFields(IReadOnlyList<string> missingFields)
    {
        return JsonSerializer.Serialize(missingFields);
    }

    public static IReadOnlyList<string> DeserializeMissingFields(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}
