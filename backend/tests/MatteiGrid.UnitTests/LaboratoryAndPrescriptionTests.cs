using FluentAssertions;
using MatteiGrid.Modules.Laboratory.Domain;
using MatteiGrid.Modules.Patients.Domain;
using MatteiGrid.Modules.Prescriptions.Domain;
using Xunit;

namespace MatteiGrid.UnitTests;

public class LaboratoryAndPrescriptionTests
{
    [Fact]
    public void LaboratoryOrder_Rejection_RequiresMandatoryComment()
    {
        var order = new LaboratoryOrder
        {
            Status = LaboratoryOrderStatus.Preparing
        };

        var act = () => order.Reject("");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Rejection comment is mandatory*");
    }

    [Fact]
    public void LaboratoryOrder_ValidRejection_SetsStatusAndComment()
    {
        var order = new LaboratoryOrder
        {
            Status = LaboratoryOrderStatus.Preparing
        };

        order.Reject("Insufficient dilution vehicle available");

        order.Status.Should().Be(LaboratoryOrderStatus.Rejected);
        order.RejectionComment.Should().Be("Insufficient dilution vehicle available");
    }

    [Fact]
    public void LaboratoryOrder_ReadyOrder_CannotBeRejected()
    {
        var order = new LaboratoryOrder
        {
            Status = LaboratoryOrderStatus.Ready
        };

        var act = () => order.Reject("Late rejection");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot be rejected*");
    }

    [Theory]
    [InlineData("Red")]
    [InlineData("Blue")]
    [InlineData("Green")]
    [InlineData("Yellow")]
    [InlineData("White")]
    public void Electricities_ValidPolarities_AreAccepted(string polarity)
    {
        Electricities.All.Should().Contain(polarity);
    }

    [Fact]
    public void PrescriptionItem_SelectionMethod_DefaultsToRepertorialAndAllowsNonRepertorial()
    {
        var item1 = new PrescriptionItem();
        item1.SelectionMethod.Should().Be(PrescriptionSelectionMethod.Repertorial);
        item1.Frequency.Should().Be("TDS");

        var item2 = new PrescriptionItem
        {
            SelectionMethod = PrescriptionSelectionMethod.NonRepertorial,
            Frequency = "Hourly",
            Instructions = "Take with warm water"
        };
        item2.SelectionMethod.Should().Be(PrescriptionSelectionMethod.NonRepertorial);
        item2.Frequency.Should().Be("Hourly");
        item2.Instructions.Should().Be("Take with warm water");
    }

    [Fact]
    public void Prescription_DietAndRegimenInstructions_SetsCorrectly()
    {
        var rx = new Prescription
        {
            DietAndRegimenInstructions = "Avoid coffee, raw onions, and camphor.",
            GeneralInstructions = "Take 30 mins before meals on empty stomach."
        };

        rx.DietAndRegimenInstructions.Should().Contain("Avoid coffee");
        rx.GeneralInstructions.Should().Contain("empty stomach");
    }
}

public class OrganonConsultationTests
{
    [Fact]
    public void Consultation_Defaults_And_Observation_SupportOrganonPage6()
    {
        var consultation = new MatteiGrid.Modules.Clinical.Domain.Consultation
        {
            PatientId = Guid.NewGuid(),
            DoctorStaffId = Guid.NewGuid(),
            CaseType = MatteiGrid.Modules.Clinical.Domain.CaseTypes.Acute,
            ProvisionalDiagnosis = "Acute Bronchial Spasm",
            FinalDiagnosis = "Asthmatic Bronchitis",
            Outcome = MatteiGrid.Modules.Clinical.Domain.CaseOutcomes.Relieved
        };

        consultation.CaseType.Should().Be(MatteiGrid.Modules.Clinical.Domain.CaseTypes.Acute);
        consultation.Outcome.Should().Be(MatteiGrid.Modules.Clinical.Domain.CaseOutcomes.Relieved);

        var observation = new MatteiGrid.Modules.Clinical.Domain.ConsultationObservation
        {
            ConsultationId = consultation.Id,
            StaffId = consultation.DoctorStaffId,
            ReactionType = MatteiGrid.Modules.Clinical.Domain.ReactionTypes.Amelioration,
            KentsObservation = "Obs 3: Aggravation quick, short, strong with rapid improvement",
            SecondPrescriptionAction = MatteiGrid.Modules.Clinical.Domain.SecondPrescriptionActions.WaitAndWatch,
            Notes = "Cough intensity reduced significantly. Sleeping better."
        };

        consultation.Observations.Add(observation);

        consultation.Observations.Should().HaveCount(1);
        consultation.Observations.First().ReactionType.Should().Be("Amelioration");
        consultation.Observations.First().SecondPrescriptionAction.Should().Be("WaitAndWatch");
    }
}

public class DpdpPatientTests
{
    [Fact]
    public void Patient_AnonymizePii_ReplacesIdentifiableDataInPlace()
    {
        var patient = new Patient
        {
            Name = "John Doe",
            Phone = "+919876543210",
            DOB = new DateOnly(1985, 5, 20)
        };

        patient.AnonymizePii();

        patient.IsAnonymized.Should().BeTrue();
        patient.Name.Should().StartWith("Anonymized-Patient-");
        patient.Phone.Should().Be("0000000000");
        patient.DOB.Should().Be(new DateOnly(1900, 1, 1));
    }
}
