using Microsoft.Extensions.Logging;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// Must read fresh identity, membership, tenancy and service scope from a trusted
/// authenticated backend. Browser/client evidence can NEVER implement this port.
/// </summary>
public interface IC1004CurrentAuditAuthorityReader
{
    ValueTask<C1004SafeStructuredAuditService.VerifiedEvidence?> ReadCurrentAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// C10-04.M03.06: bounded, structured, privacy-safe telemetry for the C10-04
/// review pipeline. This does not start jobs, write provider data, grant access,
/// persist audit records or certify a durable logging provider.
/// </summary>
public sealed class C1004SafeStructuredAuditService
{
    private readonly IC1004CurrentAuditAuthorityReader _reader;
    private readonly ILogger<C1004SafeStructuredAuditService> _logger;
    public C1004SafeStructuredAuditService(
        IC1004CurrentAuditAuthorityReader reader,
        ILogger<C1004SafeStructuredAuditService> logger)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public enum AuditOperation { CacheRead = 1, JobReview = 2,
        BackupSnapshotReview = 3, ExportAccessReview = 4 }

    public sealed record Request(AuditOperation Operation, int ExpectedTenantId,
        bool ClaimsClientAuthority = false, bool RequestsWorkerOrProviderWrite = false,
        bool ContainsSensitiveClientPayload = false);

    public sealed record VerifiedEvidence(
        int ActorId, int TenantId, int ResourceTenantId,
        bool BackendIdentityVerified, bool MembershipCurrent,
        bool RelationalRoleVerified, bool TenantActive, bool GrantRevoked,
        bool ServiceScopeCurrent, bool ProviderSourceAuthenticated,
        string? InternalCorrelationId, string? SourceSha);

    public sealed record Result(string Code, bool EventSubmittedToLogger)
    {
        public bool GrantsTenantAccess => false;
        public bool ExecutesWorkerOrProviderWrite => false;
        public bool CertifiesDurableAudit => false;
    }

    private static bool ValidCorrelation(string? value) =>
        value is { Length: >= 8 and <= 96 } &&
        value.All(ch => ch is >= 'a' and <= 'z' or >= 'A' and <= 'Z'
            or >= '0' and <= '9' or '-');

    private static bool ValidSha(string? sha) =>
        sha is { Length: 40 } && sha.All(Uri.IsHexDigit);

    public async ValueTask<Result> AuditAsync(
        Request? request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request is null || !Enum.IsDefined(request.Operation) ||
            request.ExpectedTenantId <= 0 || request.ClaimsClientAuthority ||
            request.RequestsWorkerOrProviderWrite || request.ContainsSensitiveClientPayload)
            return new("UNTRUSTED_AUDIT_INTENT_DENIED", false);

        VerifiedEvidence? trusted;
        try { trusted = await _reader.ReadCurrentAsync(cancellationToken); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Never include exception, URL, claims, token, SQL or client text in logs.
            try { _logger.LogWarning("C1004_AUDIT_AUTHORITY_UNAVAILABLE"); }
            catch (Exception logFailure) when (logFailure is not OutOfMemoryException) { }
            return new("CURRENT_AUDIT_AUTHORITY_UNAVAILABLE", false);
        }

        if (trusted is null || trusted.ActorId <= 0 ||
            trusted.TenantId <= 0 || trusted.ResourceTenantId <= 0 ||
            !trusted.BackendIdentityVerified || !trusted.MembershipCurrent ||
            !trusted.RelationalRoleVerified || !trusted.TenantActive ||
            trusted.GrantRevoked || !trusted.ServiceScopeCurrent ||
            !trusted.ProviderSourceAuthenticated ||
            request.ExpectedTenantId != trusted.TenantId ||
            trusted.ResourceTenantId != trusted.TenantId ||
            !ValidCorrelation(trusted.InternalCorrelationId) ||
            !ValidSha(trusted.SourceSha))
            return new("CURRENT_VERIFIED_TENANT_AUDIT_SCOPE_REQUIRED", false);

        // Only trusted internal correlation and allowlisted operation code are
        // formatted. Neither actor ID, tenant ID, user text nor exception is logged.
        try
        {
            _logger.LogInformation(
                "C1004_SAFE_AUDIT event={EventCode} correlation={CorrelationKey}",
                request.Operation.ToString(), trusted.InternalCorrelationId);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new("STRUCTURED_AUDIT_SINK_UNAVAILABLE", false);
        }

        return new("EXTERNAL_DURABLE_AUDIT_ADAPTER_REQUIRED", true);
    }
}
