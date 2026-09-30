using Solqaryn.API.Controllers;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N71DOutboxApplicationApiTests
{
    private static readonly RegistrarMensajeOutboxRequest Request = new(
        "inventory.stock.changed",
        "{\"sku\":\"ABC\"}",
        "Producto",
        "42",
        "corr-1");

    [Fact]
    public async Task Application_Rechaza_IdempotencyKey_Vacia()
    {
        var repository = new FakeOutboxRepository();
        var service = new MensajeOutboxService(repository, new FakeUsuarioScopeService(7));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RegistrarAsync(7, Request, "   ", CancellationToken.None));

        Assert.Null(repository.Added);
    }

    [Fact]
    public async Task Application_Rechaza_Duplicado_Por_Tenant_Y_Clave()
    {
        var repository = new FakeOutboxRepository { Exists = true };
        var service = new MensajeOutboxService(repository, new FakeUsuarioScopeService(7));

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.RegistrarAsync(7, Request, "dup-1", CancellationToken.None));

        Assert.Null(repository.Added);
    }

    [Fact]
    public async Task Application_Falla_Cerrado_Sin_Membresia_Tenant()
    {
        var repository = new FakeOutboxRepository();
        var service = new MensajeOutboxService(repository, new FakeUsuarioScopeService(null));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            service.RegistrarAsync(7, Request, "key-1", CancellationToken.None));

        Assert.Null(repository.Added);
    }

    [Fact]
    public async Task Application_Stagea_Mensaje_Tenant_Bound_Con_Clave_Normalizada()
    {
        var repository = new FakeOutboxRepository();
        var service = new MensajeOutboxService(repository, new FakeUsuarioScopeService(7));

        var mensaje = await service.RegistrarAsync(7, Request, "  key-1  ", CancellationToken.None);

        Assert.Same(mensaje, repository.Added);
        Assert.Equal(7, mensaje.EmpresaId);
        Assert.Equal("key-1", mensaje.ClaveIdempotencia);
        Assert.NotEqual(Guid.Empty, mensaje.EventoId);
    }

    [Fact]
    public async Task Api_Mapea_Duplicado_A_Rfc7807_Sin_Filtrar_Excepcion()
    {
        await using var db = NewDb();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Empresa-Id"] = "7";
        var controller = new MensajesOutboxController(
            new ThrowingOutboxService(new ConflictException("provider-secret-duplicate")),
            db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        var result = await controller.Registrar(7, "key-1", Request, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("OUTBOX_IDEMPOTENCY_CONFLICT", problem.Extensions["code"]);
        Assert.DoesNotContain("provider-secret", problem.Detail ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("application/problem+json", objectResult.ContentTypes);
    }

    [Fact]
    public async Task Api_Rechaza_Tenant_Ruta_Distinto_Al_Contexto_Autorizado()
    {
        await using var db = NewDb();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Empresa-Id"] = "8";
        var controller = new MensajesOutboxController(
            new ThrowingOutboxService(new InvalidOperationException("no debe invocarse")),
            db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        var result = await controller.Registrar(7, "key-1", Request, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("OUTBOX_TENANT_CONTEXT_MISMATCH", problem.Extensions["code"]);
        Assert.DoesNotContain("no debe invocarse", problem.Detail ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("application/problem+json", objectResult.ContentTypes);
    }

    [Fact]
    public async Task Api_Rechaza_Header_Idempotency_Ausente_Con_Rfc7807()
    {
        await using var db = NewDb();
        var controller = new MensajesOutboxController(
            new ThrowingOutboxService(new InvalidOperationException("no debe invocarse")),
            db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.Registrar(7, null, Request, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("OUTBOX_IDEMPOTENCY_KEY_REQUIRED", problem.Extensions["code"]);
    }

    private static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n71d-outbox-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }

    private sealed class FakeOutboxRepository : IMensajeOutboxRepository
    {
        public bool Exists { get; init; }
        public MensajeOutbox? Added { get; private set; }

        public Task AddAsync(MensajeOutbox mensaje, CancellationToken cancellationToken = default)
        {
            Added = mensaje;
            return Task.CompletedTask;
        }

        public Task<MensajeOutbox?> GetByEventoIdAsync(
            int empresaId,
            Guid eventoId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<MensajeOutbox?>(null);

        public Task<bool> ExisteClaveIdempotenciaAsync(
            int empresaId,
            string claveIdempotencia,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Exists);

        public Task<IReadOnlyList<MensajeOutbox>> ClaimDisponiblesAsync(
            int empresaId,
            DateTime ahoraUtc,
            int maximoMensajes,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MensajeOutbox>>(Array.Empty<MensajeOutbox>());
    }

    private sealed class FakeUsuarioScopeService : IUsuarioScopeService
    {
        private readonly int? _empresaId;

        public FakeUsuarioScopeService(int? empresaId)
        {
            _empresaId = empresaId;
        }

        public Task<UsuarioScopeActual?> ObtenerActualAsync() => Task.FromResult<UsuarioScopeActual?>(null);

        public Task<UsuarioTenantScopeActual?> ObtenerActualAsync(
            int empresaId,
            CancellationToken cancellationToken = default)
        {
            if (_empresaId != empresaId)
                return Task.FromResult<UsuarioTenantScopeActual?>(null);

            return Task.FromResult<UsuarioTenantScopeActual?>(
                new UsuarioTenantScopeActual(1, empresaId, 1, "Administrador", true));
        }
    }

    private sealed class ThrowingOutboxService : IMensajeOutboxService
    {
        private readonly Exception _exception;

        public ThrowingOutboxService(Exception exception)
        {
            _exception = exception;
        }

        public Task<MensajeOutbox> RegistrarAsync(
            int empresaId,
            RegistrarMensajeOutboxRequest request,
            string claveIdempotencia,
            CancellationToken cancellationToken = default) =>
            Task.FromException<MensajeOutbox>(_exception);
    }
}
