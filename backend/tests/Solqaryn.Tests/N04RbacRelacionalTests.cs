using System.ComponentModel.DataAnnotations.Schema;
using Solqaryn.Application.Common;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class N04RbacRelacionalTests
{
    private static PermisoService CrearServicio(
        Mock<IRolPermisoRepository> repositorio,
        Mock<IUsuarioScopeService> scope)
        => new(
            repositorio.Object,
            new Mock<IRolRepository>().Object,
            new Mock<IPermisoRepository>().Object,
            new Mock<IAuditoriaService>().Object,
            new Mock<ICurrentUserService>().Object,
            scope.Object);

    [Fact]
    public async Task TienePermiso_SinScope_FallaCerrado()
    {
        var repo = new Mock<IRolPermisoRepository>(MockBehavior.Strict);
        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(x => x.ObtenerActualAsync()).ReturnsAsync((UsuarioScopeActual?)null);

        var service = CrearServicio(repo, scope);

        Assert.False(await service.TienePermisoAsync(ModuloSistema.Productos, AccionPermiso.Ver));
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TienePermiso_AdministradorSinGrant_NoUsaBypass()
    {
        const int rolId = 7401;
        var repo = new Mock<IRolPermisoRepository>();
        repo.Setup(x => x.TienePermisoPorRolIdAsync(rolId, ModuloSistema.Productos, AccionPermiso.Editar))
            .ReturnsAsync(false);
        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(x => x.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(1, rolId, "Administrador", true));

        var service = CrearServicio(repo, scope);

        Assert.False(await service.TienePermisoAsync(ModuloSistema.Productos, AccionPermiso.Editar));
        repo.Verify(x => x.TienePermisoPorRolIdAsync(rolId, ModuloSistema.Productos, AccionPermiso.Editar), Times.Once);
    }

    [Fact]
    public async Task TienePermiso_ConGrantExplicito_Autoriza()
    {
        const int rolId = 7402;
        var repo = new Mock<IRolPermisoRepository>();
        repo.Setup(x => x.TienePermisoPorRolIdAsync(rolId, ModuloSistema.CargasMasivas, AccionPermiso.Importar))
            .ReturnsAsync(true);
        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(x => x.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(2, rolId, "Operador", false));

        var service = CrearServicio(repo, scope);

        Assert.True(await service.TienePermisoAsync(ModuloSistema.CargasMasivas, AccionPermiso.Importar));
    }

    [Fact]
    public void RolPermiso_NoExponeCamposPersistentesLegacy()
    {
        var propiedades = typeof(RolPermiso).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);

        Assert.Contains(nameof(RolPermiso.RolId), propiedades);
        Assert.Contains(nameof(RolPermiso.PermisoId), propiedades);
        Assert.DoesNotContain("Rol", propiedades);
        Assert.DoesNotContain("Modulo", propiedades);
        Assert.DoesNotContain("Accion", propiedades);
        Assert.DoesNotContain("Permitido", propiedades);
    }

    [Fact]
    public void Usuario_RolLegacy_EstaMarcadoNotMapped_YRolIdEsAutoridad()
    {
        var rolLegacy = typeof(Usuario).GetProperty(nameof(Usuario.Rol));
        var rolId = typeof(Usuario).GetProperty(nameof(Usuario.RolId));

        Assert.NotNull(rolLegacy);
        Assert.NotNull(rolId);
        Assert.NotNull(rolLegacy!.GetCustomAttributes(typeof(NotMappedAttribute), inherit: true).SingleOrDefault());
        Assert.Equal(typeof(int), rolId!.PropertyType);
        Assert.Equal("Usuario.RolId -> Rol -> RolPermiso.PermisoId -> Permiso", RbacN04Authority.Modelo);
    }

    [Fact]
    public void CatalogoRbac_IncluyeImportarCerrarYReabrir()
    {
        Assert.Contains(AccionPermiso.Importar, CatalogoPermisosBase.AccionesRbacRequeridas);
        Assert.Contains(AccionPermiso.Cerrar, CatalogoPermisosBase.AccionesRbacRequeridas);
        Assert.Contains(AccionPermiso.Reabrir, CatalogoPermisosBase.AccionesRbacRequeridas);

        var cargasMasivas = CatalogoPermisosBase.Definicion.Single(x => x.Modulo == ModuloSistema.CargasMasivas).Acciones;
        Assert.Contains(AccionPermiso.Importar, cargasMasivas);

        foreach (var modulo in new[] { ModuloSistema.Compras, ModuloSistema.Ventas, ModuloSistema.Finanzas })
        {
            var acciones = CatalogoPermisosBase.Definicion.Single(x => x.Modulo == modulo).Acciones;
            Assert.Contains(AccionPermiso.Cerrar, acciones);
            Assert.Contains(AccionPermiso.Reabrir, acciones);
        }
    }
}
