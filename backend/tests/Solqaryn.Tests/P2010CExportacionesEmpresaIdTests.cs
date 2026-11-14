using System.Text;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-010.C: un escenario causal de exportacion CSV/XLSX de auditoria A/B.
/// Usa solo EF InMemory y marcas comerciales verificadas; no certifica JWT,
/// ZIP, autorizacion HTTP ni aislamiento SQL/MySQL real.
/// </summary>
public sealed class P2010CExportacionesEmpresaIdTests
{
    [Fact]
    public async Task Csv_y_xlsx_usan_tenant_verificado_y_no_la_cabecera_falsificada()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2010c-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        const int empresaA = 101;
        const int empresaB = 202;
        var fecha = DateTime.UtcNow.Date.AddHours(12);
        db.RegistrosAuditoria.AddRange(
            new RegistroAuditoria
            {
                EmpresaId = empresaA,
                Fecha = fecha,
                NombreUsuario = "P2010C TEST A",
                Modulo = ModuloSistema.ReportesAdministrativos,
                Accion = AccionPermiso.Exportar,
                Descripcion = "P2010C_REGISTRO_A_AUTORIZADO"
            },
            new RegistroAuditoria
            {
                EmpresaId = empresaB,
                Fecha = fecha,
                NombreUsuario = "P2010C TEST B",
                Modulo = ModuloSistema.ReportesAdministrativos,
                Accion = AccionPermiso.Exportar,
                Descripcion = "P2010C_REGISTRO_B_AJENO"
            });
        await db.SaveChangesAsync();

        var servicio = new ReporteAdministrativoService(db);
        var filtro = new ReporteAdministrativoFiltroDto
        {
            Desde = fecha.Date.AddDays(-1),
            Hasta = fecha.Date.AddDays(1)
        };

        // Una cabecera aportada por el cliente no concede autoridad comercial.
        http.Request.Headers["X-Empresa-Id"] = empresaB.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA;

        var csvA = await servicio.ExportarAsync("auditoria", "csv", filtro);
        var textoA = Encoding.UTF8.GetString(csvA.Contenido);
        Assert.EndsWith(".csv", csvA.NombreArchivo);
        Assert.Contains("P2010C_REGISTRO_A_AUTORIZADO", textoA);
        Assert.DoesNotContain("P2010C_REGISTRO_B_AJENO", textoA);

        var xlsxA = await servicio.ExportarAsync("auditoria", "xlsx", filtro);
        using (var stream = new MemoryStream(xlsxA.Contenido))
        using (var workbook = new XLWorkbook(stream))
        {
            var hoja = workbook.Worksheet("Reporte");
            Assert.Equal("P2010C_REGISTRO_A_AUTORIZADO", hoja.Cell(2, 8).GetString());
            Assert.Equal(2, hoja.LastRowUsed()!.RowNumber());
        }

        // Mismo contexto EF, nueva marca RBAC: no retener la empresa anterior.
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB;
        http.Request.Headers["X-Empresa-Id"] = empresaA.ToString();
        var csvB = Encoding.UTF8.GetString(
            (await servicio.ExportarAsync("auditoria", "csv", filtro)).Contenido);
        Assert.Contains("P2010C_REGISTRO_B_AJENO", csvB);
        Assert.DoesNotContain("P2010C_REGISTRO_A_AUTORIZADO", csvB);

        // Sin marca verificada el servicio no debe producir archivos comerciales.
        http.Items.Remove(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);
        await Assert.ThrowsAsync<ForbiddenAccessException>(async () =>
        {
            await servicio.ExportarAsync("auditoria", "csv", filtro);
        });
    }

    [Fact]
    public async Task Zip_no_se_presenta_como_exportacion_soportada()
    {
        var http = new DefaultHttpContext();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = 101;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2010c-zip-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });
        var servicio = new ReporteAdministrativoService(db);

        await Assert.ThrowsAsync<BusinessRuleException>(async () =>
        {
            await servicio.ExportarAsync("auditoria", "zip",
                new ReporteAdministrativoFiltroDto());
        });
    }
}
