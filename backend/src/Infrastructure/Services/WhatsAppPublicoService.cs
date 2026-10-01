using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Solqaryn.Infrastructure.Services;

public sealed class WhatsAppPublicoService : IWhatsAppPublicoService
{
    private readonly AppDbContext _db;
    private readonly ILogger<WhatsAppPublicoService> _logger;

    public WhatsAppPublicoService(AppDbContext db, ILogger<WhatsAppPublicoService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<WhatsAppPublicoDto> ObtenerAsync(CancellationToken cancellationToken = default)
    {
        var configuraciones = await _db.Set<ConfiguracionWhatsAppEmpresa>()
            .AsNoTracking()
            .Where(x => x.Activa)
            .Select(x => new
            {
                x.NumeroTelefonoE164,
                EmpresaNombre = x.Empresa.Nombre
            })
            .ToListAsync(cancellationToken);

        var numeros = configuraciones
            .Select(x => x.NumeroTelefonoE164)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (numeros.Count == 1)
            return new WhatsAppPublicoDto(numeros[0], true);

        if (numeros.Count <= 1)
            return new WhatsAppPublicoDto(null, false);

        var identidadPublica = await _db.Set<EmpresaConfiguracion>()
            .AsNoTracking()
            .Where(x => x.Activa)
            .Select(x => new { x.NombreComercial, x.NombreVisibleSistema })
            .SingleOrDefaultAsync(cancellationToken);

        if (identidadPublica is not null)
        {
            var nombresPublicos = new[] { identidadPublica.NombreComercial, identidadPublica.NombreVisibleSistema }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var coincidencias = configuraciones
                .Where(x => nombresPublicos.Any(nombre =>
                    string.Equals(nombre, x.EmpresaNombre?.Trim(), StringComparison.OrdinalIgnoreCase)))
                .Select(x => x.NumeroTelefonoE164)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (coincidencias.Count == 1)
                return new WhatsAppPublicoDto(coincidencias[0], true);
        }

        _logger.LogWarning(
            "WhatsApp público no expuesto: existen varias configuraciones activas sin coincidencia inequívoca con la identidad pública.");

        return new WhatsAppPublicoDto(null, false);
    }
}
