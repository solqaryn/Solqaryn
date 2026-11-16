using Solqaryn.Application.AutomationV5;
using Rules = Solqaryn.Domain.AutomationV5.V5SectorModuleLifecycleDomainRules;
using Fence = Solqaryn.Application.AutomationV5.C0804SectorModuleAuthorizationFence;
using Probe = Solqaryn.Application.AutomationV5.C0804SectorModuleReadOnlyIntegrationBoundary;
using Audit = Solqaryn.Application.AutomationV5.C0804SectorModuleAuditBoundary;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class C0804SectorModuleAuditBoundaryTests
{
    private const string Tenant = "tenant-A";
    private const string Key = "sector.orders";
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";

    private static Rules.VerifiedActorAndPolicy Policy() => new()
    {
        ActorId = "actor-7", VerifiedTenantId = Tenant, EffectiveRoleId = "role-9",
        AuthenticatedJwtVerifiedByBackend = true, CurrentMembershipVerified = true,
        CurrentExplicitRelationalRbacGrant = true, TrustedCapabilityCatalogLoaded = true,
        CurrentPlanAllowsCapability = true, CurrentRubroAllowsCapability = true,
        CurrentTenantAllowsCapability = true, TrustedPolicyRevisionVerified = true,
        TrustedResourceOwnershipVerified = true, ResourceOwnerTenantId = Tenant,
        CurrentCapabilityKey = Key, CurrentModuleRevision = 7
    };

    private static Fence.Intent Intent() =>
        new(Key, Tenant, Sha, Rules.Operation.Inspect, 7, "audit-request-7");

    private sealed class Actors(bool revoked) : IC0804CurrentActorSource
    {
        public int Calls;
        public ValueTask<Fence.ActorEvidence?> ReadCurrentAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            return ValueTask.FromResult<Fence.ActorEvidence?>(
                new(Tenant, true, true, true, true,
                    Policy() with { UserOrTenantOrRoleRevoked = revoked }));
        }
    }

    private sealed class Modules : IC0804TenantModuleSource
    {
        public int Calls;
        public ValueTask<Fence.ModuleEvidence?> ReadAsync(
            string tenant, string key, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            return ValueTask.FromResult<Fence.ModuleEvidence?>(
                new(Key, Tenant, Sha, Rules.State.Disabled, 7, true, true));
        }
    }

    private sealed class Readiness(bool fail) : IC0804SectorModuleReadinessSource
    {
        public int Calls;
        public ValueTask<Probe.ProviderSnapshot?> ReadAsync(
            string tenant, string key, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            if (fail) throw new InvalidOperationException("SECRET_PROVIDER_DSN");
            return ValueTask.FromResult<Probe.ProviderSnapshot?>(
                new(Tenant, Key, Sha, 7, true, true));
        }
    }

    private sealed class Writer(bool failAttempt, bool failOutcome)
        : IC0804SectorModuleAuditWriter
    {
        public List<Audit.AuditRecord> Records { get; } = new();
        public ValueTask AppendAsync(
            Audit.AuditRecord record, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if ((record.Stage == "ATTEMPT" && failAttempt) ||
                (record.Stage == "OUTCOME" && failOutcome))
                throw new InvalidOperationException("SECRET_AUDIT_CREDENTIAL");
            Records.Add(record);
            return ValueTask.CompletedTask;
        }
    }

    private static (Audit boundary, Actors actors, Modules modules,
        Readiness provider, Writer writer) Create(
            bool revoked = false, bool failProvider = false,
            bool failAttempt = false, bool failOutcome = false)
    {
        var a = new Actors(revoked);
        var m = new Modules();
        var p = new Readiness(failProvider);
        var w = new Writer(failAttempt, failOutcome);
        return (new Audit(new Probe(new Fence(a, m), p), w), a, m, p, w);
    }

    [Fact]
    public async Task TrustedProbeRecordsStructuredAttemptAndOutcomeWithoutSensitiveFields()
    {
        var (boundary, actors, modules, provider, writer) = Create();
        var result = await boundary.InspectWithAuditAsync(Intent());
        Assert.Equal("SECTOR_READINESS_STAGED_NOT_AUTHORIZED", result.Code);
        Assert.Equal(1, actors.Calls);
        Assert.Equal(1, modules.Calls);
        Assert.Equal(1, provider.Calls);
        Assert.Equal(2, writer.Records.Count);
        Assert.Equal("ATTEMPT", writer.Records[0].Stage);
        Assert.Equal("OUTCOME", writer.Records[1].Stage);
        Assert.Equal("READ_ONLY_INSPECT", writer.Records[0].OperationClass);
        Assert.Equal("NOT_CERTIFIED", writer.Records[0].OutcomeCode);
        Assert.Equal(result.Code, writer.Records[1].OutcomeCode);
        Assert.Equal(writer.Records[0].CorrelationId, writer.Records[1].CorrelationId);
        Assert.Equal(32, writer.Records[0].CorrelationId.Length);
        Assert.DoesNotContain(Tenant, string.Join("|", writer.Records));
        Assert.DoesNotContain(Sha, string.Join("|", writer.Records));
        Assert.DoesNotContain("SECRET", string.Join("|", writer.Records));
        Assert.False(result.GrantsJwtOrRbac);
        Assert.False(result.GrantsTenantAccess);
        Assert.False(result.EnablesModule);
        Assert.False(result.DispatchesWork);
        Assert.False(result.WritesDatabase);
        Assert.False(result.CertifiesDurableAudit);
        Assert.False(result.AuthorizesRelease);
    }

    [Fact]
    public async Task DispatchCannotAccessTrustedSourcesButIsAuditedAsDenied()
    {
        var (boundary, actors, modules, provider, writer) = Create();
        var result = await boundary.InspectWithAuditAsync(Intent(), true);
        Assert.Equal("SECTOR_READINESS_DENIED", result.Code);
        Assert.Equal(0, actors.Calls);
        Assert.Equal(0, modules.Calls);
        Assert.Equal(0, provider.Calls);
        Assert.Equal("UNTRUSTED_OR_OTHER_OPERATION", writer.Records[0].OperationClass);
        Assert.Equal(2, writer.Records.Count);
    }

    [Fact]
    public async Task TenantBAndRevokedRoleFailClosedAndCannotReachProvider()
    {
        var foreign = Create();
        var first = await foreign.boundary.InspectWithAuditAsync(
            Intent() with { TenantAssertion = "tenant-B" });
        Assert.Equal("SECTOR_READINESS_DENIED", first.Code);
        Assert.Equal(0, foreign.provider.Calls);
        Assert.Equal(2, foreign.writer.Records.Count);

        var revoked = Create(revoked: true);
        var second = await revoked.boundary.InspectWithAuditAsync(Intent());
        Assert.Equal("SECTOR_READINESS_DENIED", second.Code);
        Assert.Equal(0, revoked.provider.Calls);
        Assert.Equal(2, revoked.writer.Records.Count);
    }

    [Fact]
    public async Task AuditFailureBeforeProbeFailsClosedWithoutAnyRead()
    {
        var (boundary, actors, modules, provider, writer) = Create(failAttempt: true);
        var result = await boundary.InspectWithAuditAsync(Intent());
        Assert.Equal("SECTOR_AUDIT_UNAVAILABLE", result.Code);
        Assert.Equal(0, actors.Calls);
        Assert.Equal(0, modules.Calls);
        Assert.Equal(0, provider.Calls);
        Assert.Empty(writer.Records);
    }

    [Fact]
    public async Task AuditFailureAfterReadCannotLeakPositiveReadiness()
    {
        var (boundary, _, _, provider, writer) = Create(failOutcome: true);
        var result = await boundary.InspectWithAuditAsync(Intent());
        Assert.Equal("SECTOR_AUDIT_UNAVAILABLE", result.Code);
        Assert.Equal(1, provider.Calls);
        Assert.Single(writer.Records);
        Assert.Equal("ATTEMPT", writer.Records[0].Stage);
    }

    [Fact]
    public async Task ProviderExceptionIsSanitizedAndCompletionStillRecorded()
    {
        var (boundary, _, _, provider, writer) = Create(failProvider: true);
        var result = await boundary.InspectWithAuditAsync(Intent());
        Assert.Equal("SECTOR_READINESS_SOURCE_UNAVAILABLE", result.Code);
        Assert.Equal(1, provider.Calls);
        Assert.Equal(2, writer.Records.Count);
        Assert.Equal(result.Code, writer.Records[1].OutcomeCode);
        Assert.DoesNotContain("SECRET", string.Join("|", writer.Records));
    }

    [Fact]
    public async Task CancellationBeforeAttemptDoesNotTouchAuditOrProvider()
    {
        var (boundary, actors, _, provider, writer) = Create();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await boundary.InspectWithAuditAsync(
                Intent(), cancellationToken: cts.Token));
        Assert.Equal(0, actors.Calls);
        Assert.Equal(0, provider.Calls);
        Assert.Empty(writer.Records);
    }

    [Fact]
    public void NullDependenciesAreRejectedBeforeAnyAudit()
    {
        var (boundary, _, _, _, writer) = Create();
        Assert.Throws<ArgumentNullException>(() => new Audit(null!, writer));
        Assert.Throws<ArgumentNullException>(() => new Audit(
            new Probe(new Fence(new Actors(false), new Modules()),
                new Readiness(false)), null!));
        Assert.NotNull(boundary);
    }
}
