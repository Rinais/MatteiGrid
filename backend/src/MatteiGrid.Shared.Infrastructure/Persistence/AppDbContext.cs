using System.Reflection;
using MatteiGrid.Shared.Infrastructure.Audit;
using MatteiGrid.Shared.Infrastructure.Tenancy;
using MatteiGrid.Shared.Kernel;
using Microsoft.EntityFrameworkCore;

namespace MatteiGrid.Shared.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly ICurrentTenantContext _tenantContext;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentTenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");

        // Ensure all MatteiGrid module assemblies from the binary directory are loaded into the AppDomain
        var loadedNames = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetName().Name)
            .Where(n => n != null)
            .ToHashSet();

        var binPath = AppContext.BaseDirectory;
        if (Directory.Exists(binPath))
        {
            foreach (var dllPath in Directory.GetFiles(binPath, "MatteiGrid.Modules.*.dll"))
            {
                var asmName = Path.GetFileNameWithoutExtension(dllPath);
                if (!loadedNames.Contains(asmName))
                {
                    try
                    {
                        Assembly.LoadFrom(dllPath);
                    }
                    catch
                    {
                        // Silently ignore if already loaded or unavailable
                    }
                }
            }
        }

        // Apply configurations from Infrastructure assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Apply configurations from all loaded MatteiGrid module assemblies
        var matteiGridAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name != null && a.GetName().Name!.StartsWith("MatteiGrid"))
            .Distinct();

        foreach (var assembly in matteiGridAssemblies)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        }

        // Configure tenant query filters dynamically for all IClinicScopedEntity types
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IClinicScopedEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(AppDbContext)
                    .GetMethod(nameof(ConfigureTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(entityType.ClrType);
                method.Invoke(this, new object[] { modelBuilder });
            }
        }
    }

    private void ConfigureTenantFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, IClinicScopedEntity
    {
        // AUTH-02: Strict tenant isolation - when tenant context has no ClinicId, NO records match.
        // Never allow null tenant context to evaluate to true.
        // Note: Use ?? Guid.Empty instead of .Value so EF parameter extraction does not throw when ClinicId is null.
        modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
            _tenantContext.ClinicId.HasValue && e.ClinicId == (_tenantContext.ClinicId ?? Guid.Empty));
    }

    /// <summary>
    /// Defense-in-depth: Generates PostgreSQL Row Level Security (RLS) SQL statements
    /// enforcing tenant isolation at the database engine level (AUTH-02).
    /// </summary>
    public static string GeneratePostgresRlsScript(string schema, string tableName)
    {
        return $@"
ALTER TABLE {schema}.{tableName} ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation_policy ON {schema}.{tableName};
CREATE POLICY tenant_isolation_policy ON {schema}.{tableName}
    AS RESTRICTIVE
    USING (clinic_id = NULLIF(current_setting('app.current_clinic_id', true), '')::uuid)
    WITH CHECK (clinic_id = NULLIF(current_setting('app.current_clinic_id', true), '')::uuid);
";
    }
}
