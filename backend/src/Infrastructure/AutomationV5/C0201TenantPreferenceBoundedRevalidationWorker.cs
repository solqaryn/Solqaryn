using Gate = Solqaryn.Application.AutomationV5.V17IntelligentTenantLoginAuthorization;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C02-01.M03.05: bounded, in-process revalidation preview for one actor.
/// This is not a hosted worker, live authority reader, tenant switch,
/// persistence provider, JWT issuer or durable queue.
/// </summary>
public sealed class C0201TenantPreferenceBoundedRevalidationWorker
{
    public const int MaximumBatch = 12;
    private readonly C0201TenantPreferenceIsolatedSnapshotAdapter _adapter;

    public C0201TenantPreferenceBoundedRevalidationWorker(
        C0201TenantPreferenceIsolatedSnapshotAdapter adapter) =>
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));

    public sealed record Input(string AttemptKey, Gate.Request? Request);
    public sealed record Item(string AttemptKey, string DecisionCode,
        int? ProposedTenantId, int ChoiceCount, bool DiscardStalePreference);
    public sealed record Review(string Code, IReadOnlyList<Item> Items)
    {
        public bool IssuesJwt => false;
        public bool AuthorizesTenantOrResourceAccess => false;
        public bool StartsHostedWorker => false;
        public bool WritesTenantPreference => false;
        public bool SwitchesTenantSession => false;
        public bool WritesDurableQueue => false;
        public bool ReadsOrWritesMySql => false;
        public bool CertifiesProduction => false;
    }

    public async ValueTask<Review> PreviewAsync(
        IReadOnlyList<Input>? inputs,
        int observedAuthenticatedActorId, long observedRevision,
        string? observedSourceSha,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (inputs is null || inputs.Count is < 1 or > MaximumBatch)
            return Deny("PREVIEW_BATCH_REQUIRED_OR_TOO_LARGE");
        if (observedAuthenticatedActorId <= 0 || observedRevision <= 0 ||
            !FullSha(observedSourceSha))
            return Deny("PREVIEW_TRUSTED_CONTEXT_REQUIRED");

        var attemptKeys = new HashSet<string>(StringComparer.Ordinal);
        int? expectedTenant = null;
        bool? expectedSwitch = null;
        var selectionInitialized = false;

        // Reject the entire malformed batch BEFORE consulting even the
        // isolated read-only adapter. No partial preference resolution.
        foreach (var entry in inputs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (entry is null || !SafeKey(entry.AttemptKey) ||
                entry.Request is null ||
                entry.Request.ClientClaimsGlobalRole ||
                entry.Request.ClientClaimsMembership ||
                entry.Request.ClientClaimsHostAuthority ||
                entry.Request.SelectedTenantId is <= 0 ||
                (entry.Request.SwitchingEnterprise &&
                 !entry.Request.SelectedTenantId.HasValue))
                return Deny("PREVIEW_UNTRUSTED_REQUEST_DENIED");

            if (!attemptKeys.Add(entry.AttemptKey))
                return Deny("PREVIEW_DUPLICATE_ATTEMPT_DENIED");

            if (!selectionInitialized)
            {
                expectedTenant = entry.Request.SelectedTenantId;
                expectedSwitch = entry.Request.SwitchingEnterprise;
                selectionInitialized = true;
            }
            else if (expectedTenant != entry.Request.SelectedTenantId ||
                     expectedSwitch != entry.Request.SwitchingEnterprise)
                return Deny("PREVIEW_MIXED_SELECTION_DENIED");
        }

        var rows = new List<Item>(inputs.Count);
        foreach (var entry in inputs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Gate.Decision result;
            try
            {
                result = await _adapter.EvaluateReadOnlyAsync(
                    entry.Request, observedAuthenticatedActorId,
                    observedRevision, observedSourceSha, cancellationToken);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                return Deny("PREVIEW_AUTHORITY_UNAVAILABLE");
            }

            if (!IsPreviewOnlyDecision(result.Code) ||
                result.AuthorizesResourceAccess || result.IssuesJwt ||
                result.InstallsTenantContext || result.AuthorizesGlobalConsole ||
                result.Choices.Count > 128 ||
                result.ProposedTenantId is <= 0)
                return Deny("PREVIEW_AUTHORITY_DENIED");

            // No display names, browser hints, JWTs, credentials or
            // cross-tenant session data are included in preview results.
            rows.Add(new Item(entry.AttemptKey, result.Code,
                result.ProposedTenantId, result.Choices.Count,
                result.DiscardStalePreference));
        }

        return new Review("PREVIEW_ONLY_AUTHENTICATED_BACKEND_REISSUE_REQUIRED",
            rows.AsReadOnly());
    }

    private static bool IsPreviewOnlyDecision(string code) =>
        code is "ONE_TENANT_REISSUE_REQUIRED" or
            "PREFERENCE_REISSUE_REQUIRED" or
            "CHOICE_REISSUE_REQUIRED" or
            "SWITCH_REISSUE_REQUIRED" or
            "SELECT_VERIFIED_TENANT" or
            "CURRENT_SESSION_BACKEND_CHECK_REQUIRED";

    private static bool SafeKey(string? key) =>
        key is { Length: >= 3 and <= 96 } &&
        key.All(c => (c >= 'a' && c <= 'z') ||
                     (c >= 'A' && c <= 'Z') ||
                     (c >= '0' && c <= '9') ||
                     c is '-' or '_' or '.');

    private static bool FullSha(string? sha) =>
        sha is { Length: 40 } &&
        sha.All(c => (c >= '0' && c <= '9') ||
                     (c >= 'a' && c <= 'f'));

    private static Review Deny(string code) =>
        new(code, Array.Empty<Item>());
}
