using Solqaryn.Application.Common;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Services;

/// <summary>
/// Mantiene catálogos RBAC y grants explícitos de roles administradores.
/// Desde ERP-N0.4 no migra ni consulta RolUsuario/Modulo/Accion/Permitido en RolPermiso.
/// </summary>
public class SeedPermisoService
{
    private const string AdministradorNormalizado = "ADMINISTRADOR";
    private const string VendedorNormalizado = "VENDEDOR";
    private readonly AppDbContext _context;

    public SeedPermisoService(AppDbContext context)
    {
        _context = context;
    }

    public async Task SeedDefaultsAsync()
    {
        await SeedCatalogoPermisosAsync();
        await _context.SaveChangesAsync();

        var roles = await SeedRolesSistemaAsync();
        await _context.SaveChangesAsync();

        // EsAdministrador ya no es un bypass de autorización. Para conservar el
        // acceso administrativo los roles administradores reciben grants explícitos.
        await AsegurarGrantsAdministradoresAsync();

        if (roles.VendedorCreado)
            await SeedVendedorInicialAsync(roles.Vendedor);

        await _context.SaveChangesAsync();
    }

    private async Task<(Rol Administrador, Rol Vendedor, bool VendedorCreado)> SeedRolesSistemaAsync()
    {
        var existentes = await _context.Roles.IgnoreQueryFilters().ToListAsync();

        var administrador = existentes.FirstOrDefault(r =>
            r.NombreNormalizado == AdministradorNormalizado ||
            string.Equals(r.Nombre, "Administrador", StringComparison.OrdinalIgnoreCase));
        if (administrador is null)
        {
            administrador = new Rol
            {
                Nombre = "Administrador",
                NombreNormalizado = AdministradorNormalizado,
                Descripcion = "Rol de sistema con grants administrativos explícitos.",
                EsSistema = true,
                EsAdministrador = true,
                Activo = true,
                Eliminado = false,
                FechaCreacion = DateTime.UtcNow
            };
            _context.Roles.Add(administrador);
        }
        else
        {
            administrador.NombreNormalizado = AdministradorNormalizado;
            administrador.EsSistema = true;
            administrador.EsAdministrador = true;
            administrador.Activo = true;
            administrador.Eliminado = false;
        }

        var vendedor = existentes.FirstOrDefault(r =>
            r.NombreNormalizado == VendedorNormalizado ||
            string.Equals(r.Nombre, "Vendedor", StringComparison.OrdinalIgnoreCase));
        var vendedorCreado = vendedor is null;
        if (vendedor is null)
        {
            vendedor = new Rol
            {
                Nombre = "Vendedor",
                NombreNormalizado = VendedorNormalizado,
                Descripcion = "Rol de sistema para operación comercial con permisos administrables.",
                EsSistema = true,
                EsAdministrador = false,
                Activo = true,
                Eliminado = false,
                FechaCreacion = DateTime.UtcNow
            };
            _context.Roles.Add(vendedor);
        }
        else if (string.IsNullOrWhiteSpace(vendedor.NombreNormalizado))
        {
            vendedor.NombreNormalizado = VendedorNormalizado;
        }

        await _context.SaveChangesAsync();
        return (administrador, vendedor, vendedorCreado);
    }

    private async Task AsegurarGrantsAdministradoresAsync()
    {
        var administradores = await _context.Roles
            .IgnoreQueryFilters()
            .Where(r => r.EsAdministrador && r.Activo && !r.Eliminado)
            .ToListAsync();
        if (administradores.Count == 0) return;

        var permisos = await _context.Permisos
            .IgnoreQueryFilters()
            .Where(p => p.Activo && !p.Eliminado)
            .ToListAsync();

        var rolIds = administradores.Select(r => r.Id).ToList();
        var existentes = await _context.RolPermisos
            .Where(rp => rolIds.Contains(rp.RolId))
            .Select(rp => new { rp.RolId, rp.PermisoId })
            .ToListAsync();
        var claves = existentes.Select(x => (x.RolId, x.PermisoId)).ToHashSet();

        foreach (var rol in administradores)
        {
            foreach (var permiso in permisos)
            {
                if (claves.Add((rol.Id, permiso.Id)))
                    _context.RolPermisos.Add(new RolPermiso { RolId = rol.Id, PermisoId = permiso.Id });
            }
        }
    }

    private async Task SeedVendedorInicialAsync(Rol vendedor)
    {
        foreach (var (modulo, accion) in CatalogoPermisosBase.DefaultVendedor)
        {
            var permiso = await _context.Permisos
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Modulo == modulo && p.Accion == accion && p.Activo && !p.Eliminado);
            if (permiso is null) continue;

            var existe = await _context.RolPermisos.AnyAsync(rp => rp.RolId == vendedor.Id && rp.PermisoId == permiso.Id);
            if (!existe)
                _context.RolPermisos.Add(new RolPermiso { RolId = vendedor.Id, PermisoId = permiso.Id });
        }
    }

    private async Task SeedCatalogoPermisosAsync()
    {
        foreach (var (modulo, acciones) in CatalogoPermisosBase.Definicion)
        {
            foreach (var accion in acciones)
            {
                var permiso = await _context.Permisos
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.Modulo == modulo && p.Accion == accion);

                var codigo = $"{modulo}.{accion}".ToUpperInvariant();
                if (permiso is null)
                {
                    _context.Permisos.Add(new Permiso
                    {
                        Codigo = codigo,
                        Nombre = $"{modulo} - {accion}",
                        Descripcion = $"Permite {accion} en {modulo}.",
                        Modulo = modulo,
                        Accion = accion,
                        EsSistema = true,
                        Activo = true,
                        Eliminado = false,
                        FechaCreacion = DateTime.UtcNow
                    });
                    continue;
                }

                permiso.Codigo = codigo;
                permiso.Nombre = $"{modulo} - {accion}";
                permiso.Descripcion = $"Permite {accion} en {modulo}.";
                permiso.EsSistema = true;
                permiso.Activo = true;
                permiso.Eliminado = false;
                permiso.FechaEliminacion = null;
                permiso.EliminadoPorUsuarioId = null;
                permiso.FechaActualizacion = DateTime.UtcNow;
            }
        }
    }
}
