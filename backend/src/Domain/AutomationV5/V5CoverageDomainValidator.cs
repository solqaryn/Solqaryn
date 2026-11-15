using System.Text.RegularExpressions;

namespace Solqaryn.Domain.AutomationV5;

/// <summary>
/// Pure, fail-closed validation of a *caller-supplied* snapshot of requirement
/// coverage. A consistent snapshot is only ready for independent evidence
/// review; this class cannot certify source completeness, CI, A5 or tenant access.
/// No storage, task dispatch, authentication or external calls are performed.
/// </summary>
public static class V5CoverageDomainValidator
{
    public sealed record SourceSnapshot(
        string SourceRevision,
        IReadOnlySet<string> ApprovedRequirementKeys,
        IReadOnlySet<string> ExistingPlanItemIds);

    public readonly record struct Assessment(
        string Code,
        int MappingsExamined,
        int RequirementsWithoutMapping)
    {
        public bool ReadyForEvidenceReview => Code == "READY_FOR_EVIDENCE_REVIEW";
    }

    private static readonly Regex WorkItemIdFormat = new(
        @"^C[0-9]{2}-[0-9]{2}\.M[0-9]{2}\.[0-9]{2}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static Assessment Assess(
        SourceSnapshot source,
        IReadOnlyCollection<V5RequirementCoverageRules.Entry> mappings)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(mappings);
        ArgumentNullException.ThrowIfNull(source.ApprovedRequirementKeys);
        ArgumentNullException.ThrowIfNull(source.ExistingPlanItemIds);

        if (string.IsNullOrWhiteSpace(source.SourceRevision))
            return Reject("SOURCE_VERSION_REQUIRED");
        if (source.ApprovedRequirementKeys.Count == 0 ||
            source.ExistingPlanItemIds.Count == 0)
            return Reject("SOURCE_CATALOG_EMPTY");
        if (mappings.Count == 0)
            return Reject("COVERAGE_EMPTY", 0, source.ApprovedRequirementKeys.Count);

        foreach (var entry in mappings)
        {
            if (entry is null)
                return Reject("NULL_MAPPING", mappings.Count);
            if (!string.Equals(entry.SourceVersion, source.SourceRevision, StringComparison.Ordinal))
                return Reject("SOURCE_VERSION_MISMATCH", mappings.Count);
            if (string.IsNullOrWhiteSpace(entry.WorkItemId) ||
                !WorkItemIdFormat.IsMatch(entry.WorkItemId))
                return Reject("INVALID_WORK_ITEM_FORMAT", mappings.Count);
            if (!SafeReference(entry.ContractReference) ||
                !SafeReference(entry.TestReference) ||
                !SafeReference(entry.EvidenceReference))
                return Reject("UNSAFE_REFERENCE", mappings.Count);
        }

        var baseVerdict = V5RequirementCoverageRules.ValidateBatch(
            mappings, source.ApprovedRequirementKeys, source.ExistingPlanItemIds);
        if (!baseVerdict.Accepted)
            return Reject(baseVerdict.Code, mappings.Count);

        // Only compares against the supplied authorized source slice; the caller
        // must still prove that this slice represents the full current STANDARD.
        var coveredKeys = new HashSet<string>(
            mappings.Select(x => x.RequirementKey), StringComparer.Ordinal);
        var missing = source.ApprovedRequirementKeys.Count(x => !coveredKeys.Contains(x));
        if (missing != 0)
            return Reject("REQUIREMENTS_UNMAPPED", mappings.Count, missing);

        return new Assessment("READY_FOR_EVIDENCE_REVIEW", mappings.Count, 0);
    }

    private static bool SafeReference(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 512 &&
        value.All(c => !char.IsControl(c));

    private static Assessment Reject(string code, int count = 0, int missing = 0) =>
        new(code, count, missing);
}
