using Solqaryn.Application.AutomationV5;
using Xunit;
using Adapter = Solqaryn.Application.AutomationV5.C0304IsolatedIncidentPersistenceProjectionAdapter;
using Rules = Solqaryn.Domain.AutomationV5.V5ContinuityIncidentDomainRules;

namespace Solqaryn.Tests.Application;

public sealed class C0304IsolatedIncidentPersistenceProjectionAdapterTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static Adapter.StoredMetadata Stored() =>
        new(100, 7, 4, "Open", true, false, true, Sha);

    [Fact]
    public void ValidProjectionStillRequiresAuthenticatedIncidentGate()
    {
        var r = Adapter.Project(Stored(), 100, 7, Sha);
        Assert.Equal("AUTHENTICATED_INCIDENT_GATE_REQUIRED", r.Code);
        Assert.NotNull(r.Incident);
        Assert.Equal(100, r.Incident.OwnerTenantId);
        Assert.Equal(7, r.Incident.OwnerBranchId);
        Assert.Equal(4, r.Incident.Version);
        Assert.Equal(Rules.IncidentState.Open, r.Incident.State);
        Assert.False(r.AuthorizesEvidenceDisclosure);
        Assert.False(r.AuthorizesIncidentMutation);
        Assert.False(r.AuthorizesBackupRestore);
        Assert.False(r.CertifiesPersistedOwnership);
    }

    [Fact]
    public void MissingSourceOrTrustedTenantCannotProject()
    {
        Assert.Equal("VERIFIED_INCIDENT_SOURCE_REQUIRED", Adapter.Project(null, 100, 7, Sha).Code);
        Assert.Equal("VERIFIED_INCIDENT_SOURCE_REQUIRED", Adapter.Project(Stored(), 0, 7, Sha).Code);
        Assert.Equal("VERIFIED_INCIDENT_SOURCE_REQUIRED", Adapter.Project(Stored(), -1, 7, Sha).Code);
    }

    [Fact]
    public void UnverifiedPersistenceAndTenantBCannotProject()
    {
        Assert.Equal("PERSISTENCE_PROVENANCE_UNVERIFIED", Adapter.Project(
            Stored() with { OwnershipReadFromPersistence = false }, 100, 7, Sha).Code);
        Assert.Equal("INCIDENT_CROSS_TENANT_DENIED", Adapter.Project(Stored(), 200, 7, Sha).Code);
        Assert.Equal("INCIDENT_CROSS_TENANT_DENIED", Adapter.Project(
            Stored() with { OwnerTenantId = null }, 100, 7, Sha).Code);
    }

    [Fact]
    public void ForeignOrAbsentBranchIsDenied()
    {
        Assert.Equal("INCIDENT_BRANCH_SCOPE_DENIED", Adapter.Project(Stored(), 100, 8, Sha).Code);
        Assert.Equal("INCIDENT_BRANCH_SCOPE_DENIED", Adapter.Project(Stored(), 100, null, Sha).Code);
        Assert.Equal("INCIDENT_BRANCH_SCOPE_DENIED", Adapter.Project(
            Stored() with { OwnerBranchId = -1 }, 100, 7, Sha).Code);
    }

    [Fact]
    public void StaleOrNoncanonicalShaFails()
    {
        Assert.Equal("EXACT_SOURCE_SHA_REQUIRED", Adapter.Project(Stored(), 100, 7, new string('f', 40)).Code);
        Assert.Equal("EXACT_SOURCE_SHA_REQUIRED", Adapter.Project(Stored(), 100, 7, Sha.ToUpperInvariant()).Code);
        Assert.Equal("EXACT_SOURCE_SHA_REQUIRED", Adapter.Project(
            Stored() with { CommitSha = "not-a-sha" }, 100, 7, Sha).Code);
    }

    [Fact]
    public void NegativeVersionRevocationAndUnsafeEvidenceFailClosed()
    {
        Assert.Equal("INVALID_INCIDENT_VERSION", Adapter.Project(
            Stored() with { Version = -1 }, 100, 7, Sha).Code);
        Assert.Equal("INCIDENT_ACCESS_REVOKED", Adapter.Project(
            Stored() with { Revoked = true }, 100, 7, Sha).Code);
        Assert.Equal("SANITIZED_EVIDENCE_REQUIRED", Adapter.Project(
            Stored() with { EvidenceSanitized = false }, 100, 7, Sha).Code);
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("OPEN")]
    [InlineData("")]
    [InlineData("Open\nRoot")]
    public void UnknownStateCannotProject(string state)
    {
        var r = Adapter.Project(Stored() with { State = state }, 100, 7, Sha);
        Assert.Equal("UNKNOWN_INCIDENT_STATE", r.Code);
        Assert.Null(r.Incident);
    }

    [Theory]
    [InlineData("Contained", Rules.IncidentState.Contained)]
    [InlineData("Resolved", Rules.IncidentState.Resolved)]
    public void KnownStatesNeverGrantMutation(string state, Rules.IncidentState expected)
    {
        var r = Adapter.Project(Stored() with { State = state }, 100, 7, Sha);
        Assert.Equal(expected, r.Incident?.State);
        Assert.Equal("AUTHENTICATED_INCIDENT_GATE_REQUIRED", r.Code);
        Assert.False(r.AuthorizesIncidentMutation);
        Assert.False(r.AuthorizesBackupRestore);
    }
}
