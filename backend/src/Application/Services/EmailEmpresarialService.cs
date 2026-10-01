using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

/// <summary>
/// Caso de uso N7.7.D/N7.7.F para registrar y consultar correo empresarial durable.
/// Toda autoridad tenant se vuelve a comprobar server-side, la idempotencia
/// queda acotada por EmpresaId + Idempotency-Key y los registros de creación
/// se auditan sin persistir destinatarios, cuerpo ni claves de idempotencia.
/// </summary>
public sealed class EmailEmpresarialService : IEmailEmpresarialService
{
    private readonly IEmailEmpresarialRepository _repository;
    private readonly IUsuarioScopeService _usuarioScopeService;
    private readonly IAuditoriaService _auditoriaService;

    public EmailEmpresarialService(
        IEmailEmpresarialRepository repository,
        IUsuarioScopeService usuarioScopeService,
        IAuditoriaService auditoriaService)
    {
        _repository = repository;
        _usuarioScopeService = usuarioScopeService;
        _auditoriaService = auditoriaService;
    }

    public async Task<EmailEmpresarialResultado> RegistrarAsync(
        int empresaId,
        RegistrarEmailEmpresarialRequest request,
        string claveIdempotencia,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ExigirEmpresa(empresaId);
        if (string.IsNullOrWhiteSpace(claveIdempotencia))
            throw new ArgumentException("Idempotency-Key es obligatoria.", nameof(claveIdempotencia));

        await ExigirScopeAsync(empresaId, cancellationToken);
        var clave = claveIdempotencia.Trim();
        var candidato = CrearCandidato(empresaId, request, clave);

        var existente = await _repository.GetByClaveIdempotenciaAsync(
            empresaId,
            clave,
            cancellationToken);

        if (existente is not null)
        {
            if (EsMismaSolicitud(existente, candidato))
                return Mapear(existente);

            throw new ConflictException(
                "La clave de idempotencia ya representa otra solicitud de correo para esta empresa.");
        }

        await _repository.AddAsync(candidato, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        await _auditoriaService.RegistrarAsync(
            ModuloSistema.Configuracion,
            AccionPermiso.Crear,
            "Correo empresarial registrado en cola durable.",
            entidad: "EmailEmpresarial",
            valoresNuevos: new
            {
                candidato.MensajeId,
                candidato.EmpresaId,
                candidato.Estado,
                candidato.PlantillaCodigo,
                candidato.PlantillaVersion,
                candidato.CorrelationId
            });

        return Mapear(candidato);
    }

    public async Task<EmailEmpresarialResultado?> ObtenerAsync(
        int empresaId,
        Guid mensajeId,
        CancellationToken cancellationToken = default)
    {
        ExigirEmpresa(empresaId);
        if (mensajeId == Guid.Empty)
            throw new ArgumentOutOfRangeException(nameof(mensajeId));

        await ExigirScopeAsync(empresaId, cancellationToken);
        var email = await _repository.GetByMensajeIdAsync(empresaId, mensajeId, cancellationToken);
        return email is null ? null : Mapear(email);
    }

    public async Task<PaginaEmailEmpresarial> ListarAsync(
        int empresaId,
        EstadoEntregaEmail? estado,
        string? correlationId,
        int pagina,
        int tamano,
        CancellationToken cancellationToken = default)
    {
        ExigirEmpresa(empresaId);
        if (pagina <= 0)
            throw new ArgumentOutOfRangeException(nameof(pagina));
        if (tamano is <= 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(tamano));
        if (estado is not null && !Enum.IsDefined(estado.Value))
            throw new ArgumentOutOfRangeException(nameof(estado));

        await ExigirScopeAsync(empresaId, cancellationToken);
        var correlation = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        var items = await _repository.ListarAsync(
            empresaId,
            estado,
            correlation,
            pagina,
            tamano,
            cancellationToken);
        var total = await _repository.ContarAsync(
            empresaId,
            estado,
            correlation,
            cancellationToken);

        return new PaginaEmailEmpresarial(
            items.Select(Mapear).ToArray(),
            pagina,
            tamano,
            total);
    }

    private async Task ExigirScopeAsync(int empresaId, CancellationToken cancellationToken)
    {
        var scope = await _usuarioScopeService.ObtenerActualAsync(empresaId, cancellationToken);
        if (scope is null || scope.EmpresaId != empresaId)
            throw new ForbiddenAccessException("No existe una membresía activa para la empresa solicitada.");
    }

    private static EmailEmpresarial CrearCandidato(
        int empresaId,
        RegistrarEmailEmpresarialRequest request,
        string claveIdempotencia)
    {
        var usaPlantilla = !string.IsNullOrWhiteSpace(request.PlantillaCodigo);
        if (usaPlantilla)
        {
            if (!string.IsNullOrWhiteSpace(request.Asunto) ||
                !string.IsNullOrWhiteSpace(request.CuerpoHtml) ||
                !string.IsNullOrWhiteSpace(request.CuerpoTexto))
            {
                throw new ArgumentException(
                    "Una solicitud por plantilla no puede incluir contenido directo.",
                    nameof(request));
            }

            if (request.PlantillaVersion is null or <= 0 || string.IsNullOrWhiteSpace(request.VariablesJson))
                throw new ArgumentException(
                    "PlantillaVersion y VariablesJson son obligatorios al usar plantilla.",
                    nameof(request));

            return EmailEmpresarial.CrearDesdePlantilla(
                empresaId,
                request.Destinatario,
                request.PlantillaCodigo!,
                request.PlantillaVersion.Value,
                request.VariablesJson!,
                claveIdempotencia,
                request.CorrelationId);
        }

        if (request.PlantillaVersion is not null || !string.IsNullOrWhiteSpace(request.VariablesJson))
            throw new ArgumentException(
                "PlantillaVersion/VariablesJson requieren PlantillaCodigo.",
                nameof(request));

        return EmailEmpresarial.CrearDirecto(
            empresaId,
            request.Destinatario,
            request.Asunto ?? string.Empty,
            request.CuerpoHtml ?? string.Empty,
            claveIdempotencia,
            request.CuerpoTexto,
            request.CorrelationId);
    }

    private static bool EsMismaSolicitud(EmailEmpresarial existente, EmailEmpresarial candidato)
    {
        return existente.EmpresaId == candidato.EmpresaId &&
               string.Equals(existente.Destinatario, candidato.Destinatario, StringComparison.Ordinal) &&
               string.Equals(existente.Asunto, candidato.Asunto, StringComparison.Ordinal) &&
               string.Equals(existente.CuerpoHtml, candidato.CuerpoHtml, StringComparison.Ordinal) &&
               string.Equals(existente.CuerpoTexto, candidato.CuerpoTexto, StringComparison.Ordinal) &&
               string.Equals(existente.PlantillaCodigo, candidato.PlantillaCodigo, StringComparison.Ordinal) &&
               existente.PlantillaVersion == candidato.PlantillaVersion &&
               string.Equals(existente.VariablesJson, candidato.VariablesJson, StringComparison.Ordinal) &&
               string.Equals(existente.ClaveIdempotencia, candidato.ClaveIdempotencia, StringComparison.Ordinal) &&
               string.Equals(existente.CorrelationId, candidato.CorrelationId, StringComparison.Ordinal);
    }

    private static EmailEmpresarialResultado Mapear(EmailEmpresarial email) => new(
        email.MensajeId,
        email.EmpresaId,
        email.Destinatario,
        email.Asunto,
        email.PlantillaCodigo,
        email.PlantillaVersion,
        email.Estado,
        email.Intentos,
        email.CorrelationId,
        email.ProviderMessageId,
        email.CreadoEnUtc,
        email.DisponibleDesdeUtc,
        email.EntregadoEnUtc,
        email.RebotadoEnUtc);

    private static void ExigirEmpresa(int empresaId)
    {
        if (empresaId <= 0)
            throw new ArgumentOutOfRangeException(nameof(empresaId));
    }
}
