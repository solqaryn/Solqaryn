using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Models;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

/// <summary>
/// Persistencia común sobre los cuatro maestros normalizados. El contrato conserva
/// su nombre histórico para no romper consumidores HTTP, pero no consulta ni escribe
/// la tabla legacy CatalogosProducto.
/// </summary>
public class CatalogoProductoRepository : ICatalogoProductoRepository
{
    private readonly AppDbContext _context;

    public CatalogoProductoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<MaestroProductoRegistro>> GetAllAsync(
        TipoCatalogoProducto tipo,
        string? buscar = null,
        int? catalogoPadreId = null)
    {
        var resultado = await ConsultarNormalizadosAsync(tipo, buscar, catalogoPadreId, soloActivos: false);
        await CargarUsosProductoAsync(tipo, resultado);
        return resultado;
    }

    public async Task<List<MaestroProductoRegistro>> GetActivosAsync(
        TipoCatalogoProducto tipo,
        int? catalogoPadreId = null)
    {
        var resultado = await ConsultarNormalizadosAsync(tipo, null, catalogoPadreId, soloActivos: true);
        await CargarUsosProductoAsync(tipo, resultado);
        return resultado;
    }

    public async Task<MaestroProductoRegistro?> GetByIdAsync(TipoCatalogoProducto tipo, int id)
    {
        switch (tipo)
        {
            case TipoCatalogoProducto.Marca:
            {
                var entidad = await _context.Marcas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                return entidad is null ? null : MapMarca(entidad, incluirModelos: false);
            }
            case TipoCatalogoProducto.Modelo:
            {
                var entidad = await _context.Modelos.AsNoTracking().Include(x => x.Marca).FirstOrDefaultAsync(x => x.Id == id);
                return entidad is null ? null : MapModelo(entidad);
            }
            case TipoCatalogoProducto.Color:
            {
                var entidad = await _context.Colores.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                return entidad is null ? null : MapColor(entidad);
            }
            case TipoCatalogoProducto.Talla:
            {
                var entidad = await _context.Tallas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                return entidad is null ? null : MapTalla(entidad);
            }
            default:
                return null;
        }
    }

    public async Task<MaestroProductoRegistro?> GetByIdConRelacionesAsync(TipoCatalogoProducto tipo, int id)
    {
        MaestroProductoRegistro? resultado;
        switch (tipo)
        {
            case TipoCatalogoProducto.Marca:
            {
                var entidad = await _context.Marcas.AsNoTracking().Include(x => x.Modelos).FirstOrDefaultAsync(x => x.Id == id);
                resultado = entidad is null ? null : MapMarca(entidad, incluirModelos: true);
                break;
            }
            case TipoCatalogoProducto.Modelo:
            {
                var entidad = await _context.Modelos.AsNoTracking().Include(x => x.Marca).FirstOrDefaultAsync(x => x.Id == id);
                resultado = entidad is null ? null : MapModelo(entidad);
                break;
            }
            case TipoCatalogoProducto.Color:
            {
                var entidad = await _context.Colores.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                resultado = entidad is null ? null : MapColor(entidad);
                break;
            }
            case TipoCatalogoProducto.Talla:
            {
                var entidad = await _context.Tallas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                resultado = entidad is null ? null : MapTalla(entidad);
                break;
            }
            default:
                resultado = null;
                break;
        }

        if (resultado is not null)
            await CargarUsosProductoAsync(tipo, new List<MaestroProductoRegistro> { resultado });

        return resultado;
    }

