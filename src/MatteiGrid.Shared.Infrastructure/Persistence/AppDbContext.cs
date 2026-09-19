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
        modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
            _tenantContext.ClinicId == null || e.ClinicId == _tenantContext.ClinicId);
    }
}
