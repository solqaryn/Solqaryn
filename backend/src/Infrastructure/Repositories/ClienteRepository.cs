using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public class ClienteRepository : IClienteRepository
{
    private readonly AppDbContext _context;

    public ClienteRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Cliente?> GetByIdAsync(int id) =>
        await _context.Clientes.Include(c => c.TipoCliente).FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Cliente?> GetByIdConVentasAsync(int id) =>
        await _context.Clientes.Include(c => c.Ventas).Include(c => c.TipoCliente).FirstOrDefaultAsync(c => c.Id == id);

    public async Task<List<Cliente>> GetAllAsync() =>
        await _context.Clientes.Include(c => c.Ventas).Include(c => c.TipoCliente).OrderBy(c => c.Nombre).ToListAsync();

    public async Task<List<Cliente>> GetActivosAsync() =>
        await _context.Clientes.Include(c => c.TipoCliente).Where(c => c.Activo).OrderBy(c => c.Nombre).ToListAsync();

    public async Task<List<Cliente>> BuscarActivosAsync(string termino, int limite = 10)
    {
        var normalizado = termino.Trim().ToLower();
        if (string.IsNullOrWhiteSpace(normalizado)) return new List<Cliente>();
        limite = Math.Clamp(limite, 1, 30);

        return await _context.Clientes
            .AsNoTracking()
            .Include(c => c.TipoCliente)
            .Where(c => c.Activo && (
                c.Nombre.ToLower().Contains(normalizado) ||
                (c.IdentidadORTN != null && c.IdentidadORTN.ToLower().Contains(normalizado)) ||
                (c.Correo != null && c.Correo.ToLower().Contains(normalizado)) ||
                (c.Telefono != null && c.Telefono.ToLower().Contains(normalizado))))
            .OrderBy(c => c.Nombre)
            .Take(limite)
            .ToListAsync();
    }

    public async Task<Cliente?> BuscarCoincidenciaActivaAsync(string? identidadORTN, string? correo, string? telefono, string? nombre)
    {
        var identidad = NormalizarDocumento(identidadORTN);
        var email = NormalizarTexto(correo);
        var tel = NormalizarDocumento(telefono);
        var nom = NormalizarTexto(nombre);

        var query = _context.Clientes.Include(c => c.TipoCliente).Where(c => c.Activo);

        if (!string.IsNullOrEmpty(identidad))
        {
            var porIdentidad = await query.FirstOrDefaultAsync(c =>
                c.IdentidadORTN != null && c.IdentidadORTN.Replace("-", "").Replace(" ", "").ToLower() == identidad);
            if (porIdentidad is not null) return porIdentidad;
        }

        if (!string.IsNullOrEmpty(email))
        {
            var porCorreo = await query.FirstOrDefaultAsync(c => c.Correo != null && c.Correo.ToLower() == email);
            if (porCorreo is not null) return porCorreo;
        }

        if (!string.IsNullOrEmpty(tel))
        {
            var porTelefono = await query.FirstOrDefaultAsync(c =>
                c.Telefono != null && c.Telefono.Replace("-", "").Replace(" ", "").ToLower() == tel);
            if (porTelefono is not null) return porTelefono;
        }

        // El nombre solo funciona como último recurso cuando no se recibió ningún
        // identificador más fuerte; dos personas pueden compartir el mismo nombre.
        if (string.IsNullOrEmpty(identidad) && string.IsNullOrEmpty(email) && string.IsNullOrEmpty(tel) && !string.IsNullOrEmpty(nom))
            return await query.OrderBy(c => c.Id).FirstOrDefaultAsync(c => c.Nombre.ToLower() == nom);

        return null;
    }

    public async Task<bool> ExisteNombreAsync(string nombre, int? excluirId = null) =>
        await _context.Clientes.AnyAsync(c =>
            c.Nombre.ToLower() == nombre.ToLower() && (excluirId == null || c.Id != excluirId));

    public async Task<bool> ExisteIdentidadAsync(string identidadORTN, int? excluirId = null)
    {
        var normalizada = NormalizarDocumento(identidadORTN);
        if (string.IsNullOrEmpty(normalizada)) return false;

        return await _context.Clientes.AnyAsync(c =>
            c.IdentidadORTN != null &&
            c.IdentidadORTN.Replace("-", "").Replace(" ", "").ToLower() == normalizada &&
            (excluirId == null || c.Id != excluirId));
    }

    public async Task AddAsync(Cliente cliente) =>
        await _context.Clientes.AddAsync(cliente);

    public void Update(Cliente cliente) =>
        _context.Clientes.Update(cliente);

    public void Remove(Cliente cliente) =>
        _context.Clientes.Remove(cliente);

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;

    private static string NormalizarTexto(string? value) => value?.Trim().ToLower() ?? string.Empty;

    private static string NormalizarDocumento(string? value) =>
        value?.Replace("-", "").Replace(" ", "").Trim().ToLower() ?? string.Empty;
}
