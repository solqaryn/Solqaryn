using Moq;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests.Application.Services;

public sealed class UsuarioPrimeraMembresiaServiceTests
{
    [Fact]
    public async Task ConsultarMembresiasAntesDePrimeraAsignacion_DevuelveListaVaciaSoloAlCreador()
    {
        var users = new Mock<IUsuarioRepository>(MockBehavior.Strict);
        users.Setup(r => r.GetByIdAsync(41)).ReturnsAsync((Usuario?)null);
        users.Setup(r => r.GetPendientePrimeraMembresiaAsync(41, 10))
            .ReturnsAsync(new Usuario { Id = 41, NombreUsuario = "usuario-pendiente",
                NombreCompleto = "Pendiente", Activo = true });
        users.Setup(r => r.GetEmpresasAsync(41)).ReturnsAsync(new List<UsuarioEmpresa>());
        var actor = new Mock<ICurrentUserService>();
        actor.SetupGet(x => x.UsuarioId).Returns(10);
        var sut = new UsuarioService(users.Object, Mock.Of<IRolRepository>(),
            Mock.Of<IEmpresaRepository>(), Mock.Of<IAuditoriaService>(), actor.Object);

        Assert.Empty(await sut.GetEmpresasAsync(41));
        users.Verify(r => r.GetPendientePrimeraMembresiaAsync(41, 10), Times.Once);
        users.Verify(r => r.GetEmpresasAsync(41), Times.Once);
    }

    [Fact]
    public async Task ConsultarUsuarioNoVisibleNoPuedeExponerMembresiasDeB()
    {
        var users = new Mock<IUsuarioRepository>(MockBehavior.Strict);
        users.Setup(r => r.GetByIdAsync(41)).ReturnsAsync((Usuario?)null);
        users.Setup(r => r.GetPendientePrimeraMembresiaAsync(41, 10))
            .ReturnsAsync((Usuario?)null);
        var actor = new Mock<ICurrentUserService>();
        actor.SetupGet(x => x.UsuarioId).Returns(10);
        var sut = new UsuarioService(users.Object, Mock.Of<IRolRepository>(),
            Mock.Of<IEmpresaRepository>(), Mock.Of<IAuditoriaService>(), actor.Object);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => sut.GetEmpresasAsync(41));
        Assert.Contains("no existe", error.Message, StringComparison.OrdinalIgnoreCase);
        users.Verify(r => r.GetEmpresasAsync(It.IsAny<int>()), Times.Never);
    }
}
