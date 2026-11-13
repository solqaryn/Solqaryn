using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// In-memory, scope-isolated, revision-checked adapter for V5 coverage mappings.
/// This is an isolated testable adapter, NOT Google Sheets/MySQL persistence or a lease.
/// The caller supplies the current authorization allowlists and catalog snapshot.
/// </summary>
public sealed class V5CoverageMemoryAdapter
{
    public sealed record Stored(V5RequirementCoverageRules.Entry Entry, long Revision);
    public sealed record Result(bool Accepted, string Code, long Revision);
    public sealed record ReadResult(bool Allowed, string Code, Stored? Item);

    private readonly object _sync = new();
    private readonly Dictionary<(string Scope, string Requirement, string WorkItem, string Version), Stored> _items = new();
    private readonly Dictionary<(string Scope, string Key), (string MappingKey, Stored Snapshot)> _idempotency = new();
    private readonly int _capacity;

    public V5CoverageMemoryAdapter(int capacity = 1000)
    {
        if (capacity < 1)
            throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = capacity;
    }

    public Result TrySave(
        string actorTaskId,
        string scopeKey,
        V5RequirementCoverageRules.Entry entry,
        long expectedRevision,
        string idempotencyKey,
        IReadOnlySet<string> authorizedTaskIds,
        IReadOnlySet<string> authorizedScopeKeys,
        IReadOnlySet<string> authorizedRequirementKeys,
        IReadOnlySet<string> existingWorkItemIds)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(authorizedTaskIds);
        ArgumentNullException.ThrowIfNull(authorizedScopeKeys);
        ArgumentNullException.ThrowIfNull(authorizedRequirementKeys);
        ArgumentNullException.ThrowIfNull(existingWorkItemIds);
        if (string.IsNullOrWhiteSpace(actorTaskId) || !authorizedTaskIds.Contains(actorTaskId))
            return new(false, "UNAUTHORIZED_ACTOR", 0);
        if (string.IsNullOrWhiteSpace(scopeKey) || !authorizedScopeKeys.Contains(scopeKey))
            return new(false, "SCOPE_DENIED", 0);
        if (string.IsNullOrWhiteSpace(idempotencyKey) || expectedRevision < 0)
            return new(false, "INVALID_WRITE_PRECONDITION", 0);

        var validation = V5RequirementCoverageRules.Validate(
            entry, authorizedRequirementKeys, existingWorkItemIds);
        if (!validation.Accepted)
            return new(false, validation.Code, 0);

        var key = (scopeKey, entry.RequirementKey, entry.WorkItemId, entry.SourceVersion);
        var mappingKey = string.Join("\u001f", entry.RequirementKey, entry.WorkItemId,
            entry.SourceVersion, entry.ContractReference, entry.TestReference, entry.EvidenceReference);
        lock (_sync)
        {
            var idem = (scopeKey, idempotencyKey);
            if (_idempotency.TryGetValue(idem, out var previous))
            {
                // Replay succeeds only while it still describes the current identical mapping.
                if (previous.MappingKey == mappingKey &&
                    _items.TryGetValue(key, out var latest) &&
                    latest == previous.Snapshot)
                    return new(true, "IDEMPOTENT_REPLAY", latest.Revision);
                return new(false, "IDEMPOTENCY_CONFLICT", 0);
            }

            var exists = _items.TryGetValue(key, out var stored);
            var currentRevision = exists ? stored!.Revision : 0;
            if (currentRevision != expectedRevision)
                return new(false, "REVISION_CONFLICT", currentRevision);
            if ((!exists && _items.Count >= _capacity) || _idempotency.Count >= _capacity)
                return new(false, "CAPACITY_REACHED", currentRevision);

            var newStored = new Stored(entry, checked(currentRevision + 1));
            _items[key] = newStored;
            _idempotency[idem] = (mappingKey, newStored);
            return new(true, "SAVED", newStored.Revision);
        }
    }

    public ReadResult TryRead(
        string actorTaskId,
        string scopeKey,
        string requirementKey,
        string workItemId,
        string sourceVersion,
        IReadOnlySet<string> authorizedTaskIds,
        IReadOnlySet<string> authorizedScopeKeys)
    {
        ArgumentNullException.ThrowIfNull(authorizedTaskIds);
        ArgumentNullException.ThrowIfNull(authorizedScopeKeys);
        if (string.IsNullOrWhiteSpace(actorTaskId) || !authorizedTaskIds.Contains(actorTaskId))
            return new(false, "UNAUTHORIZED_ACTOR", null);
        if (string.IsNullOrWhiteSpace(scopeKey) || !authorizedScopeKeys.Contains(scopeKey))
            return new(false, "SCOPE_DENIED", null);
        lock (_sync)
        {
            return _items.TryGetValue((scopeKey, requirementKey, workItemId, sourceVersion),
                out var stored)
                ? new(true, "FOUND", stored)
                : new(true, "NOT_FOUND", null);
        }
    }
}
