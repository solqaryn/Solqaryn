using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities.Contabilidad;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Services;

public sealed class ContabilizacionService : IContabilizacionService
{
    private readonly AppDbContext _db;
    private readonly IAsientoContableWriter _writer;

    public ContabilizacionService(AppDbContext db, IAsientoContableWriter writer)
    {
        _db = db;
        _writer = writer;
    }

    public async Task<AsientoContableWriteResult> ContabilizarAsync(
        EventoContableDto evento,
        CancellationToken cancellationToken = default)
    {
        evento.Validar();

        var configuracion = await _db.Set<ConfiguracionContable>()
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Evento == evento.Tipo, cancellationToken);

        if (configuracion is null)
            throw new ResourceNotFoundException($"No existe configuración contable para el evento {evento.Tipo}.");

        if (!configuracion.Activo)
            throw new BusinessRuleException($"La configuración contable para el evento {evento.Tipo} está inactiva.");

        var referencia = evento.Referencia.Trim();
        var dto = new CrearAsientoContableDto
        {
            Fecha = evento.Fecha,
            Concepto = $"{evento.Tipo}: {referencia}",
            Numero = $"EVT-{(int)evento.Tipo}-{evento.DocumentoOrigenId}",
            DocumentoOrigenId = evento.DocumentoOrigenId,
            TipoDocumentoOrigen = evento.Tipo.ToString(),
            Detalles =
            {
                new CrearAsientoDetalleDto
                {
                    CuentaContableId = configuracion.CuentaDebeId,
                    Debe = evento.Monto,
                    Haber = 0m,
                    Referencia = referencia
                },
                new CrearAsientoDetalleDto
                {
                    CuentaContableId = configuracion.CuentaHaberId,
                    Debe = 0m,
                    Haber = evento.Monto,
                    Referencia = referencia
                }
            }
        };

        return await _writer.CreateAsync(dto, cancellationToken);
    }
}
