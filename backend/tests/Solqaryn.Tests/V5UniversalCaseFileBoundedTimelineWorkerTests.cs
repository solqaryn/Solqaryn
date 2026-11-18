using System.Collections;
using Solqaryn.Application.AutomationV5;
using Xunit;
using Worker = Solqaryn.Application.AutomationV5.V5UniversalCaseFileBoundedTimelineWorker;

namespace Solqaryn.Tests;

public sealed class V5UniversalCaseFileBoundedTimelineWorkerTests
{
    private readonly Worker _worker = new();
    private static readonly DateTimeOffset Moment =
        new(2026, 11, 16, 17, 0, 0, TimeSpan.Zero);

    private static Worker.TimelineEvent E(
        string id = "event-1", int tenant = 7, int caseId = 42,
        long version = 1, string kind = "OBSERVATION",
        string? evidence = null, DateTimeOffset? at = null) =>
        new(id, tenant, caseId, version, at ?? Moment, kind, evidence);

    private static Worker.Request R(params Worker.TimelineEvent[] items) =>
        new(7, 42, 10, items);

    [Fact]
    public void Valid_entries_are_deterministically_ordered_but_not_authorized()
    {
        var result = _worker.Project(R(
            E("later", version: 3), E("earlier", version: 1),
            E("attachment", version: 2, kind: "ATTACHMENT", evidence: "file-3")));
        Assert.Equal("CASEFILE_TIMELINE_PROJECTION_ONLY", result.Code);
        Assert.Equal(new[] { "earlier", "attachment", "later" },
            result.Events.Select(x => x.EventId));
        Assert.False(result.AuthorizesRead);
        Assert.False(result.AllowsPersistence);
        Assert.False(result.AllowsAttachmentAccess);
        Assert.False(result.CertifiesTrustedSource);
    }

    [Fact]
    public void Tenant_A_cannot_project_tenant_B_even_if_case_id_matches()
    {
        var result = _worker.Project(R(E(tenant: 8)));
        Assert.Equal("CASEFILE_TIMELINE_TENANT_OR_CASE_MISMATCH", result.Code);
        Assert.Empty(result.Events);
    }

    [Fact]
    public void Different_case_id_is_rejected_without_partial_output()
    {
        var result = _worker.Project(R(E(), E("other", caseId: 43)));
        Assert.Equal("CASEFILE_TIMELINE_TENANT_OR_CASE_MISMATCH", result.Code);
        Assert.Empty(result.Events);
    }

    [Fact]
    public void Duplicate_event_identity_is_rejected_without_partial_output()
    {
        var result = _worker.Project(R(E(), E(version: 2)));
        Assert.Equal("CASEFILE_TIMELINE_EVENT_INVALID", result.Code);
        Assert.Empty(result.Events);
    }

    [Fact]
    public void Future_version_or_non_utc_timestamp_is_rejected()
    {
        Assert.Equal("CASEFILE_TIMELINE_VERSION_OR_TIME_INVALID",
            _worker.Project(R(E(version: 11))).Code);
        Assert.Equal("CASEFILE_TIMELINE_VERSION_OR_TIME_INVALID",
            _worker.Project(R(E(at: Moment.ToOffset(TimeSpan.FromHours(-6))))).Code);
    }

    [Fact]
    public void Unsafe_evidence_and_missing_attachment_key_are_rejected()
    {
        Assert.Equal("CASEFILE_TIMELINE_EVENT_INVALID",
            _worker.Project(R(E(kind: "ATTACHMENT"))).Code);
        Assert.Equal("CASEFILE_TIMELINE_EVENT_INVALID",
            _worker.Project(R(E(evidence: "https://example.invalid/token"))).Code);
        Assert.Equal("CASEFILE_TIMELINE_EVENT_INVALID",
            _worker.Project(R(E(id: "actor@example.invalid"))).Code);
    }

    [Fact]
    public void Maximum_boundary_is_accepted_and_overflow_is_denied()
    {
        var sixtyFour = Enumerable.Range(1, Worker.MaximumEvents)
            .Select(i => E("event-" + i, version: i)).ToArray();
        Assert.Equal(Worker.MaximumEvents, _worker.Project(
            new Worker.Request(7, 42, 64, sixtyFour)).Events.Count);

        var sixtyFive = sixtyFour.Append(E("overflow", version: 65)).ToArray();
        var denied = _worker.Project(new Worker.Request(7, 42, 65, sixtyFive));
        Assert.Equal("CASEFILE_TIMELINE_BOUND_INVALID", denied.Code);
        Assert.Empty(denied.Events);
    }

    private sealed class SpoofedTimelineCollection : IReadOnlyCollection<Worker.TimelineEvent>
    {
        private readonly IEnumerable<Worker.TimelineEvent> _events;
        public SpoofedTimelineCollection(int reportedCount, IEnumerable<Worker.TimelineEvent> events)
        {
            Count = reportedCount;
            _events = events;
        }
        public int Count { get; }
        public IEnumerator<Worker.TimelineEvent> GetEnumerator() => _events.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Fact]
    public void False_small_count_cannot_bypass_sixty_four_event_limit()
    {
        var sixtyFive = Enumerable.Range(1, Worker.MaximumEvents + 1)
            .Select(i => E("event-" + i, version: i)).ToArray();
        var result = _worker.Project(new Worker.Request(7, 42, 70,
            new SpoofedTimelineCollection(1, sixtyFive)));
        Assert.Equal("CASEFILE_TIMELINE_BOUND_INVALID", result.Code);
        Assert.Empty(result.Events);
    }

    [Fact]
    public void Declared_count_that_disagrees_with_iteration_is_denied()
    {
        var result = _worker.Project(new Worker.Request(7, 42, 10,
            new SpoofedTimelineCollection(2, new[] { E() })));
        Assert.Equal("CASEFILE_TIMELINE_BOUND_INVALID", result.Code);
        Assert.Empty(result.Events);
    }

    private static IEnumerable<Worker.TimelineEvent> EnumerateAndFail()
    {
        yield return E();
        throw new InvalidOperationException("confidential provider data should never escape");
    }

    [Fact]
    public void Throwing_enumerator_is_sanitized_without_partial_timeline()
    {
        var result = _worker.Project(new Worker.Request(7, 42, 10,
            new SpoofedTimelineCollection(2, EnumerateAndFail())));
        Assert.Equal("CASEFILE_TIMELINE_SOURCE_UNAVAILABLE", result.Code);
        Assert.Empty(result.Events);
    }

    [Fact]
    public void Caller_mutations_cannot_rewrite_returned_snapshot()
    {
        var caller = new List<Worker.TimelineEvent> { E() };
        var result = _worker.Project(new Worker.Request(7, 42, 10, caller));
        caller.Clear();
        Assert.Single(result.Events);
        Assert.Throws<NotSupportedException>(() =>
            ((IList<Worker.TimelineEvent>)result.Events).Clear());
        Assert.Single(result.Events);
    }

    [Fact]
    public void Cancelled_projection_does_not_return_data()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() =>
            _worker.Project(R(E()), cancellation.Token));
    }

    [Fact]
    public void Invalid_scope_and_empty_sequence_are_distinct()
    {
        Assert.Equal("CASEFILE_TIMELINE_SCOPE_INVALID",
            _worker.Project(new Worker.Request(0, 42, 0, Array.Empty<Worker.TimelineEvent>())).Code);
        var empty = _worker.Project(R());
        Assert.Equal("CASEFILE_TIMELINE_EMPTY", empty.Code);
        Assert.Empty(empty.Events);
    }
}
