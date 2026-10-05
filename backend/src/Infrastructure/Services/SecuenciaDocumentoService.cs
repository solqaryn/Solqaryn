using System.Data;
using System.Text.Json;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Services;

/// <summary>
/// N6.6.D/F — consulta, reserva monotónica y auditoría de secuencias documentales por tenant.
/// La reserva usa compare-and-swap dentro de una transacción corta: nunca MAX()+1,
/// memoria local ni un contador compartido entre Empresas.
/// </summary>
public sealed class SecuenciaDocumentoService : ISecuenciaDocumentoService
{
    private const int MaxIntentosConcurrencia = 32;

    private readonly AppDbContext _db;
    private readonly IUsuarioScopeService _usuarioScope;

    public SecuenciaDocumentoService(AppDbContext db, IUsuarioScopeService usuarioScope)
    {
        _db = db;
        _usuarioScope = usuarioScope;
    }

    public async Task<SecuenciaDocumentoConsultaDto> ObtenerAsync(
        int empresaId,
        int? sucursalId,
        string tipoDocumento,
        CancellationToken cancellationToken = default)
    {
        var tipo = NormalizarTipoDocumento(tipoDocumento);
        await ValidarScopeAsync(empresaId, sucursalId, cancellationToken);

        var secuencia = await BuscarAsync(empresaId, sucursalId, tipo, cancellationToken);
        if (secuencia is null)
            throw new BusinessRuleException("No existe una secuencia configurada para el scope solicitado.");

        return ToDto(secuencia);
    }

