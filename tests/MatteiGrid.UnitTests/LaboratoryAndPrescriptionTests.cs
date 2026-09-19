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
