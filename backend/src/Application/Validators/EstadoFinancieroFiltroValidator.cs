using FluentValidation;
using Solqaryn.Application.DTOs.Contabilidad;

namespace Solqaryn.Application.Validators;

public sealed class EstadoFinancieroFiltroValidator : AbstractValidator<EstadoFinancieroFiltroDto>
{
    public EstadoFinancieroFiltroValidator()
    {
        RuleFor(x => x).Custom((filtro, context) =>
        {
            var tienePeriodo = filtro.PeriodoContableId.HasValue;
            var tieneDesde = filtro.FechaDesde.HasValue;
            var tieneHasta = filtro.FechaHasta.HasValue;
            var tieneRangoCompleto = tieneDesde && tieneHasta;

            if (tienePeriodo == tieneRangoCompleto || (!tienePeriodo && (tieneDesde != tieneHasta)))
                context.AddFailure("Debe indicar exactamente un PeriodoContableId o un rango completo FechaDesde/FechaHasta.");

            if (tienePeriodo && filtro.PeriodoContableId <= 0)
                context.AddFailure(nameof(filtro.PeriodoContableId), "PeriodoContableId debe ser mayor a cero.");

            if (tieneRangoCompleto && filtro.FechaDesde > filtro.FechaHasta)
                context.AddFailure(nameof(filtro.FechaHasta), "FechaHasta debe ser igual o posterior a FechaDesde.");
        });
    }
}
