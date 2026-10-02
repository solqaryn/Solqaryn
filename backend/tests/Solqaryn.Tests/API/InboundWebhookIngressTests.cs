using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Solqaryn.Api.Controllers;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Solqaryn.Tests.API;

public sealed class InboundWebhookIngressTests
{
    private const string Secret = "n75d-test-secret";
    private const string OtherTenantSecret = "n75g-other-tenant-secret";
    private const int MaxPayloadBytes = 256 * 1024;

    [Fact]
    public async Task Invalid_signature_is_rejected_before_persistence()
    {
        await using var db = await CreateDbAsync();
        var service = new InboundWebhookIngressService(db, Secret);
        var body = Body("evt-invalid", "inventario.actualizado");

        var result = await service.ReceiveAsync(
            1,
            "provider-a",
            body,
            "sha256=" + new string('0', 64),
            "corr-invalid",
            CancellationToken.None);

        Assert.Equal(InboundWebhookIngressKind.InvalidSignature, result.Kind);
        Assert.Empty(await db.Set<WebhookEntrante>().ToListAsync());
    }

    [Fact]
    public async Task Stale_timestamp_is_rejected_before_persistence()
    {
        await using var db = await CreateDbAsync();
        var now = new DateTime(2026, 9, 14, 2, 48, 0, DateTimeKind.Utc);
        var service = new InboundWebhookIngressService(db, Secret, () => now);
        var body = Body("evt-stale", "inventario.actualizado", now.AddMinutes(-6));

        var result = await service.ReceiveAsync(
            1,
            "provider-a",
            body,
            Signature(body),
            "corr-stale",
            CancellationToken.None);

        Assert.Equal(InboundWebhookIngressKind.InvalidRequest, result.Kind);
        Assert.Empty(await db.Set<WebhookEntrante>().ToListAsync());
    }

