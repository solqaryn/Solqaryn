using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C15-05.M03.04: isolated, read-only snapshot adapter for reproducible V5
/// operational audit preflight. No real authenticated Google Sheets/GitHub/CI
/// evidence, persistence, migration, institutional mutation or dispatcher.
/// </summary>
public sealed class V5OperationalAuditIsolatedSnapshotAdapter
{
    private readonly V5OperationalAuditApplicationService _service = new();
    private readonly string _revision;
    private readonly string _headSha;
    private readonly V5OperationalAuditDomainRules.WorkItem[] _items;
    private readonly HashSet<string> _authorizedItems;
    private readonly HashSet<string> _authorizedTasks;

    public V5OperationalAuditIsolatedSnapshotAdapter(
        string revision,
        string headSha,
        IEnumerable<V5OperationalAuditDomainRules.WorkItem> items,
        IEnumerable<string> authorizedItemIds,
        IEnumerable<string> authorizedTaskIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(revision);
        ArgumentException.ThrowIfNullOrWhiteSpace(headSha);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(authorizedItemIds);
        ArgumentNullException.ThrowIfNull(authorizedTaskIds);
        _revision = revision;
        _headSha = headSha;
        _items = items.ToArray();
        _authorizedItems = new HashSet<string>(authorizedItemIds, StringComparer.Ordinal);
        _authorizedTasks = new HashSet<string>(authorizedTaskIds, StringComparer.Ordinal);
        if (_items.Any(item => item is null))
            throw new ArgumentException("Null work items are not allowed.", nameof(items));
    }

    public V5OperationalAuditApplicationService.Result PrepareReadOnly(
        string? observedHeadSha, string? observedRevision)
    {
        var source = new V5OperationalAuditDomainRules.SourceSnapshot(
            _revision, _authorizedItems, _authorizedTasks);
        var request = new V5OperationalAuditApplicationService.Request(
            _headSha,
            observedHeadSha ?? string.Empty,
            observedRevision ?? string.Empty,
            source,
            _items,
            _authorizedTasks);
        // Caller-supplied snapshots always require authenticated external evidence.
        return _service.Prepare(request);
    }

    public bool AllowsPersistence => false;
    public bool AllowsInstitutionalWrite => false;
    public bool CanBypassTenantAuthorization => false;
}
