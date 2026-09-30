using FluentValidation;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Validators;

public sealed class TransferenciaInventarioDetalleInputValidator : AbstractValidator<TransferenciaInventarioDetalleInputDto>
{
    public TransferenciaInventarioDetalleInputValidator()
    {
        RuleFor(x => x.ProductoVarianteId).GreaterThan(0);
        RuleFor(x => x.UbicacionOrigenId).GreaterThan(0).When(x => x.UbicacionOrigenId.HasValue);
        RuleFor(x => x.UbicacionDestinoId).GreaterThan(0).When(x => x.UbicacionDestinoId.HasValue);
        RuleFor(x => x.CantidadSolicitada).GreaterThan(0);
    }
}

public sealed class CreateTransferenciaInventarioValidator : AbstractValidator<CreateTransferenciaInventarioDto>
{
    public CreateTransferenciaInventarioValidator()
    {
        RuleFor(x => x.AlmacenOrigenId).GreaterThan(0);
        RuleFor(x => x.AlmacenDestinoId)
            .GreaterThan(0)
            .NotEqual(x => x.AlmacenOrigenId)
            .WithMessage("El almacén de destino debe ser distinto del almacén de origen.");
        RuleFor(x => x.Observaciones).MaximumLength(1000);
        RuleFor(x => x.Detalles)
            .NotEmpty().WithMessage("La transferencia debe contener al menos un detalle.");
        RuleForEach(x => x.Detalles).SetValidator(new TransferenciaInventarioDetalleInputValidator());
        RuleFor(x => x.Detalles)
            .Must(SinDuplicados)
            .When(x => x.Detalles is { Count: > 0 })
            .WithMessage("No puede repetirse la misma variante y par de ubicaciones dentro de la transferencia.");
    }

    private static bool SinDuplicados(IReadOnlyCollection<TransferenciaInventarioDetalleInputDto> detalles) =>
        detalles
            .GroupBy(x => new { x.ProductoVarianteId, x.UbicacionOrigenId, x.UbicacionDestinoId })
            .All(g => g.Count() == 1);
}

public sealed class UpdateTransferenciaInventarioValidator : AbstractValidator<UpdateTransferenciaInventarioDto>
{
    public UpdateTransferenciaInventarioValidator()
    {
        RuleFor(x => x.AlmacenOrigenId).GreaterThan(0);
        RuleFor(x => x.AlmacenDestinoId)
            .GreaterThan(0)
            .NotEqual(x => x.AlmacenOrigenId)
            .WithMessage("El almacén de destino debe ser distinto del almacén de origen.");
        RuleFor(x => x.Observaciones).MaximumLength(1000);
        RuleFor(x => x.Detalles).NotEmpty();
        RuleForEach(x => x.Detalles).SetValidator(new TransferenciaInventarioDetalleInputValidator());
        RuleFor(x => x.Detalles)
            .Must(detalles => detalles
                .GroupBy(d => new { d.ProductoVarianteId, d.UbicacionOrigenId, d.UbicacionDestinoId })
                .All(g => g.Count() == 1))
            .When(x => x.Detalles is { Count: > 0 })
            .WithMessage("No puede repetirse la misma variante y par de ubicaciones dentro de la transferencia.");
    }
}

public sealed class AprobarTransferenciaInventarioValidator : AbstractValidator<AprobarTransferenciaInventarioDto>
{
    public AprobarTransferenciaInventarioValidator()
    {
        RuleFor(x => x.Detalles).NotEmpty();
        RuleFor(x => x.Detalles)
            .Must(SinIdsDuplicados)
            .When(x => x.Detalles is { Count: > 0 })
            .WithMessage("La aprobación contiene detalles duplicados.");
        RuleForEach(x => x.Detalles).ChildRules(detalle =>
        {
            detalle.RuleFor(x => x.DetalleId).GreaterThan(0);
            detalle.RuleFor(x => x.CantidadAprobada).GreaterThan(0);
        });
    }

    private static bool SinIdsDuplicados(IReadOnlyCollection<AprobarTransferenciaInventarioDetalleDto> detalles) =>
        detalles.Select(x => x.DetalleId).Distinct().Count() == detalles.Count;
}

public sealed class DespacharTransferenciaInventarioValidator : AbstractValidator<DespacharTransferenciaInventarioDto>
{
    public DespacharTransferenciaInventarioValidator()
    {
        RuleFor(x => x.Detalles).NotEmpty();
        RuleFor(x => x.Detalles)
            .Must(detalles => detalles.Select(x => x.DetalleId).Distinct().Count() == detalles.Count)
            .When(x => x.Detalles is { Count: > 0 })
            .WithMessage("El despacho contiene detalles duplicados.");
        RuleForEach(x => x.Detalles).ChildRules(detalle =>
        {
            detalle.RuleFor(x => x.DetalleId).GreaterThan(0);
            detalle.RuleFor(x => x.CantidadDespachada).GreaterThan(0);
        });
    }
}

public sealed class RecibirTransferenciaInventarioValidator : AbstractValidator<RecibirTransferenciaInventarioDto>
{
    public RecibirTransferenciaInventarioValidator()
    {
        RuleFor(x => x.Detalles).NotEmpty();
        RuleFor(x => x.Detalles)
            .Must(detalles => detalles.Select(x => x.DetalleId).Distinct().Count() == detalles.Count)
            .When(x => x.Detalles is { Count: > 0 })
            .WithMessage("La recepción contiene detalles duplicados.");
        RuleForEach(x => x.Detalles).ChildRules(detalle =>
        {
            detalle.RuleFor(x => x.DetalleId).GreaterThan(0);
            detalle.RuleFor(x => x.CantidadRecibida).GreaterThanOrEqualTo(0);
            detalle.RuleFor(x => x.CantidadFaltante).GreaterThanOrEqualTo(0);
            detalle.RuleFor(x => x.CantidadDanada).GreaterThanOrEqualTo(0);
            detalle.RuleFor(x => x.CantidadSobrante).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class CancelarTransferenciaInventarioValidator : AbstractValidator<CancelarTransferenciaInventarioDto>
{
    public CancelarTransferenciaInventarioValidator()
    {
        RuleFor(x => x.Motivo)
            .NotEmpty().WithMessage("El motivo de cancelación es obligatorio.")
            .MaximumLength(500);
    }
}

public sealed class TransferenciaInventarioFiltroValidator : AbstractValidator<TransferenciaInventarioFiltroDto>
{
    public TransferenciaInventarioFiltroValidator()
    {
        RuleFor(x => x.AlmacenOrigenId).GreaterThan(0).When(x => x.AlmacenOrigenId.HasValue);
        RuleFor(x => x.AlmacenDestinoId).GreaterThan(0).When(x => x.AlmacenDestinoId.HasValue);
        RuleFor(x => x.Numero).MaximumLength(80);
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Hasta)
            .Must((filtro, hasta) => !filtro.Desde.HasValue || !hasta.HasValue || hasta.Value >= filtro.Desde.Value)
            .WithMessage("La fecha Hasta debe ser igual o posterior a Desde.");
    }
}
