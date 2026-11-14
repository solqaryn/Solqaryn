using Solqaryn.Application.Common;
using Solqaryn.Application.Security;
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

        if (enlace is null || enlace.FechaExpiracion <= DateTime.UtcNow)
            return null;

        // El token enlaza a una única empresa en MySQL por FK compuesta (P3).
        // Enlaces legacy sin propietario nunca autorizan un PDF multiempresa.
        var empresaToken = _context.Entry(enlace)
            .Property<int?>("EmpresaId").CurrentValue;
        if (empresaToken is not > 0)
            return null;

        if (_httpContextAccessor?.HttpContext is not { } httpContext)
            return null;

        // No permitir superponer el token de B a una autorización RBAC de A.
        var empresaRbac = VerifiedCommercialTenant.Resolve(httpContext);
        if (empresaRbac is > 0 && empresaRbac != empresaToken)
            return null;

        // Un vínculo probado por hash concede lectura SOLO de la factura
        // correlacionada, jamás permisos de administración.
        httpContext.Items[VerifiedPublicInvoiceTenant.AuthorizedEmpresaIdItemKey] =
            empresaToken.Value;
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
