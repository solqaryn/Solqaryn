using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-008.G: una identidad con rol global no recibe autorizacion empresarial
/// por defecto cuando no tiene membresia tenant verificada.
/// Prueba dirigida con dobles sinteticos; no ejecuta middleware, HTTP, JWT o MySQL.
/// </summary>
public sealed class I8008GGlobalSinMembresiaNoEnumeraPermisosTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GlobalSinMembresiaB_NoEnumeraRolNiPermisosYDeniegaConsulta(
        bool esAdministradorGlobal)
    {
        const int empresaAjena = 22;
        const int usuarioGlobal = 25;

        // Aunque la identidad posea un rol de plataforma, la fuente de
        // autoridad para la empresa B debe ser el scope tenant verificado.
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        scope.Setup(x => x.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(
                usuarioGlobal, 900, "Plataforma", esAdministradorGlobal));
        scope.Setup(x => x.ObtenerActualAsync(
                empresaAjena, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsuarioTenantScopeActual?)null);

        var matriz = new Mock<IRolPermisoRepository>(MockBehavior.Strict);
        var servicio = new PermisoService(
            matriz.Object,
            Mock.Of<IRolRepository>(),
            Mock.Of<IPermisoRepository>(),
            Mock.Of<IAuditoriaService>(),
            Mock.Of<ICurrentUserService>(),
            scope.Object);

        var permisos = await servicio.GetMisPermisosAsync(empresaAjena);
        Assert.NotNull(permisos);
        Assert.Empty(permisos.Permisos);
        Assert.Equal(string.Empty, permisos.Rol);
        Assert.False(permisos.EsAdministrador);

        var serializado = JsonSerializer.Serialize(permisos);
        Assert.DoesNotContain("Plataforma", serializado, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PasswordHash", serializado, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ResetToken", serializado, StringComparison.OrdinalIgnoreCase);

        Assert.False(await servicio.TienePermisoAsync(
            empresaAjena, ModuloSistema.Usuarios, AccionPermiso.Ver));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            servicio.VerificarPermisoAsync(
                empresaAjena, ModuloSistema.Usuarios, AccionPermiso.Ver));

        // Tres rutas de aplicacion realmente ejercitadas: lectura de matriz,
        // verificacion booleana y verificacion que lanza excepcion.
        scope.Verify(x => x.ObtenerActualAsync(), Times.Never);
        scope.Verify(x => x.ObtenerActualAsync(
            empresaAjena, It.IsAny<CancellationToken>()), Times.Exactly(3));
        matriz.VerifyNoOtherCalls();
    }
}
