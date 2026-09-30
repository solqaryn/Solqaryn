using Solqaryn.API.Filters;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Domain.Fiscal;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.API.Controllers;

[ApiController]
[Authorize]
[RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Crear)]
[Route("facturacion-fiscal")]
public sealed class FacturacionFiscalController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IUsuarioScopeService _usuarioScope;
    private readonly IAuditoriaService _auditoria;
    private readonly DocumentoFiscalEmisionService _service;

    public FacturacionFiscalController(
        AppDbContext db,
        IUsuarioScopeService usuarioScope,
        IEnumerable<IProveedorDocumentoFiscal> proveedores,
        IAuditoriaService auditoria)
    {
        _db = db;
        _usuarioScope = usuarioScope;
        _auditoria = auditoria;
        _service = new DocumentoFiscalEmisionService(db, proveedores);
    }

    [HttpPost("emisiones")]
    public async Task<IActionResult> Emitir(
        [FromBody] EmitirDocumentoFiscalRequest request,
        CancellationToken cancellationToken)
    {
        SolicitudEmisionFiscal solicitud;
        try
        {
            var perfil = new PerfilFiscalDocumento(
                request.EmpresaId,
                request.SucursalId,
                request.Jurisdiccion,
                request.Proveedor,
                request.TipoDocumento);

            solicitud = new SolicitudEmisionFiscal(
                perfil,
                request.FacturaId,
                request.ClaveIdempotencia,
                request.HashSnapshot);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "SOLICITUD_FISCAL_INVALIDA", mensaje = ex.Message });
        }

        // EmpresaId nunca se acepta como autoridad sólo porque venga por HTTP.
        // La membresía tenant-aware se relee server-side y falla cerrada.
        var tenant = await _usuarioScope.ObtenerActualAsync(request.EmpresaId, cancellationToken);
        if (tenant is null)
            return Forbid();

        // La identidad fiscal del tenant se toma de Empresa, no del payload. Para una
        // emisión fiscal exigimos una identidad legal verificable; un tenant sin RTN
        // no puede usar este endpoint hasta completar su configuración.
        var empresaRtn = await _db.Set<Empresa>()
            .AsNoTracking()
            .Where(x => x.Id == request.EmpresaId && x.Activa)
            .Select(x => x.Rtn)
            .SingleOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(empresaRtn))
        {
            return Conflict(new
            {
                error = "EMPRESA_SIN_IDENTIDAD_FISCAL",
                mensaje = "La empresa activa no tiene una identidad fiscal verificable configurada."
            });
        }

        if (request.SucursalId.HasValue)
        {
            var sucursalValida = await _db.Set<Sucursal>()
                .AsNoTracking()
                .AnyAsync(x =>
                    x.Id == request.SucursalId.Value &&
                    x.EmpresaId == request.EmpresaId &&
                    x.Activa &&
                    !x.Eliminado,
                    cancellationToken);

            if (!sucursalValida)
            {
                return NotFound(new
                {
                    error = "SUCURSAL_NO_DISPONIBLE_EN_TENANT",
                    mensaje = "La sucursal no existe, no está activa o no pertenece a la empresa solicitada."
                });
            }
        }

        // Factura es una entidad legacy sin EmpresaId directo. El creador de una venta
        // puede pertenecer a múltiples empresas y por sí solo NO demuestra ownership.
        // Para cerrar ese bypass, la factura sólo es elegible cuando su snapshot fiscal
        // de RTN coincide exactamente (normalizado) con la identidad legal del tenant.
        var facturaScope = await _db.Facturas
            .AsNoTracking()
            .Where(x => x.Id == solicitud.FacturaId)
            .Select(x => new { x.Id, x.EmpresaRTN })
            .SingleOrDefaultAsync(cancellationToken);

        if (facturaScope is null ||
            string.IsNullOrWhiteSpace(facturaScope.EmpresaRTN) ||
            !string.Equals(
                NormalizarIdentidadFiscal(facturaScope.EmpresaRTN),
                NormalizarIdentidadFiscal(empresaRtn),
                StringComparison.Ordinal))
        {
            return NotFound(new
            {
                error = "FACTURA_NO_DISPONIBLE_EN_TENANT",
                mensaje = "La factura no existe o su identidad fiscal no coincide con la empresa solicitada."
            });
        }

        try
        {
            var resultado = await _service.EmitirAsync(solicitud, cancellationToken);
            await RegistrarAuditoriaAsync(
                request,
                resultado.RegistroId,
                resultado.Estado.ToString(),
                resultado.Idempotente,
                resultado.ReintentoAceptado,
                resultado: "Exito");

            var body = new DocumentoFiscalEmisionResponse(
                resultado.RegistroId,
                resultado.Estado.ToString(),
                resultado.ReferenciaExterna,
                resultado.CodigoProveedor,
                resultado.Mensaje,
                resultado.EsTransitorio,
                resultado.Idempotente,
                resultado.ReintentoAceptado);

            return resultado.Estado switch
            {
                EstadoResultadoFiscal.Confirmado => Ok(body),
                EstadoResultadoFiscal.Rechazado => UnprocessableEntity(body),
                _ => Accepted(body)
            };
        }
        catch (DocumentoFiscalIdempotenciaException)
        {
            await RegistrarAuditoriaAsync(
                request,
                referenciaId: null,
                estadoFiscal: "RECHAZADO_IDEMPOTENCIA",
                idempotente: true,
                reintentoAceptado: false,
                resultado: "Rechazado",
                error: "IDEMPOTENCY_KEY_REUTILIZADA");

            return Conflict(new
            {
                error = "IDEMPOTENCY_KEY_REUTILIZADA",
                mensaje = "La clave de idempotencia ya existe para una operación fiscal distinta."
            });
        }
        catch (DocumentoFiscalProveedorNoDisponibleException ex)
        {
            await RegistrarAuditoriaAsync(
                request,
                ex.RegistroId,
                estadoFiscal: "PENDIENTE_PROVEEDOR",
                idempotente: false,
                reintentoAceptado: ex.RegistroId.HasValue,
                resultado: "Error",
                error: "PROVEEDOR_FISCAL_NO_DISPONIBLE");

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "PROVEEDOR_FISCAL_NO_DISPONIBLE",
                mensaje = "El proveedor fiscal no pudo completar la emisión. La operación puede reintentarse de forma segura con la misma clave de idempotencia.",
                registroId = ex.RegistroId
            });
        }
    }

    private Task RegistrarAuditoriaAsync(
        EmitirDocumentoFiscalRequest request,
        int? referenciaId,
        string estadoFiscal,
        bool idempotente,
        bool reintentoAceptado,
        string resultado,
        string? error = null)
    {
        // No registrar ClaveIdempotencia, HashSnapshot, payload del proveedor,
        // referencias externas ni mensajes crudos: son material sensible o correlable.
        return _auditoria.RegistrarAsync(
            ModuloSistema.Facturacion,
            AccionPermiso.Crear,
            "Emisión fiscal procesada mediante frontera tenant-aware.",
            referenciaId,
            "DocumentoFiscalEmision",
            valoresNuevos: new
            {
                request.EmpresaId,
                request.SucursalId,
                request.FacturaId,
                request.Jurisdiccion,
                request.Proveedor,
                request.TipoDocumento,
                Estado = estadoFiscal,
                Idempotente = idempotente,
                ReintentoAceptado = reintentoAceptado
            },
            resultado: resultado,
            error: error);
    }

    private static string NormalizarIdentidadFiscal(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
}

public sealed record EmitirDocumentoFiscalRequest(
    int EmpresaId,
    int? SucursalId,
    int FacturaId,
    string Jurisdiccion,
    string Proveedor,
    string TipoDocumento,
    string ClaveIdempotencia,
    string HashSnapshot);

public sealed record DocumentoFiscalEmisionResponse(
    int RegistroId,
    string Estado,
    string? ReferenciaExterna,
    string? CodigoProveedor,
    string? Mensaje,
    bool EsTransitorio,
    bool Idempotente,
    bool ReintentoAceptado);
