using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public class ProveedorRepository : IProveedorRepository
{
    private readonly AppDbContext _context;

    public ProveedorRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Proveedor?> GetByIdAsync(int id) =>
        await _context.Proveedores.FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Proveedor?> GetByIdConComprasAsync(int id) =>
        await _context.Proveedores.Include(p => p.Compras).FirstOrDefaultAsync(p => p.Id == id);

    public async Task<List<Proveedor>> GetAllAsync() =>
        await _context.Proveedores.Include(p => p.Compras).OrderBy(p => p.Nombre).ToListAsync();

    public async Task<List<Proveedor>> GetActivosAsync() =>
        await _context.Proveedores.Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync();

    public async Task<List<Proveedor>> BuscarActivosAsync(string termino, int limite = 10)
    {
        var normalizado = termino.Trim().ToLower();
        if (string.IsNullOrWhiteSpace(normalizado)) return new List<Proveedor>();
        limite = Math.Clamp(limite, 1, 30);

        return await _context.Proveedores
            .AsNoTracking()
            .Where(p => p.Activo && (
                p.Nombre.ToLower().Contains(normalizado) ||
                (p.Documento != null && p.Documento.ToLower().Contains(normalizado)) ||
                (p.Correo != null && p.Correo.ToLower().Contains(normalizado)) ||
                (p.Telefono != null && p.Telefono.ToLower().Contains(normalizado))))
            .OrderBy(p => p.Nombre)
            .Take(limite)
            .ToListAsync();
    }

    public async Task<Proveedor?> BuscarCoincidenciaActivaAsync(string? documento, string? correo, string? telefono, string? nombre)
    {
        var doc = NormalizarDocumento(documento);
        var email = NormalizarTexto(correo);
        var tel = NormalizarDocumento(telefono);
        var nom = NormalizarTexto(nombre);

        var query = _context.Proveedores.Where(p => p.Activo);

        if (!string.IsNullOrEmpty(doc))
        {
            var porDocumento = await query.FirstOrDefaultAsync(p =>
                p.Documento != null && p.Documento.Replace("-", "").Replace(" ", "").ToLower() == doc);
            if (porDocumento is not null) return porDocumento;
        }

        if (!string.IsNullOrEmpty(email))
        {
            var porCorreo = await query.FirstOrDefaultAsync(p => p.Correo != null && p.Correo.ToLower() == email);
            if (porCorreo is not null) return porCorreo;
        }

        if (!string.IsNullOrEmpty(tel))
        {
            var porTelefono = await query.FirstOrDefaultAsync(p =>
                p.Telefono != null && p.Telefono.Replace("-", "").Replace(" ", "").ToLower() == tel);
            if (porTelefono is not null) return porTelefono;
        }

        if (string.IsNullOrEmpty(doc) && string.IsNullOrEmpty(email) && string.IsNullOrEmpty(tel) && !string.IsNullOrEmpty(nom))
            return await query.OrderBy(p => p.Id).FirstOrDefaultAsync(p => p.Nombre.ToLower() == nom);

        return null;
    }

    public async Task<bool> ExisteNombreAsync(string nombre, int? excluirId = null) =>
        await _context.Proveedores.AnyAsync(p =>
            p.Nombre.ToLower() == nombre.ToLower() && (excluirId == null || p.Id != excluirId));

    public async Task<bool> ExisteDocumentoAsync(string documento, int? excluirId = null)
    {
        var normalizado = NormalizarDocumento(documento);
        if (string.IsNullOrEmpty(normalizado)) return false;

        return await _context.Proveedores.AnyAsync(p =>
            p.Documento != null &&
            p.Documento.Replace("-", "").Replace(" ", "").ToLower() == normalizado &&
            (excluirId == null || p.Id != excluirId));
    }

    public async Task AddAsync(Proveedor proveedor) =>
        await _context.Proveedores.AddAsync(proveedor);

    public void Update(Proveedor proveedor) =>
        _context.Proveedores.Update(proveedor);

    public void Remove(Proveedor proveedor) =>
        _context.Proveedores.Remove(proveedor);

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;

    private static string NormalizarTexto(string? value) => value?.Trim().ToLower() ?? string.Empty;

    private static string NormalizarDocumento(string? value) =>
        value?.Replace("-", "").Replace(" ", "").Trim().ToLower() ?? string.Empty;
}