    public Task<bool> ExisteNombreAsync(
        TipoCatalogoProducto tipo,
        string nombre,
        int? catalogoPadreId,
        int? excluirId = null)
    {
        var normalizado = nombre.Trim().ToLower();
        return tipo switch
        {
            TipoCatalogoProducto.Marca => _context.Marcas.AnyAsync(x =>
                x.Nombre.ToLower() == normalizado && (!excluirId.HasValue || x.Id != excluirId.Value)),
            TipoCatalogoProducto.Modelo => _context.Modelos.AnyAsync(x =>
                x.MarcaId == catalogoPadreId && x.Nombre.ToLower() == normalizado &&
                (!excluirId.HasValue || x.Id != excluirId.Value)),
            TipoCatalogoProducto.Color => _context.Colores.AnyAsync(x =>
                x.Nombre.ToLower() == normalizado && (!excluirId.HasValue || x.Id != excluirId.Value)),
            TipoCatalogoProducto.Talla => _context.Tallas.AnyAsync(x =>
                x.Nombre.ToLower() == normalizado && (!excluirId.HasValue || x.Id != excluirId.Value)),
            _ => Task.FromResult(false)
        };
    }

    public async Task<int> AddAsync(MaestroProductoRegistro catalogo)
    {
        switch (catalogo.Tipo)
        {
            case TipoCatalogoProducto.Marca:
            {
                var entidad = CrearMarca(catalogo);
                await _context.Marcas.AddAsync(entidad);
                await _context.SaveChangesAsync();
                catalogo.Id = entidad.Id;
                break;
            }
            case TipoCatalogoProducto.Modelo:
            {
                var entidad = CrearModelo(catalogo);
                await _context.Modelos.AddAsync(entidad);
                await _context.SaveChangesAsync();
                catalogo.Id = entidad.Id;
                break;
            }
            case TipoCatalogoProducto.Color:
            {
                var entidad = CrearColor(catalogo);
                await _context.Colores.AddAsync(entidad);
                await _context.SaveChangesAsync();
                catalogo.Id = entidad.Id;
                break;
            }
            case TipoCatalogoProducto.Talla:
            {
                var entidad = CrearTalla(catalogo);
                await _context.Tallas.AddAsync(entidad);
                await _context.SaveChangesAsync();
                catalogo.Id = entidad.Id;
                break;
            }
            default:
                throw new InvalidOperationException($"Tipo de catálogo no soportado: {catalogo.Tipo}.");
        }

        return catalogo.Id;
    }

