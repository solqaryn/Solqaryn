using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Services;

public sealed class AjusteInventarioConsultaService : IAjusteInventarioConsultaService
{
    private readonly IAjusteInventarioRepository _repository;

    public AjusteInventarioConsultaService(IAjusteInventarioRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<AjusteInventarioDto>> GetPagedAsync(AjusteInventarioFiltroDto filtro)
    {
        if (filtro is null)
            throw new BusinessRuleException("Los filtros de consulta son obligatorios.");
        if (filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Desde.Value > filtro.Hasta.Value)
            throw new BusinessRuleException("La fecha inicial no puede ser posterior a la fecha final.");
        if (filtro.ProductoId.HasValue && filtro.ProductoId.Value <= 0)
            throw new BusinessRuleException("El producto indicado en el filtro no es válido.");
        if (filtro.ProductoVarianteId.HasValue && filtro.ProductoVarianteId.Value <= 0)
            throw new BusinessRuleException("La variante indicada en el filtro no es válida.");

        var (items, totalCount) = await _repository.GetPagedAsync(filtro);
        return new PagedResult<AjusteInventarioDto>
        {
            Items = items.Select(ToDto).ToList(),
            Page = filtro.Page,
            PageSize = filtro.PageSize,
            TotalCount = totalCount
        };
    }

    private static AjusteInventarioDto ToDto(AjusteInventario ajuste) => new()
    {
        Id = ajuste.Id,
        NumeroAjuste = ajuste.NumeroAjuste,
        FechaAjuste = ajuste.FechaAjuste,
        Estado = ajuste.Estado.ToString(),
        Motivo = ajuste.Motivo,
        Observaciones = ajuste.Observaciones,
        FechaConfirmacion = ajuste.FechaConfirmacion,
        ConfirmadoPorNombreUsuario = ajuste.ConfirmadoPorNombreUsuario,
        FechaAnulacion = ajuste.FechaAnulacion,
        AnuladoPorNombreUsuario = ajuste.AnuladoPorNombreUsuario,
        MotivoAnulacion = ajuste.MotivoAnulacion,
        ImpactoCostoTotalSnapshot = ajuste.Detalles
            .Where(d => d.ImpactoCostoSnapshot.HasValue)
            .Sum(d => d.ImpactoCostoSnapshot ?? 0m),
        Detalles = ajuste.Detalles
            .OrderBy(d => d.Id)
            .Select(d => new AjusteInventarioDetalleDto
            {
                Id = d.Id,
                ProductoId = d.ProductoId,
                ProductoVarianteId = d.ProductoVarianteId,
                CantidadObjetivo = d.CantidadObjetivo,
                CantidadAnteriorSnapshot = d.CantidadAnteriorSnapshot,
                CantidadNuevaSnapshot = d.CantidadNuevaSnapshot,
                DiferenciaSnapshot = d.DiferenciaSnapshot,
                CostoUnitarioSnapshot = d.CostoUnitarioSnapshot,
                ImpactoCostoSnapshot = d.ImpactoCostoSnapshot,
                NombreSnapshot = d.NombreSnapshot,
                SkuSnapshot = d.SkuSnapshot,
                MarcaSnapshot = d.MarcaSnapshot,
                ModeloSnapshot = d.ModeloSnapshot,
                ColorSnapshot = d.ColorSnapshot,
                TallaSnapshot = d.TallaSnapshot
            })
            .ToList()
    };
}
