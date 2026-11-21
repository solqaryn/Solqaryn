using System.Threading.Channels;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C04-02.M03.05: caller-driven, bounded, non-durable read-only preflight queue.
/// Not registered as a hosted service or payment/contract mutation capability.
/// </summary>
public sealed class C0402ContractReadOnlyPreflightQueue
{
    private readonly C0402ContractApplicationService _gate;
    private readonly Channel<C0402ContractApplicationService.ClientIntent> _pending;
    private int _draining;

    public C0402ContractReadOnlyPreflightQueue(C0402ContractApplicationService gate, int capacity = 16)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        if (capacity is < 1 or > 64)
            throw new ArgumentOutOfRangeException(nameof(capacity));
        _pending = Channel.CreateBounded<C0402ContractApplicationService.ClientIntent>(
            new BoundedChannelOptions(capacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait,
                AllowSynchronousContinuations = false
            });
    }

    public bool TryEnqueue(C0402ContractApplicationService.ClientIntent? intent)
    {
        if (intent is null ||
            intent.ContractIdHint <= 0 || intent.ExpectedVersion <= 0 ||
            intent.TenantHint is <= 0 ||
            intent.EffectiveUtc.Kind != DateTimeKind.Utc ||
            string.IsNullOrWhiteSpace(intent.Cause) ||
            intent.Cause.Length > 160 || intent.Cause.Any(char.IsControl) ||
            !Enum.IsDefined(intent.Operation) ||
            intent.RequestsDirectMutation || intent.RequestsPaymentOrRefund ||
            intent.SuppliesRoleOrPolicyAuthority)
            return false;
        // FullMode.Wait makes TryWrite reject full queues without evicting work.
        return _pending.Writer.TryWrite(intent);
    }

    /// <summary>
    /// Every item re-enters the server-side JWT/RBAC/tenant/policy preflight.
    /// Never starts a daemon, authorizes writes or guarantees durable delivery.
    /// </summary>
    public async Task<IReadOnlyList<C0402ContractApplicationService.Preview>> DrainAsync(
        int maxItems, CancellationToken cancellationToken = default)
    {
        if (maxItems is < 1 or > 64)
            throw new ArgumentOutOfRangeException(nameof(maxItems));
        cancellationToken.ThrowIfCancellationRequested();
        if (Interlocked.CompareExchange(ref _draining, 1, 0) != 0)
            throw new InvalidOperationException("CONTRACT_PREFLIGHT_DRAIN_ALREADY_ACTIVE");
        try
        {
            var results = new List<C0402ContractApplicationService.Preview>(Math.Min(16, maxItems));
            while (results.Count < maxItems && _pending.Reader.TryRead(out var intent))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var preview = await _gate.PrepareAsync(intent, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                results.Add(preview);
            }
            return results;
        }
        finally
        {
            Volatile.Write(ref _draining, 0);
        }
    }
}
