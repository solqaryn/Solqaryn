using System.Text.Json;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N22OrdenCompraSecurityAuditRegressionTests
{
    [Fact]
    public async Task Enviar_a_aprobacion_audita_estricto_sin_texto_libre()
    {
        var orden = CrearBorrador();
        var auditoria = new AuditoriaSpy();
        var fixture = CrearFixture(orden, auditoria);

        await fixture.Service.EnviarAprobacionAsync(44);

        Assert.Equal(EstadoOrdenCompra.PendienteAprobacion, orden.Estado);
        fixture.Repository.Verify(x => x.SaveChangesAsync(), Times.Once);
        Assert.Equal(1, fixture.UnitOfWork.Calls);
        var audit = Assert.Single(auditoria.StrictCalls);
        Assert.Equal(ModuloSistema.Compras, audit.Modulo);
        Assert.Equal(AccionPermiso.Confirmar, audit.Accion);
        Assert.Equal(44, audit.ReferenciaId);
        Assert.Equal("OrdenCompra", audit.Entidad);
        AssertSnapshotSeguro(audit.ValoresAnteriores);
        AssertSnapshotSeguro(audit.ValoresNuevos);
    }

    [Fact]
    public async Task Aprobar_audita_estricto_la_transicion_pendiente_a_aprobada()
    {
        var orden = CrearBorrador();
        orden.EnviarAprobacion(7, DateTime.UtcNow.AddMinutes(-1));
        var auditoria = new AuditoriaSpy();
        var fixture = CrearFixture(orden, auditoria);

        await fixture.Service.AprobarAsync(44);

        Assert.Equal(EstadoOrdenCompra.Aprobada, orden.Estado);
        var audit = Assert.Single(auditoria.StrictCalls);
        Assert.Equal(AccionPermiso.Aprobar, audit.Accion);
        Assert.Equal("OrdenCompra", audit.Entidad);
        AssertSnapshotSeguro(audit.ValoresAnteriores);
        AssertSnapshotSeguro(audit.ValoresNuevos);
    }

    [Fact]
    public async Task Cancelar_normaliza_motivo_y_audita_estricto()
    {
        var orden = CrearBorrador();
        var auditoria = new AuditoriaSpy();
        var fixture = CrearFixture(orden, auditoria);

        await fixture.Service.CancelarAsync(44, new CancelarOrdenCompraDto { Motivo = "  Decisión de compra  " });

        Assert.Equal(EstadoOrdenCompra.Cancelada, orden.Estado);
        Assert.Equal("Decisión de compra", orden.MotivoCancelacion);
        var audit = Assert.Single(auditoria.StrictCalls);
        Assert.Equal(AccionPermiso.Anular, audit.Accion);
        Assert.Equal("Decisión de compra", audit.Motivo);
        AssertSnapshotSeguro(audit.ValoresAnteriores);
        AssertSnapshotSeguro(audit.ValoresNuevos);
    }

    [Fact]
    public async Task Fallo_de_auditoria_estricta_se_propaga_y_no_se_oculta()
    {
        var orden = CrearBorrador();
        var auditoria = new AuditoriaSpy { ThrowOnStrict = true };
        var fixture = CrearFixture(orden, auditoria);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.EnviarAprobacionAsync(44));

        Assert.Contains("auditoría", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, fixture.UnitOfWork.Calls);
        Assert.Equal(1, auditoria.StrictAttempts);
    }

    [Fact]
    public async Task Usuario_no_autenticado_falla_cerrado_sin_save_ni_auditoria()
    {
        var orden = CrearBorrador();
        var auditoria = new AuditoriaSpy();
        var fixture = CrearFixture(orden, auditoria, autenticado: false);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => fixture.Service.EnviarAprobacionAsync(44));

        Assert.Equal(EstadoOrdenCompra.Borrador, orden.Estado);
        fixture.Repository.Verify(x => x.SaveChangesAsync(), Times.Never);
        Assert.Equal(1, fixture.UnitOfWork.Calls);
        Assert.Empty(auditoria.StrictCalls);
        Assert.Equal(0, auditoria.StrictAttempts);
    }

    private static void AssertSnapshotSeguro(object? snapshot)
    {
        Assert.NotNull(snapshot);
        var json = JsonSerializer.Serialize(snapshot);
        Assert.Contains("Estado", json, StringComparison.Ordinal);
        Assert.Contains("ProveedorId", json, StringComparison.Ordinal);
        Assert.Contains("Total", json, StringComparison.Ordinal);
        Assert.DoesNotContain("SECRETO-ORDEN", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SECRETO-LINEA", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Observaciones", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Observacion", json, StringComparison.OrdinalIgnoreCase);
    }

    private static OrdenCompra CrearBorrador()
    {
        var detalle = new OrdenCompraDetalle
        {
            ProductoId = 10,
            ProductoVarianteId = 11,
            ProductoNombreSnapshot = "Producto",
            Observacion = "SECRETO-LINEA"
        };
        detalle.EstablecerValores(2m, 100m, 5m, 28.50m);

        return new OrdenCompra
        {
            Id = 44,
            NumeroOrden = "OC-20260818-SECURITY",
            ProveedorId = 3,
            ProveedorNombreSnapshot = "Proveedor",
            Moneda = "HNL",
            Observaciones = "SECRETO-ORDEN",
            Detalles = new List<OrdenCompraDetalle> { detalle }
        };
    }

    private static Fixture CrearFixture(OrdenCompra orden, AuditoriaSpy auditoria, bool autenticado = true)
    {
        var repository = new Mock<IOrdenCompraRepository>(MockBehavior.Strict);
        repository.Setup(x => x.GetByIdForUpdateAsync(44)).ReturnsAsync(orden);
        repository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var proveedores = new Mock<IProveedorRepository>(MockBehavior.Strict);
        var productos = new Mock<IProductoRepository>(MockBehavior.Strict);
        var solicitudes = new Mock<ISolicitudCompraRepository>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Loose);
        currentUser.SetupGet(x => x.EstaAutenticado).Returns(autenticado);
        currentUser.SetupGet(x => x.UsuarioId).Returns(autenticado ? 42 : null);
        currentUser.SetupGet(x => x.NombreUsuario).Returns(autenticado ? "qa" : null);
        currentUser.SetupGet(x => x.NombreCompleto).Returns(autenticado ? "QA Usuario" : null);

        var unitOfWork = new UnitOfWorkStub();
        var service = new OrdenCompraService(
            repository.Object,
            proveedores.Object,
            productos.Object,
            solicitudes.Object,
            currentUser.Object,
            unitOfWork,
            auditoria);

        return new Fixture(service, repository, unitOfWork);
    }

    private sealed record Fixture(
        OrdenCompraService Service,
        Mock<IOrdenCompraRepository> Repository,
        UnitOfWorkStub UnitOfWork);

    private sealed class UnitOfWorkStub : IUnitOfWork
    {
        public int Calls { get; private set; }

        public async Task ExecuteInTransactionAsync(Func<Task> operation)
        {
            Calls++;
            await operation();
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
            if (ThrowOnStrict)
                throw new InvalidOperationException("Fallo simulado de auditoría estricta.");

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
