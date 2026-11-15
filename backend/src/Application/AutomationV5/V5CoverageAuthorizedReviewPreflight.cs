using System.Security.Claims;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C15-01.M03.03: backend-only preflight for reviewing requirement coverage.
/// A validated ClaimsPrincipal is a boundary input, NOT proof that the JWT,
/// membership, source artifacts or actual tenant ownership were authenticated.
/// No institutional data is read, written or certified by this service.
/// </summary>
public sealed class V5CoverageAuthorizedReviewPreflight
{
    private const string Permission = "coverage.review";
    private static readonly V5CoverageApplicationService Coverage = new();

    public sealed record Context(
        ClaimsPrincipal? ValidatedPrincipal,
        string? BackendSubjectId,
        string? BackendTenantId,
        bool BackendMembershipVerified,
        IReadOnlySet<string>? EffectivePermissions,
        bool DevScopeLockVerified,
        string? ExpectedHeadSha,
        string? ObservedHeadSha);

    public sealed record ReviewRequest(
        string? RequestedSubjectId,
        string? RequestedTenantId,
        V5CoverageApplicationService.Request? Coverage);

    public readonly record struct Result(string Code, int MappingsExamined)
    {
        public bool AuthorizesInstitutionalWrite => false;
        public bool CertifiesFullCoverage => false;
        public bool GrantsIndependentA5 => false;
        public bool AuthorizesEnvironmentPromotion => false;
        public bool GrantsCrossTenantAccess => false;
    }

    public Result Prepare(Context? context, ReviewRequest? request)
    {
        if (context?.ValidatedPrincipal?.Identity?.IsAuthenticated != true ||
            string.IsNullOrWhiteSpace(context.BackendSubjectId) ||
            string.IsNullOrWhiteSpace(context.BackendTenantId))
            return Deny("BACKEND_AUTHENTICATION_REQUIRED");

        var principalSubject = context.ValidatedPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var principalTenant = context.ValidatedPrincipal.FindFirst("tenant_id")?.Value;
        if (!string.Equals(principalSubject, context.BackendSubjectId, StringComparison.Ordinal) ||
            !string.Equals(principalTenant, context.BackendTenantId, StringComparison.Ordinal))
            return Deny("BACKEND_IDENTITY_TENANT_MISMATCH");

        if (!context.BackendMembershipVerified ||
            context.EffectivePermissions?.Contains(Permission) != true)
            return Deny("MEMBERSHIP_OR_RBAC_REVIEW_PERMISSION_REQUIRED");

        if (!context.DevScopeLockVerified ||
            !FullSha(context.ExpectedHeadSha) ||
            !FullSha(context.ObservedHeadSha) ||
            !string.Equals(context.ExpectedHeadSha, context.ObservedHeadSha,
                StringComparison.OrdinalIgnoreCase))
            return Deny("DEV_SCOPE_OR_COMMIT_SHA_REQUIRED");

        if (request?.Coverage is null ||
            !string.Equals(request.RequestedSubjectId, context.BackendSubjectId,
                StringComparison.Ordinal) ||
            !string.Equals(request.RequestedTenantId, context.BackendTenantId,
                StringComparison.Ordinal) ||
            !string.Equals(request.Coverage.ExpectedHeadSha, context.ExpectedHeadSha,
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(request.Coverage.ObservedHeadSha, context.ObservedHeadSha,
                StringComparison.OrdinalIgnoreCase))
            return Deny("TRUSTED_REVIEW_BOUNDARY_MISMATCH");

        // This checks internal consistency of a caller-supplied snapshot only.
        // The actual Standard, PLAN, DAG, GitHub CI and reviewer identity must
        // still be authenticated independently by a separately authorized adapter.
        var assessed = Coverage.Prepare(request.Coverage);
        if (assessed.Code != "AUTHENTICATED_EVIDENCE_REQUIRED")
            return new Result(assessed.Code, assessed.MappingsExamined);

        return new Result("INDEPENDENT_VERIFIED_SOURCE_REVIEW_REQUIRED",
            assessed.MappingsExamined);
    }

    private static bool FullSha(string? sha) =>
        sha is { Length: 40 } && sha.All(Uri.IsHexDigit);

    private static Result Deny(string code) => new(code, 0);
}
