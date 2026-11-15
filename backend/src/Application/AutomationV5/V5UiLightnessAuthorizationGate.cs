using Solqaryn.Domain.Security;

namespace Solqaryn.Application.AutomationV5;

/// <summary>C05-04.M03.03: preflight only. Never grants runtime access.</summary>
public interface IV5UiLightnessAuthorizationEvidenceReader
{
    ValueTask<V5UiLightnessAuthorizationGate.Evidence?> ReadAsync(
        int actorId, int tenantId, string resourceId, CancellationToken cancellationToken);
}

public sealed class V5UiLightnessAuthorizationGate
{
    private readonly IV5UiLightnessAuthorizationEvidenceReader _reader;
    private readonly V5UiLightnessApplicationService _preflight;

    public V5UiLightnessAuthorizationGate(
        IV5UiLightnessAuthorizationEvidenceReader reader,
        V5UiLightnessApplicationService preflight)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _preflight = preflight ?? throw new ArgumentNullException(nameof(preflight));
    }

    public sealed record Request(
        int TenantId, string? ResourceId, string? ExpectedSourceSha,
        int? ClientTenantId, bool AttemptsTenantCacheBypass,
        bool AttemptsPrivatePreload, bool SuppressesUnauthorizedResponse,
        bool IncludesSensitiveTelemetry);

    // Must be populated by a trusted backend adapter, never HTTP input.
    public sealed record Evidence(
        string? SourceSha, int? ActorId, int? TenantId, string? ResourceId,
        bool BackendAuthenticationVerified, bool CurrentMembershipVerified,
        bool CurrentRbacPermissionVerified, bool PermissionRevoked,
        bool PersistedResourceOwnershipVerified, bool TenantScopedCacheVerified,
        bool RevocationInvalidationVerified);

    public readonly record struct Result(string Code)
    {
        public bool RequiresAuthenticatedAdapter =>
            Code == "AUTHENTICATED_LIGHTNESS_ADAPTER_REQUIRED";
        public bool GrantsAccess => false;
        public bool GrantsCrossTenantCacheRead => false;
        public bool GrantsPrivatePreload => false;
        public bool CertifiesCoreWebVitals => false;
    }

    public async Task<Result> EvaluateAsync(
        ContextoTenantActual authenticated, Request? request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(authenticated);

        if (request is null) return Deny("LIGHTNESS_AUTH_REQUEST_REQUIRED");
        if (request.TenantId <= 0 || !ValidOpaqueId(request.ResourceId))
            return Deny("LIGHTNESS_TENANT_OR_RESOURCE_INVALID");
        if (!ValidSha(request.ExpectedSourceSha))
            return Deny("LIGHTNESS_SOURCE_SHA_INVALID");
        if (request.AttemptsTenantCacheBypass || request.AttemptsPrivatePreload ||
            request.SuppressesUnauthorizedResponse || request.IncludesSensitiveTelemetry)
            return Deny("LIGHTNESS_CLIENT_SECURITY_BYPASS_DENIED");
        if (request.ClientTenantId is not null && request.ClientTenantId != request.TenantId)
            return Deny("LIGHTNESS_CLIENT_TENANT_OVERRIDE_DENIED");

        // Reject tenant A -> B before any provider I/O.
        authenticated.ExigirEmpresa(request.TenantId);

        var evidence = await _reader.ReadAsync(
            authenticated.UsuarioId, request.TenantId, request.ResourceId!, cancellationToken);
        if (evidence is null || !string.Equals(
            evidence.SourceSha, request.ExpectedSourceSha, StringComparison.Ordinal))
            return Deny("LIGHTNESS_TRUSTED_EVIDENCE_MISSING_OR_STALE");

        if (!evidence.BackendAuthenticationVerified ||
            !evidence.CurrentMembershipVerified || !evidence.CurrentRbacPermissionVerified ||
            evidence.PermissionRevoked)
            return Deny("LIGHTNESS_BACKEND_AUTHORIZATION_REQUIRED");

        if (evidence.ActorId != authenticated.UsuarioId ||
            evidence.TenantId != request.TenantId ||
            !string.Equals(evidence.ResourceId, request.ResourceId, StringComparison.Ordinal) ||
            !evidence.PersistedResourceOwnershipVerified)
            return Deny("LIGHTNESS_PERSISTED_OWNER_MISMATCH");

        if (!evidence.TenantScopedCacheVerified || !evidence.RevocationInvalidationVerified)
            return Deny("LIGHTNESS_TENANT_CACHE_ISOLATION_REQUIRED");

        var domain = await _preflight.EvaluateCurrentAsync(
            authenticated, request.TenantId, request.ResourceId!, cancellationToken);
        if (!domain.NeedsIndependentRuntimeVerification) return Deny(domain.Code);

        // HTTP JWT/RBAC, provider validation, Angular and browser CWV need
        // separate authenticated implementation and causal tests.
        return new Result("AUTHENTICATED_LIGHTNESS_ADAPTER_REQUIRED");
    }

    private static Result Deny(string code) => new(code);

    private static bool ValidSha(string? value) =>
        value is { Length: 40 } &&
        value.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));

    private static bool ValidOpaqueId(string? value) =>
        value is { Length: > 0 and <= 128 } &&
        value.All(c => char.IsLetterOrDigit(c) ||
            c == '-' || c == '_' || c == '.');
}
