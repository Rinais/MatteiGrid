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

    [Fact]
    public void All_Permissions_ShouldFollow_ResourceActionFormat()
    {
        foreach (var permission in Permissions.All)
        {
            permission.Should().Contain(".", "All permissions must follow Resource.Action naming per AUTH-09");
            var parts = permission.Split('.');
            parts.Length.Should().Be(2);
            parts[0].Should().NotBeNullOrWhiteSpace();
            parts[1].Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void All_RolePermissions_ShouldOnlyReference_KnownPermissions()
    {
        var allKnown = new HashSet<string>(Permissions.All);
        foreach (var (role, perms) in Permissions.RolePermissions)
        {
            foreach (var perm in perms)
            {
                allKnown.Should().Contain(perm, $"Role '{role}' references unknown permission '{perm}'");
            }
        }
    }

    [Fact]
    public void SuperAdmin_DefaultRole_ShouldNotContain_ClinicalDataPermissions()
    {
        // AUTH-05: SuperAdmin must not have implicit clinical permissions
        var superAdminPerms = Permissions.RolePermissions[Roles.SuperAdmin];
        superAdminPerms.Should().NotContain(Permissions.PatientsRead);
        superAdminPerms.Should().NotContain(Permissions.ConsultationsRead);
        superAdminPerms.Should().NotContain(Permissions.PrescriptionsRead);
        superAdminPerms.Should().NotContain(Permissions.PrescriptionsCreate);
    }
}
