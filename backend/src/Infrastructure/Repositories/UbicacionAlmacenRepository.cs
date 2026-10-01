using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public sealed class UbicacionAlmacenRepository : IUbicacionAlmacenRepository
{
    private const int ProfundidadJerarquiaMaxima = 512;
    private readonly AppDbContext _context;

    public UbicacionAlmacenRepository(AppDbContext context)
    {
        _context = context;
    }

    private DbSet<UbicacionAlmacen> Ubicaciones => _context.Set<UbicacionAlmacen>();

    public async Task<UbicacionAlmacen?> GetByIdAsync(int id) =>
        await Ubicaciones
            .Include(u => u.Almacen)
                .ThenInclude(a => a.Sucursal)
            .Include(u => u.UbicacionPadre)
            .FirstOrDefaultAsync(u => u.Id == id);

    public async Task<(List<UbicacionAlmacen> Items, int Total)> BuscarAsync(
        string? termino,
        int? almacenId,
        int? ubicacionPadreId,
        bool soloRaiz,
        TipoUbicacionAlmacen? tipo,
        bool? activa,
        int pagina,
        int tamanoPagina)
    {
        var query = Ubicaciones
            .AsNoTracking()
            .Include(u => u.Almacen)
                .ThenInclude(a => a.Sucursal)
            .Include(u => u.UbicacionPadre)
            .AsQueryable();

        if (almacenId.HasValue)
            query = query.Where(u => u.AlmacenId == almacenId.Value);

        if (soloRaiz)
            query = query.Where(u => u.UbicacionPadreId == null);
        else if (ubicacionPadreId.HasValue)
            query = query.Where(u => u.UbicacionPadreId == ubicacionPadreId.Value);

        if (tipo.HasValue)
            query = query.Where(u => u.Tipo == tipo.Value);

        if (activa.HasValue)
            query = query.Where(u => u.Activa == activa.Value);

        if (!string.IsNullOrWhiteSpace(termino))
        {
            var valor = termino.Trim();
            query = query.Where(u =>
                u.Codigo.Contains(valor) ||
                u.Nombre.Contains(valor) ||
                u.Almacen.Codigo.Contains(valor) ||
                u.Almacen.Nombre.Contains(valor) ||
                (u.UbicacionPadre != null &&
                    (u.UbicacionPadre.Codigo.Contains(valor) || u.UbicacionPadre.Nombre.Contains(valor))));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(u => u.Almacen.Codigo)
            .ThenBy(u => u.UbicacionPadreId.HasValue)
            .ThenBy(u => u.UbicacionPadreId)
            .ThenBy(u => u.Codigo)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();

        return (items, total);
    }

    public async Task<List<UbicacionAlmacen>> GetActivasAsync(int? almacenId = null, int? ubicacionPadreId = null)
    {
        var query = Ubicaciones
            .AsNoTracking()
            .Include(u => u.Almacen)
                .ThenInclude(a => a.Sucursal)
            .Include(u => u.UbicacionPadre)
            .Where(u =>
                u.Activa &&
                u.Almacen.Activo &&
                u.Almacen.Sucursal.Activa &&
                (u.UbicacionPadreId == null || (u.UbicacionPadre != null && u.UbicacionPadre.Activa)));

        if (almacenId.HasValue)
            query = query.Where(u => u.AlmacenId == almacenId.Value);

        if (ubicacionPadreId.HasValue)
            query = query.Where(u => u.UbicacionPadreId == ubicacionPadreId.Value);

        return await query
            .OrderBy(u => u.Almacen.Codigo)
            .ThenBy(u => u.Codigo)
            .ThenBy(u => u.Nombre)
            .ToListAsync();
    }

    public async Task<bool> ExisteCodigoAsync(int almacenId, string codigo, int? excluirId = null)
    {
        var normalizado = codigo.Trim().ToUpper();
        return await Ubicaciones.AnyAsync(u =>
            u.AlmacenId == almacenId &&
            u.Codigo.ToUpper() == normalizado &&
            (!excluirId.HasValue || u.Id != excluirId.Value));
    }

    public Task<bool> TieneHijasActivasAsync(int ubicacionId) =>
        Ubicaciones.AnyAsync(u => u.UbicacionPadreId == ubicacionId && u.Activa);

    public Task<bool> TieneHijasNoEliminadasAsync(int ubicacionId) =>
        Ubicaciones.AnyAsync(u => u.UbicacionPadreId == ubicacionId);

    public async Task<bool> CreariaCicloAsync(int ubicacionId, int almacenId, int? nuevoPadreId)
    {
        if (!nuevoPadreId.HasValue)
            return false;
        if (nuevoPadreId.Value == ubicacionId)
            return true;

        var visitados = new HashSet<int> { ubicacionId };
        int? cursor = nuevoPadreId;

        for (var profundidad = 0; cursor.HasValue && profundidad < ProfundidadJerarquiaMaxima; profundidad++)
        {
            if (!visitados.Add(cursor.Value))
                return true;

            var nodo = await Ubicaciones
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(u => u.Id == cursor.Value && u.AlmacenId == almacenId)
                .Select(u => new { u.UbicacionPadreId })
                .FirstOrDefaultAsync();

            if (nodo is null)
                return false;

            cursor = nodo.UbicacionPadreId;
        }

        // Una cadena que exceda el límite defensivo se considera insegura.
        return cursor.HasValue;
    }

    public Task AddAsync(UbicacionAlmacen ubicacion) =>
        Ubicaciones.AddAsync(ubicacion).AsTask();

    public void Update(UbicacionAlmacen ubicacion) =>
        Ubicaciones.Update(ubicacion);

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;
}
