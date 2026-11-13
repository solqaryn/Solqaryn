namespace Solqaryn.Domain.AutomationV5;

/// <summary>
/// Pure server-side rules for mapping an approved V5 requirement to existing work items.
/// The caller supplies the verified, current V5 catalog and requirement keys.
/// No new backlog item, database table, authorization grant or dispatcher is created.
/// </summary>
public static class V5RequirementCoverageRules
{
    public sealed record Entry(
        string RequirementKey,
        string WorkItemId,
        string SourceVersion,
        string ContractReference,
        string TestReference,
        string EvidenceReference);

    public readonly record struct Verdict(bool Accepted, string Code)
    {
        public static Verdict Pass() => new(true, "OK");
        public static Verdict Reject(string code) => new(false, code);
    }

    /// <summary>Validate the complete, explicit contract of one coverage mapping.</summary>
    public static Verdict Validate(
        Entry entry,
        IReadOnlySet<string> authorizedRequirementKeys,
        IReadOnlySet<string> existingWorkItemIds)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(authorizedRequirementKeys);
        ArgumentNullException.ThrowIfNull(existingWorkItemIds);

        if (string.IsNullOrWhiteSpace(entry.RequirementKey) ||
            !authorizedRequirementKeys.Contains(entry.RequirementKey))
            return Verdict.Reject("UNKNOWN_REQUIREMENT");

        if (string.IsNullOrWhiteSpace(entry.WorkItemId) ||
            !existingWorkItemIds.Contains(entry.WorkItemId))
            return Verdict.Reject("UNKNOWN_WORK_ITEM");

        if (string.IsNullOrWhiteSpace(entry.SourceVersion))
            return Verdict.Reject("SOURCE_VERSION_REQUIRED");

        if (string.IsNullOrWhiteSpace(entry.ContractReference))
            return Verdict.Reject("CONTRACT_REQUIRED");

        if (string.IsNullOrWhiteSpace(entry.TestReference))
            return Verdict.Reject("TEST_REQUIRED");

        if (string.IsNullOrWhiteSpace(entry.EvidenceReference))
            return Verdict.Reject("EVIDENCE_REQUIRED");

        return Verdict.Pass();
    }

    /// <summary>
    /// Validate a batch without mutating the caller's collection. The pair (requirement,
    /// work item, source version) must be unique; many-to-many coverage is supported.
    /// </summary>
    public static Verdict ValidateBatch(
        IEnumerable<Entry> entries,
        IReadOnlySet<string> authorizedRequirementKeys,
        IReadOnlySet<string> existingWorkItemIds)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(authorizedRequirementKeys);
        ArgumentNullException.ThrowIfNull(existingWorkItemIds);

        var seen = new HashSet<(string Requirement, string WorkItem, string Version)>();
        foreach (var entry in entries)
        {
            var verdict = Validate(entry, authorizedRequirementKeys, existingWorkItemIds);
            if (!verdict.Accepted)
                return verdict;

            var fingerprint = (entry.RequirementKey, entry.WorkItemId, entry.SourceVersion);
            if (!seen.Add(fingerprint))
                return Verdict.Reject("DUPLICATE_COVERAGE_MAPPING");
        }

        return Verdict.Pass();
    }
}
