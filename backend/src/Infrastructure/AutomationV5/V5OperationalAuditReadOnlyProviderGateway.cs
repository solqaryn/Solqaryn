using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C15-05.M03.05. Read-only provider boundary. A provider may supply an isolated
/// snapshot, but its assertions are NOT authenticated CI, an A5 approval, a
/// provider grant or permission to mutate Google Sheets/GitHub.
/// </summary>
public interface IV5OperationalAuditReadOnlyEvidenceProvider
{
    ValueTask<V5OperationalAuditEvidenceSnapshot?> ReadScopedAsync(
        string serverTenantId, CancellationToken cancellationToken);
}

public sealed record V5OperationalAuditEvidenceSnapshot(
    string TenantId,
    string SourceRevision,
    string HeadSha,
    IReadOnlySet<string> WorkItemIds,
    IReadOnlySet<string> AuthorizedTaskIds,
    IReadOnlyCollection<V5OperationalAuditDomainRules.WorkItem> Items);

/// <summary>
/// Isolated integration seam for an injected read-only provider. No network
/// client, worker, queue, events, webhook, DI registration or real provider is
/// installed here. Even a seemingly complete snapshot cannot certify A0-A5.
/// </summary>
public sealed class V5OperationalAuditReadOnlyProviderGateway
{
    private readonly IV5OperationalAuditReadOnlyEvidenceProvider _provider;
    private readonly V5OperationalAuditApplicationService _application = new();

    public V5OperationalAuditReadOnlyProviderGateway(
        IV5OperationalAuditReadOnlyEvidenceProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public readonly record struct Outcome(string Code, int ItemsExamined)
    {
        public bool AllowsInstitutionalWrite => false;
        public bool AllowsTenantFilterBypass => false;
        public bool AllowsEventDispatch => false;
        public bool CertifiesA5 => false;
        public bool RequiresIndependentExternalEvidence =>
            Code == "AUTHENTICATED_EVIDENCE_REQUIRED";
    }

    public async Task<Outcome> PrepareReadOnlyAsync(
        V5OperationalAuditSecurityGate.Context? security,
        string? expectedHeadSha,
        string? expectedSourceRevision,
        IReadOnlySet<string>? expectedAuthorizedTaskIds,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Validate all caller inputs before calling a potentially remote provider.
        if (security is null)
            return Deny("VERIFIED_SECURITY_CONTEXT_REQUIRED");
        if (!FullSha(expectedHeadSha) ||
            !string.Equals(expectedHeadSha, security.EvidenceSha,
                StringComparison.Ordinal))
            return Deny("CHECK_SHA_MISMATCH");
        if (string.IsNullOrWhiteSpace(expectedSourceRevision))
            return Deny("SOURCE_REVISION_MISMATCH");
        if (expectedAuthorizedTaskIds is null ||
            expectedAuthorizedTaskIds.Count != 6 ||
            expectedAuthorizedTaskIds.Any(string.IsNullOrWhiteSpace))
            return Deny("JAVIER_SIX_TASK_CATALOG_REQUIRED");

        var gate = V5OperationalAuditSecurityGate.Evaluate(security);
        if (!gate.NeedsExternalAdapterVerification)
            return Deny(gate.Code);

        // Never read a snapshot with an absent/unverified tenant.
        var tenant = security.ServerResolvedTenantId!;
        V5OperationalAuditEvidenceSnapshot? snapshot;
        try
        {
            snapshot = await _provider.ReadScopedAsync(tenant, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // Do not leak provider URL, tokens, response body or exception details.
            return Deny("EVIDENCE_PROVIDER_UNAVAILABLE");
        }

        if (snapshot is null)
            return Deny("EVIDENCE_PROVIDER_UNAVAILABLE");
        if (!string.Equals(snapshot.TenantId, tenant, StringComparison.Ordinal))
            return Deny("EVIDENCE_PROVIDER_TENANT_MISMATCH");
        if (snapshot.WorkItemIds is null ||
            snapshot.AuthorizedTaskIds is null ||
            snapshot.Items is null)
            return Deny("EVIDENCE_PROVIDER_INCOMPLETE");

        // Clone the provider-supplied data to avoid later collection mutation.
        var ids = new HashSet<string>(snapshot.WorkItemIds, StringComparer.Ordinal);
        var tasks = new HashSet<string>(snapshot.AuthorizedTaskIds, StringComparer.Ordinal);
        var items = snapshot.Items.ToArray();
        if (tasks.Count != expectedAuthorizedTaskIds.Count ||
            !tasks.SetEquals(expectedAuthorizedTaskIds))
            return Deny("TASK_CATALOG_MISMATCH");

        var source = new V5OperationalAuditDomainRules.SourceSnapshot(
            snapshot.SourceRevision, ids, tasks);
        var request = new V5OperationalAuditApplicationService.Request(
            expectedHeadSha!, snapshot.HeadSha, expectedSourceRevision!,
            source, items, new HashSet<string>(expectedAuthorizedTaskIds, StringComparer.Ordinal));
        var result = _application.PrepareWithTenantSecurityPreflight(
            request, security);

        // Application returns AUTHENTICATED_EVIDENCE_REQUIRED on an apparently
        // complete snapshot. It never turns provider data into a grant.
        return new Outcome(result.Code, result.ItemsExamined);
    }

    private static Outcome Deny(string code) => new(code, 0);

    private static bool FullSha(string? sha) =>
        sha is { Length: 40 } && sha.All(Uri.IsHexDigit);
}
