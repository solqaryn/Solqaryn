using Solqaryn.Application.Common;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public class FacturaCompartirRepository : IFacturaCompartirRepository
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public FacturaCompartirRepository(
        AppDbContext context,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<EnlacePublicoFactura?> GetPorTokenHashAsync(string tokenHash)
    {
        var enlace = await _context.EnlacesPublicosFactura
            .FirstOrDefaultAsync(e => e.Token == tokenHash);

        // La marca vive exclusivamente durante esta solicitud. El servicio aún
        // valida expiración, revocación y límite de accesos antes de solicitar el
        // PDF; nunca se almacena el token completo.
        if (enlace is not null && _httpContextAccessor?.HttpContext is { } httpContext)
            httpContext.Items[PublicInvoiceAccessContext.FacturaIdKey] = enlace.FacturaId;

        return enlace;
    }

    public async Task<int> ExpirarVigentesAsync(int facturaId, DateTime fechaExpiracion)
    {
        var enlaces = await _context.EnlacesPublicosFactura
            .Where(e => e.FacturaId == facturaId && e.FechaExpiracion > fechaExpiracion)
            .ToListAsync();

        foreach (var enlace in enlaces)
            enlace.FechaExpiracion = fechaExpiracion;

        return enlaces.Count;
    }

    public async Task AddEnlaceAsync(EnlacePublicoFactura enlace) =>
        await _context.EnlacesPublicosFactura.AddAsync(enlace);

    public void UpdateEnlace(EnlacePublicoFactura enlace) =>
        _context.EnlacesPublicosFactura.Update(enlace);

    public async Task AddHistorialAsync(HistorialEnvioFactura historial) =>
        await _context.HistorialEnviosFactura.AddAsync(historial);

    public async Task<List<HistorialEnvioFactura>> GetHistorialAsync(int facturaId) =>
        await _context.HistorialEnviosFactura
            .Where(h => h.FacturaId == facturaId)
            .OrderByDescending(h => h.Fecha)
            .ToListAsync();

    public async Task SaveChangesAsync() =>
        await _context.SaveChangesAsync();
}
