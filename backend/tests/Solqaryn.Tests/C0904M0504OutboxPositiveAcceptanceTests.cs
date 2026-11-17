using Solqaryn.API.Controllers;
using Solqaryn.Application.Common;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests;

// Positive acceptance of existing API+Application staging; fake repository, no real SMTP/JWT/MySQL.
public sealed class C0904M0504OutboxPositiveAcceptanceTests
{
    private static readonly RegistrarMensajeOutboxRequest Request = new(
        "crm.mensaje.preparado", "{\"canal\":\"email\"}", "Campana", "10", "corr-acceptance");

    [Fact]
    public async Task AceptacionAplicacionControladorCreaSoloIntencionTenantBound()
    {
        var repo = new RepositorioEnMemoria();
        await using var db = NuevaDb();
        var controller = NuevoControlador(repo, db, 7);

        var result = await controller.Registrar(7, "  clave-positiva  ", Request, CancellationToken.None);

        var data = Aceptado(result);
        Assert.NotEqual(Guid.Empty, data.EventoId);
        Assert.Equal(7, data.EmpresaId);
        Assert.Equal("Pendiente", data.Estado);
        Assert.Equal(1, repo.AddCount);
        var staged = Assert.Single(repo.Mensajes);
        Assert.Equal("clave-positiva", staged.ClaveIdempotencia);
        Assert.Equal(Request.TipoEvento, staged.TipoEvento);
        Assert.Equal(Request.PayloadJson, staged.PayloadJson);
        Assert.Equal(data.EventoId, staged.EventoId);
        Assert.Null(staged.EntregadoEnUtc);
    }

    [Fact]
    public async Task ReplayMismoTenantYPayloadRecuperaEventoSinDuplicar()
    {
        var repo = new RepositorioEnMemoria();
        await using var db = NuevaDb();
        var controller = NuevoControlador(repo, db, 7);

        var primero = Aceptado(await controller.Registrar(7, "idem-replay", Request, CancellationToken.None));
        var repetido = Aceptado(await controller.Registrar(7, "idem-replay", Request, CancellationToken.None));

        Assert.Equal(primero.EventoId, repetido.EventoId);
        Assert.Equal(7, repetido.EmpresaId);
        Assert.Equal(1, repo.AddCount);
        Assert.Single(repo.Mensajes);
        Assert.True(repo.LookupCount >= 2);
    }

    [Fact]
    public async Task DosTenantsConClaveIgualMantienenEventosSeparados()
    {
        var repo = new RepositorioEnMemoria();
        await using var dbA = NuevaDb();
        await using var dbB = NuevaDb();
        var a = NuevoControlador(repo, dbA, 7);
        var b = NuevoControlador(repo, dbB, 8);

        var eventoA = Aceptado(await a.Registrar(7, "shared-key", Request, CancellationToken.None));
        var eventoB = Aceptado(await b.Registrar(8, "shared-key", Request, CancellationToken.None));

        Assert.Equal(7, eventoA.EmpresaId);
        Assert.Equal(8, eventoB.EmpresaId);
        Assert.NotEqual(eventoA.EventoId, eventoB.EventoId);
        Assert.Equal(2, repo.AddCount);
        Assert.Equal(2, repo.Mensajes.Count);
        Assert.Single(repo.Mensajes.Where(x => x.EmpresaId == 7));
        Assert.Single(repo.Mensajes.Where(x => x.EmpresaId == 8));
    }

    private static MensajeOutboxRegistroResponse Aceptado(IActionResult result)
    {
        var accepted = Assert.IsType<AcceptedResult>(result);
        var body = Assert.IsType<ApiResponse<MensajeOutboxRegistroResponse>>(accepted.Value);
        Assert.True(body.Success);
        return Assert.IsType<MensajeOutboxRegistroResponse>(body.Data);
    }

    private static MensajesOutboxController NuevoControlador(
        RepositorioEnMemoria repo, AppDbContext db, int empresa)
    {
        var service = new MensajeOutboxService(repo, new ScopeFalso(empresa));
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["X-Empresa-Id"] = empresa.ToString();
        ctx.Request.Path = $"/api/outbox/tenants/{empresa}";
        return new MensajesOutboxController(service, db)
        {
            ControllerContext = new ControllerContext { HttpContext = ctx }
        };
    }

    private static AppDbContext NuevaDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"c0904-m0504-{Guid.NewGuid():N}").Options;
        return new AppDbContext(options);
    }

    private sealed class ScopeFalso : IUsuarioScopeService
    {
        private readonly int _empresa;
        public ScopeFalso(int empresa) => _empresa = empresa;
        public Task<UsuarioScopeActual?> ObtenerActualAsync() =>
            Task.FromResult<UsuarioScopeActual?>(null);
        public Task<UsuarioTenantScopeActual?> ObtenerActualAsync(
            int empresaId, CancellationToken cancellationToken = default) =>
            Task.FromResult<UsuarioTenantScopeActual?>(
                empresaId == _empresa
                    ? new UsuarioTenantScopeActual(10, _empresa, 3, "Operador", false)
                    : null);
    }

    private sealed class RepositorioEnMemoria : IMensajeOutboxRepository
    {
        private readonly List<MensajeOutbox> _mensajes = new();
        public IReadOnlyList<MensajeOutbox> Mensajes => _mensajes;
        public int AddCount { get; private set; }
        public int LookupCount { get; private set; }

        public Task AddAsync(MensajeOutbox mensaje, CancellationToken cancellationToken = default)
        {
            _mensajes.Add(mensaje);
            AddCount++;
            return Task.CompletedTask;
        }

        public Task<MensajeOutbox?> GetByEventoIdAsync(
            int empresaId, Guid eventoId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_mensajes.FirstOrDefault(x => x.EmpresaId == empresaId && x.EventoId == eventoId));

        public Task<MensajeOutbox?> GetByClaveIdempotenciaAsync(
            int empresaId, string clave, CancellationToken cancellationToken = default)
        {
            LookupCount++;
            return Task.FromResult(_mensajes.FirstOrDefault(x =>
                x.EmpresaId == empresaId &&
                string.Equals(x.ClaveIdempotencia, clave, StringComparison.Ordinal)));
        }

        public Task<bool> ExisteClaveIdempotenciaAsync(
            int empresaId, string clave, CancellationToken cancellationToken = default) =>
            Task.FromResult(_mensajes.Any(x => x.EmpresaId == empresaId && x.ClaveIdempotencia == clave));

        public Task<IReadOnlyList<MensajeOutbox>> ClaimDisponiblesAsync(
            int empresaId, DateTime ahoraUtc, int maximoMensajes,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MensajeOutbox>>(Array.Empty<MensajeOutbox>());
    }
}
