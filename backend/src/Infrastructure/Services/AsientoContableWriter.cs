using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Services;

public sealed class AsientoContableWriter : IAsientoContableWriter
{
    private readonly AppDbContext _db;
    private readonly IAuditoriaService _auditoria;

    public AsientoContableWriter(AppDbContext db, IAuditoriaService auditoria)
    {
        _db = db;
        _auditoria = auditoria;
    }

    public async Task<AsientoContableWriteResult> CreateAsync(
        CrearAsientoContableDto dto,
        CancellationToken cancellationToken = default)
    {
        var entity = AsientoContableApplicationService.CrearAggregate(dto);

        if (!string.IsNullOrWhiteSpace(entity.Numero))
        {
            var replay = await _db.AsientosContables
                .AsNoTracking()
                .Include(a => a.Detalles)
                    .ThenInclude(d => d.CuentaContable)
                .SingleOrDefaultAsync(a => a.Numero == entity.Numero, cancellationToken);

            if (replay is not null)
            {
                return new AsientoContableWriteResult(
                    AsientoContableApplicationService.Mapear(replay),
                    Created: false,
                    replay.Id);
            }
        }

        var cuentaIds = entity.Detalles.Select(d => d.CuentaContableId).Distinct().ToList();
        var cuentas = await _db.Set<CuentaContable>()
            .Where(c => cuentaIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        foreach (var cuentaId in cuentaIds)
        {
            if (!cuentas.TryGetValue(cuentaId, out var cuenta))
                throw new ResourceNotFoundException($"La cuenta contable {cuentaId} no existe.");
            if (!cuenta.Activa)
                throw new BusinessRuleException($"La cuenta contable {cuenta.Codigo} está inactiva.");
            if (!cuenta.AceptaMovimientos)
                throw new BusinessRuleException($"La cuenta contable {cuenta.Codigo} no acepta movimientos directos.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        _db.AsientosContables.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        await _auditoria.RegistrarEstrictoAsync(
            ModuloSistema.Finanzas,
            AccionPermiso.Crear,
            $"Registró el asiento contable '{entity.Numero ?? entity.Id.ToString()}'.",
            entity.Id,
            entidad: "AsientoContable",
            valoresNuevos: new
            {
                entity.Fecha,
                entity.Concepto,
                entity.Numero,
                entity.DocumentoOrigenId,
                entity.TipoDocumentoOrigen,
                TotalDebe = entity.Detalles.Sum(d => d.Debe),
                TotalHaber = entity.Detalles.Sum(d => d.Haber),
                Detalles = entity.Detalles.Count
            });
        await transaction.CommitAsync(cancellationToken);

        return new AsientoContableWriteResult(
            AsientoContableApplicationService.Mapear(entity),
            Created: true,
            entity.Id);
    }
}
