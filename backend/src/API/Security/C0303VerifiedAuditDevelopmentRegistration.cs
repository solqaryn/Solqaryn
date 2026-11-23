using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Solqaryn.Application.AutomationV5;

namespace Solqaryn.API.Security;

/// <summary>
/// C03-03.M03.03: diagnostic audit preflight must not be registered outside
/// Development. This composition boundary never grants read or write access.
/// </summary>
public static class C0303VerifiedAuditDevelopmentRegistration
{
    public static IServiceCollection AddC0303VerifiedAuditDevelopmentOnly(
        this IServiceCollection services, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(environment);
        if (!environment.IsDevelopment())
            return services;

        services.AddScoped<C0303VerifiedAuditTenantReaders>();
        services.AddScoped<IC0303VerifiedAuditEventReader>(
            sp => sp.GetRequiredService<C0303VerifiedAuditTenantReaders>());
        services.AddScoped<IC0303VerifiedAuditResourceReader>(
            sp => sp.GetRequiredService<C0303VerifiedAuditTenantReaders>());
        services.AddScoped<C0303SecurityAuditApplicationService>();
        // M03.04: isolated MySQL transactional writer, no production registration.
        services.AddScoped<C0303TransactionalAuditPersistenceAdapter>();
        return services;
    }
}
