using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Rules = Solqaryn.Domain.AutomationV5.C0701CatalogDomainRules;
using Service = Solqaryn.Application.AutomationV5.C0701CatalogApplicationService;

namespace Solqaryn.API.Security;

/// <summary>
/// C07-01.M03.03: actor firmado y permisos/tenant relacionales actuales.
/// Adaptador READ-ONLY, DEV, opt-in OFF por defecto; sin concesion de lectura
/// de productos, escritura o mutacion. Ruta tenantId es selector, no autoridad.
/// </summary>
public sealed class C0701CatalogVerifiedActorSource(
    IHttpContextAccessor contexts,
    ICurrentUserService current,
    IUsuarioScopeService memberships,
    AppDbContext database,
    IConfiguration configuration) : IC0701VerifiedCatalogActorReader
{
    public async ValueTask<Service.VerifiedActor?> ReadCurrentAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!configuration.GetValue<bool>("CatalogAuthorization:EnableReadOnlyProbe"))
            return null;

        var http = contexts.HttpContext;
        var rawTenant = Convert.ToString(http?.Request.RouteValues["tenantId"], CultureInfo.InvariantCulture);
        if (http?.User.Identity?.IsAuthenticated != true ||
            !current.EstaAutenticado || current.UsuarioId is not > 0 ||
            !int.TryParse(rawTenant, NumberStyles.None,
                CultureInfo.InvariantCulture, out var tenantId) || tenantId <= 0 ||
            !string.Equals(rawTenant, tenantId.ToString(CultureInfo.InvariantCulture),
                StringComparison.Ordinal))
            return null;

        var marker = VerifiedCommercialTenant.Resolve(http);
        if (marker.HasValue && marker.Value != tenantId)
            return null;

        try
        {
            var jwt = await http.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
            cancellationToken.ThrowIfCancellationRequested();
            if (!jwt.Succeeded || jwt.Principal?.Identity?.IsAuthenticated != true)
                return null;

            var ids = jwt.Principal.FindAll("id").ToArray();
            if (ids.Length != 1 ||
                !int.TryParse(ids[0].Value, NumberStyles.None,
                    CultureInfo.InvariantCulture, out var actorId) ||
                actorId != current.UsuarioId.Value)
                return null;

            var scope = await memberships.ObtenerActualAsync(tenantId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (scope is null || scope.UsuarioId != actorId ||
                scope.EmpresaId != tenantId || scope.RolId <= 0)
                return null;

            async Task<bool> HasCurrentGrantAsync(int roleId) =>
                await (from grant in database.RolPermisos.IgnoreQueryFilters().AsNoTracking()
                       join permission in database.Permisos.IgnoreQueryFilters().AsNoTracking()
                           on grant.PermisoId equals permission.Id
                       where grant.RolId == roleId &&
                             permission.Ambito == AmbitoAutorizacion.Empresa &&
                             permission.Modulo == ModuloSistema.Productos &&
                             permission.Accion == AccionPermiso.Editar &&
                             permission.Activo && !permission.Eliminado
                       select grant.Id).AnyAsync(cancellationToken);

            if (!await HasCurrentGrantAsync(scope.RolId))
                return null;
            cancellationToken.ThrowIfCancellationRequested();

            var fresh = await memberships.ObtenerActualAsync(tenantId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (fresh is null || fresh.UsuarioId != actorId ||
                fresh.EmpresaId != tenantId || fresh.RolId != scope.RolId ||
                !await HasCurrentGrantAsync(fresh.RolId))
                return null;
            cancellationToken.ThrowIfCancellationRequested();

            return new Service.VerifiedActor(
                new Rules.VerifiedServerScope(
                    tenantId, actorId, fresh.RolId, null,
                    true, true, true, true, false),
                true, true, true, true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Nunca reflejar SQL, claims o datos privados.
            return null;
        }
    }
}
