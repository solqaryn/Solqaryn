using System.Collections.Concurrent;
using Solqaryn.API.Middleware;

namespace Solqaryn.Tests.API;

/// <summary>Test-only atomic store; production always uses shared MySQL.</summary>
internal sealed class TestPublicStoreProofReplayStore : IPublicStoreProofReplayStore
{
    private readonly ConcurrentDictionary<string, byte> _used = new(StringComparer.Ordinal);
    public Task<bool> TryConsumeAsync(string proofHex, CancellationToken cancellationToken) =>
        Task.FromResult(_used.TryAdd(proofHex, 0));
}
