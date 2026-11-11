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
/// I8-008.E: la autoridad Plataforma no se incorpora parcialmente a una matriz
/// empresarial, ni siquiera si el rol empresarial es administrador.
/// Prueba del servicio de aplicación con repositorios simulados; no HTTP/JWT/MySQL.
/// </summary>
public sealed class I8008EAutoridadGlobalLoteAtomicoTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task MatrizEmpresarial_MezclaPermisoGlobal_RechazaLoteSinGrantsNiAuditoria(
        bool esAdministrador, bool plataformaPrimero)
    {
        var rol = new Rol
        {
            Id = 78,
            Nombre = "Operador privado A",
            NombreNormalizado = "I8008E_EMPRESA_A",
            EmpresaId = 11,
            Ambito = AmbitoAutorizacion.Empresa,
            EsSistema = false,
            EsAdministrador = esAdministrador,
            Activo = true
        };
        var permisoEmpresa = new Permiso
        {
            Id = 781,
            Codigo = "USUARIOS.VER",
            Modulo = ModuloSistema.Usuarios,
            Accion = AccionPermiso.Ver,
            Ambito = AmbitoAutorizacion.Empresa,
            Activo = true
        };
        var permisoPlataforma = new Permiso
        {
            Id = 782,
            Codigo = "USUARIOS.ASIGNARROL.PLATAFORMA",
            Modulo = ModuloSistema.Usuarios,
            Accion = AccionPermiso.AsignarRol,
            Ambito = AmbitoAutorizacion.Plataforma,
            Activo = true,
            EsSistema = true
        };

        var roles = new Mock<IRolRepository>(MockBehavior.Strict);
        roles.Setup(r => r.GetByIdAsync(78)).ReturnsAsync(rol);

        var permisos = new Mock<IPermisoRepository>(MockBehavior.Strict);
        permisos.Setup(p => p.GetAllAsync(false))
            .ReturnsAsync(new List<Permiso> { permisoEmpresa, permisoPlataforma });
        permisos.Setup(p => p.GetByModuloAccionAsync(
                ModuloSistema.Usuarios, AccionPermiso.Ver))
            .ReturnsAsync(permisoEmpresa);
        permisos.Setup(p => p.GetByModuloAccionAsync(
                ModuloSistema.Usuarios, AccionPermiso.AsignarRol))
            .ReturnsAsync(permisoPlataforma);

        var matriz = new Mock<IRolPermisoRepository>(MockBehavior.Strict);
        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        var servicio = new PermisoService(
            matriz.Object, roles.Object, permisos.Object, auditoria.Object,
            Mock.Of<ICurrentUserService>(), Mock.Of<IUsuarioScopeService>());

        var empresarial = new PermisoMatrizItemDto
        {
            Modulo = "Usuarios",
            Accion = "Ver",
            Permitido = true
        };
        var global = new PermisoMatrizItemDto
        {
            Modulo = "Usuarios",
            Accion = "AsignarRol",
            Permitido = true
        };
        var items = plataformaPrimero
            ? new List<PermisoMatrizItemDto> { global, empresarial }
            : new List<PermisoMatrizItemDto> { empresarial, global };

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            servicio.UpdateMatrizAsync(78, new UpdatePermisoMatrizDto
            {
                Permisos = items
            }));

        // Validar todo el lote antes de reemplazar evita grants parciales,
        // incluso cuando el usuario solicita primero el permiso empresarial.
        matriz.Verify(m => m.ReemplazarMatrizPorRolIdAsync(
            It.IsAny<int>(), It.IsAny<List<RolPermiso>>()), Times.Never);
        matriz.VerifyNoOtherCalls();
        auditoria.VerifyNoOtherCalls();
        Assert.Equal(AmbitoAutorizacion.Empresa, rol.Ambito);
        Assert.Equal(11, rol.EmpresaId);
    }
}
