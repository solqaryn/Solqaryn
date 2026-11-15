using Gate = Solqaryn.Application.AutomationV5.V17IntelligentTenantLoginAuthorization;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C0201TenantPreferenceIsolatedSnapshotAdapterTests
{
    private const int Actor = 42;
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";

    private static Gate.Membership Member(
        int tenant = 11, int actor = Actor) =>
        new(actor, tenant, $"Empresa {tenant}", true, true, true, true, false);

    private static Gate.TrustedSnapshot Snapshot(params Gate.Membership[] members) =>
        new(Actor, true, true, true, false, false, null,
            null, null, false, false, members);

    private static C0201TenantPreferenceIsolatedSnapshotAdapter Adapter(
        Gate.TrustedSnapshot? snapshot = null) =>
        new(Actor, 7, Sha, snapshot ?? Snapshot(Member()));

    private static async Task<Gate.Decision> Check(
        C0201TenantPreferenceIsolatedSnapshotAdapter adapter,
        Gate.Request? request = null, int actor = Actor, long revision = 7,
        string? sha = Sha) =>
        await adapter.EvaluateReadOnlyAsync(request ?? new(), actor, revision, sha);

    [Fact]
    public async Task SingleTenantNeverGrantsAccessOrIssuesToken()
    {
        var a = Adapter();
        var decision = await Check(a);
        Assert.Equal("ONE_TENANT_REISSUE_REQUIRED", decision.Code);
        Assert.Equal(11, decision.ProposedTenantId);
        Assert.True(decision.RequiresAuthenticatedBackendReissue);
        Assert.False(decision.AuthorizesResourceAccess);
        Assert.False(decision.IssuesJwt);
        Assert.False(decision.InstallsTenantContext);
        Assert.False(a.IsLivePersistenceProvider);
        Assert.False(a.ReadsOrWritesMysql);
        Assert.False(a.WritesPreference);
        Assert.False(a.PerformsTenantSwitch);
        Assert.False(a.AuthorizesResourceAccess);
        Assert.False(a.IssuesJwt);
    }

    [Fact]
    public async Task StoredPreferenceForSameActorNeedsBackendReissue()
    {
        var snapshot = Snapshot(Member(11), Member(22)) with
        {
            PreferredTenantId = 22,
            PreferenceOwnerActorId = Actor,
            PreferenceFromPersistence = true
        };
        var result = await Check(Adapter(snapshot));
        Assert.Equal("PREFERENCE_REISSUE_REQUIRED", result.Code);
        Assert.Equal(22, result.ProposedTenantId);
        Assert.Empty(result.Choices);
    }

    [Fact]
    public async Task PreferenceFromOtherBrowserUserIsDiscarded()
    {
        var snapshot = Snapshot(Member(11), Member(22)) with
        {
            PreferredTenantId = 22,
            PreferenceOwnerActorId = 99,
            PreferenceFromPersistence = true
        };
        var result = await Check(Adapter(snapshot));
        Assert.Equal("SELECT_VERIFIED_TENANT", result.Code);
        Assert.Null(result.ProposedTenantId);
        Assert.True(result.DiscardStalePreference);
    }

    [Fact]
    public async Task RevokedAndUnknownPreferencesNeverBecomeAuthority()
    {
        var snapshot = Snapshot(Member(11), Member(22)) with
        {
            PreferredTenantId = 22,
            PreferenceOwnerActorId = Actor,
            PreferenceFromPersistence = true,
            PreferenceRevoked = true
        };
        Assert.Equal("SELECT_VERIFIED_TENANT",
            (await Check(Adapter(snapshot))).Code);
        Assert.True((await Check(Adapter(snapshot))).DiscardStalePreference);
        var unknown = snapshot with { PreferredTenantId = 99, PreferenceRevoked = false };
        Assert.True((await Check(Adapter(unknown))).DiscardStalePreference);
    }

    [Fact]
    public async Task WrongObservedActorFailsBeforeReadingPreference()
    {
        var result = await Check(Adapter(), actor: 99);
        Assert.Equal("ISOLATED_PREFERENCE_ACTOR_MISMATCH", result.Code);
        Assert.Null(result.ProposedTenantId);
    }

    [Fact]
    public async Task StaleRevisionAndShaCannotSelectTenant()
    {
        Assert.Equal("ISOLATED_PREFERENCE_REVISION_MISMATCH",
            (await Check(Adapter(), revision: 8)).Code);
        Assert.Equal("ISOLATED_PREFERENCE_SOURCE_SHA_MISMATCH",
            (await Check(Adapter(), sha: new string('f', 40))).Code);
    }

    [Fact]
    public async Task CapturedActorMismatchDenied()
    {
        var mismatch = Snapshot(Member()) with { ActorId = 99 };
        Assert.Equal("ISOLATED_PREFERENCE_ACTOR_MISMATCH",
            (await Check(Adapter(mismatch))).Code);
    }

    [Fact]
    public async Task ClientSelectedForeignTenantDenied()
    {
        var d = await Check(Adapter(), new Gate.Request(SelectedTenantId: 22));
        Assert.Equal("FOREIGN_OR_REVOKED_TENANT_DENIED", d.Code);
        Assert.Null(d.ProposedTenantId);
    }

    [Fact]
    public async Task RevokedMembershipDoesNotEnterChoices()
    {
        var snapshot = Snapshot(Member(11), Member(22) with { Revoked = true });
        var d = await Check(Adapter(snapshot));
        Assert.Equal("ONE_TENANT_REISSUE_REQUIRED", d.Code);
        Assert.Equal(11, d.ProposedTenantId);
    }

    [Fact]
    public async Task UntrustedBrowserRoleHostAndMembershipHintsDenied()
    {
        foreach (var request in new[]
        {
            new Gate.Request(ClientClaimsGlobalRole: true),
            new Gate.Request(ClientClaimsMembership: true),
            new Gate.Request(ClientClaimsHostAuthority: true)
        })
        {
            var result = await Check(Adapter(), request);
            Assert.Equal("CLIENT_OR_MISSING_AUTHORITY_DENIED", result.Code);
            Assert.Null(result.ProposedTenantId);
        }
    }

    [Fact]
    public async Task ZeroMembershipDoesNotGrantGlobalConsole()
    {
        var result = await Check(Adapter(Snapshot()));
        Assert.Equal("NO_AUTHORIZED_ENTERPRISE", result.Code);
        var global = await Check(Adapter(
            Snapshot() with { GlobalPrivilegeSeparatelyVerified = true }));
        Assert.Equal("GLOBAL_CONSOLE_AUTHENTICATED_ADAPTER_REQUIRED", global.Code);
        Assert.False(global.AuthorizesGlobalConsole);
    }

    [Fact]
    public async Task CallerMutationAfterCaptureDoesNotChangeEligibleTenants()
    {
        var members = new List<Gate.Membership> { Member(11), Member(22) };
        var adapter = Adapter(Snapshot(members.ToArray()) with { Memberships = members });
        members.Clear();
        members.Add(Member(99));
        var result = await Check(adapter);
        Assert.Equal("SELECT_VERIFIED_TENANT", result.Code);
        Assert.Equal(new[] { 11, 22 }, result.Choices.Select(x => x.TenantId));
    }

    [Fact]
    public async Task RevokedOrStaleSessionIsDenied()
    {
        var old = Snapshot(Member(11)) with { SessionTenantId = 11, SessionValid = false };
        Assert.Equal("STALE_OR_REVOKED_SESSION_DENIED",
            (await Check(Adapter(old))).Code);
        var foreign = old with { SessionValid = true, SessionTenantId = 22 };
        Assert.Equal("STALE_OR_REVOKED_SESSION_DENIED",
            (await Check(Adapter(foreign))).Code);
    }

    [Fact]
    public async Task RoleRevocationAndDuplicateMembershipFailClosed()
    {
        var role = Snapshot(Member() with { CurrentRelationalRoleVerified = false });
        Assert.Equal("NO_AUTHORIZED_ENTERPRISE",
            (await Check(Adapter(role))).Code);
        var duplicates = Snapshot(Member(11), Member(11));
        Assert.Equal("AMBIGUOUS_OR_INVALID_ACTIVE_MEMBERSHIP",
            (await Check(Adapter(duplicates))).Code);
    }

    [Fact]
    public async Task CancellationPropagatesAndNullRequestIsDenied()
    {
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await Adapter().EvaluateReadOnlyAsync(
                new Gate.Request(), Actor, 7, Sha, new CancellationToken(true)));
        var d = await Adapter().EvaluateReadOnlyAsync(null, Actor, 7, Sha);
        Assert.Equal("ISOLATED_PREFERENCE_REQUEST_REQUIRED", d.Code);
    }

    [Fact]
    public void InvalidConstructionNeverCreatesAnAdapter()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new C0201TenantPreferenceIsolatedSnapshotAdapter(0, 7, Sha, Snapshot(Member())));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new C0201TenantPreferenceIsolatedSnapshotAdapter(Actor, 0, Sha, Snapshot(Member())));
        Assert.Throws<ArgumentException>(() =>
            new C0201TenantPreferenceIsolatedSnapshotAdapter(Actor, 7, "short", Snapshot(Member())));
        Assert.Throws<ArgumentNullException>(() =>
            new C0201TenantPreferenceIsolatedSnapshotAdapter(Actor, 7, Sha, null!));
    }
}
