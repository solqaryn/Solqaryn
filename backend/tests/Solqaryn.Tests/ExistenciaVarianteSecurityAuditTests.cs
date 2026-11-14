using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class ExistenciaVarianteSecurityTests
{
    [Fact]
    public void Controller_ExigeAutenticacion_Y_NoExponeAllowAnonymous()
    {
        var tipo = typeof(ExistenciasVarianteController);

        Assert.NotNull(tipo.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Null(tipo.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.Equal("existencias-variante", tipo.GetCustomAttribute<RouteAttribute>()?.Template);

        foreach (var metodo in MetodosPublicosDeAccion(tipo))
            Assert.Null(metodo.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Theory]
    [InlineData(nameof(ExistenciasVarianteController.Buscar), AccionPermiso.Ver)]
    [InlineData(nameof(ExistenciasVarianteController.GetById), AccionPermiso.Ver)]
    [InlineData(nameof(ExistenciasVarianteController.Create), AccionPermiso.Crear)]
    [InlineData(nameof(ExistenciasVarianteController.UpdateConfiguracion), AccionPermiso.Editar)]
    public void Endpoint_ExigePermisoRelacionalCorrecto(string nombreMetodo, AccionPermiso accionEsperada)
    {
        var metodo = typeof(ExistenciasVarianteController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Single(m => m.Name == nombreMetodo);
        var atributo = Assert.Single(metodo.GetCustomAttributes<RequierePermisoAttribute>());

        Assert.Equal(ModuloSistema.Inventario, LeerCampoPrivado<ModuloSistema>(atributo, "_modulo"));
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

public sealed class ExistenciaVarianteAuditoriaTests
{
    private readonly Mock<IExistenciaVarianteRepository> _existencias = new();
    private readonly Mock<IProductoVarianteRepository> _variantes = new();
    private readonly Mock<IAlmacenRepository> _almacenes = new();
    private readonly Mock<IUbicacionAlmacenRepository> _ubicaciones = new();
    private readonly Mock<ICurrentUserService> _usuario = new();
    private readonly Mock<IAuditoriaService> _auditoria = new();
    private readonly ExistenciaVarianteService _service;

    public ExistenciaVarianteAuditoriaTests()
    {
        _usuario.SetupGet(x => x.UsuarioId).Returns(27);
        _usuario.SetupGet(x => x.NombreUsuario).Returns("auditor-existencias");
        _service = new ExistenciaVarianteService(
            _existencias.Object,
            _variantes.Object,
            _almacenes.Object,
            _ubicaciones.Object,
            _usuario.Object,
            _auditoria.Object);
    }

    [Fact]
    public async Task CreateAsync_RegistraProcedenciaYAuditoria_SoloTrasPersistenciaConfirmada()
    {
        var variante = CrearVariante(20);
        var almacen = CrearAlmacen(10);
        _variantes.Setup(x => x.GetByIdAsync(20)).ReturnsAsync(variante);
        _almacenes.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(almacen);
        _existencias.Setup(x => x.ExisteClaveAsync(20, 10, null, null)).ReturnsAsync(false);
        _existencias.Setup(x => x.AddAsync(It.IsAny<ExistenciaVariante>()))
            .Callback<ExistenciaVariante>(x => x.Id = 77)
            .Returns(Task.CompletedTask);
        _existencias.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var resultado = await _service.CreateAsync(new CreateExistenciaVarianteDto
        {
            ProductoVarianteId = 20,
            AlmacenId = 10,
            StockFisico = 12,
            StockReservado = 2,
            StockTransito = 3,
            StockMinimo = 4,
            StockMaximo = 30
        });

        Assert.Equal(77, resultado.Id);
        _existencias.Verify(x => x.AddAsync(It.Is<ExistenciaVariante>(e =>
            e.CreadoPorUsuarioId == 27 &&
            e.CreadoPorNombreUsuario == "auditor-existencias" &&
            e.StockFisico == 12 && e.StockReservado == 2 && e.StockDisponible == 10)), Times.Once);
        VerificarAuditoria(AccionPermiso.Crear, 77, "Existencia creada para variante 20");
    }

    [Fact]
    public async Task UpdateConfiguracionAsync_RegistraProcedenciaYAuditoriaEditar()
    {
        var existencia = CrearExistencia(77, 20, 10);
        var ubicacion = CrearUbicacion(55, 10);
        _existencias.Setup(x => x.GetByIdAsync(77)).ReturnsAsync(existencia);
        _ubicaciones.Setup(x => x.GetByIdAsync(55)).ReturnsAsync(ubicacion);
        _existencias.Setup(x => x.ExisteClaveAsync(20, 10, 55, 77)).ReturnsAsync(false);
        _existencias.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var resultado = await _service.UpdateConfiguracionAsync(77, new UpdateExistenciaVarianteConfiguracionDto
        {
            UbicacionAlmacenId = 55,
            StockMinimo = 5,
            StockMaximo = 25
        });

        Assert.NotNull(resultado);
        Assert.Equal(55, existencia.UbicacionAlmacenId);
        Assert.Equal(27, existencia.ActualizadoPorUsuarioId);
        Assert.Equal("auditor-existencias", existencia.ActualizadoPorNombreUsuario);
        _existencias.Verify(x => x.Update(existencia), Times.Once);
        VerificarAuditoria(AccionPermiso.Editar, 77, "Configuración de existencia 77 actualizada");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PersistenciaNoConfirmada_NoEmiteAuditoriaDeExito(bool esCreacion)
    {
        _existencias.Setup(x => x.SaveChangesAsync()).ReturnsAsync(false);

        if (esCreacion)
        {
            _variantes.Setup(x => x.GetByIdAsync(20)).ReturnsAsync(CrearVariante(20));
            _almacenes.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(CrearAlmacen(10));
            _existencias.Setup(x => x.ExisteClaveAsync(20, 10, null, null)).ReturnsAsync(false);
            _existencias.Setup(x => x.AddAsync(It.IsAny<ExistenciaVariante>())).Returns(Task.CompletedTask);

            await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateAsync(new CreateExistenciaVarianteDto
            {
                ProductoVarianteId = 20,
                AlmacenId = 10,
                StockFisico = 10,
                StockReservado = 0,
                StockTransito = 0,
                StockMinimo = 2,
                StockMaximo = 20
            }));
        }
        else
        {
            _existencias.Setup(x => x.GetByIdAsync(77)).ReturnsAsync(CrearExistencia(77, 20, 10));
            _existencias.Setup(x => x.ExisteClaveAsync(20, 10, null, 77)).ReturnsAsync(false);

            await Assert.ThrowsAsync<BusinessRuleException>(() => _service.UpdateConfiguracionAsync(77,
                new UpdateExistenciaVarianteConfiguracionDto { StockMinimo = 2, StockMaximo = 20 }));
        }

        _auditoria.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_RechazaUbicacionDeOtroAlmacen_SinPersistirNiAuditar()
    {
        _variantes.Setup(x => x.GetByIdAsync(20)).ReturnsAsync(CrearVariante(20));
        _almacenes.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(CrearAlmacen(10));
        _ubicaciones.Setup(x => x.GetByIdAsync(55)).ReturnsAsync(CrearUbicacion(55, 99));

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateAsync(new CreateExistenciaVarianteDto
        {
            ProductoVarianteId = 20,
            AlmacenId = 10,
            UbicacionAlmacenId = 55,
            StockFisico = 10,
            StockMinimo = 2
        }));

        Assert.Contains("mismo almacén", ex.Message, StringComparison.OrdinalIgnoreCase);
        _existencias.Verify(x => x.AddAsync(It.IsAny<ExistenciaVariante>()), Times.Never);
        _existencias.Verify(x => x.SaveChangesAsync(), Times.Never);
        _auditoria.VerifyNoOtherCalls();
    }

    private void VerificarAuditoria(AccionPermiso accion, int referenciaId, string prefijo)
    {
        _auditoria.Verify(x => x.RegistrarAsync(
            ModuloSistema.Inventario,
            accion,
            It.Is<string>(d => d.StartsWith(prefijo, StringComparison.Ordinal)),
            referenciaId,
            "ExistenciaVariante",
            It.IsAny<object?>(),
            It.IsAny<object?>(),
            It.IsAny<string?>(),
            "Exito",
            It.IsAny<string?>()), Times.Once);
    }

    private static ProductoVariante CrearVariante(int id) => new()
    {
        Id = id,
        ProductoId = 100 + id,
        Producto = new Producto { Id = 100 + id, Nombre = $"Producto {id}", Activo = true },
        Sku = $"SKU-{id}",
        Activo = true
    };

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

    private static UbicacionAlmacen CrearUbicacion(int id, int almacenId) => new()
    {
        Id = id,
        AlmacenId = almacenId,
        Almacen = CrearAlmacen(almacenId),
        Codigo = $"U-{id}",
        Nombre = $"Ubicación {id}",
        Tipo = TipoUbicacionAlmacen.Rack,
        Activa = true
    };

    private static ExistenciaVariante CrearExistencia(int id, int varianteId, int almacenId)
    {
        var existencia = new ExistenciaVariante
        {
            Id = id,
            ProductoVarianteId = varianteId,
            ProductoVariante = CrearVariante(varianteId),
            AlmacenId = almacenId,
            Almacen = CrearAlmacen(almacenId)
        };
        existencia.EstablecerStocks(12, 2, 3, 4, 30);
        return existencia;
    }
}
