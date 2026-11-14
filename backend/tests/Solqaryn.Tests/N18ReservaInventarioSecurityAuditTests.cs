using System.Globalization;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N18ReservaInventarioSecurityAuditTests
{
    [Fact]
    public void Controller_ExigeAutenticacionYNoPermiteBypassAnonimo()
    {
        Assert.Contains(
            typeof(ReservasInventarioController).CustomAttributes,
            atributo => atributo.AttributeType == typeof(AuthorizeAttribute));
        Assert.DoesNotContain(
            typeof(ReservasInventarioController).CustomAttributes,
            atributo => atributo.AttributeType == typeof(AllowAnonymousAttribute));

        var endpoints = typeof(ReservasInventarioController)
            .GetMethods()
            .Where(m => m.DeclaringType == typeof(ReservasInventarioController));

        Assert.DoesNotContain(endpoints, method =>
            method.CustomAttributes.Any(attribute => attribute.AttributeType == typeof(AllowAnonymousAttribute)));
    }

    [Theory]
    [InlineData(nameof(ReservasInventarioController.Buscar), AccionPermiso.Ver)]
    [InlineData(nameof(ReservasInventarioController.GetById), AccionPermiso.Ver)]
    [InlineData(nameof(ReservasInventarioController.Create), AccionPermiso.Crear)]
    [InlineData(nameof(ReservasInventarioController.Update), AccionPermiso.Editar)]
    [InlineData(nameof(ReservasInventarioController.Activar), AccionPermiso.Confirmar)]
    [InlineData(nameof(ReservasInventarioController.Consumir), AccionPermiso.Confirmar)]
    [InlineData(nameof(ReservasInventarioController.Liberar), AccionPermiso.Anular)]
    [InlineData(nameof(ReservasInventarioController.Expirar), AccionPermiso.CambiarEstado)]
    [InlineData(nameof(ReservasInventarioController.Cancelar), AccionPermiso.Anular)]
    public void Endpoints_ExigenPermisoRelacionalExacto(string metodo, AccionPermiso accionEsperada)
    {
        var methodInfo = typeof(ReservasInventarioController).GetMethod(metodo);
        Assert.NotNull(methodInfo);

        var permiso = Assert.Single(methodInfo!.CustomAttributes.Where(a =>
            a.AttributeType == typeof(RequierePermisoAttribute)));
        var modulo = (ModuloSistema)Convert.ToInt32(
            permiso.ConstructorArguments[0].Value,
            CultureInfo.InvariantCulture);
        var accion = (AccionPermiso)Convert.ToInt32(
            permiso.ConstructorArguments[1].Value,
            CultureInfo.InvariantCulture);

        Assert.Equal(ModuloSistema.MovimientosInventario, modulo);
        Assert.Equal(accionEsperada, accion);
    }

    [Fact]
    public async Task Activar_RegistraAuditoriaEstrictaDentroDeLaMismaTransaccion()
    {
        var enTransaccion = false;
        var (service, auditoria) = CrearServicioAuditable(
            onTransaction: async action =>
            {
                enTransaccion = true;
                try { await action(); }
                finally { enTransaccion = false; }
            },
            onAudit: () => Assert.True(enTransaccion));

        var resultado = await service.ActivarAsync(7);

        Assert.Equal(EstadoReservaInventario.Activa.ToString(), resultado.Estado);
        auditoria.Verify(x => x.RegistrarEstrictoAsync(
            ModuloSistema.MovimientosInventario,
            AccionPermiso.Confirmar,
            "Reserva activada correctamente.",
            7,
            nameof(ReservaInventario),
            null,
            It.IsAny<object>(),
            null,
            "Exito",
            null), Times.Once);
        auditoria.Verify(x => x.RegistrarAsync(
            It.IsAny<ModuloSistema>(),
            It.IsAny<AccionPermiso>(),
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<string?>(),
            It.IsAny<object?>(),
            It.IsAny<object?>(),
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task Activar_SiAuditoriaEstrictaFalla_PropagaErrorDeLaTransaccion()
    {
        var (service, auditoria) = CrearServicioAuditable(
            onTransaction: action => action(),
            onAudit: null);
        auditoria.Setup(x => x.RegistrarEstrictoAsync(
                It.IsAny<ModuloSistema>(),
                It.IsAny<AccionPermiso>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<object?>(),
                It.IsAny<object?>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<string?>()))
            .ThrowsAsync(new InvalidOperationException("auditoría no disponible"));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ActivarAsync(7));

        Assert.Equal("auditoría no disponible", error.Message);
    }

    private static (ReservaInventarioService Service, Mock<IAuditoriaService> Auditoria) CrearServicioAuditable(
        Func<Func<Task>, Task> onTransaction,
        Action? onAudit)
    {
        var variante = new ProductoVariante
        {
            Id = 10,
            ProductoId = 1,
            Activo = true,
            Sku = "SKU-RSV-SEC",
            Producto = new Producto { Id = 1, Nombre = "Producto reserva seguridad" }
        };
        var detalle = new ReservaInventarioDetalle
        {
            Id = 8,
            ProductoVarianteId = 10,
            ProductoVariante = variante,
            AlmacenId = 20,
            ProductoSkuSnapshot = variante.Sku
        };
        detalle.EstablecerCantidadReservada(3);
        var reserva = new ReservaInventario
        {
            Id = 7,
            Numero = "RSV-SEC-0007",
            CreadoPorUsuarioId = 5,
            Detalles = new List<ReservaInventarioDetalle> { detalle }
        };
        var existencia = new ExistenciaVariante
        {
            Id = 91,
            ProductoVarianteId = 10,
            AlmacenId = 20,
            ProductoVariante = variante
        };
        existencia.EstablecerStocks(12, 2, 0, 0, null);
        var clave = new InventarioExistenciaClave(10, 20, null);

        var repository = new Mock<IReservaInventarioRepository>();
        repository.Setup(x => x.GetByIdAsync(7, It.IsAny<bool>())).ReturnsAsync(reserva);
        repository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var variantes = new Mock<IProductoVarianteRepository>();
        var existencias = new Mock<IExistenciaVarianteConcurrencyService>();
        existencias.Setup(x => x.BloquearYValidarExistenciasAsync(
                It.IsAny<IEnumerable<InventarioDemandaExistencia>>(), true))
            .ReturnsAsync(new InventarioExistenciaLockSet(
                new Dictionary<InventarioExistenciaClave, ExistenciaVariante> { [clave] = existencia },
                new[] { new InventarioDemandaExistencia(1, 10, 20, null, 3) }));
        existencias.Setup(x => x.AjustarStockReservadoPesimistaAsync(clave, 2, 5))
            .Returns(Task.CompletedTask)
            .Callback(() => existencia.EstablecerStocks(12, 5, 0, 0, null));

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.EstaAutenticado).Returns(true);
        currentUser.SetupGet(x => x.UsuarioId).Returns(5);
        currentUser.SetupGet(x => x.NombreUsuario).Returns("n18-security");

        var auditoria = new Mock<IAuditoriaService>();
        auditoria.Setup(x => x.RegistrarEstrictoAsync(
                It.IsAny<ModuloSistema>(),
                It.IsAny<AccionPermiso>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<object?>(),
                It.IsAny<object?>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<string?>()))
            .Callback(() => onAudit?.Invoke())
            .Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns((Func<Task> action) => onTransaction(action));

        return (new ReservaInventarioService(
            repository.Object,
            variantes.Object,
            existencias.Object,
            currentUser.Object,
            auditoria.Object,
            unitOfWork.Object), auditoria);
    }
}
