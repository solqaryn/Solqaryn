using System.Security.Cryptography;
using System.Text;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Fiscal;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Services;

/// <summary>
/// Orquesta una emisión fiscal sin asumir legislación, autoridad o formato nacional único.
/// La fila DocumentoFiscalEmision actúa como claim durable por tenant/proveedor/idempotencia:
/// primero se persiste el claim y sólo el ganador invoca al adaptador externo.
/// </summary>
public sealed class DocumentoFiscalEmisionService
{
    private static readonly TimeSpan ClaimAbandonado = TimeSpan.FromMinutes(2);

    private readonly AppDbContext _db;
    private readonly IReadOnlyList<IProveedorDocumentoFiscal> _proveedores;

    public DocumentoFiscalEmisionService(
        AppDbContext db,
        IEnumerable<IProveedorDocumentoFiscal> proveedores)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _proveedores = proveedores?.ToList()
            ?? throw new ArgumentNullException(nameof(proveedores));
    }

    public async Task<ResultadoOperacionDocumentoFiscal> EmitirAsync(
        SolicitudEmisionFiscal solicitud,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        var proveedor = ResolverProveedor(solicitud.Perfil);
        var hashIdempotencia = CalcularHashIdempotencia(solicitud.ClaveIdempotencia);
        var ahora = DateTime.UtcNow;

        var existente = await BuscarExistenteAsync(
            solicitud.Perfil.EmpresaId,
            solicitud.Perfil.Proveedor,
            hashIdempotencia,
            cancellationToken);

        if (existente is not null)
        {
            ValidarMismaOperacion(existente, solicitud);

            if (existente.Estado != EstadoResultadoFiscal.Pendiente)
                return DesdePersistencia(existente, idempotente: true, reintentoAceptado: false);

            var claimExpirado = existente.UltimaActualizacionUtc <= ahora - ClaimAbandonado;
            if (!existente.EsTransitorio && !claimExpirado)
                return DesdePersistencia(existente, idempotente: true, reintentoAceptado: false);

            if (!await IntentarTomarReintentoAsync(existente, ahora, cancellationToken))
            {
                var ganador = await BuscarExistenteAsync(
                    solicitud.Perfil.EmpresaId,
                    solicitud.Perfil.Proveedor,
                    hashIdempotencia,
                    cancellationToken)
                    ?? throw new InvalidOperationException("El claim fiscal desapareció durante la reconciliación de concurrencia.");

                return DesdePersistencia(ganador, idempotente: true, reintentoAceptado: false);
            }

            return await EjecutarProveedorYGuardarAsync(
                existente,
                proveedor,
                solicitud,
                idempotente: true,
                reintentoAceptado: true,
                cancellationToken);
        }

        var nuevo = new DocumentoFiscalEmision
        {
            EmpresaId = solicitud.Perfil.EmpresaId,
            SucursalId = solicitud.Perfil.SucursalId,
            FacturaId = solicitud.FacturaId,
            Jurisdiccion = solicitud.Perfil.Jurisdiccion,
            Proveedor = solicitud.Perfil.Proveedor,
            TipoDocumento = solicitud.Perfil.TipoDocumento,
            ClaveIdempotenciaHash = hashIdempotencia,
            HashSnapshot = solicitud.HashSnapshot,
            Estado = EstadoResultadoFiscal.Pendiente,
            EsTransitorio = false,
            CreadoUtc = ahora,
            UltimaActualizacionUtc = ahora
        };

        _db.Set<DocumentoFiscalEmision>().Add(nuevo);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            _db.Entry(nuevo).State = EntityState.Detached;

            var ganador = await BuscarExistenteAsync(
                solicitud.Perfil.EmpresaId,
                solicitud.Perfil.Proveedor,
                hashIdempotencia,
                cancellationToken);

            if (ganador is null)
                throw;

            ValidarMismaOperacion(ganador, solicitud);
            return DesdePersistencia(ganador, idempotente: true, reintentoAceptado: false);
        }

        return await EjecutarProveedorYGuardarAsync(
            nuevo,
            proveedor,
            solicitud,
            idempotente: false,
            reintentoAceptado: false,
            cancellationToken);
    }

    private IProveedorDocumentoFiscal ResolverProveedor(PerfilFiscalDocumento perfil)
    {
        var compatibles = _proveedores
            .Where(p => string.Equals(p.Codigo?.Trim(), perfil.Proveedor, StringComparison.OrdinalIgnoreCase))
            .Where(p => p.Soporta(perfil))
            .Take(2)
            .ToList();

        return compatibles.Count switch
        {
            1 => compatibles[0],
            0 => throw new DocumentoFiscalProveedorNoDisponibleException(
                $"No existe un adaptador fiscal configurado para proveedor '{perfil.Proveedor}' y jurisdicción '{perfil.Jurisdiccion}'."),
            _ => throw new DocumentoFiscalProveedorNoDisponibleException(
                $"Existe más de un adaptador fiscal compatible para proveedor '{perfil.Proveedor}' y jurisdicción '{perfil.Jurisdiccion}'.")
        };
    }

    private async Task<DocumentoFiscalEmision?> BuscarExistenteAsync(
        int empresaId,
        string proveedor,
        string hashIdempotencia,
        CancellationToken cancellationToken)
    {
        return await _db.Set<DocumentoFiscalEmision>()
            .AsTracking()
            .SingleOrDefaultAsync(x =>
                x.EmpresaId == empresaId &&
                x.Proveedor == proveedor &&
                x.ClaveIdempotenciaHash == hashIdempotencia,
                cancellationToken);
    }

    private async Task<bool> IntentarTomarReintentoAsync(
        DocumentoFiscalEmision existente,
        DateTime ahora,
        CancellationToken cancellationToken)
    {
        var versionObservada = existente.UltimaActualizacionUtc;

        if (!_db.Database.IsRelational())
        {
            existente.UltimaActualizacionUtc = ahora;
            existente.EsTransitorio = false;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        _db.Entry(existente).State = EntityState.Detached;
        var actualizadas = await _db.Set<DocumentoFiscalEmision>()
            .Where(x =>
                x.Id == existente.Id &&
                x.Estado == EstadoResultadoFiscal.Pendiente &&
                x.UltimaActualizacionUtc == versionObservada)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.UltimaActualizacionUtc, ahora)
                    .SetProperty(x => x.EsTransitorio, false),
                cancellationToken);

        if (actualizadas != 1)
            return false;

        existente.UltimaActualizacionUtc = ahora;
        existente.EsTransitorio = false;
        _db.Attach(existente);
        return true;
    }

    private async Task<ResultadoOperacionDocumentoFiscal> EjecutarProveedorYGuardarAsync(
        DocumentoFiscalEmision registro,
        IProveedorDocumentoFiscal proveedor,
        SolicitudEmisionFiscal solicitud,
        bool idempotente,
        bool reintentoAceptado,
        CancellationToken cancellationToken)
    {
        try
        {
            var respuesta = await proveedor.EmitirAsync(solicitud, cancellationToken);
            registro.Estado = respuesta.Estado;
            registro.ReferenciaExterna = respuesta.ReferenciaExterna;
            registro.CodigoProveedor = respuesta.CodigoProveedor;
            registro.EsTransitorio = respuesta.EsTransitorio;
            registro.UltimaActualizacionUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            return new ResultadoOperacionDocumentoFiscal(
                registro.Id,
                registro.Estado,
                registro.ReferenciaExterna,
                registro.CodigoProveedor,
                respuesta.Mensaje,
                registro.EsTransitorio,
                idempotente,
                reintentoAceptado);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            registro.Estado = EstadoResultadoFiscal.Pendiente;
            registro.EsTransitorio = true;
            registro.CodigoProveedor = "PROVIDER_UNAVAILABLE";
            registro.UltimaActualizacionUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            throw new DocumentoFiscalProveedorNoDisponibleException(
                "El proveedor fiscal no pudo completar la emisión. El mismo idempotency key puede reintentarse de forma segura.",
                ex,
                registro.Id);
        }
    }

    private static void ValidarMismaOperacion(
        DocumentoFiscalEmision existente,
        SolicitudEmisionFiscal solicitud)
    {
        if (existente.FacturaId != solicitud.FacturaId ||
            existente.SucursalId != solicitud.Perfil.SucursalId ||
            !string.Equals(existente.Jurisdiccion, solicitud.Perfil.Jurisdiccion, StringComparison.Ordinal) ||
            !string.Equals(existente.Proveedor, solicitud.Perfil.Proveedor, StringComparison.Ordinal) ||
            !string.Equals(existente.TipoDocumento, solicitud.Perfil.TipoDocumento, StringComparison.Ordinal) ||
            !string.Equals(existente.HashSnapshot, solicitud.HashSnapshot, StringComparison.Ordinal))
        {
            throw new DocumentoFiscalIdempotenciaException(
                "La clave de idempotencia ya existe para una operación fiscal con factura, perfil o snapshot diferente.");
        }
    }

    private static ResultadoOperacionDocumentoFiscal DesdePersistencia(
        DocumentoFiscalEmision registro,
        bool idempotente,
        bool reintentoAceptado)
    {
        return new ResultadoOperacionDocumentoFiscal(
            registro.Id,
            registro.Estado,
            registro.ReferenciaExterna,
            registro.CodigoProveedor,
            null,
            registro.EsTransitorio,
            idempotente,
            reintentoAceptado);
    }

    internal static string CalcularHashIdempotencia(string clave)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(clave));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

public sealed record ResultadoOperacionDocumentoFiscal(
    int RegistroId,
    EstadoResultadoFiscal Estado,
    string? ReferenciaExterna,
    string? CodigoProveedor,
    string? Mensaje,
    bool EsTransitorio,
    bool Idempotente,
    bool ReintentoAceptado);

public sealed class DocumentoFiscalIdempotenciaException : InvalidOperationException
{
    public DocumentoFiscalIdempotenciaException(string message) : base(message) { }
}

public sealed class DocumentoFiscalProveedorNoDisponibleException : InvalidOperationException
{
    public DocumentoFiscalProveedorNoDisponibleException(string message)
        : base(message) { }

    public DocumentoFiscalProveedorNoDisponibleException(
        string message,
        Exception innerException,
        int? registroId = null)
        : base(message, innerException)
    {
        RegistroId = registroId;
    }

    public int? RegistroId { get; }
}
