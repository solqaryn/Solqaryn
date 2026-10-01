using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class N27NotaCreditoProveedorApplicationApiTests
{
    [Fact]
    public async Task Paginacion_normaliza_limites_y_rango_utc()
    {
        NotaCreditoProveedorFiltroDto? recibido = null;
        var repository = new Mock<INotaCreditoProveedorRepository>();
        repository
            .Setup(x => x.GetPagedAsync(It.IsAny<NotaCreditoProveedorFiltroDto>()))
            .Callback<NotaCreditoProveedorFiltroDto>(x => recibido = x)
            .ReturnsAsync((Array.Empty<NotaCreditoProveedor>(), 0));

        var service = CrearServicio(repository: repository);
        var desde = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var hasta = new DateTime(2026, 8, 31, 23, 59, 59, DateTimeKind.Utc);

        var result = await service.GetPagedAsync(new NotaCreditoProveedorFiltroDto
        {
            Page = 0,
            PageSize = 500,
            Desde = desde,
            Hasta = hasta,
            SortDirection = "unexpected"
        });

        Assert.NotNull(recibido);
        Assert.Equal(1, recibido!.Page);
        Assert.Equal(100, recibido.PageSize);
        Assert.Equal("desc", recibido.SortDirection);
        Assert.Equal(1, result.Page);
        Assert.Equal(100, result.PageSize);
    }

    [Fact]
    public async Task Reintento_create_mismo_payload_no_duplica_persistencia_ni_auditoria()
    {
        var factura = new FacturaProveedor { Id = 44, ProveedorId = 7 };
        EstablecerPropiedadPrivada(factura, nameof(FacturaProveedor.Estado), EstadoFacturaProveedor.Registrada);
        var proveedor = new Proveedor { Id = 7, Nombre = "Proveedor Uno" };
        var existente = new NotaCreditoProveedor
        {
            Id = 91,
            NumeroNotaCredito = "NC-001",
            ProveedorId = 7,
            FacturaProveedorId = 44,
            ProveedorNombreSnapshot = "Proveedor Uno",
            Moneda = "HNL",
            FechaEmisionUtc = new DateTime(2026, 8, 22, 8, 0, 0, DateTimeKind.Utc),
            Motivo = "Ajuste comercial",
            SubtotalCredito = 100m,
            ImpuestoCredito = 15m
        };

        var repository = new Mock<INotaCreditoProveedorRepository>();
        repository
            .Setup(x => x.GetByProveedorNumeroAsync(7, "NC-001", false))
            .ReturnsAsync(existente);

        var facturas = new Mock<IFacturaProveedorRepository>();
        facturas.Setup(x => x.GetByIdAsync(44, false)).ReturnsAsync(factura);
        var proveedores = new Mock<IProveedorRepository>();
        proveedores.Setup(x => x.GetByIdAsync(7)).ReturnsAsync(proveedor);
        var auditoria = new Mock<IAuditoriaService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.EstaAutenticado).Returns(true);
        currentUser.SetupGet(x => x.UsuarioId).Returns(5);

        var service = CrearServicio(repository, facturas, proveedores, currentUser: currentUser, auditoria: auditoria, unitOfWork: unitOfWork);
        var dto = new CreateNotaCreditoProveedorDto
        {
            NumeroNotaCredito = " nc-001 ",
            FacturaProveedorId = 44,
            FechaEmisionUtc = existente.FechaEmisionUtc,
            Moneda = "hnl",
            Motivo = "Ajuste comercial",
            SubtotalCredito = 100m,
            ImpuestoCredito = 15m
        };

        var result = await service.CreateAsync(dto);

        Assert.Equal(91, result.Id);
        repository.Verify(x => x.AddAsync(It.IsAny<NotaCreditoProveedor>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
        unitOfWork.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()), Times.Never);
        auditoria.Verify(x => x.RegistrarEstrictoAsync(
            It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>(), It.IsAny<string>(),
            It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<object?>(),
            It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public void Controller_exige_auth_y_rbac_explicito_por_operacion()
    {
        var type = typeof(NotasCreditoProveedorController);
        Assert.NotNull(type.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Null(type.GetCustomAttribute<AllowAnonymousAttribute>());

        AssertPermiso(type, nameof(NotasCreditoProveedorController.Buscar), AccionPermiso.Ver);
        AssertPermiso(type, nameof(NotasCreditoProveedorController.GetById), AccionPermiso.Ver);
        AssertPermiso(type, nameof(NotasCreditoProveedorController.Create), AccionPermiso.Crear);
        AssertPermiso(type, nameof(NotasCreditoProveedorController.Update), AccionPermiso.Editar);
        AssertPermiso(type, nameof(NotasCreditoProveedorController.Registrar), AccionPermiso.Confirmar);
        AssertPermiso(type, nameof(NotasCreditoProveedorController.Anular), AccionPermiso.Anular);

        var route = type.GetCustomAttribute<RouteAttribute>();
        Assert.Equal("notas-credito-proveedor", route?.Template);
    }

    [Fact]
    public void Servicio_declara_transaccion_auditoria_y_autoridad_documental_sin_dependencias_de_stock()
    {
        var parameters = typeof(NotaCreditoProveedorService)
            .GetConstructors()
            .Single()
            .GetParameters()
            .Select(x => x.ParameterType)
            .ToHashSet();

        Assert.Contains(typeof(IUnitOfWork), parameters);
        Assert.Contains(typeof(IAuditoriaService), parameters);
        Assert.Contains(typeof(IFacturaProveedorRepository), parameters);
        Assert.Contains(typeof(IDevolucionProveedorRepository), parameters);
        Assert.DoesNotContain(typeof(IExistenciaVarianteConcurrencyService), parameters);
        Assert.DoesNotContain(typeof(IKardexMovimientoWriter), parameters);
        Assert.DoesNotContain(typeof(IMovimientoFinancieroRepository), parameters);
    }

    private static NotaCreditoProveedorService CrearServicio(
        Mock<INotaCreditoProveedorRepository>? repository = null,
        Mock<IFacturaProveedorRepository>? facturas = null,
        Mock<IProveedorRepository>? proveedores = null,
        Mock<IDevolucionProveedorRepository>? devoluciones = null,
        Mock<ICurrentUserService>? currentUser = null,
        Mock<IUnitOfWork>? unitOfWork = null,
        Mock<IAuditoriaService>? auditoria = null)
    {
        repository ??= new Mock<INotaCreditoProveedorRepository>();
        facturas ??= new Mock<IFacturaProveedorRepository>();
        proveedores ??= new Mock<IProveedorRepository>();
        devoluciones ??= new Mock<IDevolucionProveedorRepository>();
        currentUser ??= new Mock<ICurrentUserService>();
        unitOfWork ??= new Mock<IUnitOfWork>();
        auditoria ??= new Mock<IAuditoriaService>();
        var logger = new Mock<ILogger<NotaCreditoProveedorService>>();

        return new NotaCreditoProveedorService(
            repository.Object,
            facturas.Object,
            proveedores.Object,
            devoluciones.Object,
            currentUser.Object,
            unitOfWork.Object,
            auditoria.Object,
            logger.Object);
    }

    private static void AssertPermiso(Type type, string methodName, AccionPermiso expected)
    {
        var method = type.GetMethod(methodName) ?? throw new InvalidOperationException(methodName);
        var attr = method.GetCustomAttribute<RequierePermisoAttribute>();
        Assert.NotNull(attr);

        var modulo = (ModuloSistema?)typeof(RequierePermisoAttribute)
            .GetField("_modulo", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(attr);
        var accion = (AccionPermiso?)typeof(RequierePermisoAttribute)
            .GetField("_accion", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(attr);

        Assert.Equal(ModuloSistema.Compras, modulo);
        Assert.Equal(expected, accion);
    }

    private static void EstablecerPropiedadPrivada<T>(object target, string propertyName, T value)
    {
        var property = target.GetType().GetProperty(propertyName)
            ?? throw new InvalidOperationException(propertyName);
        property.SetValue(target, value);
    }
}
