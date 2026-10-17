using FluentAssertions;
using MatteiGrid.Modules.Identity.Contracts;
using MatteiGrid.Modules.Identity.Domain;
using MatteiGrid.Modules.Identity.Services;
using MatteiGrid.Modules.Patients.Domain;
using MatteiGrid.Shared.Infrastructure.Audit;
using MatteiGrid.Shared.Infrastructure.Persistence;
using MatteiGrid.Shared.Infrastructure.Tenancy;
using MatteiGrid.Shared.Kernel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace MatteiGrid.IntegrationTests;

public class MultiTenantSecurityTests
{
    private static AppDbContext CreateDbContext(ICurrentTenantContext tenantContext, string dbName)
    {
        var interceptor = new TenantSaveChangesInterceptor(tenantContext);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(interceptor)
            .Options;

        return new AppDbContext(options, tenantContext);
    }

    [Fact]
    public async Task CrossTenant_GlobalQueryFilter_ShouldIsolateData_BetweenClinics()
    {
        // Arrange (AUTH-02: Strict Tenant Query Isolation)
        var dbName = Guid.NewGuid().ToString();

        var clinicA = Guid.NewGuid();
        var clinicB = Guid.NewGuid();

        var tenantContextA = new CurrentTenantContext();
        tenantContextA.SetTenant(clinicA, Guid.NewGuid(), Array.Empty<string>());

        var tenantContextB = new CurrentTenantContext();
        tenantContextB.SetTenant(clinicB, Guid.NewGuid(), Array.Empty<string>());

        var nullTenantContext = new CurrentTenantContext();

        // 1. Seed Patient for Clinic A
        using (var dbA = CreateDbContext(tenantContextA, dbName))
        {
            dbA.Set<Patient>().Add(new Patient
            {
                Id = Guid.NewGuid(),
                ClinicId = clinicA,
                Name = "Clinic A Patient",
                Phone = "+919000000001",
                DOB = new DateOnly(1990, 1, 1),
                CreatedAt = DateTimeOffset.UtcNow
            });
            await dbA.SaveChangesAsync();
        }

        // 2. Seed Patient for Clinic B
        using (var dbB = CreateDbContext(tenantContextB, dbName))
        {
            dbB.Set<Patient>().Add(new Patient
            {
                Id = Guid.NewGuid(),
                ClinicId = clinicB,
                Name = "Clinic B Patient",
                Phone = "+919000000002",
                DOB = new DateOnly(1992, 2, 2),
                CreatedAt = DateTimeOffset.UtcNow
            });
            await dbB.SaveChangesAsync();
        }

        // Act & Assert 1: Clinic A context only sees Clinic A's patient
        using (var dbA = CreateDbContext(tenantContextA, dbName))
        {
            var patientsA = await dbA.Set<Patient>().ToListAsync();
            patientsA.Should().HaveCount(1);
            patientsA[0].Name.Should().Be("Clinic A Patient");
        }

        // Act & Assert 2: Clinic B context only sees Clinic B's patient
        using (var dbB = CreateDbContext(tenantContextB, dbName))
        {
            var patientsB = await dbB.Set<Patient>().ToListAsync();
            patientsB.Should().HaveCount(1);
            patientsB[0].Name.Should().Be("Clinic B Patient");
        }

        // Act & Assert 3: Null/Unauthenticated context sees ZERO patients (No leakage when context is empty!)
        using (var dbNull = CreateDbContext(nullTenantContext, dbName))
        {
            var patientsEmpty = await dbNull.Set<Patient>().ToListAsync();
            patientsEmpty.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task CrossTenant_SavingEntityWithMismatchedClinicId_ShouldThrowException()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var clinicA = Guid.NewGuid();
        var clinicB = Guid.NewGuid();

        var tenantContextA = new CurrentTenantContext();
        tenantContextA.SetTenant(clinicA, Guid.NewGuid(), Array.Empty<string>());

        using var db = CreateDbContext(tenantContextA, dbName);

        // Act: Attempt to save an entity belonging to Clinic B under Clinic A tenant context
        var maliciousPatient = new Patient
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicB, // Mismatched!
            Name = "Injected Patient",
            Phone = "+919999999999",
            DOB = new DateOnly(1985, 5, 5),
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Set<Patient>().Add(maliciousPatient);

        // Assert (AUTH-02: Cross-tenant creation blocked at interceptor level)
        var act = async () => await db.SaveChangesAsync();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cross-tenant write attempt detected*");
    }

    [Fact]
    public async Task AuditLog_ShouldBeAppendOnly_ModificationThrowsException()
    {
        // Arrange (AUTH-06: Append-only compliance for audit logs)
        var dbName = Guid.NewGuid().ToString();
        var clinicId = Guid.NewGuid();

        var tenantContext = new CurrentTenantContext();
        tenantContext.SetTenant(clinicId, Guid.NewGuid(), Array.Empty<string>());

        Guid logId;
        using (var db = CreateDbContext(tenantContext, dbName))
        {
            var log = new AuditLog
            {
                ClinicId = clinicId,
                Action = "PatientRecordViewed",
                EntityType = "Patient",
                Details = "Initial log"
            };
            db.AuditLogs.Add(log);
            await db.SaveChangesAsync();
            logId = log.Id;
        }

        // Act: Attempt to modify audit log
        using (var db = CreateDbContext(tenantContext, dbName))
        {
            var existingLog = await db.AuditLogs.FirstAsync(l => l.Id == logId);
            existingLog.Details = "Tampered details";

            var act = async () => await db.SaveChangesAsync();
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Audit logs are append-only*");
        }
    }

    [Fact]
    public async Task RefreshToken_ReuseDetection_ShouldRevoke_EntireTokenFamily()
    {
        // Arrange (AUTH-04: Refresh Token Reuse Detection & Family Revocation)
        var dbName = Guid.NewGuid().ToString();
        var clinicId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var email = "doctor@matteigrid.com";

        var tenantContext = new CurrentTenantContext();
        tenantContext.SetTenant(clinicId, userId, Roles.Doctor, Permissions.RolePermissions[Roles.Doctor]);

        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "SuperSecretKeyForTestingRefreshTokenRotation12345!",
            ["Jwt:Issuer"] = "MatteiGridApi",
            ["Jwt:Audience"] = "MatteiGridClients"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var jwtService = new JwtTokenService(config);

        using var db = CreateDbContext(tenantContext, dbName);
        var auditMock = new Mock<IAuditService>();

        // Set up user in db
        var user = new ApplicationUser
        {
            Id = userId,
            Email = email,
            UserName = email,
            ClinicId = clinicId,
            IsActive = true
        };

        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        var userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        userManagerMock.Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(new[] { Roles.Doctor });

        var roleStoreMock = new Mock<IRoleStore<IdentityRole<Guid>>>();
        var roleManagerMock = new Mock<RoleManager<IdentityRole<Guid>>>(
            roleStoreMock.Object, null!, null!, null!, null!);

        var identityModule = new IdentityModule(
            userManagerMock.Object,
            roleManagerMock.Object,
            db,
            jwtService,
            tenantContext,
            auditMock.Object);

        // Step 1: Initial refresh token created in family
        var initialRawRefreshToken = jwtService.GenerateRefreshToken();
        var familyId = Guid.NewGuid();
        db.Set<RefreshToken>().Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ClinicId = clinicId,
            FamilyId = familyId,
            TokenHash = jwtService.HashToken(initialRawRefreshToken),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        });
        await db.SaveChangesAsync();

