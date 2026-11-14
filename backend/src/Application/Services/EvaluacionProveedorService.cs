using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public sealed class EvaluacionProveedorService : IEvaluacionProveedorService
{
    private readonly IEvaluacionProveedorRepository _evaluaciones;
    private readonly IRecepcionCompraRepository _recepciones;
    private readonly IOrdenCompraRepository _ordenes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditoriaService _auditoria;

    public EvaluacionProveedorService(
        IEvaluacionProveedorRepository evaluaciones,
        IRecepcionCompraRepository recepciones,
        IOrdenCompraRepository ordenes,
        IUnitOfWork unitOfWork,
        IAuditoriaService auditoria)
    {
        _evaluaciones = evaluaciones ?? throw new ArgumentNullException(nameof(evaluaciones));
        _recepciones = recepciones ?? throw new ArgumentNullException(nameof(recepciones));
        _ordenes = ordenes ?? throw new ArgumentNullException(nameof(ordenes));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditoria = auditoria ?? throw new ArgumentNullException(nameof(auditoria));
    }

    public async Task<PagedResult<EvaluacionProveedorDto>> GetPagedAsync(EvaluacionProveedorFiltroDto filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        filtro.ValidarYNormalizar();
        var (items, total) = await _evaluaciones.GetPagedAsync(filtro);
        return new PagedResult<EvaluacionProveedorDto>
        {
            Items = items.Select(Map).ToList(),
            TotalCount = total,
            Page = filtro.Page,
            PageSize = filtro.PageSize
        };
    }

    public async Task<EvaluacionProveedorDto?> GetByIdAsync(int id)
    {
        if (id <= 0)
            throw new BusinessRuleException("El identificador de la evaluación debe ser válido.");
        var entity = await _evaluaciones.GetByIdAsync(id);
        return entity is null ? null : Map(entity);
    }

    public async Task<EvaluacionProveedorDto> GenerarPorRecepcionAsync(int recepcionCompraId)
    {
        if (recepcionCompraId <= 0)
            throw new BusinessRuleException("El identificador de la recepción debe ser válido.");

        EvaluacionProveedor? resultado = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var recepcion = await _recepciones.GetByIdForUpdateAsync(recepcionCompraId)
                ?? throw new ResourceNotFoundException("Recepción de compra no encontrada.");
            if (recepcion.Estado != EstadoRecepcionCompra.Recibida || !recepcion.FechaRecepcionUtc.HasValue)
                throw new BusinessRuleException("Solo una recepción materializada puede generar una evaluación de proveedor.");

            var orden = await _ordenes.GetByIdAsync(recepcion.OrdenCompraId, tracking: false)
                ?? throw new ResourceNotFoundException("Orden de compra no encontrada.");
            if (!orden.FechaEsperadaUtc.HasValue)
                throw new BusinessRuleException("La orden de compra requiere FechaEsperadaUtc para evaluar desviación de entrega.");
            if (orden.ProveedorId <= 0)
                throw new BusinessRuleException("La orden de compra no tiene un proveedor válido.");

            var evaluacion = await _evaluaciones.GetByRecepcionCompraIdAsync(recepcion.Id, tracking: true);
            var esNueva = evaluacion is null;
            evaluacion ??= new EvaluacionProveedor(
                orden.ProveedorId,
                orden.Id,
                recepcion.Id,
                orden.FechaEsperadaUtc.Value,
                recepcion.FechaRecepcionUtc.Value);

            if (!esNueva)
                evaluacion.ConfigurarDesviacionEntrega(orden.FechaEsperadaUtc.Value, recepcion.FechaRecepcionUtc.Value);

            evaluacion.EstablecerCantidades(
                orden.Detalles.Sum(x => x.CantidadOrdenada),
                recepcion.CantidadRecibidaTotal,
                recepcion.CantidadAceptadaTotal,
                recepcion.CantidadDanadaTotal,
                recepcion.CantidadSobranteTotal);

            if (esNueva)
                await _evaluaciones.AddAsync(evaluacion);

            await _evaluaciones.SaveChangesAsync();
            await _auditoria.RegistrarEstrictoAsync(
                ModuloSistema.Compras,
                esNueva ? AccionPermiso.Crear : AccionPermiso.Editar,
                esNueva ? "Generación de evaluación de proveedor desde recepción materializada." : "Actualización de evaluación de proveedor desde recepción materializada.",
                referenciaId: evaluacion.Id,
                entidad: nameof(EvaluacionProveedor),
                valoresNuevos: Map(evaluacion),
                motivo: "N2.9.D Evaluación de proveedores");

            resultado = evaluacion;
        });

        return Map(resultado ?? throw new InvalidOperationException("La evaluación de proveedor no produjo un resultado."));
    }

    private static EvaluacionProveedorDto Map(EvaluacionProveedor entity) => new()
    {
        Id = entity.Id,
        ProveedorId = entity.ProveedorId,
        OrdenCompraId = entity.OrdenCompraId,
        RecepcionCompraId = entity.RecepcionCompraId,
        FechaEsperadaUtc = entity.FechaEsperadaUtc,
        FechaRecepcionUtc = entity.FechaRecepcionUtc,
        CantidadOrdenada = entity.CantidadOrdenada,
        CantidadAceptada = entity.CantidadAceptada,
        CantidadDanada = entity.CantidadDanada,
        CantidadSobrante = entity.CantidadSobrante
    };
}
