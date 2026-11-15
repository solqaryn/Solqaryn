using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class V5ContinuityIncidentAuthorizationGateTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";

    private static V5ContinuityIncidentDomainRules.ServerAuthority Authority() =>
        new(10, 100, 7, true, true, true, true, true);

    private static V5ContinuityIncidentDomainRules.PersistedIncident Incident() =>
        new(100, 7, 4, V5ContinuityIncidentDomainRules.IncidentState.Open,
            true, false, true);

    private static V5ContinuityIncidentDomainRules.Request Read() =>
        new(V5ContinuityIncidentDomainRules.IncidentAction.ReadEvidence,
            null, null, null, null, false, false);

    private static V5ContinuityIncidentAuthorizationGate.Request Request() =>
        new("incident-100", Sha, null, false, false, Read());

    private static V5ContinuityIncidentAuthorizationGate.VerifiedEvidence Evidence() =>
        new(Sha, 10, 100, true, true, true, true, false, true,
            "git-proof:dev-verified", Authority(), Incident());

    private sealed class Source : IV5ContinuityIncidentVerifiedEvidenceReader
    {
        public V5ContinuityIncidentAuthorizationGate.VerifiedEvidence? Value { get; set; } =
            Evidence();
        public bool ThrowOnRead { get; set; }

        public ValueTask<V5ContinuityIncidentAuthorizationGate.VerifiedEvidence?> ReadAsync(
            string incidentKey, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (ThrowOnRead)
                throw new InvalidOperationException("source unavailable");
            return ValueTask.FromResult(Value);
        }
    }

    private static V5ContinuityIncidentAuthorizationGate Gate(Source? source = null) =>
        new(source ?? new Source());

    [Fact]
    public async Task ValidSnapshotOnlyYieldsPendingTrustedRuntimeNeverAReadGrant()
    {
        var decision = await Gate().EvaluateAsync(Request());
        Assert.Equal("AUTHENTICATED_INCIDENT_RUNTIME_ADAPTER_REQUIRED", decision.Code);
        Assert.True(decision.NeedsAuthenticatedRuntimeAdapter);
        Assert.False(decision.GrantsEvidenceRead);
        Assert.False(decision.GrantsMutation);
        Assert.False(decision.GrantsBackupRestore);
        Assert.False(decision.CertifiesJwtOrDatabase);
    }

    [Fact]
    public async Task NullInvalidKeyAndShaAreDeniedBeforeReadingEvidence()
    {
        Assert.Equal("INCIDENT_AUTH_REQUEST_INVALID", (await Gate().EvaluateAsync(null)).Code);
        Assert.Equal("INCIDENT_AUTH_REQUEST_INVALID",
            (await Gate().EvaluateAsync(Request() with { IncidentKey = "a/b" })).Code);
        Assert.Equal("INCIDENT_AUTH_REQUEST_INVALID",
            (await Gate().EvaluateAsync(Request() with { IncidentKey = new string('x', 129) })).Code);
        Assert.Equal("INCIDENT_AUTH_REQUEST_INVALID",
            (await Gate().EvaluateAsync(Request() with { ExpectedCommitSha = OtherSha.ToUpperInvariant() })).Code);
        Assert.Equal("INCIDENT_AUTH_REQUEST_INVALID",
            (await Gate().EvaluateAsync(Request() with { Command = null })).Code);
    }

    [Fact]
    public async Task ClientRoleExportAndRestoreCannotBypassBackend()
    {
        Assert.Equal("INCIDENT_CLIENT_AUTHORITY_OR_EXPORT_DENIED",
            (await Gate().EvaluateAsync(Request() with { ClientSuppliedRole = true })).Code);
        Assert.Equal("INCIDENT_CLIENT_AUTHORITY_OR_EXPORT_DENIED",
            (await Gate().EvaluateAsync(Request() with { RequestsEvidenceExport = true })).Code);
        Assert.Equal("INCIDENT_CLIENT_AUTHORITY_OR_EXPORT_DENIED",
            (await Gate().EvaluateAsync(Request() with
            {
                Command = Read() with
                {
                    Action = V5ContinuityIncidentDomainRules.IncidentAction.RequestSandboxRestore,
                    RecoveryApprovalFromTrustedSource = true
                }
            })).Code);
    }

    [Fact]
    public async Task MissingOrChangedSourceShaIsRejected()
    {
        var source = new Source { Value = Evidence() with { ObservedCommitSha = OtherSha } };
        Assert.Equal("INCIDENT_TRUSTED_EVIDENCE_MISSING_OR_STALE",
            (await Gate(source).EvaluateAsync(Request())).Code);
        source.Value = null;
        Assert.Equal("INCIDENT_TRUSTED_EVIDENCE_MISSING_OR_STALE",
            (await Gate(source).EvaluateAsync(Request())).Code);
    }

    [Fact]
    public async Task UnavailableSourceFailsClosedWithoutExposingDetails()
    {
        var source = new Source { ThrowOnRead = true };
        var verdict = await Gate(source).EvaluateAsync(Request());
        Assert.Equal("VERIFIED_INCIDENT_EVIDENCE_UNAVAILABLE", verdict.Code);
        Assert.False(verdict.GrantsEvidenceRead);
    }

    [Fact]
    public async Task ForgedOrCrossTenantOwnershipAndActorAreDenied()
    {
        var source = new Source { Value = Evidence() with { PersistedOwnerTenantId = 200 } };
        Assert.Equal("INCIDENT_TRUSTED_ACTOR_OR_OWNER_MISMATCH",
            (await Gate(source).EvaluateAsync(Request())).Code);
        source.Value = Evidence() with { VerifiedActorId = 11 };
        Assert.Equal("INCIDENT_TRUSTED_ACTOR_OR_OWNER_MISMATCH",
            (await Gate(source).EvaluateAsync(Request())).Code);
        source.Value = Evidence() with { Incident = Incident() with { OwnershipReadFromPersistence = false } };
        Assert.Equal("INCIDENT_TRUSTED_ACTOR_OR_OWNER_MISMATCH",
            (await Gate(source).EvaluateAsync(Request())).Code);
    }

    [Fact]
    public async Task RevokedJwtMembershipPermissionAndAuditDeny()
    {
        var source = new Source();
        foreach (var evidence in new[]
        {
            Evidence() with { JwtValidatedByBackend = false },
            Evidence() with { MembershipCurrent = false },
            Evidence() with { PermissionCurrent = false },
            Evidence() with { RevocationChecked = false },
            Evidence() with { PermissionRevoked = true },
            Evidence() with { AuditSinkReady = false },
            Evidence() with { Authority = Authority() with { ExactPermissionVerified = false } }
        })
        {
            source.Value = evidence;
            Assert.Equal("INCIDENT_CURRENT_BACKEND_PERMISSION_REQUIRED",
                (await Gate(source).EvaluateAsync(Request())).Code);
        }
    }

    [Fact]
    public async Task ClientTenantHintAndCommandTenantOverrideCannotEscalate()
    {
        Assert.Equal("INCIDENT_CLIENT_TENANT_OVERRIDE_DENIED",
            (await Gate().EvaluateAsync(Request() with { ClientTenantHint = 200 })).Code);
        Assert.Equal("INCIDENT_CLIENT_TENANT_OVERRIDE_DENIED",
            (await Gate().EvaluateAsync(Request() with
            {
                Command = Read() with { ClientTenantOverride = 200 }
            })).Code);
    }

    [Fact]
    public async Task BranchOrRevocationDenialIsPreservedFromDomain()
    {
        var source = new Source { Value = Evidence() with
        {
            Authority = Authority() with { BranchId = 8 }
        } };
        Assert.Equal("INCIDENT_BRANCH_MISMATCH", (await Gate(source).EvaluateAsync(Request())).Code);
        source.Value = Evidence() with { Incident = Incident() with { IncidentRevoked = true } };
        Assert.Equal("INCIDENT_ACCESS_REVOKED", (await Gate(source).EvaluateAsync(Request())).Code);
    }

    [Fact]
    public async Task ContainWithStaleVersionOrMissingAuditKeyFailsClosed()
    {
        var contain = Read() with
        {
            Action = V5ContinuityIncidentDomainRules.IncidentAction.Contain,
            ExpectedVersion = 3,
            IdempotencyKey = "incident-100-v3",
            Reason = "contain-with-evidence",
            MitigationEvidenceVerified = true
        };
        Assert.Equal("INCIDENT_VERSION_CONFLICT",
            (await Gate().EvaluateAsync(Request() with { Command = contain })).Code);
        Assert.Equal("AUDIT_REASON_AND_IDEMPOTENCY_REQUIRED",
            (await Gate().EvaluateAsync(Request() with
            {
                Command = contain with { ExpectedVersion = 4, IdempotencyKey = null }
            })).Code);
    }

    [Fact]
    public async Task CancellationPropagatesWithoutTreatingAsSuccess()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await Gate().EvaluateAsync(Request(), cts.Token));
    }
}
