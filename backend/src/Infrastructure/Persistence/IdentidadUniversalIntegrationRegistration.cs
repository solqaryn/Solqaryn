using Microsoft.Extensions.DependencyInjection;
using Solqaryn.Application.Security;

namespace Solqaryn.Infrastructure.Persistence;

/// <summary>
/// C01-01.M03.05: integra la fuente EF y los servicios de identidad universal.
/// La vida Scoped evita reutilizar DbContext o membresias entre solicitudes.
/// No registra jobs, proveedores remotos, colas ni nuevos endpoints.
/// </summary>
public static class IdentidadUniversalIntegrationRegistration
{
    public static IServiceCollection AddIdentidadUniversalIntegration(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IIdentidadUniversalSnapshotSource, IdentidadUniversalSnapshotEfSource>();
        services.AddScoped<IdentidadUniversalApplicationService>();
        services.AddScoped<IdentidadUniversalAuthorizationGuard>();
        return services;
    }
}