    [Fact]
    public async Task Valid_signature_persists_received_webhook_without_secret_or_raw_payload()
    {
        await using var db = await CreateDbAsync();
        var service = new InboundWebhookIngressService(db, Secret);
        var body = Body("evt-accepted", "inventario.actualizado");

        var result = await service.ReceiveAsync(
            1,
            "provider-a",
            body,
            Signature(body),
            "corr-accepted",
            CancellationToken.None);

        Assert.Equal(InboundWebhookIngressKind.Accepted, result.Kind);
        var stored = Assert.Single(await db.Set<WebhookEntrante>().ToListAsync());
        Assert.Equal(1, stored.EmpresaId);
        Assert.Equal("provider-a", stored.Proveedor);
        Assert.Equal("evt-accepted", stored.EventoExternoId);
        Assert.Equal(EstadoWebhookEntrante.Recibido, stored.Estado);
        Assert.Equal("corr-accepted", stored.CorrelationId);
        Assert.Equal(64, stored.PayloadHash.Length);
        Assert.DoesNotContain(Secret, stored.PayloadHash, StringComparison.Ordinal);
        Assert.DoesNotContain("inventario.actualizado", stored.PayloadHash, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Same_tenant_provider_event_and_payload_is_idempotent()
    {
        await using var db = await CreateDbAsync();
        var service = new InboundWebhookIngressService(db, Secret);
        var body = Body("evt-repeat", "inventario.actualizado");
        var signature = Signature(body);

        var first = await service.ReceiveAsync(
            1, "provider-a", body, signature, "corr-1", CancellationToken.None);
        var second = await service.ReceiveAsync(
            1, "provider-a", body, signature, "corr-2", CancellationToken.None);

        Assert.Equal(InboundWebhookIngressKind.Accepted, first.Kind);
        Assert.Equal(InboundWebhookIngressKind.Duplicate, second.Kind);
        Assert.Equal(first.WebhookId, second.WebhookId);
        Assert.Single(await db.Set<WebhookEntrante>().ToListAsync());
    }

    [Fact]
    public async Task Same_tenant_provider_event_with_different_payload_is_conflict()
    {
        await using var db = await CreateDbAsync();
        var service = new InboundWebhookIngressService(db, Secret);
        var firstBody = Body("evt-conflict", "inventario.actualizado");
        var secondBody = Body("evt-conflict", "inventario.eliminado");

        var first = await service.ReceiveAsync(
            1, "provider-a", firstBody, Signature(firstBody), "corr-1", CancellationToken.None);
        var second = await service.ReceiveAsync(
            1, "provider-a", secondBody, Signature(secondBody), "corr-2", CancellationToken.None);

        Assert.Equal(InboundWebhookIngressKind.Accepted, first.Kind);
        Assert.Equal(InboundWebhookIngressKind.Conflict, second.Kind);
        Assert.Single(await db.Set<WebhookEntrante>().ToListAsync());
    }

    [Fact]
    public async Task Tenant_route_signed_with_another_tenant_secret_is_rejected_without_persistence()
    {
        await using var db = await CreateDbAsync();
        var otherTenant = new Empresa("Tenant N7.5.G Other");
        db.Set<Empresa>().Add(otherTenant);
        await db.SaveChangesAsync();

        var body = Body("evt-tenant-mismatch", "inventario.actualizado");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"Webhooks:{otherTenant.Id}:provider-a:Secret"] = OtherTenantSecret
            })
            .Build();

        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "corr-tenant-mismatch"
        };
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        httpContext.Request.ContentLength = Encoding.UTF8.GetByteCount(body);
        httpContext.Request.Headers["X-Webhook-Signature"] = Signature(body);

        var controller = new InboundWebhooksController(
            db,
            configuration,
            new CaptureLogger<InboundWebhooksController>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        var action = await controller.ReceiveAsync(otherTenant.Id, "provider-a", CancellationToken.None);

        var rejected = Assert.IsType<ObjectResult>(action);
        Assert.Equal(StatusCodes.Status401Unauthorized, rejected.StatusCode);
        Assert.Empty(await db.Set<WebhookEntrante>().ToListAsync());
    }

    [Fact]
    public async Task Oversized_payload_is_rejected_before_persistence()
    {
        await using var db = await CreateDbAsync();
        var body = new string('x', MaxPayloadBytes + 1);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Webhooks:1:provider-a:Secret"] = Secret
            })
            .Build();

        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "corr-oversize"
        };
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        httpContext.Request.ContentLength = Encoding.UTF8.GetByteCount(body);
        httpContext.Request.Headers["X-Webhook-Signature"] = Signature(body);

        var controller = new InboundWebhooksController(
            db,
            configuration,
            new CaptureLogger<InboundWebhooksController>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        var action = await controller.ReceiveAsync(1, "provider-a", CancellationToken.None);

        var rejected = Assert.IsType<ObjectResult>(action);
        Assert.Equal(StatusCodes.Status413PayloadTooLarge, rejected.StatusCode);
        Assert.Empty(await db.Set<WebhookEntrante>().ToListAsync());
    }

    [Fact]
    public async Task Chunked_oversized_payload_is_rejected_without_reading_past_limit_probe()
    {
        await using var db = await CreateDbAsync();
        var payload = Enumerable.Repeat((byte)'x', MaxPayloadBytes + 4096).ToArray();
        await using var guardedBody = new ReadLimitStream(payload, MaxPayloadBytes + 1);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Webhooks:1:provider-a:Secret"] = Secret
            })
            .Build();

        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "corr-chunked-oversize"
        };
        httpContext.Request.Body = guardedBody;
        httpContext.Request.ContentLength = null;
        httpContext.Request.Headers["X-Webhook-Signature"] = "sha256=" + new string('0', 64);

        var controller = new InboundWebhooksController(
            db,
            configuration,
            new CaptureLogger<InboundWebhooksController>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        var action = await controller.ReceiveAsync(1, "provider-a", CancellationToken.None);

        var rejected = Assert.IsType<ObjectResult>(action);
        Assert.Equal(StatusCodes.Status413PayloadTooLarge, rejected.StatusCode);
        Assert.Equal(MaxPayloadBytes + 1, guardedBody.TotalBytesRead);
        Assert.Empty(await db.Set<WebhookEntrante>().ToListAsync());
    }

    [Fact]
    public async Task Controller_uses_trace_identifier_and_logs_without_sensitive_material()
    {
        await using var db = await CreateDbAsync();
        var body = Body("evt-observable", "inventario.actualizado");
        var signature = Signature(body);
        var logger = new CaptureLogger<InboundWebhooksController>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Webhooks:1:provider-a:Secret"] = Secret
            })
            .Build();

        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "corr-safe-123"
        };
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        httpContext.Request.Headers["X-Webhook-Signature"] = signature;
        httpContext.Request.Headers["X-Correlation-Id"] = new string('!', 200);

        var controller = new InboundWebhooksController(db, configuration, logger)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        var action = await controller.ReceiveAsync(1, "provider-a", CancellationToken.None);

        Assert.IsType<AcceptedResult>(action);
        var stored = Assert.Single(await db.Set<WebhookEntrante>().ToListAsync());
        Assert.Equal("corr-safe-123", stored.CorrelationId);

        var log = Assert.Single(logger.Messages);
        Assert.Contains("Accepted", log, StringComparison.Ordinal);
        Assert.Contains("corr-safe-123", log, StringComparison.Ordinal);
        Assert.DoesNotContain(Secret, log, StringComparison.Ordinal);
        Assert.DoesNotContain(signature, log, StringComparison.Ordinal);
        Assert.DoesNotContain(body, log, StringComparison.Ordinal);
    }

    private static async Task<AppDbContext> CreateDbAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var db = new AppDbContext(options);
        db.Set<Empresa>().Add(new Empresa("Tenant N7.5.D"));
        await db.SaveChangesAsync();
        return db;
    }

    private static string Body(string eventId, string eventType, DateTime? emittedAtUtc = null) =>
        JsonSerializer.Serialize(new
        {
            eventoExternoId = eventId,
            tipoEvento = eventType,
            emitidoEnUtc = emittedAtUtc ?? DateTime.UtcNow,
            data = new { sku = "ABC-1", quantity = 2 }
        });

    private static string Signature(string body)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Secret));
        return "sha256=" + Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body)));
    }

    private sealed class ReadLimitStream : Stream
    {
        private readonly MemoryStream _inner;
        private readonly int _maxReadableBytes;

        public ReadLimitStream(byte[] content, int maxReadableBytes)
        {
            _inner = new MemoryStream(content, writable: false);
            _maxReadableBytes = maxReadableBytes;
        }

        public int TotalBytesRead { get; private set; }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _inner.Read(buffer, offset, count);
            RegisterRead(read);
            return read;
        }

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            var read = await _inner.ReadAsync(buffer, cancellationToken);
            RegisterRead(read);
            return read;
        }

        public override async Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            var read = await _inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
            RegisterRead(read);
            return read;
        }

        private void RegisterRead(int bytesRead)
        {
            TotalBytesRead += bytesRead;
            if (TotalBytesRead > _maxReadableBytes)
            {
                throw new InvalidOperationException(
                    $"Request body read exceeded the allowed probe of {_maxReadableBytes} bytes.");
            }
        }

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _inner.Dispose();
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await _inner.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }

    private sealed class CaptureLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NoopScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NoopScope : IDisposable
        {
            public static NoopScope Instance { get; } = new();
            public void Dispose() { }
        }
    }
}
