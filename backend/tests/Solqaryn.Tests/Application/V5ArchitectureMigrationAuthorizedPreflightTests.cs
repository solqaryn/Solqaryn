using System.Security.Claims;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5ArchitectureMigrationDomainRules;

namespace Solqaryn.Tests.Application;

public sealed class V5ArchitectureMigrationAuthorizedPreflightTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly V5ArchitectureMigrationAuthorizedPreflight Gate = new();

    private static ClaimsPrincipal Identity(string id = "user-A", string tenant = "tenant-A") =>
        new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, id),
            new Claim("tenant_id", tenant)
        }, "synthetic-validated-backend-identity"));

    private static V5ArchitectureMigrationAuthorizedPreflight.Context Context() =>
        new(Identity(), "user-A", "tenant-A",
            new HashSet<string>(StringComparer.Ordinal) { "architecture.migration.review" },
            true, true, true, Sha, Sha);

    private static Rules.Candidate Candidate() =>
        new(Sha, "tenant-A", "Render", "Render", "dev", true, true,
            false, false, true, true, true, true, false);

    private static V5ArchitectureMigrationApplicationService.Request Request(
        params Rules.Candidate?[] candidates) =>
        new(new Rules.TrustedBoundary(Sha, "tenant-A", true, true,
            new HashSet<string> { "Render", "Vercel", "Aiven" }),
            candidates.Length == 0 ? new Rules.Candidate?[] { Candidate() } : candidates);

    [Fact]
    public void VerifiedSyntheticContextStillCannotAuthorizeAnyMutationOrA5()
    {
        var result = Gate.Prepare(Context(), Request(Candidate()));
        Assert.Equal("AUTHENTICATED_ADAPTER_EVIDENCE_REQUIRED", result.Code);
        Assert.Equal(1, result.ItemsReviewed);
        Assert.False(result.AuthorizesDeployment);
        Assert.False(result.AuthorizesDataMigration);
        Assert.False(result.AuthorizesSecretsOrDnsMutation);
        Assert.False(result.GrantsA5OrEnvironmentPromotion);
    }

    [Fact]
    public void MissingPrincipalMembershipOrOwnershipIsDenied()
    {
        Assert.Equal("BACKEND_AUTHENTICATION_AND_MEMBERSHIP_REQUIRED",
            Gate.Prepare(null, Request()).Code);
        Assert.Equal("BACKEND_AUTHENTICATION_AND_MEMBERSHIP_REQUIRED",
            Gate.Prepare(Context() with { BackendMembershipVerified = false }, Request()).Code);
        Assert.Equal("BACKEND_AUTHENTICATION_AND_MEMBERSHIP_REQUIRED",
            Gate.Prepare(Context() with { BackendTenantOwnershipVerified = false }, Request()).Code);
        Assert.Equal("BACKEND_AUTHENTICATION_AND_MEMBERSHIP_REQUIRED",
            Gate.Prepare(Context() with
            {
                ValidatedPrincipal = new ClaimsPrincipal(new ClaimsIdentity())
            }, Request()).Code);
    }

    [Fact]
    public void CrossTenantOrSpoofedSubjectDoesNotReachMigrationPreflight()
    {
        Assert.Equal("TENANT_OR_SUBJECT_MISMATCH",
            Gate.Prepare(Context() with { ValidatedPrincipal = Identity(tenant: "tenant-B") },
                Request()).Code);
        Assert.Equal("TENANT_OR_SUBJECT_MISMATCH",
            Gate.Prepare(Context() with { BackendVerifiedSubjectId = "another-user" },
                Request()).Code);
        Assert.Equal("TRUSTED_BOUNDARY_MISMATCH",
            Gate.Prepare(Context() with { BackendVerifiedTenantId = "tenant-B",
                ValidatedPrincipal = Identity(tenant: "tenant-B") },
                Request()).Code);
    }

    [Fact]
    public void AbsentPermissionAndInvalidScopeOrShaDenyByDefault()
    {
        Assert.Equal("VERIFIED_RBAC_REVIEW_PERMISSION_REQUIRED",
            Gate.Prepare(Context() with
            {
                BackendEffectivePermissions = new HashSet<string> { "architecture.migration.read" }
            }, Request()).Code);
        Assert.Equal("DEV_SCOPE_AND_SHA_REQUIRED",
            Gate.Prepare(Context() with { DevScopeLockVerified = false }, Request()).Code);
        Assert.Equal("DEV_SCOPE_AND_SHA_REQUIRED",
            Gate.Prepare(Context() with { ObservedHeadSha = new string('f', 40) },
                Request()).Code);
    }

    [Fact]
    public void QaProdAndUnsafeProvidersRemainBlockedByDomain()
    {
        Assert.Equal("PRODUCTION_OR_QA_NOT_AUTHORIZED",
            Gate.Prepare(Context(), Request(Candidate() with { Environment = "qa" })).Code);
        Assert.Equal("PRODUCTION_OR_QA_NOT_AUTHORIZED",
            Gate.Prepare(Context(), Request(Candidate() with { Environment = "prod" })).Code);
        Assert.Equal("PROVIDER_NOT_IN_VERIFIED_DEV_CATALOG",
            Gate.Prepare(Context(), Request(Candidate() with { TargetProvider = "Hostinger" })).Code);
        Assert.Equal("RUNTIME_MUTATION_OUT_OF_SCOPE",
            Gate.Prepare(Context(), Request(Candidate() with
            {
                RequiresLiveDnsOrSecretMutation = true
            })).Code);
    }

    [Fact]
    public void MissingBoundaryAndSandboxEvidenceNeverPass()
    {
        Assert.Equal("TRUSTED_BOUNDARY_MISMATCH",
            Gate.Prepare(Context(), Request() with { Boundary = null }).Code);
        Assert.Equal("RECOVERY_EVIDENCE_REQUIRED",
            Gate.Prepare(Context(), Request(Candidate() with
            {
                SandboxRestoreVerified = false
            })).Code);
    }
}
