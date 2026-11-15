using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class V5UniversalCaseFileApplicationServiceTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";
    private static readonly V5UniversalCaseFileApplicationService Service = new();

    private static V5UniversalCaseFileDomainRules.ServerAuthority Authority() =>
        new(10, 100, 5, true, true, true, true);

    private static V5UniversalCaseFileDomainRules.StoredCase Stored() =>
        new(100, 5, 3, true, false, false);

    private static V5UniversalCaseFileDomainRules.Command Read() =>
        new(V5UniversalCaseFileDomainRules.CaseAction.ReadTimeline,
            null, null, null, null, false, false, false);

    private static V5UniversalCaseFileDomainRules.Command Write() =>
        new(V5UniversalCaseFileDomainRules.CaseAction.AddObservation,
            null, 3, "case-100-v3", "verified-update", false, false, false);

    private static V5UniversalCaseFileApplicationService.Request Valid() =>
        new(Sha, Sha, "git-evidence:verified-dev", Authority(), Stored(), Read());

    [Fact]
    public void ValidReadOnlyPreflightRequiresAnExternalAuthenticatedAdapter()
    {
        var result = Service.Prepare(Valid());
        Assert.Equal("AUTHENTICATED_CASEFILE_ADAPTER_REQUIRED", result.Code);
        Assert.True(result.NeedsTrustedAdapter);
        Assert.False(result.AllowsPersistence);
        Assert.False(result.AllowsExternalMediaAccess);
        Assert.False(result.IsFinalAuthorization);
    }

    [Fact]
    public void PositiveWriteIntentStillNeverGrantsPersistence()
    {
        var result = Service.Prepare(Valid() with { Command = Write() });
        Assert.Equal("AUTHENTICATED_CASEFILE_ADAPTER_REQUIRED", result.Code);
        Assert.True(result.NeedsTrustedAdapter);
        Assert.False(result.AllowsPersistence);
        Assert.False(result.IsFinalAuthorization);
    }

    [Fact]
    public void NullRequestAndUntrustedShaFailClosed()
    {
        Assert.Equal("CASEFILE_REQUEST_REQUIRED", Service.Prepare(null).Code);
        Assert.Equal("CASEFILE_SOURCE_SHA_UNVERIFIED",
            Service.Prepare(Valid() with { ExpectedSourceSha = null }).Code);
        Assert.Equal("CASEFILE_SOURCE_SHA_UNVERIFIED",
            Service.Prepare(Valid() with { ObservedSourceSha = OtherSha }).Code);
        Assert.Equal("CASEFILE_SOURCE_SHA_UNVERIFIED",
            Service.Prepare(Valid() with { ObservedSourceSha = "bad" }).Code);
    }

    [Fact]
    public void EvidenceReferenceMustBeNonSensitiveAndBounded()
    {
        Assert.Equal("CASEFILE_EVIDENCE_REFERENCE_REQUIRED",
            Service.Prepare(Valid() with { EvidenceReference = null }).Code);
        Assert.Equal("CASEFILE_EVIDENCE_REFERENCE_REQUIRED",
            Service.Prepare(Valid() with { EvidenceReference = "https://private/resource" }).Code);
        Assert.Equal("CASEFILE_EVIDENCE_REFERENCE_REQUIRED",
            Service.Prepare(Valid() with { EvidenceReference = "x@y" }).Code);
        Assert.Equal("CASEFILE_EVIDENCE_REFERENCE_REQUIRED",
            Service.Prepare(Valid() with { EvidenceReference = new string('a', 161) }).Code);
    }

    [Fact]
    public void RevokedPermissionAndCrossTenantResourceAreDenied()
    {
        var revoked = Service.Prepare(Valid() with
        {
            Authority = Authority() with { MembershipActive = false }
        });
        Assert.Equal("SERVER_AUTHORITY_REQUIRED", revoked.Code);
        Assert.False(revoked.NeedsTrustedAdapter);

        var foreign = Service.Prepare(Valid() with
        {
            StoredCase = Stored() with { OwnerTenantId = 200 }
        });
        Assert.Equal("CASEFILE_OWNER_OR_LEGACY_SCOPE_DENIED", foreign.Code);
        Assert.False(foreign.AllowsPersistence);
    }

    [Fact]
    public void ClientTenantOverrideAndMissingServerScopeAreRejected()
    {
        Assert.Equal("CLIENT_TENANT_OVERRIDE_DENIED",
            Service.Prepare(Valid() with
            {
                Command = Read() with { ClientSelectedTenantId = 200 }
            }).Code);

        Assert.Equal("SERVER_AUTHORITY_REQUIRED",
            Service.Prepare(Valid() with
            {
                Authority = Authority() with { ScopeResolvedOnServer = false }
            }).Code);
    }

    [Fact]
    public void VersionConflictAndMissingIdempotencyAreNotBypassed()
    {
        Assert.Equal("CASEFILE_VERSION_CONFLICT",
            Service.Prepare(Valid() with
            {
                Command = Write() with { ExpectedVersion = 2 }
            }).Code);

        Assert.Equal("SCOPED_IDEMPOTENCY_KEY_REQUIRED",
            Service.Prepare(Valid() with
            {
                Command = Write() with { IdempotencyKey = null }
            }).Code);
    }

    [Fact]
    public void ArchivedCaseAndAttachmentWithoutOwnershipNeverAuthorize()
    {
        Assert.Equal("ARCHIVED_CASEFILE_IS_READ_ONLY",
            Service.Prepare(Valid() with
            {
                StoredCase = Stored() with { Archived = true },
                Command = Write()
            }).Code);

        Assert.Equal("ATTACHMENT_ACCESS_DENIED",
            Service.Prepare(Valid() with
            {
                Command = Write() with
                {
                    Action = V5UniversalCaseFileDomainRules.CaseAction.LinkAttachment
                }
            }).Code);
    }

    [Fact]
    public void IndependentDecisionEvidenceIsStillRequired()
    {
        Assert.Equal("DECISION_REVIEW_REQUIRED",
            Service.Prepare(Valid() with
            {
                Command = Write() with
                {
                    Action = V5UniversalCaseFileDomainRules.CaseAction.RecordDecision
                }
            }).Code);
    }
}
