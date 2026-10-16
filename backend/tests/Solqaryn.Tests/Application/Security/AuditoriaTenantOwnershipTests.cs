using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests.Application.Security;

public sealed class AuditoriaTenantOwnershipTests
{
    private static (AuditoriaService Service, List<RegistroAuditoria> Auditorias) Create(HttpContext? http)
    {
        var auditorias = new List<RegistroAuditoria>();
        var repo = new Mock<IAuditoriaRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<RegistroAuditoria>()))
            .Callback<RegistroAuditoria>(auditorias.Add)
            .Returns(Task.CompletedTask);
        repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        var user = new Mock<ICurrentUserService>();
        user.SetupGet(u => u.UsuarioId).Returns(10);
        user.SetupGet(u => u.NombreUsuario).Returns("empleado-prueba");

        var accessor = new HttpContextAccessor { HttpContext = http };
        var service = new AuditoriaService(repo.Object, user.Object, accessor,
            Mock.Of<ILogger<AuditoriaService>>());
        return (service, auditorias);
    }

    [Fact]
    public async Task Marca_RBAC_verificada_se_guarda_como_propiedad_de_auditoria()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers["X-Empresa-Id"] = "999";
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = 7;
        var (service, records) = Create(http);

        await service.RegistrarEstrictoAsync(ModuloSistema.Productos, AccionPermiso.Crear,
            "Alta probada", 55, "Producto");

        var record = Assert.Single(records);
        Assert.Equal(7, record.EmpresaId);
        Assert.Equal(55, record.ReferenciaId);
        Assert.Equal("Producto", record.Entidad);
    }

    [Fact]
    public async Task Header_manipulado_sin_RBAC_no_atribuye_una_empresa()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers["X-Empresa-Id"] = "999";
        var (service, records) = Create(http);

        await service.RegistrarEstrictoAsync(ModuloSistema.Productos, AccionPermiso.Crear,
            "Alta sin marca", 56, "Producto");

        Assert.Null(Assert.Single(records).EmpresaId);
    }

    [Fact]
    public async Task FacturaPublica_UsaEmpresaDelTokenVerificado_NoHeader()
    {
        var http = new DefaultHttpContext();
        http.Request.Method = HttpMethods.Get;
        http.Request.Path = "/facturas/publico/abcdefghijklmnopqrstuvwxyz0123456789ABCDEFG/pdf";
        http.Request.Headers["X-Empresa-Id"] = "999";
        http.Items[VerifiedPublicInvoiceTenant.AuthorizedEmpresaIdItemKey] = 7;
        var (service, records) = Create(http);

        await service.RegistrarEstrictoAsync(
            ModuloSistema.Facturacion, AccionPermiso.Exportar, "PDF público A");

        Assert.Equal(7, Assert.Single(records).EmpresaId);
    }

    [Fact]
    public async Task TiendaPublica_UsaEmpresaVerificadaPorHost_NoHeader()
    {
        var http = new DefaultHttpContext();
        http.Request.Path = "/tienda/checkout/validar";
        http.Request.Headers["X-Empresa-Id"] = "999";
        http.Items[VerifiedPublicStoreTenant.AuthorizedEmpresaIdItemKey] = 8;
        var (service, records) = Create(http);

        await service.RegistrarEstrictoAsync(
            ModuloSistema.Ventas, AccionPermiso.Crear, "Checkout tienda B");

        Assert.Equal(8, Assert.Single(records).EmpresaId);
    }

    [Fact]
    public async Task RbacAdministrativoTienePrecedenciaSobreMarcasPublicas()
    {
        var http = new DefaultHttpContext();
        http.Request.Path = "/tienda/cuenta";
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = 9;
        http.Items[VerifiedPublicStoreTenant.AuthorizedEmpresaIdItemKey] = 8;
        http.Items[VerifiedPublicInvoiceTenant.AuthorizedEmpresaIdItemKey] = 7;
        var (service, records) = Create(http);

        await service.RegistrarEstrictoAsync(
            ModuloSistema.Auditoria, AccionPermiso.Ver, "Prioridad RBAC");

        Assert.Equal(9, Assert.Single(records).EmpresaId);
    }

    [Fact]
    public async Task MarcasPublicasFueraDeSuRutaNoAtribuyenEmpresa()
    {
        var http = new DefaultHttpContext();
        http.Request.Path = "/reportes/ventas";
        http.Request.Headers["X-Empresa-Id"] = "999";
        http.Items[VerifiedPublicStoreTenant.AuthorizedEmpresaIdItemKey] = 8;
        http.Items[VerifiedPublicInvoiceTenant.AuthorizedEmpresaIdItemKey] = 7;
        var (service, records) = Create(http);

        await service.RegistrarEstrictoAsync(
            ModuloSistema.Auditoria, AccionPermiso.Ver, "Marca pública fuera de ruta");

        Assert.Null(Assert.Single(records).EmpresaId);
    }

    [Fact]
    public async Task Operacion_no_HTTP_no_infiere_propietario()
    {
        var (service, records) = Create(null);

        await service.RegistrarEstrictoAsync(ModuloSistema.Productos, AccionPermiso.Crear,
            "Proceso sin contexto", 57, "Producto");

        Assert.Null(Assert.Single(records).EmpresaId);
    }
}
