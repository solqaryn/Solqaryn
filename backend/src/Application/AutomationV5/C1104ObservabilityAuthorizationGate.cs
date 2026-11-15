using System.Text.RegularExpressions;
using Domain = Solqaryn.Domain.AutomationV5.C1104ObservabilitySignalDomainRules;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// Backend-supplied principal, never a client DTO, cookie or query parameter.
/// No production implementation or endpoint is registered in this microtask.
/// </summary>
public sealed record C1104VerifiedPrincipal(
    int ActorId, string? TenantScope, bool JwtVerified, bool MembershipActive,
    bool RoleActive, bool ExplicitReadPermissionCurrent, bool Revoked,
    bool AuditFenceAvailable);

public interface IC1104VerifiedPrincipalSource
{
    ValueTask<C1104VerifiedPrincipal?> CurrentAsync(CancellationToken cancellationToken);
}

public interface IC1104TenantPartitionedEvidenceSource
{
    ValueTask<C1104ObservabilityApplicationService.VerifiedEvidence?> ReadAsync(
        string verifiedTenantScope, string signalName, string correlationId,
        CancellationToken cancellationToken);
}

/// <summary>
/// C11-04.M03.03: fail-closed DEV-only authorization and tenant partition
/// preflight. Always returns a non-authorizing verdict, without data or grants.
/// An authenticated API adapter is required in a separate microtask.
/// </summary>
public sealed class C1104ObservabilityAuthorizationGate
{
    private readonly IC1104VerifiedPrincipalSource _principals;
    private readonly IC1104TenantPartitionedEvidenceSource _evidence;

    public C1104ObservabilityAuthorizationGate(
        IC1104VerifiedPrincipalSource principals,
        IC1104TenantPartitionedEvidenceSource evidence)
    {
        _principals = principals ?? throw new ArgumentNullException(nameof(principals));
        _evidence = evidence ?? throw new ArgumentNullException(nameof(evidence));
    }

    public sealed record Request(string? SignalName, string? CorrelationId,
        string? ExpectedSourceSha);

    public readonly record struct Verdict(string Code)
    {
        public bool RequiresAuthenticatedApiAdapter =>
            Code == "AUTHENTICATED_TENANT_SCOPED_ADAPTER_REQUIRED";
        public bool GrantsAccess => false;
        public bool ExposesTelemetry => false;
        public bool WritesData => false;
        public bool CertifiesSlo => false;
        public bool MayOverrideTenant => false;
    }

    public async Task<Verdict> EvaluateAsync(
        Request? request, CancellationToken cancellationToken = default)
    {
        if (request is null || !FullSha(request.ExpectedSourceSha) ||
            request.SignalName is not ("http_requests" or "request_latency" or
                "error_rate" or "db_duration") ||
            request.CorrelationId is not { Length: >= 1 and <= 64 } ||
            !Regex.IsMatch(request.CorrelationId,
                @"\A[a-zA-Z0-9_.-]{1,64}\z", RegexOptions.CultureInvariant))
            return Deny("INVALID_SIGNAL_CORRELATION_OR_SOURCE");

        cancellationToken.ThrowIfCancellationRequested();
        var principal = await _principals.CurrentAsync(cancellationToken);
        if (principal is null || principal.ActorId <= 0 ||
            !SafeTenant(principal.TenantScope) || !principal.JwtVerified ||
            !principal.MembershipActive || !principal.RoleActive ||
            !principal.ExplicitReadPermissionCurrent || principal.Revoked ||
            !principal.AuditFenceAvailable)
            return Deny("FRESH_JWT_MEMBERSHIP_RBAC_AUDIT_REQUIRED");

        // Partition the physical read by a trusted backend identity BEFORE I/O.
        // No client-provided tenant may ever be used as a partition selector.
        var evidence = await _evidence.ReadAsync(
            principal.TenantScope!, request.SignalName, request.CorrelationId,
            cancellationToken);

        if (evidence is null || !evidence.FromTrustedBackend ||
            !evidence.CurrentJwtVerified || !evidence.CurrentMembershipVerified ||
            !evidence.CurrentExplicitReadRbacVerified ||
            !evidence.AuditFenceAvailable ||
            !string.Equals(evidence.TrustedActorTenantId, principal.TenantScope,
                StringComparison.Ordinal) ||
            !string.Equals(evidence.PersistedResourceTenantId, principal.TenantScope,
                StringComparison.Ordinal) ||
            !string.Equals(evidence.Aggregate?.IdentityTenant, principal.TenantScope,
                StringComparison.Ordinal) ||
            !string.Equals(evidence.Aggregate?.ResourceTenant, principal.TenantScope,
                StringComparison.Ordinal))
            return Deny("TENANT_EVIDENCE_OR_REVOCATION_MISMATCH");

        // Compose the previous Application and Domain guards rather than
        // relying on boolean evidence alone. No actual read is returned.
        var service = new C1104ObservabilityApplicationService(
            new FixedEvidenceReader(evidence));
        var preflight = await service.PrepareAsync(
            new C1104ObservabilityApplicationService.Request(
                request.SignalName, request.CorrelationId, request.ExpectedSourceSha),
            cancellationToken);
        if (!preflight.RequiresAuthenticatedReadAdapter)
            return Deny(preflight.Code);

        return new Verdict("AUTHENTICATED_TENANT_SCOPED_ADAPTER_REQUIRED");
    }

    private sealed class FixedEvidenceReader(
        C1104ObservabilityApplicationService.VerifiedEvidence evidence)
        : IC1104ObservabilityEvidenceReader
    {
        public ValueTask<C1104ObservabilityApplicationService.VerifiedEvidence?> ReadAsync(
            string signalName, string correlationId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult<C1104ObservabilityApplicationService.VerifiedEvidence?>(
                evidence);
        }
    }

    private static Verdict Deny(string code) => new(code);

    private static bool FullSha(string? value) =>
        value is { Length: 40 } &&
        value.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));

    private static bool SafeTenant(string? value) =>
        value is { Length: >= 3 and <= 128 } &&
        value.All(c => (c >= 'a' && c <= 'z') ||
                       (c >= 'A' && c <= 'Z') ||
                       (c >= '0' && c <= '9') || c is '-' or '_');
}
