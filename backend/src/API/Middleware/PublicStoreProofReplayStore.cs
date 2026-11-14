using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Solqaryn.Infrastructure.Persistence;

namespace Solqaryn.API.Middleware;

/// <summary>
/// Consume a signed proof exactly once in shared MySQL, not local instance memory.
/// </summary>
public interface IPublicStoreProofReplayStore
{
    Task<bool> TryConsumeAsync(string proofHex, CancellationToken cancellationToken);
}

public sealed class MySqlPublicStoreProofReplayStore : IPublicStoreProofReplayStore
{
    private readonly AppDbContext _db;
    private readonly ILogger<MySqlPublicStoreProofReplayStore> _logger;
    private static long _accepted;

    public MySqlPublicStoreProofReplayStore(
        AppDbContext db, ILogger<MySqlPublicStoreProofReplayStore> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<bool> TryConsumeAsync(string proofHex, CancellationToken cancellationToken)
    {
        if (proofHex.Length != 64 ||
            proofHex.Any(c => !(c is >= '0' and <= '9' or >= 'a' and <= 'f')))
            return false;

        try
        {
            // The PK is the atomic decision across all Render API replicas.
            var written = await _db.Database.ExecuteSqlInterpolatedAsync(
                $@"INSERT INTO PublicStoreProofReplays (ProofDigest, ExpiresUtc)
                   VALUES (UNHEX({proofHex}), DATE_ADD(UTC_TIMESTAMP(6), INTERVAL 180 SECOND))",
                cancellationToken);
            if (written != 1)
                return false;

            if (Interlocked.Increment(ref _accepted) % 128 == 0)
            {
                // Delete only after the entire +/-60-second signed-proof window.
                try
                {
                    await _db.Database.ExecuteSqlRawAsync(
                        "DELETE FROM PublicStoreProofReplays WHERE ExpiresUtc < UTC_TIMESTAMP(6) LIMIT 512",
                        cancellationToken);
                }
                catch (Exception error) when (error is not OperationCanceledException)
                {
                    _logger.LogWarning("Store replay-ledger cleanup failed: {FailureType}.",
                        error.GetType().Name);
                }
            }

            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception error)
        {
            // Duplicate proof, missing table, database outage: all fail closed.
            // Never include token/host/secret in logs.
            _logger.LogWarning("Store proof rejected by replay ledger: {FailureType}.",
                error.GetType().Name);
            return false;
        }
    }
}
