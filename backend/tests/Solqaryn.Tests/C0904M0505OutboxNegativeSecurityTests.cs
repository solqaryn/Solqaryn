using Moq;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// C09-04.M05.05: pruebas adversariales de Application real con autoridad
/// y repositorio simulados. Sin HTTP firmado, MySQL ni proveedores externos.
/// </summary>
public sealed class C0904M0505OutboxNegativeSecurityTests
{
    private static readonly RegistrarMensajeOutboxRequest Peticion =
        new("crm.mensaje.preparado", "{\"canal\":\"email\"}", "Campana", "10", "corr-negative");

    [Theory]
    [InlineData(8, 10, 3, "Operador")]
    [InlineData(7, 0, 3, "Operador")]
    [InlineData(7, 10, 0, "Operador")]
    [InlineData(7, 10, 3, "  ")]
    public async Task ContextoTenantOIdentidadInvalida_DeniegaAntesDeConsultarRepositorio(
        int empresa, int usuario, int rol, string nombreRol)
    {
        var repository = new Mock<IMensajeOutboxRepository>(MockBehavior.Strict);
        var service = new MensajeOutboxService(repository.Object,
            new ScopeFalso(new UsuarioTenantScopeActual(usuario, empresa, rol, nombreRol, true)));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            service.RegistrarAsync(7, Peticion, "clave-segura"));
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MembresiaRevocadaSinContexto_NuncaGuardaNiExponeOtroTenant()
    {
        var repository = new Mock<IMensajeOutboxRepository>(MockBehavior.Strict);
        var service = new MensajeOutboxService(repository.Object, new ScopeFalso(null));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            service.RegistrarAsync(7, Peticion, "clave-revocada"));
        repository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("clave\n")]
    [InlineData("clave\r")]
    [InlineData("clave\t")]
    public async Task ClaveDeIdempotenciaConControlRechazadaSinLookupNiAlta(string clave)
    {
        var repository = new Mock<IMensajeOutboxRepository>(MockBehavior.Strict);
        var service = new MensajeOutboxService(repository.Object, new ScopeFalso(
            new UsuarioTenantScopeActual(10, 7, 3, "Operador", false)));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RegistrarAsync(7, Peticion, clave));
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReplayDeClaveConPayloadDistinto_ConflictoAntesDeGuardar()
    {
        var existing = MensajeOutbox.Crear(7, Peticion.TipoEvento,
            "{\"canal\":\"whatsapp\"}", "clave-reutilizada",
            Peticion.TipoAgregado, Peticion.IdAgregado, Peticion.CorrelationId);
        var repository = new Mock<IMensajeOutboxRepository>(MockBehavior.Strict);
        repository.Setup(r => r.GetByClaveIdempotenciaAsync(7, "clave-reutilizada",
            It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        var service = new MensajeOutboxService(repository.Object, new ScopeFalso(
            new UsuarioTenantScopeActual(10, 7, 3, "Operador", false)));

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.RegistrarAsync(7, Peticion, "clave-reutilizada"));
        repository.Verify(r => r.GetByClaveIdempotenciaAsync(7, "clave-reutilizada",
            It.IsAny<CancellationToken>()), Times.Once);
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RepositorioMaliciosoRetornaMensajeAjeno_NoFiltraNiReplica()
    {
        var foreign = MensajeOutbox.Crear(8, Peticion.TipoEvento, Peticion.PayloadJson,
            "clave-ajena", Peticion.TipoAgregado, Peticion.IdAgregado, Peticion.CorrelationId);
        var repository = new Mock<IMensajeOutboxRepository>(MockBehavior.Strict);
        repository.Setup(r => r.GetByClaveIdempotenciaAsync(7, "clave-ajena",
            It.IsAny<CancellationToken>())).ReturnsAsync(foreign);
        var service = new MensajeOutboxService(repository.Object, new ScopeFalso(
            new UsuarioTenantScopeActual(10, 7, 3, "Operador", false)));

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.RegistrarAsync(7, Peticion, "clave-ajena"));
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PayloadVacioRechazadoAntesDePersistirIntencion()
    {
        var repository = new Mock<IMensajeOutboxRepository>(MockBehavior.Strict);
        var service = new MensajeOutboxService(repository.Object, new ScopeFalso(
            new UsuarioTenantScopeActual(10, 7, 3, "Operador", false)));
        var invalid = Peticion with { PayloadJson = " " };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RegistrarAsync(7, invalid, "clave-payload-vacio"));
        repository.VerifyNoOtherCalls();
    }

    private sealed class ScopeFalso : IUsuarioScopeService
    {
        private readonly UsuarioTenantScopeActual? _contexto;
        public ScopeFalso(UsuarioTenantScopeActual? contexto) => _contexto = contexto;

        public Task<UsuarioScopeActual?> ObtenerActualAsync() =>
            Task.FromResult<UsuarioScopeActual?>(null);

        public Task<UsuarioTenantScopeActual?> ObtenerActualAsync(
            int empresaId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_contexto);
    }
}
