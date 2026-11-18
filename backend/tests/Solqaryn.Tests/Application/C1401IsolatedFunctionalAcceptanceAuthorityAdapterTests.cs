using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class C1401IsolatedFunctionalAcceptanceAuthorityAdapterTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static C1401FunctionalAcceptanceAuthorizationGate.VerifiedContext Trusted() =>
        new("functional_acceptance", Sha, 7, 11, true, true, true, true,
            false, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "AF01" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "READ_OWN" });

    private sealed class Source : IC1401TrustedAcceptanceSnapshotSource
    {
        public C1401FunctionalAcceptanceAuthorizationGate.VerifiedContext? Snapshot = Trusted();
        public Exception? Error;
        public int Reads;
        public ValueTask<C1401FunctionalAcceptanceAuthorizationGate.VerifiedContext?> ReadVerifiedAsync(
            string key, CancellationToken cancellationToken)
        {
            Reads++;
            cancellationToken.ThrowIfCancellationRequested();
            if (Error is not null) throw Error;
            return ValueTask.FromResult(Snapshot);
        }
    }

    private static C1401FunctionalAcceptanceAuthorizationGate.Request Intent(
        string caseKey = "AF01", string actionKey = "READ_OWN",
        int tenant = 11) =>
        new("functional_acceptance", Sha, tenant,
            new V5FunctionalAcceptanceDomainRules.AcceptanceEvidence?[]
            {
                new(caseKey, actionKey, 11, Sha, "ci:isolated:proof",
                    true, true, false, false, true, true)
            });

    [Fact]
    public async Task CanonicalSnapshotAllowsOnlyPreflightAndNeverGrantsAccess()
    {
        var source = new Source();
        var bridge = new C1401IsolatedFunctionalAcceptanceAuthorityAdapter(source);
        var decision = await new C1401FunctionalAcceptanceAuthorizationGate(bridge)
            .InspectAsync(Intent());
        Assert.Equal("AUTHENTICATED_FUNCTIONAL_ACCEPTANCE_ADAPTER_REQUIRED", decision.Code);
        Assert.False(decision.GrantsTenantOrResourceAccess);
        Assert.False(decision.AllowsInstitutionalWrite);
        Assert.False(decision.AuthorizesQaOrProduction);
        Assert.Equal(1, source.Reads);
    }

    [Theory]
    [InlineData("af01", "READ_OWN")]
    [InlineData("AF01", "read_own")]
    public async Task SourceComparerCannotAcceptNoncanonicalCaseOrAction(
        string caseKey, string actionKey)
    {
        var gate = new C1401FunctionalAcceptanceAuthorizationGate(
            new C1401IsolatedFunctionalAcceptanceAuthorityAdapter(new Source()));
        var verdict = await gate.InspectAsync(Intent(caseKey, actionKey));
        Assert.Equal("UNAPPROVED_CASE_OR_ACTION", verdict.Code);
        Assert.False(verdict.GrantsTenantOrResourceAccess);
    }

    [Fact]
    public async Task SnapshotCollectionsAreDefensivelyCopiedWithOrdinalComparer()
    {
        var source = new Source();
        var adapter = new C1401IsolatedFunctionalAcceptanceAuthorityAdapter(source);
        var read = await adapter.ReadAsync("functional_acceptance", default);
        Assert.NotNull(read);
        Assert.False(read.ApprovedCaseKeys!.Contains("af01"));
        Assert.False(read.ApprovedActionKeys!.Contains("read_own"));
        ((HashSet<string>)source.Snapshot!.ApprovedCaseKeys!).Add("AF02");
        Assert.False(read.ApprovedCaseKeys.Contains("AF02"));
    }

    [Fact]
    public async Task ForeignTenantIsRejectedByExistingGate()
    {
        var gate = new C1401FunctionalAcceptanceAuthorizationGate(
            new C1401IsolatedFunctionalAcceptanceAuthorityAdapter(new Source()));
        var outcome = await gate.InspectAsync(Intent(tenant: 22));
        Assert.Equal("CROSS_TENANT_CLIENT_ASSERTION_DENIED", outcome.Code);
        Assert.False(outcome.GrantsTenantOrResourceAccess);
    }

    [Fact]
    public async Task RevocationAndMissingServerAuthorityFailClosed()
    {
        var source = new Source();
        var bridge = new C1401IsolatedFunctionalAcceptanceAuthorityAdapter(source);
        foreach (var context in new[] {
            Trusted() with { SessionOrRoleRevoked = true },
            Trusted() with { CurrentBackendJwtVerified = false },
            Trusted() with { CurrentMembershipVerified = false },
            Trusted() with { CurrentRelationalRbacVerified = false },
            Trusted() with { PersistedOwnershipVerified = false },
            Trusted() with { TenantId = 0 },
            Trusted() with { ApprovedCaseKeys = null }
        })
        {
            source.Snapshot = context;
            Assert.Null(await bridge.ReadAsync("functional_acceptance", default));
        }
    }

    [Fact]
    public async Task MismatchedResourceOrInvalidShaNeverBecomesVerified()
    {
        var source = new Source();
        var bridge = new C1401IsolatedFunctionalAcceptanceAuthorityAdapter(source);
        source.Snapshot = Trusted() with { PersistedResourceKey = "another_resource" };
        Assert.Null(await bridge.ReadAsync("functional_acceptance", default));
        source.Snapshot = Trusted() with { SourceCommitSha = "short" };
        Assert.Null(await bridge.ReadAsync("functional_acceptance", default));
    }

    [Fact]
    public async Task InvalidResourceKeyRejectedWithoutReadingProvider()
    {
        var source = new Source();
        var bridge = new C1401IsolatedFunctionalAcceptanceAuthorityAdapter(source);
        Assert.Null(await bridge.ReadAsync("../other", default));
        Assert.Null(await bridge.ReadAsync(new string('x', 97), default));
        Assert.Null(await bridge.ReadAsync("", default));
        Assert.Equal(0, source.Reads);
    }

    [Fact]
    public async Task SourceErrorsReturnNullWithoutLeakingDetails()
    {
        var source = new Source { Error = new InvalidOperationException("private-provider-secret") };
        var bridge = new C1401IsolatedFunctionalAcceptanceAuthorityAdapter(source);
        Assert.Null(await bridge.ReadAsync("functional_acceptance", default));
    }

    [Fact]
    public async Task CancellationPropagatesWithoutProviderCall()
    {
        var source = new Source();
        var bridge = new C1401IsolatedFunctionalAcceptanceAuthorityAdapter(source);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await bridge.ReadAsync("functional_acceptance", new CancellationToken(true)));
        Assert.Equal(0, source.Reads);
    }

    [Fact]
    public async Task SourceCancellationPropagatesForCallerRollback()
    {
        var source = new Source { Error = new OperationCanceledException() };
        var bridge = new C1401IsolatedFunctionalAcceptanceAuthorityAdapter(source);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await bridge.ReadAsync("functional_acceptance", default));
    }
}
