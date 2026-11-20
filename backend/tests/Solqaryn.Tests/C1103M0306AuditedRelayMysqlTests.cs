using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Xunit;

namespace Solqaryn.Tests;

[Trait("Category", "Integration")]
public sealed class C1103M0306AuditedRelayMysqlTests
{
    private sealed class SafeLogger : ILogger<C1103OutboxAuditedRelay>
    {
        private sealed class Scope : IDisposable { public void Dispose() { } }
        public List<string> Messages { get; } = new();
        public bool ThrowOnWrite { get; init; }
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => new Scope();
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Assert.Null(exception);
            if (ThrowOnWrite) throw new InvalidOperationException("SINK_SECRET");
            Messages.Add(formatter(state, exception));
        }
    }

    private sealed class Transport(
        Func<int, Guid, string, string, string, CancellationToken, Task> handler)
        : IC1103OutboxIsolatedTransport
    {
        public Task SendAsync(int tenantId, Guid eventId, string kind,
            string payload, string key, CancellationToken ct)
            => handler(tenantId, eventId, kind, payload, key, ct);
    }

    [Fact]
    public async Task RealMysqlTenantAbDurableRetryAndSafeStructuredAudit()
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var connection = new MySqlConnectionStringBuilder(raw);
        if (connection.Server is not ("127.0.0.1" or "localhost") ||
            connection.Port != 3306 || connection.UserID != "root")
            throw new InvalidOperationException("EPHEMERAL_LOOPBACK_ONLY");
        connection.Database = "c1103_m0306_" + Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(connection.ConnectionString,
                x => x.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;
        try
        {
            await using var db = new AppDbContext(options);
            await Phase7MySqlTestDatabase.InitializeFreshAsync(db);
            var a = new Empresa("Audited tenant A");
            var b = new Empresa("Audited tenant B");
            db.Set<Empresa>().AddRange(a, b);
            await db.SaveChangesAsync();
            var now = DateTime.UtcNow;
            var own = MensajeOutbox.Crear(a.Id, "factura.emitida",
                "{\"token\":\"SECRET_PAYLOAD_A\"}", "SECRET_KEY_A",
                creadoEnUtc: now.AddMinutes(-2));
            var other = MensajeOutbox.Crear(b.Id, "factura.emitida",
                "{\"token\":\"SECRET_PAYLOAD_B\"}", "SECRET_KEY_B",
                creadoEnUtc: now.AddMinutes(-1));
            db.MensajesOutbox.AddRange(own, other);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            var called = 0;
            var worker = new C1103OutboxIsolatedRelayWorker(
                new MensajeOutboxRepository(db),
                new Transport((tenant, eventId, kind, payload, key, ct) =>
                {
                    Assert.Equal(a.Id, tenant);
                    Assert.Equal(own.EventoId, eventId);
                    ct.ThrowIfCancellationRequested();
                    if (++called == 1)
                        throw new InvalidOperationException("PROVIDER_SECRET_PII");
                    return Task.CompletedTask;
                }));
            var logger = new SafeLogger();
            var relay = new C1103OutboxAuditedRelay(worker, logger);
            var first = await relay.RunTenantAsync(a.Id, now, 2, 2, TimeSpan.FromMinutes(1));
            Assert.Equal(new C1103OutboxIsolatedRelayWorker.BatchResult(1, 0, 1, 0, 0), first);
            Assert.Equal(EstadoMensajeOutbox.Fallido,
                (await db.MensajesOutbox.AsNoTracking().SingleAsync(x => x.Id == own.Id)).Estado);
            Assert.Equal(EstadoMensajeOutbox.Pendiente,
                (await db.MensajesOutbox.AsNoTracking().SingleAsync(x => x.Id == other.Id)).Estado);
            var second = await relay.RunTenantAsync(
                a.Id, now.AddSeconds(61), 2, 2, TimeSpan.FromMinutes(1));
            Assert.Equal(new C1103OutboxIsolatedRelayWorker.BatchResult(1, 1, 0, 0, 0), second);
            Assert.Equal(EstadoMensajeOutbox.Entregado,
                (await db.MensajesOutbox.AsNoTracking().SingleAsync(x => x.Id == own.Id)).Estado);
            Assert.Equal(EstadoMensajeOutbox.Pendiente,
                (await db.MensajesOutbox.AsNoTracking().SingleAsync(x => x.Id == other.Id)).Estado);
            Assert.Equal(2, logger.Messages.Count);
            Assert.All(logger.Messages, message =>
            {
                Assert.Contains("OUTBOX_BATCH_COMPLETED", message);
                Assert.Contains("TenantId=" + a.Id, message);
                Assert.DoesNotContain("SECRET", message);
                Assert.DoesNotContain("PROVIDER", message);
                Assert.DoesNotContain("factura.emitida", message);
            });
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task UnexpectedSqlExceptionCannotExposeProviderSecrets()
    {
        var repo = new Mock<IMensajeOutboxRepository>();
        repo.Setup(x => x.ClaimDisponiblesAsync(11, It.IsAny<DateTime>(), 1,
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SECRET_CONNECTION_STRING"));
        var logger = new SafeLogger();
        var worker = new C1103OutboxIsolatedRelayWorker(
            repo.Object, new Transport((_, _, _, _, _, _) => Task.CompletedTask));
        var audited = new C1103OutboxAuditedRelay(worker, logger);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            audited.RunTenantAsync(11, DateTime.UtcNow, 1, 2, TimeSpan.FromMinutes(1)));
        Assert.Equal("OUTBOX_RELAY_INTERNAL_FAILURE", error.Message);
        Assert.Null(error.InnerException);
        Assert.Single(logger.Messages);
        Assert.Contains("OUTBOX_BATCH_FAILED", logger.Messages[0]);
        Assert.DoesNotContain("SECRET", logger.Messages[0]);
    }

    [Fact]
    public async Task BrokenAuditSinkDoesNotAffectEmptyClaim()
    {
        var repo = new Mock<IMensajeOutboxRepository>();
        repo.Setup(x => x.ClaimDisponiblesAsync(11, It.IsAny<DateTime>(), 1,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<MensajeOutbox>());
        var worker = new C1103OutboxIsolatedRelayWorker(
            repo.Object, new Transport((_, _, _, _, _, _) => Task.CompletedTask));
        var audited = new C1103OutboxAuditedRelay(
            worker, new SafeLogger { ThrowOnWrite = true });
        var result = await audited.RunTenantAsync(
            11, DateTime.UtcNow, 1, 2, TimeSpan.FromMinutes(1));
        Assert.Equal(new C1103OutboxIsolatedRelayWorker.BatchResult(0, 0, 0, 0, 0), result);
        repo.Verify(x => x.ClaimDisponiblesAsync(11, It.IsAny<DateTime>(), 1,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RepositoryArgumentExceptionCannotLeakProviderSecrets()
    {
        var repo = new Mock<IMensajeOutboxRepository>();
        repo.Setup(x => x.ClaimDisponiblesAsync(11, It.IsAny<DateTime>(), 1,
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("SECRET_PII_CONNECTION_STRING"));
        var logger = new SafeLogger();
        var worker = new C1103OutboxIsolatedRelayWorker(
            repo.Object, new Transport((_, _, _, _, _, _) => Task.CompletedTask));
        var audited = new C1103OutboxAuditedRelay(worker, logger);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            audited.RunTenantAsync(11, DateTime.UtcNow, 1, 2, TimeSpan.FromMinutes(1)));
        Assert.Equal("OUTBOX_RELAY_INTERNAL_FAILURE", error.Message);
        Assert.Null(error.InnerException);
        Assert.Single(logger.Messages);
        Assert.Contains("OUTBOX_BATCH_FAILED", logger.Messages[0]);
        Assert.DoesNotContain("SECRET", logger.Messages[0]);
        repo.Verify(x => x.ClaimDisponiblesAsync(11, It.IsAny<DateTime>(), 1,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CallerArgumentErrorsAreValidatedBeforeUntrustedRepository()
    {
        var repo = new Mock<IMensajeOutboxRepository>(MockBehavior.Strict);
        var worker = new C1103OutboxIsolatedRelayWorker(
            repo.Object, new Transport((_, _, _, _, _, _) => Task.CompletedTask));
        var logger = new SafeLogger();
        var audited = new C1103OutboxAuditedRelay(worker, logger);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            audited.RunTenantAsync(0, DateTime.UtcNow, 1, 2, TimeSpan.FromMinutes(1)));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            audited.RunTenantAsync(11, DateTime.Now, 1, 2, TimeSpan.FromMinutes(1)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            audited.RunTenantAsync(11, DateTime.UtcNow, 0, 2, TimeSpan.FromMinutes(1)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            audited.RunTenantAsync(11, DateTime.UtcNow, 1, 0, TimeSpan.FromMinutes(1)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            audited.RunTenantAsync(11, DateTime.UtcNow, 1, 2, TimeSpan.Zero));
        repo.VerifyNoOtherCalls();
        Assert.Empty(logger.Messages);
    }
}
