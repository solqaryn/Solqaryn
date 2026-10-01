namespace Solqaryn.Application.DTOs;

public sealed class CreditoClienteDto
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public decimal LimiteCredito { get; set; }
    public int DiasCredito { get; set; }
    public decimal? UmbralAlertaPorcentaje { get; set; }
    public bool BloqueadoAutomaticamente { get; set; }
    public string? MotivoBloqueo { get; set; }
    public DateTime? BloqueadoUtc { get; set; }
    public decimal? MontoExcepcion { get; set; }
    public DateTime? ExcepcionVigenteHastaUtc { get; set; }
    public string? ExcepcionAutorizadaPor { get; set; }
    public DateTime? ExcepcionAutorizadaUtc { get; set; }
}

public sealed class CreateCreditoClienteDto
{
    public int ClienteId { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public decimal LimiteCredito { get; set; }
    public int DiasCredito { get; set; }
    public decimal? UmbralAlertaPorcentaje { get; set; }
}

public sealed class UpdateCreditoClienteDto
{
    public string Moneda { get; set; } = string.Empty;
    public decimal LimiteCredito { get; set; }
    public int DiasCredito { get; set; }
    public decimal? UmbralAlertaPorcentaje { get; set; }
}

public sealed class AplicarBloqueoCreditoClienteDto
{
    public string Motivo { get; set; } = string.Empty;
}

public sealed class AutorizarExcepcionCreditoClienteDto
{
    public decimal Monto { get; set; }
    public DateTime VigenteHastaUtc { get; set; }
}
