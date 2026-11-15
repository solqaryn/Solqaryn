using Guard = Solqaryn.Domain.AutomationV5.V5GlobalControlPlaneDomainRules;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C02-04.M03.02: an application-layer, non-authorizing request preflight.
/// It does not load grants, authenticate JWT, grant access, query tenants,
/// stage a mutation, emit an audit record or expose any runtime endpoint.
/// A trusted backend adapter and persisted auditing must be built and tested
/// separately before any real control-plane operation can be executed.
/// </summary>
public sealed class V5GlobalControlPlaneApplicationPreflight
{
    // Client requests never contain a trust/grant/MFA/role snapshot.
    public sealed record ReadIntent(
        Guard.Operation Operation,
        string? RequestedTenantId,
        string? Justification,
        bool ContainsClientAuthorityHint,
        bool AttemptsWriteOrExport,
        bool IncludesSensitiveData);

    public sealed record PreflightReview(string Code, bool RequiresLiveSecurityChecks)
    {
        // Intentionally non-authorizing: even complete trusted snapshots may
        // only request a further independent server-side enforcement stage.
        public bool GrantsAccess => false;
    }

    private static bool SafeTenantIdentifier(string? id) =>
        !string.IsNullOrWhiteSpace(id) &&
        id.Length <= 128 &&
        !id.Any(char.IsControl);

    public PreflightReview ReviewOnly(
        Guard.TrustedSnapshot? trustedBackendSnapshot,
        ReadIntent? intent,
        DateTimeOffset serverUtc)
    {
        if (serverUtc.Offset != TimeSpan.Zero)
            return new("SERVER_UTC_CLOCK_REQUIRED", false);

        if (intent is null)
            return new("READ_INTENT_REQUIRED", false);

        // A request's identifier is at most a consistency assertion, not a
        // selector for a tenant and never an authorization source.
        if (!SafeTenantIdentifier(intent.RequestedTenantId) ||
            !string.Equals(
                trustedBackendSnapshot?.VerifiedTargetTenantId,
                intent.RequestedTenantId,
                StringComparison.Ordinal))
            return new("SERVER_VERIFIED_TARGET_MISMATCH", false);

        // Prevent arbitrary client text entering an audit envelope for regular
        // reads; exception reasons are separately bounded by Domain rules.
        if (intent.Operation != Guard.Operation.ExceptionalTenantMetadataRead &&
            !string.IsNullOrEmpty(intent.Justification))
            return new("UNEXPECTED_JUSTIFICATION_REJECTED", false);

        var proposal = new Guard.ProposedAction(
            intent.Operation,
            intent.Justification,
            intent.ContainsClientAuthorityHint,
            intent.AttemptsWriteOrExport,
            intent.IncludesSensitiveData);

        Guard.Verdict domain = Guard.Evaluate(
            trustedBackendSnapshot, proposal, serverUtc);

        // Even the most favorable result explicitly requires runtime JWT,
        // membership/RBAC, MFA, reauthentication, current grants, resource
        // ownership, persistent audit and a separate trusted adapter.
        return new(
            domain.Code,
            domain.RequiresBackendEnforcement);
    }
}
