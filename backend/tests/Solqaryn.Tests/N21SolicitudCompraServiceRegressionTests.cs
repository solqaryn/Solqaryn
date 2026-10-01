using System.Text.Json;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N21SolicitudCompraServiceRegressionTests
{
    [Fact]
    public async Task Update_invalido_no_muta_el_borrador_existente()
    {
        var solicitud = CrearBorrador();
        var detalleOriginal = solicitud.Detalles.Single();
        var repo = new RepoStub(solicitud);
        var unitOfWork = new UnitOfWorkStub();
        var auditoria = new AuditoriaSpy();
        var service = CrearServicio(repo, unitOfWork, auditoria);

        var dto = new UpdateSolicitudCompraDto
        {
            ProveedorId = 99,
            Notas = "debe fallar",
            Detalles =
            {
                new SolicitudCompraDetalleInputDto
                {
                    ProductoId = 1,
                    CantidadSolicitada = 0
                }
            }
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(17, dto));

        Assert.Equal(7, solicitud.ProveedorId);
        Assert.Equal("original", solicitud.Notas);
        Assert.Same(detalleOriginal, solicitud.Detalles.Single());
        Assert.Equal(0, repo.SaveCalls);
        Assert.Equal(0, repo.LockCalls);
        Assert.Equal(0, unitOfWork.Calls);
        Assert.Empty(auditoria.StrictCalls);
    }

    [Fact]
    public async Task Create_persiste_y_audita_estricto_dentro_de_la_misma_transaccion()
    {
        var repo = new RepoStub(null);
        var unitOfWork = new UnitOfWorkStub();
        var auditoria = new AuditoriaSpy();
        var service = CrearServicio(repo, unitOfWork, auditoria);
        var dto = new CreateSolicitudCompraDto
        {
            ProveedorId = 7,
            Notas = "texto que no debe replicarse en auditoría",
            Detalles =
            {
                new SolicitudCompraDetalleInputDto
                {
                    ProductoId = 1,
                    CantidadSolicitada = 2,
                    CostoEstimadoUnitario = 10,
                    Observacion = "observación sensible de prueba"
                }
            }
        };

        var resultado = await service.CreateAsync(dto);

        Assert.Equal(101, resultado.Id);
        Assert.Equal(1, repo.SaveCalls);
        Assert.Equal(1, unitOfWork.Calls);
        var audit = Assert.Single(auditoria.StrictCalls);
        Assert.Equal(ModuloSistema.Compras, audit.Modulo);
        Assert.Equal(AccionPermiso.Crear, audit.Accion);
        Assert.Equal(101, audit.ReferenciaId);
        Assert.Equal("SolicitudCompra", audit.Entidad);
        Assert.Null(audit.ValoresAnteriores);
        AssertSnapshotSinTextoLibre(audit.ValoresNuevos);
    }

    [Fact]
    public async Task Update_valido_audita_antes_y_despues_sin_texto_libre()
    {
        var solicitud = CrearBorrador();
        var repo = new RepoStub(solicitud);
        var unitOfWork = new UnitOfWorkStub();
        var auditoria = new AuditoriaSpy();
        var service = CrearServicio(repo, unitOfWork, auditoria);
        var dto = new UpdateSolicitudCompraDto
        {
            ProveedorId = 99,
            Notas = "nota actualizada no auditable",
            Detalles =
            {
                new SolicitudCompraDetalleInputDto
                {
                    ProductoId = 2,
                    CantidadSolicitada = 3,
                    CostoEstimadoUnitario = 12,
                    Observacion = "observación nueva no auditable"
                }
            }
        };

        await service.UpdateAsync(17, dto);

        var audit = Assert.Single(auditoria.StrictCalls);
        Assert.Equal(AccionPermiso.Editar, audit.Accion);
        Assert.Equal(17, audit.ReferenciaId);
        AssertSnapshotSinTextoLibre(audit.ValoresAnteriores);
        AssertSnapshotSinTextoLibre(audit.ValoresNuevos);
        Assert.Equal(99, solicitud.ProveedorId);
        Assert.Equal(1, unitOfWork.Calls);
    }

    [Fact]
    public async Task Enviar_exige_usuario_autenticado_y_no_muta_estado()
    {
        var solicitud = CrearBorrador();
        var repo = new RepoStub(solicitud);
        var unitOfWork = new UnitOfWorkStub();
        var auditoria = new AuditoriaSpy();
        var service = CrearServicio(repo, unitOfWork, auditoria, autenticado: false);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.EnviarAsync(17));

        Assert.Equal(EstadoSolicitudCompra.Borrador, solicitud.Estado);
        Assert.Null(solicitud.FechaSolicitudUtc);
        Assert.Equal(0, repo.SaveCalls);
        Assert.Equal(0, repo.LockCalls);
        Assert.Equal(0, unitOfWork.Calls);
        Assert.Empty(auditoria.StrictCalls);
    }

    [Fact]
    public async Task Enviar_serializa_transicion_y_registra_auditoria_estricta()
    {
        var solicitud = CrearBorrador();
        var repo = new RepoStub(solicitud);
        var unitOfWork = new UnitOfWorkStub();
        var auditoria = new AuditoriaSpy();
        var service = CrearServicio(repo, unitOfWork, auditoria);

        await service.EnviarAsync(17);

        Assert.Equal(EstadoSolicitudCompra.Solicitada, solicitud.Estado);
        Assert.Equal(1, repo.SaveCalls);
        Assert.Equal(1, repo.LockCalls);
        Assert.Equal(1, unitOfWork.Calls);
        var audit = Assert.Single(auditoria.StrictCalls);
        Assert.Equal(AccionPermiso.Confirmar, audit.Accion);
        AssertSnapshotSinTextoLibre(audit.ValoresAnteriores);
        AssertSnapshotSinTextoLibre(audit.ValoresNuevos);
    }

    [Fact]
    public async Task Aprobar_dos_veces_falla_cerrado_sin_segunda_persistencia_ni_auditoria()
    {
        var solicitud = CrearBorrador();
        solicitud.Solicitar(41, "Solicitante", DateTime.UtcNow);
        var repo = new RepoStub(solicitud);
        var unitOfWork = new UnitOfWorkStub();
        var auditoria = new AuditoriaSpy();
        var service = CrearServicio(repo, unitOfWork, auditoria);

        await service.AprobarAsync(17);
        Assert.Equal(EstadoSolicitudCompra.Aprobada, solicitud.Estado);
        Assert.Equal(1, repo.SaveCalls);
        Assert.Single(auditoria.StrictCalls);
        Assert.Equal(AccionPermiso.Aprobar, auditoria.StrictCalls[0].Accion);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AprobarAsync(17));
        Assert.Equal(1, repo.SaveCalls);
        Assert.Equal(2, repo.LockCalls);
        Assert.Equal(2, unitOfWork.Calls);
        Assert.Single(auditoria.StrictCalls);
    }

    [Fact]
    public async Task Rechazar_normaliza_motivo_y_lo_registra_sin_replicar_notas()
    {
        var solicitud = CrearBorrador();
        solicitud.Solicitar(41, "Solicitante", DateTime.UtcNow);
        var repo = new RepoStub(solicitud);
        var unitOfWork = new UnitOfWorkStub();
        var auditoria = new AuditoriaSpy();
        var service = CrearServicio(repo, unitOfWork, auditoria);

        await service.RechazarAsync(17, new RechazarSolicitudCompraDto { Motivo = "  Falta presupuesto  " });

        Assert.Equal(EstadoSolicitudCompra.Rechazada, solicitud.Estado);
        var audit = Assert.Single(auditoria.StrictCalls);
        Assert.Equal(AccionPermiso.Rechazar, audit.Accion);
        Assert.Equal("Falta presupuesto", audit.Motivo);
        AssertSnapshotSinTextoLibre(audit.ValoresAnteriores);
        AssertSnapshotSinTextoLibre(audit.ValoresNuevos);
    }

    [Fact]
    public async Task Fallo_de_auditoria_estricta_se_propaga_y_no_se_reporta_como_exito()
    {
        var solicitud = CrearBorrador();
        var repo = new RepoStub(solicitud);
        var unitOfWork = new UnitOfWorkStub();
        var auditoria = new AuditoriaSpy { ThrowOnStrict = true };
        var service = CrearServicio(repo, unitOfWork, auditoria);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.EnviarAsync(17));

        Assert.Contains("auditoría", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, unitOfWork.Calls);
        Assert.Equal(1, auditoria.StrictAttempts);
    }

    [Fact]
    public async Task Paginacion_y_rango_temporal_se_validan_antes_del_repositorio()
    {
        var repo = new RepoStub(null);
        var service = CrearServicio(repo, new UnitOfWorkStub(), new AuditoriaSpy());
        var filtro = new SolicitudCompraFiltroDto
        {
            Page = -4,
            PageSize = 999,
            Desde = new DateTime(2026, 8, 18, 12, 0, 0, DateTimeKind.Utc),
            Hasta = new DateTime(2026, 8, 17, 12, 0, 0, DateTimeKind.Utc)
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetPagedAsync(filtro));
        Assert.Equal(0, repo.PagedCalls);
    }

    private static SolicitudCompraService CrearServicio(
        RepoStub repo,
        UnitOfWorkStub unitOfWork,
        AuditoriaSpy auditoria,
        bool autenticado = true) =>
        new(repo, new CurrentUserStub(autenticado), unitOfWork, auditoria);

    private static void AssertSnapshotSinTextoLibre(object? snapshot)
    {
        Assert.NotNull(snapshot);
        var json = JsonSerializer.Serialize(snapshot);
        Assert.DoesNotContain("Notas", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Observacion", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("original", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sensible", json, StringComparison.OrdinalIgnoreCase);
    }

    private static SolicitudCompra CrearBorrador()
    {
        var detalle = new SolicitudCompraDetalle
        {
            ProductoId = 1,
            Observacion = "línea original"
        };
        detalle.EstablecerCantidad(2);
        detalle.EstablecerCostoEstimado(10);

        return new SolicitudCompra
        {
            Id = 17,
            NumeroSolicitud = "SC-000017",
            ProveedorId = 7,
            Notas = "original",
            Detalles = new List<SolicitudCompraDetalle> { detalle }
        };
    }

    private sealed class CurrentUserStub : ICurrentUserService
    {
        public CurrentUserStub(bool autenticado = true) => EstaAutenticado = autenticado;
        public int? UsuarioId => EstaAutenticado ? 42 : null;
        public string? NombreUsuario => EstaAutenticado ? "qa" : null;
        public string? NombreCompleto => EstaAutenticado ? "QA Usuario" : null;
        public int? RolId => EstaAutenticado ? 2 : null;
        public bool EsAdministrador => false;
        public bool EstaAutenticado { get; }
    }

    private sealed class UnitOfWorkStub : IUnitOfWork
    {
        public int Calls { get; private set; }

        public async Task ExecuteInTransactionAsync(Func<Task> operation)
        {
            Calls++;
            await operation();
        }
    }

    private sealed class RepoStub : ISolicitudCompraRepository
    {
        private readonly SolicitudCompra? _solicitud;
        public RepoStub(SolicitudCompra? solicitud) => _solicitud = solicitud;
        public int SaveCalls { get; private set; }
        public int PagedCalls { get; private set; }
        public int LockCalls { get; private set; }

        public Task<(IReadOnlyList<SolicitudCompra> Items, int Total)> GetPagedAsync(SolicitudCompraFiltroDto filtro)
        {
            PagedCalls++;
            IReadOnlyList<SolicitudCompra> items = _solicitud is null ? [] : [_solicitud];
            return Task.FromResult((items, items.Count));
        }

        public Task<SolicitudCompra?> GetByIdAsync(int id, bool tracking = false) =>
            Task.FromResult(_solicitud is not null && _solicitud.Id == id ? _solicitud : null);

        public Task<SolicitudCompra?> GetByIdForUpdateAsync(int id)
        {
            LockCalls++;
            return Task.FromResult(_solicitud is not null && _solicitud.Id == id ? _solicitud : null);
        }

        public Task<bool> ExisteNumeroAsync(string numero, int? excluirId = null) => Task.FromResult(false);

        public Task AddAsync(SolicitudCompra solicitud)
        {
            if (solicitud.Id == 0) solicitud.Id = 101;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync()
        {
            SaveCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed record AuditCall(
        ModuloSistema Modulo,
        AccionPermiso Accion,
        string Descripcion,
        int? ReferenciaId,
        string? Entidad,
        object? ValoresAnteriores,
        object? ValoresNuevos,
        string? Motivo);

    private sealed class AuditoriaSpy : IAuditoriaService
    {
        public List<AuditCall> StrictCalls { get; } = [];
        public bool ThrowOnStrict { get; init; }
        public int StrictAttempts { get; private set; }

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
            string? error = null) => Task.CompletedTask;

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
            string? error = null)
        {
            StrictAttempts++;
            if (ThrowOnStrict) throw new InvalidOperationException("Fallo simulado de auditoría estricta.");
            StrictCalls.Add(new AuditCall(
                modulo,
                accion,
                descripcion,
                referenciaId,
                entidad,
                valoresAnteriores,
                valoresNuevos,
                motivo));
            return Task.CompletedTask;
        }

        public Task<PagedResult<RegistroAuditoriaDto>> GetFilteredAsync(AuditoriaFiltroDto filtro) =>
            throw new NotSupportedException();
    }
}
