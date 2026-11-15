using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C15-02.M03.02: bounded, read-only architecture migration application preflight.
/// This does not provision providers, access data stores, mutate DNS or deploy.
/// Only a trusted backend adapter may supply authenticated boundary snapshots.
/// </summary>
public sealed class V5ArchitectureMigrationApplicationService
{
    public sealed record Request(
        V5ArchitectureMigrationDomainRules.TrustedBoundary? Boundary,
        IReadOnlyList<V5ArchitectureMigrationDomainRules.Candidate?>? Candidates,
        int MaxCandidates = 64);

    public readonly record struct Result(string Code, int ItemsReviewed)
    {
        public bool AuthorizesDeployment => false;
        public bool AuthorizesDataMigration => false;
        public bool AuthorizesExternalWrite => false;
        public bool CertifiesBackupOrRestore => false;
    }

    public Result Prepare(Request? request)
    {
        if (request?.Boundary is null || request.Candidates is null)
            return new Result("TRUSTED_REQUEST_REQUIRED", 0);

        if (request.MaxCandidates is < 1 or > 64 ||
            request.Candidates.Count is < 1 ||
            request.Candidates.Count > request.MaxCandidates)
            return new Result("CANDIDATE_CAPACITY_DENIED", 0);

        var reviewed = 0;
        foreach (var candidate in request.Candidates)
        {
            if (candidate is null)
                return new Result("NULL_CANDIDATE_DENIED", reviewed);

            reviewed++;
            var verdict = V5ArchitectureMigrationDomainRules.Evaluate(
                request.Boundary, candidate);
            if (!verdict.RequiresIndependentRuntimeVerification)
                return new Result(verdict.Code, reviewed);
        }

        // A positive structural review is never evidence that an authenticated
        // adapter, MySQL restore, DNS provider, security test or A5 ran.
        return new Result("INDEPENDENT_RUNTIME_EVIDENCE_AND_A5_REQUIRED", reviewed);
    }
}
