using System.Reflection;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N22OrdenCompraServiceRegressionTests
{
    [Fact]
    public async Task Reintento_misma_clave_y_mismo_payload_devuelve_misma_orden_sin_duplicar()
    {
        const string key = "oc-test-replay-001";
        var dto = CrearDto();
        var fingerprint = CalcularFingerprint(dto);
        var existente = CrearOrdenPersistida(key, fingerprint);
        var fixture = CrearFixture();
        fixture.Repository.Setup(x => x.GetByIdempotencyKeyAsync(key, false)).ReturnsAsync(existente);

        var result = await fixture.Service.CreateAsync(dto, key);

        Assert.Equal(existente.Id, result.Id);
        Assert.Equal(existente.NumeroOrden, result.NumeroOrden);
        fixture.Repository.Verify(x => x.AddAsync(It.IsAny<OrdenCompra>()), Times.Never);
        fixture.UnitOfWork.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()), Times.Never);
        fixture.Auditoria.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Reusar_clave_con_payload_distinto_falla_con_conflict()
    {
        const string key = "oc-test-conflict-001";
        var existente = CrearOrdenPersistida(key, new string('0', 64));
        var fixture = CrearFixture();
        fixture.Repository.Setup(x => x.GetByIdempotencyKeyAsync(key, false)).ReturnsAsync(existente);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => fixture.Service.CreateAsync(CrearDto(), key));

        Assert.Contains("payload diferente", ex.Message, StringComparison.OrdinalIgnoreCase);
        fixture.Repository.Verify(x => x.AddAsync(It.IsAny<OrdenCompra>()), Times.Never);
        fixture.Auditoria.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Creacion_desde_solicitud_no_aprobada_se_rechaza_antes_de_persistir()
    {
        const string key = "oc-test-source-001";
        var dto = CrearDto();
        dto.SolicitudCompraId = 19;
        var fixture = CrearFixture();
        fixture.Repository.Setup(x => x.GetByIdempotencyKeyAsync(key, false)).ReturnsAsync((OrdenCompra?)null);
        fixture.Repository.Setup(x => x.GetByIdempotencyKeyAsync(key, true)).ReturnsAsync((OrdenCompra?)null);
        fixture.UnitOfWork.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(operation => operation());
        fixture.CurrentUser.SetupGet(x => x.EstaAutenticado).Returns(true);
        fixture.CurrentUser.SetupGet(x => x.UsuarioId).Returns(7);
        fixture.CurrentUser.SetupGet(x => x.NombreUsuario).Returns("tester");
        fixture.Proveedores.Setup(x => x.GetByIdAsync(3)).ReturnsAsync(new Proveedor { Id = 3, Nombre = "Proveedor", Activo = true });
        fixture.Solicitudes.Setup(x => x.GetByIdAsync(19, false)).ReturnsAsync(new SolicitudCompra
        {
            Id = 19,
            NumeroSolicitud = "SC-19",
            ProveedorId = 3
        });

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => fixture.Service.CreateAsync(dto, key));

        Assert.Contains("aprobada", ex.Message, StringComparison.OrdinalIgnoreCase);
        fixture.Repository.Verify(x => x.AddAsync(It.IsAny<OrdenCompra>()), Times.Never);
        fixture.Auditoria.VerifyNoOtherCalls();
    }

    [Fact]
    public void Servicio_no_depende_de_inventario_kardex_compra_ni_finanzas()
    {
        var parametros = typeof(OrdenCompraService).GetConstructors().Single().GetParameters();
        var nombres = parametros.Select(x => x.ParameterType.Name).ToArray();

        Assert.DoesNotContain(nombres, x => x.Contains("Inventario", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(nombres, x => x.Contains("Kardex", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(nombres, x => x.Equals("ICompraRepository", StringComparison.Ordinal));
        Assert.DoesNotContain(nombres, x => x.Contains("Financiero", StringComparison.OrdinalIgnoreCase));
    }

    private static CreateOrdenCompraDto CrearDto() => new()
    {
        ProveedorId = 3,
        Moneda = "HNL",
        CondicionesCompra = "30 días",
        Detalles = new List<OrdenCompraDetalleInputDto>
        {
            new()
            {
                ProductoId = 10,
                ProductoVarianteId = 11,
                CantidadOrdenada = 2m,
                PrecioUnitario = 100m,
                Descuento = 5m,
                Impuesto = 28.50m
            }
        }
    };

    private static OrdenCompra CrearOrdenPersistida(string key, string fingerprint)
    {
        var detalle = new OrdenCompraDetalle { ProductoId = 10, ProductoVarianteId = 11, ProductoNombreSnapshot = "Producto" };
        detalle.EstablecerValores(2m, 100m, 5m, 28.50m);
        var orden = new OrdenCompra
        {
            Id = 44,
            NumeroOrden = "OC-2026-000044",
            ProveedorId = 3,
            ProveedorNombreSnapshot = "Proveedor",
            Moneda = "HNL",
            Detalles = new List<OrdenCompraDetalle> { detalle }
        };
        orden.EstablecerIdempotencia(key, fingerprint);
        return orden;
    }

    private static string CalcularFingerprint(CreateOrdenCompraDto dto)
    {
        var method = typeof(OrdenCompraService).GetMethod("CalcularFingerprint", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return Assert.IsType<string>(method!.Invoke(null, new object[] { dto }));
    }

    private static Fixture CrearFixture()
    {
        var repository = new Mock<IOrdenCompraRepository>(MockBehavior.Strict);
        var proveedores = new Mock<IProveedorRepository>(MockBehavior.Strict);
        var productos = new Mock<IProductoRepository>(MockBehavior.Strict);
        var solicitudes = new Mock<ISolicitudCompraRepository>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Loose);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        var service = new OrdenCompraService(repository.Object, proveedores.Object, productos.Object, solicitudes.Object, currentUser.Object, unitOfWork.Object, auditoria.Object);
        return new Fixture(service, repository, proveedores, productos, solicitudes, currentUser, unitOfWork, auditoria);
    }

    private sealed record Fixture(
        OrdenCompraService Service,
        Mock<IOrdenCompraRepository> Repository,
        Mock<IProveedorRepository> Proveedores,
        Mock<IProductoRepository> Productos,
        Mock<ISolicitudCompraRepository> Solicitudes,
        Mock<ICurrentUserService> CurrentUser,
        Mock<IUnitOfWork> UnitOfWork,
        Mock<IAuditoriaService> Auditoria);
}
