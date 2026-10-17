using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MatteiGrid.Shared.Infrastructure.Logging;

public static class ServiceLoggingExtensions
{
    /// <summary>
    /// Registers a scoped service interface wrapped with an AOP ServiceLoggingInterceptor,
    /// enabling automated method entry and exit tracing, timing, and error logging.
    /// </summary>
    public static IServiceCollection AddLoggedScoped<TInterface, TImplementation>(this IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        services.TryAddSingleton<ProxyGenerator>();
        services.TryAddScoped<ServiceLoggingInterceptor>();
        services.AddScoped<TImplementation>();
        services.AddScoped<TInterface>(sp =>
        {
            var proxyGenerator = sp.GetRequiredService<ProxyGenerator>();
            var actual = sp.GetRequiredService<TImplementation>();
            var interceptor = sp.GetRequiredService<ServiceLoggingInterceptor>();
            return proxyGenerator.CreateInterfaceProxyWithTarget<TInterface>(actual, interceptor);
        });

        return services;
    }
}
