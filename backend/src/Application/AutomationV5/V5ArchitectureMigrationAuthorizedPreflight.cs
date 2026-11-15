using System.Security.Claims;
using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// Fail-closed authorization/tenancy preflight for C15-02.M03.03.
/// A future authenticated backend adapter must supply the validated principal,
/// membership, effective RBAC permissions and tenant ownership. This service
/// cannot verify the JWT signature, authenticate a caller, access a provider,
/// issue an A5 or authorize an external mutation.
/// </summary>
public sealed class V5ArchitectureMigrationAuthorizedPreflight
{
    private const string ReviewPermission = "architecture.migration.review";
    private static readonly V5ArchitectureMigrationApplicationService Application = new();

    public sealed record Context(
        ClaimsPrincipal? ValidatedPrincipal,
        string? BackendVerifiedSubjectId,
        string? BackendVerifiedTenantId,
        IReadOnlySet<string>? BackendEffectivePermissions,
        bool BackendMembershipVerified,
        bool BackendTenantOwnershipVerified,
        bool DevScopeLockVerified,
        string? ExpectedHeadSha,
        string? ObservedHeadSha);

    public readonly record struct Result(string Code, int ItemsReviewed)
    {
        public bool AuthorizesDeployment => false;
        public bool AuthorizesDataMigration => false;
        public bool AuthorizesSecretsOrDnsMutation => false;
        public bool GrantsA5OrEnvironmentPromotion => false;
    }

    public Result Prepare(
        Context? context, V5ArchitectureMigrationApplicationService.Request? request)
    {
        if (context?.ValidatedPrincipal?.Identity?.IsAuthenticated != true ||
            string.IsNullOrWhiteSpace(context.BackendVerifiedSubjectId) ||
            string.IsNullOrWhiteSpace(context.BackendVerifiedTenantId) ||
            !context.BackendMembershipVerified ||
            !context.BackendTenantOwnershipVerified)
            return Deny("BACKEND_AUTHENTICATION_AND_MEMBERSHIP_REQUIRED");

        var principalSubject =
            context.ValidatedPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var principalTenant =
            context.ValidatedPrincipal.FindFirst("tenant_id")?.Value;
        if (!string.Equals(principalSubject, context.BackendVerifiedSubjectId,
                StringComparison.Ordinal) ||
            !string.Equals(principalTenant, context.BackendVerifiedTenantId,
                StringComparison.Ordinal))
            return Deny("TENANT_OR_SUBJECT_MISMATCH");

        if (context.BackendEffectivePermissions is null ||
            !context.BackendEffectivePermissions.Contains(ReviewPermission))
            return Deny("VERIFIED_RBAC_REVIEW_PERMISSION_REQUIRED");

        if (!context.DevScopeLockVerified ||
            !FullSha(context.ExpectedHeadSha) ||
            !FullSha(context.ObservedHeadSha) ||
            !string.Equals(context.ExpectedHeadSha, context.ObservedHeadSha,
                StringComparison.OrdinalIgnoreCase))
            return Deny("DEV_SCOPE_AND_SHA_REQUIRED");

        if (request?.Boundary is null ||
            !string.Equals(context.BackendVerifiedTenantId,
                request.Boundary.AuthenticatedTenantId, StringComparison.Ordinal) ||
            !string.Equals(context.ExpectedHeadSha,
                request.Boundary.ExpectedCommitSha, StringComparison.OrdinalIgnoreCase))
            return Deny("TRUSTED_BOUNDARY_MISMATCH");

        var appResult = Application.Prepare(request);
        if (appResult.Code != "INDEPENDENT_RUNTIME_EVIDENCE_AND_A5_REQUIRED")
            return new Result(appResult.Code, appResult.ItemsReviewed);

        // Even a coherent, validated principal supplied in this preflight is
        // not proof of a real JWT signature, database-backed ownership, checks,
        // backup/restore, independent A5 or environment authorization.
        return new Result("AUTHENTICATED_ADAPTER_EVIDENCE_REQUIRED",
            appResult.ItemsReviewed);
    }

    private static bool FullSha(string? sha) =>
        sha is { Length: 40 } && sha.All(Uri.IsHexDigit);

    private static Result Deny(string code) => new(code, 0);
}
