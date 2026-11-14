namespace Solqaryn.Application.DTOs;

public class RegistroAuditoriaDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public int? UsuarioId { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string Modulo { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public string? Entidad { get; set; }
    public int? ReferenciaId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string? ValoresAnteriores { get; set; }
    public string? ValoresNuevos { get; set; }
    public string? Motivo { get; set; }
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
    public string? CorrelationId { get; set; }
    public string Resultado { get; set; } = "Exito";
    public string? Error { get; set; }
}

public class AuditoriaFiltroDto
{
    public int? UsuarioId { get; set; }
    public string? Modulo { get; set; }
    public string? Accion { get; set; }
    public string? Entidad { get; set; }
    public int? ReferenciaId { get; set; }
    public string? Resultado { get; set; }
    /// Búsqueda de texto libre sobre Descripción, NombreUsuario y Motivo.
    public string? Texto { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
