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
/// I8-008.D: una matriz de permisos empresarial no puede recibir autoridad
/// Plataforma aunque un cliente falsifique la solicitud. Prueba sin HTTP,
/// JWT real ni MySQL: usa el servicio de aplicación y repositorios simulados.
/// </summary>
public sealed class I8008DPrivilegioPlataformaMatrizEmpresaTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MatrizEmpresa_RechazaGrantDePlataformaSinEscriturasNiAuditoria(bool esSistema)
    {
        var rolEmpresaA = new Rol
        {
            Id = 7,
            Nombre = "Operador A",
            NombreNormalizado = "I8008D_OPERADOR_A",
            EmpresaId = 11,
            Ambito = AmbitoAutorizacion.Empresa,
            EsSistema = false,
            Activo = true
        };
        var permisoPlataforma = new Permiso
        {
            Id = 71,
            Codigo = "USUARIOS.ASIGNARROL.PLATAFORMA",
            Modulo = ModuloSistema.Usuarios,
            Accion = AccionPermiso.AsignarRol,
            Ambito = AmbitoAutorizacion.Plataforma,
            EsSistema = esSistema,
            Activo = true
        };

        var roles = new Mock<IRolRepository>(MockBehavior.Strict);
        roles.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(rolEmpresaA);

        var permisos = new Mock<IPermisoRepository>(MockBehavior.Strict);
        permisos.Setup(p => p.GetAllAsync(false))
            .ReturnsAsync(new List<Permiso> { permisoPlataforma });
        permisos.Setup(p => p.GetByModuloAccionAsync(
                ModuloSistema.Usuarios, AccionPermiso.AsignarRol))
            .ReturnsAsync(permisoPlataforma);

        var matrices = new Mock<IRolPermisoRepository>(MockBehavior.Strict);
        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        var servicio = new PermisoService(
            matrices.Object, roles.Object, permisos.Object, auditoria.Object,
            Mock.Of<ICurrentUserService>(), Mock.Of<IUsuarioScopeService>());

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            servicio.UpdateMatrizAsync(7, new UpdatePermisoMatrizDto
            {
                Permisos = new List<PermisoMatrizItemDto>
                {
                    new()
                    {
                        Modulo = "Usuarios",
                        Accion = "AsignarRol",
                        Permitido = true
                    }
                }
            }));

        permisos.Verify(p => p.GetByModuloAccionAsync(
            ModuloSistema.Usuarios, AccionPermiso.AsignarRol), Times.Once);
        matrices.Verify(m => m.ReemplazarMatrizPorRolIdAsync(
            It.IsAny<int>(), It.IsAny<List<RolPermiso>>()), Times.Never);
        matrices.VerifyNoOtherCalls();
        auditoria.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MatrizEmpresa_SoloMuestraCatalogoEmpresarialSinFiltrarPorNombre()
    {
        var rolEmpresaA = new Rol
        {
            Id = 7,
            Nombre = "Operador A",
            EmpresaId = 11,
            Ambito = AmbitoAutorizacion.Empresa,
            Activo = true
        };
        var permisoPlataforma = new Permiso
        {
            Id = 71,
            Codigo = "USUARIOS.ASIGNARROL.PLATAFORMA",
            Modulo = ModuloSistema.Usuarios,
            Accion = AccionPermiso.AsignarRol,
            Ambito = AmbitoAutorizacion.Plataforma,
            Activo = true
        };
        var permisoEmpresarial = new Permiso
        {
            Id = 72,
            Codigo = "USUARIOS.VER",
            Modulo = ModuloSistema.Usuarios,
            Accion = AccionPermiso.Ver,
            Ambito = AmbitoAutorizacion.Empresa,
            Activo = true
        };

        var roles = new Mock<IRolRepository>(MockBehavior.Strict);
        roles.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(rolEmpresaA);
        var matrices = new Mock<IRolPermisoRepository>(MockBehavior.Strict);
        matrices.Setup(m => m.GetByRolIdAsync(7))
            .ReturnsAsync(new List<RolPermiso>());
        var permisos = new Mock<IPermisoRepository>(MockBehavior.Strict);
        permisos.Setup(p => p.GetAllAsync(false))
            .ReturnsAsync(new List<Permiso> { permisoPlataforma, permisoEmpresarial });
        var servicio = new PermisoService(
            matrices.Object, roles.Object, permisos.Object,
            Mock.Of<IAuditoriaService>(),
            Mock.Of<ICurrentUserService>(), Mock.Of<IUsuarioScopeService>());

        var visibles = await servicio.GetMatrizAsync(7);

        var unico = Assert.Single(visibles);
        Assert.Equal("Usuarios", unico.Modulo);
        Assert.Equal("Ver", unico.Accion);
        Assert.False(unico.Permitido);
        matrices.Verify(m => m.GetByRolIdAsync(7), Times.Once);
        matrices.Verify(m => m.ReemplazarMatrizPorRolIdAsync(
            It.IsAny<int>(), It.IsAny<List<RolPermiso>>()), Times.Never);
    }
}
