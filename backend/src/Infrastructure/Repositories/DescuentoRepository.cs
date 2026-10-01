using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public class DescuentoRepository : IDescuentoRepository
{
    private readonly AppDbContext _context;
    public DescuentoRepository(AppDbContext context) => _context = context;

    private IQueryable<Descuento> ConRelaciones() => _context.Descuentos
        .Include(d => d.Productos)
        .Include(d => d.Categorias)
        .Include(d => d.Clientes)
        .Include(d => d.Roles);

    public async Task<Descuento?> GetByIdAsync(int id) =>
        await _context.Descuentos.FirstOrDefaultAsync(d => d.Id == id && !d.Eliminado);

    public async Task<Descuento?> GetByIdConRelacionesAsync(int id) =>
        await ConRelaciones().FirstOrDefaultAsync(d => d.Id == id && !d.Eliminado);

    public async Task<List<Descuento>> GetAllAsync(bool incluirEliminados = false) =>
        await _context.Descuentos.Where(d => incluirEliminados || !d.Eliminado)
            .OrderByDescending(d => d.FechaCreacion).ToListAsync();

    public async Task<bool> ExisteCodigoAsync(string codigoNormalizado, int? excluirId = null) =>
        await _context.Descuentos.AnyAsync(d =>
            !d.Eliminado && d.CodigoPromocionalNormalizado == codigoNormalizado &&
            (excluirId == null || d.Id != excluirId));

    public async Task<int> ContarUsosAsync(int descuentoId) =>
        await _context.HistorialUsoDescuentos.CountAsync(h => h.DescuentoId == descuentoId);

    public async Task<List<Descuento>> GetVigentesConRelacionesAsync(DateTime fecha) =>
        await ConRelaciones().Where(d =>
            d.Activo && !d.Eliminado &&
            (d.FechaInicio == null || d.FechaInicio <= fecha) &&
            (d.FechaFin == null || d.FechaFin >= fecha)
        ).OrderBy(d => d.Prioridad).ToListAsync();

    public async Task AddAsync(Descuento descuento) => await _context.Descuentos.AddAsync(descuento);
    public void Update(Descuento descuento) => _context.Descuentos.Update(descuento);
    public void Remove(Descuento descuento) => _context.Descuentos.Remove(descuento);

    public async Task AddHistorialAsync(HistorialUsoDescuento historial) =>
        await _context.HistorialUsoDescuentos.AddAsync(historial);

    public async Task<int> ContarUsosPorClienteAsync(int descuentoId, int clienteId) =>
        await _context.HistorialUsoDescuentos.CountAsync(h => h.DescuentoId == descuentoId && h.ClienteId == clienteId);

    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}
