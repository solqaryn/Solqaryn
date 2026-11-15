namespace Solqaryn.Domain.AutomationV5;

/// <summary>
/// C15-02.M03.01: pure architecture migration safety preflight.
/// A positive verdict only requests verification by a trusted server adapter;
/// it never provisions infrastructure, performs SQL, changes DNS or deploys.
/// </summary>
public static class V5ArchitectureMigrationDomainRules
{
    public sealed record TrustedBoundary(
        string? ExpectedCommitSha,
        string? AuthenticatedTenantId,
        bool CurrentMembershipVerified,
        bool ExplicitDevScopeVerified,
        IReadOnlySet<string> ApprovedProviderCatalog);

    public sealed record Candidate(
        string? ManifestSha,
        string? TenantId,
        string? SourceProvider,
        string? TargetProvider,
        string? Environment,
        bool SourceOwnershipFromTrustedCatalog,
        bool TargetOwnershipVerified,
        bool RequiresLiveDnsOrSecretMutation,
        bool DestructiveDatabaseOperation,
        bool BackupChecksumVerified,
        bool SandboxRestoreVerified,
        bool TenantMappingVerified,
        bool ReversibleRollbackPlanVerified,
        bool HasUnmitigatedCriticalFinding);

    public readonly record struct Verdict(string Code)
    {
        public bool RequiresIndependentRuntimeVerification =>
            Code == "READY_FOR_TRUSTED_ARCHITECTURE_REVIEW";

        public bool AuthorizesDeployment => false;
        public bool AuthorizesDataMigration => false;
    }

    public static Verdict Evaluate(TrustedBoundary? boundary, Candidate? candidate)
    {
        if (boundary is null || candidate is null ||
            !FullSha(boundary.ExpectedCommitSha) ||
            string.IsNullOrWhiteSpace(boundary.AuthenticatedTenantId) ||
            !boundary.CurrentMembershipVerified ||
            !boundary.ExplicitDevScopeVerified ||
            boundary.ApprovedProviderCatalog is null ||
            boundary.ApprovedProviderCatalog.Count == 0)
            return new Verdict("TRUSTED_DEV_AUTHORITY_REQUIRED");

        if (!string.Equals(candidate.Environment, "dev", StringComparison.Ordinal))
            return new Verdict("PRODUCTION_OR_QA_NOT_AUTHORIZED");

        if (!string.Equals(candidate.ManifestSha, boundary.ExpectedCommitSha,
                StringComparison.Ordinal))
            return new Verdict("MANIFEST_EXACT_SHA_REQUIRED");

        if (!string.Equals(candidate.TenantId, boundary.AuthenticatedTenantId,
                StringComparison.Ordinal) ||
            !candidate.SourceOwnershipFromTrustedCatalog ||
            !candidate.TargetOwnershipVerified ||
            !candidate.TenantMappingVerified)
            return new Verdict("TENANT_OWNERSHIP_OR_MAPPING_UNVERIFIED");

        if (string.IsNullOrWhiteSpace(candidate.SourceProvider) ||
            string.IsNullOrWhiteSpace(candidate.TargetProvider) ||
            !boundary.ApprovedProviderCatalog.Contains(candidate.SourceProvider) ||
            !boundary.ApprovedProviderCatalog.Contains(candidate.TargetProvider))
            return new Verdict("PROVIDER_NOT_IN_VERIFIED_DEV_CATALOG");

        if (candidate.RequiresLiveDnsOrSecretMutation ||
            candidate.DestructiveDatabaseOperation)
            return new Verdict("RUNTIME_MUTATION_OUT_OF_SCOPE");

        if (candidate.HasUnmitigatedCriticalFinding)
            return new Verdict("UNMITIGATED_CRITICAL_FINDING");

        if (!candidate.BackupChecksumVerified ||
            !candidate.SandboxRestoreVerified ||
            !candidate.ReversibleRollbackPlanVerified)
            return new Verdict("RECOVERY_EVIDENCE_REQUIRED");

        // Even validated snapshots can be forged by an untrusted caller.
        // API/Application must independently authenticate their provenance,
        // check real CI, audit tenant A/B and obtain environment authorization.
        return new Verdict("READY_FOR_TRUSTED_ARCHITECTURE_REVIEW");
    }

    private static bool FullSha(string? value) =>
        value is { Length: 40 } && value.All(Uri.IsHexDigit);
}
