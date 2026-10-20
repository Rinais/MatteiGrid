using FluentAssertions;
using MatteiGrid.Modules.Patients.Domain;
using Xunit;

namespace MatteiGrid.UnitTests;

public class PatientProgressiveProfilingUnitTests
{
    [Fact]
    public void ProfileCompletionCalculator_QuickIntakeOnly_ShouldBeIncomplete_With40Percent()
    {
        // Arrange: Patient registered with quick intake fields only
        var patient = new Patient
        {
            Name = "Anand Sharma",
            Phone = "9876543210",
            DOB = new DateOnly(1985, 6, 15),
            Gender = "Male",
            PrimaryChiefComplaint = "Chronic dry cough with nightly wheezing"
        };

        // Act
        var result = ProfileCompletionCalculator.Calculate(patient, null);

        // Assert
        result.IsComplete.Should().BeFalse();
        result.Status.Should().Be("Incomplete");
        result.Score.Should().Be(40);
        result.MissingFields.Should().Contain(new[]
        {
            "Chief Complaint Timeline / Onset",
            "Thermals",
            "Appetite & Thirst Characteristics",
            "Sleep Quality & Rest Patterns",
            "Known Drug / Substance Allergies"
        });
    }

    [Fact]
    public void ProfileCompletionCalculator_MissingQuickIntakeGender_ShouldDeductScore()
    {
        // Arrange: Patient registered without specifying gender
        var patient = new Patient
        {
            Name = "Anand Sharma",
            Phone = "9876543210",
            DOB = new DateOnly(1985, 6, 15),
            Gender = "Undisclosed",
            PrimaryChiefComplaint = "Chronic migraine"
        };

        // Act
        var result = ProfileCompletionCalculator.Calculate(patient, null);

        // Assert: 40 - 5 = 35
        result.Score.Should().Be(35);
        result.MissingFields.Should().Contain("Gender");
    }

    [Fact]
    public void ProfileCompletionCalculator_FullHomeopathicDossier_ShouldBeComplete_With100Percent()
    {
        // Arrange
        var patient = new Patient
        {
            Name = "Sunita Verma",
            Phone = "9123456780",
            DOB = new DateOnly(1992, 4, 10),
            Gender = "Female",
            PrimaryChiefComplaint = "Severe allergic rhinitis with morning sneezing"
        };

        var caseRecord = new HomeopathicCaseRecord
        {
            PatientId = patient.Id,
            OnsetTimeline = "Started 6 months ago after exposure to damp monsoon dust",
            ThermalState = "Chilly",
            Appetite = "Good, craves warm drinks and sweets",
            Thirst = "Thirstless, sips occasionally",
            SleepPattern = "Disturbed after 3 AM due to nasal obstruction",
            AllergyAssessmentConfirmed = true,
            HasKnownAllergies = true,
            AllergyDetails = "House dust mites, raw pollen"
        };

        // Act
        var result = ProfileCompletionCalculator.Calculate(patient, caseRecord);

        // Assert
        result.IsComplete.Should().BeTrue();
        result.Status.Should().Be("Complete");
        result.Score.Should().Be(100);
        result.MissingFields.Should().BeEmpty();
    }

    [Fact]
    public void ProfileCompletionCalculator_MissingAllergyAssessment_ShouldRemainIncomplete()
    {
        // Arrange: All generals filled except allergy confirmation
        var patient = new Patient
        {
            Name = "Sunita Verma",
            Phone = "9123456780",
            DOB = new DateOnly(1992, 4, 10),
            Gender = "Female",
            PrimaryChiefComplaint = "Eczema on flexures"
        };

        var caseRecord = new HomeopathicCaseRecord
        {
            PatientId = patient.Id,
            OnsetTimeline = "Gradual onset over 2 years",
            ThermalState = "Hot",
            Appetite = "Moderate",
            SleepPattern = "Sound 7 hours",
            AllergyAssessmentConfirmed = false // Crucial safety gap!
        };

        // Act
        var result = ProfileCompletionCalculator.Calculate(patient, caseRecord);

        // Assert
        result.IsComplete.Should().BeFalse();
        result.Status.Should().Be("Incomplete");
        result.Score.Should().Be(88); // 100 - 12 = 88
        result.MissingFields.Should().ContainSingle().Which.Should().Be("Known Drug / Substance Allergies");
    }

    [Fact]
    public void AnonymizePii_ShouldAnonymizePatientAndHomeopathicCaseRecord()
    {
        // Arrange
        var patient = new Patient
        {
            Name = "Rajesh Patel",
            Phone = "9845011223",
            DOB = new DateOnly(1975, 12, 1),
            Gender = "Male",
            PrimaryChiefComplaint = "Rheumatic joint pains"
        };

        var caseRecord = new HomeopathicCaseRecord
        {
            PatientId = patient.Id,
            Address = "124 Park View Lane, Bengaluru",
            Email = "rajesh.patel@sample.com",
            EmergencyContactName = "Kavita Patel",
            EmergencyContactPhone = "9845099887",
            EmergencyContactRelation = "Spouse",
            ThermalState = "Chilly"
        };
        patient.CaseRecord = caseRecord;

        // Act (DPDP Act 2023 Right to Erasure)
        patient.AnonymizePii();

        // Assert
        patient.IsAnonymized.Should().BeTrue();
        patient.Name.Should().StartWith("Anonymized-Patient-");
        patient.Phone.Should().Be("0000000000");
        patient.Gender.Should().Be("Undisclosed");
        patient.PrimaryChiefComplaint.Should().BeNull();

        caseRecord.Address.Should().Be("Redacted");
        caseRecord.Email.Should().BeNull();
        caseRecord.EmergencyContactName.Should().Be("Redacted");
        caseRecord.EmergencyContactPhone.Should().Be("0000000000");
        // Clinical homeopathy findings remain preserved for statutory medical records retention
        caseRecord.ThermalState.Should().Be("Chilly");
    }
}
