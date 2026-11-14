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
            Mock.Of<ICurrentUserService>());

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
}
