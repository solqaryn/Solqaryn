using System.Text.Json;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public sealed class EmailEmpresarialServiceTests
{
    [Fact]
    public async Task RegistrarAsync_ReplayMismoTenantYMismaSolicitud_NoDuplica()
    {
        var repo = new FakeEmailRepository();
        var service = new EmailEmpresarialService(repo, new FakeScope(7), new FakeAuditoria());
        var request = new RegistrarEmailEmpresarialRequest(
            "cliente@example.com",
            "Factura disponible",
            "<p>Lista</p>",
            "Lista",
            CorrelationId: "corr-77");

        var primero = await service.RegistrarAsync(7, request, "idem-001");
        var replay = await service.RegistrarAsync(7, request, "idem-001");

        Assert.Equal(primero.MensajeId, replay.MensajeId);
        Assert.Single(repo.Items);
        Assert.Equal(1, repo.SaveCount);
    }

    [Fact]
    public async Task RegistrarAsync_MismaClavePayloadDistinto_FallaCerrado()
    {
        var repo = new FakeEmailRepository();
        var service = new EmailEmpresarialService(repo, new FakeScope(7), new FakeAuditoria());

        await service.RegistrarAsync(
            7,
            new RegistrarEmailEmpresarialRequest(
                "cliente@example.com",
                "Factura A",
                "<p>A</p>"),
            "idem-002");

        await Assert.ThrowsAsync<ConflictException>(() => service.RegistrarAsync(
            7,
            new RegistrarEmailEmpresarialRequest(
                "cliente@example.com",
                "Factura B",
                "<p>B</p>"),
            "idem-002"));

        Assert.Single(repo.Items);
    }

    [Fact]
    public async Task RegistrarAsync_ScopeDeOtroTenant_FallaAntesDePersistir()
    {
        var repo = new FakeEmailRepository();
        var audit = new FakeAuditoria();
        var service = new EmailEmpresarialService(repo, new FakeScope(8), audit);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.RegistrarAsync(
            7,
            new RegistrarEmailEmpresarialRequest(
                "cliente@example.com",
                "Factura",
                "<p>Contenido</p>"),
            "idem-003"));

        Assert.Empty(repo.Items);
        Assert.Equal(0, repo.SaveCount);
        Assert.Empty(audit.Calls);
    }

    [Fact]
    public async Task RegistrarAsync_AuditaMetadatosSinDestinatarioCuerpoNiClaveIdempotencia()
    {
        var repo = new FakeEmailRepository();
        var audit = new FakeAuditoria();
        var service = new EmailEmpresarialService(repo, new FakeScope(7), audit);

        await service.RegistrarAsync(
            7,
            new RegistrarEmailEmpresarialRequest(
                "secreto-destino@example.com",
                "Asunto sensible",
                "<p>cuerpo-secreto</p>",
                "texto-secreto",
                CorrelationId: "corr-segura"),
            "idem-secreta-004");

        var call = Assert.Single(audit.Calls);
        Assert.Equal(ModuloSistema.Configuracion, call.Modulo);
        Assert.Equal(AccionPermiso.Crear, call.Accion);
        Assert.Equal("EmailEmpresarial", call.Entidad);
        Assert.Contains("corr-segura", call.ValoresNuevosJson);
        Assert.DoesNotContain("secreto-destino@example.com", call.ValoresNuevosJson);
        Assert.DoesNotContain("cuerpo-secreto", call.ValoresNuevosJson);
        Assert.DoesNotContain("texto-secreto", call.ValoresNuevosJson);
        Assert.DoesNotContain("idem-secreta-004", call.ValoresNuevosJson);
    }

    [Fact]
    public async Task ListarAsync_AplicaTenantEstadoCorrelacionYPaginacion()
    {
        var repo = new FakeEmailRepository();
        repo.Items.Add(EmailEmpresarial.CrearDirecto(
            7, "a@example.com", "A", "<p>A</p>", "a", correlationId: "corr"));
        repo.Items.Add(EmailEmpresarial.CrearDirecto(
            8, "b@example.com", "B", "<p>B</p>", "b", correlationId: "corr"));
        repo.Items.Add(EmailEmpresarial.CrearDirecto(
            7, "c@example.com", "C", "<p>C</p>", "c", correlationId: "otra"));

        var service = new EmailEmpresarialService(repo, new FakeScope(7), new FakeAuditoria());
        var pagina = await service.ListarAsync(
            7,
            EstadoEntregaEmail.Pendiente,
            "corr",
            1,
            25);

        Assert.Single(pagina.Items);
        Assert.Equal(7, pagina.Items[0].EmpresaId);
        Assert.Equal("corr", pagina.Items[0].CorrelationId);
        Assert.Equal(1, pagina.Total);
    }

    private sealed class FakeScope : IUsuarioScopeService
    {
        private readonly int _empresaId;

        public FakeScope(int empresaId)
        {
            _empresaId = empresaId;
        }

        public Task<UsuarioScopeActual?> ObtenerActualAsync() =>
            Task.FromResult<UsuarioScopeActual?>(new UsuarioScopeActual(1, 1, "Test", false));

        public Task<UsuarioTenantScopeActual?> ObtenerActualAsync(
            int empresaId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<UsuarioTenantScopeActual?>(
                new UsuarioTenantScopeActual(1, _empresaId, 1, "Test", false));
    }

    private sealed class FakeAuditoria : IAuditoriaService
    {
        public List<AuditCall> Calls { get; } = new();

        public Task RegistrarAsync(
            ModuloSistema modulo,
            AccionPermiso accion,
            string descripcion,
            int? referenciaId = null,
            string? entidad = null,
            object? valoresAnteriores = null,
            object? valoresNuevos = null,
            string? motivo = null,
            string resultado = "Exito",
            string? error = null)
        {
            Calls.Add(new AuditCall(
                modulo,
                accion,
                entidad,
                valoresNuevos is null ? string.Empty : JsonSerializer.Serialize(valoresNuevos)));
            return Task.CompletedTask;
        }

        public Task RegistrarEstrictoAsync(
            ModuloSistema modulo,
            AccionPermiso accion,
            string descripcion,
            int? referenciaId = null,
            string? entidad = null,
            object? valoresAnteriores = null,
            object? valoresNuevos = null,
            string? motivo = null,
            string resultado = "Exito",
            string? error = null) =>
            RegistrarAsync(modulo, accion, descripcion, referenciaId, entidad, valoresAnteriores, valoresNuevos, motivo, resultado, error);

        public Task<PagedResult<RegistroAuditoriaDto>> GetFilteredAsync(AuditoriaFiltroDto filtro) =>
            Task.FromResult(new PagedResult<RegistroAuditoriaDto>
            {
                Page = filtro.Page,
                PageSize = filtro.PageSize
            });
    }

    private sealed record AuditCall(
        ModuloSistema Modulo,
        AccionPermiso Accion,
        string? Entidad,
        string ValoresNuevosJson);

    private sealed class FakeEmailRepository : IEmailEmpresarialRepository
    {
        public List<EmailEmpresarial> Items { get; } = new();
        public int SaveCount { get; private set; }

        public Task<EmailEmpresarial?> GetByClaveIdempotenciaAsync(
            int empresaId,
            string claveIdempotencia,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.SingleOrDefault(x =>
                x.EmpresaId == empresaId && x.ClaveIdempotencia == claveIdempotencia));

        public Task<EmailEmpresarial?> GetByMensajeIdAsync(
            int empresaId,
            Guid mensajeId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.SingleOrDefault(x =>
                x.EmpresaId == empresaId && x.MensajeId == mensajeId));

        public Task<IReadOnlyList<EmailEmpresarial>> ListarAsync(
            int empresaId,
            EstadoEntregaEmail? estado,
            string? correlationId,
            int pagina,
            int tamano,
            CancellationToken cancellationToken = default)
        {
            var query = AplicarFiltro(empresaId, estado, correlationId);
            IReadOnlyList<EmailEmpresarial> result = query
                .Skip((pagina - 1) * tamano)
                .Take(tamano)
                .ToArray();
            return Task.FromResult(result);
        }

        public Task<int> ContarAsync(
            int empresaId,
            EstadoEntregaEmail? estado,
            string? correlationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(AplicarFiltro(empresaId, estado, correlationId).Count());

        public Task AddAsync(EmailEmpresarial email, CancellationToken cancellationToken = default)
        {
            Items.Add(email);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.CompletedTask;
        }

        private IEnumerable<EmailEmpresarial> AplicarFiltro(
            int empresaId,
            EstadoEntregaEmail? estado,
            string? correlationId)
        {
            var query = Items.Where(x => x.EmpresaId == empresaId);
            if (estado is not null)
                query = query.Where(x => x.Estado == estado.Value);
            if (!string.IsNullOrWhiteSpace(correlationId))
                query = query.Where(x => x.CorrelationId == correlationId);
            return query;
        }
    }
}
