using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P4: las invocaciones internas sin RBAC no pueden producir reportes
/// ni exportaciones de usuarios, roles o auditoria empresariales.
/// </summary>
public sealed class AdministrativeReportTenantGuardTests
{
    [Fact]
    public async Task SinTenantVerificado_TodosLosReportesYExportacionesFallanCerrado()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p4-admin-report-guard-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options);
        var service = new ReporteAdministrativoService(db);
        var filtro = new ReporteAdministrativoFiltroDto();

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => service.ObtenerResumenAsync(filtro));
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => service.ObtenerUsuariosAccesoAsync());
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => service.ObtenerRolesPermisosAsync());
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => service.ObtenerResumenAuditoriaAsync(filtro));

        foreach (var tipo in new[] { "usuarios", "roles", "auditoria" })
        {
            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => service.ExportarAsync(tipo, "csv", filtro));
        }
    }

    [Fact]
    public async Task CapabilityPublica_NoConfiereAccesoAReportesPrivados()
    {
        var http = new DefaultHttpContext();
        http.Request.Path = "/tienda/productos";
        http.Items[VerifiedPublicStoreTenant.AuthorizedEmpresaIdItemKey] = 31;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p4-admin-public-guard-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(
            options, tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });
        var service = new ReporteAdministrativoService(db);

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => service.ObtenerUsuariosAccesoAsync());
        http.Items.Remove(VerifiedPublicStoreTenant.AuthorizedEmpresaIdItemKey);
        http.Request.Path = "/facturas/publico/token/pdf";
        http.Items[VerifiedPublicInvoiceTenant.AuthorizedEmpresaIdItemKey] = 31;
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => service.ObtenerResumenAuditoriaAsync(new ReporteAdministrativoFiltroDto()));
    }

    [Fact]
    public async Task A_B_ReportesYExportsUsanMembresiasActuales_YNoRolLegacy()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p4-admin-ab-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(
            options, tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });
        var a = new Empresa("REPORTES EMPRESA A");
        var b = new Empresa("REPORTES EMPRESA B");
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();

        var plantilla = new Rol { Nombre = "PLANTILLA", NombreNormalizado = "PLANTILLA" };
        db.Roles.Add(plantilla);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = a.Id;
        var rolA = new Rol { Nombre = "ADMIN A", NombreNormalizado = "ADMIN_A", EmpresaId = a.Id };
        db.Roles.Add(rolA);
        await db.SaveChangesAsync();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = b.Id;
        var rolB = new Rol { Nombre = "ADMIN B", NombreNormalizado = "ADMIN_B", EmpresaId = b.Id };
        db.Roles.Add(rolB);
        await db.SaveChangesAsync();

        http.Items.Remove(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);
        // El RolId global heredado se invierte deliberadamente: no es el rol efectivo.
        var usuarioA = new Usuario
        {
            NombreUsuario = "solo-usuario-a", NombreCompleto = "Usuario de empresa A",
            PasswordHash = "test", RolId = rolB.Id
        };
        var usuarioB = new Usuario
        {
            NombreUsuario = "solo-usuario-b", NombreCompleto = "Usuario de empresa B",
            PasswordHash = "test", RolId = rolA.Id
        };
        db.Usuarios.AddRange(usuarioA, usuarioB);
        await db.SaveChangesAsync();
        db.UsuarioEmpresas.AddRange(
            new UsuarioEmpresa(usuarioA.Id, a.Id, rolA.Id),
            new UsuarioEmpresa(usuarioB.Id, b.Id, rolB.Id));
        await db.SaveChangesAsync();
        db.RegistrosAuditoria.AddRange(
            new RegistroAuditoria { EmpresaId = a.Id, NombreUsuario = "solo-usuario-a", Descripcion = "evento-a" },
            new RegistroAuditoria { EmpresaId = b.Id, NombreUsuario = "solo-usuario-b", Descripcion = "evento-b" });
        await db.SaveChangesAsync();

        var service = new ReporteAdministrativoService(db);
        var periodo = new ReporteAdministrativoFiltroDto();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = a.Id;
        var visibleA = Assert.Single(await service.ObtenerUsuariosAccesoAsync());
        Assert.Equal(usuarioA.Id, visibleA.UsuarioId);
        Assert.Equal(rolA.Id, visibleA.RolId);
        Assert.Equal("ADMIN A", visibleA.Rol);
        Assert.Equal(1, (await service.ObtenerResumenAuditoriaAsync(periodo)).Total);
        Assert.DoesNotContain(await service.ObtenerRolesPermisosAsync(), x => x.RolId == rolB.Id);
        var csvA = await service.ExportarAsync("usuarios", "csv", periodo);
        var textoA = System.Text.Encoding.UTF8.GetString(csvA.Contenido);
        Assert.Contains("solo-usuario-a", textoA, StringComparison.Ordinal);
        Assert.DoesNotContain("solo-usuario-b", textoA, StringComparison.Ordinal);
        var auditA = await service.ExportarAsync("auditoria", "csv", periodo);
        Assert.DoesNotContain("evento-b", System.Text.Encoding.UTF8.GetString(auditA.Contenido),
            StringComparison.Ordinal);

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = b.Id;
        var visibleB = Assert.Single(await service.ObtenerUsuariosAccesoAsync());
        Assert.Equal(usuarioB.Id, visibleB.UsuarioId);
        Assert.Equal(rolB.Id, visibleB.RolId);
        Assert.Equal("ADMIN B", visibleB.Rol);
        Assert.Equal(1, (await service.ObtenerResumenAuditoriaAsync(periodo)).Total);
        var csvB = await service.ExportarAsync("usuarios", "csv", periodo);
        Assert.DoesNotContain("solo-usuario-a", System.Text.Encoding.UTF8.GetString(csvB.Contenido),
            StringComparison.Ordinal);
    }

}
