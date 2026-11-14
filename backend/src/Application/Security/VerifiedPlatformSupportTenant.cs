using Microsoft.AspNetCore.Http;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Security;

/// <summary>
/// Server-side request markers for a support read. A candidate is created only
/// after durable audit and live platform/session checks; the authorized marker is
/// created only by the normal permission action filter.
/// </summary>
public static class VerifiedPlatformSupportTenant
{
    public const string CandidateItemKey = "SOLQARYN_SUPPORT_READ_CANDIDATE";
    public const string AuthorizedItemKey = "SOLQARYN_SUPPORT_READ_AUTHORIZED";

    public sealed record Scope(int SesionId, int EmpresaId, ModuloSistema Modulo);

    public static void MarkCandidate(HttpContext context, Scope scope)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(scope);
        if (scope.SesionId <= 0 || scope.EmpresaId <= 0 ||
            !PlatformSupportPolicy.IsAllowedModule(scope.Modulo))
            throw new ArgumentException("El alcance de soporte no es válido.", nameof(scope));

        context.Items[CandidateItemKey] = scope;
    }

    public static Scope? ResolveCandidate(HttpContext? context) =>
        context is not null &&
        context.Items.TryGetValue(CandidateItemKey, out var value) &&
        value is Scope scope
            ? scope
            : null;

    public static void MarkAuthorized(HttpContext context, Scope scope)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(scope);
        var candidate = ResolveCandidate(context);
        if (candidate != scope)
            throw new InvalidOperationException("La autorización de soporte no coincide con el candidato verificado.");

        context.Items[AuthorizedItemKey] = scope;
    }

    public static Scope? Resolve(HttpContext? context) =>
        context is not null &&
        context.Items.TryGetValue(AuthorizedItemKey, out var value) &&
        value is Scope scope
            ? scope
            : null;

    public static int? ResolveEmpresaId(HttpContext? context) => Resolve(context)?.EmpresaId;
}