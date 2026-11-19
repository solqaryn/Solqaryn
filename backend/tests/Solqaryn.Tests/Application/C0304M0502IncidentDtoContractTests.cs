using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

/// <summary>
/// C03-04.M05.02 - JSON DTO contract of the real read-only incident preflight.
/// These tests do not impersonate signed JWT, HTTP or persisted MySQL authority.
/// </summary>
public sealed class C0304M0502IncidentDtoContractTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly V5ContinuityIncidentApplicationService Service = new();

    private static V5ContinuityIncidentApplicationService.Request Nominal() =>
        new(
            Sha, Sha, "git-proof:dev-verified",
            new V5ContinuityIncidentDomainRules.ServerAuthority(
                101, 11, 2, true, true, true, true, true),
            new V5ContinuityIncidentDomainRules.PersistedIncident(
                11, 2, 4, V5ContinuityIncidentDomainRules.IncidentState.Open,
                true, false, true),
            new V5ContinuityIncidentDomainRules.Request(
                V5ContinuityIncidentDomainRules.IncidentAction.ReadEvidence,
                null, null, null, null, false, false));

    private static JsonObject Envelope() =>
        JsonNode.Parse(JsonSerializer.Serialize(Nominal(), JsonOptions))!.AsObject();

    private static V5ContinuityIncidentApplicationService.Result Evaluate(JsonObject payload) =>
        Service.Prepare(JsonSerializer.Deserialize<V5ContinuityIncidentApplicationService.Request>(
            payload.ToJsonString(), JsonOptions));

    private static void Safe(V5ContinuityIncidentApplicationService.Result result, string code)
    {
        Assert.Equal(code, result.Code);
        Assert.False(result.AllowsIncidentMutation);
        Assert.False(result.AllowsBackupRestore);
        Assert.False(result.AllowsEvidenceDisclosure);
    }

    [Fact]
    public void NominalSerializedDtoIsOnlyARequestForAuthenticatedAdapter()
    {
        var json = JsonSerializer.Serialize(Nominal(), JsonOptions);
        Assert.Contains("\"expectedCommitSha\"", json);
        Assert.Contains("\"sanitizedEvidenceReference\"", json);
        var decoded = JsonSerializer.Deserialize<V5ContinuityIncidentApplicationService.Request>(
            json, JsonOptions);
        Assert.NotNull(decoded);
        Assert.Equal(Sha, decoded!.ExpectedCommitSha);
        var result = Service.Prepare(decoded);
        Safe(result, "AUTHENTICATED_INCIDENT_ADAPTER_REQUIRED");
        Assert.True(result.NeedsTrustedAdapter);
    }

    [Fact]
    public void SafeResultSerializesExplicitFalseEffectFlags()
    {
        var result = Evaluate(Envelope());
        var json = JsonSerializer.Serialize(result, JsonOptions);
        var node = JsonNode.Parse(json)!.AsObject();
        Assert.Equal("AUTHENTICATED_INCIDENT_ADAPTER_REQUIRED", (string?)node["code"]);
        Assert.Equal(false, (bool?)node["allowsIncidentMutation"]);
        Assert.Equal(false, (bool?)node["allowsBackupRestore"]);
        Assert.Equal(false, (bool?)node["allowsEvidenceDisclosure"]);
    }

    [Fact]
    public void ForgedResponseFlagsCannotCreateRealPermission()
    {
        const string forged = "{\"code\":\"READY\",\"needsTrustedAdapter\":true,\"allowsIncidentMutation\":true,\"allowsBackupRestore\":true,\"allowsEvidenceDisclosure\":true}";
        var parsed = JsonSerializer.Deserialize<V5ContinuityIncidentApplicationService.Result>(
            forged, JsonOptions);
        Assert.NotNull(parsed);
        Assert.False(parsed!.AllowsIncidentMutation);
        Assert.False(parsed.AllowsBackupRestore);
        Assert.False(parsed.AllowsEvidenceDisclosure);
    }

    [Theory]
    [InlineData("expectedCommitSha")]
    [InlineData("observedCommitSha")]
    public void MissingShaFieldIsRejectedAfterJsonDecode(string field)
    {
        var dto = Envelope();
        Assert.True(dto.Remove(field));
        Safe(Evaluate(dto), "INCIDENT_SOURCE_SHA_UNVERIFIED");
    }

    [Fact]
    public void MismatchedOrCaseAlteredShaCannotAuthorize()
    {
        var dto = Envelope();
        dto["observedCommitSha"] = OtherSha;
        Safe(Evaluate(dto), "INCIDENT_SOURCE_SHA_UNVERIFIED");
        dto["observedCommitSha"] = Sha.ToUpperInvariant();
        Safe(Evaluate(dto), "INCIDENT_SOURCE_SHA_UNVERIFIED");
    }

    [Theory]
    [InlineData("")]
    [InlineData("https://private.invalid/incident")]
    [InlineData("person@example.test")]
    [InlineData("line\nbreak")]
    public void UnsafeEvidenceReferencesRejectWithoutDisclosure(string input)
    {
        var dto = Envelope();
        dto["sanitizedEvidenceReference"] = input;
        Safe(Evaluate(dto), "INCIDENT_EVIDENCE_REFERENCE_REQUIRED");
    }

    [Fact]
    public void ClientTenantOverrideCannotCrossTenantBoundary()
    {
        var dto = Envelope();
        ((JsonObject)dto["command"]!)["clientTenantOverride"] = 22;
        Safe(Evaluate(dto), "CLIENT_TENANT_OVERRIDE_DENIED");
    }

    [Fact]
    public void ForeignPersistedOwnershipCannotBeOverriddenByDto()
    {
        var dto = Envelope();
        ((JsonObject)dto["incident"]!)["ownerTenantId"] = 22;
        Safe(Evaluate(dto), "CROSS_TENANT_OR_UNOWNED_INCIDENT_DENIED");
    }

    [Theory]
    [InlineData("jwtVerified")]
    [InlineData("activeMembershipVerified")]
    [InlineData("exactPermissionVerified")]
    [InlineData("serverResolvedScope")]
    public void UntrustedClaimSnapshotsCannotAuthorize(string field)
    {
        var dto = Envelope();
        ((JsonObject)dto["authority"]!)[field] = false;
        Safe(Evaluate(dto), "VERIFIED_SERVER_AUTHORITY_REQUIRED");
    }

    [Fact]
    public void RevokedIncidentFailsClosed()
    {
        var dto = Envelope();
        ((JsonObject)dto["incident"]!)["incidentRevoked"] = true;
        Safe(Evaluate(dto), "INCIDENT_ACCESS_REVOKED");
    }

    [Fact]
    public void EvenApprovedRestoreIntentCannotStartRestore()
    {
        var dto = Envelope();
        ((JsonObject)dto["command"]!)["action"] = 3;
        ((JsonObject)dto["command"]!)["recoveryApprovalFromTrustedSource"] = true;
        Safe(Evaluate(dto), "RESTORE_REQUIRES_SEPARATE_AUTHORIZED_RUNNER");
    }

    [Fact]
    public void StaleVersionAndMissingIdempotencyFailClosed()
    {
        var dto = Envelope();
        var command = (JsonObject)dto["command"]!;
        command["action"] = 1;
        command["expectedVersion"] = 3;
        command["idempotencyKey"] = "incident-11-v4";
        command["reason"] = "contained with audit";
        Safe(Evaluate(dto), "INCIDENT_VERSION_CONFLICT");
        command["expectedVersion"] = 4;
        command["idempotencyKey"] = null;
        Safe(Evaluate(dto), "AUDIT_REASON_AND_IDEMPOTENCY_REQUIRED");
    }

    [Fact]
    public void InvalidNestedDtoIsRejectedBeforeTrustedAuthority()
    {
        var dto = Envelope();
        dto.Remove("command");
        Safe(Evaluate(dto), "VERIFIED_SERVER_AUTHORITY_REQUIRED");
        dto = Envelope();
        dto.Remove("incident");
        Safe(Evaluate(dto), "VERIFIED_SERVER_AUTHORITY_REQUIRED");
    }

    [Fact]
    public void UnknownActionAndInjectedFlagsCannotBypassPreflight()
    {
        var dto = Envelope();
        dto["allowsIncidentMutation"] = true;
        dto["allowsBackupRestore"] = true;
        ((JsonObject)dto["command"]!)["action"] = 999;
        Safe(Evaluate(dto), "UNKNOWN_INCIDENT_STATE_OR_ACTION");
    }

    [Fact]
    public void NullOrMalformedJsonDoesNotCreateAValidRequest()
    {
        var empty = JsonSerializer.Deserialize<V5ContinuityIncidentApplicationService.Request>(
            "null", JsonOptions);
        Safe(Service.Prepare(empty), "INCIDENT_REQUEST_REQUIRED");
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<V5ContinuityIncidentApplicationService.Request>(
                "{\"authority\":", JsonOptions));
    }
}
