using System.Security.Cryptography;
using System.Text;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public sealed class DevolucionClienteService : IDevolucionClienteService
{
    private readonly IDevolucionClienteRepository _repository;
    private readonly IVentaRepository _ventas;
    private readonly IFacturaRepository _facturas;
    private readonly IAuditoriaService _auditoria;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public DevolucionClienteService(IDevolucionClienteRepository repository, IVentaRepository ventas, IFacturaRepository facturas, IAuditoriaService auditoria, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _ventas = ventas ?? throw new ArgumentNullException(nameof(ventas));
        _facturas = facturas ?? throw new ArgumentNullException(nameof(facturas));
        _auditoria = auditoria ?? throw new ArgumentNullException(nameof(auditoria));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<PagedResult<DevolucionClienteDto>> GetPagedAsync(DevolucionClienteFiltroDto filtro)
    {
        ValidarFiltro(filtro);
        var (items, total) = await _repository.GetPagedAsync(filtro);
        return new PagedResult<DevolucionClienteDto> { Items = items.Select(Map).ToList(), Page = filtro.Page, PageSize = filtro.PageSize, TotalCount = total };
    }

    public async Task<DevolucionClienteDto> GetByIdAsync(int id)
    {
        if (id <= 0) throw new BusinessRuleException("El identificador de la devolución debe ser mayor que cero.");
        var item = await _repository.GetByIdAsync(id, asNoTracking: true) ?? throw new ResourceNotFoundException($"Devolución de cliente con Id {id} no encontrada.");
        return Map(item);
    }

    public async Task<DevolucionClienteDto> CrearAsync(CreateDevolucionClienteDto dto, string idempotencyKey)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.VentaId <= 0) throw new BusinessRuleException("VentaId debe ser mayor que cero.");
        if (dto.FacturaId is <= 0) throw new BusinessRuleException("FacturaId debe ser válido cuando se especifica.");
        if (dto.Detalles is null || dto.Detalles.Count == 0) throw new BusinessRuleException("La devolución requiere al menos un detalle.");
        if (dto.Detalles.GroupBy(x => x.VentaDetalleId).Any(x => x.Count() > 1)) throw new BusinessRuleException("Una línea de venta solo puede aparecer una vez en la solicitud.");

        var key = NormalizarKey(idempotencyKey);
        var fingerprint = Fingerprint(dto);
        RequerirUsuario();
        var id = 0;

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var replay = await _repository.GetByIdempotencyKeyForUpdateAsync(key);
                if (replay is not null)
                {
                    ValidarReplay(replay, fingerprint);
                    id = replay.Id;
                    return;
                }

                var venta = await _ventas.GetByIdForUpdateAsync(dto.VentaId) ?? throw new ResourceNotFoundException($"Venta con Id {dto.VentaId} no encontrada.");
                Factura? factura = null;
                if (dto.FacturaId.HasValue)
                {
                    factura = await _facturas.GetByIdAsync(dto.FacturaId.Value) ?? throw new ResourceNotFoundException($"Factura con Id {dto.FacturaId.Value} no encontrada.");
                    if (factura.VentaId != venta.Id) throw new BusinessRuleException("La factura debe pertenecer a la venta de origen.");
                }

                var devolucion = DevolucionCliente.CrearDesdeVenta(venta, factura);
                foreach (var linea in dto.Detalles)
                {
                    if (linea.VentaDetalleId <= 0 || linea.Cantidad <= 0) throw new BusinessRuleException("Cada detalle debe indicar VentaDetalleId y cantidad válidos.");
                    if (!Enum.IsDefined(typeof(TipoResolucionDevolucionCliente), linea.Resolucion)) throw new BusinessRuleException("La resolución de devolución no es válida.");
                    var detalleVenta = venta.Detalles.FirstOrDefault(x => x.Id == linea.VentaDetalleId) ?? throw new BusinessRuleException($"El detalle de venta {linea.VentaDetalleId} no pertenece a la venta.");
                    var yaDevuelta = await _repository.GetCantidadConfirmadaPorVentaDetalleAsync(linea.VentaDetalleId);
                    devolucion.AgregarDetalle(detalleVenta, linea.Cantidad, yaDevuelta, linea.Resolucion);
                }
                devolucion.ActualizarObservaciones(dto.Observaciones);
                devolucion.EstablecerIdempotencia(key, fingerprint);
                devolucion.ValidarDocumento();

                await _repository.AddAsync(devolucion);
                await _repository.SaveChangesAsync();
                await _auditoria.RegistrarEstrictoAsync(ModuloSistema.Ventas, AccionPermiso.Crear, "Devolución de cliente creada en borrador.", devolucion.Id, nameof(DevolucionCliente), valoresNuevos: new { devolucion.VentaId, devolucion.FacturaId, devolucion.Estado, devolucion.MontoReferencia });
                id = devolucion.Id;
            });
        }
        catch (Exception ex) when (string.Equals(ex.GetType().FullName, "Microsoft.EntityFrameworkCore.DbUpdateException", StringComparison.Ordinal))
        {
            var replay = await _repository.GetByIdempotencyKeyAsync(key, tracking: false);
            if (replay is null) throw;
            ValidarReplay(replay, fingerprint);
            id = replay.Id;
        }

        return await GetByIdAsync(id);
    }

    public async Task<DevolucionClienteDto> ConfirmarAsync(int id)
    {
        var (usuarioId, nombre) = RequerirUsuario();
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var item = await RequerirForUpdateAsync(id);
            if (item.Estado == EstadoDevolucionCliente.Confirmada) return;
            item.Confirmar(usuarioId, nombre, DateTime.UtcNow);
            _repository.Update(item);
            await _repository.SaveChangesAsync();
            await _auditoria.RegistrarEstrictoAsync(ModuloSistema.Ventas, AccionPermiso.Confirmar, "Devolución de cliente confirmada.", item.Id, nameof(DevolucionCliente), valoresNuevos: new { item.Estado, item.MontoReferencia });
        });
        return await GetByIdAsync(id);
    }

    public async Task<DevolucionClienteDto> AnularAsync(int id, string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo)) throw new BusinessRuleException("El motivo de anulación es obligatorio.");
        var (usuarioId, nombre) = RequerirUsuario();
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var item = await RequerirForUpdateAsync(id);
            item.Anular(usuarioId, nombre, motivo, DateTime.UtcNow);
            _repository.Update(item);
            await _repository.SaveChangesAsync();
            await _auditoria.RegistrarEstrictoAsync(ModuloSistema.Ventas, AccionPermiso.Anular, "Devolución de cliente anulada.", item.Id, nameof(DevolucionCliente), valoresNuevos: new { item.Estado }, motivo: motivo.Trim());
        });
        return await GetByIdAsync(id);
    }

    private async Task<DevolucionCliente> RequerirForUpdateAsync(int id)
    {
        if (id <= 0) throw new BusinessRuleException("El identificador de la devolución debe ser mayor que cero.");
        return await _repository.GetByIdForUpdateAsync(id) ?? throw new ResourceNotFoundException($"Devolución de cliente con Id {id} no encontrada.");
    }

    private (int UsuarioId, string Nombre) RequerirUsuario()
    {
        if (_currentUser.UsuarioId is not > 0) throw new ForbiddenAccessException("La operación requiere un usuario autenticado.");
        var nombre = _currentUser.NombreCompleto?.Trim();
        if (string.IsNullOrWhiteSpace(nombre)) nombre = _currentUser.NombreUsuario?.Trim();
        if (string.IsNullOrWhiteSpace(nombre)) throw new ForbiddenAccessException("No se pudo resolver la identidad del usuario autenticado.");
        return (_currentUser.UsuarioId.Value, nombre);
    }

    private static void ValidarFiltro(DevolucionClienteFiltroDto filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        if (filtro.Page < 1) throw new BusinessRuleException("Page debe ser mayor o igual a 1.");
        if (filtro.PageSize is < 1 or > 200) throw new BusinessRuleException("PageSize debe estar entre 1 y 200.");
        if (filtro.Estado.HasValue && !Enum.IsDefined(typeof(EstadoDevolucionCliente), filtro.Estado.Value)) throw new BusinessRuleException("El estado solicitado no es válido.");
    }

    private static string NormalizarKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Trim().Length > 128) throw new BusinessRuleException("Idempotency-Key es obligatorio y no puede superar 128 caracteres.");
        return key.Trim();
    }

    private static string Fingerprint(CreateDevolucionClienteDto dto)
    {
        var lineas = string.Join(";", dto.Detalles.OrderBy(x => x.VentaDetalleId).Select(x => $"{x.VentaDetalleId}:{x.Cantidad}:{(int)x.Resolucion}"));
        var payload = $"{dto.VentaId}|{dto.FacturaId?.ToString() ?? ""}|{dto.Observaciones?.Trim() ?? ""}|{lineas}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    private static void ValidarReplay(DevolucionCliente item, string fingerprint)
    {
        if (!string.Equals(item.IdempotencyFingerprint, fingerprint, StringComparison.Ordinal)) throw new BusinessRuleException("La clave de idempotencia ya fue usada con otro payload.");
    }

    private static DevolucionClienteDto Map(DevolucionCliente x) => new()
    {
        Id = x.Id, VentaId = x.VentaId, FacturaId = x.FacturaId, Estado = x.Estado, Observaciones = x.Observaciones,
        IdempotencyKey = x.IdempotencyKey, MontoReferencia = x.MontoReferencia, FechaCreacion = x.FechaCreacion,
        FechaConfirmacion = x.FechaConfirmacion, FechaAnulacion = x.FechaAnulacion, MotivoAnulacion = x.MotivoAnulacion,
        Detalles = x.Detalles.Select(d => new DevolucionClienteDetalleDto
        {
            Id = d.Id, VentaDetalleId = d.VentaDetalleId, ProductoId = d.ProductoId, ProductoVarianteId = d.ProductoVarianteId,
            ProductoSkuSnapshot = d.ProductoSkuSnapshot, ProductoNombreSnapshot = d.ProductoNombreSnapshot, ProductoMarcaSnapshot = d.ProductoMarcaSnapshot,
            ProductoModeloSnapshot = d.ProductoModeloSnapshot, ProductoColorSnapshot = d.ProductoColorSnapshot, ProductoTallaSnapshot = d.ProductoTallaSnapshot,
            Cantidad = d.Cantidad, CantidadVendidaSnapshot = d.CantidadVendidaSnapshot, PrecioUnitarioSnapshot = d.PrecioUnitarioSnapshot,
            Resolucion = d.Resolucion, MontoReferencia = d.MontoReferencia
        }).ToList()
    };
}
