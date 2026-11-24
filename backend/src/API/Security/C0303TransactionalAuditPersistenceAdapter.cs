using System.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Rules = Solqaryn.Domain.Security.C0303SecurityAuditDomainRules;

namespace Solqaryn.API.Security;

/// <summary>
/// C03-03.M03.04: bounded, Development-only MySQL audit persistence adapter.
/// Does not create an audit permission, tenant context, business mutation,
/// platform grant or new public endpoint. The only call site is the existing
/// authenticated, opt-in audit-read diagnostic POST.
/// </summary>
public sealed class C0303TransactionalAuditPersistenceAdapter(
    AppDbContext database,
    IUsuarioScopeService scopes,
    ICurrentUserService current,
    IHttpContextAccessor contexts,
    C0303SecurityAuditApplicationService audit,
    IAuditoriaService ledger)
{
    public async Task<bool> RecordVerifiedAuditReadAsync(
        int tenantId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var http = contexts.HttpContext;
        if (tenantId <= 0 || current.UsuarioId is not > 0 ||
            !current.EstaAutenticado ||
            http?.User.Identity?.IsAuthenticated != true ||
            !HttpMethods.IsPost(http.Request.Method) ||
            VerifiedCommercialTenant.Resolve(http) != tenantId ||
            !database.Database.IsRelational())
            return false;

        // Never infer authority from the route, JWT tenant hint, browser or
        // a cached guard. Bind the actor and its role to the live SQL scope.
        var actorId = current.UsuarioId.Value;
        await using var transaction = await database.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var membership = await scopes.ObtenerActualAsync(
            tenantId, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (membership is null || membership.EmpresaId != tenantId ||
            membership.UsuarioId != actorId ||
            !current.EstaAutenticado || current.UsuarioId != actorId)
            return false;

        // The Application gate revalidates signed JWT, membership, tenant,
        // current audit RBAC and the only proven resource (Empresa root).
        // Never make an untrusted intent decide if an audit should be stored.
        // M03.05: a server-generated event id binds the safe envelope to the
        // strict ledger, independent of any client-supplied correlation header.
        var eventId = Guid.NewGuid().ToString("N");
        var decision = await audit.PrepareAsync(
            new C0303SecurityAuditApplicationService.ClientIntent(eventId), cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (!decision.RequiresDurableAuthenticatedAdapter ||
            decision.Candidate is not { } candidate ||
            candidate.ActorId != actorId || candidate.TenantId != tenantId ||
            candidate.Scope != Rules.Scope.Enterprise ||
            candidate.Action != Rules.Action.AuditRead ||
            candidate.Outcome != Rules.Outcome.Allowed ||
            !string.Equals(candidate.CorrelationId, eventId, StringComparison.Ordinal) ||
            VerifiedCommercialTenant.Resolve(http) != tenantId ||
            !current.EstaAutenticado || current.UsuarioId != actorId)
            return false;

        // Write strictly, then commit ONLY after the audit ledger accepted
        // the row. Exceptions (including database failure) propagate to the
        // diagnostic endpoint as an opaque 503; disposal rolls back changes.
        await ledger.RegistrarEstrictoAsync(
            ModuloSistema.Auditoria, AccionPermiso.Ver,
            "C03-03.M03.03: acceso de auditoría validado",
            referenciaId: tenantId, entidad: "Empresa",
            valoresNuevos: new
            {
                Schema = "security-audit/v1",
                EventId = eventId,
                Scope = "Enterprise",
                Action = "AuditRead",
                Outcome = "Allowed"
            });
        cancellationToken.ThrowIfCancellationRequested();
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
