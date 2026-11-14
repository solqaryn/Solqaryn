using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities.Contabilidad;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class N411FCentrosCostoAuditTests
{
    private readonly Mock<ICentroCostoRepository> _repository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IAuditoriaService> _auditoria = new();

    public N411FCentrosCostoAuditTests()
    {
        _currentUser.SetupGet(x => x.UsuarioId).Returns(17);
        _currentUser.SetupGet(x => x.NombreUsuario).Returns("qa-centros-costo");
        _repository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
    }

    [Fact]
    public async Task Create_Stamps_Current_User_And_Registers_Correlated_Audit()
    {
        CentroCosto? agregado = null;
        _repository.Setup(x => x.ExisteCodigoAsync("CC-001", null)).ReturnsAsync(false);
        _repository.Setup(x => x.AddAsync(It.IsAny<CentroCosto>()))
            .Callback<CentroCosto>(x => agregado = x)
            .Returns(Task.CompletedTask);

        var service = CreateService();
        await service.CreateAsync(new CreateCentroCostoDto
        {
            Codigo = " cc-001 ",
            Nombre = "Administración",
            Tipo = TipoCentroCosto.Departamento
        });

        Assert.NotNull(agregado);
        Assert.Equal(17, agregado!.CreadoPorUsuarioId);
        Assert.Equal("qa-centros-costo", agregado.CreadoPorNombreUsuario);

        VerifyAudit(AccionPermiso.Crear);
    }

    [Theory]
    [InlineData(true, AccionPermiso.Activar)]
    [InlineData(false, AccionPermiso.Desactivar)]
    public async Task CambiarEstado_Stamps_Current_User_And_Registers_Audit(bool estadoNuevo, AccionPermiso accion)
    {
        var entity = NewEntity(activo: !estadoNuevo);
        _repository.Setup(x => x.GetByIdAsync(entity.Id)).ReturnsAsync(entity);

        var service = CreateService();
        var result = await service.CambiarEstadoAsync(entity.Id, estadoNuevo);

        Assert.NotNull(result);
        Assert.Equal(estadoNuevo, entity.Activo);
        Assert.Equal(17, entity.ActualizadoPorUsuarioId);
        Assert.Equal("qa-centros-costo", entity.ActualizadoPorNombreUsuario);

        VerifyAudit(accion);
    }

    [Fact]
    public async Task Delete_Stamps_Logical_Delete_And_Registers_Audit()
    {
        var entity = NewEntity(activo: true);
        _repository.Setup(x => x.GetByIdAsync(entity.Id)).ReturnsAsync(entity);

        var service = CreateService();
        var deleted = await service.DeleteAsync(entity.Id);

        Assert.True(deleted);
        Assert.True(entity.Eliminado);
        Assert.False(entity.Activo);
        Assert.Equal(17, entity.EliminadoPorUsuarioId);
        Assert.Equal(17, entity.ActualizadoPorUsuarioId);
        Assert.NotNull(entity.FechaEliminacion);

        VerifyAudit(AccionPermiso.EliminarLogico);
    }

    private CentroCostoService CreateService()
        => new(_repository.Object, _currentUser.Object, _auditoria.Object);

    private void VerifyAudit(AccionPermiso accion)
        => _auditoria.Verify(x => x.RegistrarAsync(
                ModuloSistema.Finanzas,
                accion,
                It.IsAny<string>(),
                It.IsAny<int?>(),
                nameof(CentroCosto),
                It.IsAny<object?>(),
                It.IsAny<object?>(),
                It.IsAny<string?>(),
                "Exito",
                It.IsAny<string?>()),
            Times.Once);

    private static CentroCosto NewEntity(bool activo)
        => new()
        {
            Id = 42,
            Codigo = "CC-042",
            Nombre = "Operaciones",
            Tipo = TipoCentroCosto.Departamento,
            Activo = activo
        };
}
