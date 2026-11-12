using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-010.A: una cabecera X-Empresa-Id escrita por el solicitante no autoriza
/// exportaciones administrativas. Prueba dirigida en EF InMemory; no acredita
/// middleware HTTP/JWT, ZIP, ficheros temporales ni MySQL real.
/// </summary>
public sealed class P2010AExportacionSinTenantVerificadoTests
{
    [Theory]
    [InlineData("auditoria", "csv", true)]
    [InlineData("auditoria", "xlsx", true)]
    [InlineData("auditoria", "pdf", true)]
    [InlineData("usuarios", "csv", true)]
    [InlineData("roles", "xlsx", false)]
    public async Task ExportarAsync_RechazaSolicitudSinTenantVerificado_AunqueSeFalsifiqueCabecera(
        string tipo, string formato, bool cabeceraSuplantada)
    {
        var http = new DefaultHttpContext();
        if (cabeceraSuplantada)
            http.Request.Headers["X-Empresa-Id"] = "202";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2010a-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(
            options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });
        var servicio = new ReporteAdministrativoService(db);
        var filtro = new ReporteAdministrativoFiltroDto
        {
            Desde = DateTime.UtcNow.Date.AddDays(-1),
            Hasta = DateTime.UtcNow.Date
        };

        // La cabecera sola NO establece VerifiedCommercialTenant.
        Assert.Null(db.AuthorizedCommercialEmpresaId);
        var error = await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => servicio.ExportarAsync(tipo, formato, filtro));

        // Los mensajes de denegación no deben filtrar una empresa reclamada.
        Assert.DoesNotContain("202", error.Message, StringComparison.Ordinal);
    }
}
