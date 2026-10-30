using Moq;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P1-001.B: prueba dirigida de la frontera al ASIGNAR roles empresariales.
/// Mocks de repositorio, sin acceso a datos DEV/Aiven ni sustitucion del gate HTTP/JWT real.
/// </summary>
public sealed class P1001BRoleAssignmentCertificationTests
{
    [Theory]
    [InlineData(22, AmbitoAutorizacion.Empresa, false, true, false)] // rol de B
    [InlineData(null, AmbitoAutorizacion.Plataforma, true, true, false)] // plataforma
    [InlineData(null, AmbitoAutorizacion.Empresa, false, true, false)] // huerfano
    [InlineData(11, AmbitoAutorizacion.Empresa, false, false, false)] // inactivo
    [InlineData(11, AmbitoAutorizacion.Empresa, false, true, true)] // eliminado
    public async Task TenantA_RechazaRolNoAsignable_SinMutarMembresiaNiPersistir(
        int? propietario, AmbitoAutorizacion ambito, bool esSistema, bool activo, bool eliminado)
    {
        var rol = new Rol
        {
            Id = 4,
            Nombre = "ROL NO ASIGNABLE",
            NombreNormalizado = "rol_no_asignable",
            EmpresaId = propietario,
            Ambito = ambito,
            EsSistema = esSistema,
            Activo = activo,
            Eliminado = eliminado
        };
        var (servicio, usuarios) = CrearFixture(rol);
        var membresiaOriginal = new UsuarioEmpresa(30, 11, 3);
        usuarios.Setup(x => x.GetEmpresaAsync(30, 11)).ReturnsAsync(membresiaOriginal);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            servicio.AsignarEmpresaAsync(30, new AsignarUsuarioEmpresaDto
            {
                EmpresaId = 11, RolId = 4
            }));
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            servicio.CambiarRolEmpresaAsync(30, 11, 4));

        Assert.Equal(3, membresiaOriginal.RolId);
        Assert.Equal(11, membresiaOriginal.EmpresaId);
        usuarios.Verify(x => x.AddEmpresaAsync(It.IsAny<UsuarioEmpresa>()), Times.Never);
        usuarios.Verify(x => x.UpdateEmpresa(It.IsAny<UsuarioEmpresa>()), Times.Never);
        usuarios.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RolPersonalizadoDeA_SiPermiteAltaYActualizacionExclusivasDeA()
    {
        var rolA = new Rol
        {
            Id = 4,
            Nombre = "ROL PROPIO A",
            NombreNormalizado = "rol_propio_a",
            EmpresaId = 11,
            Ambito = AmbitoAutorizacion.Empresa,
            Activo = true
        };
        var (servicio, usuarios) = CrearFixture(rolA);
        UsuarioEmpresa? membresia = null;
        usuarios.Setup(x => x.GetEmpresaAsync(30, 11))
            .ReturnsAsync(() => membresia);
        usuarios.Setup(x => x.AddEmpresaAsync(It.IsAny<UsuarioEmpresa>()))
            .Callback<UsuarioEmpresa>(x => membresia = x)
            .Returns(Task.CompletedTask);
        usuarios.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var asignada = await servicio.AsignarEmpresaAsync(30, new AsignarUsuarioEmpresaDto
        {
            EmpresaId = 11, RolId = 4
        });
        Assert.NotNull(membresia);
        Assert.Equal(11, asignada.EmpresaId);
        Assert.Equal(4, asignada.RolId);
        Assert.Equal(11, membresia!.EmpresaId);

        membresia.CambiarRol(3);
        var actualizada = await servicio.CambiarRolEmpresaAsync(30, 11, 4);
        Assert.Equal(11, actualizada.EmpresaId);
        Assert.Equal(4, actualizada.RolId);
        Assert.Equal(4, membresia.RolId);

        usuarios.Verify(x => x.AddEmpresaAsync(It.IsAny<UsuarioEmpresa>()), Times.Once);
        usuarios.Verify(x => x.UpdateEmpresa(membresia), Times.Once);
        usuarios.Verify(x => x.SaveChangesAsync(), Times.Exactly(2));
    }

    private static (UsuarioService Servicio, Mock<IUsuarioRepository> Usuarios) CrearFixture(Rol rol)
    {
        var usuarios = new Mock<IUsuarioRepository>();
        usuarios.Setup(x => x.GetByIdAsync(30)).ReturnsAsync(new Usuario
        {
            Id = 30,
            NombreUsuario = "p1001b-usuario-a",
            NombreCompleto = "Usuario A de prueba",
            Activo = true,
            Bloqueado = false
        });
        var roles = new Mock<IRolRepository>();
        roles.Setup(x => x.GetByIdAsync(4)).ReturnsAsync(rol);
        var empresas = new Mock<IEmpresaRepository>();
        empresas.Setup(x => x.GetByIdAsync(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Empresa("Empresa A de prueba") { Id = 11 });
        var actual = new Mock<ICurrentUserService>();
        actual.SetupGet(x => x.UsuarioId).Returns(99);
        return (new UsuarioService(
            usuarios.Object,
            roles.Object,
            empresas.Object,
            Mock.Of<IAuditoriaService>(),
            actual.Object), usuarios);
    }
}
