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
/// P2-010.D: una capacidad publica o una cabecera de cliente no pueden
/// sustituir al tenant comercial RBAC para generar exportaciones privadas.
/// Usa EF InMemory; no prueba autenticacion JWT HTTP ni MySQL real.
/// </summary>
public sealed class P2010DExportacionesCapacidadPublicaTests
{
    [Fact]
    public async Task Capacidad_publica_B_y_header_falso_no_autorizan_csv_xlsx_pdf_sin_tenant_comercial()
    {
        const int empresaA = 101;
        const int empresaB = 202;
        var http = new DefaultHttpContext();
        http.Request.Headers["X-Empresa-Id"] = empresaB.ToString();
        http.Items[VerifiedPublicStoreTenant.AuthorizedEmpresaIdItemKey] = empresaB;
        http.Items[VerifiedPublicInvoiceTenant.AuthorizedEmpresaIdItemKey] = empresaB;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2010d-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(
            options, tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var fecha = DateTime.UtcNow.Date.AddHours(12);
        db.RegistrosAuditoria.AddRange(
            new RegistroAuditoria
            {
                EmpresaId = empresaA,
                Fecha = fecha,
                NombreUsuario = "P2010D A",
                Modulo = ModuloSistema.ReportesAdministrativos,
                Accion = AccionPermiso.Exportar,
                Descripcion = "P2010D_PRIVADO_A"
            },
            new RegistroAuditoria
            {
                EmpresaId = empresaB,
                Fecha = fecha,
                NombreUsuario = "P2010D B",
                Modulo = ModuloSistema.ReportesAdministrativos,
                Accion = AccionPermiso.Exportar,
                Descripcion = "P2010D_AJENO_B"
            });
        await db.SaveChangesAsync();

        var servicio = new ReporteAdministrativoService(db);
        var filtro = new ReporteAdministrativoFiltroDto
        {
            Desde = fecha.Date.AddDays(-1),
            Hasta = fecha.Date.AddDays(1)
        };

        // Control positivo: el scope comercial A prevalece sobre header y capacidades B.
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA;
        var archivo = await servicio.ExportarAsync("auditoria", "xlsx", filtro);
        using (var stream = new MemoryStream(archivo.Contenido))
        using (var workbook = new XLWorkbook(stream))
        {
            var hoja = workbook.Worksheet("Reporte");
            Assert.Equal("P2010D_PRIVADO_A", hoja.Cell(2, 8).GetString());
            Assert.Equal(2, hoja.LastRowUsed()!.RowNumber());
        }

        // Al revocar la unica autoridad comercial, ninguna exportacion puede salir.
        // Las dos capacidades publicas y la cabecera manipulada permanecen activas.
        http.Items.Remove(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);
        foreach (var formato in new[] { "csv", "xlsx", "pdf" })
        {
            await Assert.ThrowsAsync<ForbiddenAccessException>(async () =>
            {
                await servicio.ExportarAsync("auditoria", formato, filtro);
            });
        }
    }
}
