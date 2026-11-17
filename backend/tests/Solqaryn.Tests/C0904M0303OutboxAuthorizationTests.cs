using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public sealed class C0904M0303OutboxAuthorizationTests
{
    private static readonly RegistrarMensajeOutboxRequest Request = new(
        "crm.mensaje.preparado",
        "{\"canal\":\"email\"}",
        "Campana",
        "10",
        "corr-tenant");

    [Fact]
    public async Task SinMembresiaDeniegaAntesDeConsultarOEscribirOutbox()
    {
        var repo = new ContadorRepositorio();
        var service = new MensajeOutboxService(repo, new ScopeStub(null));

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => service.RegistrarAsync(7, Request, "idem-tenant"));

        Assert.Equal(0, repo.ReadCount);
        Assert.Equal(0, repo.AddCount);
    }

    [Theory]
    [InlineData(0, 7, 1, "Administrador")]
    [InlineData(10, 8, 1, "Administrador")]
    [InlineData(10, 7, 0, "Administrador")]
    [InlineData(10, 7, 1, " ")]
    public async Task ContextoInvalidoONoPertenecienteAlTenantFallaCerrado(
        int usuarioId, int empresaScope, int rolId, string nombreRol)
    {
        var repo = new ContadorRepositorio();
        var scope = new UsuarioTenantScopeActual(usuarioId, empresaScope, rolId, nombreRol, true);
        var service = new MensajeOutboxService(repo, new ScopeStub(scope));

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => service.RegistrarAsync(7, Request, "idem-tenant"));

        Assert.Equal(0, repo.ReadCount);
        Assert.Equal(0, repo.AddCount);
    }

    [Fact]
    public async Task AutoridadNoDisponibleNoProduceLecturasNiEfectos()
    {
        var repo = new ContadorRepositorio();
        var service = new MensajeOutboxService(repo, new ScopeStub(null, falla: true));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RegistrarAsync(7, Request, "idem-tenant"));

        Assert.Equal(0, repo.ReadCount);
        Assert.Equal(0, repo.AddCount);
    }

    [Fact]
    public async Task MembresiaTenantValidaNoAdminPermiteRegistrarSoloSuTenant()
    {
        var repo = new ContadorRepositorio();
        var scope = new UsuarioTenantScopeActual(10, 7, 3, "Operador", false);
        var service = new MensajeOutboxService(repo, new ScopeStub(scope));

        var mensaje = await service.RegistrarAsync(7, Request, "  idem-tenant  ");

        Assert.Equal(7, mensaje.EmpresaId);
        Assert.Equal("idem-tenant", mensaje.ClaveIdempotencia);
        Assert.Equal(2, repo.ReadCount);
        Assert.Equal(1, repo.AddCount);
    }

    private sealed class ScopeStub : IUsuarioScopeService
    {
        private readonly UsuarioTenantScopeActual? _scope;
        private readonly bool _falla;

        public ScopeStub(UsuarioTenantScopeActual? scope, bool falla = false)
        {
            _scope = scope;
            _falla = falla;
        }

        public Task<UsuarioScopeActual?> ObtenerActualAsync() =>
            Task.FromResult<UsuarioScopeActual?>(null);

        public Task<UsuarioTenantScopeActual?> ObtenerActualAsync(
            int empresaId, CancellationToken cancellationToken = default) =>
            _falla
                ? Task.FromException<UsuarioTenantScopeActual?>(
                    new InvalidOperationException("Origen de membresías no disponible."))
                : Task.FromResult(_scope);
    }

    private sealed class ContadorRepositorio : IMensajeOutboxRepository
    {
        public int ReadCount { get; private set; }
        public int AddCount { get; private set; }

        public Task AddAsync(MensajeOutbox mensaje, CancellationToken cancellationToken = default)
        {
            AddCount++;
            return Task.CompletedTask;
        }

        public Task<MensajeOutbox?> GetByEventoIdAsync(
            int empresaId, Guid eventoId, CancellationToken cancellationToken = default) =>
            Task.FromResult<MensajeOutbox?>(null);

        public Task<MensajeOutbox?> GetByClaveIdempotenciaAsync(
            int empresaId, string claveIdempotencia, CancellationToken cancellationToken = default)
        {
            ReadCount++;
            return Task.FromResult<MensajeOutbox?>(null);
        }

        public Task<bool> ExisteClaveIdempotenciaAsync(
            int empresaId, string claveIdempotencia, CancellationToken cancellationToken = default)
        {
            ReadCount++;
            return Task.FromResult(false);
        }

        public Task<IReadOnlyList<MensajeOutbox>> ClaimDisponiblesAsync(
            int empresaId, DateTime ahoraUtc, int maximoMensajes,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MensajeOutbox>>(Array.Empty<MensajeOutbox>());
    }
}
