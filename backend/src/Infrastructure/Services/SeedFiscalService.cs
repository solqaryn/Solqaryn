using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Services;

/// Crea configuraciones fiscales iniciales solo cuando no existen. Nunca
/// reactiva, modifica tasas ni revierte decisiones realizadas desde la interfaz.
public class SeedFiscalService
{
    private readonly AppDbContext _context;

    public SeedFiscalService(AppDbContext context)
    {
        _context = context;
    }

    // Explicit maintenance context only, never the HTTP-scoped context.
    // Each company has its own seed; existing fiscal decisions remain untouched.
    public async Task SeedDefaultsAsync()
    {
        if (_context.CommercialRequestRequiresTenant)
            throw new InvalidOperationException("La inicialización fiscal requiere un contexto interno sin solicitud HTTP.");
        var companies = await _context.Set<Empresa>().AsNoTracking()
            .Where(e => e.Activa).OrderBy(e => e.Id).Select(e => e.Id).ToListAsync();
        foreach (var empresaId in companies)
        {
            if (_context.Database.IsRelational())
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                var locked = await _context.Database.SqlQueryRaw<int>(
                    "SELECT Id AS Value FROM Empresas WHERE Id={0} AND Activa=1 FOR UPDATE", empresaId).ToListAsync();
                if (locked.Count != 1)
                    throw new InvalidOperationException("Empresa activa no disponible para inicialización fiscal.");
                await SeedEmpresaAsync(empresaId);
                await transaction.CommitAsync();
            }
            else
            {
                await SeedEmpresaAsync(empresaId);
            }
        }
    }

    private async Task SeedEmpresaAsync(int empresaId)
    {
        await CrearImpuestoSiNoExisteAsync(
            empresaId: empresaId,
            codigo: "ISV15",
            nombre: "ISV 15%",
            descripcion: "Impuesto sobre ventas general, incluido en el precio de venta cuando corresponda.",
            tasa: 15m,
            prioridad: 10,
            incluidoEnPrecio: true,
            activo: true,
            operaciones: new[] { OperacionImpuesto.Venta });

        await CrearImpuestoSiNoExisteAsync(
            empresaId: empresaId,
            codigo: "ISC5",
            nombre: "ISC 5%",
            descripcion: "Impuesto selectivo al consumo disponible para compras cuando corresponda.",
            tasa: 5m,
            prioridad: 20,
            incluidoEnPrecio: true,
            activo: false,
            operaciones: new[] { OperacionImpuesto.Compra });

        await _context.SaveChangesAsync();
    }

    private async Task CrearImpuestoSiNoExisteAsync(
        int empresaId,
        string codigo,
        string nombre,
        string descripcion,
        decimal tasa,
        int prioridad,
        bool incluidoEnPrecio,
        bool activo,
        OperacionImpuesto[] operaciones)
    {
        // El query filter oculta eliminados lógicamente. El seed debe ignorarlo:
        // un borrado/desactivación administrativa sigue siendo una decisión
        // persistida y no debe provocar recreación, reactivación ni colisión del
        // índice único de Codigo durante un reinicio de la API.
        var existe = await _context.Impuestos
            .IgnoreQueryFilters()
            .AnyAsync(i => EF.Property<int?>(i, "EmpresaId") == empresaId && i.Codigo == codigo);
        if (existe) return;

        var impuesto = new Impuesto
        {
            Codigo = codigo,
            Nombre = nombre,
            Descripcion = descripcion,
            Tipo = TipoImpuesto.Porcentaje,
            Tasa = tasa,
            MontoFijo = null,
            IncluidoEnPrecio = incluidoEnPrecio,
            SeCalculaAntesDescuento = false,
            Acumulativo = true,
            Prioridad = prioridad,
            RequiereRetencion = false,
            Activo = activo,
            Eliminado = false,
            FechaCreacion = DateTime.UtcNow,
            Operaciones = operaciones
                .Distinct()
                .Select(o => new ImpuestoOperacion { Operacion = o })
                .ToList()
        };

        _context.Impuestos.Add(impuesto);
        _context.Entry(impuesto).Property("EmpresaId").CurrentValue = empresaId;
        foreach (var operacion in impuesto.Operaciones)
            _context.Entry(operacion).Property("EmpresaId").CurrentValue = empresaId;
    }

}
