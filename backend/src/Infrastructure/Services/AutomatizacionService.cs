using System.Data;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Services;

public sealed class AutomatizacionService : IAutomatizacionService
{
    private const string VersionReglas = "M12.1";
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AutomatizacionService(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AutomatizacionConfiguracionDto> GetConfiguracionAsync(CancellationToken cancellationToken = default)
    {
        var connection = _db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT DiasBorradorVentaAlerta, DiasBorradorCompraAlerta, DiasCargaPendienteAlerta,
                   DiasMovimientoFinancieroPendienteAlerta, LimiteSugerencias, LimiteAutocompletado,
                   MostrarRecordatoriosDashboard, FechaActualizacion, ActualizadoPor
            FROM AutomatizacionConfiguraciones
            WHERE Id = 1
            LIMIT 1;
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            throw new BusinessRuleException("La configuración M12 no está inicializada. Ejecuta las migraciones pendientes.");

        return new AutomatizacionConfiguracionDto
        {
            DiasBorradorVentaAlerta = reader.GetInt32(0),
            DiasBorradorCompraAlerta = reader.GetInt32(1),
            DiasCargaPendienteAlerta = reader.GetInt32(2),
            DiasMovimientoFinancieroPendienteAlerta = reader.GetInt32(3),
            LimiteSugerencias = reader.GetInt32(4),
            LimiteAutocompletado = reader.GetInt32(5),
            MostrarRecordatoriosDashboard = reader.GetBoolean(6),
            VersionReglas = VersionReglas,
            FechaActualizacion = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
            ActualizadoPor = reader.IsDBNull(8) ? null : reader.GetString(8)
        };
    }

    public async Task<AutomatizacionConfiguracionDto> UpdateConfiguracionAsync(
        ActualizarAutomatizacionConfiguracionRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidarConfiguracion(request);
        var connection = _db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE AutomatizacionConfiguraciones
            SET DiasBorradorVentaAlerta = @venta,
                DiasBorradorCompraAlerta = @compra,
                DiasCargaPendienteAlerta = @carga,
                DiasMovimientoFinancieroPendienteAlerta = @finanzas,
                LimiteSugerencias = @sugerencias,
                LimiteAutocompletado = @autocomplete,
                MostrarRecordatoriosDashboard = @mostrar,
                FechaActualizacion = UTC_TIMESTAMP(6),
                ActualizadoPor = @usuario
            WHERE Id = 1;
            """;
        AgregarParametro(command, "@venta", request.DiasBorradorVentaAlerta);
        AgregarParametro(command, "@compra", request.DiasBorradorCompraAlerta);
        AgregarParametro(command, "@carga", request.DiasCargaPendienteAlerta);
        AgregarParametro(command, "@finanzas", request.DiasMovimientoFinancieroPendienteAlerta);
        AgregarParametro(command, "@sugerencias", request.LimiteSugerencias);
        AgregarParametro(command, "@autocomplete", request.LimiteAutocompletado);
        AgregarParametro(command, "@mostrar", request.MostrarRecordatoriosDashboard);
        AgregarParametro(command, "@usuario", _currentUser.NombreUsuario ?? "sistema");
        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        if (affected != 1)
            throw new BusinessRuleException("No fue posible actualizar la configuración M12.");
        return await GetConfiguracionAsync(cancellationToken);
    }

    public async Task<AutomatizacionResumenDto> GetSugerenciasAsync(CancellationToken cancellationToken = default)
    {
        var config = await GetConfiguracionAsync(cancellationToken);
        if (!config.MostrarRecordatoriosDashboard)
            return new AutomatizacionResumenDto { VersionReglas = VersionReglas, GeneradoEnUtc = DateTime.UtcNow };

        var ahora = DateTime.UtcNow;
        var limiteVenta = ahora.AddDays(-config.DiasBorradorVentaAlerta);
        var limiteCompra = ahora.AddDays(-config.DiasBorradorCompraAlerta);
        var limiteCarga = ahora.AddDays(-config.DiasCargaPendienteAlerta);
        var limiteFinanzas = ahora.AddDays(-config.DiasMovimientoFinancieroPendienteAlerta);

        var stockBajo = await _db.ProductoVariantes.AsNoTracking()
            .CountAsync(x => x.Activo && x.Cantidad <= x.UmbralStockBajo, cancellationToken);
        var productosSinVariante = await _db.Productos.AsNoTracking()
            .CountAsync(x => x.Activo && !x.Variantes.Any(v => v.Activo), cancellationToken);
        var comprasBorrador = await _db.Compras.AsNoTracking()
            .CountAsync(x => !x.Eliminado && x.Estado == EstadoDocumento.Borrador && x.Fecha <= limiteCompra, cancellationToken);
        var ventasBorrador = await _db.Ventas.AsNoTracking()
            .CountAsync(x => !x.Eliminado && x.Estado == EstadoDocumento.Borrador && x.Fecha <= limiteVenta, cancellationToken);
        var clientesIncompletos = await _db.Clientes.AsNoTracking()
            .CountAsync(x => x.Activo && string.IsNullOrEmpty(x.Telefono) && string.IsNullOrEmpty(x.Correo), cancellationToken);
        var ventasSinFactura = await _db.Ventas.AsNoTracking()
            .CountAsync(x => !x.Eliminado && x.Estado == EstadoDocumento.Confirmada && x.Factura == null, cancellationToken);
        var financierosPendientes = await _db.MovimientosFinancieros.AsNoTracking()
            .CountAsync(x => x.Estado == EstadoMovimientoFinanciero.Pendiente && x.Fecha <= limiteFinanzas, cancellationToken);
        var cargasPendientes = await _db.CargasMasivas.AsNoTracking()
            .CountAsync(x => (x.Estado == EstadoCargaMasiva.PendienteValidacion || x.Estado == EstadoCargaMasiva.ConErrores) && x.FechaCreacion <= limiteCarga, cancellationToken);
        var configuracionIncompleta = !await _db.EmpresaConfiguraciones.AsNoTracking()
            .AnyAsync(x => x.Activa && !string.IsNullOrEmpty(x.NombreComercial), cancellationToken);

        var sugerencias = new List<AutomatizacionSugerenciaDto>();
        Agregar(sugerencias, stockBajo, "M12-STOCK-BAJO", "Inventario", "Critica", "Variantes con stock bajo", "Revisa las variantes cuya cantidad alcanzó o bajó de su mínimo configurado.", "/inventario");
        Agregar(sugerencias, productosSinVariante, "M12-PRODUCTO-SIN-VARIANTE", "Productos", "Alta", "Productos sin variante operativa", "Completa Marca, Modelo, Color, Talla, SKU y existencias mediante una variante activa.", "/productos");
        Agregar(sugerencias, comprasBorrador, "M12-COMPRA-BORRADOR", "Compras", "Media", "Compras en borrador antiguas", $"Hay compras en borrador con {config.DiasBorradorCompraAlerta} días o más.", "/compras");
        Agregar(sugerencias, ventasBorrador, "M12-VENTA-BORRADOR", "Ventas", "Media", "Ventas en borrador antiguas", $"Hay ventas en borrador con {config.DiasBorradorVentaAlerta} días o más.", "/ventas");
        Agregar(sugerencias, clientesIncompletos, "M12-CLIENTE-CONTACTO", "Clientes", "Baja", "Clientes sin medio de contacto", "Completa teléfono o correo para mejorar seguimiento y facturación.", "/clientes");
        Agregar(sugerencias, ventasSinFactura, "M12-VENTA-SIN-FACTURA", "Facturación", "Alta", "Ventas confirmadas sin factura", "Revisa las ventas confirmadas que todavía no tienen factura asociada.", "/facturas");
        Agregar(sugerencias, financierosPendientes, "M12-FINANZA-PENDIENTE", "Finanzas", "Alta", "Movimientos financieros pendientes", $"Hay movimientos pendientes con {config.DiasMovimientoFinancieroPendienteAlerta} días o más.", "/finanzas");
        Agregar(sugerencias, cargasPendientes, "M12-CARGA-PENDIENTE", "Cargas", "Media", "Cargas masivas requieren atención", "Hay archivos pendientes de validar o con errores que superaron el umbral configurado.", "/cargas-masivas");
        if (configuracionIncompleta)
            Agregar(sugerencias, 1, "M12-CONFIG-INCOMPLETA", "Configuración", "Alta", "Configuración empresarial incompleta", "Completa la identidad empresarial antes de emitir documentos definitivos.", "/configuracion");

        var orden = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["Critica"] = 0, ["Alta"] = 1, ["Media"] = 2, ["Baja"] = 3, ["Info"] = 4 };
        var resultado = sugerencias
            .OrderBy(x => orden.TryGetValue(x.Severidad, out var valor) ? valor : 99)
            .ThenByDescending(x => x.Cantidad)
            .ThenBy(x => x.Codigo)
            .Take(config.LimiteSugerencias)
            .ToList();

        return new AutomatizacionResumenDto
        {
            VersionReglas = VersionReglas,
            GeneradoEnUtc = ahora,
            TotalSugerencias = resultado.Count,
            Sugerencias = resultado
        };
    }

    public async Task<IReadOnlyList<AutocompletadoItemDto>> AutocompletarAsync(string contexto, string termino, CancellationToken cancellationToken = default)
    {
        var q = (termino ?? string.Empty).Trim();
        if (q.Length < 2) return Array.Empty<AutocompletadoItemDto>();
        var limite = (await GetConfiguracionAsync(cancellationToken)).LimiteAutocompletado;
        var patron = $"%{q}%";

        return contexto.Trim().ToLowerInvariant() switch
        {
            "productos" or "inventario" => await _db.ProductoVariantes.AsNoTracking()
                .Where(x => x.Activo && (EF.Functions.Like(x.Producto.Nombre, patron) || (x.Sku != null && EF.Functions.Like(x.Sku, patron)) || (x.CodigoBarras != null && EF.Functions.Like(x.CodigoBarras, patron))))
                .OrderBy(x => x.Producto.Nombre).ThenBy(x => x.Sku).Take(limite)
                .Select(x => new AutocompletadoItemDto { Id = x.Id, Contexto = "inventario", Etiqueta = x.Producto.Nombre, Detalle = (x.Marca != null ? x.Marca.Nombre : "") + " · " + (x.Modelo != null ? x.Modelo.Nombre : ""), Codigo = x.Sku ?? x.CodigoBarras })
                .ToListAsync(cancellationToken),
            "clientes" => await _db.Clientes.AsNoTracking().Where(x => x.Activo && (EF.Functions.Like(x.Nombre, patron) || (x.IdentidadORTN != null && EF.Functions.Like(x.IdentidadORTN, patron))))
                .OrderBy(x => x.Nombre).Take(limite)
                .Select(x => new AutocompletadoItemDto { Id = x.Id, Contexto = "clientes", Etiqueta = x.Nombre, Detalle = x.Telefono ?? x.Correo, Codigo = x.IdentidadORTN })
                .ToListAsync(cancellationToken),
            "proveedores" or "compras" => await _db.Proveedores.AsNoTracking().Where(x => x.Activo && (EF.Functions.Like(x.Nombre, patron) || (x.Documento != null && EF.Functions.Like(x.Documento, patron))))
                .OrderBy(x => x.Nombre).Take(limite)
                .Select(x => new AutocompletadoItemDto { Id = x.Id, Contexto = "proveedores", Etiqueta = x.Nombre, Detalle = x.Telefono ?? x.Correo, Codigo = x.Documento })
                .ToListAsync(cancellationToken),
            _ => throw new BusinessRuleException("Contexto de autocompletado no soportado. Usa productos, inventario, clientes, proveedores o compras.")
        };
    }

    public async Task<AccionMasivaPreviewDto> PrevisualizarAccionMasivaAsync(AccionMasivaPreviewRequest request, CancellationToken cancellationToken = default)
    {
        var ids = request.Ids.Where(x => x > 0).Distinct().Take(500).ToList();
        if (ids.Count == 0) throw new BusinessRuleException("Selecciona al menos un registro válido.");
        var accion = request.Accion.Trim().ToLowerInvariant();
        List<int> aplicables;
        var advertencias = new List<string>();

        switch (accion)
        {
            case "revisar-stock-bajo":
                aplicables = await _db.ProductoVariantes.AsNoTracking()
                    .Where(x => ids.Contains(x.Id) && x.Activo && x.Cantidad <= x.UmbralStockBajo)
                    .Select(x => x.Id).ToListAsync(cancellationToken);
                advertencias.Add("Vista previa solamente: no ajusta inventario ni crea movimientos.");
                break;
            case "revisar-clientes-contacto":
                aplicables = await _db.Clientes.AsNoTracking()
                    .Where(x => ids.Contains(x.Id) && x.Activo && string.IsNullOrEmpty(x.Telefono) && string.IsNullOrEmpty(x.Correo))
                    .Select(x => x.Id).ToListAsync(cancellationToken);
                advertencias.Add("Vista previa solamente: no modifica datos personales ni clasificación.");
                break;
            case "revisar-cargas-con-error":
                aplicables = await _db.CargasMasivas.AsNoTracking()
                    .Where(x => ids.Contains(x.Id) && x.Estado == EstadoCargaMasiva.ConErrores)
                    .Select(x => x.Id).ToListAsync(cancellationToken);
                advertencias.Add("Vista previa solamente: la revalidación debe ejecutarse desde el flujo transaccional de Cargas Masivas.");
                break;
            default:
                throw new BusinessRuleException("Acción masiva no soportada o potencialmente insegura.");
        }

        return new AccionMasivaPreviewDto
        {
            Accion = accion,
            Solicitados = ids.Count,
            Aplicables = aplicables.Count,
            Omitidos = ids.Count - aplicables.Count,
            SoloVistaPrevia = true,
            RequiereConfirmacion = true,
            IdsAplicables = aplicables,
            Advertencias = advertencias
        };
    }

    private static void ValidarConfiguracion(ActualizarAutomatizacionConfiguracionRequest x)
    {
        if (x.DiasBorradorVentaAlerta is < 1 or > 90 || x.DiasBorradorCompraAlerta is < 1 or > 180 || x.DiasCargaPendienteAlerta is < 1 or > 30 || x.DiasMovimientoFinancieroPendienteAlerta is < 1 or > 180)
            throw new BusinessRuleException("Los umbrales de días están fuera del rango empresarial permitido.");
        if (x.LimiteSugerencias is < 5 or > 100 || x.LimiteAutocompletado is < 5 or > 50)
            throw new BusinessRuleException("Los límites de sugerencias/autocompletado están fuera del rango permitido.");
    }

    private static void Agregar(List<AutomatizacionSugerenciaDto> lista, int cantidad, string codigo, string modulo, string severidad, string titulo, string detalle, string ruta)
    {
        if (cantidad <= 0) return;
        lista.Add(new AutomatizacionSugerenciaDto { Codigo = codigo, Modulo = modulo, Severidad = severidad, Titulo = titulo, Detalle = detalle, Cantidad = cantidad, Ruta = ruta, RequiereConfirmacion = true });
    }

    private static void AgregarParametro(System.Data.Common.DbCommand command, string nombre, object? valor)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = nombre;
        parameter.Value = valor ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