    public async Task<bool> UpdateAsync(MaestroProductoRegistro catalogo)
    {
        switch (catalogo.Tipo)
        {
            case TipoCatalogoProducto.Marca:
            {
                var entidad = await _context.Marcas.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == catalogo.Id);
                if (entidad is null) return false;
                entidad.Nombre = catalogo.Nombre;
                entidad.Descripcion = catalogo.Descripcion;
                entidad.Orden = catalogo.Orden;
                entidad.Activo = catalogo.Activo;
                AplicarAuditoria(entidad, catalogo);
                break;
            }
            case TipoCatalogoProducto.Modelo:
            {
                var entidad = await _context.Modelos.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == catalogo.Id);
                if (entidad is null) return false;
                entidad.MarcaId = catalogo.CatalogoPadreId
                    ?? throw new InvalidOperationException("Todo modelo normalizado requiere MarcaId.");
                entidad.Nombre = catalogo.Nombre;
                entidad.Descripcion = catalogo.Descripcion;
                entidad.Orden = catalogo.Orden;
                entidad.Activo = catalogo.Activo;
                AplicarAuditoria(entidad, catalogo);
                break;
            }
            case TipoCatalogoProducto.Color:
            {
                var entidad = await _context.Colores.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == catalogo.Id);
                if (entidad is null) return false;
                entidad.Nombre = catalogo.Nombre;
                entidad.Descripcion = catalogo.Descripcion;
                entidad.CodigoVisual = catalogo.CodigoVisual;
                entidad.Orden = catalogo.Orden;
                entidad.Activo = catalogo.Activo;
                AplicarAuditoria(entidad, catalogo);
                break;
            }
            case TipoCatalogoProducto.Talla:
            {
                var entidad = await _context.Tallas.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == catalogo.Id);
                if (entidad is null) return false;
                entidad.Nombre = catalogo.Nombre;
                entidad.Descripcion = catalogo.Descripcion;
                entidad.Orden = catalogo.Orden;
                entidad.Activo = catalogo.Activo;
                AplicarAuditoria(entidad, catalogo);
                break;
            }
            default:
                return false;
        }

        return await _context.SaveChangesAsync() > 0;
    }

    private async Task<List<MaestroProductoRegistro>> ConsultarNormalizadosAsync(
        TipoCatalogoProducto tipo,
        string? buscar,
        int? catalogoPadreId,
        bool soloActivos)
    {
        var termino = string.IsNullOrWhiteSpace(buscar) ? null : buscar.Trim();

        switch (tipo)
        {
            case TipoCatalogoProducto.Marca:
            {
                var query = _context.Marcas.AsNoTracking().Include(x => x.Modelos).AsQueryable();
                if (soloActivos) query = query.Where(x => x.Activo);
                if (termino is not null)
                    query = query.Where(x => x.Nombre.Contains(termino) || (x.Descripcion != null && x.Descripcion.Contains(termino)));
                var rows = await query.OrderBy(x => x.Orden).ThenBy(x => x.Nombre).ToListAsync();
                return rows.Select(x => MapMarca(x, incluirModelos: true)).ToList();
            }
            case TipoCatalogoProducto.Modelo:
            {
                var query = _context.Modelos.AsNoTracking().Include(x => x.Marca).AsQueryable();
                if (soloActivos) query = query.Where(x => x.Activo);
                if (catalogoPadreId.HasValue) query = query.Where(x => x.MarcaId == catalogoPadreId.Value);
                if (termino is not null)
                    query = query.Where(x => x.Nombre.Contains(termino) || (x.Descripcion != null && x.Descripcion.Contains(termino)));
                var rows = await query.OrderBy(x => x.Orden).ThenBy(x => x.Nombre).ToListAsync();
                return rows.Select(MapModelo).ToList();
            }
            case TipoCatalogoProducto.Color:
            {
                var query = _context.Colores.AsNoTracking().AsQueryable();
                if (soloActivos) query = query.Where(x => x.Activo);
                if (termino is not null)
                    query = query.Where(x => x.Nombre.Contains(termino) || (x.Descripcion != null && x.Descripcion.Contains(termino)));
                var rows = await query.OrderBy(x => x.Orden).ThenBy(x => x.Nombre).ToListAsync();
                return rows.Select(MapColor).ToList();
            }
            case TipoCatalogoProducto.Talla:
            {
                var query = _context.Tallas.AsNoTracking().AsQueryable();
                if (soloActivos) query = query.Where(x => x.Activo);
                if (termino is not null)
                    query = query.Where(x => x.Nombre.Contains(termino) || (x.Descripcion != null && x.Descripcion.Contains(termino)));
                var rows = await query.OrderBy(x => x.Orden).ThenBy(x => x.Nombre).ToListAsync();
                return rows.Select(MapTalla).ToList();
            }
            default:
                return new List<MaestroProductoRegistro>();
        }
    }

    private async Task CargarUsosProductoAsync(
        TipoCatalogoProducto tipo,
        List<MaestroProductoRegistro> catalogos)
    {
        if (catalogos.Count == 0) return;
        var ids = catalogos.Select(x => x.Id).ToHashSet();
        Dictionary<int, int> totales;

        switch (tipo)
        {
            case TipoCatalogoProducto.Marca:
            {
                var pares = await _context.ProductoVariantes.AsNoTracking()
                    .Where(v => !v.Eliminado && v.MarcaId.HasValue && ids.Contains(v.MarcaId.Value))
                    .Select(v => new { CatalogoId = v.MarcaId!.Value, v.ProductoId })
                    .Distinct()
                    .ToListAsync();
                totales = pares.GroupBy(x => x.CatalogoId).ToDictionary(g => g.Key, g => g.Count());
                break;
            }
            case TipoCatalogoProducto.Modelo:
            {
                var pares = await _context.ProductoVariantes.AsNoTracking()
                    .Where(v => !v.Eliminado && v.ModeloId.HasValue && ids.Contains(v.ModeloId.Value))
                    .Select(v => new { CatalogoId = v.ModeloId!.Value, v.ProductoId })
                    .Distinct()
                    .ToListAsync();
                totales = pares.GroupBy(x => x.CatalogoId).ToDictionary(g => g.Key, g => g.Count());
                break;
            }
            case TipoCatalogoProducto.Color:
            {
                var pares = await _context.ProductoVariantes.AsNoTracking()
                    .Where(v => !v.Eliminado && v.ColorId.HasValue && ids.Contains(v.ColorId.Value))
                    .Select(v => new { CatalogoId = v.ColorId!.Value, v.ProductoId })
                    .Distinct()
                    .ToListAsync();
                totales = pares.GroupBy(x => x.CatalogoId).ToDictionary(g => g.Key, g => g.Count());
                break;
            }
            case TipoCatalogoProducto.Talla:
            {
                var pares = await _context.ProductoVariantes.AsNoTracking()
                    .Where(v => !v.Eliminado && v.TallaId.HasValue && ids.Contains(v.TallaId.Value))
                    .Select(v => new { CatalogoId = v.TallaId!.Value, v.ProductoId })
                    .Distinct()
                    .ToListAsync();
                totales = pares.GroupBy(x => x.CatalogoId).ToDictionary(g => g.Key, g => g.Count());
                break;
            }
            default:
                return;
        }

        foreach (var catalogo in catalogos)
            catalogo.TotalProductos = totales.GetValueOrDefault(catalogo.Id);
    }

    private static Marca CrearMarca(MaestroProductoRegistro x) => new()
    {
        Nombre = x.Nombre,
        Descripcion = x.Descripcion,
        Orden = x.Orden,
        Activo = x.Activo,
        Eliminado = x.Eliminado,
        FechaEliminacion = x.FechaEliminacion,
        EliminadoPorUsuarioId = x.EliminadoPorUsuarioId,
        CreadoPorUsuarioId = x.CreadoPorUsuarioId,
        CreadoPorNombreUsuario = x.CreadoPorNombreUsuario,
        ActualizadoPorUsuarioId = x.ActualizadoPorUsuarioId,
        ActualizadoPorNombreUsuario = x.ActualizadoPorNombreUsuario,
        FechaCreacion = x.FechaCreacion,
        FechaActualizacion = x.FechaActualizacion
    };

    private static Modelo CrearModelo(MaestroProductoRegistro x) => new()
    {
        MarcaId = x.CatalogoPadreId ?? throw new InvalidOperationException("Todo modelo normalizado requiere MarcaId."),
        Nombre = x.Nombre,
        Descripcion = x.Descripcion,
        Orden = x.Orden,
        Activo = x.Activo,
        Eliminado = x.Eliminado,
        FechaEliminacion = x.FechaEliminacion,
        EliminadoPorUsuarioId = x.EliminadoPorUsuarioId,
        CreadoPorUsuarioId = x.CreadoPorUsuarioId,
        CreadoPorNombreUsuario = x.CreadoPorNombreUsuario,
        ActualizadoPorUsuarioId = x.ActualizadoPorUsuarioId,
        ActualizadoPorNombreUsuario = x.ActualizadoPorNombreUsuario,
        FechaCreacion = x.FechaCreacion,
        FechaActualizacion = x.FechaActualizacion
    };

    private static Color CrearColor(MaestroProductoRegistro x) => new()
    {
        Nombre = x.Nombre,
        Descripcion = x.Descripcion,
        CodigoVisual = x.CodigoVisual,
        Orden = x.Orden,
        Activo = x.Activo,
        Eliminado = x.Eliminado,
        FechaEliminacion = x.FechaEliminacion,
        EliminadoPorUsuarioId = x.EliminadoPorUsuarioId,
        CreadoPorUsuarioId = x.CreadoPorUsuarioId,
        CreadoPorNombreUsuario = x.CreadoPorNombreUsuario,
        ActualizadoPorUsuarioId = x.ActualizadoPorUsuarioId,
        ActualizadoPorNombreUsuario = x.ActualizadoPorNombreUsuario,
        FechaCreacion = x.FechaCreacion,
        FechaActualizacion = x.FechaActualizacion
    };

    private static Talla CrearTalla(MaestroProductoRegistro x) => new()
    {
        Nombre = x.Nombre,
        Descripcion = x.Descripcion,
        Orden = x.Orden,
        Activo = x.Activo,
        Eliminado = x.Eliminado,
        FechaEliminacion = x.FechaEliminacion,
        EliminadoPorUsuarioId = x.EliminadoPorUsuarioId,
        CreadoPorUsuarioId = x.CreadoPorUsuarioId,
        CreadoPorNombreUsuario = x.CreadoPorNombreUsuario,
        ActualizadoPorUsuarioId = x.ActualizadoPorUsuarioId,
        ActualizadoPorNombreUsuario = x.ActualizadoPorNombreUsuario,
        FechaCreacion = x.FechaCreacion,
        FechaActualizacion = x.FechaActualizacion
    };

    private static void AplicarAuditoria(Marca entidad, MaestroProductoRegistro x)
    {
        entidad.Eliminado = x.Eliminado;
        entidad.FechaEliminacion = x.FechaEliminacion;
        entidad.EliminadoPorUsuarioId = x.EliminadoPorUsuarioId;
        entidad.ActualizadoPorUsuarioId = x.ActualizadoPorUsuarioId;
        entidad.ActualizadoPorNombreUsuario = x.ActualizadoPorNombreUsuario;
        entidad.FechaActualizacion = x.FechaActualizacion;
    }

    private static void AplicarAuditoria(Modelo entidad, MaestroProductoRegistro x)
    {
        entidad.Eliminado = x.Eliminado;
        entidad.FechaEliminacion = x.FechaEliminacion;
        entidad.EliminadoPorUsuarioId = x.EliminadoPorUsuarioId;
        entidad.ActualizadoPorUsuarioId = x.ActualizadoPorUsuarioId;
        entidad.ActualizadoPorNombreUsuario = x.ActualizadoPorNombreUsuario;
        entidad.FechaActualizacion = x.FechaActualizacion;
    }

    private static void AplicarAuditoria(Color entidad, MaestroProductoRegistro x)
    {
        entidad.Eliminado = x.Eliminado;
        entidad.FechaEliminacion = x.FechaEliminacion;
        entidad.EliminadoPorUsuarioId = x.EliminadoPorUsuarioId;
        entidad.ActualizadoPorUsuarioId = x.ActualizadoPorUsuarioId;
        entidad.ActualizadoPorNombreUsuario = x.ActualizadoPorNombreUsuario;
        entidad.FechaActualizacion = x.FechaActualizacion;
    }

    private static void AplicarAuditoria(Talla entidad, MaestroProductoRegistro x)
    {
        entidad.Eliminado = x.Eliminado;
        entidad.FechaEliminacion = x.FechaEliminacion;
        entidad.EliminadoPorUsuarioId = x.EliminadoPorUsuarioId;
        entidad.ActualizadoPorUsuarioId = x.ActualizadoPorUsuarioId;
        entidad.ActualizadoPorNombreUsuario = x.ActualizadoPorNombreUsuario;
        entidad.FechaActualizacion = x.FechaActualizacion;
    }

    private static MaestroProductoRegistro MapMarca(Marca x, bool incluirModelos) => new()
    {
        Id = x.Id,
        Tipo = TipoCatalogoProducto.Marca,
        Nombre = x.Nombre,
        Descripcion = x.Descripcion,
        Orden = x.Orden,
        Activo = x.Activo,
        Eliminado = x.Eliminado,
        FechaEliminacion = x.FechaEliminacion,
        EliminadoPorUsuarioId = x.EliminadoPorUsuarioId,
        TotalModelos = incluirModelos ? x.Modelos.Count : 0,
        TotalModelosActivos = incluirModelos ? x.Modelos.Count(m => m.Activo && !m.Eliminado) : 0,
        CreadoPorUsuarioId = x.CreadoPorUsuarioId,
        CreadoPorNombreUsuario = x.CreadoPorNombreUsuario,
        ActualizadoPorUsuarioId = x.ActualizadoPorUsuarioId,
        ActualizadoPorNombreUsuario = x.ActualizadoPorNombreUsuario,
        FechaCreacion = x.FechaCreacion,
        FechaActualizacion = x.FechaActualizacion
    };

    private static MaestroProductoRegistro MapModelo(Modelo x) => new()
    {
        Id = x.Id,
        Tipo = TipoCatalogoProducto.Modelo,
        Nombre = x.Nombre,
        Descripcion = x.Descripcion,
        Orden = x.Orden,
        Activo = x.Activo,
        Eliminado = x.Eliminado,
        FechaEliminacion = x.FechaEliminacion,
        EliminadoPorUsuarioId = x.EliminadoPorUsuarioId,
        CatalogoPadreId = x.MarcaId,
        CatalogoPadreNombre = x.Marca?.Nombre,
        CatalogoPadreActivo = x.Marca?.Activo,
        CreadoPorUsuarioId = x.CreadoPorUsuarioId,
        CreadoPorNombreUsuario = x.CreadoPorNombreUsuario,
        ActualizadoPorUsuarioId = x.ActualizadoPorUsuarioId,
        ActualizadoPorNombreUsuario = x.ActualizadoPorNombreUsuario,
        FechaCreacion = x.FechaCreacion,
        FechaActualizacion = x.FechaActualizacion
    };

    private static MaestroProductoRegistro MapColor(Color x) => new()
    {
        Id = x.Id,
        Tipo = TipoCatalogoProducto.Color,
        Nombre = x.Nombre,
        Descripcion = x.Descripcion,
        CodigoVisual = x.CodigoVisual,
        Orden = x.Orden,
        Activo = x.Activo,
        Eliminado = x.Eliminado,
        FechaEliminacion = x.FechaEliminacion,
        EliminadoPorUsuarioId = x.EliminadoPorUsuarioId,
        CreadoPorUsuarioId = x.CreadoPorUsuarioId,
        CreadoPorNombreUsuario = x.CreadoPorNombreUsuario,
        ActualizadoPorUsuarioId = x.ActualizadoPorUsuarioId,
        ActualizadoPorNombreUsuario = x.ActualizadoPorNombreUsuario,
        FechaCreacion = x.FechaCreacion,
        FechaActualizacion = x.FechaActualizacion
    };

    private static MaestroProductoRegistro MapTalla(Talla x) => new()
    {
        Id = x.Id,
        Tipo = TipoCatalogoProducto.Talla,
        Nombre = x.Nombre,
        Descripcion = x.Descripcion,
        Orden = x.Orden,
        Activo = x.Activo,
        Eliminado = x.Eliminado,
        FechaEliminacion = x.FechaEliminacion,
        EliminadoPorUsuarioId = x.EliminadoPorUsuarioId,
        CreadoPorUsuarioId = x.CreadoPorUsuarioId,
        CreadoPorNombreUsuario = x.CreadoPorNombreUsuario,
        ActualizadoPorUsuarioId = x.ActualizadoPorUsuarioId,
        ActualizadoPorNombreUsuario = x.ActualizadoPorNombreUsuario,
        FechaCreacion = x.FechaCreacion,
        FechaActualizacion = x.FechaActualizacion
    };
}
