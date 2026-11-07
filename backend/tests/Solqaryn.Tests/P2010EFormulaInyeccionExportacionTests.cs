using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-010.E: el contenido editable de auditoria no debe inyectar formulas
/// al exportar CSV/XLSX, incluso con una cabecera de empresa falsificada.
/// EF InMemory solamente: no acredita JWT HTTP, MySQL ni archivos ZIP.
/// </summary>
public sealed class P2010EFormulaInyeccionExportacionTests
{
    [Theory]
    [InlineData("=1+1")]
    [InlineData("+SUM(1,2)")]
    [InlineData("-42+42")]
    [InlineData("@SUM(1,2)")]
    public async Task Auditoria_de_tenant_A_no_exporta_formulas_activas_ni_filas_de_B(
        string contenidoMalicioso)
    {
        const int empresaA = 101;
        const int empresaB = 202;
        var http = new DefaultHttpContext();
        http.Request.Headers["X-Empresa-Id"] = empresaB.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2010e-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(
            options, tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var fecha = DateTime.UtcNow.Date.AddHours(12);
        db.RegistrosAuditoria.AddRange(
            new RegistroAuditoria
            {
                EmpresaId = empresaA,
                Fecha = fecha,
                NombreUsuario = "P2010E A",
                Modulo = ModuloSistema.ReportesAdministrativos,
                Accion = AccionPermiso.Exportar,
                Descripcion = contenidoMalicioso
            },
            new RegistroAuditoria
            {
                EmpresaId = empresaB,
                Fecha = fecha,
                NombreUsuario = "P2010E B",
                Modulo = ModuloSistema.ReportesAdministrativos,
                Accion = AccionPermiso.Exportar,
                Descripcion = "P2010E_REGISTRO_B_PROHIBIDO"
            });
        await db.SaveChangesAsync();

        var servicio = new ReporteAdministrativoService(db);
        var filtro = new ReporteAdministrativoFiltroDto
        {
            Desde = fecha.Date.AddDays(-1),
            Hasta = fecha.Date.AddDays(1)
        };

        var csv = await servicio.ExportarAsync("auditoria", "csv", filtro);
        var texto = Encoding.UTF8.GetString(csv.Contenido);
        Assert.Contains("\"'" + contenidoMalicioso + "\"", texto);
        Assert.DoesNotContain("P2010E_REGISTRO_B_PROHIBIDO", texto);

        var xlsx = await servicio.ExportarAsync("auditoria", "xlsx", filtro);
        using var stream = new MemoryStream(xlsx.Contenido);
        using var workbook = new XLWorkbook(stream);
        var hoja = workbook.Worksheet("Reporte");
        var celda = hoja.Cell(2, 8);
        Assert.False(celda.HasFormula);
        Assert.Equal("'" + contenidoMalicioso, celda.GetString());
        Assert.Equal(2, hoja.LastRowUsed()!.RowNumber());
    }
}
