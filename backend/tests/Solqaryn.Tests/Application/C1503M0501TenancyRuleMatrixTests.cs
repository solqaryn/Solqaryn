using Solqaryn.Application.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5TenancySecurityDomainRules;
using Gate = Solqaryn.Application.AutomationV5.V5TenancySecurityAuthorizationGate;

namespace Solqaryn.Tests.Application;

/// <summary>
/// C15-03.M05.01: synthetic evidence against real Domain/Application guards.
/// Never certifies live JWT, HTTP, MySQL or a distributed job.
/// </summary>
public sealed class C1503M0501TenancyRuleMatrixTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";

    private sealed class Reader : IV5TenancySecurityVerifiedAuthorizationEvidenceReader
    {
        public Gate.VerifiedEvidence? Current { get; set; } = Evidence();
        public int Calls { get; private set; }
        public ValueTask<Gate.VerifiedEvidence?> ReadVerifiedAsync(int resourceId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            return ValueTask.FromResult(Current);
        }
    }

    private static Gate.VerifiedEvidence Evidence() => new(
        ObservedSourceSha: Sha,
        VerifiedResourceId: 101,
        VerifiedTenantId: 7,
        VerifiedActorId: 11,
        ServerAuthenticationVerified: true,
        CurrentMembershipVerified: true,
        ExplicitPermissionVerified: true,
        AuthorizationReadFromTrustedBackend: true,
        PermissionRevoked: false,
        Authority: new Rules.TrustedAuthority(7, 11, 3, true, true, true, true),
        Resource: new Rules.PersistedResource(7, 7, 3, true, false, false));

    private static Gate.Request Request(Rules.Surface surface) => new(
        ResourceId: 101, Surface: surface, ExpectedSourceSha: Sha,
        RequestsTenantFilterBypass: false, AttemptsCrossTenantEnumeration: false,
        RequestsInstitutionalWrite: false, IdempotencyKey: "c1503-m0501-idempotent");

    private static void DeniesCapabilities(Gate.Result result)
    {
        Assert.False(result.GrantsResourceRead);
        Assert.False(result.GrantsFileDownload);
        Assert.False(result.GrantsBackgroundJob);
        Assert.False(result.GrantsControlPlane);
        Assert.False(result.GrantsInstitutionalWrite);
        Assert.False(result.AllowsTenantFilterBypass);
    }

    [Fact]
    public async Task PositivePreflightAcrossFiveSurfacesStillNeverGrantsExecution()
    {
        foreach (var surface in Enum.GetValues<Rules.Surface>())
        {
            var reader = new Reader();
            var result = await new Gate(reader).EvaluateAsync(Request(surface));
            Assert.Equal("AUTHENTICATED_EXECUTION_ADAPTER_REQUIRED", result.Code);
            Assert.True(result.NeedsAuthenticatedExecutionAdapter);
            DeniesCapabilities(result);
            Assert.Equal(1, reader.Calls);
        }
    }

    [Fact]
    public async Task TenantActorAndOwnershipSwapsAreDeniedOnEverySurface()
    {
        foreach (var surface in Enum.GetValues<Rules.Surface>())
        for (int mutation = 0; mutation < 7; mutation++)
        {
            var snapshot = Evidence();
            snapshot = mutation switch
            {
                0 => snapshot with { VerifiedTenantId = 8 },
                1 => snapshot with { Authority = snapshot.Authority! with { AuthenticatedTenantId = 8 } },
                2 => snapshot with { Resource = snapshot.Resource! with { OwnerTenantId = 8 } },
                3 => snapshot with { Resource = snapshot.Resource! with { ParentTenantId = 8 } },
                4 => snapshot with { VerifiedActorId = 12 },
                5 => snapshot with { Authority = snapshot.Authority! with { AuthenticatedActorId = 12 } },
                _ => snapshot with { Resource = snapshot.Resource! with { OwnershipReadFromPersistence = false } }
            };
            var reader = new Reader { Current = snapshot };
            var result = await new Gate(reader).EvaluateAsync(Request(surface));
            Assert.Equal("RESOURCE_SCOPE_OR_IDENTITY_MISMATCH", result.Code);
            DeniesCapabilities(result);
            Assert.Equal(1, reader.Calls);
        }
    }

    [Fact]
    public async Task RevocationAndStaleGrantsDenyEverySurface()
    {
        foreach (var surface in Enum.GetValues<Rules.Surface>())
        for (int mutation = 0; mutation < 5; mutation++)
        {
            var snapshot = Evidence();
            snapshot = mutation switch
            {
                0 => snapshot with { PermissionRevoked = true },
                1 => snapshot with { CurrentMembershipVerified = false },
                2 => snapshot with { ExplicitPermissionVerified = false },
                3 => snapshot with { ServerAuthenticationVerified = false },
                _ => snapshot with { AuthorizationReadFromTrustedBackend = false }
            };
            var result = await new Gate(new Reader { Current = snapshot })
                .EvaluateAsync(Request(surface));
            Assert.Equal("CURRENT_VERIFIED_AUTHORIZATION_REQUIRED", result.Code);
            DeniesCapabilities(result);
        }
    }

    [Fact]
    public async Task BranchFileJobAndControlPlaneRetainDistinctBarriers()
    {
        var wrongBranch = Evidence() with { Resource = Evidence().Resource! with { OwnerBranchId = 9 } };
        Assert.Equal("BRANCH_SCOPE_MISMATCH",
            (await new Gate(new Reader { Current = wrongBranch })
                .EvaluateAsync(Request(Rules.Surface.BranchRead))).Code);

        foreach (var resource in new[]
        {
            Evidence().Resource! with { AccessRevoked = true },
            Evidence().Resource! with { SignedLinkExpired = true }
        })
            Assert.Equal("FILE_ACCESS_REVOKED_OR_EXPIRED",
                (await new Gate(new Reader { Current = Evidence() with { Resource = resource } })
                    .EvaluateAsync(Request(Rules.Surface.FileDownload))).Code);

        foreach (string? key in new string?[] { null, "", "bad key", new string('x', 129) })
            Assert.Equal("SERVICE_JOB_AUTHORITY_REQUIRED",
                (await new Gate(new Reader()).EvaluateAsync(
                    Request(Rules.Surface.BackgroundJob) with { IdempotencyKey = key })).Code);

        var revoked = Evidence() with
        {
            Authority = Evidence().Authority! with { PrivilegedServiceGrantVerified = false }
        };
        foreach (var surface in new[] { Rules.Surface.BackgroundJob, Rules.Surface.ControlPlane })
        {
            var result = await new Gate(new Reader { Current = revoked }).EvaluateAsync(Request(surface));
            Assert.Equal(surface == Rules.Surface.BackgroundJob ?
                "SERVICE_JOB_AUTHORITY_REQUIRED" : "CONTROL_PLANE_AUTHORITY_REQUIRED", result.Code);
            DeniesCapabilities(result);
        }
    }

    [Fact]
    public async Task HostileRequestsFailBeforeEvidenceReader()
    {
        foreach (var surface in Enum.GetValues<Rules.Surface>())
        {
            var reader = new Reader();
            var gate = new Gate(reader);
            var cases = new[]
            {
                (Request(surface) with { ResourceId = 0 }, "RESOURCE_OR_SECURITY_SURFACE_INVALID"),
                (Request(surface) with { ExpectedSourceSha = "short" }, "EXACT_SOURCE_SHA_REQUIRED"),
                (Request(surface) with { RequestsTenantFilterBypass = true }, "TENANT_QUERY_BYPASS_DENIED"),
                (Request(surface) with { AttemptsCrossTenantEnumeration = true }, "TENANT_QUERY_BYPASS_DENIED"),
                (Request(surface) with { RequestsInstitutionalWrite = true }, "INSTITUTIONAL_WRITE_OUT_OF_SCOPE")
            };
            foreach (var (request, code) in cases)
            {
                var result = await gate.EvaluateAsync(request);
                Assert.Equal(code, result.Code);
                DeniesCapabilities(result);
            }
            Assert.Equal(0, reader.Calls);
        }
    }

    [Fact]
    public async Task CancellationStopsBeforeReadingTrustedEvidence()
    {
        var reader = new Reader();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await new Gate(reader).EvaluateAsync(Request(Rules.Surface.BusinessRead), cts.Token));
        Assert.Equal(0, reader.Calls);
    }
}
