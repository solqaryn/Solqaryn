using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

/// <summary>
/// C14-02.M05.02: contract errors on the existing Application preflight only.
/// No HTTP endpoint, real JWT, tenant database, permission grant or deployment is certified.
/// </summary>
public sealed class C1402M0502SafeApiContractTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static V5SecurityAcceptanceDomainRules.VerifiedAuthority Authority() =>
        new(7, 11, 2, Sha, true, true, true, true, true, false, false);
    private static V5SecurityAcceptanceDomainRules.PersistedTarget Target() =>
        new(11, 2, 11, true, true, false, false);
    private static V5SecurityAcceptanceDomainRules.Attempt Attempt() =>
        new(V5SecurityAcceptanceDomainRules.Surface.BusinessRead, null, Sha, false, false, false);
    private static C1402SecurityAcceptanceAuthorizationGate.VerifiedEvidence Evidence() =>
        new("safe-tenant-A", Sha, true, true, true, true, true, "git-proof:dev",
            Authority(), Target(), Attempt());
    private static C1402SecurityAcceptanceAuthorizationGate.Request Request() =>
        new("safe-tenant-A", Sha, V5SecurityAcceptanceDomainRules.Surface.BusinessRead);

    private sealed class Reader(
        C1402SecurityAcceptanceAuthorizationGate.VerifiedEvidence? evidence,
        bool fail = false) : IC1402SecurityAcceptanceEvidenceReader
    {
        public int Calls { get; private set; }

        public ValueTask<C1402SecurityAcceptanceAuthorizationGate.VerifiedEvidence?> ReadAsync(
            string resourceKey, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            if (fail)
                throw new InvalidOperationException("sensitive-internal-source-details");
            return ValueTask.FromResult(evidence);
        }
    }

    [Fact]
    public async Task ValidSnapshotIsStillNotHttpOrInstitutionalAuthorization()
    {
        var reader = new Reader(Evidence());
        var decision = await new C1402SecurityAcceptanceAuthorizationGate(reader).CheckAsync(Request());
        Assert.Equal(1, reader.Calls);
        Assert.Equal("AUTHENTICATED_SECURITY_ACCEPTANCE_ADAPTER_REQUIRED", decision.Code);
        Assert.True(decision.RequiresSeparateAuthenticatedAdapter);
        Assert.False(decision.GrantsTenantOrResourceAccess);
        Assert.False(decision.AllowsInstitutionalWrite);
        Assert.False(decision.AllowsRestoreOrDeployment);
        Assert.False(decision.CertifiesSecurityAcceptance);
    }

    [Fact]
    public async Task InvalidDtoAndBoundaryAreRejectedBeforeEvidenceIo()
    {
        var reader = new Reader(Evidence());
        var gate = new C1402SecurityAcceptanceAuthorizationGate(reader);
        var invalid = new C1402SecurityAcceptanceAuthorizationGate.Request?[]
        {
            null, Request() with { ResourceKey = "" },
            Request() with { ResourceKey = "../tenant-B" },
            Request() with { ResourceKey = new string('a', 97) },
            Request() with { ResourceKey = "tenant-B:secret" },
            Request() with { ExpectedCommitSha = "short" },
            Request() with { ExpectedCommitSha = new string('z', 40) },
            Request() with { Surface = (V5SecurityAcceptanceDomainRules.Surface)999 }
        };
        foreach (var request in invalid)
        {
            var verdict = await gate.CheckAsync(request);
            Assert.Equal("INVALID_REQUEST_OR_SHA", verdict.Code);
            Assert.False(verdict.GrantsTenantOrResourceAccess);
        }
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task ClientTenantRbacBypassAndRestoreNeverReachTrustedReader()
    {
        var reader = new Reader(Evidence());
        var gate = new C1402SecurityAcceptanceAuthorizationGate(reader);
        var malicious = new[]
        {
            Request() with { ClientControlsTenantOrRole = true },
            Request() with { RequestsBypassOrRestore = true },
            Request() with { Surface = V5SecurityAcceptanceDomainRules.Surface.BackupRestore }
        };
        foreach (var input in malicious)
            Assert.Equal("CLIENT_AUTHORITY_OR_RESTORE_OVERRIDE_DENIED",
                (await gate.CheckAsync(input)).Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task WrongResourceAndSourceShaCannotLeakOtherTenants()
    {
        foreach (var evidence in new[]
        {
            Evidence() with { PersistedResourceKey = "tenant-B" },
            Evidence() with { ObservedSourceSha = new string('a', 40) },
            Evidence() with { ServerBackendEvidenceVerified = false }
        })
        {
            var verdict = await new C1402SecurityAcceptanceAuthorizationGate(
                new Reader(evidence)).CheckAsync(Request());
            Assert.Equal("PERSISTED_RESOURCE_AND_SOURCE_REQUIRED", verdict.Code);
            Assert.False(verdict.GrantsTenantOrResourceAccess);
        }
    }

    [Fact]
    public async Task RevokedMembershipTenantBAndOldPermissionsFailClosed()
    {
        foreach (var evidence in new[]
        {
            Evidence() with { CurrentMembershipVerified = false },
            Evidence() with { CurrentExplicitRbacVerified = false },
            Evidence() with { CurrentJwtVerified = false },
            Evidence() with { Target = Target() with { OwnerTenantId = 12 } },
            Evidence() with { Target = Target() with { AccessRevoked = true } }
        })
        {
            var verdict = await new C1402SecurityAcceptanceAuthorizationGate(
                new Reader(evidence)).CheckAsync(Request());
            Assert.Equal("CURRENT_AUTHORIZATION_TENANT_AND_AUDIT_REQUIRED", verdict.Code);
            Assert.False(verdict.CertifiesSecurityAcceptance);
        }
    }

    [Fact]
    public async Task HostFileAndJobNegativeContractsRemainPrivate()
    {
        var scenarios = new[]
        {
            (V5SecurityAcceptanceDomainRules.Surface.HostResolve,
                Evidence() with {
                    Attempt = Attempt() with { Surface = V5SecurityAcceptanceDomainRules.Surface.HostResolve },
                    Target = Target() with { HostBindingVerified = false }
                }, "UNTRUSTED_OR_CROSS_TENANT_HOST"),
            (V5SecurityAcceptanceDomainRules.Surface.FileDownload,
                Evidence() with {
                    Attempt = Attempt() with { Surface = V5SecurityAcceptanceDomainRules.Surface.FileDownload },
                    Target = Target() with { SignedLinkExpired = true }
                }, "FILE_LINK_EXPIRED"),
            (V5SecurityAcceptanceDomainRules.Surface.BackgroundJob,
                Evidence() with {
                    Attempt = Attempt() with { Surface = V5SecurityAcceptanceDomainRules.Surface.BackgroundJob }
                }, "JOB_AUTHORITY_OR_IDEMPOTENCY_REQUIRED")
        };
        foreach (var (surface, evidence, code) in scenarios)
        {
            var verdict = await new C1402SecurityAcceptanceAuthorizationGate(
                new Reader(evidence)).CheckAsync(Request() with { Surface = surface });
            Assert.Equal(code, verdict.Code);
            Assert.False(verdict.GrantsTenantOrResourceAccess);
        }
    }

    [Fact]
    public async Task ReaderFailureReturnsOnlyFixedSafeError()
    {
        var reader = new Reader(Evidence(), fail: true);
        var verdict = await new C1402SecurityAcceptanceAuthorizationGate(reader).CheckAsync(Request());
        Assert.Equal(1, reader.Calls);
        Assert.Equal("TRUSTED_EVIDENCE_SOURCE_UNAVAILABLE", verdict.Code);
        Assert.False(verdict.CertifiesSecurityAcceptance);
    }

    [Fact]
    public async Task CancellationPropagatesWithoutReadingEvidence()
    {
        var reader = new Reader(Evidence());
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await new C1402SecurityAcceptanceAuthorizationGate(reader).CheckAsync(Request(), cts.Token));
        Assert.Equal(0, reader.Calls);
    }
}
