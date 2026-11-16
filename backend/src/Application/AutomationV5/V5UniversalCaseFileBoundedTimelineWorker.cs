namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C01-04.M03.05: bounded, deterministic, in-process projection of already
/// trusted timeline snapshots. This does NOT authenticate an actor, grant reads,
/// resolve a tenant, query storage or authorize media/persistence. An authenticated
/// adapter must verify every record from an authoritative tenant-scoped source.
/// </summary>
public sealed class V5UniversalCaseFileBoundedTimelineWorker
{
    public const int MaximumEvents = 64;

    public sealed record TimelineEvent(
        string EventId, int TenantId, int CaseFileId, long Version,
        DateTimeOffset OccurredAtUtc, string Kind, string? OpaqueEvidenceKey);

    public sealed record Request(
        int TenantId, int CaseFileId, long ExpectedVersion,
        IReadOnlyCollection<TimelineEvent>? Events);

    public sealed record Projection(string Code, IReadOnlyList<TimelineEvent> Events)
    {
        public bool AuthorizesRead => false;
        public bool AllowsPersistence => false;
        public bool AllowsAttachmentAccess => false;
        public bool CertifiesTrustedSource => false;
    }

    public Projection Project(Request? request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request is null || request.TenantId <= 0 || request.CaseFileId <= 0 ||
            request.ExpectedVersion < 0)
            return Reject("CASEFILE_TIMELINE_SCOPE_INVALID");

        if (request.Events is null || request.Events.Count > MaximumEvents)
            return Reject("CASEFILE_TIMELINE_BOUND_INVALID");

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var projected = new List<TimelineEvent>(request.Events.Count);

        foreach (var item in request.Events)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (item is null || item.TenantId != request.TenantId ||
                item.CaseFileId != request.CaseFileId)
                return Reject("CASEFILE_TIMELINE_TENANT_OR_CASE_MISMATCH");

            if (item.Version < 0 || item.Version > request.ExpectedVersion ||
                item.OccurredAtUtc == default || item.OccurredAtUtc.Offset != TimeSpan.Zero)
                return Reject("CASEFILE_TIMELINE_VERSION_OR_TIME_INVALID");

            if (!SafeKey(item.EventId, 96) || !seen.Add(item.EventId) ||
                item.Kind is not ("OBSERVATION" or "DECISION" or "ATTACHMENT" or "VERSION") ||
                (item.OpaqueEvidenceKey is not null &&
                 !SafeKey(item.OpaqueEvidenceKey, 128)) ||
                (item.Kind == "ATTACHMENT" && item.OpaqueEvidenceKey is null))
                return Reject("CASEFILE_TIMELINE_EVENT_INVALID");

            projected.Add(item);
        }

        projected.Sort(static (a, b) =>
        {
            var byVersion = a.Version.CompareTo(b.Version);
            if (byVersion != 0) return byVersion;
            var byTime = a.OccurredAtUtc.CompareTo(b.OccurredAtUtc);
            return byTime != 0 ? byTime :
                StringComparer.Ordinal.Compare(a.EventId, b.EventId);
        });

        // An owned array and a read-only view prevent consumers from mutating
        // another request's snapshot after projection.
        return new Projection(
            projected.Count == 0 ? "CASEFILE_TIMELINE_EMPTY" : "CASEFILE_TIMELINE_PROJECTION_ONLY",
            Array.AsReadOnly(projected.ToArray()));
    }

    private static Projection Reject(string code) =>
        new(code, Array.Empty<TimelineEvent>());

    private static bool SafeKey(string? text, int maximum) =>
        !string.IsNullOrWhiteSpace(text) && text.Length <= maximum &&
        text.All(c => (c >= 'a' && c <= 'z') ||
                      (c >= 'A' && c <= 'Z') ||
                      (c >= '0' && c <= '9') ||
                      c is '-' or '_' or '.');
}
