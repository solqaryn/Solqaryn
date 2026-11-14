using System.Reflection;
using System.Text.Json;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N23RecepcionCompraSecurityAuditObservabilityTests
{
    [Theory]
    [InlineData(AccionPermiso.Ver)]
    [InlineData(AccionPermiso.Crear)]
    [InlineData(AccionPermiso.Editar)]
    [InlineData(AccionPermiso.Confirmar)]
    [InlineData(AccionPermiso.Anular)]
    public async Task Administrador_sin_grant_explicito_no_tiene_bypass_en_recepciones(AccionPermiso accion)
    {
        var rolPermisos = new Mock<IRolPermisoRepository>(MockBehavior.Strict);
        rolPermisos
            .Setup(x => x.TienePermisoPorRolIdAsync(99, ModuloSistema.Compras, accion))
            .ReturnsAsync(false);

        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        scope.Setup(x => x.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(7, 99, "Administrador", EsAdministrador: true));

        var service = new PermisoService(
            rolPermisos.Object,
            Mock.Of<IRolRepository>(),
            Mock.Of<IPermisoRepository>(),
            Mock.Of<IAuditoriaService>(),
            Mock.Of<ICurrentUserService>(),
            scope.Object);

        Assert.False(await service.TienePermisoAsync(ModuloSistema.Compras, accion));
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => service.VerificarPermisoAsync(ModuloSistema.Compras, accion));

        rolPermisos.Verify(
            x => x.TienePermisoPorRolIdAsync(99, ModuloSistema.Compras, accion),
            Times.Exactly(2));
    }

    [Theory]
    [InlineData("confirmar")]
    [InlineData("anular")]
    public async Task Mutacion_critica_sin_usuario_autenticado_falla_antes_de_transaccion(string operacion)
    {
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Loose);
        currentUser.SetupGet(x => x.EstaAutenticado).Returns(false);
        currentUser.SetupGet(x => x.UsuarioId).Returns((int?)null);

        var unitOfWork = new CountingUnitOfWork();
        var service = new RecepcionCompraService(
            Mock.Of<IRecepcionCompraRepository>(),
            Mock.Of<IOrdenCompraRepository>(),
            Mock.Of<IAlmacenRepository>(),
            Mock.Of<IUbicacionAlmacenRepository>(),
            Mock.Of<IMovimientoInventarioRepository>(),
            new RecepcionCompraExistenciaMaterializador(Mock.Of<IExistenciaVarianteConcurrencyService>()),
            new RecepcionCompraKardexRegistrar(Mock.Of<IKardexMovimientoWriter>(), currentUser.Object),
            currentUser.Object,
            unitOfWork,
            Mock.Of<IAuditoriaService>());

        if (operacion == "confirmar")
            await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.ConfirmarAsync(44));
        else
            await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.AnularAsync(44, new() { Motivo = "QA" }));

        Assert.Equal(0, unitOfWork.Calls);
    }

    [Fact]
    public async Task Anular_con_movimientos_posteriores_falla_antes_de_revertir_y_persistir()
    {
        var detalle = new RecepcionCompraDetalle
        {
            OrdenCompraDetalleId = 101,
            ProductoId = 20,
            ProductoVarianteId = 30,
            AlmacenId = 40,
            CostoUnitarioSnapshot = 12.50m
        };
        detalle.EstablecerCantidades(5m);

        var recepcion = new RecepcionCompra
        {
            Id = 44,
            NumeroRecepcion = "RC-GUARD-44",
            OrdenCompraId = 10,
            Detalles = new List<RecepcionCompraDetalle> { detalle }
        };
        recepcion.Confirmar(7, "qa", DateTime.UtcNow.AddMinutes(-5));

        var repository = new Mock<IRecepcionCompraRepository>(MockBehavior.Strict);
        repository.Setup(x => x.GetByIdForUpdateAsync(44)).ReturnsAsync(recepcion);

        var movimientos = new Mock<IMovimientoInventarioRepository>(MockBehavior.Strict);
        movimientos.Setup(x => x.ExisteMovimientoPosteriorRecepcionAsync(44)).ReturnsAsync(true);

        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Loose);
        currentUser.SetupGet(x => x.EstaAutenticado).Returns(true);
        currentUser.SetupGet(x => x.UsuarioId).Returns(7);
        currentUser.SetupGet(x => x.NombreUsuario).Returns("qa");

        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        var unitOfWork = new CountingUnitOfWork();
        var service = new RecepcionCompraService(
            repository.Object,
            Mock.Of<IOrdenCompraRepository>(),
            Mock.Of<IAlmacenRepository>(),
            Mock.Of<IUbicacionAlmacenRepository>(),
            movimientos.Object,
            new RecepcionCompraExistenciaMaterializador(Mock.Of<IExistenciaVarianteConcurrencyService>(MockBehavior.Strict)),
            new RecepcionCompraKardexRegistrar(Mock.Of<IKardexMovimientoWriter>(MockBehavior.Strict), currentUser.Object),
            currentUser.Object,
            unitOfWork,
            auditoria.Object);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.AnularAsync(44, new() { Motivo = "Reversión QA" }));

        Assert.Contains("movimientos de inventario posteriores", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(EstadoRecepcionCompra.Recibida, recepcion.Estado);
        Assert.Null(recepcion.FechaAnulacionUtc);
        Assert.Equal(1, unitOfWork.Calls);
        movimientos.Verify(x => x.ExisteMovimientoPosteriorRecepcionAsync(44), Times.Once);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
        auditoria.Verify(
            x => x.RegistrarEstrictoAsync(
                It.IsAny<ModuloSistema>(),
                It.IsAny<AccionPermiso>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<object?>(),
                It.IsAny<object?>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task Auditoria_estricta_usa_correlation_id_saneado_y_snapshot_no_expone_texto_libre()
    {
        var recepcion = new RecepcionCompra
        {
            Id = 44,
            NumeroRecepcion = "RC-SEC-44",
            OrdenCompraId = 10,
            Observaciones = "SECRETO-OBSERVACION",
            Detalles = new List<RecepcionCompraDetalle>()
        };
        recepcion.EstablecerIdempotencia("SECRETO-IDEMPOTENCY", new string('a', 64));

        var snapshotMethod = typeof(RecepcionCompraService).GetMethod(
            "Snapshot",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("No se encontró el snapshot seguro de RecepcionCompra.");
        var snapshot = snapshotMethod.Invoke(null, new object[] { recepcion });
        var serializedSnapshot = JsonSerializer.Serialize(snapshot);

        Assert.Contains("NumeroRecepcion", serializedSnapshot, StringComparison.Ordinal);
        Assert.Contains("OrdenCompraId", serializedSnapshot, StringComparison.Ordinal);
        Assert.DoesNotContain("SECRETO-OBSERVACION", serializedSnapshot, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SECRETO-IDEMPOTENCY", serializedSnapshot, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Observaciones", serializedSnapshot, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Idempotency", serializedSnapshot, StringComparison.OrdinalIgnoreCase);

        RegistroAuditoria? registrada = null;
        var repository = new Mock<IAuditoriaRepository>(MockBehavior.Strict);
        repository.Setup(x => x.AddAsync(It.IsAny<RegistroAuditoria>()))
            .Callback<RegistroAuditoria>(x => registrada = x)
            .Returns(Task.CompletedTask);
        repository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Loose);
        currentUser.SetupGet(x => x.UsuarioId).Returns(7);
        currentUser.SetupGet(x => x.NombreUsuario).Returns("qa");

        var http = new DefaultHttpContext { TraceIdentifier = "corr-N23_2026.08" };
        var accessor = new HttpContextAccessor { HttpContext = http };
        var auditoria = new AuditoriaService(
            repository.Object,
            currentUser.Object,
            accessor,
            NullLogger<AuditoriaService>.Instance);

        await auditoria.RegistrarEstrictoAsync(
            ModuloSistema.Compras,
            AccionPermiso.Confirmar,
            "Recepción confirmada",
            referenciaId: recepcion.Id,
            entidad: "RecepcionCompra",
            valoresNuevos: snapshot);

        Assert.NotNull(registrada);
        Assert.Equal("corr-N23_2026.08", registrada!.CorrelationId);
        Assert.Equal("RecepcionCompra", registrada.Entidad);
        Assert.Equal(AccionPermiso.Confirmar, registrada.Accion);
        Assert.DoesNotContain("SECRETO-OBSERVACION", registrada.ValoresNuevos ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SECRETO-IDEMPOTENCY", registrada.ValoresNuevos ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        repository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    private sealed class CountingUnitOfWork : IUnitOfWork
    {
        public int Calls { get; private set; }

        public async Task ExecuteInTransactionAsync(Func<Task> operation)
        {
            Calls++;
            await operation();
        }
    }
}