    public async Task<SecuenciaDocumentoSiguienteDto> ReservarSiguienteAsync(
        ReservarSecuenciaDocumentoRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tipo = NormalizarTipoDocumento(request.TipoDocumento);
        var tenant = await ValidarScopeAsync(request.EmpresaId, request.SucursalId, cancellationToken);

        for (var intento = 1; intento <= MaxIntentosConcurrencia; intento++)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

            var secuencia = await BuscarAsync(
                request.EmpresaId,
                request.SucursalId,
                tipo,
                cancellationToken);

            if (secuencia is null)
                throw new BusinessRuleException("No existe una secuencia configurada para el scope solicitado.");
            if (!secuencia.Activa)
                throw new BusinessRuleException("La secuencia solicitada está inactiva.");
            if (secuencia.UltimoValor == long.MaxValue)
                throw new BusinessRuleException("La secuencia alcanzó el máximo valor soportado.");

            var anterior = secuencia.UltimoValor;
            var siguiente = anterior + 1;
            var ahora = DateTime.UtcNow;

            var filas = await _db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE `SecuenciasDocumento`
                   SET `UltimoValor` = {siguiente},
                       `FechaActualizacion` = {ahora},
                       `ActualizadoPorUsuarioId` = {tenant.UsuarioId}
                 WHERE `Id` = {secuencia.Id}
                   AND `EmpresaId` = {request.EmpresaId}
                   AND `UltimoValor` = {anterior}
                   AND `Activa` = TRUE
                """, cancellationToken);

            if (filas == 1)
            {
                _db.RegistrosAuditoria.Add(new RegistroAuditoria
                {
                    Fecha = ahora,
                    UsuarioId = tenant.UsuarioId,
                    NombreUsuario = $"Usuario:{tenant.UsuarioId}",
                    Modulo = ModuloSistema.Configuracion,
                    Accion = AccionPermiso.Editar,
                    Entidad = nameof(SecuenciaDocumento),
                    ReferenciaId = secuencia.Id,
                    Descripcion = $"Reserva de numeración {tipo} para Empresa {request.EmpresaId} / PuntoEmision {request.SucursalId?.ToString() ?? "GLOBAL"}: {anterior} -> {siguiente}.",
                    ValoresAnteriores = JsonSerializer.Serialize(new
                    {
                        usuarioId = tenant.UsuarioId,
                        fecha = ahora,
                        empresaId = request.EmpresaId,
                        puntoEmisionId = request.SucursalId,
                        tipoDocumento = tipo,
                        valorAnterior = anterior,
                        origen = nameof(SecuenciaDocumentoService),
                        canal = "API"
                    }),
                    ValoresNuevos = JsonSerializer.Serialize(new
                    {
                        usuarioId = tenant.UsuarioId,
                        fecha = ahora,
                        empresaId = request.EmpresaId,
                        puntoEmisionId = request.SucursalId,
                        tipoDocumento = tipo,
                        valorAnterior = anterior,
                        valorSiguiente = siguiente,
                        origen = nameof(SecuenciaDocumentoService),
                        canal = "API"
                    }),
                    Resultado = "Exito"
                });

                // La auditoría forma parte de la misma transacción que el CAS: si la
                // evidencia no se puede persistir, la numeración tampoco se consume.
                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return new SecuenciaDocumentoSiguienteDto(
                    secuencia.EmpresaId,
                    secuencia.SucursalId,
                    secuencia.TipoDocumento,
                    siguiente,
                    secuencia.Formatear(siguiente));
            }

            await transaction.RollbackAsync(cancellationToken);
            _db.ChangeTracker.Clear();

            // Desincroniza los compare-and-swap contendientes para que una ráfaga
            // concurrente no consuma todos los intentos antes de que avance la fila.
            var esperaMaximaMs = Math.Min(intento * 5, 100);
            await Task.Delay(
                TimeSpan.FromMilliseconds(Random.Shared.Next(1, esperaMaximaMs + 1)),
                cancellationToken);
        }

        throw new BusinessRuleException(
            "No fue posible reservar el siguiente número por concurrencia. Intente nuevamente.");
    }

    private async Task<UsuarioTenantScopeActual> ValidarScopeAsync(
        int empresaId,
        int? sucursalId,
        CancellationToken cancellationToken)
    {
        if (empresaId <= 0)
            throw new BusinessRuleException("La empresa es obligatoria.");
        if (sucursalId.HasValue && sucursalId.Value <= 0)
            throw new BusinessRuleException("La sucursal debe ser positiva cuando se informa.");

        var tenant = await _usuarioScope.ObtenerActualAsync(empresaId, cancellationToken);
        if (tenant is null)
            throw new BusinessRuleException("El usuario no tiene acceso activo a la empresa solicitada.");

        if (sucursalId.HasValue)
        {
            var sucursalValida = await _db.Set<Sucursal>()
                .AsNoTracking()
                .AnyAsync(
                    x => x.Id == sucursalId.Value &&
                         x.EmpresaId == empresaId &&
                         x.Activa &&
                         !x.Eliminado,
                    cancellationToken);

            if (!sucursalValida)
                throw new BusinessRuleException("La sucursal no pertenece a la empresa activa solicitada.");
        }

        return tenant;
    }

    private Task<SecuenciaDocumento?> BuscarAsync(
        int empresaId,
        int? sucursalId,
        string tipoDocumento,
        CancellationToken cancellationToken) =>
        _db.SecuenciasDocumento
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.EmpresaId == empresaId &&
                     x.SucursalId == sucursalId &&
                     x.TipoDocumento == tipoDocumento,
                cancellationToken);

    private static SecuenciaDocumentoConsultaDto ToDto(SecuenciaDocumento secuencia) => new(
        secuencia.EmpresaId,
        secuencia.SucursalId,
        secuencia.TipoDocumento,
        secuencia.UltimoValor,
        secuencia.Prefijo,
        secuencia.LongitudNumero,
        secuencia.Activa,
        secuencia.UltimoValor > 0 ? secuencia.Formatear(secuencia.UltimoValor) : null);

    private static string NormalizarTipoDocumento(string? tipoDocumento)
    {
        var normalizado = tipoDocumento?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalizado.Length == 0)
            throw new BusinessRuleException("El tipo documental es obligatorio.");
        if (normalizado.Length > 80)
            throw new BusinessRuleException("El tipo documental no puede exceder 80 caracteres.");
        return normalizado;
    }
}
