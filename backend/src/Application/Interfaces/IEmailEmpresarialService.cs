using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public sealed record RegistrarEmailEmpresarialRequest(
    string Destinatario,
    string? Asunto = null,
    string? CuerpoHtml = null,
    string? CuerpoTexto = null,
    string? PlantillaCodigo = null,
    int? PlantillaVersion = null,
    string? VariablesJson = null,
    string? CorrelationId = null);

public sealed record EmailEmpresarialResultado(
    Guid MensajeId,
    int EmpresaId,
    string Destinatario,
    string? Asunto,
    string? PlantillaCodigo,
    int? PlantillaVersion,
    EstadoEntregaEmail Estado,
    int Intentos,
    string? CorrelationId,
    string? ProviderMessageId,
    DateTime CreadoEnUtc,
    DateTime DisponibleDesdeUtc,
    DateTime? EntregadoEnUtc,
    DateTime? RebotadoEnUtc);

public sealed record PaginaEmailEmpresarial(
    IReadOnlyList<EmailEmpresarialResultado> Items,
    int Pagina,
    int Tamano,
    int Total);

public interface IEmailEmpresarialRepository
{
    Task<EmailEmpresarial?> GetByClaveIdempotenciaAsync(
        int empresaId,
        string claveIdempotencia,
        CancellationToken cancellationToken = default);

    Task<EmailEmpresarial?> GetByMensajeIdAsync(
        int empresaId,
        Guid mensajeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmailEmpresarial>> ListarAsync(
        int empresaId,
        EstadoEntregaEmail? estado,
        string? correlationId,
        int pagina,
        int tamano,
        CancellationToken cancellationToken = default);

    Task<int> ContarAsync(
        int empresaId,
        EstadoEntregaEmail? estado,
        string? correlationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(EmailEmpresarial email, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IEmailEmpresarialService
{
    Task<EmailEmpresarialResultado> RegistrarAsync(
        int empresaId,
        RegistrarEmailEmpresarialRequest request,
        string claveIdempotencia,
        CancellationToken cancellationToken = default);

    Task<EmailEmpresarialResultado?> ObtenerAsync(
        int empresaId,
        Guid mensajeId,
        CancellationToken cancellationToken = default);

    Task<PaginaEmailEmpresarial> ListarAsync(
        int empresaId,
        EstadoEntregaEmail? estado,
        string? correlationId,
        int pagina,
        int tamano,
        CancellationToken cancellationToken = default);
}
