using System.Data;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

/// <summary>
/// Persistencia N6.9.D para suscripciones SaaS. El ledger de idempotencia es una
/// tabla auxiliar deliberadamente fuera del modelo agregado: se escribe dentro de
/// la misma transacción que Suscripcion para garantizar replay durable por tenant.
/// </summary>
public sealed class SuscripcionesSaaSRepository : ISuscripcionesSaaSRepository
{
    private const int IdempotencyKeyMaxLength = 160;
    private readonly AppDbContext _db;
    private readonly List<(Suscripcion Suscripcion, string IdempotencyKey)> _idempotenciasPendientes = new();

    public SuscripcionesSaaSRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Plan?> ObtenerPlanActivoPorCodigoAsync(
        string codigo,
        CancellationToken cancellationToken = default)
    {
        var normalizado = NormalizarCodigoPlan(codigo);
        return _db.Set<Plan>()
            .AsNoTracking()
            .Include(x => x.Limites)
            .SingleOrDefaultAsync(x => x.Activo && x.Codigo == normalizado, cancellationToken);
    }

    public Task<Plan?> ObtenerPlanPorIdAsync(
        int planId,
        CancellationToken cancellationToken = default)
    {
        if (planId <= 0)
            return Task.FromResult<Plan?>(null);

        return _db.Set<Plan>()
            .AsNoTracking()
            .Include(x => x.Limites)
            .SingleOrDefaultAsync(x => x.Id == planId, cancellationToken);
    }

    public async Task<IReadOnlyList<PlanModulo>> ObtenerModulosPlanAsync(
        int planId,
        string planCodigo,
        CancellationToken cancellationToken = default)
    {
        if (planId <= 0 || string.IsNullOrWhiteSpace(planCodigo))
            return Array.Empty<PlanModulo>();

        var codigoNormalizado = planCodigo.Trim().ToUpperInvariant();
        return await _db.Set<PlanModulo>()
            .AsNoTracking()
            .Where(x => x.PlanId == planId && x.PlanCodigo == codigoNormalizado)
            .OrderBy(x => x.ModuloClave)
            .ToListAsync(cancellationToken);
    }

    public Task<Suscripcion?> ObtenerVigenteAsync(
        int empresaId,
        DateTime instanteUtc,
        CancellationToken cancellationToken = default)
    {
        if (empresaId <= 0)
            return Task.FromResult<Suscripcion?>(null);

        var instante = NormalizarUtc(instanteUtc);
        return _db.Set<Suscripcion>()
            .AsNoTracking()
            .Where(x =>
                x.EmpresaId == empresaId &&
                x.Estado == EstadoSuscripcion.Activa &&
                x.InicioUtc <= instante &&
                (!x.FinUtc.HasValue || x.FinUtc.Value > instante))
            .OrderByDescending(x => x.InicioUtc)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Suscripcion?> ObtenerPorIdempotenciaAsync(
        int empresaId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (empresaId <= 0)
            return null;

        var key = NormalizarIdempotencyKey(idempotencyKey);
        var connection = _db.Database.GetDbConnection();
        var debeCerrar = connection.State != ConnectionState.Open;

        if (debeCerrar)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT `SuscripcionId` FROM `SuscripcionSaaSIdempotencia` WHERE `EmpresaId` = @empresaId AND `IdempotencyKey` = @key LIMIT 1;";

            var empresaParameter = command.CreateParameter();
            empresaParameter.ParameterName = "@empresaId";
            empresaParameter.Value = empresaId;
            command.Parameters.Add(empresaParameter);

            var keyParameter = command.CreateParameter();
            keyParameter.ParameterName = "@key";
            keyParameter.Value = key;
            command.Parameters.Add(keyParameter);

            var scalar = await command.ExecuteScalarAsync(cancellationToken);
            if (scalar is null || scalar is DBNull)
                return null;

            var suscripcionId = Convert.ToInt32(scalar);
            return await _db.Set<Suscripcion>()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.Id == suscripcionId && x.EmpresaId == empresaId,
                    cancellationToken);
        }
        finally
        {
            if (debeCerrar)
                await connection.CloseAsync();
        }
    }

    public Task AgregarAsync(
        Suscripcion suscripcion,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(suscripcion);
        cancellationToken.ThrowIfCancellationRequested();

        if (suscripcion.EmpresaId <= 0)
            throw new ArgumentOutOfRangeException(nameof(suscripcion), "La suscripción debe pertenecer a un tenant válido.");

        var key = NormalizarIdempotencyKey(idempotencyKey);
        _db.Set<Suscripcion>().Add(suscripcion);
        _idempotenciasPendientes.Add((suscripcion, key));
        return Task.CompletedTask;
    }

    public async Task GuardarCambiosAsync(CancellationToken cancellationToken = default)
    {
        if (_idempotenciasPendientes.Count == 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);

            var creadoUtc = DateTime.UtcNow;
            foreach (var pendiente in _idempotenciasPendientes)
            {
                try
                {
                    await _db.Database.ExecuteSqlInterpolatedAsync(
                        $"INSERT INTO `SuscripcionSaaSIdempotencia` (`EmpresaId`, `IdempotencyKey`, `SuscripcionId`, `CreadoUtc`) VALUES ({pendiente.Suscripcion.EmpresaId}, {pendiente.IdempotencyKey}, {pendiente.Suscripcion.Id}, {creadoUtc});",
                        cancellationToken);
                }
                catch (Exception ex) when (
                    MySqlProviderErrorClassifier.TryFind(ex, out var error) &&
                    error.Number == 1062)
                {
                    throw new IdempotencyConcurrencyException(
                        "Otra solicitud ganó concurrentemente la clave durable de idempotencia del tenant.",
                        ex);
                }
            }

            await transaction.CommitAsync(cancellationToken);
            _idempotenciasPendientes.Clear();
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            _idempotenciasPendientes.Clear();

            // SaveChanges puede haber marcado la Suscripcion como persistida antes de que
            // el INSERT del ledger colisionara. El rollback revierte la base, pero no el
            // ChangeTracker; limpiarlo evita que un guardado posterior resurrecte el loser.
            _db.ChangeTracker.Clear();
            throw;
        }
    }

    private static string NormalizarCodigoPlan(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            throw new ArgumentException("El código del plan es obligatorio.", nameof(codigo));

        return codigo.Trim().ToUpperInvariant();
    }

    private static string NormalizarIdempotencyKey(string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("Idempotency-Key es obligatorio.", nameof(idempotencyKey));

        var key = idempotencyKey.Trim();
        if (key.Length > IdempotencyKeyMaxLength)
            throw new ArgumentException($"Idempotency-Key no puede superar {IdempotencyKeyMaxLength} caracteres.", nameof(idempotencyKey));

        return key;
    }

    private static DateTime NormalizarUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
}
