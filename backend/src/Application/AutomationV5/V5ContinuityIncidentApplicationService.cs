using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C03-04.M03.02: read-only Application preflight for incident/continuity cases.
/// Authority, incident ownership and evidence are input snapshots, not proof of
/// authentication. A future trusted adapter must resolve and recheck them.
/// This service never exposes evidence, mutates incidents or runs restore.
/// </summary>
public sealed class V5ContinuityIncidentApplicationService
{
    public sealed record Request(
        string? ExpectedCommitSha,
        string? ObservedCommitSha,
        string? SanitizedEvidenceReference,
        V5ContinuityIncidentDomainRules.ServerAuthority? Authority,
        V5ContinuityIncidentDomainRules.PersistedIncident? Incident,
        V5ContinuityIncidentDomainRules.Request? Command);

    public sealed record Result(string Code, bool NeedsTrustedAdapter)
    {
        public bool AllowsIncidentMutation => false;
        public bool AllowsBackupRestore => false;
        public bool AllowsEvidenceDisclosure => false;
    }

    public Result Prepare(Request? request)
    {
        if (request is null)
            return Deny("INCIDENT_REQUEST_REQUIRED");

        if (!FullSha(request.ExpectedCommitSha) ||
            !FullSha(request.ObservedCommitSha) ||
            !string.Equals(request.ExpectedCommitSha, request.ObservedCommitSha,
                StringComparison.Ordinal))
            return Deny("INCIDENT_SOURCE_SHA_UNVERIFIED");

        if (!SafeReference(request.SanitizedEvidenceReference))
            return Deny("INCIDENT_EVIDENCE_REFERENCE_REQUIRED");

        var verdict = V5ContinuityIncidentDomainRules.Evaluate(
            request.Authority, request.Incident, request.Command);

        if (!verdict.RequiresTrustedServiceEnforcement)
            return Deny(verdict.Code);

        // Neither the claimed JWT nor authorization flags are verified here.
        // Caller must obtain server context, version, idempotency and audit
        // from trusted providers before any real operation can occur.
        return new Result("AUTHENTICATED_INCIDENT_ADAPTER_REQUIRED", true);
    }

    private static Result Deny(string code) => new(code, false);

    private static bool FullSha(string? sha) =>
        sha is { Length: 40 } && sha.All(Uri.IsHexDigit);

    private static bool SafeReference(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length <= 160 &&
        value.All(c => c >= '!' && c <= '~') &&
        !value.Contains("://", StringComparison.Ordinal) &&
        !value.Contains('@');
}
