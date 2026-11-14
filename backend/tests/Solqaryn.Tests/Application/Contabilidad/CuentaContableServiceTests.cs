using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests.Application.Contabilidad;

public sealed class CuentaContableServiceTests
{
    private readonly Mock<ICuentaContableRepository> _repository = new();
    private readonly Mock<IAuditoriaService> _auditoria = new();

    public CuentaContableServiceTests()
    {
        _auditoria.Setup(x => x.RegistrarAsync(
                It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>(), It.IsAny<string>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<object?>(),
                It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task CreateAsync_ValidatesAndMapsAccount()
    {
        _repository.Setup(x => x.GetByCodigoAsync("1000")).ReturnsAsync((CuentaContable?)null);
        _repository.Setup(x => x.AddAsync(It.IsAny<CuentaContable>())).Returns(Task.CompletedTask);
        _repository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        var result = await new CuentaContableService(_repository.Object, _auditoria.Object).CreateAsync(new CreateCuentaContableDto
        {
            Codigo = " 1000 ", Nombre = "Activos", Tipo = TipoCuentaContable.Activo
        });

        Assert.Equal("1000", result.Codigo);
        Assert.True(result.AceptaMovimientos);
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateCodeAndInvalidParent()
    {
        _repository.Setup(x => x.GetByCodigoAsync("1000"))
            .ReturnsAsync(new CuentaContable { Id = 4, Codigo = "1000" });
        var service = new CuentaContableService(_repository.Object, _auditoria.Object);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(new CreateCuentaContableDto
        {
            Codigo = "1000", Nombre = "Duplicada", Tipo = TipoCuentaContable.Activo
        }));

        _repository.Setup(x => x.GetByCodigoAsync("1100")).ReturnsAsync((CuentaContable?)null);
        _repository.Setup(x => x.GetByIdAsync(9)).ReturnsAsync(new CuentaContable
        {
            Id = 9, Tipo = TipoCuentaContable.Pasivo
        });
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(new CreateCuentaContableDto
        {
            Codigo = "1100", Nombre = "Hijo", Tipo = TipoCuentaContable.Activo, CuentaPadreId = 9
        }));
    }
}
