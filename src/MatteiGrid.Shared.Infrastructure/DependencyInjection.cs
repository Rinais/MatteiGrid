using MatteiGrid.Shared.Infrastructure.Audit;
using MatteiGrid.Shared.Infrastructure.Persistence;
using MatteiGrid.Shared.Infrastructure.Tenancy;
using MatteiGrid.Shared.Kernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MatteiGrid.Shared.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICurrentTenantContext, CurrentTenantContext>();
        services.AddScoped<TenantSaveChangesInterceptor>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IIntegrationEventPublisher, InMemoryIntegrationEventPublisher>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
            options.AddInterceptors(sp.GetRequiredService<TenantSaveChangesInterceptor>());
        });

        return services;
    }
}

internal sealed class InMemoryIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IServiceProvider _serviceProvider;

    public InMemoryIntegrationEventPublisher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        var handlers = _serviceProvider.GetServices<IIntegrationEventHandler<TEvent>>();
        foreach (var handler in handlers)
        {
            await handler.HandleAsync(integrationEvent, cancellationToken);
        }
    }
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIntegrationEventHandler<TEvent, THandler>(this IServiceCollection services)
        where TEvent : IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        services.AddScoped<IIntegrationEventHandler<TEvent>, THandler>();
        return services;
    }
}
