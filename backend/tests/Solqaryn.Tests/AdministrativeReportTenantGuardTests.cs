using Microsoft.EntityFrameworkCore;
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
}
