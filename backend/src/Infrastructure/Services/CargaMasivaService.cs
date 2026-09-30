using System.Globalization;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Solqaryn.Infrastructure.Services;

public sealed class CargaMasivaService : ICargaMasivaService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly AppDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditoriaService _auditoria;
    private readonly ILogger<CargaMasivaService> _logger;
    private readonly ITipoClientePredeterminadoResolver _predeterminadoResolver;

    public CargaMasivaService(
        AppDbContext db,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IAuditoriaService auditoria,
        ILogger<CargaMasivaService> logger,
        ITipoClientePredeterminadoResolver predeterminadoResolver)
    {
        _db = db;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _auditoria = auditoria;
        _logger = logger;
        _predeterminadoResolver = predeterminadoResolver;
    }

    public CargaMasivaConfiguracionDto ObtenerConfiguracion() => new()
    {
        MaximoBytes = CargaMasivaArchivoHelper.MaximoBytes,
        MaximoFilas = CargaMasivaArchivoHelper.MaximoFilas,
        ExtensionesPermitidas = CargaMasivaArchivoHelper.ExtensionesPermitidas,
        Tipos = Enum.GetValues<TipoCargaMasiva>().Select(tipo => new CargaMasivaTipoDto
        {
            Tipo = tipo.ToString(),
            Nombre = CargaMasivaArchivoHelper.NombreAmigable(tipo),
            Descripcion = CargaMasivaArchivoHelper.Descripcion(tipo),
            Columnas = CargaMasivaArchivoHelper.ObtenerColumnas(tipo)
        }).ToList()
    };

    public Task<ArchivoDescargableDto> DescargarPlantillaAsync(TipoCargaMasiva tipo, string formato) =>
        Task.FromResult(CargaMasivaArchivoHelper.CrearPlantilla(tipo, formato));

    public async Task<CargaMasivaDetalleDto> ValidarAsync(
        TipoCargaMasiva tipo,
        string nombreArchivo,
        string? contentType,
        long tamanoBytes,
        Stream contenido,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(tipo)) throw new BusinessRuleException("El tipo de carga no es válido.");
        if (tamanoBytes <= 0) throw new BusinessRuleException("El archivo está vacío.");
        if (tamanoBytes > CargaMasivaArchivoHelper.MaximoBytes)
            throw new BusinessRuleException($"El archivo supera el máximo de {CargaMasivaArchivoHelper.MaximoBytes / 1024 / 1024} MB.");

        var nombreSeguro = Path.GetFileName(nombreArchivo ?? string.Empty);
        var extension = Path.GetExtension(nombreSeguro).ToLowerInvariant();
        if (!CargaMasivaArchivoHelper.ExtensionesPermitidas.Contains(extension, StringComparer.OrdinalIgnoreCase))
            throw new BusinessRuleException("Solo se permiten archivos CSV o XLSX sin macros.");

        await using var memoria = new MemoryStream();
        await contenido.CopyToAsync(memoria, cancellationToken);
        if (memoria.Length != tamanoBytes) tamanoBytes = memoria.Length;
        var bytes = memoria.ToArray();
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        var existente = await _db.CargasMasivas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Tipo == tipo && x.HashArchivo == hash, cancellationToken);
        if (existente is not null)
        {
            if (existente.Estado == EstadoCargaMasiva.Confirmada)
                throw new BusinessRuleException($"Este archivo ya fue confirmado en la carga #{existente.Id}. No se importará nuevamente.");

            var detalleExistente = await GetByIdAsync(existente.Id)
                ?? throw new BusinessRuleException("No se pudo recuperar la carga previamente validada.");
            detalleExistente.ArchivoReutilizado = true;
            return detalleExistente;
        }

        memoria.Position = 0;
        ArchivoLeido archivo;
        try
        {
            archivo = await CargaMasivaArchivoHelper.LeerAsync(extension, memoria, cancellationToken);
        }
        catch (Exception ex) when (ex is not BusinessRuleException)
        {
            _logger.LogWarning(ex, "Archivo de carga masiva inválido: {NombreArchivo}", nombreSeguro);
            throw new BusinessRuleException("El archivo no pudo leerse. Verifica que no esté dañado, protegido o contenga macros.");
        }

        var errores = archivo.Problemas.Select(p => new CargaMasivaErrorDto
        {
            NumeroFila = p.NumeroFila,
            Campo = p.Campo,
            Codigo = p.Codigo,
            Mensaje = p.Mensaje,
            ValorOriginal = p.ValorOriginal,
            EsAdvertencia = p.EsAdvertencia
        }).ToList();

        var columnasEsperadas = CargaMasivaArchivoHelper.ObtenerColumnas(tipo);
        var mapaColumnas = columnasEsperadas.ToDictionary(
            CargaMasivaArchivoHelper.NormalizarCabecera,
            x => x,
            StringComparer.OrdinalIgnoreCase);

        ValidarCabeceras(archivo.Cabeceras, mapaColumnas, errores);
        var filas = archivo.Filas.Select((fila, index) => new CargaMasivaFilaDto
        {
            NumeroFila = index + 2,
            Datos = columnasEsperadas.ToDictionary(
                columna => columna,
                columna => fila.TryGetValue(CargaMasivaArchivoHelper.NormalizarCabecera(columna), out var valor) ? Limpiar(valor) : null,
                StringComparer.OrdinalIgnoreCase)
        }).ToList();

        if (filas.Count == 0)
            AgregarError(errores, 1, null, "ARCHIVO_SIN_DATOS", "El archivo no contiene filas de datos.", null);

        var cabeceraInvalida = errores.Any(x => x.NumeroFila == 1 && !x.EsAdvertencia);
        if (!cabeceraInvalida)
            await ValidarFilasAsync(tipo, filas, errores, cancellationToken);

        foreach (var fila in filas)
        {
            var observaciones = errores.Where(x => x.NumeroFila == fila.NumeroFila).ToList();
            fila.EsValida = observaciones.All(x => x.EsAdvertencia);
            fila.Mensajes = observaciones.Select(x => x.Mensaje).Distinct().ToList();
        }

        if (cabeceraInvalida)
        {
            foreach (var fila in filas) fila.EsValida = false;
        }

        var carga = new CargaMasiva
        {
            Tipo = tipo,
            Estado = errores.Any(x => !x.EsAdvertencia)
                ? EstadoCargaMasiva.ConErrores
                : EstadoCargaMasiva.Validada,
            NombreArchivo = string.IsNullOrWhiteSpace(nombreSeguro) ? $"carga{extension}" : nombreSeguro,
            Extension = extension,
            ContentType = Limitar(contentType, 150) ?? "application/octet-stream",
            TamanoBytes = tamanoBytes,
            HashArchivo = hash,
            DatosNormalizadosJson = JsonSerializer.Serialize(filas, JsonOptions),
            TotalFilas = filas.Count,
            FilasValidas = filas.Count(x => x.EsValida),
            FilasConError = filas.Count(x => !x.EsValida),
            FilasConAdvertencia = filas.Count(x => errores.Any(e => e.NumeroFila == x.NumeroFila && e.EsAdvertencia)),
            FechaValidacion = DateTime.UtcNow,
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario
        };

        foreach (var error in errores)
        {
            carga.Errores.Add(new CargaMasivaError
            {
                NumeroFila = error.NumeroFila,
                Campo = Limitar(error.Campo, 120),
                Codigo = Limitar(error.Codigo, 80) ?? "ERROR",
                Mensaje = Limitar(error.Mensaje, 700) ?? "Error de validación.",
                ValorOriginal = Limitar(error.ValorOriginal, 1000),
                EsAdvertencia = error.EsAdvertencia
            });
        }

        _db.CargasMasivas.Add(carga);
        await _db.SaveChangesAsync(cancellationToken);

        await _auditoria.RegistrarAsync(
            ModuloSistema.CargasMasivas,
            AccionPermiso.Crear,
            $"Validó la carga masiva #{carga.Id} de {CargaMasivaArchivoHelper.NombreAmigable(tipo)}: {carga.FilasValidas} válidas y {carga.FilasConError} con error.",
            carga.Id,
            entidad: "CargaMasiva",
            valoresNuevos: new { carga.Tipo, carga.Estado, carga.TotalFilas, carga.FilasValidas, carga.FilasConError, carga.HashArchivo });

        return MapDetalle(carga, filas, errores);
    }

    public async Task<PagedResult<CargaMasivaDto>> GetPagedAsync(PagedRequest request)
    {
        var query = _db.CargasMasivas.AsNoTracking().AsQueryable();
        if (!_currentUser.EsAdministrador && _currentUser.UsuarioId.HasValue)
            query = query.Where(x => x.CreadoPorUsuarioId == _currentUser.UsuarioId.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.NombreArchivo.Contains(search) ||
                                     x.Tipo.ToString().Contains(search) ||
                                     x.Estado.ToString().Contains(search));
        }

        query = request.SortBy?.ToLowerInvariant() switch
        {
            "tipo" => request.SortDirection == "desc" ? query.OrderByDescending(x => x.Tipo) : query.OrderBy(x => x.Tipo),
            "estado" => request.SortDirection == "desc" ? query.OrderByDescending(x => x.Estado) : query.OrderBy(x => x.Estado),
            "archivo" or "nombrearchivo" => request.SortDirection == "desc" ? query.OrderByDescending(x => x.NombreArchivo) : query.OrderBy(x => x.NombreArchivo),
            _ => request.SortDirection == "asc" ? query.OrderBy(x => x.FechaCreacion) : query.OrderByDescending(x => x.FechaCreacion)
        };

        var total = await query.CountAsync();
        var items = await query.Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => MapResumenExpression(x))
            .ToListAsync();

        return new PagedResult<CargaMasivaDto>
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }

    public async Task<CargaMasivaDetalleDto?> GetByIdAsync(int id)
    {
        var carga = await _db.CargasMasivas
            .AsNoTracking()
            .Include(x => x.Errores)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (carga is null) return null;
        if (!_currentUser.EsAdministrador && _currentUser.UsuarioId.HasValue && carga.CreadoPorUsuarioId != _currentUser.UsuarioId.Value)
            throw new ForbiddenAccessException("No tienes acceso a esta carga masiva.");

        var filas = DeserializarFilas(carga.DatosNormalizadosJson);
        var errores = carga.Errores.Select(MapError).ToList();
        return MapDetalle(carga, filas, errores);
    }

    public async Task<CargaMasivaDetalleDto> ConfirmarAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        CargaMasiva? carga = null;
        List<CargaMasivaFilaDto> filas = new();
        var creados = 0;
        var actualizados = 0;
        var confirmadaAhora = false;
        var yaEstabaConfirmada = false;

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Todos los valores y entidades se recargan en cada intento. Esto
                // es obligatorio porque UnitOfWork limpia el ChangeTracker después
                // de un 1205/1213 antes de repetir la transacción completa.
                carga = null;
                filas = new List<CargaMasivaFilaDto>();
                creados = 0;
                actualizados = 0;
                confirmadaAhora = false;
                yaEstabaConfirmada = false;

                if (!_currentUser.EsAdministrador && !_currentUser.UsuarioId.HasValue)
                    throw new ForbiddenAccessException("No tienes acceso a esta carga masiva.");

                if (_currentUser.EsAdministrador)
                {
                    carga = await _db.CargasMasivas
                        .FromSqlInterpolated($"SELECT c.* FROM CargasMasivas c WHERE c.Id = {id} FOR UPDATE")
                        .AsTracking()
                        .SingleOrDefaultAsync(cancellationToken);
                }
                else
                {
                    var usuarioId = _currentUser.UsuarioId!.Value;
                    carga = await _db.CargasMasivas
                        .FromSqlInterpolated($"SELECT c.* FROM CargasMasivas c WHERE c.Id = {id} AND c.CreadoPorUsuarioId = {usuarioId} FOR UPDATE")
                        .AsTracking()
                        .SingleOrDefaultAsync(cancellationToken);
                }

                if (carga is null)
                {
                    var existe = await _db.CargasMasivas
                        .AsNoTracking()
                        .AnyAsync(x => x.Id == id, cancellationToken);
                    if (existe)
                        throw new ForbiddenAccessException("No tienes acceso a esta carga masiva.");
                    throw new BusinessRuleException("La carga masiva no existe.");
                }

                await _db.Entry(carga)
                    .Collection(x => x.Errores)
                    .LoadAsync(cancellationToken);

                filas = DeserializarFilas(carga.DatosNormalizadosJson);

                if (carga.Estado == EstadoCargaMasiva.Confirmada)
                {
                    yaEstabaConfirmada = true;
                    return;
                }

                if (carga.Estado != EstadoCargaMasiva.Validada || carga.FilasConError > 0)
                    throw new BusinessRuleException("La carga contiene errores o no ha sido validada correctamente.");
                if (filas.Count == 0 || filas.Any(x => !x.EsValida))
                    throw new BusinessRuleException("La vista previa validada no contiene filas confirmables.");

                (creados, actualizados) = carga.Tipo switch
                {
                    TipoCargaMasiva.Clientes => await AplicarClientesAsync(filas, cancellationToken),
                    TipoCargaMasiva.Proveedores => await AplicarProveedoresAsync(filas, cancellationToken),
                    TipoCargaMasiva.Colores => await AplicarColoresAsync(filas, cancellationToken),
                    TipoCargaMasiva.Productos => await AplicarProductosAsync(filas, cancellationToken),
                    TipoCargaMasiva.VariantesInventario => await AplicarVariantesAsync(carga.Id, filas, cancellationToken),
                    _ => throw new BusinessRuleException("El tipo de carga no es válido.")
                };

                carga.Estado = EstadoCargaMasiva.Confirmada;
                carga.FilasProcesadas = filas.Count;
                carga.RegistrosCreados = creados;
                carga.RegistrosActualizados = actualizados;
                carga.FechaConfirmacion = DateTime.UtcNow;
                carga.ConfirmadoPorUsuarioId = _currentUser.UsuarioId;
                carga.ConfirmadoPorNombreUsuario = _currentUser.NombreUsuario;
                carga.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
                carga.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
                carga.FechaActualizacion = DateTime.UtcNow;
                carga.ErrorGeneral = null;
                await _db.SaveChangesAsync(cancellationToken);
                confirmadaAhora = true;
            });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _db.ChangeTracker.Clear();

            // Esta actualización se ejecuta únicamente después de que UnitOfWork
            // agotó los reintentos transitorios y revirtió la transacción.
            try
            {
                var cargaFallida = await _db.CargasMasivas
                    .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (cargaFallida is not null &&
                    cargaFallida.Estado != EstadoCargaMasiva.Confirmada)
                {
                    cargaFallida.Estado = EstadoCargaMasiva.Fallida;
                    cargaFallida.ErrorGeneral =
                        "La confirmación fue revertida completamente. Revalida el archivo antes de intentar nuevamente.";
                    cargaFallida.FechaActualizacion = DateTime.UtcNow;
                    cargaFallida.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
                    cargaFallida.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
                    await _db.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception persistenciaEx)
            {
                _logger.LogError(
                    persistenciaEx,
                    "No se pudo registrar el estado fallido de la carga masiva {CargaId}",
                    id);
            }

            _logger.LogError(
                ex,
                "Falló la confirmación reintentable de la carga masiva {CargaId}",
                id);

            if (carga is not null)
            {
                await _auditoria.RegistrarAsync(
                    ModuloSistema.CargasMasivas,
                    AccionPermiso.Confirmar,
                    $"Falló la confirmación de la carga masiva #{id}; la transacción fue revertida.",
                    id,
                    entidad: "CargaMasiva",
                    resultado: "Error",
                    error: "Confirmación transaccional revertida");
            }

            if (ex is BusinessRuleException or ForbiddenAccessException)
                throw;

            throw new BusinessRuleException(
                "La importación no pudo confirmarse y ningún cambio fue aplicado. Revalida el archivo.");
        }

        if (yaEstabaConfirmada)
        {
            return await GetByIdAsync(id)
                ?? throw new BusinessRuleException("No se pudo recuperar la carga confirmada.");
        }

        if (confirmadaAhora)
        {
            await _auditoria.RegistrarAsync(
                ModuloSistema.CargasMasivas,
                AccionPermiso.Confirmar,
                $"Confirmó la carga masiva #{id}: {creados} registros creados y {actualizados} actualizados.",
                id,
                entidad: "CargaMasiva",
                valoresNuevos: new
                {
                    Tipo = carga!.Tipo,
                    FilasProcesadas = filas.Count,
                    RegistrosCreados = creados,
                    RegistrosActualizados = actualizados
                });
        }

        return await GetByIdAsync(id)
            ?? throw new BusinessRuleException("No se pudo recuperar la carga confirmada.");
    }

    public async Task<ArchivoDescargableDto> DescargarErroresAsync(int id, string formato)
    {
        var detalle = await GetByIdAsync(id) ?? throw new BusinessRuleException("La carga masiva no existe.");
        return CargaMasivaArchivoHelper.CrearReporteErrores(detalle.Errores, id, formato);
    }

    private static void ValidarCabeceras(
        IReadOnlyCollection<string> cabeceras,
        IReadOnlyDictionary<string, string> mapaEsperado,
        List<CargaMasivaErrorDto> errores)
    {
        if (cabeceras.Count == 0)
        {
            AgregarError(errores, 1, null, "CABECERA_AUSENTE", "No se encontró la fila de encabezados.", null);
            return;
        }

        var columnasOpcionales = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "TipoCliente", "TipoInventario" };

        foreach (var duplicada in cabeceras.Where(x => !string.IsNullOrWhiteSpace(x)).GroupBy(x => x, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
            AgregarError(errores, 1, duplicada.Key, "COLUMNA_DUPLICADA", "La columna aparece más de una vez.", duplicada.Key);

        foreach (var esperada in mapaEsperado)
        {
            if (columnasOpcionales.Contains(esperada.Key))
                continue; // Tolerar su ausencia (se asignará el valor por defecto/fallback)

            if (!cabeceras.Contains(esperada.Key, StringComparer.OrdinalIgnoreCase))
                AgregarError(errores, 1, esperada.Value, "COLUMNA_FALTANTE", $"Falta la columna obligatoria '{esperada.Value}'.", null);
        }

        foreach (var desconocida in cabeceras.Where(x => !string.IsNullOrWhiteSpace(x) && !mapaEsperado.ContainsKey(x)))
            AgregarError(errores, 1, desconocida, "COLUMNA_IGNORADA", "La columna no pertenece a la plantilla y será ignorada.", desconocida, true);
    }

    private async Task ValidarFilasAsync(
        TipoCargaMasiva tipo,
        List<CargaMasivaFilaDto> filas,
        List<CargaMasivaErrorDto> errores,
        CancellationToken cancellationToken)
    {
        switch (tipo)
        {
            case TipoCargaMasiva.Clientes:
                await ValidarClientesAsync(filas, errores, cancellationToken);
                break;
            case TipoCargaMasiva.Proveedores:
                await ValidarProveedoresAsync(filas, errores, cancellationToken);
                break;
            case TipoCargaMasiva.Colores:
                await ValidarColoresAsync(filas, errores, cancellationToken);
                break;
            case TipoCargaMasiva.Productos:
                await ValidarProductosAsync(filas, errores, cancellationToken);
                break;
            case TipoCargaMasiva.VariantesInventario:
                await ValidarVariantesAsync(filas, errores, cancellationToken);
                break;
            default:
                throw new BusinessRuleException("El tipo de carga no es válido.");
        }
    }

    private async Task ValidarClientesAsync(List<CargaMasivaFilaDto> filas, List<CargaMasivaErrorDto> errores, CancellationToken ct)
    {
        var existentes = await _db.Clientes.AsNoTracking().ToListAsync(ct);
        var tiposClientes = await _db.TipoClientes.Where(t => t.Activo && !t.Eliminado).AsNoTracking().ToListAsync(ct);
        var clavesArchivo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var fila in filas)
        {
            NormalizarTexto(fila, "Nombre", 200, true, errores);
            NormalizarTexto(fila, "Telefono", 30, false, errores);
            NormalizarTexto(fila, "IdentidadORTN", 50, false, errores);
            NormalizarTexto(fila, "Correo", 150, false, errores);
            NormalizarTexto(fila, "Direccion", 300, false, errores);
            NormalizarTexto(fila, "TipoCliente", 100, false, errores);
            NormalizarActivo(fila, errores);
            ValidarCorreo(fila, "Correo", errores);

            var tipoNombre = V(fila, "TipoCliente");
            if (!string.IsNullOrWhiteSpace(tipoNombre))
            {
                var tipoObj = tiposClientes.FirstOrDefault(t => t.Nombre.Equals(tipoNombre.Trim(), StringComparison.OrdinalIgnoreCase));
                if (tipoObj is null)
                {
                    AgregarError(errores, fila.NumeroFila, "TipoCliente", "TIPO_CLIENTE_NO_EXISTE", "El tipo de cliente indicado no existe o está inactivo.", tipoNombre);
                }
            }

            var clave = ClavePersona(fila.Datos, "IdentidadORTN");
            if (!clavesArchivo.Add(clave))
                AgregarError(errores, fila.NumeroFila, "Nombre", "DUPLICADO_ARCHIVO", "La misma persona aparece más de una vez en el archivo.", V(fila, "Nombre"));

            var existente = BuscarPersona(existentes, fila.Datos, x => x.IdentidadORTN, x => x.Correo, x => x.Telefono, x => x.Nombre, "IdentidadORTN");
            fila.Accion = existente is null ? "Crear" : "Actualizar";
        }
    }

    private async Task ValidarProveedoresAsync(List<CargaMasivaFilaDto> filas, List<CargaMasivaErrorDto> errores, CancellationToken ct)
    {
        var existentes = await _db.Proveedores.AsNoTracking().ToListAsync(ct);
        var clavesArchivo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var fila in filas)
        {
            NormalizarTexto(fila, "Nombre", 200, true, errores);
            NormalizarTexto(fila, "Telefono", 30, false, errores);
            NormalizarTexto(fila, "Documento", 50, false, errores);
            NormalizarTexto(fila, "Correo", 150, false, errores);
            NormalizarTexto(fila, "Direccion", 300, false, errores);
            NormalizarActivo(fila, errores);
            ValidarCorreo(fila, "Correo", errores);

            var clave = ClavePersona(fila.Datos, "Documento");
            if (!clavesArchivo.Add(clave))
                AgregarError(errores, fila.NumeroFila, "Nombre", "DUPLICADO_ARCHIVO", "El mismo proveedor aparece más de una vez en el archivo.", V(fila, "Nombre"));

            var existente = BuscarPersona(existentes, fila.Datos, x => x.Documento, x => x.Correo, x => x.Telefono, x => x.Nombre, "Documento");
            fila.Accion = existente is null ? "Crear" : "Actualizar";
        }
    }

    private async Task ValidarColoresAsync(List<CargaMasivaFilaDto> filas, List<CargaMasivaErrorDto> errores, CancellationToken ct)
    {
        var existentes = await _db.Colores.AsNoTracking()
            .Where(x => !x.Eliminado)
            .ToListAsync(ct);
        var claves = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var fila in filas)
        {
            NormalizarTexto(fila, "Nombre", 120, true, errores);
            NormalizarTexto(fila, "CodigoVisual", 20, false, errores);
            NormalizarTexto(fila, "Descripcion", 500, false, errores);
            NormalizarEntero(fila, "Orden", 0, 100000, errores, requerido: false, valorPredeterminado: 0);
            NormalizarActivo(fila, errores);

            var codigo = V(fila, "CodigoVisual");
            if (!string.IsNullOrWhiteSpace(codigo) && !Regex.IsMatch(codigo, "^#[0-9A-Fa-f]{6}$"))
                AgregarError(errores, fila.NumeroFila, "CodigoVisual", "COLOR_HEX_INVALIDO", "El código visual debe tener formato #RRGGBB.", codigo);

            var clave = NormalizarClave(V(fila, "Nombre"));
            if (!claves.Add(clave))
                AgregarError(errores, fila.NumeroFila, "Nombre", "DUPLICADO_ARCHIVO", "El color aparece más de una vez en el archivo.", V(fila, "Nombre"));

            fila.Accion = existentes.Any(x => NormalizarClave(x.Nombre) == clave) ? "Actualizar" : "Crear";
        }
    }

    private async Task ValidarProductosAsync(List<CargaMasivaFilaDto> filas, List<CargaMasivaErrorDto> errores, CancellationToken ct)
    {
        var marcas = await _db.Marcas.AsNoTracking().Where(x => x.Activo && !x.Eliminado).ToListAsync(ct);
        var modelos = await _db.Modelos.AsNoTracking().Where(x => x.Activo && !x.Eliminado).ToListAsync(ct);
        var tallas = await _db.Tallas.AsNoTracking().Where(x => x.Activo && !x.Eliminado).ToListAsync(ct);
        var categorias = await _db.Categorias.AsNoTracking().Where(x => !x.Eliminada).ToListAsync(ct);
        var productos = await _db.Productos.AsNoTracking()
            .Include(x => x.Variantes.Where(v => !v.Eliminado))
            .Where(x => !x.Eliminado)
            .ToListAsync(ct);
        var claves = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var fila in filas)
        {
            NormalizarTexto(fila, "Nombre", 150, true, errores);
            NormalizarTexto(fila, "Marca", 100, true, errores);
            NormalizarTexto(fila, "Modelo", 100, true, errores);
            NormalizarTexto(fila, "Categoria", 150, false, errores);
            NormalizarTexto(fila, "Talla", 120, false, errores);
            NormalizarTexto(fila, "Descripcion", 1000, false, errores);
            NormalizarDecimal(fila, "Costo", 0m, decimal.MaxValue, errores, requerido: true);
            NormalizarDecimal(fila, "Precio", 0.01m, decimal.MaxValue, errores, requerido: true);
            NormalizarEntero(fila, "UmbralStockBajo", 0, int.MaxValue, errores, requerido: false, valorPredeterminado: 5);
            NormalizarActivo(fila, errores);

            var marca = marcas.FirstOrDefault(x => NormalizarClave(x.Nombre) == NormalizarClave(V(fila, "Marca")));
            if (marca is null)
                AgregarError(errores, fila.NumeroFila, "Marca", "MARCA_NO_EXISTE", "La marca debe existir y estar activa antes de importar productos.", V(fila, "Marca"));

            var modelo = marca is null ? null : modelos.FirstOrDefault(x => x.MarcaId == marca.Id && NormalizarClave(x.Nombre) == NormalizarClave(V(fila, "Modelo")));
            if (modelo is null)
                AgregarError(errores, fila.NumeroFila, "Modelo", "MODELO_NO_EXISTE", "El modelo debe existir, estar activo y pertenecer a la marca indicada.", V(fila, "Modelo"));

            var categoria = V(fila, "Categoria");
            if (!string.IsNullOrWhiteSpace(categoria) && !categorias.Any(x => x.Activa && NormalizarClave(x.Nombre) == NormalizarClave(categoria)))
                AgregarError(errores, fila.NumeroFila, "Categoria", "CATEGORIA_NO_EXISTE", "La categoría indicada no existe o está inactiva.", categoria);

            var talla = V(fila, "Talla");
            if (!string.IsNullOrWhiteSpace(talla) && !tallas.Any(x => NormalizarClave(x.Nombre) == NormalizarClave(talla)))
                AgregarError(errores, fila.NumeroFila, "Talla", "TALLA_NO_EXISTE", "La talla o tamaño indicado no existe o está inactivo.", talla);

            var clave = ClaveProducto(fila.Datos);
            if (!claves.Add(clave))
                AgregarError(errores, fila.NumeroFila, "Nombre", "DUPLICADO_ARCHIVO", "El producto con la misma marca y modelo aparece más de una vez.", V(fila, "Nombre"));

            var candidatos = productos.Where(x => NormalizarClave(x.Nombre) == NormalizarClave(V(fila, "Nombre"))).ToList();
            if (candidatos.Count > 1)
            {
                AgregarError(errores, fila.NumeroFila, "Nombre", "PRODUCTO_AMBIGUO", "Existe más de una familia con ese nombre. Corrige el catálogo antes de importar.", V(fila, "Nombre"));
                fila.Accion = "Actualizar";
                continue;
            }

            if (candidatos.Count == 1)
            {
                var existente = candidatos[0];
                var tecnica = existente.Variantes.SingleOrDefault(v => v.EsTecnica && !v.Eliminado);
                if (tecnica is null && existente.Variantes.Any(v => !v.EsTecnica && !v.Eliminado))
                    AgregarError(errores, fila.NumeroFila, "Nombre", "PRODUCTO_REQUIERE_VARIANTES", "El producto usa variantes comerciales. Actualiza sus dimensiones, costo y precio mediante la carga VariantesInventario.", V(fila, "Nombre"));
                fila.Accion = "Actualizar";
            }
            else
            {
                fila.Accion = "Crear";
            }
        }
    }

    private async Task ValidarVariantesAsync(List<CargaMasivaFilaDto> filas, List<CargaMasivaErrorDto> errores, CancellationToken ct)
    {
        var productos = await _db.Productos.AsNoTracking().Where(x => !x.Eliminado).ToListAsync(ct);
        var marcas = await _db.Marcas.AsNoTracking().Where(x => x.Activo && !x.Eliminado).ToListAsync(ct);
        var modelos = await _db.Modelos.AsNoTracking().Where(x => x.Activo && !x.Eliminado).ToListAsync(ct);
        var colores = await _db.Colores.AsNoTracking().Where(x => x.Activo && !x.Eliminado).ToListAsync(ct);
        var tallas = await _db.Tallas.AsNoTracking().Where(x => x.Activo && !x.Eliminado).ToListAsync(ct);
        var variantes = await _db.ProductoVariantes.IgnoreQueryFilters().AsNoTracking().Where(x => !x.Eliminado).ToListAsync(ct);
        var combinacionesArchivo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var skusArchivo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var codigosArchivo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var fila in filas)
        {
            NormalizarTexto(fila, "Producto", 150, true, errores);
            NormalizarTexto(fila, "Marca", 120, false, errores);
            NormalizarTexto(fila, "Modelo", 120, false, errores);
            NormalizarTexto(fila, "Color", 120, false, errores);
            NormalizarTexto(fila, "Talla", 120, false, errores);
            NormalizarTexto(fila, "SKU", 80, false, errores, mayusculas: true);
            NormalizarTexto(fila, "CodigoBarras", 120, false, errores);
            NormalizarEntero(fila, "Cantidad", 0, int.MaxValue, errores, requerido: true);
            NormalizarEntero(fila, "UmbralStockBajo", 0, int.MaxValue, errores, requerido: false, valorPredeterminado: 5);
            NormalizarDecimal(fila, "Costo", 0m, decimal.MaxValue, errores, requerido: true);
            NormalizarDecimal(fila, "Precio", 0.01m, decimal.MaxValue, errores, requerido: true);
            NormalizarActivo(fila, errores);

            var nombreProducto = NormalizarClave(V(fila, "Producto"));
            var candidatos = productos.Where(x => NormalizarClave(x.Nombre) == nombreProducto).ToList();
            if (candidatos.Count == 0)
            {
                AgregarError(errores, fila.NumeroFila, "Producto", "PRODUCTO_NO_EXISTE", "El producto debe existir antes de importar variantes.", V(fila, "Producto"));
                continue;
            }
            if (candidatos.Count > 1)
            {
                AgregarError(errores, fila.NumeroFila, "Producto", "PRODUCTO_AMBIGUO", "Existe más de una familia de producto con ese nombre. Corrige el catálogo antes de importar.", V(fila, "Producto"));
                continue;
            }
            var producto = candidatos[0];

            var marcaNombre = V(fila, "Marca");
            var modeloNombre = V(fila, "Modelo");
            var colorNombre = V(fila, "Color");
            var tallaNombre = V(fila, "Talla");
            Marca? marca = null;
            Modelo? modelo = null;
            Color? color = null;
            Talla? talla = null;

            if (!string.IsNullOrWhiteSpace(marcaNombre))
            {
                marca = marcas.FirstOrDefault(x => NormalizarClave(x.Nombre) == NormalizarClave(marcaNombre));
                if (marca is null)
                    AgregarError(errores, fila.NumeroFila, "Marca", "MARCA_NO_EXISTE", "La marca debe existir y estar activa antes de importar variantes.", marcaNombre);
            }

            if (!string.IsNullOrWhiteSpace(modeloNombre))
            {
                if (marca is null)
                    AgregarError(errores, fila.NumeroFila, "Modelo", "MODELO_REQUIERE_MARCA", "Todo modelo de una variante debe indicar su marca.", modeloNombre);
                else
                {
                    modelo = modelos.FirstOrDefault(x => x.MarcaId == marca.Id && NormalizarClave(x.Nombre) == NormalizarClave(modeloNombre));
                    if (modelo is null)
                        AgregarError(errores, fila.NumeroFila, "Modelo", "MODELO_NO_EXISTE", "El modelo no existe, está inactivo o no pertenece a la marca indicada.", modeloNombre);
                }
            }

            if (!string.IsNullOrWhiteSpace(colorNombre))
            {
                color = colores.FirstOrDefault(x => NormalizarClave(x.Nombre) == NormalizarClave(colorNombre));
                if (color is null)
                    AgregarError(errores, fila.NumeroFila, "Color", "COLOR_NO_EXISTE", "El color debe existir y estar activo antes de importar variantes.", colorNombre);
            }

            if (!string.IsNullOrWhiteSpace(tallaNombre))
            {
                talla = tallas.FirstOrDefault(x => NormalizarClave(x.Nombre) == NormalizarClave(tallaNombre));
                if (talla is null)
                    AgregarError(errores, fila.NumeroFila, "Talla", "TALLA_NO_EXISTE", "La talla o tamaño debe existir y estar activo antes de importar variantes.", tallaNombre);
            }

            if (marca is null && modelo is null && color is null && talla is null)
            {
                AgregarError(errores, fila.NumeroFila, "Producto", "VARIANTE_SIN_DIMENSION", "Una variante comercial debe definir al menos Marca, Modelo, Color o Talla.", V(fila, "Producto"));
                continue;
            }

            var existentePorCombinacion = variantes.FirstOrDefault(x =>
                x.ProductoId == producto.Id &&
                x.MarcaId == marca?.Id && x.ModeloId == modelo?.Id &&
                x.ColorId == color?.Id && x.TallaId == talla?.Id);

            var sku = V(fila, "SKU");
            if (string.IsNullOrWhiteSpace(sku))
            {
                sku = existentePorCombinacion?.Sku ?? $"VAR-{producto.Id:D6}-{(marca?.Id ?? 0):D4}-{(modelo?.Id ?? 0):D4}-{(color?.Id ?? 0):D4}-{(talla?.Id ?? 0):D4}";
                fila.Datos["SKU"] = sku;
            }

            var existentePorSku = variantes.FirstOrDefault(x => NormalizarClave(x.Sku) == NormalizarClave(sku));
            if (existentePorSku is not null && existentePorSku.Id != existentePorCombinacion?.Id)
                AgregarError(errores, fila.NumeroFila, "SKU", "SKU_DUPLICADO", "El SKU ya pertenece a otra variante.", sku);

            var codigo = V(fila, "CodigoBarras");
            if (!string.IsNullOrWhiteSpace(codigo) && variantes.Any(x => x.Id != existentePorCombinacion?.Id && NormalizarClave(x.CodigoBarras) == NormalizarClave(codigo)))
                AgregarError(errores, fila.NumeroFila, "CodigoBarras", "CODIGO_BARRAS_DUPLICADO", "El código de barras ya está asignado a otra variante.", codigo);

            var claveCombinacion = $"{producto.Id}|{marca?.Id ?? 0}|{modelo?.Id ?? 0}|{color?.Id ?? 0}|{talla?.Id ?? 0}";
            if (!combinacionesArchivo.Add(claveCombinacion))
                AgregarError(errores, fila.NumeroFila, "Producto", "DUPLICADO_COMBINACION_ARCHIVO", "La misma combinación de Producto, Marca, Modelo, Color y Talla aparece más de una vez en el archivo.", V(fila, "Producto"));
            if (!skusArchivo.Add(NormalizarClave(sku)))
                AgregarError(errores, fila.NumeroFila, "SKU", "DUPLICADO_SKU_ARCHIVO", "El SKU aparece más de una vez en el archivo.", sku);
            if (!string.IsNullOrWhiteSpace(codigo) && !codigosArchivo.Add(NormalizarClave(codigo)))
                AgregarError(errores, fila.NumeroFila, "CodigoBarras", "DUPLICADO_CODIGO_ARCHIVO", "El código de barras aparece más de una vez en el archivo.", codigo);

            var existente = existentePorCombinacion ?? existentePorSku;
            fila.ProductoIdSnapshot = producto.Id;
            fila.ProductoVarianteIdSnapshot = existente?.Id;
            fila.CantidadActualSnapshot = existente?.Cantidad;
            fila.FechaValidacionSnapshot = DateTime.UtcNow;
            fila.Accion = existente is null ? "Crear" : "Actualizar";
        }
    }
    private async Task<(int Creados, int Actualizados)> AplicarClientesAsync(List<CargaMasivaFilaDto> filas, CancellationToken ct)
    {
        var existentes = await _db.Clientes.ToListAsync(ct);
        var tiposClientes = await _db.TipoClientes.Where(t => t.Activo && !t.Eliminado).ToListAsync(ct);
        var defaultTipoClienteId = await _predeterminadoResolver.ResolverIdPredeterminadoAsync();

        var creados = 0;
        var actualizados = 0;
        foreach (var fila in filas)
        {
            var cliente = BuscarPersona(existentes, fila.Datos, x => x.IdentidadORTN, x => x.Correo, x => x.Telefono, x => x.Nombre, "IdentidadORTN");
            if (cliente is null)
            {
                cliente = new Cliente { CreadoPorUsuarioId = _currentUser.UsuarioId, CreadoPorNombreUsuario = _currentUser.NombreUsuario, TipoClienteId = defaultTipoClienteId };
                _db.Clientes.Add(cliente);
                existentes.Add(cliente);
                creados++;
            }
            else actualizados++;

            cliente.Nombre = V(fila, "Nombre")!;
            cliente.Telefono = NuloSiVacio(V(fila, "Telefono"));
            cliente.IdentidadORTN = NuloSiVacio(V(fila, "IdentidadORTN"));
            cliente.Correo = NuloSiVacio(V(fila, "Correo"));
            cliente.Direccion = NuloSiVacio(V(fila, "Direccion"));
            cliente.Activo = Booleano(fila, "Activo");

            var tipoNombre = V(fila, "TipoCliente");
            if (!string.IsNullOrWhiteSpace(tipoNombre))
            {
                var tipoObj = tiposClientes.FirstOrDefault(t => t.Nombre.Equals(tipoNombre.Trim(), StringComparison.OrdinalIgnoreCase));
                if (tipoObj is not null)
                {
                    cliente.TipoClienteId = tipoObj.Id;
                }
            }

            MarcarActualizacion(cliente);
        }
        return (creados, actualizados);
    }

    private async Task<(int Creados, int Actualizados)> AplicarProveedoresAsync(List<CargaMasivaFilaDto> filas, CancellationToken ct)
    {
        var existentes = await _db.Proveedores.ToListAsync(ct);
        var creados = 0;
        var actualizados = 0;
        foreach (var fila in filas)
        {
            var proveedor = BuscarPersona(existentes, fila.Datos, x => x.Documento, x => x.Correo, x => x.Telefono, x => x.Nombre, "Documento");
            if (proveedor is null)
            {
                proveedor = new Proveedor { CreadoPorUsuarioId = _currentUser.UsuarioId, CreadoPorNombreUsuario = _currentUser.NombreUsuario };
                _db.Proveedores.Add(proveedor);
                existentes.Add(proveedor);
                creados++;
            }
            else actualizados++;

            proveedor.Nombre = V(fila, "Nombre")!;
            proveedor.Telefono = NuloSiVacio(V(fila, "Telefono"));
            proveedor.Documento = NuloSiVacio(V(fila, "Documento"));
            proveedor.Correo = NuloSiVacio(V(fila, "Correo"));
            proveedor.Direccion = NuloSiVacio(V(fila, "Direccion"));
            proveedor.Activo = Booleano(fila, "Activo");
            MarcarActualizacion(proveedor);
        }
        return (creados, actualizados);
    }

    private async Task<(int Creados, int Actualizados)> AplicarColoresAsync(List<CargaMasivaFilaDto> filas, CancellationToken ct)
    {
        var normalizados = await _db.Colores.IgnoreQueryFilters().ToListAsync(ct);
        var creados = 0;
        var actualizados = 0;

        foreach (var fila in filas)
        {
            var nombre = V(fila, "Nombre")!;
            var clave = NormalizarClave(nombre);
            var normalizado = normalizados.FirstOrDefault(x => NormalizarClave(x.Nombre) == clave);

            if (normalizado is null)
            {
                normalizado = new Color
                {
                    CreadoPorUsuarioId = _currentUser.UsuarioId,
                    CreadoPorNombreUsuario = _currentUser.NombreUsuario
                };
                _db.Colores.Add(normalizado);
                normalizados.Add(normalizado);
                creados++;
            }
            else
            {
                actualizados++;
            }

            normalizado.Nombre = nombre;
            normalizado.CodigoVisual = NuloSiVacio(V(fila, "CodigoVisual"));
            normalizado.Descripcion = NuloSiVacio(V(fila, "Descripcion"));
            normalizado.Orden = Entero(fila, "Orden");
            normalizado.Activo = Booleano(fila, "Activo");
            normalizado.Eliminado = false;
            normalizado.FechaEliminacion = null;
            normalizado.EliminadoPorUsuarioId = null;
            MarcarActualizacion(normalizado);
        }

        return (creados, actualizados);
    }

    private async Task<(int Creados, int Actualizados)> AplicarProductosAsync(List<CargaMasivaFilaDto> filas, CancellationToken ct)
    {
        var marcas = await _db.Marcas.Where(x => x.Activo && !x.Eliminado).ToListAsync(ct);
        var modelos = await _db.Modelos.Where(x => x.Activo && !x.Eliminado).ToListAsync(ct);
        var tallas = await _db.Tallas.Where(x => x.Activo && !x.Eliminado).ToListAsync(ct);
        var categorias = await _db.Categorias.Where(x => !x.Eliminada).ToListAsync(ct);
        var existentes = await _db.Productos
            .Include(x => x.Variantes.Where(v => !v.Eliminado))
            .Where(x => !x.Eliminado)
            .ToListAsync(ct);
        var creados = 0;
        var actualizados = 0;

        foreach (var fila in filas)
        {
            var candidatos = existentes.Where(x => NormalizarClave(x.Nombre) == NormalizarClave(V(fila, "Nombre"))).ToList();
            if (candidatos.Count > 1)
                throw new BusinessRuleException($"El producto '{V(fila, "Nombre")}' es ambiguo. Revalida la carga.");

            var producto = candidatos.SingleOrDefault();
            var marca = marcas.First(x => NormalizarClave(x.Nombre) == NormalizarClave(V(fila, "Marca")));
            var modelo = modelos.First(x => x.MarcaId == marca.Id && NormalizarClave(x.Nombre) == NormalizarClave(V(fila, "Modelo")));
            var categoriaNombre = V(fila, "Categoria");
            var tallaNombre = V(fila, "Talla");
            var categoria = string.IsNullOrWhiteSpace(categoriaNombre) ? null : categorias.First(x => x.Activa && NormalizarClave(x.Nombre) == NormalizarClave(categoriaNombre));
            var talla = string.IsNullOrWhiteSpace(tallaNombre) ? null : tallas.First(x => NormalizarClave(x.Nombre) == NormalizarClave(tallaNombre));

            if (producto is null)
            {
                producto = new Producto
                {
                    Nombre = V(fila, "Nombre")!,
                    CategoriaId = categoria?.Id,
                    Descripcion = NuloSiVacio(V(fila, "Descripcion")),
                    Activo = Booleano(fila, "Activo"),
                    Eliminado = false,
                    Cantidad = 0,
                    CreadoPorUsuarioId = _currentUser.UsuarioId,
                    CreadoPorNombreUsuario = _currentUser.NombreUsuario
                };
                _db.Productos.Add(producto);
                await _db.SaveChangesAsync(ct);
                existentes.Add(producto);
                creados++;
            }
            else
            {
                actualizados++;
            }

            var variante = producto.Variantes.SingleOrDefault(v => v.EsTecnica && !v.Eliminado);
            if (variante is null && producto.Variantes.Any(v => !v.EsTecnica && !v.Eliminado))
                throw new BusinessRuleException($"El producto '{producto.Nombre}' usa variantes comerciales. Revalida y utiliza VariantesInventario.");

            if (variante is null)
            {
                variante = new ProductoVariante
                {
                    ProductoId = producto.Id,
                    Producto = producto,
                    Sku = $"TEC-{producto.Id:D10}",
                    Cantidad = 0,
                    EsTecnica = true,
                    CreadoPorUsuarioId = _currentUser.UsuarioId,
                    CreadoPorNombreUsuario = _currentUser.NombreUsuario
                };
                _db.ProductoVariantes.Add(variante);
                producto.Variantes.Add(variante);
            }

            variante.MarcaId = marca.Id;
            variante.ModeloId = modelo.Id;
            variante.ColorId = null;
            variante.TallaId = talla?.Id;
            variante.CodigoBarras = null;
            variante.Costo = Decimal(fila, "Costo");
            variante.Precio = Decimal(fila, "Precio");
            variante.UmbralStockBajo = Entero(fila, "UmbralStockBajo");
            variante.Activo = Booleano(fila, "Activo");
            variante.Eliminado = false;
            variante.FechaEliminacion = null;
            variante.EliminadoPorUsuarioId = null;
            MarcarActualizacion(variante);

            producto.Nombre = V(fila, "Nombre")!;
            producto.CategoriaId = categoria?.Id;
            producto.Descripcion = NuloSiVacio(V(fila, "Descripcion"));
            producto.Activo = variante.Activo;
            producto.Eliminado = false;
            producto.FechaEliminacion = null;
            producto.EliminadoPorUsuarioId = null;
            producto.Cantidad = variante.Cantidad;
            producto.Marca = marca.Nombre;
            producto.Modelo = modelo.Nombre;
            producto.MarcaId = variante.MarcaId;
            producto.ModeloId = variante.ModeloId;
            producto.ColorId = variante.ColorId;
            producto.TallaId = variante.TallaId;
            producto.Costo = variante.Costo ?? 0m;
            producto.Precio = variante.Precio ?? 0m;
            producto.UmbralStockBajo = variante.UmbralStockBajo;
            MarcarActualizacion(producto);
        }
        return (creados, actualizados);
    }

    private async Task<(int Creados, int Actualizados)> AplicarVariantesAsync(
        int cargaId,
        List<CargaMasivaFilaDto> filas,
        CancellationToken ct)
    {
        if (_db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("La confirmación de variantes requiere una transacción activa.");
        if (filas.Any(x => !x.ProductoIdSnapshot.HasValue))
            throw new BusinessRuleException("La carga no contiene snapshots completos. Valida el archivo nuevamente.");

        var productoIds = filas.Select(x => x.ProductoIdSnapshot!.Value).Distinct().OrderBy(x => x).ToArray();
        var varianteIds = filas.Where(x => x.ProductoVarianteIdSnapshot.HasValue).Select(x => x.ProductoVarianteIdSnapshot!.Value).Distinct().OrderBy(x => x).ToArray();
        var productos = new Dictionary<int, Producto>();
        foreach (var productoId in productoIds)
        {
            var producto = await _db.Productos
                .FromSqlInterpolated($"SELECT p.* FROM Productos p WHERE p.Id = {productoId} AND p.Eliminado = 0 FOR UPDATE")
                .AsTracking().SingleOrDefaultAsync(ct)
                ?? throw new BusinessRuleException($"El producto ID '{productoId}' ya no existe. Revalida el archivo.");
            productos.Add(producto.Id, producto);
        }

        var variantes = new Dictionary<int, ProductoVariante>();
        foreach (var varianteId in varianteIds)
        {
            var variante = await _db.ProductoVariantes
                .FromSqlInterpolated($"SELECT v.* FROM ProductoVariantes v WHERE v.Id = {varianteId} AND v.Eliminado = 0 FOR UPDATE")
                .AsTracking().SingleOrDefaultAsync(ct)
                ?? throw new BusinessRuleException($"La variante ID '{varianteId}' ya no existe. Revalida el archivo.");
            variantes.Add(variante.Id, variante);
        }

        // N0.3: una familia no puede mezclar variante técnica y variantes comerciales activas.
        // Se bloquea la técnica dentro de la misma transacción para que la conversión sea atómica.
        var tecnicas = new Dictionary<int, ProductoVariante>();
        foreach (var productoId in productoIds)
        {
            var tecnica = await _db.ProductoVariantes
                .FromSqlInterpolated($"SELECT v.* FROM ProductoVariantes v WHERE v.ProductoId = {productoId} AND v.EsTecnica = 1 AND v.Eliminado = 0 FOR UPDATE")
                .AsTracking().SingleOrDefaultAsync(ct);
            if (tecnica is not null)
                tecnicas.Add(productoId, tecnica);
        }

        var marcas = await _db.Marcas.Where(x => x.Activo && !x.Eliminado).ToListAsync(ct);
        var modelos = await _db.Modelos.Where(x => x.Activo && !x.Eliminado).ToListAsync(ct);
        var colores = await _db.Colores.Where(x => x.Activo && !x.Eliminado).ToListAsync(ct);
        var tallas = await _db.Tallas.Where(x => x.Activo && !x.Eliminado).ToListAsync(ct);
        var movimientos = new List<(Producto Producto, ProductoVariante Variante, CargaMasivaFilaDto Fila, int Anterior, int Nueva)>();
        var productosAfectados = new HashSet<int>();
        var creados = 0;
        var actualizados = 0;

        foreach (var fila in filas.OrderBy(x => x.ProductoIdSnapshot).ThenBy(x => x.ProductoVarianteIdSnapshot))
        {
            var producto = productos[fila.ProductoIdSnapshot!.Value];
            var marcaNombre = V(fila, "Marca");
            var modeloNombre = V(fila, "Modelo");
            var colorNombre = V(fila, "Color");
            var tallaNombre = V(fila, "Talla");
            var marca = string.IsNullOrWhiteSpace(marcaNombre) ? null : marcas.FirstOrDefault(x => NormalizarClave(x.Nombre) == NormalizarClave(marcaNombre));
            var modelo = string.IsNullOrWhiteSpace(modeloNombre) || marca is null ? null : modelos.FirstOrDefault(x => x.MarcaId == marca.Id && NormalizarClave(x.Nombre) == NormalizarClave(modeloNombre));
            var color = string.IsNullOrWhiteSpace(colorNombre) ? null : colores.FirstOrDefault(x => NormalizarClave(x.Nombre) == NormalizarClave(colorNombre));
            var talla = string.IsNullOrWhiteSpace(tallaNombre) ? null : tallas.FirstOrDefault(x => NormalizarClave(x.Nombre) == NormalizarClave(tallaNombre));

            if ((!string.IsNullOrWhiteSpace(marcaNombre) && marca is null) ||
                (!string.IsNullOrWhiteSpace(modeloNombre) && modelo is null) ||
                (!string.IsNullOrWhiteSpace(colorNombre) && color is null) ||
                (!string.IsNullOrWhiteSpace(tallaNombre) && talla is null))
                throw new BusinessRuleException("Una dimensión de la variante dejó de estar disponible. Revalida el archivo.");

            var marcaId = marca?.Id;
            var modeloId = modelo?.Id;
            var colorId = color?.Id;
            var tallaId = talla?.Id;
            var sku = V(fila, "SKU")!;
            var codigoBarras = NuloSiVacio(V(fila, "CodigoBarras"));
            ProductoVariante variante;
            int cantidadAnterior;

            if (fila.ProductoVarianteIdSnapshot.HasValue)
            {
                variante = variantes[fila.ProductoVarianteIdSnapshot.Value];
                if (variante.ProductoId != producto.Id || variante.MarcaId != marcaId || variante.ModeloId != modeloId || variante.ColorId != colorId || variante.TallaId != tallaId)
                    throw new BusinessRuleException("La identidad de la variante cambió después de validar. Revalida el archivo.");
                if (!fila.CantidadActualSnapshot.HasValue || variante.Cantidad != fila.CantidadActualSnapshot.Value)
                    throw new BusinessRuleException("El inventario cambió después de validar el archivo. Revalida la carga antes de confirmarla.");
                cantidadAnterior = variante.Cantidad;
                actualizados++;
            }
            else
            {
                if (tecnicas.TryGetValue(producto.Id, out var tecnica))
                {
                    if (tecnica.Cantidad != 0)
                        throw new BusinessRuleException($"El producto '{producto.Nombre}' conserva stock en su variante técnica. Ajusta o migra ese stock antes de crear variantes comerciales.");

                    tecnica.Activo = false;
                    tecnica.Eliminado = true;
                    tecnica.FechaEliminacion = DateTime.UtcNow;
                    tecnica.EliminadoPorUsuarioId = _currentUser.UsuarioId;
                    MarcarActualizacion(tecnica);
                    tecnicas.Remove(producto.Id);
                }

                var conflicto = await _db.ProductoVariantes.IgnoreQueryFilters().AsNoTracking().AnyAsync(x =>
                    !x.Eliminado && !x.EsTecnica &&
                    (x.Sku == sku ||
                     (codigoBarras != null && x.CodigoBarras == codigoBarras) ||
                     (x.ProductoId == producto.Id && x.MarcaId == marcaId && x.ModeloId == modeloId && x.ColorId == colorId && x.TallaId == tallaId)), ct);
                if (conflicto)
                    throw new BusinessRuleException("Una variante fue creada o modificada después de validar el archivo. Revalida la carga.");

                variante = new ProductoVariante
                {
                    ProductoId = producto.Id,
                    CreadoPorUsuarioId = _currentUser.UsuarioId,
                    CreadoPorNombreUsuario = _currentUser.NombreUsuario
                };
                _db.ProductoVariantes.Add(variante);
                cantidadAnterior = 0;
                creados++;
            }

            var cantidadNueva = Entero(fila, "Cantidad");
            variante.MarcaId = marcaId;
            variante.ModeloId = modeloId;
            variante.ColorId = colorId;
            variante.TallaId = tallaId;
            variante.Sku = sku;
            variante.CodigoBarras = codigoBarras;
            variante.Cantidad = cantidadNueva;
            variante.UmbralStockBajo = Entero(fila, "UmbralStockBajo");
            variante.Costo = Decimal(fila, "Costo");
            variante.Precio = Decimal(fila, "Precio");
            variante.Activo = Booleano(fila, "Activo");
            variante.Eliminado = false;
            variante.EsTecnica = false;
            variante.FechaEliminacion = null;
            variante.EliminadoPorUsuarioId = null;
            MarcarActualizacion(variante);
            movimientos.Add((producto, variante, fila, cantidadAnterior, cantidadNueva));
            productosAfectados.Add(producto.Id);
        }

        await _db.SaveChangesAsync(ct);

        foreach (var item in movimientos.Where(x => x.Anterior != x.Nueva))
        {
            _db.MovimientosInventario.Add(new MovimientoInventario
            {
                ProductoId = item.Producto.Id,
                ProductoVarianteId = item.Variante.Id,
                ProductoMarcaSnapshot = NuloSiVacio(V(item.Fila, "Marca")),
                ProductoModeloSnapshot = NuloSiVacio(V(item.Fila, "Modelo")),
                ProductoColorSnapshot = NuloSiVacio(V(item.Fila, "Color")),
                ProductoTallaSnapshot = NuloSiVacio(V(item.Fila, "Talla")),
                ProductoSkuSnapshot = item.Variante.Sku,
                Tipo = TipoMovimientoInventario.Ajuste,
                Cantidad = Math.Abs(item.Nueva - item.Anterior),
                StockAnterior = item.Anterior,
                StockNuevo = item.Nueva,
                CostoUnitario = item.Variante.Costo,
                PrecioUnitario = item.Variante.Precio,
                ReferenciaTipo = "CargaMasiva",
                ReferenciaId = cargaId,
                Descripcion = $"Ajuste por carga masiva #{cargaId}",
                CreadoPorUsuarioId = _currentUser.UsuarioId,
                CreadoPorNombreUsuario = _currentUser.NombreUsuario
            });
        }

        foreach (var producto in productos.Values.Where(x => productosAfectados.Contains(x.Id)))
        {
            var lista = await _db.ProductoVariantes.Where(x => x.ProductoId == producto.Id && !x.Eliminado).ToListAsync(ct);
            var total = lista.Sum(x => x.Cantidad);
            producto.Cantidad = total;
            if (lista.Count > 0)
            {
                producto.Costo = total > 0
                    ? Math.Round(lista.Sum(x => (x.Costo ?? 0m) * x.Cantidad) / total, 2, MidpointRounding.AwayFromZero)
                    : lista.Average(x => x.Costo ?? producto.Costo);
                var activas = lista.Where(x => x.Activo).ToList();
                producto.Precio = (activas.Count > 0 ? activas : lista).Min(x => x.Precio ?? 0m);
                producto.UmbralStockBajo = lista.Sum(x => x.UmbralStockBajo);
                producto.MarcaId = ValorComunCompat(lista.Select(x => x.MarcaId));
                producto.ModeloId = ValorComunCompat(lista.Select(x => x.ModeloId));
                producto.ColorId = ValorComunCompat(lista.Select(x => x.ColorId));
                producto.TallaId = ValorComunCompat(lista.Select(x => x.TallaId));
            }
            MarcarActualizacion(producto);
        }

        return (creados, actualizados);
    }
    private static int? ValorComunCompat(IEnumerable<int?> valores)
    {
        var lista = valores.Distinct().Take(2).ToList();
        return lista.Count == 1 ? lista[0] : null;
    }

    private static CargaMasivaDto MapResumenExpression(CargaMasiva x) => new()
    {
        Id = x.Id,
        Tipo = x.Tipo.ToString(),
        Estado = x.Estado.ToString(),
        NombreArchivo = x.NombreArchivo,
        TamanoBytes = x.TamanoBytes,
        TotalFilas = x.TotalFilas,
        FilasValidas = x.FilasValidas,
        FilasConError = x.FilasConError,
        FilasConAdvertencia = x.FilasConAdvertencia,
        FilasProcesadas = x.FilasProcesadas,
        RegistrosCreados = x.RegistrosCreados,
        RegistrosActualizados = x.RegistrosActualizados,
        FechaValidacion = x.FechaValidacion,
        FechaConfirmacion = x.FechaConfirmacion,
        CreadoPorNombreUsuario = x.CreadoPorNombreUsuario,
        ConfirmadoPorNombreUsuario = x.ConfirmadoPorNombreUsuario,
        ErrorGeneral = x.ErrorGeneral,
        FechaCreacion = x.FechaCreacion
    };

    private static CargaMasivaDetalleDto MapDetalle(
        CargaMasiva carga,
        List<CargaMasivaFilaDto> filas,
        List<CargaMasivaErrorDto> errores)
    {
        var resumen = MapResumenExpression(carga);
        return new CargaMasivaDetalleDto
        {
            Id = resumen.Id,
            Tipo = resumen.Tipo,
            Estado = resumen.Estado,
            NombreArchivo = resumen.NombreArchivo,
            TamanoBytes = resumen.TamanoBytes,
            TotalFilas = resumen.TotalFilas,
            FilasValidas = resumen.FilasValidas,
            FilasConError = resumen.FilasConError,
            FilasConAdvertencia = resumen.FilasConAdvertencia,
            FilasProcesadas = resumen.FilasProcesadas,
            RegistrosCreados = resumen.RegistrosCreados,
            RegistrosActualizados = resumen.RegistrosActualizados,
            FechaValidacion = resumen.FechaValidacion,
            FechaConfirmacion = resumen.FechaConfirmacion,
            CreadoPorNombreUsuario = resumen.CreadoPorNombreUsuario,
            ConfirmadoPorNombreUsuario = resumen.ConfirmadoPorNombreUsuario,
            ErrorGeneral = resumen.ErrorGeneral,
            FechaCreacion = resumen.FechaCreacion,
            PuedeConfirmarse = carga.Estado == EstadoCargaMasiva.Validada && carga.FilasConError == 0 && filas.Count > 0,
            Filas = filas,
            Errores = errores
        };
    }

    private static CargaMasivaErrorDto MapError(CargaMasivaError error) => new()
    {
        NumeroFila = error.NumeroFila,
        Campo = error.Campo,
        Codigo = error.Codigo,
        Mensaje = error.Mensaje,
        ValorOriginal = error.ValorOriginal,
        EsAdvertencia = error.EsAdvertencia
    };

    private static List<CargaMasivaFilaDto> DeserializarFilas(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<CargaMasivaFilaDto>>(json, JsonOptions) ?? new();
        }
        catch (JsonException)
        {
            throw new BusinessRuleException("La vista previa almacenada no puede recuperarse. Valida el archivo nuevamente.");
        }
    }

    private static void NormalizarTexto(
        CargaMasivaFilaDto fila,
        string campo,
        int maximo,
        bool requerido,
        List<CargaMasivaErrorDto> errores,
        bool mayusculas = false)
    {
        var valor = Limpiar(V(fila, campo));
        if (requerido && string.IsNullOrWhiteSpace(valor))
            AgregarError(errores, fila.NumeroFila, campo, "CAMPO_OBLIGATORIO", $"El campo '{campo}' es obligatorio.", valor);
        if (valor?.Length > maximo)
            AgregarError(errores, fila.NumeroFila, campo, "LONGITUD_MAXIMA", $"El campo '{campo}' no puede superar {maximo} caracteres.", valor);
        fila.Datos[campo] = mayusculas ? valor?.ToUpperInvariant() : valor;
    }

    private static void NormalizarEntero(
        CargaMasivaFilaDto fila,
        string campo,
        int minimo,
        int maximo,
        List<CargaMasivaErrorDto> errores,
        bool requerido,
        int valorPredeterminado = 0)
    {
        var valor = V(fila, campo);
        if (string.IsNullOrWhiteSpace(valor))
        {
            if (requerido) AgregarError(errores, fila.NumeroFila, campo, "CAMPO_OBLIGATORIO", $"El campo '{campo}' es obligatorio.", valor);
            fila.Datos[campo] = valorPredeterminado.ToString(CultureInfo.InvariantCulture);
            return;
        }
        if (!int.TryParse(valor, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numero) || numero < minimo || numero > maximo)
        {
            AgregarError(errores, fila.NumeroFila, campo, "ENTERO_INVALIDO", $"El campo '{campo}' debe ser un entero entre {minimo} y {maximo}.", valor);
            return;
        }
        fila.Datos[campo] = numero.ToString(CultureInfo.InvariantCulture);
    }

    private static void NormalizarDecimal(
        CargaMasivaFilaDto fila,
        string campo,
        decimal minimo,
        decimal maximo,
        List<CargaMasivaErrorDto> errores,
        bool requerido)
    {
        var valor = V(fila, campo);
        if (string.IsNullOrWhiteSpace(valor))
        {
            if (requerido) AgregarError(errores, fila.NumeroFila, campo, "CAMPO_OBLIGATORIO", $"El campo '{campo}' es obligatorio.", valor);
            return;
        }
        if (!TryDecimal(valor, out var numero) || numero < minimo || numero > maximo)
        {
            AgregarError(errores, fila.NumeroFila, campo, "DECIMAL_INVALIDO", $"El campo '{campo}' debe ser un monto válido mayor o igual que {minimo.ToString("0.##", CultureInfo.InvariantCulture)}.", valor);
            return;
        }
        fila.Datos[campo] = numero.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private static void NormalizarActivo(CargaMasivaFilaDto fila, List<CargaMasivaErrorDto> errores)
    {
        var valor = V(fila, "Activo");
        if (string.IsNullOrWhiteSpace(valor))
        {
            fila.Datos["Activo"] = "true";
            return;
        }
        if (!TryBool(valor, out var activo))
        {
            AgregarError(errores, fila.NumeroFila, "Activo", "BOOLEANO_INVALIDO", "Activo debe indicar Si/No, Verdadero/Falso o 1/0.", valor);
            return;
        }
        fila.Datos["Activo"] = activo ? "true" : "false";
    }

    private static void ValidarCorreo(CargaMasivaFilaDto fila, string campo, List<CargaMasivaErrorDto> errores)
    {
        var correo = V(fila, campo);
        if (!string.IsNullOrWhiteSpace(correo) && !MailAddress.TryCreate(correo, out _))
            AgregarError(errores, fila.NumeroFila, campo, "CORREO_INVALIDO", "El correo electrónico no tiene un formato válido.", correo);
    }

    private static T? BuscarPersona<T>(
        IEnumerable<T> existentes,
        IReadOnlyDictionary<string, string?> datos,
        Func<T, string?> documento,
        Func<T, string?> correo,
        Func<T, string?> telefono,
        Func<T, string> nombre,
        string campoDocumento)
        where T : class
    {
        var doc = datos.TryGetValue(campoDocumento, out var d) ? d : null;
        var mail = datos.TryGetValue("Correo", out var c) ? c : null;
        var tel = datos.TryGetValue("Telefono", out var t) ? t : null;
        var nom = datos.TryGetValue("Nombre", out var n) ? n : null;

        if (!string.IsNullOrWhiteSpace(doc))
        {
            var found = existentes.FirstOrDefault(x => NormalizarClave(documento(x)) == NormalizarClave(doc));
            if (found is not null) return found;
        }
        if (!string.IsNullOrWhiteSpace(mail))
        {
            var found = existentes.FirstOrDefault(x => NormalizarClave(correo(x)) == NormalizarClave(mail));
            if (found is not null) return found;
        }
        if (!string.IsNullOrWhiteSpace(tel))
        {
            var found = existentes.FirstOrDefault(x => NormalizarClave(telefono(x)) == NormalizarClave(tel));
            if (found is not null) return found;
        }
        return existentes.FirstOrDefault(x => NormalizarClave(nombre(x)) == NormalizarClave(nom));
    }

    private static string ClavePersona(IReadOnlyDictionary<string, string?> datos, string campoDocumento)
    {
        if (datos.TryGetValue(campoDocumento, out var documento) && !string.IsNullOrWhiteSpace(documento)) return $"D:{NormalizarClave(documento)}";
        if (datos.TryGetValue("Correo", out var correo) && !string.IsNullOrWhiteSpace(correo)) return $"C:{NormalizarClave(correo)}";
        if (datos.TryGetValue("Telefono", out var telefono) && !string.IsNullOrWhiteSpace(telefono)) return $"T:{NormalizarClave(telefono)}";
        return $"N:{NormalizarClave(datos.TryGetValue("Nombre", out var nombre) ? nombre : null)}";
    }

    private static string ClaveProducto(IReadOnlyDictionary<string, string?> datos) =>
        ClaveProducto(
            datos.TryGetValue("Nombre", out var nombre) ? nombre : datos.TryGetValue("Producto", out var producto) ? producto : null,
            datos.TryGetValue("Marca", out var marca) ? marca : null,
            datos.TryGetValue("Modelo", out var modelo) ? modelo : null);

    private static string ClaveProducto(string? nombre, string? marca, string? modelo) =>
        $"{NormalizarClave(nombre)}|{NormalizarClave(marca)}|{NormalizarClave(modelo)}";

    private static string NormalizarClave(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var sinAcentos = new string(normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
        return Regex.Replace(sinAcentos, "\\s+", " ").ToUpperInvariant();
    }

    private static bool TryBool(string valor, out bool resultado)
    {
        switch (NormalizarClave(valor))
        {
            case "SI": case "S": case "TRUE": case "VERDADERO": case "1": case "ACTIVO":
                resultado = true; return true;
            case "NO": case "N": case "FALSE": case "FALSO": case "0": case "INACTIVO":
                resultado = false; return true;
            default:
                resultado = false; return false;
        }
    }

    private static bool TryDecimal(string valor, out decimal resultado)
    {
        if (decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out resultado)) return true;
        return decimal.TryParse(valor, NumberStyles.Number, CultureInfo.GetCultureInfo("es-HN"), out resultado);
    }

    private static string? V(CargaMasivaFilaDto fila, string campo) =>
        fila.Datos.TryGetValue(campo, out var valor) ? valor : null;

    private static int Entero(CargaMasivaFilaDto fila, string campo) =>
        int.Parse(V(fila, campo) ?? "0", CultureInfo.InvariantCulture);

    private static decimal Decimal(CargaMasivaFilaDto fila, string campo) =>
        decimal.Parse(V(fila, campo) ?? "0", CultureInfo.InvariantCulture);

    private static bool Booleano(CargaMasivaFilaDto fila, string campo) =>
        bool.Parse(V(fila, campo) ?? "true");

    private static string? Limpiar(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? NuloSiVacio(string? value) => Limpiar(value);
    private static string? Limitar(string? value, int maximo) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, maximo)];

    private void MarcarActualizacion(Solqaryn.Domain.Common.AuditableEntity entity)
    {
        entity.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        entity.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        entity.FechaActualizacion = DateTime.UtcNow;
    }

    private static void AgregarError(
        ICollection<CargaMasivaErrorDto> errores,
        int fila,
        string? campo,
        string codigo,
        string mensaje,
        string? valor,
        bool advertencia = false)
    {
        errores.Add(new CargaMasivaErrorDto
        {
            NumeroFila = fila,
            Campo = campo,
            Codigo = codigo,
            Mensaje = mensaje,
            ValorOriginal = valor,
            EsAdvertencia = advertencia
        });
    }
}
