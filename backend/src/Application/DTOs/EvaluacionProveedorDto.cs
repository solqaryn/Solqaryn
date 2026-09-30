using Solqaryn.Application.Common;
using Solqaryn.Application.Exceptions;

namespace Solqaryn.Application.DTOs;

public sealed class EvaluacionProveedorFiltroDto : PagedRequest
{
    public int? ProveedorId { get; set; }
    public int? OrdenCompraId { get; set; }
    public int? RecepcionCompraId { get; set; }
    public DateTime? DesdeUtc { get; set; }
    public DateTime? HastaUtc { get; set; }

    public void ValidarYNormalizar()
    {
        if (ProveedorId is <= 0 || OrdenCompraId is <= 0 || RecepcionCompraId is <= 0)
            throw new BusinessRuleException("Los filtros de identificadores deben ser válidos.");
        if (DesdeUtc.HasValue && DesdeUtc.Value.Kind != DateTimeKind.Utc)
            throw new BusinessRuleException("DesdeUtc debe expresarse en UTC.");
        if (HastaUtc.HasValue && HastaUtc.Value.Kind != DateTimeKind.Utc)
            throw new BusinessRuleException("HastaUtc debe expresarse en UTC.");
        if (DesdeUtc.HasValue && HastaUtc.HasValue && DesdeUtc.Value > HastaUtc.Value)
            throw new BusinessRuleException("El rango de fechas de recepción es inválido.");

        Page = Math.Max(1, Page);
        PageSize = Math.Clamp(PageSize, 1, 100);
    }
}

public sealed class EvaluacionProveedorDto
{
    public int Id { get; set; }
    public int ProveedorId { get; set; }
    public int OrdenCompraId { get; set; }
    public int RecepcionCompraId { get; set; }
    public DateTime FechaEsperadaUtc { get; set; }
    public DateTime FechaRecepcionUtc { get; set; }
    public decimal CantidadOrdenada { get; set; }
    public decimal CantidadAceptada { get; set; }
    public decimal CantidadDanada { get; set; }
    public decimal CantidadSobrante { get; set; }
}
