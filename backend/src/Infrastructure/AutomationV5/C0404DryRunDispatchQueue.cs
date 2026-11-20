using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C04-04.M03.05: isolated durable DRY-RUN queue only. Not registered in
/// AppDbContext, dependency injection, API, hosted workers or migrations.
/// No dequeue, dispatch, provider credentials, PSP, ledger or money movement.
/// Input MUST NOT be considered authenticated merely by being enqueued.
/// </summary>
public sealed class C0404DryRunQueueEntry
{
    public long Id { get; set; }
    public int EmpresaId { get; set; }
    public int FacturaId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty;
    public string PayloadDigest { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string State { get; set; } = "DryRunOnly";
    public DateTime CreatedUtc { get; set; }
}

public sealed class C0404DryRunQueueDbContext(
    DbContextOptions<C0404DryRunQueueDbContext> options) : DbContext(options)
{
    public DbSet<C0404DryRunQueueEntry> Entries => Set<C0404DryRunQueueEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<C0404DryRunQueueEntry>();
        e.ToTable("V22C0404DryRunQueue", table =>
        {
            table.HasCheckConstraint("CK_C0404_QueueTenant", "EmpresaId > 0 AND FacturaId > 0");
            table.HasCheckConstraint("CK_C0404_QueueAmount", "Amount > 0");
        });
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).ValueGeneratedOnAdd();
        e.Property(x => x.Provider).HasMaxLength(64).IsRequired();
        e.Property(x => x.KeyHash).HasMaxLength(64).IsRequired();
        e.Property(x => x.PayloadDigest).HasMaxLength(64).IsRequired();
        e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        e.Property(x => x.State).HasMaxLength(16).IsRequired();
        e.Property(x => x.Amount).HasPrecision(18, 2);
        e.Property(x => x.CreatedUtc).IsRequired();
        e.HasIndex(x => new { x.EmpresaId, x.Provider, x.KeyHash })
            .IsUnique().HasDatabaseName("UX_C0404_DryRunTenantProviderKey");
    }
}

/// <summary>
/// Durable, tenant-fenced idempotency PREVIEW with no effectful consumer.
/// A future authenticated integration requires a separate gate and cannot
/// interpret Queued/Replay as authorization to charge or contact a PSP.
/// </summary>
public sealed class C0404DryRunDispatchQueue(C0404DryRunQueueDbContext database)
{
    public enum Status { Denied, Missing, Queued, Replay, Conflict, Unavailable }

    public sealed record Command(
        int EmpresaId, int FacturaId, string Provider, string KeyHash,
        string PayloadDigest, decimal Amount, string Currency);

    public readonly record struct Result(Status Code, long? EntryId = null)
    {
        public bool MayContactProvider => false;
        public bool MayChargeOrRefund => false;
        public bool MayDispatchWorker => false;
        public bool CertifiesJwtRbacOrLivePayments => false;
    }

    public async Task<Result> InspectAsync(Command? command, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!Valid(command)) return new(Status.Denied);
        try
        {
            var row = await FindAsync(command!, token);
            token.ThrowIfCancellationRequested();
            return row is null ? new(Status.Missing) : Compare(command!, row);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException) { return new(Status.Unavailable); }
    }

    public async Task<Result> EnqueueDryRunAsync(Command? command, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!Valid(command)) return new(Status.Denied);

        var prior = await InspectAsync(command, token);
        if (prior.Code != Status.Missing) return prior;

        var row = new C0404DryRunQueueEntry
        {
            EmpresaId = command!.EmpresaId,
            FacturaId = command.FacturaId,
            Provider = command.Provider,
            KeyHash = command.KeyHash.ToLowerInvariant(),
            PayloadDigest = command.PayloadDigest.ToLowerInvariant(),
            Amount = command.Amount,
            Currency = command.Currency,
            State = "DryRunOnly",
            CreatedUtc = DateTime.UtcNow
        };

        database.Entries.Add(row);
        try
        {
            await database.SaveChangesAsync(token);
            token.ThrowIfCancellationRequested();
            return new(Status.Queued, row.Id);
        }
        catch (OperationCanceledException) { throw; }
        catch (DbUpdateException)
        {
            // Physical UNIQUE is the arbitration point; do not retry writes.
            database.ChangeTracker.Clear();
            var winner = await InspectAsync(command, token);
            return winner.Code is Status.Replay or Status.Conflict
                ? winner : new(Status.Unavailable);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            database.ChangeTracker.Clear();
            return new(Status.Unavailable);
        }
    }

    private Task<C0404DryRunQueueEntry?> FindAsync(Command command, CancellationToken token) =>
        database.Entries.AsNoTracking()
            .Where(x => x.EmpresaId == command.EmpresaId &&
                        x.Provider == command.Provider &&
                        x.KeyHash == command.KeyHash.ToLowerInvariant())
            .SingleOrDefaultAsync(token);

    private static Result Compare(Command request, C0404DryRunQueueEntry saved) =>
        saved.State == "DryRunOnly" &&
        saved.FacturaId == request.FacturaId &&
        saved.Amount == request.Amount &&
        saved.Currency == request.Currency &&
        string.Equals(saved.PayloadDigest, request.PayloadDigest, StringComparison.OrdinalIgnoreCase)
            ? new(Status.Replay, saved.Id)
            : new(Status.Conflict);

    private static bool Valid(Command? r) =>
        r is not null && r.EmpresaId > 0 && r.FacturaId > 0 &&
        r.Amount > 0m && r.Amount <= 999999999.99m &&
        decimal.Round(r.Amount, 2) == r.Amount &&
        r.Currency is { Length: 3 } &&
        r.Currency.All(c => c is >= 'A' and <= 'Z') &&
        r.Provider is { Length: >= 2 and <= 64 } &&
        r.Provider.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_') &&
        IsHex64(r.KeyHash) && IsHex64(r.PayloadDigest);

    private static bool IsHex64(string? value) =>
        value is { Length: 64 } && value.All(Uri.IsHexDigit);
}
