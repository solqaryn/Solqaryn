using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Security;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C10-04.M03.02: bounded DEV-only Application preflight for private cache/job/backup/export work.
/// An authenticated backend adapter must provide the current membership, owner, policy and
/// persisted version. A successful shape check never enqueues a job, changes data, grants a
/// lease, emits a message, restores a backup or authorizes a provider.
/// </summary>
public interface IC1004VerifiedWorkEvidenceSource
{
    ValueTask<C1004CacheJobsApplicationService.VerifiedSnapshot?> ReadVerifiedAsync(
        string workId, CancellationToken cancellationToken);
}

public sealed class C1004CacheJobsApplicationService
{
    private readonly IC1004VerifiedWorkEvidenceSource _source;

    public C1004CacheJobsApplicationService(IC1004VerifiedWorkEvidenceSource source) =>
        _source = source ?? throw new ArgumentNullException(nameof(source));

    public sealed record Request(
        string? WorkId,
        string? Operation,
        string? IdempotencyKey,
        string? PayloadSha256,
        int? ClientTenantHint,
        long ExpectedVersion);

    public sealed record VerifiedSnapshot(
        ContextoTenantActual? Context,
        int VerifiedActorId,
        int PersistedResourceEmpresaId,
        long PersistedVersion,
        bool ActiveMembership,
        bool PermissionRevoked,
        bool ExplicitOperationPermission,
        bool IsServiceActor,
        bool ServiceGrantVerified,
        IReadOnlySet<string>? AllowedOperations,
        int RetryAttempt,
        int RetryLimit,
        DateTimeOffset DeadlineUtc,
        DateTimeOffset ServerNowUtc);

    public readonly record struct Result(string Code)
    {
        // External effects always remain forbidden by this preflight.
        public bool CanQueueWork => false;
        public bool CanWriteData => false;
        public bool CanIssueProviderGrant => false;
        public bool CanReadPrivateArtifact => false;
        public bool CertifiesTenantSecurity => false;
    }

    public async Task<Result> PrepareAsync(Request? request, CancellationToken cancellationToken = default)
    {
        if (request is null || !SafeWorkId(request.WorkId) || !SafeOperation(request.Operation) ||
            string.IsNullOrWhiteSpace(request.IdempotencyKey) ||
            string.IsNullOrWhiteSpace(request.PayloadSha256) || request.ExpectedVersion <= 0)
            return Deny("REQUEST_INVALID");

        cancellationToken.ThrowIfCancellationRequested();
        var verified = await _source.ReadVerifiedAsync(request.WorkId!, cancellationToken);
        if (verified?.Context is null || verified.AllowedOperations is null ||
            verified.PersistedResourceEmpresaId <= 0 || verified.VerifiedActorId <= 0)
            return Deny("VERIFIED_BACKEND_CONTEXT_REQUIRED");

        if (!verified.ActiveMembership || verified.PermissionRevoked ||
            !verified.ExplicitOperationPermission ||
            (verified.IsServiceActor && !verified.ServiceGrantVerified))
            return Deny("ACTIVE_MEMBERSHIP_AND_OPERATION_PERMISSION_REQUIRED");

        if (!verified.AllowedOperations.Contains(request.Operation!))
            return Deny("OPERATION_NOT_ALLOWLISTED");

        // A tenant hint is only a consistency check, never authority.
        if (request.ClientTenantHint is int hint &&
            hint != verified.PersistedResourceEmpresaId)
            return Deny("CLIENT_TENANT_OVERRIDE_DENIED");

        if (verified.PersistedVersion != request.ExpectedVersion)
            return Deny("RESOURCE_VERSION_MISMATCH");

        try
        {
            C1004CacheJobsDomainRules.ValidarSolicitudTrabajo(
                verified.Context, verified.VerifiedActorId, verified.PersistedResourceEmpresaId,
                request.Operation!, request.IdempotencyKey!, request.PayloadSha256!,
                request.ExpectedVersion, verified.RetryAttempt, verified.RetryLimit,
                verified.DeadlineUtc, verified.ServerNowUtc);
        }
        catch (ArgumentException)
        {
            return Deny("WORK_PARAMETERS_INVALID");
        }
        catch (InvalidOperationException)
        {
            return Deny("WORK_POLICY_OR_TENANT_DENIED");
        }

        // Even this result is NOT evidence that the injected provider verified a JWT,
        // performed CAS, checked CI, committed an operation or acquired an exclusive lease.
        return new Result("AUTHENTICATED_ADAPTER_AND_EXCLUSIVE_CAS_REQUIRED");
    }

    private static Result Deny(string code) => new(code);

    private static bool SafeWorkId(string? value) =>
        value is { Length: >= 1 and <= 128 } &&
        value.All(c => (c >= 'A' && c <= 'Z') ||
                       (c >= 'a' && c <= 'z') ||
                       (c >= '0' && c <= '9') || c is '-' or '_');

    private static bool SafeOperation(string? value) =>
        value is { Length: >= 3 and <= 64 } &&
        value.All(c => (c >= 'a' && c <= 'z') ||
                       (c >= '0' && c <= '9') || c is '.' or '_' or '-');
}
