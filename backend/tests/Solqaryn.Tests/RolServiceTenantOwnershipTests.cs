using Moq;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public sealed class RolServiceTenantOwnershipTests
{
    private static (RolService Service, Mock<IRolRepository> Repository, List<Rol> Added)
        Create(Rol? origen = null)
    {
        var added = new List<Rol>();
        var roles = new Mock<IRolRepository>();
        roles.Setup(x => x.ExisteNombreAsync(It.IsAny<string>(), It.IsAny<int?>()))
            .ReturnsAsync(false);
        roles.Setup(x => x.AddAsync(It.IsAny<Rol>()))
            .Callback<Rol>(added.Add)
            .Returns(Task.CompletedTask);
        roles.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        if (origen is not null)
            roles.Setup(x => x.GetByIdAsync(origen.Id)).ReturnsAsync(origen);
        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.UsuarioId).Returns(99);
        return (new RolService(roles.Object, Mock.Of<IAuditoriaService>(),
            current.Object), roles, added);
    }

    [Fact]
    public async Task CrearRolPersonalizado_UsaExclusivamenteEmpresaAutorizada()
    {
        var (sut, _, added) = Create();
        var creado = await sut.CreateAsync(new CrearRolDto
        {
            Nombre = "Inventarios A",
            Descripcion = "Rol privado A"
        }, 11);

        var rol = Assert.Single(added);
        Assert.Equal(11, rol.EmpresaId);
        Assert.Equal(11, creado.EmpresaId);
        Assert.Equal(AmbitoAutorizacion.Empresa, rol.Ambito);
        Assert.False(rol.EsSistema);
    }

    [Fact]
    public async Task CrearSinEmpresaVerificada_FallaAntesDeEscribir()
    {
        var (sut, repo, added) = Create();
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            sut.CreateAsync(new CrearRolDto { Nombre = "Sin tenant" }, 0));
        Assert.Empty(added);
        repo.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Theory]
    [InlineData(22, AmbitoAutorizacion.Empresa, false)]
    [InlineData(null, AmbitoAutorizacion.Plataforma, true)]
    [InlineData(null, AmbitoAutorizacion.Empresa, false)]
    public async Task DuplicarRolAjenoOPlataformaOHuerfano_SeRechazaAntesDeEscribir(
        int? owner, AmbitoAutorizacion ambito, bool sistema)
    {
        var original = new Rol
        {
            Id = 7, Nombre = "Rol externo", NombreNormalizado = "EXTERNO",
            EmpresaId = owner, Ambito = ambito, EsSistema = sistema, Activo = true
        };
        var (sut, repo, added) = Create(original);
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.DuplicarAsync(7, "No autorizado", 11));
        Assert.Empty(added);
        repo.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DuplicarPlantillaSistema_ProduceCopiaPropiaDeA()
    {
        var plantilla = new Rol
        {
            Id = 7, Nombre = "Plantilla", NombreNormalizado = "PLANTILLA",
            EsSistema = true, EmpresaId = null, Ambito = AmbitoAutorizacion.Empresa
        };
        var (sut, _, added) = Create(plantilla);
        var copia = await sut.DuplicarAsync(7, "Mi copia A", 11);
        Assert.Equal(11, Assert.Single(added).EmpresaId);
        Assert.False(added[0].EsSistema);
        Assert.Equal(11, copia.EmpresaId);
    }

    [Fact]
    public async Task NoModificarPlantillaCompartidaDesdeTenant()
    {
        var plantilla = new Rol
        {
            Id = 7, Nombre = "Plantilla", NombreNormalizado = "PLANTILLA",
            EsSistema = true, EmpresaId = null, Ambito = AmbitoAutorizacion.Empresa
        };
        var (sut, repo, _) = Create(plantilla);
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.UpdateAsync(7, new ActualizarRolDto { Nombre = "Alterado" }));
        await Assert.ThrowsAsync<BusinessRuleException>(() => sut.ActivarAsync(7));
        await Assert.ThrowsAsync<BusinessRuleException>(() => sut.DesactivarAsync(7));
        await Assert.ThrowsAsync<BusinessRuleException>(() => sut.EliminarLogicoAsync(7));
        await Assert.ThrowsAsync<BusinessRuleException>(() => sut.EliminarPermanenteAsync(7));
        repo.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Theory]
    [InlineData(null, AmbitoAutorizacion.Empresa, true)]
    [InlineData(null, AmbitoAutorizacion.Empresa, false)]
    [InlineData(null, AmbitoAutorizacion.Plataforma, true)]
    public async Task MatrizDePlantillaHuerfanoOPlataforma_NoSePuedeMutar(
        int? owner, AmbitoAutorizacion ambito, bool sistema)
    {
        var role = new Rol
        {
            Id = 7, Nombre = "Rol no privado", NombreNormalizado = "NO_PRIVADO",
            EmpresaId = owner, Ambito = ambito, EsSistema = sistema, Activo = true
        };
        var roles = new Mock<IRolRepository>();
        roles.Setup(x => x.GetByIdAsync(7)).ReturnsAsync(role);
        var rp = new Mock<IRolPermisoRepository>();
        var service = new PermisoService(rp.Object, roles.Object,
            Mock.Of<IPermisoRepository>(), Mock.Of<IAuditoriaService>(),
            Mock.Of<ICurrentUserService>(), Mock.Of<IUsuarioScopeService>());

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.UpdateMatrizAsync(7, new UpdatePermisoMatrizDto
            {
                Permisos = new List<PermisoMatrizItemDto>()
            }));
        rp.Verify(x => x.ReemplazarMatrizPorRolIdAsync(
            It.IsAny<int>(), It.IsAny<List<RolPermiso>>()), Times.Never);
    }
}
