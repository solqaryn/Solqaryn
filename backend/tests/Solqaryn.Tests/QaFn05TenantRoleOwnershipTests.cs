using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
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
    [Fact]
    public async Task IdentidadCompartida_ProyeccionCompatibleUsaRolDeEmpresaVerificada_NoHeaderNiRolBase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"qa-fn05-effective-{Guid.NewGuid():N}").Options;
        await using var setup = new AppDbContext(options);
        setup.Set<Empresa>().AddRange(new Empresa("A") { Id = 11 }, new Empresa("B") { Id = 12 });
        setup.Roles.AddRange(
            new Rol { Id = 3, Nombre = "Admin A", NombreNormalizado = "ADMIN A", EmpresaId = 11,
                Ambito = AmbitoAutorizacion.Empresa, EsAdministrador = true, Activo = true },
            new Rol { Id = 4, Nombre = "Operador B", NombreNormalizado = "OPERADOR B", EmpresaId = 12,
                Ambito = AmbitoAutorizacion.Empresa, EsAdministrador = false, Activo = true });
        setup.Usuarios.Add(new Usuario { Id = 7, NombreUsuario = "qa_shared_actor",
            NombreCompleto = "Actor de prueba", PasswordHash = "fixture-unused", RolId = 3, Activo = true });
        setup.UsuarioEmpresas.AddRange(new UsuarioEmpresa(7, 11, 3), new UsuarioEmpresa(7, 12, 4));
        await setup.SaveChangesAsync();
        var http = new DefaultHttpContext();
        var accessor = new HttpContextAccessor { HttpContext = http };
        await using var db = new AppDbContext(options, httpContextAccessor: accessor);
        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.EstaAutenticado).Returns(true);
        current.SetupGet(x => x.UsuarioId).Returns(7);
        current.SetupGet(x => x.EsAdministrador).Returns(true);
        var service = new UsuarioScopeService(db, current.Object);

        http.Request.Headers["X-Empresa-Id"] = "12";
        var legacy = await service.ObtenerActualAsync();
        Assert.NotNull(legacy);
        Assert.Equal(3, legacy!.RolId);

        http.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"] = 12;
        http.Request.Headers["X-Empresa-Id"] = "11";
        var b = await service.ObtenerActualAsync();
        Assert.NotNull(b);
        Assert.Equal(4, b!.RolId);
        Assert.False(b.EsAdministrador);

        http.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"] = 11;
        var a = await service.ObtenerActualAsync();
        Assert.NotNull(a);
        Assert.Equal(3, a!.RolId);
        Assert.True(a.EsAdministrador);

        (await setup.UsuarioEmpresas.SingleAsync(x => x.EmpresaId == 12)).Desactivar();
        await setup.SaveChangesAsync();
        http.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"] = 12;
        Assert.Null(await service.ObtenerActualAsync());
        http.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"] = 999;
        Assert.Null(await service.ObtenerActualAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Dashboard_RespetaAdministradorEfectivo_YNoExponeCostosEnAlertaOperativa(bool administrador)
    {
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        scope.Setup(x => x.ObtenerActualAsync()).ReturnsAsync(new UsuarioScopeActual(7, 4, "Actor", administrador));
        var productos = new Mock<IProductoRepository>();
        var ventas = new Mock<IVentaRepository>();
        var compras = new Mock<ICompraRepository>();
        var revision = new Mock<IRevisionFinancieraRepository>();
        var movimientos = new Mock<IMovimientoFinancieroRepository>();
        var variantes = new Mock<IProductoVarianteRepository>();
        var producto = new Producto { Id = 17, Nombre = "Fixture", Activo = true };
        producto.Variantes.Add(new ProductoVariante { Id = 18, ProductoId = 17, Producto = producto,
            Cantidad = 1, UmbralStockBajo = 5, Costo = 123.45m, Precio = 200m, Activo = true });
        productos.Setup(x => x.GetStockBajoAsync()).ReturnsAsync(new List<Producto> { producto });
        productos.Setup(x => x.GetUltimosAgregadosAsync(It.IsAny<int>())).ReturnsAsync(new List<Producto>());
        productos.Setup(x => x.GetValorTotalCostoPorTipoAsync(It.IsAny<TipoInventario>())).ReturnsAsync(123.45m);
        ventas.Setup(x => x.GetUltimasAsync(It.IsAny<int>(), It.IsAny<int?>())).ReturnsAsync(new List<Venta>());
        compras.Setup(x => x.GetUltimasAsync(It.IsAny<int>(), It.IsAny<int?>())).ReturnsAsync(new List<Compra>());
        movimientos.Setup(x => x.GetFilteredAsync(null, null)).ReturnsAsync(new List<MovimientoFinanciero>());
        var servicio = new DashboardService(productos.Object, variantes.Object, compras.Object,
            ventas.Object, revision.Object, movimientos.Object, scope.Object);

        var resultado = await servicio.GetResumenAsync();
        var alerta = Assert.Single(resultado.ProductosStockBajo);
        Assert.Equal(administrador ? 123.45m : 0m, alerta.Costo);
        Assert.Equal(administrador ? 123.45m : 0m, Assert.Single(alerta.Variantes).Costo);
        Assert.Equal(200m, alerta.Precio);
        Assert.Equal(123.45m, producto.Variantes.Single().Costo!.Value);
        if (!administrador)
        {
            Assert.Equal(0m, resultado.ValorTotalInventario);
            Assert.Empty(resultado.UltimasCompras);
            movimientos.Verify(x => x.GetFilteredAsync(null, null), Times.Never);
            revision.Verify(x => x.GetUltimaAsync(), Times.Never);
            compras.Verify(x => x.GetUltimasAsync(It.IsAny<int>(), It.IsAny<int?>()), Times.Never);
        }
    }

}
