using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class N04AdministradorSemanticaTests
{
    private static Permiso CrearPermiso(int id, ModuloSistema modulo, AccionPermiso accion) => new()
    {
        Id = id,
        Codigo = $"{modulo}.{accion}".ToUpperInvariant(),
        Nombre = $"{modulo} - {accion}",
        Modulo = modulo,
        Accion = accion,
        Activo = true,
        Eliminado = false
    };

    private static PermisoService CrearServicio(
        Mock<IRolPermisoRepository> matrices,
        Mock<IRolRepository> roles,
        Mock<IPermisoRepository> permisos,
        Mock<IAuditoriaService>? auditoria = null)
        => new(
            matrices.Object,
            roles.Object,
            permisos.Object,
            (auditoria ?? new Mock<IAuditoriaService>()).Object,
            new Mock<ICurrentUserService>().Object,
            new Mock<IUsuarioScopeService>().Object);

    [Fact]
    public async Task UpdateMatriz_Administrador_NoPuedeReducirGrants_YNoMutaRepositorio()
    {
        const int rolId = 9101;
        var ver = CrearPermiso(1, ModuloSistema.Productos, AccionPermiso.Ver);
        var editar = CrearPermiso(2, ModuloSistema.Productos, AccionPermiso.Editar);

        var matrices = new Mock<IRolPermisoRepository>(MockBehavior.Strict);
        var roles = new Mock<IRolRepository>();
        roles.Setup(x => x.GetByIdAsync(rolId)).ReturnsAsync(new Rol
        {
            Id = rolId,
            Nombre = "Administrador",
            NombreNormalizado = "ADMINISTRADOR",
            EsAdministrador = true,
            Activo = true
        });
        var permisos = new Mock<IPermisoRepository>();
        permisos.Setup(x => x.GetAllAsync(false)).ReturnsAsync(new List<Permiso> { ver, editar });
        permisos.Setup(x => x.GetByModuloAccionAsync(ver.Modulo, ver.Accion)).ReturnsAsync(ver);

        var service = CrearServicio(matrices, roles, permisos);
        var solicitudReducida = new UpdatePermisoMatrizDto
        {
            Permisos = new List<PermisoMatrizItemDto>
            {
                new() { Modulo = ver.Modulo.ToString(), Accion = ver.Accion.ToString(), Permitido = true },
                new() { Modulo = editar.Modulo.ToString(), Accion = editar.Accion.ToString(), Permitido = false }
            }
        };

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.UpdateMatrizAsync(rolId, solicitudReducida));

        Assert.Contains("grants explícitos", error.Message, StringComparison.OrdinalIgnoreCase);
        matrices.Verify(x => x.ReemplazarMatrizPorRolIdAsync(
            It.IsAny<int>(), It.IsAny<List<RolPermiso>>()), Times.Never);
        matrices.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateMatriz_RolNormal_PuedeRetirarYAgregarGrants()
    {
        const int rolId = 9102;
        var anterior = CrearPermiso(11, ModuloSistema.Productos, AccionPermiso.Ver);
        var nuevo = CrearPermiso(12, ModuloSistema.Productos, AccionPermiso.Editar);

        var matrices = new Mock<IRolPermisoRepository>();
        matrices.SetupSequence(x => x.GetByRolIdAsync(rolId))
            .ReturnsAsync(new List<RolPermiso> { new() { RolId = rolId, PermisoId = anterior.Id } })
            .ReturnsAsync(new List<RolPermiso> { new() { RolId = rolId, PermisoId = nuevo.Id } });

        var roles = new Mock<IRolRepository>();
        roles.Setup(x => x.GetByIdAsync(rolId)).ReturnsAsync(new Rol
        {
            Id = rolId,
            Nombre = "Operador",
            NombreNormalizado = "OPERADOR",
            EsAdministrador = false,
            Activo = true
        });

        var permisos = new Mock<IPermisoRepository>();
        permisos.Setup(x => x.GetAllAsync(false)).ReturnsAsync(new List<Permiso> { anterior, nuevo });
        permisos.Setup(x => x.GetByModuloAccionAsync(nuevo.Modulo, nuevo.Accion)).ReturnsAsync(nuevo);

        var service = CrearServicio(matrices, roles, permisos);
        var solicitud = new UpdatePermisoMatrizDto
        {
            Permisos = new List<PermisoMatrizItemDto>
            {
                new() { Modulo = anterior.Modulo.ToString(), Accion = anterior.Accion.ToString(), Permitido = false },
                new() { Modulo = nuevo.Modulo.ToString(), Accion = nuevo.Accion.ToString(), Permitido = true }
            }
        };

        var resultado = await service.UpdateMatrizAsync(rolId, solicitud);

        matrices.Verify(x => x.ReemplazarMatrizPorRolIdAsync(
            rolId,
            It.Is<List<RolPermiso>>(filas =>
                filas.Count == 1 && filas[0].RolId == rolId && filas[0].PermisoId == nuevo.Id)),
            Times.Once);
        Assert.Contains(resultado, x => x.Modulo == nuevo.Modulo.ToString() && x.Accion == nuevo.Accion.ToString() && x.Permitido);
        Assert.Contains(resultado, x => x.Modulo == anterior.Modulo.ToString() && x.Accion == anterior.Accion.ToString() && !x.Permitido);
    }
}