        // Step 2: Legitimate refresh - Token 1 rotated to Token 2
        var rotateResponse = await identityModule.RefreshAsync(new RefreshRequest(initialRawRefreshToken));
        rotateResponse.Should().NotBeNull();
        var secondRawRefreshToken = rotateResponse!.RefreshToken;

        // Verify Token 1 is revoked and Token 2 is active
        var tokensAfterRotate = await db.Set<RefreshToken>().Where(t => t.FamilyId == familyId).ToListAsync();
        tokensAfterRotate.Should().HaveCount(2);

        // Step 3: Attacker replays the compromised Token 1!
        var replayResponse = await identityModule.RefreshAsync(new RefreshRequest(initialRawRefreshToken));

        // Assert: Replay must fail and return null
        replayResponse.Should().BeNull();

        // Assert: ALL tokens in the entire family must now be revoked!
        var allTokensInFamily = await db.Set<RefreshToken>().Where(t => t.FamilyId == familyId).ToListAsync();
        allTokensInFamily.All(t => t.RevokedAt != null).Should().BeTrue();

        // Assert: Security event logged
        auditMock.Verify(a => a.LogSecurityEventAsync(
            AuditEventTypes.AuthTokenRevokedReuse,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Guid?>(),
            It.IsAny<string?>(),
            null,
            clinicId,
            userId.ToString(),
            null,
            default), Times.Once);
    }

    [Fact]
    public async Task SuperAdmin_BreakGlass_WithValidJustification_IssuesEmergencyTokenAndAudits()
    {
        // Arrange (AUTH-05: Break-glass emergency access protocol)
        var dbName = Guid.NewGuid().ToString();
        var targetClinicId = Guid.NewGuid();
        var superAdminId = Guid.NewGuid();
        var superAdminEmail = "superadmin@matteigrid.com";

        var tenantContext = new CurrentTenantContext();
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "SuperSecretKeyForTestingSuperAdminBreakGlass12345!",
            ["Jwt:Issuer"] = "MatteiGridApi",
            ["Jwt:Audience"] = "MatteiGridClients"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var jwtService = new JwtTokenService(config);

        using var db = CreateDbContext(tenantContext, dbName);
        var auditMock = new Mock<IAuditService>();

        var superAdminUser = new ApplicationUser
        {
            Id = superAdminId,
            Email = superAdminEmail,
            UserName = superAdminEmail,
            IsActive = true
        };

        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        var userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        userManagerMock.Setup(m => m.FindByIdAsync(superAdminId.ToString()))
            .ReturnsAsync(superAdminUser);
        userManagerMock.Setup(m => m.GetRolesAsync(superAdminUser))
            .ReturnsAsync(new[] { Roles.SuperAdmin });

        var roleStoreMock = new Mock<IRoleStore<IdentityRole<Guid>>>();
        var roleManagerMock = new Mock<RoleManager<IdentityRole<Guid>>>(
            roleStoreMock.Object, null!, null!, null!, null!);

        var identityModule = new IdentityModule(
            userManagerMock.Object,
            roleManagerMock.Object,
            db,
            jwtService,
            tenantContext,
            auditMock.Object);

        var breakGlassRequest = new BreakGlassRequest(
            ClinicId: targetClinicId,
            Reason: "Emergency incident INC-9812 - Data corruption recovery requested by clinic admin",
            TicketNumber: "INC-9812",
            DurationMinutes: 60);

        // Act
        var result = await identityModule.InitiateBreakGlassAsync(superAdminId, breakGlassRequest);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.ClinicId.Should().Be(targetClinicId);
        result.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);

        // Verify session was persisted in DB
        var session = await db.Set<BreakGlassSession>().FirstOrDefaultAsync(s => s.Id == result.SessionId);
        session.Should().NotBeNull();
        session!.Reason.Should().Be(breakGlassRequest.Reason);
        session.TicketNumber.Should().Be("INC-9812");

        // Verify audit event logged with SuperAdminBreakGlass type
        auditMock.Verify(a => a.LogSecurityEventAsync(
            AuditEventTypes.SuperAdminBreakGlass,
            "BreakGlassInitiated",
            "BreakGlassSession",
            result.SessionId,
            It.Is<string>(s => s.Contains("EMERGENCY BREAK-GLASS") && s.Contains("INC-9812")),
            null,
            targetClinicId,
            superAdminId.ToString(),
            superAdminEmail,
            default), Times.Once);
    }

    [Fact]
    public async Task SuperAdmin_BreakGlass_WithInsufficientReason_ThrowsArgumentException()
    {
        // Arrange (AUTH-05: Enforce min 20 char justification)
        var dbName = Guid.NewGuid().ToString();
        var targetClinicId = Guid.NewGuid();
        var superAdminId = Guid.NewGuid();

        var tenantContext = new CurrentTenantContext();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "SuperSecretKeyForTestingSuperAdminBreakGlass12345!",
            ["Jwt:Issuer"] = "MatteiGridApi",
            ["Jwt:Audience"] = "MatteiGridClients"
        }).Build();
        var jwtService = new JwtTokenService(config);

        using var db = CreateDbContext(tenantContext, dbName);
        var auditMock = new Mock<IAuditService>();

        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        var userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var roleStoreMock = new Mock<IRoleStore<IdentityRole<Guid>>>();
        var roleManagerMock = new Mock<RoleManager<IdentityRole<Guid>>>(
            roleStoreMock.Object, null!, null!, null!, null!);

        var identityModule = new IdentityModule(
            userManagerMock.Object,
            roleManagerMock.Object,
            db,
            jwtService,
            tenantContext,
            auditMock.Object);

        var shortReasonRequest = new BreakGlassRequest(
            ClinicId: targetClinicId,
            Reason: "too short",
            TicketNumber: "INC-1",
            DurationMinutes: 30);

        // Act & Assert
        var act = async () => await identityModule.InitiateBreakGlassAsync(superAdminId, shortReasonRequest);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Break-glass justification must be at least 20 characters.*");
    }

    [Fact]
    public async Task SuperAdmin_BreakGlass_ExceedingMaxDuration_ThrowsArgumentOutOfRangeException()
    {
        // Arrange (AUTH-05: Enforce max 120 minutes time-box)
        var dbName = Guid.NewGuid().ToString();
        var targetClinicId = Guid.NewGuid();
        var superAdminId = Guid.NewGuid();

        var tenantContext = new CurrentTenantContext();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "SuperSecretKeyForTestingSuperAdminBreakGlass12345!",
            ["Jwt:Issuer"] = "MatteiGridApi",
            ["Jwt:Audience"] = "MatteiGridClients"
        }).Build();
        var jwtService = new JwtTokenService(config);

        using var db = CreateDbContext(tenantContext, dbName);
        var auditMock = new Mock<IAuditService>();

        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        var userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var roleStoreMock = new Mock<IRoleStore<IdentityRole<Guid>>>();
        var roleManagerMock = new Mock<RoleManager<IdentityRole<Guid>>>(
            roleStoreMock.Object, null!, null!, null!, null!);

        var identityModule = new IdentityModule(
            userManagerMock.Object,
            roleManagerMock.Object,
            db,
            jwtService,
            tenantContext,
            auditMock.Object);

        var excessiveDurationRequest = new BreakGlassRequest(
            ClinicId: targetClinicId,
            Reason: "Legitimate justification but exceeds maximum time-box window allowed",
            TicketNumber: "INC-2000",
            DurationMinutes: 180);

        // Act & Assert
        var act = async () => await identityModule.InitiateBreakGlassAsync(superAdminId, excessiveDurationRequest);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*Break-glass duration must be between 1 and 120 minutes.*");
    }
}
