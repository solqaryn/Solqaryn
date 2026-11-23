using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class ImpuestoCodigoEstableTests
{
    [Fact]
    public async Task UpdateAsync_RechazaCambioDeCodigoFiscalEstable()
    {
        var existente = new Impuesto
        {
            Id = 1,
            Nombre = "ISV 15%",
            Codigo = "ISV15",
            Tipo = TipoImpuesto.Porcentaje,
            Tasa = 15m,
            IncluidoEnPrecio = true,
            Activo = true,
            Operaciones = [new ImpuestoOperacion { ImpuestoId = 1, Operacion = OperacionImpuesto.Venta }]
        };

        var repository = new Mock<IImpuestoRepository>();
        repository.Setup(x => x.GetByIdConRelacionesAsync(1)).ReturnsAsync(existente);

        var service = new ImpuestoService(
            repository.Object,
            Mock.Of<IAuditoriaService>(),
            Mock.Of<ICurrentUserService>(),
            Mock.Of<IUnitOfWork>());

        var dto = new GuardarImpuestoDto
        {
            Nombre = "ISV administrado",
            Codigo = "ISV_RENOMBRADO",
            Tipo = "Porcentaje",
            Tasa = 12m,
            IncluidoEnPrecio = false,
            Acumulativo = true,
            Prioridad = 10,
            Operaciones = ["Venta"]
        };

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpdateAsync(1, dto));

        Assert.Contains("código", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no puede modificarse", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("ISV15", existente.Codigo);
        repository.Verify(x => x.Update(It.IsAny<Impuesto>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }
    [Fact]
    public async Task CreateAsync_AuditoriaEstricaFallaAntesDeConfirmarUnidadTransaccional()
    {
        var repository = new Mock<IImpuestoRepository>();
        var auditoria = new Mock<IAuditoriaService>();
        var unidad = new Mock<IUnitOfWork>();
        var ingresoEnTransaccion = false;
        unidad.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(async operacion =>
            {
                ingresoEnTransaccion = true;
                await operacion();
            });
        auditoria.Setup(a => a.RegistrarEstrictoAsync(
                ModuloSistema.Impuestos, AccionPermiso.Crear,
                It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(),
                It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<string?>()))
            .ThrowsAsync(new InvalidOperationException("AUDIT_SINK_DOWN"));
        var servicio = new ImpuestoService(repository.Object, auditoria.Object,
            Mock.Of<ICurrentUserService>(), unidad.Object);
        var alta = new GuardarImpuestoDto
        {
            Codigo = "ISV15-TEST", Nombre = "ISV prueba", Tipo = "Porcentaje",
            Tasa = 15m, Operaciones = ["Venta"]
        };

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.CreateAsync(alta));
        Assert.Equal("AUDIT_SINK_DOWN", error.Message);
        Assert.True(ingresoEnTransaccion);
        repository.Verify(x => x.AddAsync(It.IsAny<Impuesto>()), Times.Once);
        repository.Verify(x => x.SaveChangesAsync(), Times.Once);
        unidad.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()), Times.Once);
        auditoria.Verify(a => a.RegistrarEstrictoAsync(
            ModuloSistema.Impuestos, AccionPermiso.Crear,
            It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(),
            It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>(),
            It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
        // Mock contract proves exception propagation; physical MySQL rollback
        // remains an A3 integration requirement and is NOT certified here.
    }

}
