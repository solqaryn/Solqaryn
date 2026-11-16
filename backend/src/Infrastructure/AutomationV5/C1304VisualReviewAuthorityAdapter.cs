using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// Trusted backend current-authority reader. Implementations MUST authenticate
/// their source and recheck membership, roles and revocation on every call.
/// Client claims or cached browser snapshots cannot implement this contract.
/// No production reader or DI registration is provided by this microtask.
/// </summary>
public interface IC1304VisualReviewAuthoritySource
{
    Task<C1304VisualReviewAuthorityAdapter.Authority?> ReadCurrentAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// C13-04.M03.03: fail-closed, isolated .NET 10 tenancy/permission boundary
/// in front of the existing Application and Domain visual review preflight.
/// Not an HTTP middleware, JWT verifier, live tenant connector or release gate.
/// </summary>
public sealed class C1304VisualReviewAuthorityAdapter
{
    private readonly IC1304VisualReviewAuthoritySource _source;
    private readonly V5VisualConsistencyApplicationService _application;
    private readonly TimeProvider _time;

    public C1304VisualReviewAuthorityAdapter(
        IC1304VisualReviewAuthoritySource source,
        V5VisualConsistencyApplicationService application,
        TimeProvider time)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _application = application ?? throw new ArgumentNullException(nameof(application));
        _time = time ?? throw new ArgumentNullException(nameof(time));
    }

    public sealed record Authority(
        int ActorId, int TenantId, bool IdentityVerified,
        bool MembershipCurrent, bool ExplicitReviewPermission,
        bool RoleRevoked, bool SourceAuthenticated,
        string? SourceSha, DateTimeOffset ValidUntilUtc,
        IReadOnlySet<string>? ServerApprovedSurfaces);

    public readonly record struct Outcome(string Code, int ItemsChecked)
    {
        public bool ReadyForIndependentBrowserReview =>
            Code == "EXTERNAL_BROWSER_AND_A5_REQUIRED";
        public bool GrantsLiveTenantAccess => false;
        public bool GrantsJwtOrRbac => false;
        public bool CertifiesAccessibility => false;
        public bool PersistsEvidence => false;
        public bool AuthorizesQaOrProduction => false;
    }

    private static bool FullSha(string? value) =>
        value is { Length: 40 } && value.All(Uri.IsHexDigit);

    private static bool SafeSurfaces(IReadOnlySet<string> values) =>
        values.Count is >= 1 and <= 64 &&
        values.All(value => value is { Length: >= 3 and <= 64 } &&
            value.All(ch => ch is >= 'a' and <= 'z' or >= '0' and <= '9' or '-'));

    public async Task<Outcome> PrepareAsync(
        int requestedTenantId, string? expectedSourceSha,
        V5VisualConsistencyApplicationService.Request? request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (requestedTenantId <= 0 || !FullSha(expectedSourceSha) ||
            request is null || request.ClientRequestedTenantId != requestedTenantId)
            return new("INVALID_REQUEST_SCOPE", 0);

        Authority? current;
        try
        {
            current = await _source.ReadCurrentAsync(cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new("CURRENT_AUTHORITY_UNAVAILABLE", 0);
        }

        var now = _time.GetUtcNow();
        var allowlist = current?.ServerApprovedSurfaces;
        if (current is null || current.ActorId <= 0 || current.TenantId <= 0 ||
            current.TenantId != requestedTenantId || !current.IdentityVerified ||
            !current.MembershipCurrent || !current.ExplicitReviewPermission ||
            current.RoleRevoked || !current.SourceAuthenticated ||
            !FullSha(current.SourceSha) ||
            !string.Equals(current.SourceSha, expectedSourceSha, StringComparison.Ordinal) ||
            current.ValidUntilUtc <= now || current.ValidUntilUtc > now.AddMinutes(15) ||
            allowlist is null || !SafeSurfaces(allowlist))
            return new("CURRENT_VERIFIED_VISUAL_REVIEW_AUTHORITY_REQUIRED", 0);

        // Construct a trusted context only from the current backend adapter,
        // never from the browser's tenant, permission, role or token claims.
        var context = new V5VisualConsistencyDomainRules.VerifiedContext(
            current.ActorId, current.TenantId, current.MembershipCurrent,
            current.ExplicitReviewPermission, current.SourceSha!);
        var result = _application.Prepare(context, request, allowlist);
        return new(result.Code, result.ItemsChecked);
    }
}
