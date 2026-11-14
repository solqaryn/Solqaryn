using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using CatalogoMetodoPago = Solqaryn.Domain.Entities.Catalogos.MetodoPago;

namespace Solqaryn.Infrastructure.Repositories;

public class MovimientoFinancieroRepository : IMovimientoFinancieroRepository
{
    private readonly AppDbContext _context;
    private readonly IUsuarioScopeService _usuarioScope;

    public MovimientoFinancieroRepository(AppDbContext context, IUsuarioScopeService usuarioScope)
    {
        _context = context;
        _usuarioScope = usuarioScope;
    }

    private IQueryable<MovimientoFinanciero> ConMetodoPago() =>
        _context.MovimientosFinancieros.Include(m => m.MetodoPagoCatalogo);

    private static IQueryable<MovimientoFinanciero> AplicarAlcance(
        IQueryable<MovimientoFinanciero> query,
        UsuarioScopeActual? alcance)
    {
        if (alcance is null)
            return query.Where(_ => false);

        return alcance.EsAdministrador
            ? query
            : query.Where(m => m.CreadoPorUsuarioId == alcance.UsuarioId);
    }

    public async Task AddAsync(MovimientoFinanciero movimiento)
    {
        if (EsReversionAutomaticaDeCompra(movimiento))
        {
            var original = await ConMetodoPago()
                .Where(m =>
                    m.CompraId == movimiento.CompraId &&
                    m.EsAutomatico &&
                    m.Tipo == TipoMovimientoFinanciero.Egreso &&
                    m.Categoria == CategoriaMovimientoFinanciero.Compra)
                .OrderBy(m => m.Id)
                .FirstOrDefaultAsync()
                ?? throw new BusinessRuleException(
                    "No se encontró el movimiento financiero original de la compra; la anulación no puede conciliarse.");

            if (original.Estado == EstadoMovimientoFinanciero.Pendiente)
            {
                original.Estado = EstadoMovimientoFinanciero.Anulado;
                original.AnuladoPorUsuarioId = movimiento.CreadoPorUsuarioId;
                original.AnuladoPorNombreUsuario = movimiento.CreadoPorNombreUsuario;
                original.FechaAnulacion = DateTime.UtcNow;
                original.MotivoAnulacion = movimiento.Descripcion ?? movimiento.Concepto;
                _context.MovimientosFinancieros.Update(original);
                return;
            }

            if (original.Estado == EstadoMovimientoFinanciero.Pagado)
            {
                if (!original.MetodoPagoId.HasValue)
                    throw new BusinessRuleException(
                        "El movimiento financiero original de la compra no tiene un método de pago relacional y no puede revertirse de forma segura.");

                movimiento.Estado = EstadoMovimientoFinanciero.Pendiente;
                movimiento.MetodoPagoId = original.MetodoPagoId;
                movimiento.MetodoPagoCatalogo = original.MetodoPagoCatalogo;
                movimiento.MetodoPago = null;
                movimiento.ReferenciaId = original.Id;
                await _context.MovimientosFinancieros.AddAsync(movimiento);
                return;
            }

            throw new BusinessRuleException(
                "El movimiento financiero original de la compra ya está anulado y no admite otra reversión.");
        }

        await NormalizarMetodoPagoRelacionalAsync(movimiento);
        await _context.MovimientosFinancieros.AddAsync(movimiento);
    }

    public async Task<MovimientoFinanciero?> GetByIdAsync(int id)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        return await AplicarAlcance(ConMetodoPago(), alcance)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public void Update(MovimientoFinanciero movimiento) =>
        _context.MovimientosFinancieros.Update(movimiento);

    public async Task<MovimientoFinanciero?> GetByCompraIdAsync(int compraId)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        return await AplicarAlcance(ConMetodoPago(), alcance)
            .Where(m =>
                m.CompraId == compraId &&
                m.EsAutomatico &&
                m.Tipo == TipoMovimientoFinanciero.Egreso &&
                m.Categoria == CategoriaMovimientoFinanciero.Compra)
            .OrderBy(m => m.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<MovimientoFinanciero?> GetByVentaIdAsync(int ventaId)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        return await AplicarAlcance(ConMetodoPago(), alcance)
            .FirstOrDefaultAsync(m => m.VentaId == ventaId && m.EsAutomatico);
    }

    public async Task<List<MovimientoFinanciero>> GetByBancosIdempotencyKeyAsync(string key, int usuarioId)
    {
        var descripcionBase = $"IdempotencyKey: {key}";
        return await ConMetodoPago()
            .Where(m =>
                m.ModuloOrigen == "Bancos" &&
                m.CreadoPorUsuarioId == usuarioId &&
                (m.Descripcion == descripcionBase ||
                 m.Descripcion == descripcionBase + "-Egreso" ||
                 m.Descripcion == descripcionBase + "-Ingreso"))
            .OrderBy(m => m.Id)
            .ToListAsync();
    }

    public async Task<List<MovimientoFinanciero>> GetFilteredAsync(DateTime? desde, DateTime? hasta)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        var query = AplicarAlcance(ConMetodoPago(), alcance);
        if (desde.HasValue) query = query.Where(m => m.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(m => m.Fecha <= hasta.Value);
        return await query.OrderByDescending(m => m.Fecha).ToListAsync();
    }

    public async Task<CatalogoMetodoPago?> GetMetodoPagoPorCodigoONombreAsync(string valor)
    {
        var normalizado = valor.Trim().ToUpper();
        return await _context.Set<CatalogoMetodoPago>()
            .FirstOrDefaultAsync(m => m.Activo && !m.Eliminado &&
                (m.Codigo.ToUpper() == normalizado || m.Nombre.ToUpper() == normalizado));
    }

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;

    private async Task NormalizarMetodoPagoRelacionalAsync(MovimientoFinanciero movimiento)
    {
        if (movimiento.MetodoPagoCatalogo is not null)
        {
            if (!movimiento.MetodoPagoCatalogo.Activo || movimiento.MetodoPagoCatalogo.Eliminado)
                throw new BusinessRuleException("El método de pago seleccionado está inactivo o eliminado.");

            movimiento.MetodoPagoId = movimiento.MetodoPagoCatalogo.Id;
            movimiento.MetodoPago = null;
            return;
        }

        if (movimiento.MetodoPagoId.HasValue)
        {
            var catalogoPorId = await _context.Set<CatalogoMetodoPago>()
                .FirstOrDefaultAsync(m => m.Id == movimiento.MetodoPagoId.Value && m.Activo && !m.Eliminado)
                ?? throw new BusinessRuleException("El método de pago seleccionado está inactivo, eliminado o no existe.");

            movimiento.MetodoPagoCatalogo = catalogoPorId;
            movimiento.MetodoPago = null;
            return;
        }

        if (!movimiento.MetodoPago.HasValue)
            return;

        var catalogo = await GetMetodoPagoPorCodigoONombreAsync(movimiento.MetodoPago.Value.ToString())
            ?? throw new BusinessRuleException(
                $"El método de pago legacy '{movimiento.MetodoPago.Value}' no existe o no está activo en el catálogo relacional.");

        movimiento.MetodoPagoId = catalogo.Id;
        movimiento.MetodoPagoCatalogo = catalogo;
        movimiento.MetodoPago = null;
    }

    private static bool EsReversionAutomaticaDeCompra(MovimientoFinanciero movimiento) =>
        movimiento.EsAutomatico &&
        movimiento.CompraId.HasValue &&
        movimiento.Tipo == TipoMovimientoFinanciero.Ingreso &&
        movimiento.Categoria == CategoriaMovimientoFinanciero.Reversion;
}
