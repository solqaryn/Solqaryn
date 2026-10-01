using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N26DevolucionProveedorSecurityAuditTests
{
    [Fact]
    public void Controller_exige_autenticacion()
    {
        Assert.Contains(
            typeof(DevolucionesProveedorController).GetCustomAttributes(inherit: true),
            attribute => attribute is AuthorizeAttribute);
    }

    [Theory]
    [InlineData(nameof(DevolucionesProveedorController.Buscar), AccionPermiso.Ver)]
    [InlineData(nameof(DevolucionesProveedorController.GetById), AccionPermiso.Ver)]
    [InlineData(nameof(DevolucionesProveedorController.Create), AccionPermiso.Crear)]
    [InlineData(nameof(DevolucionesProveedorController.Update), AccionPermiso.Editar)]
    [InlineData(nameof(DevolucionesProveedorController.Confirmar), AccionPermiso.Confirmar)]
    [InlineData(nameof(DevolucionesProveedorController.Anular), AccionPermiso.Anular)]
    public void Endpoint_declara_permiso_compras_esperado(string methodName, AccionPermiso accion)
    {
        var method = typeof(DevolucionesProveedorController).GetMethod(methodName)
            ?? throw new InvalidOperationException($"No se encontró {methodName}.");
        var permiso = Assert.Single(
            method.CustomAttributes.Where(attribute => attribute.AttributeType == typeof(RequierePermisoAttribute)));

        Assert.Equal((int)ModuloSistema.Compras, Convert.ToInt32(permiso.ConstructorArguments[0].Value));
        Assert.Equal((int)accion, Convert.ToInt32(permiso.ConstructorArguments[1].Value));
    }

    [Theory]
    [InlineData(AccionPermiso.Ver)]
    [InlineData(AccionPermiso.Crear)]
    [InlineData(AccionPermiso.Editar)]
    [InlineData(AccionPermiso.Confirmar)]
    [InlineData(AccionPermiso.Anular)]
    public async Task Administrador_sin_grant_explicito_no_tiene_bypass(AccionPermiso accion)
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
    }

    [Fact]
    public async Task Create_registra_auditoria_estricta_en_compras()
    {
        var repo = new Mock<IDevolucionProveedorRepository>();
        repo.Setup(x => x.GetByIdempotencyKeyAsync("qa-key", false)).ReturnsAsync((DevolucionProveedor?)null);
        repo.Setup(x => x.GetByIdempotencyKeyAsync("qa-key", true)).ReturnsAsync((DevolucionProveedor?)null);
        repo.Setup(x => x.ExisteNumeroAsync(It.IsAny<string>(), null)).ReturnsAsync(false);
        repo.Setup(x => x.AddAsync(It.IsAny<DevolucionProveedor>()))
            .Callback<DevolucionProveedor>(x => x.Id = 91)
            .Returns(Task.CompletedTask);
        repo.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var recepciones = new Mock<IRecepcionCompraRepository>();
        recepciones.Setup(x => x.GetByIdAsync(20, false)).ReturnsAsync(CrearRecepcion());
        var facturas = new Mock<IFacturaProveedorRepository>();
        facturas.Setup(x => x.GetByIdAsync(30, false)).ReturnsAsync(CrearFactura());

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.UsuarioId).Returns(7);
        currentUser.SetupGet(x => x.NombreUsuario).Returns("qa");

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns((Func<Task> operation) => operation());

        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        auditoria.Setup(x => x.RegistrarEstrictoAsync(
                ModuloSistema.Compras,
                AccionPermiso.Crear,
                It.Is<string>(descripcion => descripcion.Contains("creada", StringComparison.OrdinalIgnoreCase)),
                91,
                "DevolucionProveedor",
                null,
                It.IsAny<object?>(),
                null,
                It.IsAny<string>(),
                It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var service = new DevolucionProveedorService(
            repo.Object,
            recepciones.Object,
            facturas.Object,
            currentUser.Object,
            uow.Object,
            auditoria.Object);

        await service.CreateAsync(new CreateDevolucionProveedorDto
        {
            RecepcionCompraId = 20,
            FacturaProveedorId = 30,
            Motivo = "QA seguridad",
            Detalles = new()
            {
                new DevolucionProveedorDetalleInputDto
                {
                    RecepcionCompraDetalleId = 200,
                    Cantidad = 1m
                }
            }
        }, "qa-key");

        auditoria.VerifyAll();
    }

    [Fact]
    public async Task Confirmar_sin_usuario_valido_falla_antes_de_transaccion_y_auditoria()
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.UsuarioId).Returns((int?)null);

        var uow = new CountingUnitOfWork();
        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        var service = new DevolucionProveedorService(
            Mock.Of<IDevolucionProveedorRepository>(),
            Mock.Of<IRecepcionCompraRepository>(),
            Mock.Of<IFacturaProveedorRepository>(),
            currentUser.Object,
            uow,
            auditoria.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ConfirmarAsync(91));

        Assert.Equal(0, uow.Calls);
        auditoria.VerifyNoOtherCalls();
    }

    private static RecepcionCompra CrearRecepcion()
    {
        var detalle = new RecepcionCompraDetalle
        {
            Id = 200,
            OrdenCompraDetalleId = 400,
            ProductoId = 500,
            ProductoVarianteId = 600,
            AlmacenId = 700,
            CostoUnitarioSnapshot = 10m,
            ProductoNombreSnapshot = "Producto QA"
        };
        detalle.EstablecerCantidades(10m);

        var recepcion = new RecepcionCompra
        {
            Id = 20,
            NumeroRecepcion = "RC-QA-20",
            OrdenCompraId = 40,
            OrdenCompra = new OrdenCompra { Id = 40, ProveedorId = 10 },
            Detalles = new List<RecepcionCompraDetalle> { detalle }
        };
        recepcion.Confirmar(7, "qa", DateTime.UtcNow);
        return recepcion;
    }

    private static FacturaProveedor CrearFactura()
    {
        var detalle = new FacturaProveedorDetalle
        {
            Id = 300,
            OrdenCompraDetalleId = 400,
            ProductoId = 500,
            ProductoVarianteId = 600,
            ProductoNombreSnapshot = "Producto QA"
        };
        detalle.EstablecerValores(10m, 10m, 10m, 15m);

        var factura = new FacturaProveedor
        {
            Id = 30,
            NumeroFactura = "FP-QA-30",
            ProveedorId = 10,
            OrdenCompraId = 40,
            ProveedorNombreSnapshot = "Proveedor QA",
            Moneda = "HNL",
            FechaEmisionUtc = DateTime.UtcNow.Date,
            Detalles = new List<FacturaProveedorDetalle> { detalle }
        };
        factura.Registrar(7, "qa", DateTime.UtcNow);
        return factura;
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
