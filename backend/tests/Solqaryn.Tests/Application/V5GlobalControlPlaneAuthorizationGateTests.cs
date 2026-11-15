using Solqaryn.Application.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5GlobalControlPlaneDomainRules;
using Gate = Solqaryn.Application.AutomationV5.V5GlobalControlPlaneAuthorizationGate;
namespace Solqaryn.Tests.Application;

public sealed class V5GlobalControlPlaneAuthorizationGateTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 13, 0, 0, TimeSpan.Zero);
    private sealed class Reader : IV5GlobalControlPlaneVerifiedEvidenceReader
    {
        public Gate.Evidence? Current { get; set; } = Verified();
        public int Calls { get; private set; }
        public ValueTask<Gate.Evidence?> ReadVerifiedAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested(); Calls++; return ValueTask.FromResult(Current);
        }
    }
    private static Rules.TrustedSnapshot Snapshot() => new(
        "actor-a", "tenant-a", "tenant-a", true, true, true,
        false, false, true, true, true, "audit-42",
        false, false, null, null, null, false, null);
    private static Gate.Evidence Verified() => new(
        Sha, "actor-a", "tenant-a", true, true, true, false, Now, Snapshot());
    private static Gate.Request Request() => new(
        Rules.Operation.OwnTenantMetadataRead, "tenant-a", Sha,
        null, false, false, false);

    [Fact] public async Task OwnTenantPreflightNeverGrantsPermissions()
    {
        var result = await new Gate(new Reader()).EvaluateAsync(Request());
        Assert.Equal("AUTHENTICATED_GLOBAL_CONTROL_PLANE_ADAPTER_REQUIRED", result.Code);
        Assert.True(result.NeedsAuthenticatedRuntimeAdapter);
        Assert.False(result.GrantsAccess);
        Assert.False(result.GrantsTenantEnumeration);
        Assert.False(result.GrantsCrossTenantRead);
        Assert.False(result.GrantsInstitutionalWrite);
        Assert.False(result.GrantsExceptionalLease);
    }
    [Fact] public async Task InvalidRequestsRejectBeforeReader()
    {
        var reader = new Reader(); var gate = new Gate(reader);
        Assert.Equal("CONTROL_PLANE_REQUEST_REQUIRED", (await gate.EvaluateAsync(null)).Code);
        Assert.Equal("CONTROL_PLANE_OPERATION_INVALID",
            (await gate.EvaluateAsync(Request() with { Operation = (Rules.Operation)77 })).Code);
        Assert.Equal("CONTROL_PLANE_EXACT_SOURCE_SHA_REQUIRED",
            (await gate.EvaluateAsync(Request() with { ExpectedSourceSha = "bad" })).Code);
        Assert.Equal("CONTROL_PLANE_CLIENT_AUTHORITY_DENIED",
            (await gate.EvaluateAsync(Request() with { ClientAuthorityHint = true })).Code);
        Assert.Equal("CONTROL_PLANE_UNSAFE_OPERATION_DENIED",
            (await gate.EvaluateAsync(Request() with { WriteOrExport = true })).Code);
        Assert.Equal("CONTROL_PLANE_UNSAFE_OPERATION_DENIED",
            (await gate.EvaluateAsync(Request() with { SensitiveData = true })).Code);
        Assert.Equal("CONTROL_PLANE_TARGET_TENANT_INVALID",
            (await gate.EvaluateAsync(Request() with { RequestedTenantId = null })).Code);
        Assert.Equal(0, reader.Calls);
    }
    [Fact] public async Task StaleEvidenceUtcAndPermissionRevocationFailClosed()
    {
        var reader = new Reader(); var gate = new Gate(reader);
        reader.Current = null;
        Assert.Equal("CONTROL_PLANE_EVIDENCE_MISSING_OR_STALE",
            (await gate.EvaluateAsync(Request())).Code);
        reader.Current = Verified() with { ObservedSourceSha = "abcdef0123456789abcdef0123456789abcdef01" };
        Assert.Equal("CONTROL_PLANE_EVIDENCE_MISSING_OR_STALE",
            (await gate.EvaluateAsync(Request())).Code);
        reader.Current = Verified() with
        { ServerUtc = new DateTimeOffset(2026, 11, 15, 7, 0, 0, TimeSpan.FromHours(-6)) };
        Assert.Equal("CONTROL_PLANE_SERVER_UTC_REQUIRED",
            (await gate.EvaluateAsync(Request())).Code);
        reader.Current = Verified() with { PermissionRevoked = true };
        Assert.Equal("CONTROL_PLANE_CURRENT_BACKEND_AUTHORITY_REQUIRED",
            (await gate.EvaluateAsync(Request())).Code);
        reader.Current = Verified() with { AuditReadFromBackend = false };
        Assert.Equal("CONTROL_PLANE_CURRENT_BACKEND_AUTHORITY_REQUIRED",
            (await gate.EvaluateAsync(Request())).Code);
    }
    [Fact] public async Task ActorTenantAndMfaCannotBeSpoofed()
    {
        var reader = new Reader(); var gate = new Gate(reader);
        reader.Current = Verified() with { VerifiedActorId = "actor-b" };
        Assert.Equal("CONTROL_PLANE_ACTOR_TENANT_MISMATCH",
            (await gate.EvaluateAsync(Request())).Code);
        reader.Current = Verified() with { VerifiedTenantId = "tenant-b" };
        Assert.Equal("CONTROL_PLANE_ACTOR_TENANT_MISMATCH",
            (await gate.EvaluateAsync(Request())).Code);
        reader.Current = Verified();
        Assert.Equal("CONTROL_PLANE_ACTOR_TENANT_MISMATCH",
            (await gate.EvaluateAsync(Request() with { RequestedTenantId = "tenant-b" })).Code);
        reader.Current = Verified() with { Snapshot = Snapshot() with { RecentMfaVerified = false } };
        Assert.Equal("MFA_AND_REAUTHENTICATION_REQUIRED",
            (await gate.EvaluateAsync(Request())).Code);
        reader.Current = Verified() with { Snapshot = Snapshot() with { CurrentRoleGrantVerified = false } };
        Assert.Equal("SERVER_IDENTITY_AND_GRANT_REQUIRED",
            (await gate.EvaluateAsync(Request())).Code);
    }
    [Fact] public async Task PlatformGrantIsDistinctFromTenantRole()
    {
        var reader = new Reader(); var gate = new Gate(reader);
        var directory = Request() with { Operation = Rules.Operation.PlatformTenantDirectoryRead };
        Assert.Equal("INDEPENDENT_PLATFORM_GRANT_REQUIRED",
            (await gate.EvaluateAsync(directory)).Code);
        reader.Current = Verified() with { Snapshot = Snapshot() with
        { PlatformIdentityVerified = true, PlatformGrantVerified = true } };
        var result = await gate.EvaluateAsync(directory);
        Assert.True(result.NeedsAuthenticatedRuntimeAdapter);
        Assert.False(result.GrantsTenantEnumeration);
    }
    [Fact] public async Task ExceptionalLeaseNeedsIndependentApprover()
    {
        var reader = new Reader { Current = Verified() with { Snapshot = Snapshot() with
        { PlatformIdentityVerified = true, PlatformGrantVerified = true } } };
        var gate = new Gate(reader);
        var request = Request() with { Operation = Rules.Operation.ExceptionalTenantMetadataRead,
            Justification = "audited security investigation" };
        Assert.Equal("EXCEPTION_GRANT_AND_REASON_REQUIRED",
            (await gate.EvaluateAsync(request)).Code);
        reader.Current = Verified() with { Snapshot = Snapshot() with
        { PlatformIdentityVerified = true, PlatformGrantVerified = true,
          ExceptionGrantActive = true, ExceptionTenantId = "tenant-a",
          ExceptionIssuedUtc = Now.AddMinutes(-5), ExceptionExpiresUtc = Now.AddMinutes(5),
          IndependentApprovalVerified = true, IndependentApproverActorId = "actor-b" } };
        var result = await gate.EvaluateAsync(request);
        Assert.True(result.NeedsAuthenticatedRuntimeAdapter);
        Assert.False(result.GrantsExceptionalLease);
    }
    [Fact] public async Task CancellationAndMissingReaderAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new Gate(null!));
        var reader = new Reader(); using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await new Gate(reader).EvaluateAsync(Request(), cts.Token));
        Assert.Equal(0, reader.Calls);
    }
}
