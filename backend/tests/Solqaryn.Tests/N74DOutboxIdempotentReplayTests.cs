using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N74DOutboxIdempotentReplayTests
{
    private static readonly RegistrarMensajeOutboxRequest Request = new(
        "inventario.stock.cambiado",
        "{\"sku\":\"ABC\"}",
        "Producto",
        "42",
        "corr-74d");

    [Fact]
    public async Task Misma_Clave_Y_Misma_Solicitud_Reusa_Respuesta_Sin_Segundo_Efecto()
    {
        var existente = MensajeOutbox.Crear(
            7,
            Request.TipoEvento,
            Request.PayloadJson,
            "idem-74d",
            Request.TipoAgregado,
            Request.IdAgregado,
            Request.CorrelationId,
            new DateTime(2026, 9, 13, 20, 0, 0, DateTimeKind.Utc));
        var repository = new ReplayRepository(existente);
        var service = new MensajeOutboxService(repository, new FakeUsuarioScopeService(7));

        var replay = await service.RegistrarAsync(7, Request, "  idem-74d  ", CancellationToken.None);

        Assert.Same(existente, replay);
        Assert.Equal(existente.EventoId, replay.EventoId);
        Assert.Equal(existente.Estado, replay.Estado);
        Assert.Equal(0, repository.AddCalls);
    }

    [Fact]
    public async Task Misma_Clave_Con_Solicitud_Diferente_Falla_Cerrado_Sin_Segundo_Efecto()
    {
        var existente = MensajeOutbox.Crear(
            7,
            Request.TipoEvento,
            Request.PayloadJson,
            "idem-74d",
            Request.TipoAgregado,
            Request.IdAgregado,
            Request.CorrelationId);
        var repository = new ReplayRepository(existente);
        var service = new MensajeOutboxService(repository, new FakeUsuarioScopeService(7));
        var diferente = Request with { PayloadJson = "{\"sku\":\"XYZ\"}" };

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.RegistrarAsync(7, diferente, "idem-74d", CancellationToken.None));

        Assert.Equal(0, repository.AddCalls);
    }

    [Fact]
    public async Task Clave_Nueva_Stagea_Una_Sola_Intencion_Normalizada()
    {
        var repository = new ReplayRepository(null);
        var service = new MensajeOutboxService(repository, new FakeUsuarioScopeService(7));

        var creado = await service.RegistrarAsync(7, Request, "  new-74d  ", CancellationToken.None);

        Assert.Equal(1, repository.AddCalls);
        Assert.Same(creado, repository.Added);
        Assert.Equal("new-74d", creado.ClaveIdempotencia);
    }

    [Theory]
    [InlineData("  clave-valida\r\n")]
    [InlineData("\tclave-valida")]
    [InlineData("clave-valida\n")]
    [InlineData("clave-valida\0sufijo")]
    [InlineData("clave-valida\u001f")]
    public async Task C09M0302_RechazaControlesOriginalesSinStagear(string clave)
    {
        var repository = new ReplayRepository(null);
        var service = new MensajeOutboxService(repository, new FakeUsuarioScopeService(7));

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RegistrarAsync(7, Request, clave, CancellationToken.None));

        Assert.Equal("claveIdempotencia", ex.ParamName);
        Assert.Equal(0, repository.AddCalls);
    }

    [Fact]
    public async Task C09M0302_ClaveUnicodeYJsonMultilinea_ConservanReplaySinDuplicacion()
    {
        var repository = new ReplayRepository(null);
        var service = new MensajeOutboxService(repository, new FakeUsuarioScopeService(7));
        var input = Request with { PayloadJson = "{\n  \"sku\": \"ABC\"\n}" };

        var creado = await service.RegistrarAsync(7, input, "  correo-ñ-2026  ");
        Assert.Equal("correo-ñ-2026", creado.ClaveIdempotencia);
        Assert.Equal(input.PayloadJson, creado.PayloadJson);
        Assert.Equal(1, repository.AddCalls);

        // La regresión de replay ya existente verifica la respuesta sin segundo efecto.
    }

    [Fact]
    public async Task C09M0302_Clave200AceptadaY201Rechazada()
    {
        var repository = new ReplayRepository(null);
        var service = new MensajeOutboxService(repository, new FakeUsuarioScopeService(7));
        var limite = new string('x', MensajeOutbox.LongitudMaximaClaveIdempotencia);

        var creado = await service.RegistrarAsync(7, Request, limite);
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RegistrarAsync(7, Request, limite + "x"));

        Assert.Equal(limite, creado.ClaveIdempotencia);
        Assert.Equal("claveIdempotencia", ex.ParamName);
        Assert.Equal(1, repository.AddCalls);
    }

    [Fact]
    public async Task C09M0302_EmpresaAjenaFallaCerradoSinNuevoEfecto()
    {
        var repository = new ReplayRepository(null);
        var service = new MensajeOutboxService(repository, new FakeUsuarioScopeService(7));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            service.RegistrarAsync(8, Request, "clave-empresa-b"));

        Assert.Equal(0, repository.AddCalls);
    }

    [Fact]
    public async Task C09M0302_CorrelacionConControlesNoSeStagea()
    {
        var repository = new ReplayRepository(null);
        var service = new MensajeOutboxService(repository, new FakeUsuarioScopeService(7));
        var invalida = Request with { CorrelationId = "correlacion\r\n" };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RegistrarAsync(7, invalida, "clave-ok"));

        Assert.Equal("correlationId", ex.ParamName);
        Assert.Equal(0, repository.AddCalls);
    }

    private sealed class ReplayRepository : IMensajeOutboxRepository
    {
        private readonly MensajeOutbox? _existing;

        public ReplayRepository(MensajeOutbox? existing)
        {
            _existing = existing;
        }

        public int AddCalls { get; private set; }
        public MensajeOutbox? Added { get; private set; }

        public Task AddAsync(MensajeOutbox mensaje, CancellationToken cancellationToken = default)
        {
            AddCalls++;
            Added = mensaje;
            return Task.CompletedTask;
        }

        public Task<MensajeOutbox?> GetByEventoIdAsync(
            int empresaId,
            Guid eventoId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_existing is not null && _existing.EmpresaId == empresaId && _existing.EventoId == eventoId
                ? _existing
                : null);

        public Task<MensajeOutbox?> GetByClaveIdempotenciaAsync(
            int empresaId,
            string claveIdempotencia,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_existing is not null &&
                            _existing.EmpresaId == empresaId &&
                            string.Equals(_existing.ClaveIdempotencia, claveIdempotencia.Trim(), StringComparison.Ordinal)
                ? _existing
                : null);

        public Task<bool> ExisteClaveIdempotenciaAsync(
            int empresaId,
            string claveIdempotencia,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_existing is not null &&
                            _existing.EmpresaId == empresaId &&
                            string.Equals(_existing.ClaveIdempotencia, claveIdempotencia.Trim(), StringComparison.Ordinal));

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
}
