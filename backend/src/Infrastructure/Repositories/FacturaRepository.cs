using Solqaryn.Application.Common;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using CatalogoBanco = Solqaryn.Domain.Entities.Catalogos.Banco;
using CatalogoMetodoPago = Solqaryn.Domain.Entities.Catalogos.MetodoPago;
namespace Solqaryn.Infrastructure.Repositories;
public class FacturaRepository : IFacturaRepository
{
    private readonly AppDbContext _context; private readonly IUsuarioScopeService _usuarioScope; private readonly IHttpContextAccessor? _httpContextAccessor;
    public FacturaRepository(AppDbContext context, IUsuarioScopeService usuarioScope, IHttpContextAccessor? httpContextAccessor = null) { _context=context; _usuarioScope=usuarioScope; _httpContextAccessor=httpContextAccessor; }
    private IQueryable<Factura> ConIncludes()=>_context.Facturas.Include(f=>f.Detalles).Include(f=>f.Pagos).ThenInclude(p=>p.MetodoPagoCatalogo).Include(f=>f.Pagos).ThenInclude(p=>p.Banco).Include(f=>f.Venta).ThenInclude(v=>v!.MetodoPagoCatalogo).Include(f=>f.Venta).ThenInclude(v=>v!.DescuentosAplicados).Include(f=>f.Venta).ThenInclude(v=>v!.ImpuestosAplicados);
    private static IQueryable<Factura> AplicarAlcance(IQueryable<Factura> query, UsuarioScopeActual? alcance){ if(alcance is null)return query.Where(_=>false); return alcance.EsAdministrador?query:query.Where(f=>f.VendedorUsuarioId==alcance.UsuarioId||f.GeneradaPorUsuarioId==alcance.UsuarioId||(f.Venta!=null&&f.Venta.CreadoPorUsuarioId==alcance.UsuarioId)); }
    public async Task<Factura?> GetByIdAsync(int id){var a=await _usuarioScope.ObtenerActualAsync(); if(a is null&&TieneAccesoPublicoValidado(id)) return await GetByIdParaEnlacePublicoValidadoAsync(id); return await AplicarAlcance(ConIncludes(),a).FirstOrDefaultAsync(f=>f.Id==id);}
    public async Task<Factura?> GetByVentaIdAsync(int ventaId){var a=await _usuarioScope.ObtenerActualAsync();return await AplicarAlcance(ConIncludes(),a).FirstOrDefaultAsync(f=>f.VentaId==ventaId);}
    public async Task<List<Factura>> GetAllAsync(){var a=await _usuarioScope.ObtenerActualAsync();return await AplicarAlcance(ConIncludes(),a).OrderByDescending(f=>f.FechaEmision).ToListAsync();}
    public async Task<CatalogoMetodoPago?> GetMetodoPagoPorCodigoONombreAsync(string valor){var n=valor.Trim().ToUpper();return await _context.Set<CatalogoMetodoPago>().AsTracking().FirstOrDefaultAsync(m=>m.Activo&&!m.Eliminado&&(m.Codigo.ToUpper()==n||m.Nombre.ToUpper()==n));}
    public Task<CatalogoBanco?> GetBancoActivoPorIdAsync(int id)=>_context.Set<CatalogoBanco>().AsTracking().FirstOrDefaultAsync(b=>b.Id==id&&b.Activo&&!b.Eliminado);
    public Task<List<CatalogoBanco>> GetBancosActivosAsync()=>_context.Set<CatalogoBanco>().AsNoTracking().Where(b=>b.Activo&&!b.Eliminado).OrderBy(b=>b.Nombre).ThenBy(b=>b.Codigo).ThenBy(b=>b.Id).ToListAsync();
    public async Task<Factura?> GetByIdParaEnlacePublicoValidadoAsync(int id)=>TieneAccesoPublicoValidado(id)?await ConIncludes().FirstOrDefaultAsync(f=>f.Id==id):null;
    public async Task AddAsync(Factura factura){FacturaDetalleDistribuidor.Aplicar(factura);await _context.Facturas.AddAsync(factura);} public void Update(Factura factura)=>_context.Facturas.Update(factura); public async Task<bool> SaveChangesAsync()=>await _context.SaveChangesAsync()>0;
    private bool TieneAccesoPublicoValidado(int facturaId){var items=_httpContextAccessor?.HttpContext?.Items;return items is not null&&items.TryGetValue(PublicInvoiceAccessContext.FacturaIdKey,out var value)&&value is int autorizado&&autorizado==facturaId;}
}
