using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C04-04.M03.04: isolated durable MySQL preflight only. NOT registered in
/// AppDbContext, API, DI, migrations or commercial runtime. Authorization,
/// tenant membership, JWT signature and invoice ownership MUST be established
/// independently by server code before a future consumer can use it.
/// NEVER calls payment providers, charges, refunds or writes ledger entries.
/// </summary>
public sealed class C0404PaymentReservation
{
    public long Id { get; set; }
    public int EmpresaId { get; set; }
    public int FacturaId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty;
    public string PayloadDigest { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
}

public sealed class C0404PaymentReservationDbContext(
    DbContextOptions<C0404PaymentReservationDbContext> options) : DbContext(options)
{
    public DbSet<C0404PaymentReservation> Reservations => Set<C0404PaymentReservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<C0404PaymentReservation>();
        e.ToTable("V22C0404PaymentReservations", table =>
        {
            table.HasCheckConstraint("CK_C0404_ReservationTenant", "EmpresaId > 0 AND FacturaId > 0");
            table.HasCheckConstraint("CK_C0404_ReservationAmount", "Amount > 0");
        });
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).ValueGeneratedOnAdd();
        e.Property(x => x.Provider).HasMaxLength(64).IsRequired();
        e.Property(x => x.KeyHash).HasMaxLength(64).IsRequired();
        e.Property(x => x.PayloadDigest).HasMaxLength(64).IsRequired();
        e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        e.Property(x => x.Amount).HasPrecision(18, 2);
        e.Property(x => x.CreatedUtc).IsRequired();
        e.HasIndex(x => new { x.EmpresaId, x.Provider, x.KeyHash })
            .IsUnique().HasDatabaseName("UX_C0404_TenantProviderKey");
    }
}

public sealed class C0404PaymentReservationStore(C0404PaymentReservationDbContext database)
{
    public enum Status { Denied, Missing, Reserved, Replay, Conflict, Unavailable }
    public sealed record Command(int EmpresaId, int FacturaId, string Provider,
        string KeyHash, string PayloadDigest, decimal Amount, string Currency);
    public readonly record struct Result(Status Code, long? ReservationId = null)
    {
        public bool AuthorizesProviderCall => false;
        public bool AuthorizesChargeOrRefund => false;
        public bool CertifiesJwtOrCommercialIntegration => false;
    }

    public async Task<Result> InspectAsync(Command? command, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!Valid(command)) return new(Status.Denied);
        try
        {
            var persisted = await FindAsync(command!, token);
            token.ThrowIfCancellationRequested();
            return persisted is null ? new(Status.Missing) : Compare(command!, persisted);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException) { return new(Status.Unavailable); }
    }

    public async Task<Result> ReserveAsync(Command? command, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!Valid(command)) return new(Status.Denied);
        var prior = await InspectAsync(command, token);
        if (prior.Code != Status.Missing) return prior;
        var row = new C0404PaymentReservation
        {
            EmpresaId = command!.EmpresaId,
            FacturaId = command.FacturaId,
            Provider = command.Provider,
            KeyHash = command.KeyHash.ToLowerInvariant(),
            PayloadDigest = command.PayloadDigest.ToLowerInvariant(),
            Amount = command.Amount,
            Currency = command.Currency,
            CreatedUtc = DateTime.UtcNow
        };
        database.Reservations.Add(row);
        try
        {
            await database.SaveChangesAsync(token);
            token.ThrowIfCancellationRequested();
            return new(Status.Reserved, row.Id);
        }
        catch (OperationCanceledException) { throw; }
        catch (DbUpdateException)
        {
            // A different writer may have won the UNIQUE constraint. Never
            // retry the insert or invoke a provider; inspect the winner.
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

    private Task<C0404PaymentReservation?> FindAsync(Command command, CancellationToken token) =>
        database.Reservations.AsNoTracking()
            .Where(x => x.EmpresaId == command.EmpresaId &&
                        x.Provider == command.Provider &&
                        x.KeyHash == command.KeyHash.ToLowerInvariant())
            .SingleOrDefaultAsync(token);

    private static Result Compare(Command request, C0404PaymentReservation persisted) =>
        persisted.FacturaId == request.FacturaId &&
        persisted.Amount == request.Amount &&
        string.Equals(persisted.Currency, request.Currency, StringComparison.Ordinal) &&
        string.Equals(persisted.PayloadDigest, request.PayloadDigest, StringComparison.OrdinalIgnoreCase)
            ? new(Status.Replay, persisted.Id)
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
