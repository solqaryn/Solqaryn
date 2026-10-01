using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public sealed class ExistenciaVarianteService : IExistenciaVarianteService
{
    private readonly IExistenciaVarianteRepository _repository;
    private readonly IProductoVarianteRepository _varianteRepository;
    private readonly IAlmacenRepository _almacenRepository;
    private readonly IUbicacionAlmacenRepository _ubicacionRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditoriaService _auditoria;

    public ExistenciaVarianteService(
        IExistenciaVarianteRepository repository,
        IProductoVarianteRepository varianteRepository,
        IAlmacenRepository almacenRepository,
        IUbicacionAlmacenRepository ubicacionRepository,
        ICurrentUserService currentUser,
        IAuditoriaService auditoria)
    {
        _repository = repository;
        _varianteRepository = varianteRepository;
        _almacenRepository = almacenRepository;
        _ubicacionRepository = ubicacionRepository;
        _currentUser = currentUser;
        _auditoria = auditoria;
    }

    public async Task<PagedResult<ExistenciaVarianteDto>> BuscarAsync(ExistenciaVarianteFiltroDto filtro)
    {
        ValidarIdOpcional(filtro.ProductoId, "ProductoId");
        ValidarIdOpcional(filtro.ProductoVarianteId, "ProductoVarianteId");
        ValidarIdOpcional(filtro.AlmacenId, "AlmacenId");
        ValidarIdOpcional(filtro.UbicacionAlmacenId, "UbicacionAlmacenId");

        var pagina = Math.Max(1, filtro.Page);
        var tamanoPagina = Math.Clamp(filtro.PageSize, 1, 200);
        var (items, total) = await _repository.BuscarAsync(
            filtro.ProductoId,
            filtro.ProductoVarianteId,
            filtro.AlmacenId,
            filtro.UbicacionAlmacenId,
            filtro.SoloRaizAlmacen,
            filtro.StockBajo,
            filtro.Agotada,
            pagina,
            tamanoPagina);

        return new PagedResult<ExistenciaVarianteDto>
        {
            Items = items.Select(ToDto).ToList(),
            Page = pagina,
            PageSize = tamanoPagina,
            TotalCount = total
        };
    }

    public async Task<ExistenciaVarianteDto?> GetByIdAsync(int id)
    {
        ValidarId(id, "id");
        var existencia = await _repository.GetByIdAsync(id);
        return existencia is null ? null : ToDto(existencia);
    }

    public async Task<ExistenciaVarianteDto> CreateAsync(CreateExistenciaVarianteDto dto)
    {
        var variante = await ObtenerVarianteOperativaAsync(dto.ProductoVarianteId);
        var almacen = await ObtenerAlmacenOperativoAsync(dto.AlmacenId);
        var ubicacion = await ObtenerUbicacionValidaAsync(dto.UbicacionAlmacenId, almacen.Id);

        if (await _repository.ExisteClaveAsync(variante.Id, almacen.Id, ubicacion?.Id))
            throw new BusinessRuleException("Ya existe una existencia para la variante, almacén y ubicación indicados.");

        var existencia = new ExistenciaVariante
        {
            ProductoVarianteId = variante.Id,
            ProductoVariante = variante,
            AlmacenId = almacen.Id,
            Almacen = almacen,
            UbicacionAlmacenId = ubicacion?.Id,
            UbicacionAlmacen = ubicacion,
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario
        };

        AplicarStocks(existencia, dto.StockFisico, dto.StockReservado, dto.StockTransito, dto.StockMinimo, dto.StockMaximo);
        await _repository.AddAsync(existencia);
        if (!await _repository.SaveChangesAsync())
            throw new BusinessRuleException("No fue posible persistir la existencia de inventario.");

        await _auditoria.RegistrarAsync(
            ModuloSistema.Inventario,
            AccionPermiso.Crear,
            $"Existencia creada para variante {variante.Id} en almacén {almacen.Codigo}.",
            existencia.Id,
            entidad: "ExistenciaVariante");

        return ToDto(existencia);
    }

    public async Task<ExistenciaVarianteDto?> UpdateConfiguracionAsync(
        int id,
        UpdateExistenciaVarianteConfiguracionDto dto)
    {
        ValidarId(id, "id");
        var existencia = await _repository.GetByIdAsync(id);
        if (existencia is null)
            return null;

        var ubicacion = await ObtenerUbicacionValidaAsync(dto.UbicacionAlmacenId, existencia.AlmacenId);
        if (await _repository.ExisteClaveAsync(
                existencia.ProductoVarianteId,
                existencia.AlmacenId,
                ubicacion?.Id,
                existencia.Id))
            throw new BusinessRuleException("Ya existe otra existencia para la variante, almacén y ubicación indicados.");

        AplicarStocks(
            existencia,
            existencia.StockFisico,
            existencia.StockReservado,
            existencia.StockTransito,
            dto.StockMinimo,
            dto.StockMaximo);

        existencia.UbicacionAlmacenId = ubicacion?.Id;
        existencia.UbicacionAlmacen = ubicacion;
        existencia.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        existencia.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        existencia.FechaActualizacion = DateTime.UtcNow;

        _repository.Update(existencia);
        if (!await _repository.SaveChangesAsync())
            throw new BusinessRuleException("No fue posible persistir la configuración de la existencia de inventario.");

        await _auditoria.RegistrarAsync(
            ModuloSistema.Inventario,
            AccionPermiso.Editar,
            $"Configuración de existencia {existencia.Id} actualizada.",
            existencia.Id,
            entidad: "ExistenciaVariante");

        return ToDto(existencia);
    }

    private async Task<ProductoVariante> ObtenerVarianteOperativaAsync(int varianteId)
    {
        ValidarId(varianteId, "ProductoVarianteId");
        var variante = await _varianteRepository.GetByIdAsync(varianteId);
        if (variante is null || variante.Eliminado)
            throw new BusinessRuleException("La variante indicada no existe.");
        if (!variante.Activo)
            throw new BusinessRuleException("La variante indicada está inactiva.");
        return variante;
    }

    private async Task<Almacen> ObtenerAlmacenOperativoAsync(int almacenId)
    {
        ValidarId(almacenId, "AlmacenId");
        var almacen = await _almacenRepository.GetByIdAsync(almacenId);
        if (almacen is null || almacen.Eliminado)
            throw new BusinessRuleException("El almacén indicado no existe.");
        if (!almacen.Activo || almacen.Sucursal is null || !almacen.Sucursal.Activa)
            throw new BusinessRuleException("El almacén indicado o su sucursal están inactivos.");
        return almacen;
    }

    private async Task<UbicacionAlmacen?> ObtenerUbicacionValidaAsync(int? ubicacionId, int almacenId)
    {
        if (!ubicacionId.HasValue)
            return null;

        ValidarId(ubicacionId.Value, "UbicacionAlmacenId");
        var ubicacion = await _ubicacionRepository.GetByIdAsync(ubicacionId.Value);
        if (ubicacion is null || ubicacion.Eliminado)
            throw new BusinessRuleException("La ubicación indicada no existe.");
        if (ubicacion.AlmacenId != almacenId)
            throw new BusinessRuleException("La ubicación indicada debe pertenecer al mismo almacén.");
        if (!ubicacion.Activa)
            throw new BusinessRuleException("La ubicación indicada está inactiva.");
        return ubicacion;
    }

    private static void AplicarStocks(
        ExistenciaVariante existencia,
        int fisico,
        int reservado,
        int transito,
        int minimo,
        int? maximo)
    {
        try
        {
            existencia.EstablecerStocks(fisico, reservado, transito, minimo, maximo);
        }
        catch (ArgumentException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }
    }

    private static void ValidarId(int id, string nombre)
    {
        if (id <= 0)
            throw new BusinessRuleException($"{nombre} debe ser mayor que cero.");
    }

    private static void ValidarIdOpcional(int? id, string nombre)
    {
        if (id.HasValue)
            ValidarId(id.Value, nombre);
    }

    private static ExistenciaVarianteDto ToDto(ExistenciaVariante e) => new()
    {
        Id = e.Id,
        ProductoVarianteId = e.ProductoVarianteId,
        ProductoNombre = e.ProductoVariante?.Producto?.Nombre ?? string.Empty,
        VarianteSku = e.ProductoVariante?.Sku ?? string.Empty,
        AlmacenId = e.AlmacenId,
        AlmacenCodigo = e.Almacen?.Codigo ?? string.Empty,
        AlmacenNombre = e.Almacen?.Nombre ?? string.Empty,
        UbicacionAlmacenId = e.UbicacionAlmacenId,
        UbicacionCodigo = e.UbicacionAlmacen?.Codigo,
        UbicacionNombre = e.UbicacionAlmacen?.Nombre,
        StockFisico = e.StockFisico,
        StockReservado = e.StockReservado,
        StockDisponible = e.StockDisponible,
        StockTransito = e.StockTransito,
        StockMinimo = e.StockMinimo,
        StockMaximo = e.StockMaximo,
        TieneStockBajo = e.TieneStockBajo,
        EstaAgotada = e.EstaAgotada,
        FechaCreacion = e.FechaCreacion,
        FechaActualizacion = e.FechaActualizacion
    };
}
