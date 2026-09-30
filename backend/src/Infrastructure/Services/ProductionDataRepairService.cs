using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Services;

public sealed record ProductionDataRepairResult(
    int EmpresasTotales,
    int EmpresaConfiguracionesActivas,
    int? EmpresaMaterializadaId,
    int MembresiasLegacyCreadas);

public class ProductionDataRepairService
{
    private const string RepairActor = "SYSTEM:N61_LEGACY_TENANT_REPAIR";
    private readonly AppDbContext _context;

    public ProductionDataRepairService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ProductionDataRepairResult> RepairAsync()
    {
        await _context.Database.ExecuteSqlRawAsync("""
            UPDATE `Usuarios`
            SET `Eliminado` = FALSE
            WHERE `Eliminado` = TRUE
              AND `FechaEliminacion` IS NULL
              AND `EliminadoPorUsuarioId` IS NULL;
            """);

        var empresasTotales = await _context.Set<Empresa>()
            .AsNoTracking()
            .CountAsync();

        var configuracionesActivas = await _context.EmpresaConfiguraciones
            .AsNoTracking()
            .Where(x => x.Activa)
            .OrderBy(x => x.Id)
            .Take(2)
            .ToListAsync();

        int? empresaMaterializadaId = null;

        // Rollout legacy single-tenant: sólo existe autoridad suficiente para
        // materializar Empresa cuando no hay ninguna y exactamente una
        // EmpresaConfiguracion activa ya persistida. No se inventa identidad ni
        // se decide entre múltiples configuraciones.
        if (empresasTotales == 0 && configuracionesActivas.Count == 1)
        {
            var legacy = configuracionesActivas[0];
            var nombre = !string.IsNullOrWhiteSpace(legacy.NombreComercial)
                ? legacy.NombreComercial.Trim()
                : legacy.NombreVisibleSistema?.Trim();

            if (!string.IsNullOrWhiteSpace(nombre))
            {
                var empresa = new Empresa(nombre)
                {
                    CreadoPorNombreUsuario = RepairActor
                };
                empresa.ActualizarIdentidadLegal(
                    legacy.RTN,
                    legacy.Direccion,
                    legacy.LogoUrl,
                    legacy.LogoPublicId);

                _context.Set<Empresa>().Add(empresa);
                await _context.SaveChangesAsync();

                var configTenant = new ConfigEmpresa(empresa.Id)
                {
                    Moneda = string.IsNullOrWhiteSpace(legacy.Moneda)
                        ? "HNL"
                        : legacy.Moneda.Trim().ToUpperInvariant(),
                    ZonaHoraria = string.IsNullOrWhiteSpace(legacy.ZonaHoraria)
                        ? "America/Tegucigalpa"
                        : legacy.ZonaHoraria.Trim(),
                    CreadoPorNombreUsuario = RepairActor
                };

                _context.Set<ConfigEmpresa>().Add(configTenant);
                await _context.SaveChangesAsync();

                empresaMaterializadaId = empresa.Id;
                empresasTotales = 1;
            }
        }

        if (empresasTotales != 1)
        {
            return new ProductionDataRepairResult(
                empresasTotales,
                configuracionesActivas.Count,
                empresaMaterializadaId,
                0);
        }

        var empresaUnica = await _context.Set<Empresa>()
            .AsNoTracking()
            .SingleAsync();

        if (!empresaUnica.Activa)
        {
            return new ProductionDataRepairResult(
                empresasTotales,
                configuracionesActivas.Count,
                empresaMaterializadaId,
                0);
        }

        var candidatos = await _context.Usuarios
            .AsNoTracking()
            .Where(u =>
                !u.Eliminado &&
                u.Activo &&
                !u.Bloqueado &&
                u.RolId > 0 &&
                u.RolEntidad.Activo &&
                !u.RolEntidad.Eliminado &&
                !_context.UsuarioEmpresas.Any(m => m.UsuarioId == u.Id))
            .Select(u => new
            {
                u.Id,
                u.RolId
            })
            .ToListAsync();

        foreach (var candidato in candidatos)
        {
            _context.UsuarioEmpresas.Add(new UsuarioEmpresa(
                candidato.Id,
                empresaUnica.Id,
                candidato.RolId)
            {
                CreadoPorNombreUsuario = RepairActor
            });
        }

        if (candidatos.Count > 0)
            await _context.SaveChangesAsync();

        return new ProductionDataRepairResult(
            empresasTotales,
            configuracionesActivas.Count,
            empresaMaterializadaId,
            candidatos.Count);
    }
}
