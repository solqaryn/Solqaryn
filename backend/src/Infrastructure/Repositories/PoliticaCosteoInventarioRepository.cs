using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public sealed class PoliticaCosteoInventarioRepository : IPoliticaCosteoInventarioRepository
{
    private readonly AppDbContext _context;
    private DbSet<PoliticaCosteoInventario> Politicas => _context.Set<PoliticaCosteoInventario>();

    public PoliticaCosteoInventarioRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PoliticaCosteoInventario?> GetVigenteAsync(int empresaConfiguracionId, bool tracking = false)
    {
        if (empresaConfiguracionId <= 0)
            return null;

        if (tracking && _context.Database.IsRelational())
        {
            // La columna computada forma parte del índice UNIQUE de la política vigente.
            // Bloquear esa clave serializa cambios concurrentes y evita que la API dependa
            // de una colisión de índice como mecanismo normal de concurrencia.
            var bloqueadas = await Politicas
                .FromSqlInterpolated($"SELECT * FROM `PoliticasCosteoInventario` WHERE `EmpresaConfiguracionVigenteId` = {empresaConfiguracionId} FOR UPDATE")
                .AsTracking()
                .ToListAsync();
            return bloqueadas.SingleOrDefault();
        }

        var query = tracking ? Politicas.AsTracking() : Politicas.AsNoTracking();
        return await query.SingleOrDefaultAsync(x =>
            x.EmpresaConfiguracionId == empresaConfiguracionId && x.VigenteHastaUtc == null);
    }

    public async Task<(IReadOnlyList<PoliticaCosteoInventario> Items, int Total)> GetHistorialAsync(
        int empresaConfiguracionId,
        PoliticaCosteoInventarioQueryDto filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        var query = Politicas.AsNoTracking().Where(x => x.EmpresaConfiguracionId == empresaConfiguracionId);

        if (filtro.Metodo.HasValue)
            query = query.Where(x => x.Metodo == filtro.Metodo.Value);
        if (filtro.Vigente.HasValue)
            query = filtro.Vigente.Value
                ? query.Where(x => x.VigenteHastaUtc == null)
                : query.Where(x => x.VigenteHastaUtc != null);
        if (filtro.DesdeUtc.HasValue)
            query = query.Where(x => x.VigenteDesdeUtc >= filtro.DesdeUtc.Value);
        if (filtro.HastaUtc.HasValue)
            query = query.Where(x => x.VigenteDesdeUtc <= filtro.HastaUtc.Value);

        var total = await query.CountAsync();
        var page = Math.Max(1, filtro.Page);
        var pageSize = Math.Clamp(filtro.PageSize, 1, 100);
        var items = await query
            .OrderByDescending(x => x.VigenteDesdeUtc)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, total);
    }

    public Task AddAsync(PoliticaCosteoInventario politica) => Politicas.AddAsync(politica).AsTask();
    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
