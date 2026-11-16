using App = Solqaryn.Application.AutomationV5.C0501DesignTokenApplicationService;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C05-01.M03.05: bounded in-process synthetic design-token review worker.
/// Not a hosted worker, CSS applier, JWT authenticator or live provider.
/// A successful preview still requires a separate authenticated backend adapter.
/// </summary>
public sealed class C0501DesignTokenBoundedReviewWorker
{
    public const int MaximumBatch = 12;
    private readonly C0501DesignTokenIsolatedSnapshotAdapter _adapter;

    public C0501DesignTokenBoundedReviewWorker(
        C0501DesignTokenIsolatedSnapshotAdapter adapter) =>
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));

    public sealed record Input(string? AttemptKey, App.ClientIntent? Intent);
    public sealed record Item(int Ordinal, string DecisionCode);
    public sealed record Result(string Code, IReadOnlyList<Item> Items)
    {
        public bool AuthorizesThemeChange => false;
        public bool AuthorizesTenantAccess => false;
        public bool PersistsAnything => false;
        public bool ReadsOrWritesMysql => false;
        public bool AppliesCss => false;
        public bool StartsHostedWorker => false;
        public bool CertifiesAccessibility => false;
        public bool CertifiesProduction => false;
    }

    public async ValueTask<Result> PreviewAsync(
        IReadOnlyList<Input>? batch,
        int authenticatedActorId, int authenticatedTenantId,
        long observedRevision, int observedSessionEpoch,
        string? observedSourceSha,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (batch is null || batch.Count is < 1 or > MaximumBatch)
            return Deny("THEME_BATCH_REQUIRED_OR_TOO_LARGE");
        if (authenticatedActorId <= 0 || authenticatedTenantId <= 0 ||
            observedRevision < 0 || observedSessionEpoch <= 0 ||
            !CanonicalSha(observedSourceSha))
            return Deny("THEME_TRUSTED_CONTEXT_REQUIRED");

        // Validate the entire batch before ever querying the isolated adapter.
        // Never allow a failed later item to leave an approved earlier item.
        var attempts = new HashSet<string>(StringComparer.Ordinal);
        var tokenKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (Input? entry in batch)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (entry is null || !SafeKey(entry.AttemptKey) ||
                entry.Intent is null || entry.Intent.DirectWrite ||
                entry.Intent.PlatformCatalogMutation ||
                entry.Intent.ClaimedTenantId != authenticatedTenantId ||
                entry.Intent.SessionEpoch != observedSessionEpoch ||
                entry.Intent.ExpectedRevision != observedRevision ||
                !SafeTokenKey(entry.Intent.TokenKey) ||
                string.IsNullOrWhiteSpace(entry.Intent.IdempotencyKey))
                return Deny("THEME_UNTRUSTED_BATCH_DENIED");
            if (!attempts.Add(entry.AttemptKey!))
                return Deny("THEME_DUPLICATE_ATTEMPT_DENIED");
            if (!tokenKeys.Add(entry.Intent.TokenKey!))
                return Deny("THEME_DUPLICATE_TOKEN_DENIED");
        }

        var items = new List<Item>(batch.Count);
        for (var i = 0; i < batch.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            App.Result decision;
            try
            {
                decision = await _adapter.InspectReadOnlyAsync(
                    batch[i].Intent, authenticatedActorId,
                    authenticatedTenantId, observedRevision,
                    observedSessionEpoch, observedSourceSha, cancellationToken);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                return Deny("THEME_ADAPTER_UNAVAILABLE");
            }

            if (!string.Equals(decision.Code,
                    "AUTHENTICATED_THEME_ADAPTER_REQUIRED", StringComparison.Ordinal) ||
                decision.AuthorizesThemeMutation ||
                decision.AuthorizesTenantAccess || decision.PersistsAnything ||
                decision.CertifiesAccessibility)
                return Deny("THEME_AUTHORITY_DENIED");
            items.Add(new Item(i, decision.Code));
        }

        return new Result("THEME_PREVIEW_ONLY_REAUTH_REQUIRED",
            items.AsReadOnly());
    }

    private static Result Deny(string code) => new(code, Array.Empty<Item>());

    private static bool SafeKey(string? value) =>
        value is { Length: >= 3 and <= 96 } &&
        value.All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z'
            or >= '0' and <= '9' or '-' or '_' or '.');

    private static bool SafeTokenKey(string? key) =>
        key is { Length: >= 3 and <= 64 } &&
        key[0] is >= 'a' and <= 'z' &&
        key[^1] != '-' &&
        key.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-');

    private static bool CanonicalSha(string? value) =>
        value is { Length: 40 } &&
        value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
}
