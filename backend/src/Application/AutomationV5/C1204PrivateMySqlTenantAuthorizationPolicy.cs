using System;
using System.Linq;
using Guard = Solqaryn.Domain.AutomationV5.V5PrivateMySqlCutoverDomainRules;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C12-04.M03.03. Tenant/RBAC/MFA/revocation preflight before a private
/// MySQL readiness evidence lookup. Inputs must come from SERVER-verified
/// identity, not JWT fields or boolean claims supplied by a browser.
/// A null denial is permission to request a read-only evidence check,
/// NEVER authorization to execute SQL, migrations, cutover or deployment.
/// </summary>
public static class C1204PrivateMySqlTenantAuthorizationPolicy
{
    public static string? DenyBeforeEvidenceRead(
        C1204VerifiedActor? actor,
        Guard.Candidate? request)
    {
        if (actor is null || request is null ||
            !SafeOpaqueId(actor.ActorId) || !SafeOpaqueId(actor.TenantId) ||
            !actor.JwtAuthenticatedByServer ||
            !actor.MembershipCurrent || actor.MembershipRevoked ||
            !actor.CanReviewCutover || !actor.RecentMfaVerified)
            return "VERIFIED_SERVER_ACTOR_REQUIRED";

        if (!Enum.IsDefined(request.Intent))
            return "UNKNOWN_CUTOVER_INTENT";

        if (!string.Equals(request.Environment, "dev", StringComparison.Ordinal))
            return "NON_DEV_ENVIRONMENT_DENIED";

        if (!SafeOpaqueId(request.RequestedTenantId) ||
            !string.Equals(request.RequestedTenantId, actor.TenantId, StringComparison.Ordinal) ||
            !request.SourceAndDestinationOwnedByTenant)
            return "TENANT_OWNERSHIP_UNVERIFIED";

        if (request.ClientControlsConnectionOrTarget ||
            request.RequestsRealCutoverOrDualWrite ||
            request.RequestsDestructiveSchemaMutation ||
            request.RequestsGrantElevation ||
            request.RequestsSecretOrPersonalDataEmission ||
            request.AllowsCrossTenantFallback)
            return "UNAUTHORIZED_RUNTIME_MUTATION_DENIED";

        if (request.HasUnmitigatedP0OrP1)
            return "CRITICAL_FINDING_NO_GO";

        if (request.ExpectedCommitSha is not { Length: 40 } ||
            !request.ExpectedCommitSha.All(Uri.IsHexDigit) ||
            !string.Equals(request.ExpectedCommitSha, request.ManifestCommitSha,
                StringComparison.Ordinal))
            return "EXACT_MANIFEST_SHA_REQUIRED";

        if (request.Intent == Guard.Intent.ProposeIsolatedDryRun &&
            !SafeOpaqueId(request.IdempotencyKey))
            return "DRY_RUN_IDEMPOTENCY_REQUIRED";

        // Evidence and its server-side provenance are checked afterwards.
        // No caller-supplied boolean may grant SQL or deployment authority.
        return null;
    }

    private static bool SafeOpaqueId(string? value) =>
        value is { Length: >= 1 and <= 128 } &&
        value.All(c => (c >= 'a' && c <= 'z') ||
                       (c >= 'A' && c <= 'Z') ||
                       (c >= '0' && c <= '9') || c is '-' or '_' or '.');
}
