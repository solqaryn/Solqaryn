using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Exceptions;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public class ImpuestoRepository : IImpuestoRepository
{
    private readonly AppDbContext _context;
    public ImpuestoRepository(AppDbContext context) => _context = context;

    private IQueryable<Impuesto> ConRelaciones() => _context.Impuestos
        .Include(i => i.Productos)
        .Include(i => i.Categorias)
        .Include(i => i.Operaciones)
        .Include(i => i.ClientesExentos)
        .Include(i => i.ProveedoresExentos);

    public async Task<Impuesto?> GetByIdAsync(int id) =>
        await _context.Impuestos.FirstOrDefaultAsync(i => i.Id == id && !i.Eliminado);

    public async Task<Impuesto?> GetByIdConRelacionesAsync(int id) =>
        await ConRelaciones().FirstOrDefaultAsync(i => i.Id == id && !i.Eliminado);

    public async Task<List<Impuesto>> GetAllAsync(bool incluirEliminados = false) =>
        await _context.Impuestos.Where(i => incluirEliminados || !i.Eliminado)
            .OrderByDescending(i => i.FechaCreacion).ToListAsync();

    public async Task<bool> ExisteCodigoAsync(string codigo, int? excluirId = null) =>
        await _context.Impuestos.AnyAsync(i =>
            !i.Eliminado && i.Codigo == codigo && (excluirId == null || i.Id != excluirId));

    public async Task<int> ContarAplicacionesAsync(int impuestoId) =>
        await _context.HistorialAplicacionImpuestos.CountAsync(h => h.ImpuestoId == impuestoId);

    public async Task<List<Impuesto>> GetVigentesConRelacionesAsync(DateTime fecha, OperacionImpuesto operacion) =>
        await ConRelaciones().Where(i =>
            i.Activo && !i.Eliminado &&
            (i.FechaInicio == null || i.FechaInicio <= fecha) &&
            (i.FechaFin == null || i.FechaFin >= fecha) &&
            i.Operaciones.Any(o => o.Operacion == operacion)
        ).OrderBy(i => i.Prioridad).ToListAsync();

    public async Task AddAsync(Impuesto impuesto)
    {
        // The verified server-side RBAC mark, never a DTO/header, is authority.
        // Check each referenced principal before tracking the new aggregate.
        if (_context.AuthorizedCommercialEmpresaId is int empresaId)
        {
            var productos = impuesto.Productos.Select(x => x.ProductoId).Distinct().ToArray();
            var categorias = impuesto.Categorias.Select(x => x.CategoriaId).Distinct().ToArray();
            var clientes = impuesto.ClientesExentos.Select(x => x.ClienteId).Distinct().ToArray();
            var proveedores = impuesto.ProveedoresExentos.Select(x => x.ProveedorId).Distinct().ToArray();

            if ((productos.Length > 0 && await _context.Set<Producto>().CountAsync(x =>
                    productos.Contains(x.Id) && x.EmpresaId == empresaId && !x.Eliminado) != productos.Length) ||
                (categorias.Length > 0 && await _context.Set<Categoria>().CountAsync(x =>
                    categorias.Contains(x.Id) && EF.Property<int?>(x, "EmpresaId") == empresaId && !x.Eliminada) != categorias.Length) ||
                (clientes.Length > 0 && await _context.Set<Cliente>().CountAsync(x =>
                    clientes.Contains(x.Id) && x.EmpresaId == empresaId) != clientes.Length) ||
                (proveedores.Length > 0 && await _context.Set<Proveedor>().CountAsync(x =>
                    proveedores.Contains(x.Id) && x.EmpresaId == empresaId) != proveedores.Length))
                throw new BusinessRuleException("El alcance fiscal contiene referencias no disponibles.");
        }
        await _context.Impuestos.AddAsync(impuesto);
    }
    public void Update(Impuesto impuesto) => _context.Impuestos.Update(impuesto);
    public void Remove(Impuesto impuesto) => _context.Impuestos.Remove(impuesto);

    public async Task AddHistorialAsync(HistorialAplicacionImpuesto historial) =>
        await _context.HistorialAplicacionImpuestos.AddAsync(historial);

    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}
