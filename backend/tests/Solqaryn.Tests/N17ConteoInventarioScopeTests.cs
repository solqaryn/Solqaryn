using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N17ConteoInventarioScopeTests
{
    [Fact]
    public async Task Crear_PorUbicacionSinUbicacion_FallaAntesDeConsultarExistencias()
    {
        var (service, repository, existencias) = CrearServicio();

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(new CreateConteoInventarioDto
        {
            Tipo = TipoConteoInventario.PorUbicacion,
            AlmacenId = 3
        }));

        Assert.Contains("UbicacionAlmacenId", error.Message, StringComparison.OrdinalIgnoreCase);
        repository.Verify(x => x.ExisteNumeroAsync(It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
        existencias.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Crear_PorCategoriaSinCategoria_FallaAntesDeConsultarExistencias()
    {
        var (service, repository, existencias) = CrearServicio();

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(new CreateConteoInventarioDto
        {
            Tipo = TipoConteoInventario.PorCategoria,
            AlmacenId = 3
        }));

        Assert.Contains("CategoriaId", error.Message, StringComparison.OrdinalIgnoreCase);
        repository.Verify(x => x.ExisteNumeroAsync(It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
        existencias.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Actualizar_ScopeInvalido_FallaAntesDeTomarLock()
    {
        var (service, repository, existencias) = CrearServicio();

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpdateAsync(18, new UpdateConteoInventarioDto
        {
            Tipo = TipoConteoInventario.PorUbicacion,
            AlmacenId = 3,
            UbicacionAlmacenId = 0
        }));

        Assert.Contains("UbicacionAlmacenId", error.Message, StringComparison.OrdinalIgnoreCase);
        repository.Verify(x => x.GetByIdForUpdateAsync(It.IsAny<int>()), Times.Never);
        existencias.VerifyNoOtherCalls();
    }

    private static (ConteoInventarioService Service, Mock<IConteoInventarioRepository> Repository, Mock<IExistenciaVarianteRepository> Existencias) CrearServicio()
    {
        var repository = new Mock<IConteoInventarioRepository>();
        var existencias = new Mock<IExistenciaVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var service = new ConteoInventarioService(repository.Object, existencias.Object, currentUser.Object, unitOfWork.Object);
        return (service, repository, existencias);
    }
}
