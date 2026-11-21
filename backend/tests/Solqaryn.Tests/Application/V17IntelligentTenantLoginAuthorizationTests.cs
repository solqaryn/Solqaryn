using Solqaryn.Application.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class V17IntelligentTenantLoginAuthorizationTests
{
    private static V17IntelligentTenantLoginAuthorization.Membership Member(
        int tenant = 11, int actor = 7) =>
        new(actor, tenant, $"Empresa {tenant}", true, true, true, true, false);
    private static V17IntelligentTenantLoginAuthorization.TrustedSnapshot Snapshot(
        params V17IntelligentTenantLoginAuthorization.Membership[] memberships) =>
        new(7, true, true, true, false, false, null,
            null, null, false, false, memberships);

    private sealed class Reader : ITrustedTenantLoginAuthorityReader
    {
        public V17IntelligentTenantLoginAuthorization.TrustedSnapshot? Current =
            Snapshot(Member());
        public Exception? Error;
        public int Reads;
        public ValueTask<V17IntelligentTenantLoginAuthorization.TrustedSnapshot?>
            ReadCurrentAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Reads++;
            if (Error is not null) throw Error;
            return ValueTask.FromResult(Current);
        }
    }
    private static V17IntelligentTenantLoginAuthorization Gate(Reader r) => new(r);

    [Fact]
    public async Task OneEnterpriseDirectWithoutSelectorAndNeverARealGrant()
    {
        var d = await Gate(new Reader()).ResolveAsync(new());
        Assert.Equal("ONE_TENANT_REISSUE_REQUIRED", d.Code);
        Assert.Equal(11, d.ProposedTenantId);
        Assert.Empty(d.Choices);
        Assert.True(d.RequiresAuthenticatedBackendReissue);
        Assert.False(d.IssuesJwt);
        Assert.False(d.AuthorizesResourceAccess);
        Assert.False(d.InstallsTenantContext);
    }

    [Fact]
    public async Task MultipleEnterprisesOnlyShowFreshAllowedMemberships()
    {
        var r = new Reader { Current = Snapshot(Member(22), Member(11)) };
        var d = await Gate(r).ResolveAsync(new());
        Assert.Equal("SELECT_VERIFIED_TENANT", d.Code);
        Assert.Null(d.ProposedTenantId);
        Assert.Equal(new[] { 11, 22 }, d.Choices.Select(x => x.TenantId));
        Assert.Equal(1, r.Reads);
    }

    [Fact]
    public async Task SameUserPersistedPreferenceAutomaticallySelects()
    {
        var s = Snapshot(Member(11), Member(22)) with {
            PreferredTenantId = 22, PreferenceOwnerActorId = 7,
            PreferenceFromPersistence = true
        };
        var d = await Gate(new Reader { Current = s }).ResolveAsync(new());
        Assert.Equal("PREFERENCE_REISSUE_REQUIRED", d.Code);
        Assert.Equal(22, d.ProposedTenantId);
        Assert.Empty(d.Choices);
    }

    [Fact]
    public async Task RevokedForeignUserOrUnknownPreferenceNeverGrants()
    {
        var baseSnapshot = Snapshot(Member(11), Member(22)) with {
            PreferredTenantId = 22, PreferenceOwnerActorId = 7,
            PreferenceFromPersistence = true
        };
        foreach (var s in new[] {
            baseSnapshot with { PreferenceOwnerActorId = 8 },
            baseSnapshot with { PreferenceRevoked = true },
            baseSnapshot with { PreferredTenantId = 99 }
        })
        {
            var d = await Gate(new Reader { Current = s }).ResolveAsync(new());
            Assert.Equal("SELECT_VERIFIED_TENANT", d.Code);
            Assert.True(d.DiscardStalePreference);
            Assert.Null(d.ProposedTenantId);
        }
    }

    [Fact]
    public async Task ZeroMembershipDeniesOrRequiresSeparateGlobalAdapter()
    {
        var ordinary = await Gate(new Reader { Current = Snapshot() }).ResolveAsync(new());
        Assert.Equal("NO_AUTHORIZED_ENTERPRISE", ordinary.Code);
        var global = await Gate(new Reader {
            Current = Snapshot() with { GlobalPrivilegeSeparatelyVerified = true }
        }).ResolveAsync(new());
        Assert.Equal("GLOBAL_CONSOLE_AUTHENTICATED_ADAPTER_REQUIRED", global.Code);
        Assert.False(global.AuthorizesGlobalConsole);
    }

    [Fact]
    public async Task RevokedRoleTenantOrMembershipDoesNotBecomeChoice()
    {
        var s = Snapshot(Member(11), Member(22) with { Revoked = true },
            Member(33) with { TenantActive = false },
            Member(44) with { CurrentRelationalRoleVerified = false });
        var d = await Gate(new Reader { Current = s }).ResolveAsync(new());
        Assert.Equal("ONE_TENANT_REISSUE_REQUIRED", d.Code);
        Assert.Equal(11, d.ProposedTenantId);
    }

    [Fact]
    public async Task ForeignTenantSelectionDenied()
    {
        var d = await Gate(new Reader()).ResolveAsync(new(SelectedTenantId: 22));
        Assert.Equal("FOREIGN_OR_REVOKED_TENANT_DENIED", d.Code);
        Assert.Null(d.ProposedTenantId);
    }

    [Fact]
    public async Task ClientGlobalHostAndMembershipClaimsNeverReachReader()
    {
        var r = new Reader();
        foreach (var request in new[] {
            new V17IntelligentTenantLoginAuthorization.Request(ClientClaimsGlobalRole: true),
            new V17IntelligentTenantLoginAuthorization.Request(ClientClaimsMembership: true),
            new V17IntelligentTenantLoginAuthorization.Request(ClientClaimsHostAuthority: true),
            new V17IntelligentTenantLoginAuthorization.Request(SelectedTenantId: 0)
        })
        {
            Assert.Null((await Gate(r).ResolveAsync(request)).ProposedTenantId);
        }
        Assert.Equal(0, r.Reads);
    }

    [Fact]
    public async Task VerifiedExistingSessionReusesOnlySameEnterprise()
    {
        var s = Snapshot(Member(11), Member(22)) with {
            SessionValid = true, SessionTenantId = 11
        };
        var d = await Gate(new Reader { Current = s }).ResolveAsync(new());
        Assert.Equal("CURRENT_SESSION_BACKEND_CHECK_REQUIRED", d.Code);
        Assert.Equal(11, d.ProposedTenantId);
        Assert.Equal("EXPLICIT_SWITCH_REQUIRED",
            (await Gate(new Reader { Current = s }).ResolveAsync(new(22))).Code);
    }

    [Fact]
    public async Task ExpiredOrRevokedSessionFailsClosedInsteadOfFallback()
    {
        foreach (var s in new[] {
            Snapshot(Member(11)) with { SessionTenantId = 11, SessionValid = false },
            Snapshot(Member(11)) with { SessionTenantId = 22, SessionValid = true }
        })
        {
            Assert.Equal("STALE_OR_REVOKED_SESSION_DENIED",
                (await Gate(new Reader { Current = s }).ResolveAsync(new())).Code);
        }
    }

    [Fact]
    public async Task ExplicitSwitchRequiresFreshSessionAndCachePurge()
    {
        var r = new Reader { Current = Snapshot(Member(11), Member(22)) };
        Assert.Equal("ACTIVE_SESSION_REQUIRED_TO_SWITCH",
            (await Gate(r).ResolveAsync(new(22, SwitchingEnterprise: true))).Code);
        r.Current = r.Current with { SessionValid = true, SessionTenantId = 11 };
        var d = await Gate(r).ResolveAsync(new(22, SwitchingEnterprise: true));
        Assert.Equal("SWITCH_REISSUE_REQUIRED", d.Code);
        Assert.True(d.RequiresOldTenantCachePurge);
        Assert.False(d.InstallsTenantContext);
        Assert.Equal("FOREIGN_OR_REVOKED_TENANT_DENIED",
            (await Gate(r).ResolveAsync(new(99, SwitchingEnterprise: true))).Code);
    }

    [Fact]
    public async Task MixedActorAndDuplicateTenantSnapshotsFailClosed()
    {
        var mixed = await Gate(new Reader {
            Current = Snapshot(Member(11), Member(22, actor: 8))
        }).ResolveAsync(new());
        Assert.Equal("CROSS_ACTOR_MEMBERSHIP_SNAPSHOT_DENIED", mixed.Code);
        var duplicated = await Gate(new Reader {
            Current = Snapshot(Member(11), Member(11))
        }).ResolveAsync(new());
        Assert.Equal("AMBIGUOUS_OR_INVALID_ACTIVE_MEMBERSHIP", duplicated.Code);
    }

    [Fact]
    public async Task UnverifiedJwtUserOrStoreNeverSelectEnterprise()
    {
        foreach (var s in new[] {
            Snapshot(Member()) with { JwtValidatedByBackend = false },
            Snapshot(Member()) with { UserActive = false },
            Snapshot(Member()) with { AuthorityStoreAvailable = false },
            Snapshot(Member()) with { ActorId = 0 },
            Snapshot(Member()) with { Memberships = null }
        })
        {
            var d = await Gate(new Reader { Current = s }).ResolveAsync(new());
            Assert.Equal("AUTHENTICATED_CURRENT_MEMBERSHIPS_REQUIRED", d.Code);
            Assert.Null(d.ProposedTenantId);
        }
    }

    [Fact]
    public async Task BadDisplayNameAndMoreThan128MembershipsAreRejected()
    {
        Assert.Equal("AMBIGUOUS_OR_INVALID_ACTIVE_MEMBERSHIP",
            (await Gate(new Reader {
                Current = Snapshot(Member() with { DisplayName = "Fake\nCompany" })
            }).ResolveAsync(new())).Code);
        var tooMany = Enumerable.Range(1, 129).Select(i => Member(i)).ToArray();
        Assert.Equal("MEMBERSHIP_SNAPSHOT_CAPACITY_INVALID",
            (await Gate(new Reader { Current = Snapshot(tooMany) }).ResolveAsync(new())).Code);
    }

    private sealed class LateCancellationReader(
        CancellationTokenSource source) : ITrustedTenantLoginAuthorityReader
    {
        public ValueTask<V17IntelligentTenantLoginAuthorization.TrustedSnapshot?>
            ReadCurrentAsync(CancellationToken ignored)
        {
            source.Cancel(); // Deliberately ignore cancellation.
            var snapshot = Snapshot(Member(11), Member(22)) with
            {
                PreferredTenantId = 22,
                PreferenceOwnerActorId = 7,
                PreferenceFromPersistence = true
            };
            return ValueTask.FromResult<
                V17IntelligentTenantLoginAuthorization.TrustedSnapshot?>(snapshot);
        }
    }

    [Fact]
    public async Task CancelledRequestWithNonCooperativeReaderNeverSelectsTenant()
    {
        using var cts = new CancellationTokenSource();
        var gate = new V17IntelligentTenantLoginAuthorization(
            new LateCancellationReader(cts));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await gate.ResolveAsync(new(), cts.Token);
        });
        Assert.True(cts.IsCancellationRequested);
    }

    private sealed class CancellingLazyMemberships(CancellationTokenSource source)
        : IReadOnlyCollection<V17IntelligentTenantLoginAuthorization.Membership>
    {
        public int Count => 2;

        public IEnumerator<V17IntelligentTenantLoginAuthorization.Membership>
            GetEnumerator()
        {
            yield return Member(11);
            // Simulates a lazy trusted reader cancelling DURING enumeration,
            // after ReadCurrentAsync already returned a seemingly valid snapshot.
            source.Cancel();
            yield return Member(22);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            => GetEnumerator();
    }

    [Fact]
    public async Task CancellationDuringLazyMembershipEnumerationNeverProposesPreferredTenant()
    {
        using var cts = new CancellationTokenSource();
        var snapshot = Snapshot() with
        {
            Memberships = new CancellingLazyMemberships(cts),
            PreferredTenantId = 22,
            PreferenceOwnerActorId = 7,
            PreferenceFromPersistence = true
        };
        var gate = Gate(new Reader { Current = snapshot });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await gate.ResolveAsync(new(), cts.Token);
        });
        Assert.True(cts.IsCancellationRequested);
    }

    [Fact]
    public async Task ReaderFailureIsOpaqueAndCancellationPropagates()
    {
        var failed = await Gate(new Reader {
            Error = new InvalidOperationException("sensitive details")
        }).ResolveAsync(new());
        Assert.Equal("CURRENT_AUTHORITY_STORE_UNAVAILABLE", failed.Code);
        Assert.DoesNotContain("sensitive", failed.Code);
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await Gate(new Reader()).ResolveAsync(new(), new CancellationToken(true)));
    }

    // Adversarial authority reader: Count may lie while the iterator continues.
    // The policy must bound work by observed elements and reject torn snapshots.
    private sealed class MisreportedMembershipCount(int claimed, int actual)
        : IReadOnlyCollection<V17IntelligentTenantLoginAuthorization.Membership>
    {
        public int Count => claimed;

        public IEnumerator<V17IntelligentTenantLoginAuthorization.Membership>
            GetEnumerator()
        {
            for (var i = 1; i <= actual; i++)
                yield return Member(i);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            => GetEnumerator();
    }

    [Theory]
    [InlineData(-1, 0, "MEMBERSHIP_SNAPSHOT_CAPACITY_INVALID")]
    [InlineData(1, 2, "MEMBERSHIP_SNAPSHOT_COUNT_MISMATCH")]
    [InlineData(1, 129, "MEMBERSHIP_SNAPSHOT_CAPACITY_INVALID")]
    public async Task DishonestOrTornMembershipCountCannotBypassCapacityOrProposeTenant(
        int declaredCount, int actualCount, string denial)
    {
        var snapshot = Snapshot() with
        {
            Memberships = new MisreportedMembershipCount(declaredCount, actualCount),
            PreferredTenantId = 2,
            PreferenceOwnerActorId = 7,
            PreferenceFromPersistence = true
        };
        var decision = await Gate(new Reader { Current = snapshot }).ResolveAsync(new());
        Assert.Equal(denial, decision.Code);
        Assert.Null(decision.ProposedTenantId);
        Assert.False(decision.IssuesJwt);
        Assert.False(decision.AuthorizesResourceAccess);
    }

    private sealed class FailingAuthorityMemberships(bool failCount, bool failIteration)
        : IReadOnlyCollection<V17IntelligentTenantLoginAuthorization.Membership>
    {
        public int Count => failCount
            ? throw new InvalidOperationException("sensitive authority store detail")
            : 2;

        public IEnumerator<V17IntelligentTenantLoginAuthorization.Membership> GetEnumerator()
        {
            yield return Member(11);
            if (failIteration)
                throw new InvalidOperationException("sensitive partial authority read");
            yield return Member(22);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            => GetEnumerator();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task FaultyLazyMembershipCollectionMustFailClosedWithoutTenant(
        bool failCount, bool failIteration)
    {
        var snapshot = Snapshot() with
        {
            Memberships = new FailingAuthorityMemberships(failCount, failIteration),
            PreferredTenantId = 11,
            PreferenceOwnerActorId = 7,
            PreferenceFromPersistence = true
        };
        var decision = await Gate(new Reader { Current = snapshot }).ResolveAsync(new());
        Assert.Equal("CURRENT_AUTHORITY_STORE_UNAVAILABLE", decision.Code);
        Assert.Null(decision.ProposedTenantId);
        Assert.Empty(decision.Choices);
        Assert.False(decision.IssuesJwt);
        Assert.False(decision.AuthorizesResourceAccess);
        Assert.False(decision.RequiresAuthenticatedBackendReissue);
    }
}
