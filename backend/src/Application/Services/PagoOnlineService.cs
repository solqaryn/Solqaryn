using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public sealed class PagoOnlineService : IPagoOnlineService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> InicioLocks = new();

    private readonly IPagoOnlineRepository _repository;
    private readonly IReadOnlyList<IPagoOnlineProvider> _providers;
    private readonly ICurrentUserService _currentUser;

    public PagoOnlineService(
        IPagoOnlineRepository repository,
        IEnumerable<IPagoOnlineProvider> providers,
        ICurrentUserService currentUser)
    {
        _repository = repository;
        _providers = providers.ToList();
        _currentUser = currentUser;
    }

    public async Task<InicioPagoOnlineResultadoDto> IniciarAsync(
        IniciarPagoOnlineDto solicitud,
        string claveIdempotencia,
        CancellationToken cancellationToken = default)
    {
        ValidarSolicitud(solicitud, claveIdempotencia);

        var proveedor = NormalizarProveedor(solicitud.Proveedor);
        var moneda = solicitud.Moneda.Trim().ToUpperInvariant();
        var claveHash = CalcularHashIdempotencia(claveIdempotencia);
        var lockKey = $"{solicitud.EmpresaId}:{proveedor}:{claveHash}";
        var gate = InicioLocks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));

        await gate.WaitAsync(cancellationToken);
        try
        {
            var existente = await _repository.ObtenerPorIdempotenciaAsync(
                solicitud.EmpresaId,
                proveedor,
                claveHash,
                cancellationToken);

            if (existente is not null)
                return new InicioPagoOnlineResultadoDto(ToDto(existente), true);

            var factura = await _repository.ObtenerFacturaAsync(solicitud.FacturaId, cancellationToken)
                ?? throw new ResourceNotFoundException("Factura no encontrada o fuera del alcance del usuario actual.");

            if (factura.SaldoPendiente <= 0)
                throw new BusinessRuleException("La factura no tiene saldo pendiente para un pago online.");

            if (solicitud.Monto > factura.SaldoPendiente)
                throw new BusinessRuleException("El monto del pago online no puede superar el saldo pendiente de la factura.");

            if (!string.Equals(moneda, factura.Moneda?.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new BusinessRuleException("La moneda del pago online debe coincidir con la moneda de la factura.");

            var provider = _providers.FirstOrDefault(x =>
                string.Equals(x.Codigo?.Trim(), proveedor, StringComparison.OrdinalIgnoreCase));
            if (provider is null)
                throw new BusinessRuleException($"El proveedor de pago '{proveedor}' no está configurado en este entorno.");

            var ahora = DateTime.UtcNow;
            var pago = new PagoOnline
            {
                EmpresaId = solicitud.EmpresaId,
                FacturaId = solicitud.FacturaId,
                Proveedor = proveedor,
                ClaveIdempotenciaHash = claveHash,
                Monto = solicitud.Monto,
                Moneda = moneda,
                Estado = EstadoPagoOnline.Pendiente,
                CreadoUtc = ahora,
                FechaCreacion = ahora,
                FechaActualizacion = ahora,
                CreadoPorUsuarioId = _currentUser.UsuarioId,
                CreadoPorNombreUsuario = _currentUser.NombreUsuario
            };

            await _repository.AgregarAsync(pago, cancellationToken);
            await _repository.GuardarCambiosAsync(cancellationToken);

            try
            {
                var resultado = await provider.IniciarAsync(
                    new SolicitudInicioPagoOnline(
                        solicitud.EmpresaId,
                        solicitud.FacturaId,
                        solicitud.Monto,
                        moneda,
                        factura.NumeroFactura,
                        claveIdempotencia),
                    cancellationToken);

                ValidarResultadoProveedor(resultado);

                pago.ReferenciaProveedor = resultado.ReferenciaProveedor.Trim();
                pago.UrlPago = resultado.UrlPago.AbsoluteUri;
                pago.Estado = resultado.Estado;
                pago.ConfirmadoUtc = resultado.Estado == EstadoPagoOnline.Pagado ? DateTime.UtcNow : null;
                pago.UltimoError = null;
                pago.FechaActualizacion = DateTime.UtcNow;
                pago.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
                pago.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
                await _repository.GuardarCambiosAsync(cancellationToken);

                return new InicioPagoOnlineResultadoDto(ToDto(pago), false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                pago.Estado = EstadoPagoOnline.Fallido;
                pago.UltimoError = "PROVIDER_INIT_FAILED";
                pago.FechaActualizacion = DateTime.UtcNow;
                pago.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
                pago.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
                await _repository.GuardarCambiosAsync(CancellationToken.None);
                throw new BusinessRuleException(
                    "El proveedor de pago no pudo iniciar la transacción. Usa una nueva clave de idempotencia para reintentar.");
            }
        }
        finally
        {
            gate.Release();
            if (gate.CurrentCount == 1)
                InicioLocks.TryRemove(lockKey, out _);
        }
    }

    public async Task<PagoOnlineDto?> ObtenerAsync(
        int empresaId,
        int id,
        CancellationToken cancellationToken = default)
    {
        if (empresaId <= 0 || id <= 0)
            throw new BusinessRuleException("EmpresaId e id deben ser positivos.");

        var pago = await _repository.ObtenerPorIdAsync(empresaId, id, cancellationToken);
        return pago is null ? null : ToDto(pago);
    }

    public async Task<PaginaPagosOnlineDto> ListarAsync(
        int empresaId,
        int pagina,
        int tamanoPagina,
        EstadoPagoOnline? estado,
        int? facturaId,
        string? proveedor,
        CancellationToken cancellationToken = default)
    {
        if (empresaId <= 0)
            throw new BusinessRuleException("EmpresaId debe ser positivo.");
        if (pagina <= 0)
            throw new BusinessRuleException("La página debe ser positiva.");
        if (tamanoPagina is < 1 or > 100)
            throw new BusinessRuleException("El tamaño de página debe estar entre 1 y 100.");
        if (facturaId.HasValue && facturaId.Value <= 0)
            throw new BusinessRuleException("FacturaId debe ser positivo cuando se especifica.");

        var proveedorNormalizado = string.IsNullOrWhiteSpace(proveedor)
            ? null
            : NormalizarProveedor(proveedor);

        var (items, total) = await _repository.ListarAsync(
            empresaId,
            pagina,
            tamanoPagina,
            estado,
            facturaId,
            proveedorNormalizado,
            cancellationToken);

        return new PaginaPagosOnlineDto(items.Select(ToDto).ToList(), total, pagina, tamanoPagina);
    }

    private static void ValidarSolicitud(IniciarPagoOnlineDto solicitud, string claveIdempotencia)
    {
        if (solicitud.EmpresaId <= 0)
            throw new BusinessRuleException("EmpresaId debe ser positivo.");
        if (solicitud.FacturaId <= 0)
            throw new BusinessRuleException("FacturaId debe ser positivo.");
        if (solicitud.Monto <= 0)
            throw new BusinessRuleException("El monto del pago online debe ser positivo.");
        if (string.IsNullOrWhiteSpace(solicitud.Moneda) || solicitud.Moneda.Trim().Length != 3)
            throw new BusinessRuleException("La moneda debe ser un código ISO de tres caracteres.");
        _ = NormalizarProveedor(solicitud.Proveedor);

        if (string.IsNullOrWhiteSpace(claveIdempotencia) || claveIdempotencia.Length is < 16 or > 128)
            throw new BusinessRuleException("Idempotency-Key debe contener entre 16 y 128 caracteres.");
    }

    private static string NormalizarProveedor(string proveedor)
    {
        if (string.IsNullOrWhiteSpace(proveedor))
            throw new BusinessRuleException("El proveedor de pago es obligatorio.");

        var normalizado = proveedor.Trim().ToLowerInvariant();
        if (normalizado.Length > 64 || normalizado.Any(c => !(char.IsLetterOrDigit(c) || c is '-' or '_' or '.')))
            throw new BusinessRuleException("El código de proveedor contiene caracteres inválidos o supera 64 caracteres.");
        return normalizado;
    }

    private static string CalcularHashIdempotencia(string clave) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(clave))).ToLowerInvariant();

    private static void ValidarResultadoProveedor(ResultadoInicioPagoOnline resultado)
    {
        if (string.IsNullOrWhiteSpace(resultado.ReferenciaProveedor) || resultado.ReferenciaProveedor.Trim().Length > 160)
            throw new BusinessRuleException("El proveedor devolvió una referencia inválida.");
        if (!resultado.UrlPago.IsAbsoluteUri || resultado.UrlPago.Scheme != Uri.UriSchemeHttps)
            throw new BusinessRuleException("El proveedor devolvió una URL de pago inválida.");
        if (!Enum.IsDefined(resultado.Estado))
            throw new BusinessRuleException("El proveedor devolvió un estado de pago inválido.");
    }

    private static PagoOnlineDto ToDto(PagoOnline x) => new(
        x.Id,
        x.EmpresaId,
        x.FacturaId,
        x.Proveedor,
        x.ReferenciaProveedor,
        x.Monto,
        x.Moneda,
        x.Estado,
        x.UrlPago,
        x.CreadoUtc,
        x.ConfirmadoUtc,
        x.UltimoError);
}
