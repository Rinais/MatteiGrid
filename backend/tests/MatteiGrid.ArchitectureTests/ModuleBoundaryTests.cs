using FluentAssertions;
using MatteiGrid.Shared.Kernel;
using NetArchTest.Rules;
using Xunit;

namespace MatteiGrid.ArchitectureTests;

public class ModuleBoundaryTests
{
    private static readonly System.Reflection.Assembly SharedKernelAssembly = typeof(IClinicScopedEntity).Assembly;
    private static readonly System.Reflection.Assembly SharedInfrastructureAssembly = typeof(MatteiGrid.Shared.Infrastructure.Persistence.AppDbContext).Assembly;

    [Fact]
    public void SharedKernel_ShouldNotDependOn_AnyOtherProjects()
    {
        var result = Types.InAssembly(SharedKernelAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "MatteiGrid.Shared.Infrastructure",
                "MatteiGrid.Modules.Billing",
                "MatteiGrid.Modules.Clinical",
                "MatteiGrid.Modules.Prescriptions",
                "MatteiGrid.Modules.Laboratory",
                "MatteiGrid.Modules.Patients",
                "MatteiGrid.Modules.Appointments",
                "MatteiGrid.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void BillingModule_ShouldNotDirectlyDependOn_ClinicalModuleInternals()
    {
        var billingAssembly = typeof(MatteiGrid.Modules.Billing.Domain.Invoice).Assembly;

        var result = Types.InAssembly(billingAssembly)
            .ShouldNot()
            .HaveDependencyOn("MatteiGrid.Modules.Clinical")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void LaboratoryModule_ShouldNotDirectlyDependOn_PrescriptionModuleInternals()
    {
        var labAssembly = typeof(MatteiGrid.Modules.Laboratory.Domain.LaboratoryOrder).Assembly;

        var result = Types.InAssembly(labAssembly)
            .ShouldNot()
            .HaveDependencyOn("MatteiGrid.Modules.Prescriptions")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
