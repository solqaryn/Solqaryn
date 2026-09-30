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

    public async Task SeedDefaultsAsync()
    {
        await CrearImpuestoSiNoExisteAsync(
            codigo: "ISV15",
            nombre: "ISV 15%",
            descripcion: "Impuesto sobre ventas general, incluido en el precio de venta cuando corresponda.",
            tasa: 15m,
            prioridad: 10,
            incluidoEnPrecio: true,
            activo: true,
            operaciones: new[] { OperacionImpuesto.Venta });

        await CrearImpuestoSiNoExisteAsync(
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
            .AnyAsync(i => i.Codigo == codigo);
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
    }

}
