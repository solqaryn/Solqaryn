using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests.Application.Services;

public class UsuarioEmpresaAuditContractTests
{
    [Fact]
    public async Task CambiarEstadoEmpresa_DebePersistirActorYRegistrarAuditoriaUsuarioEmpresa()
    {
        var repository = new Mock<IUsuarioRepository>();
        var rolRepository = new Mock<IRolRepository>();
        var empresaRepository = new Mock<IEmpresaRepository>();
        var auditoria = new Mock<IAuditoriaService>();
        var currentUser = new Mock<ICurrentUserService>();

        var usuario = new Usuario
        {
            Id = 7,
            NombreUsuario = "usuario-empresa",
            NombreCompleto = "Usuario Empresa",
            Activo = true,
            Bloqueado = false
        };
        var membresia = new UsuarioEmpresa(7, 11, 3);

        repository.Setup(repo => repo.GetByIdAsync(7)).ReturnsAsync(usuario);
        repository.Setup(repo => repo.GetEmpresaAsync(7, 11)).ReturnsAsync(membresia);
        repository.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(true);
        currentUser.SetupGet(user => user.UsuarioId).Returns(99);

        var service = new UsuarioService(
            repository.Object,
            rolRepository.Object,
            empresaRepository.Object,
            auditoria.Object,
            currentUser.Object);

        var result = await service.CambiarEstadoEmpresaAsync(7, 11, false);

        Assert.False(result.Activa);
        Assert.Equal(99, membresia.ActualizadoPorUsuarioId);
        repository.Verify(repo => repo.UpdateEmpresa(membresia), Times.Once);
        repository.Verify(repo => repo.SaveChangesAsync(), Times.Once);

        var auditCall = Assert.Single(auditoria.Invocations.Where(
            invocation => invocation.Method.Name == nameof(IAuditoriaService.RegistrarAsync)));
        Assert.Equal(ModuloSistema.Usuarios, auditCall.Arguments[0]);
        Assert.Equal(AccionPermiso.Desactivar, auditCall.Arguments[1]);
        Assert.Contains("membresía empresarial", Assert.IsType<string>(auditCall.Arguments[2]), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(7, auditCall.Arguments[3]);
        Assert.Equal("UsuarioEmpresa", auditCall.Arguments[4]);
    }
}
