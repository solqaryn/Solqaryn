using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class N27NotaCreditoProveedorQaRegressionTests
{
    [Fact]
    public async Task Rango_temporal_invertido_falla_antes_de_consultar_repositorio()
    {
        var repository = new Mock<INotaCreditoProveedorRepository>();
        var service = CrearServicio(repository: repository);

        var filtro = new NotaCreditoProveedorFiltroDto
        {
            Desde = new DateTime(2026, 8, 31, 0, 0, 0, DateTimeKind.Utc),
            Hasta = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.GetPagedAsync(filtro));

        repository.Verify(x => x.GetPagedAsync(It.IsAny<NotaCreditoProveedorFiltroDto>()), Times.Never);
    }

    [Fact]
    public async Task Create_sin_usuario_autenticado_falla_cerrado_antes_de_tocar_dependencias()
    {
        var repository = new Mock<INotaCreditoProveedorRepository>();
        var facturas = new Mock<IFacturaProveedorRepository>();
        var proveedores = new Mock<IProveedorRepository>();
        var devoluciones = new Mock<IDevolucionProveedorRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var auditoria = new Mock<IAuditoriaService>();
        currentUser.SetupGet(x => x.EstaAutenticado).Returns(false);

        var service = CrearServicio(
            repository,
            facturas,
            proveedores,
            devoluciones,
            currentUser,
            unitOfWork,
            auditoria);

        var dto = new CreateNotaCreditoProveedorDto
        {
            NumeroNotaCredito = "NC-QA-001",
            FacturaProveedorId = 44,
            FechaEmisionUtc = new DateTime(2026, 8, 22, 12, 0, 0, DateTimeKind.Utc),
            Moneda = "HNL",
            Motivo = "Regresión QA fail-closed",
            SubtotalCredito = 100m,
            ImpuestoCredito = 15m
        };

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.CreateAsync(dto));

        facturas.Verify(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
        proveedores.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
        devoluciones.Verify(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
        repository.Verify(x => x.AddAsync(It.IsAny<Solqaryn.Domain.Entities.NotaCreditoProveedor>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
        unitOfWork.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()), Times.Never);
        auditoria.Verify(x => x.RegistrarEstrictoAsync(
            It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>(), It.IsAny<string>(),
            It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<object?>(),
            It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
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
}
