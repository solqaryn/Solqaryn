using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;

namespace Solqaryn.API.Security;

/// <summary>
/// C04-04.M03.03: DEV-only, read-only adapter to signed Bearer authentication,
/// current UsuarioEmpresa/RolPermiso grants and the real Factura EF mapping.
/// No payment execution, PSP, ledger writes or production registration.
/// </summary>
public sealed class C0404PaymentVerifiedEvidenceReader(
    IHttpContextAccessor contexts,
    ICurrentUserService current,
    IUsuarioScopeService scopes,
    IPermisoService permissions,
    AppDbContext database,
    IConfiguration configuration) : IC0404PaymentAuthorizationEvidenceReader
{
    public async ValueTask<C0404PaymentAuthorizationGate.TrustedSnapshot?> ReadCurrentAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Default OFF in every environment. The DI registration/route is also DEV-only.
        if (!configuration.GetValue<bool>("PaymentPlanning:EnableReadOnlyAuthorizationProbe"))
            return null;

        var http = contexts.HttpContext;
        if (http is null || http.User.Identity?.IsAuthenticated != true ||
            !current.EstaAutenticado || current.UsuarioId is not > 0 ||
            !int.TryParse(Convert.ToString(http.Request.RouteValues["tenantId"]), out var tenantId) ||
            !int.TryParse(Convert.ToString(http.Request.RouteValues["invoiceId"]), out var invoiceId) ||
            tenantId <= 0 || invoiceId <= 0)
            return null;

        var verifiedBefore = VerifiedCommercialTenant.Resolve(http);
        if (verifiedBefore.HasValue && verifiedBefore.Value != tenantId)
            return null;

        try
        {
            // A ClaimsPrincipal alone is not authentication. The registered Bearer
            // handler must validate the signature, issuer, audience and lifetime.
            var bearer = await http.AuthenticateAsync("Bearer");
            cancellationToken.ThrowIfCancellationRequested();
            if (!bearer.Succeeded || bearer.Principal?.Identity?.IsAuthenticated != true ||
                !int.TryParse(bearer.Principal.FindFirstValue("id"), out var signedActor) ||
                signedActor != current.UsuarioId.Value)
                return null;

            // Role/membership are freshly resolved from actual UsuarioEmpresa,
            // Usuario, Empresa and Rol records, never claims or client headers.
            var scope = await scopes.ObtenerActualAsync(tenantId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (scope is null || scope.UsuarioId != signedActor ||
                scope.EmpresaId != tenantId || scope.RolId <= 0)
                return null;

            // IPermisoService uses the current RolPermiso grant. Revocation
            // must take effect on the next request; no admin-claim shortcut.
            if (!await permissions.TienePermisoAsync(
                    tenantId, ModuloSistema.Facturacion, AccionPermiso.Compartir))
                return null;
            cancellationToken.ThrowIfCancellationRequested();

            var marker = VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey;
            var hadMarker = http.Items.ContainsKey(marker);
            if (!hadMarker)
                http.Items[marker] = tenantId; // only after verified server-side RBAC
            try
            {
                var owned = await database.Facturas.AsNoTracking()
                    .AnyAsync(f => f.Id == invoiceId && f.EmpresaId == tenantId,
                        cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (!owned)
                    return null;

                // The invoice lookup is asynchronous: a real permission or
                // membership revocation may commit while it is running.
                // Never reuse the pre-lookup authorization snapshot afterwards.
                var scopeAfterInvoice = await scopes.ObtenerActualAsync(
                    tenantId, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (scopeAfterInvoice is null ||
                    scopeAfterInvoice.UsuarioId != signedActor ||
                    scopeAfterInvoice.EmpresaId != tenantId ||
                    scopeAfterInvoice.RolId != scope.RolId)
                    return null;
                if (!await permissions.TienePermisoAsync(
                        tenantId, ModuloSistema.Facturacion, AccionPermiso.Compartir))
                    return null;
                cancellationToken.ThrowIfCancellationRequested();
            }
            finally
            {
                if (!hadMarker)
                    http.Items.Remove(marker);
            }

            return new C0404PaymentAuthorizationGate.TrustedSnapshot
            {
                ActorId = signedActor,
                TenantId = tenantId,
                InvoiceId = invoiceId,
                JwtSignatureIssuerAudienceLifetimeVerifiedByBackend = true,
                CurrentSessionVerified = true,
                UserActive = true,
                AuthorityDatabaseAvailable = true,
                MembershipFromPersistence = true,
                MembershipActive = true,
                RoleAndGrantFromPersistence = true,
                TenantReadFencedByDatabase = true,
                InvoiceOwnershipVerifiedFromPersistence = true,
                AuthorizationRevisionFresh = true,
                FeatureEnabledByTrustedConfig = true,
                CreateGrant = true,
                // A read-only preflight never certifies a refundable settlement.
                RefundGrant = false,
                ReceiptAndLedgerVersionVerified = false
            };
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Fail closed. Never reflect JWT, database errors or identity/PII.
            return null;
        }
    }
}
