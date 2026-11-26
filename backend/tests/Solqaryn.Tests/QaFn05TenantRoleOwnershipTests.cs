using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class QaFn05TenantRoleOwnershipTests
{
    [Theory]
    [InlineData(AmbitoAutorizacion.Empresa, null, true)]
    [InlineData(AmbitoAutorizacion.Empresa, 11, true)]
    [InlineData(AmbitoAutorizacion.Empresa, 12, false)]
    [InlineData(AmbitoAutorizacion.Plataforma, null, false)]
    public async Task ResolverContexto_ExigeAmbitoYPropiedadDelRol(
        AmbitoAutorizacion ambito, int? propietario, bool permitido)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"qa-fn05-role-{Guid.NewGuid():N}")
            .Options;
        await using var db = new AppDbContext(options);
        db.Set<Empresa>().AddRange(new Empresa("Empresa A") { Id = 11 },
            new Empresa("Empresa B") { Id = 12 });
        db.Roles.Add(new Rol
        {
            Id = 3, Nombre = "Rol de prueba", NombreNormalizado = "ROL DE PRUEBA",
            Ambito = ambito, EmpresaId = propietario, Activo = true
        });
        db.Usuarios.Add(new Usuario
        {
            Id = 7, NombreUsuario = "qa_role_user", NombreCompleto = "Usuario de prueba",
            PasswordHash = "fixture-unused", RolId = 3, Activo = true
        });
        db.UsuarioEmpresas.Add(new UsuarioEmpresa(7, 11, 3));
        await db.SaveChangesAsync();
        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.EstaAutenticado).Returns(true);
        current.SetupGet(x => x.UsuarioId).Returns(7);
        var service = new UsuarioScopeService(db, current.Object);
        var direct = await service.ObtenerActualAsync(11);
        var unique = await service.ObtenerUnicoActualAsync();
        if (permitido)
        {
            Assert.NotNull(direct);
            Assert.Equal(11, direct!.EmpresaId);
            Assert.Equal(3, direct.RolId);
            Assert.Equal(direct, unique);
        }
        else
        {
            Assert.Null(direct);
            Assert.Null(unique);
        }
    }

    [Theory]
    [InlineData(AmbitoAutorizacion.Empresa, 12)]
    [InlineData(AmbitoAutorizacion.Plataforma, null)]
    public async Task AsignarOCambiarRol_RechazaAutoridadAjenaSinPersistir(
        AmbitoAutorizacion ambito, int? propietario)
    {
        var usuarios = new Mock<IUsuarioRepository>();
        usuarios.Setup(x => x.GetByIdAsync(7)).ReturnsAsync(new Usuario { Id = 7, Activo = true });
        var roles = new Mock<IRolRepository>();
        roles.Setup(x => x.GetByIdAsync(3)).ReturnsAsync(new Rol
        {
            Id = 3, Nombre = "Rol ajeno", Activo = true, Ambito = ambito, EmpresaId = propietario
        });
        var empresas = new Mock<IEmpresaRepository>();
        empresas.Setup(x => x.GetByIdAsync(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Empresa("Empresa A") { Id = 11 });
        var service = new UsuarioService(usuarios.Object, roles.Object, empresas.Object,
            Mock.Of<IAuditoriaService>(), Mock.Of<ICurrentUserService>());

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            service.AsignarEmpresaAsync(7, new AsignarUsuarioEmpresaDto { EmpresaId = 11, RolId = 3 }));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            service.CambiarRolEmpresaAsync(7, 11, 3));
        usuarios.Verify(x => x.AddEmpresaAsync(It.IsAny<UsuarioEmpresa>()), Times.Never);
        usuarios.Verify(x => x.UpdateEmpresa(It.IsAny<UsuarioEmpresa>()), Times.Never);
        usuarios.Verify(x => x.SaveChangesAsync(), Times.Never);
    }
}
