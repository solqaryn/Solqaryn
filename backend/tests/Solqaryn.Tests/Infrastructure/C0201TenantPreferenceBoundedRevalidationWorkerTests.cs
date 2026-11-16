using Gate = Solqaryn.Application.AutomationV5.V17IntelligentTenantLoginAuthorization;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C0201TenantPreferenceBoundedRevalidationWorkerTests
{
    private const int Actor = 42;
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";

    private static Gate.Membership Member(int tenant = 11, bool revoked = false) =>
        new(Actor, tenant, "Private tenant display", true, true, true, true, revoked);

    private static Gate.TrustedSnapshot Snapshot(params Gate.Membership[] members) =>
        new(Actor, true, true, true, false, false, null,
            null, null, false, false, members);

    private static C0201TenantPreferenceBoundedRevalidationWorker Worker(
        Gate.TrustedSnapshot? snapshot = null) =>
        new(new C0201TenantPreferenceIsolatedSnapshotAdapter(
            Actor, 7, Sha, snapshot ?? Snapshot(Member())));

    private static C0201TenantPreferenceBoundedRevalidationWorker.Input Input(
        string key = "attempt-001", Gate.Request? request = null) =>
        new(key, request ?? new Gate.Request());

    [Fact]
    public async Task ValidSingleTenantBatchReturnsPreviewNotAccess()
    {
        var result = await Worker().PreviewAsync(
            new[] { Input("attempt-001"), Input("attempt-002") }, Actor, 7, Sha);
        Assert.Equal("PREVIEW_ONLY_AUTHENTICATED_BACKEND_REISSUE_REQUIRED", result.Code);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item => {
            Assert.Equal("ONE_TENANT_REISSUE_REQUIRED", item.DecisionCode);
            Assert.Equal(11, item.ProposedTenantId);
            Assert.Equal(0, item.ChoiceCount);
        });
        Assert.False(result.IssuesJwt);
        Assert.False(result.AuthorizesTenantOrResourceAccess);
        Assert.False(result.StartsHostedWorker);
        Assert.False(result.WritesTenantPreference);
        Assert.False(result.SwitchesTenantSession);
        Assert.False(result.WritesDurableQueue);
        Assert.False(result.ReadsOrWritesMySql);
        Assert.False(result.CertifiesProduction);
    }

    [Fact]
    public async Task MultipleTenantsRequireBackendChoiceWithoutExposingNames()
    {
        var d = await Worker(Snapshot(Member(11), Member(22))).PreviewAsync(
            new[] { Input() }, Actor, 7, Sha);
        Assert.Single(d.Items);
        Assert.Equal("SELECT_VERIFIED_TENANT", d.Items[0].DecisionCode);
        Assert.Null(d.Items[0].ProposedTenantId);
        Assert.Equal(2, d.Items[0].ChoiceCount);
        Assert.DoesNotContain("Private tenant", d.Items[0].DecisionCode);
    }

    [Fact]
    public async Task RevokedOnlyMembershipYieldsNoPartialRows()
    {
        var result = await Worker(Snapshot(Member(revoked: true))).PreviewAsync(
            new[] { Input() }, Actor, 7, Sha);
        Assert.Equal("PREVIEW_AUTHORITY_DENIED", result.Code);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task ForeignTenantDoesNotLeakProposedChoice()
    {
        var result = await Worker().PreviewAsync(
            new[] { Input(request: new Gate.Request(SelectedTenantId: 22)) },
            Actor, 7, Sha);
        Assert.Equal("PREVIEW_AUTHORITY_DENIED", result.Code);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task WrongActorRevisionOrShaFailsClosed()
    {
        var worker = Worker();
        var items = new[] { Input() };
        Assert.Equal("PREVIEW_AUTHORITY_DENIED",
            (await worker.PreviewAsync(items, 99, 7, Sha)).Code);
        Assert.Equal("PREVIEW_AUTHORITY_DENIED",
            (await worker.PreviewAsync(items, Actor, 8, Sha)).Code);
        Assert.Equal("PREVIEW_AUTHORITY_DENIED",
            (await worker.PreviewAsync(items, Actor, 7, OtherSha)).Code);
        Assert.Equal("PREVIEW_TRUSTED_CONTEXT_REQUIRED",
            (await worker.PreviewAsync(items, Actor, 7, "bad")).Code);
    }

    [Fact]
    public async Task BrowserAuthorityHintsDeniedBeforePreview()
    {
        foreach (var request in new[] {
            new Gate.Request(ClientClaimsGlobalRole: true),
            new Gate.Request(ClientClaimsMembership: true),
            new Gate.Request(ClientClaimsHostAuthority: true)
        })
        {
            var d = await Worker().PreviewAsync(
                new[] { Input(request: request) }, Actor, 7, Sha);
            Assert.Equal("PREVIEW_UNTRUSTED_REQUEST_DENIED", d.Code);
            Assert.Empty(d.Items);
        }
    }

    [Fact]
    public async Task DuplicateAttemptAndConflictingSelectionsDeniedAtomically()
    {
        var worker = Worker(Snapshot(Member(11), Member(22)));
        Assert.Equal("PREVIEW_DUPLICATE_ATTEMPT_DENIED",
            (await worker.PreviewAsync(
                new[] { Input("attempt-001"), Input("attempt-001") },
                Actor, 7, Sha)).Code);
        Assert.Equal("PREVIEW_MIXED_SELECTION_DENIED",
            (await worker.PreviewAsync(
                new[] {
                    Input("attempt-001", new Gate.Request(SelectedTenantId: 11)),
                    Input("attempt-002", new Gate.Request(SelectedTenantId: 22))
                }, Actor, 7, Sha)).Code);
    }

    [Fact]
    public async Task EmptyOversizedAndMissingRequestsAreDenied()
    {
        var worker = Worker();
        Assert.Equal("PREVIEW_BATCH_REQUIRED_OR_TOO_LARGE",
            (await worker.PreviewAsync(null, Actor, 7, Sha)).Code);
        Assert.Equal("PREVIEW_BATCH_REQUIRED_OR_TOO_LARGE",
            (await worker.PreviewAsync(Array.Empty<C0201TenantPreferenceBoundedRevalidationWorker.Input>(),
                Actor, 7, Sha)).Code);
        var large = Enumerable.Range(0, 13).Select(i => Input($"attempt-{i:000}")).ToArray();
        Assert.Equal("PREVIEW_BATCH_REQUIRED_OR_TOO_LARGE",
            (await worker.PreviewAsync(large, Actor, 7, Sha)).Code);
        Assert.Equal("PREVIEW_UNTRUSTED_REQUEST_DENIED",
            (await worker.PreviewAsync(
                new[] { new C0201TenantPreferenceBoundedRevalidationWorker.Input("attempt-001", null) },
                Actor, 7, Sha)).Code);
    }

    [Fact]
    public async Task MissingSwitchTargetOrMalformedAttemptDenied()
    {
        var worker = Worker();
        Assert.Equal("PREVIEW_UNTRUSTED_REQUEST_DENIED",
            (await worker.PreviewAsync(
                new[] { Input("bad key") }, Actor, 7, Sha)).Code);
        Assert.Equal("PREVIEW_UNTRUSTED_REQUEST_DENIED",
            (await worker.PreviewAsync(
                new[] { Input(request: new Gate.Request(SwitchingEnterprise: true)) },
                Actor, 7, Sha)).Code);
    }

    [Fact]
    public async Task CancelledBatchThrowsWithoutReturningPartialPreview()
    {
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await Worker().PreviewAsync(new[] { Input() }, Actor, 7, Sha,
                new CancellationToken(true)));
    }

    [Fact]
    public void MissingAdapterFailsFast()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new C0201TenantPreferenceBoundedRevalidationWorker(null!));
    }
}
