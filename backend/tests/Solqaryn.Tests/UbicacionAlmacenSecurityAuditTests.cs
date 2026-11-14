using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class UbicacionAlmacenSecurityTests
{
    [Fact]
    public void Controller_ExigeAutenticacion_Y_NoExponeAllowAnonymous()
    {
        var tipo = typeof(UbicacionesAlmacenController);

        Assert.NotNull(tipo.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Null(tipo.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.Equal("ubicaciones-almacen", tipo.GetCustomAttribute<RouteAttribute>()?.Template);

        foreach (var metodo in MetodosPublicosDeAccion(tipo))
            Assert.Null(metodo.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Theory]
    [InlineData(nameof(UbicacionesAlmacenController.Buscar), AccionPermiso.Ver)]
    [InlineData(nameof(UbicacionesAlmacenController.GetActivas), AccionPermiso.Ver)]
    [InlineData(nameof(UbicacionesAlmacenController.GetTipos), AccionPermiso.Ver)]
    [InlineData(nameof(UbicacionesAlmacenController.GetById), AccionPermiso.Ver)]
    [InlineData(nameof(UbicacionesAlmacenController.Create), AccionPermiso.Crear)]
    [InlineData(nameof(UbicacionesAlmacenController.Update), AccionPermiso.Editar)]
    [InlineData(nameof(UbicacionesAlmacenController.Activar), AccionPermiso.Activar)]
    [InlineData(nameof(UbicacionesAlmacenController.Desactivar), AccionPermiso.Desactivar)]
    [InlineData(nameof(UbicacionesAlmacenController.Delete), AccionPermiso.EliminarLogico)]
    public void Endpoint_ExigePermisoRelacionalCorrecto(string nombreMetodo, AccionPermiso accionEsperada)
    {
        var metodo = typeof(UbicacionesAlmacenController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Single(m => m.Name == nombreMetodo);
        var atributo = Assert.Single(metodo.GetCustomAttributes<RequierePermisoAttribute>());

        Assert.Equal(ModuloSistema.UbicacionesAlmacen, LeerCampoPrivado<ModuloSistema>(atributo, "_modulo"));
        Assert.Equal(accionEsperada, LeerCampoPrivado<AccionPermiso>(atributo, "_accion"));
    }

    private static IEnumerable<MethodInfo> MetodosPublicosDeAccion(Type tipo) =>
        tipo.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any());

    private static T LeerCampoPrivado<T>(object instancia, string nombre)
    {
        var campo = instancia.GetType().GetField(nombre, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(campo);
        return Assert.IsType<T>(campo!.GetValue(instancia));
    }
}

public class UbicacionAlmacenAuditoriaTests
{
    private readonly Mock<IUbicacionAlmacenRepository> _repo = new();
    private readonly Mock<IAlmacenRepository> _almacenes = new();
    private readonly Mock<ICurrentUserService> _usuario = new();
    private readonly Mock<IAuditoriaService> _auditoria = new();
    private readonly UbicacionAlmacenService _service;

    public UbicacionAlmacenAuditoriaTests()
    {
        _usuario.SetupGet(x => x.UsuarioId).Returns(17);
        _usuario.SetupGet(x => x.NombreUsuario).Returns("auditor-ubicaciones");
        _service = new UbicacionAlmacenService(_repo.Object, _almacenes.Object, _usuario.Object, _auditoria.Object);
    }

    [Fact]
    public async Task CreateAsync_RegistraAuditoriaCrear_ConEntidadYReferencia()
    {
        var almacen = CrearAlmacen(10);
        _almacenes.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(almacen);
        _repo.Setup(x => x.ExisteCodigoAsync(10, "R-01", null)).ReturnsAsync(false);
        _repo.Setup(x => x.AddAsync(It.IsAny<UbicacionAlmacen>()))
            .Callback<UbicacionAlmacen>(x => x.Id = 44)
            .Returns(Task.CompletedTask);
        _repo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        await _service.CreateAsync(new CreateUbicacionAlmacenDto
        {
            AlmacenId = 10,
            Codigo = "r-01",
            Nombre = "Rack 01",
            Tipo = "Rack"
        });

        VerificarAuditoria(AccionPermiso.Crear, 44, "Ubicación creada:");
    }

    [Fact]
    public async Task UpdateAsync_RegistraAuditoriaEditar_ConEntidadYReferencia()
    {
        var actual = CrearUbicacion(44, 10, activa: true);
        _repo.Setup(x => x.GetByIdAsync(44)).ReturnsAsync(actual);
        _repo.Setup(x => x.ExisteCodigoAsync(10, "R-02", 44)).ReturnsAsync(false);
        _repo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        await _service.UpdateAsync(44, new UpdateUbicacionAlmacenDto
        {
            AlmacenId = 10,
            Codigo = "r-02",
            Nombre = "Rack 02",
            Tipo = "Rack"
        });

        VerificarAuditoria(AccionPermiso.Editar, 44, "Ubicación actualizada:");
    }

    [Theory]
    [InlineData(false, true, AccionPermiso.Activar)]
    [InlineData(true, false, AccionPermiso.Desactivar)]
    public async Task CambiarEstadoAsync_RegistraAccionDeAuditoriaCorrecta(
        bool estadoInicial,
        bool estadoFinal,
        AccionPermiso accionEsperada)
    {
        var actual = CrearUbicacion(44, 10, estadoInicial);
        _repo.Setup(x => x.GetByIdAsync(44)).ReturnsAsync(actual);
        _repo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        _repo.Setup(x => x.TieneHijasActivasAsync(44)).ReturnsAsync(false);
        if (estadoFinal)
            _almacenes.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(actual.Almacen);

        await _service.CambiarEstadoAsync(44, estadoFinal);

        VerificarAuditoria(accionEsperada, 44, estadoFinal ? "Ubicación activada:" : "Ubicación desactivada:");
    }

    [Fact]
    public async Task DeleteAsync_RegistraAuditoriaEliminarLogico_SoloTrasPersistir()
    {
        var actual = CrearUbicacion(44, 10, activa: true);
        _repo.Setup(x => x.GetByIdAsync(44)).ReturnsAsync(actual);
        _repo.Setup(x => x.TieneHijasNoEliminadasAsync(44)).ReturnsAsync(false);
        _repo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var eliminado = await _service.DeleteAsync(44);

        Assert.True(eliminado);
        Assert.True(actual.Eliminado);
        Assert.Equal(17, actual.EliminadoPorUsuarioId);
        VerificarAuditoria(AccionPermiso.EliminarLogico, 44, "Ubicación eliminada lógicamente:");
    }

    [Fact]
    public async Task DeleteAsync_SiPersistenciaNoConfirma_NoEmiteAuditoriaDeExito()
    {
        var actual = CrearUbicacion(44, 10, activa: true);
        _repo.Setup(x => x.GetByIdAsync(44)).ReturnsAsync(actual);
        _repo.Setup(x => x.TieneHijasNoEliminadasAsync(44)).ReturnsAsync(false);
        _repo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(false);

        var eliminado = await _service.DeleteAsync(44);

        Assert.False(eliminado);
        _auditoria.VerifyNoOtherCalls();
    }

    private void VerificarAuditoria(AccionPermiso accion, int referenciaId, string prefijoDescripcion)
    {
        _auditoria.Verify(x => x.RegistrarAsync(
            ModuloSistema.UbicacionesAlmacen,
            accion,
            It.Is<string>(d => d.StartsWith(prefijoDescripcion, StringComparison.Ordinal)),
            referenciaId,
            "UbicacionAlmacen",
            It.IsAny<object?>(),
            It.IsAny<object?>(),
            It.IsAny<string?>(),
            "Exito",
            It.IsAny<string?>()), Times.Once);
    }

    private static Almacen CrearAlmacen(int id) => new()
    {
        Id = id,
        SucursalId = 100 + id,
        Sucursal = new Sucursal
        {
            Id = 100 + id,
            Codigo = $"S-{id}",
            Nombre = $"Sucursal {id}",
            ZonaHoraria = "America/Tegucigalpa",
            Activa = true
        },
        Codigo = $"A-{id}",
        Nombre = $"Almacén {id}",
        Tipo = TipoAlmacen.Bodega,
        Activo = true
    };

    private static UbicacionAlmacen CrearUbicacion(int id, int almacenId, bool activa) => new()
    {
        Id = id,
        AlmacenId = almacenId,
        Almacen = CrearAlmacen(almacenId),
        Codigo = $"U-{id}",
        Nombre = $"Ubicación {id}",
        Tipo = TipoUbicacionAlmacen.Rack,
        Activa = activa
    };
}
