using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public class ProductoVarianteService : IProductoVarianteService
{
    private readonly IProductoVarianteRepository _repository;
    private readonly IProductoRepository _productoRepository;
    private readonly ICatalogoProductoService _catalogoService;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditoriaService _auditoria;

    public ProductoVarianteService(
        IProductoVarianteRepository repository,
        IProductoRepository productoRepository,
        ICatalogoProductoService catalogoService,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IAuditoriaService auditoria)
    {
        _repository = repository;
        _productoRepository = productoRepository;
        _catalogoService = catalogoService;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _auditoria = auditoria;
    }

    public async Task<List<ProductoVarianteDto>> GetByProductoIdAsync(int productoId, bool incluirInactivas = true)
    {
        await ObtenerProductoAsync(productoId);
        var variantes = await _repository.GetByProductoIdAsync(productoId, incluirInactivas);
        return variantes.Select(ToDto).ToList();
    }

    public async Task<ProductoVarianteDto?> GetByIdAsync(int productoId, int id)
    {
        var variante = await _repository.GetByIdAsync(id);
        return variante is null || variante.ProductoId != productoId ? null : ToDto(variante);
    }

    public async Task<ProductoVarianteDto> CreateAsync(int productoId, CreateProductoVarianteDto dto)
    {
        Producto? producto = null;
        ProductoVariante? variante = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            producto = await _productoRepository.GetByIdForUpdateAsync(productoId)
                ?? throw new BusinessRuleException("El producto no existe o fue eliminado.");

            var tecnicaExistente = await _repository.GetTecnicaByProductoIdAsync(productoId);
            if (tecnicaExistente is not null)
                throw new BusinessRuleException("El producto es simple. Convierte primero su variante técnica desde el mantenimiento del producto.");

            await ValidarAsync(productoId, null, dto);
            variante = new ProductoVariante
            {
                ProductoId = productoId,
                MarcaId = dto.MarcaId,
                ModeloId = dto.ModeloId,
                ColorId = dto.ColorId,
                TallaId = dto.TallaId,
                Sku = await ResolverSkuAsync(productoId, dto),
                CodigoBarras = NormalizarOpcional(dto.CodigoBarras),
                Cantidad = dto.Cantidad,
                UmbralStockBajo = dto.UmbralStockBajo,
                Costo = dto.Costo,
                Precio = dto.Precio,
                Activo = true,
                Eliminado = false,
                EsTecnica = false,
                CreadoPorUsuarioId = _currentUser.UsuarioId,
                CreadoPorNombreUsuario = _currentUser.NombreUsuario
            };

            await _repository.AddAsync(variante);
            await _repository.SaveChangesAsync();
            await SincronizarProyeccionCompatibilidadAsync(producto);
        });

        await _auditoria.RegistrarAsync(
            ModuloSistema.Productos,
            AccionPermiso.Crear,
            $"Variante creada para {producto!.Nombre}: {variante!.Sku}.",
            variante.Id,
            entidad: "ProductoVariante",
            valoresNuevos: Snapshot(variante));

        return ToDto((await _repository.GetByIdAsync(variante.Id))!);
    }

    public async Task<ProductoVarianteDto?> UpdateAsync(int productoId, int id, UpdateProductoVarianteDto dto)
    {
        Producto? producto = null;
        ProductoVariante? variante = null;
        object? anteriores = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            producto = await _productoRepository.GetByIdForUpdateAsync(productoId);
            if (producto is null) return;

            variante = await _repository.GetByIdForUpdateAsync(id);
            if (variante is null || variante.ProductoId != productoId)
            {
                variante = null;
                return;
            }
            if (variante.EsTecnica)
                throw new BusinessRuleException("La variante técnica no puede editarse manualmente. Actualiza el producto simple.");

            anteriores = Snapshot(variante);
            await ValidarAsync(productoId, id, dto);
            if (dto.Cantidad != variante.Cantidad)
                throw new BusinessRuleException("El stock de la variante no puede modificarse desde el mantenimiento general. Utiliza la operación Ajustar inventario.");

            variante.MarcaId = dto.MarcaId;
            variante.ModeloId = dto.ModeloId;
            variante.ColorId = dto.ColorId;
            variante.TallaId = dto.TallaId;
            variante.Sku = string.IsNullOrWhiteSpace(dto.Sku) ? variante.Sku : NormalizarSku(dto.Sku);
            variante.CodigoBarras = NormalizarOpcional(dto.CodigoBarras);
            variante.UmbralStockBajo = dto.UmbralStockBajo;
            variante.Costo = dto.Costo;
            variante.Precio = dto.Precio;
            MarcarActualizacion(variante);

            await _repository.SaveChangesAsync();
            await SincronizarProyeccionCompatibilidadAsync(producto);
        });

        if (producto is null || variante is null) return null;

        await _auditoria.RegistrarAsync(
            ModuloSistema.Productos,
            AccionPermiso.Editar,
            $"Variante actualizada para {producto.Nombre}: {variante.Sku}.",
            variante.Id,
            entidad: "ProductoVariante",
            valoresAnteriores: anteriores,
            valoresNuevos: Snapshot(variante));

        return ToDto((await _repository.GetByIdAsync(variante.Id))!);
    }

    public async Task<ProductoVarianteDto?> CambiarEstadoAsync(int productoId, int id, bool activo)
    {
        Producto? producto = null;
        ProductoVariante? variante = null;
        var cambioRealizado = false;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            producto = await _productoRepository.GetByIdForUpdateAsync(productoId);
            if (producto is null) return;
            variante = await _repository.GetByIdForUpdateAsync(id);
            if (variante is null || variante.ProductoId != productoId)
            {
                variante = null;
                return;
            }
            if (variante.EsTecnica)
                throw new BusinessRuleException("La variante técnica hereda el estado del producto y no puede activarse o desactivarse manualmente.");
            if (variante.Activo == activo) return;

            variante.Activo = activo;
            MarcarActualizacion(variante);
            cambioRealizado = true;
            await _repository.SaveChangesAsync();
            await SincronizarProyeccionCompatibilidadAsync(producto);
        });

        if (producto is null || variante is null) return null;
        if (!cambioRealizado) return ToDto(variante);

        await _auditoria.RegistrarAsync(
            ModuloSistema.Productos,
            activo ? AccionPermiso.Activar : AccionPermiso.Desactivar,
            $"Variante {(activo ? "activada" : "desactivada")}: {variante.Sku}.",
            variante.Id,
            entidad: "ProductoVariante",
            valoresNuevos: new { variante.Activo });

        return ToDto((await _repository.GetByIdAsync(variante.Id))!);
    }

    public async Task<bool> DeleteAsync(int productoId, int id)
    {
        Producto? producto = null;
        ProductoVariante? variante = null;
        var guardado = false;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            producto = await _productoRepository.GetByIdForUpdateAsync(productoId);
            if (producto is null) return;
            variante = await _repository.GetByIdForUpdateAsync(id);
            if (variante is null || variante.ProductoId != productoId)
            {
                variante = null;
                return;
            }
            if (variante.EsTecnica)
                throw new BusinessRuleException("La variante técnica no puede eliminarse manualmente.");
            if (variante.Cantidad != 0)
                throw new BusinessRuleException("Solo se puede eliminar una variante cuando su stock sea cero. Puedes desactivarla para conservar existencias e historial.");

            variante.Activo = false;
            variante.Eliminado = true;
            variante.FechaEliminacion = DateTime.UtcNow;
            variante.EliminadoPorUsuarioId = _currentUser.UsuarioId;
            MarcarActualizacion(variante);
            guardado = await _repository.SaveChangesAsync();
            await SincronizarProyeccionCompatibilidadAsync(producto);
        });

        if (producto is null || variante is null) return false;
        if (guardado)
            await _auditoria.RegistrarAsync(ModuloSistema.Productos, AccionPermiso.EliminarLogico,
                $"Variante eliminada lógicamente: {variante.Sku}.", variante.Id,
                entidad: "ProductoVariante", valoresNuevos: new { variante.Eliminado, variante.FechaEliminacion });
        return guardado;
    }

    public async Task<ProductoVarianteDto> AsegurarTecnicaAsync(int productoId)
    {
        ProductoVariante? tecnica = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var producto = await _productoRepository.GetByIdForUpdateAsync(productoId)
                ?? throw new BusinessRuleException("El producto no existe o fue eliminado.");
            tecnica = await AsegurarTecnicaBajoLockAsync(producto);
        });
        return ToDto(tecnica!);
    }

    public async Task RetirarTecnicaParaConversionAsync(int productoId)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var producto = await _productoRepository.GetByIdForUpdateAsync(productoId)
                ?? throw new BusinessRuleException("El producto no existe o fue eliminado.");
            var tecnica = await _repository.GetTecnicaByProductoIdAsync(productoId);
            if (tecnica is null) return;
            tecnica = await _repository.GetByIdForUpdateAsync(tecnica.Id) ?? tecnica;
            if (tecnica.Cantidad != 0)
                throw new BusinessRuleException("No puedes convertir el producto a variantes comerciales mientras la variante técnica tenga existencias. Ajusta primero su stock a cero.");
            MarcarEliminacionTecnica(tecnica);
            await _repository.SaveChangesAsync();
            await SincronizarProyeccionCompatibilidadAsync(producto);
        });
    }

    public async Task<ProductoVarianteDto> SincronizarTecnicaAsync(int productoId, ProductoVarianteFormularioDto dto)
    {
        ProductoVariante? tecnica = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var producto = await _productoRepository.GetByIdForUpdateAsync(productoId)
                ?? throw new BusinessRuleException("El producto no existe o fue eliminado.");
            if (!string.IsNullOrWhiteSpace(dto.Sku) || !string.IsNullOrWhiteSpace(dto.CodigoBarras))
                throw new BusinessRuleException("La variante técnica utiliza un SKU interno y no admite código de barras manual.");
            if (dto.ModeloId.HasValue && !dto.MarcaId.HasValue)
                throw new BusinessRuleException("Todo modelo debe indicar su marca.");
            if (dto.Cantidad < 0 || dto.UmbralStockBajo < 0 || dto.Costo < 0 || dto.Precio <= 0)
                throw new BusinessRuleException("La variante técnica requiere stock/umbral no negativos, costo no negativo y precio mayor que cero.");
            await _catalogoService.ValidarSeleccionProductoAsync(dto.ColorId, dto.TallaId, dto.MarcaId, dto.ModeloId);

            var vigentes = await _repository.GetByProductoIdAsync(productoId, true);
            if (vigentes.Any(v => !v.EsTecnica))
                throw new BusinessRuleException("No se puede sincronizar una variante técnica mientras existan variantes comerciales.");

            tecnica = await _repository.GetTecnicaByProductoIdAsync(productoId, true);
            var esNueva = tecnica is null || tecnica.Eliminado;
            if (tecnica is null)
            {
                tecnica = new ProductoVariante
                {
                    ProductoId = productoId,
                    Sku = CrearSkuTecnico(productoId),
                    EsTecnica = true,
                    CreadoPorUsuarioId = _currentUser.UsuarioId,
                    CreadoPorNombreUsuario = _currentUser.NombreUsuario
                };
                await _repository.AddAsync(tecnica);
            }
            else if (!esNueva && tecnica.Cantidad != dto.Cantidad)
            {
                throw new BusinessRuleException("El stock del producto simple debe cambiarse mediante Ajustar inventario para conservar trazabilidad.");
            }

            tecnica.EsTecnica = true;
            tecnica.MarcaId = dto.MarcaId;
            tecnica.ModeloId = dto.ModeloId;
            tecnica.ColorId = dto.ColorId;
            tecnica.TallaId = dto.TallaId;
            tecnica.CodigoBarras = null;
            if (esNueva) tecnica.Cantidad = dto.Cantidad;
            tecnica.UmbralStockBajo = dto.UmbralStockBajo;
            tecnica.Costo = dto.Costo;
            tecnica.Precio = dto.Precio;
            tecnica.Activo = producto.Activo;
            tecnica.Eliminado = false;
            tecnica.FechaEliminacion = null;
            tecnica.EliminadoPorUsuarioId = null;
            MarcarActualizacion(tecnica);
            await _repository.SaveChangesAsync();
            await SincronizarProyeccionCompatibilidadAsync(producto);
        });
        return ToDto((await _repository.GetByIdAsync(tecnica!.Id))!);
    }

    public async Task SincronizarTecnicaConProductoAsync(int productoId)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var producto = await _productoRepository.GetByIdForUpdateAsync(productoId);
            if (producto is null) return;
            var vigentes = await _repository.GetByProductoIdAsync(productoId, true);
            var comerciales = vigentes.Where(v => !v.EsTecnica).ToList();
            var tecnica = vigentes.SingleOrDefault(v => v.EsTecnica);
            if (comerciales.Count > 0)
            {
                if (tecnica is not null)
                    throw new BusinessRuleException("El producto no puede conservar simultáneamente una variante técnica y variantes comerciales.");
                return;
            }
            tecnica ??= await AsegurarTecnicaBajoLockAsync(producto);
            tecnica.Activo = producto.Activo;
            MarcarActualizacion(tecnica);
            await _repository.SaveChangesAsync();
        });
    }

    public async Task EliminarTecnicaConProductoAsync(int productoId)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var producto = await _productoRepository.GetByIdForUpdateAsync(productoId);
            if (producto is null) return;
            var tecnica = await _repository.GetTecnicaByProductoIdAsync(productoId);
            if (tecnica is null) return;
            tecnica = await _repository.GetByIdForUpdateAsync(tecnica.Id) ?? tecnica;
            MarcarEliminacionTecnica(tecnica);
            await _repository.SaveChangesAsync();
        });
    }

    private async Task<ProductoVariante> AsegurarTecnicaBajoLockAsync(Producto producto)
    {
        var vigentes = await _repository.GetByProductoIdAsync(producto.Id, true);
        if (vigentes.Any(v => !v.EsTecnica))
            throw new BusinessRuleException("No se puede crear una variante técnica porque el producto tiene variantes comerciales.");

        var tecnica = await _repository.GetTecnicaByProductoIdAsync(producto.Id, true);
        var esNueva = tecnica is null;
        if (tecnica is null)
        {
            tecnica = new ProductoVariante
            {
                ProductoId = producto.Id,
                Sku = CrearSkuTecnico(producto.Id),
                EsTecnica = true,
                CreadoPorUsuarioId = _currentUser.UsuarioId,
                CreadoPorNombreUsuario = _currentUser.NombreUsuario
            };
            await _repository.AddAsync(tecnica);
        }

        if (esNueva)
        {
            tecnica.MarcaId = producto.MarcaId;
            tecnica.ModeloId = producto.ModeloId;
            tecnica.ColorId = producto.ColorId;
            tecnica.TallaId = producto.TallaId;
            tecnica.Cantidad = producto.Cantidad;
            tecnica.UmbralStockBajo = producto.UmbralStockBajo;
            tecnica.Costo = producto.Costo;
            tecnica.Precio = producto.Precio;
        }

        tecnica.EsTecnica = true;
        tecnica.CodigoBarras = null;
        tecnica.Activo = producto.Activo;
        tecnica.Eliminado = false;
        tecnica.FechaEliminacion = null;
        tecnica.EliminadoPorUsuarioId = null;
        MarcarActualizacion(tecnica);
        await _repository.SaveChangesAsync();
        return tecnica;
    }

    private async Task ValidarAsync(int productoId, int? varianteId, CreateProductoVarianteDto dto)
    {
        if (!dto.MarcaId.HasValue && !dto.ModeloId.HasValue && !dto.ColorId.HasValue && !dto.TallaId.HasValue)
            throw new BusinessRuleException("Una variante comercial debe definir al menos una dimensión: marca, modelo, color o talla.");
        if (dto.ModeloId.HasValue && !dto.MarcaId.HasValue)
            throw new BusinessRuleException("Todo modelo de variante debe indicar su marca.");
        if (dto.Cantidad < 0)
            throw new BusinessRuleException("El stock de la variante no puede ser negativo.");
        if (dto.UmbralStockBajo < 0)
            throw new BusinessRuleException("El umbral de stock bajo no puede ser negativo.");
        if (dto.Costo < 0 || dto.Precio <= 0)
            throw new BusinessRuleException("El costo no puede ser negativo y el precio debe ser mayor que cero.");

        await _catalogoService.ValidarSeleccionProductoAsync(dto.ColorId, dto.TallaId, dto.MarcaId, dto.ModeloId);

        var porCombinacion = await _repository.GetByCombinacionAsync(
            productoId, dto.MarcaId, dto.ModeloId, dto.ColorId, dto.TallaId);
        if (porCombinacion is not null && porCombinacion.Id != varianteId)
            throw new BusinessRuleException("Este producto ya tiene una variante con la misma combinación de marca, modelo, color y talla.");

        if (!string.IsNullOrWhiteSpace(dto.Sku))
        {
            var sku = NormalizarSku(dto.Sku);
            var porSku = await _repository.GetBySkuAsync(sku);
            if (porSku is not null && porSku.Id != varianteId)
                throw new BusinessRuleException($"El SKU '{sku}' ya está utilizado por otra variante.");
        }

        var codigo = NormalizarOpcional(dto.CodigoBarras);
        if (codigo is not null)
        {
            var porCodigo = await _repository.GetByCodigoBarrasAsync(codigo);
            if (porCodigo is not null && porCodigo.Id != varianteId)
                throw new BusinessRuleException("El código de barras ya está utilizado por otra variante.");
        }
    }

    private async Task<string> ResolverSkuAsync(int productoId, CreateProductoVarianteDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.Sku)) return NormalizarSku(dto.Sku);

        for (var intento = 0; intento < 5; intento++)
        {
            var sku = $"VAR-{productoId:D6}-M{dto.MarcaId ?? 0}-O{dto.ModeloId ?? 0}-C{dto.ColorId ?? 0}-T{dto.TallaId ?? 0}-{Guid.NewGuid():N}";
            sku = sku[..Math.Min(sku.Length, 80)].ToUpperInvariant();
            if (await _repository.GetBySkuAsync(sku) is null) return sku;
        }
        throw new BusinessRuleException("No fue posible generar un SKU único para la variante. Intenta nuevamente.");
    }

    private async Task<Producto> ObtenerProductoAsync(int productoId) =>
        await _productoRepository.GetByIdAsync(productoId)
        ?? throw new BusinessRuleException("El producto no existe o fue eliminado.");

    // Compatibilidad transitoria N0.3: Producto conserva un espejo DERIVADO, nunca autoridad operativa.
    private async Task SincronizarProyeccionCompatibilidadAsync(Producto producto)
    {
        var variantes = await _repository.GetByProductoIdAsync(producto.Id, true);
        var activas = variantes.Where(v => v.Activo).ToList();
        var total = variantes.Sum(v => v.Cantidad);
        producto.Cantidad = total;
        producto.UmbralStockBajo = variantes.Sum(v => v.UmbralStockBajo);
        if (variantes.Count > 0)
        {
            producto.Costo = total > 0
                ? Math.Round(variantes.Sum(v => (v.Costo ?? 0m) * v.Cantidad) / total, 2, MidpointRounding.AwayFromZero)
                : Math.Round(variantes.Average(v => v.Costo ?? 0m), 2, MidpointRounding.AwayFromZero);
            var fuentePrecio = activas.Count > 0 ? activas : variantes;
            producto.Precio = fuentePrecio.Min(v => v.Precio ?? 0m);
            producto.MarcaId = ValorComun(variantes.Select(v => v.MarcaId));
            producto.ModeloId = ValorComun(variantes.Select(v => v.ModeloId));
            producto.ColorId = ValorComun(variantes.Select(v => v.ColorId));
            producto.TallaId = ValorComun(variantes.Select(v => v.TallaId));
            producto.Marca = producto.MarcaId.HasValue
                ? variantes.FirstOrDefault(v => v.MarcaId == producto.MarcaId)?.Marca?.Nombre ?? producto.Marca
                : string.Empty;
            producto.Modelo = producto.ModeloId.HasValue
                ? variantes.FirstOrDefault(v => v.ModeloId == producto.ModeloId)?.Modelo?.Nombre ?? producto.Modelo
                : string.Empty;
        }
        producto.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        producto.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        producto.FechaActualizacion = DateTime.UtcNow;
        await _productoRepository.SaveChangesAsync();
    }

    private static int? ValorComun(IEnumerable<int?> valores)
    {
        var lista = valores.Distinct().Take(2).ToList();
        return lista.Count == 1 ? lista[0] : null;
    }


    private void MarcarActualizacion(ProductoVariante variante)
    {
        variante.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        variante.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        variante.FechaActualizacion = DateTime.UtcNow;
    }

    private void MarcarEliminacionTecnica(ProductoVariante tecnica)
    {
        tecnica.Activo = false;
        tecnica.Eliminado = true;
        tecnica.FechaEliminacion = DateTime.UtcNow;
        tecnica.EliminadoPorUsuarioId = _currentUser.UsuarioId;
        MarcarActualizacion(tecnica);
    }

    private static string CrearSkuTecnico(int productoId) => $"TEC-{productoId:D10}";
    private static string NormalizarSku(string sku) => sku.Trim().ToUpperInvariant();
    private static string? NormalizarOpcional(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static object Snapshot(ProductoVariante v) => new
    {
        v.ProductoId, v.MarcaId, v.ModeloId, v.ColorId, v.TallaId,
        v.Sku, v.CodigoBarras, v.Cantidad, v.UmbralStockBajo, v.Costo, v.Precio
    };

    private static ProductoVarianteDto ToDto(ProductoVariante v)
    {
        var partes = new[] { v.Marca?.Nombre, v.Modelo?.Nombre, v.Color?.Nombre, v.Talla?.Nombre }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToList();
        if (!string.IsNullOrWhiteSpace(v.Sku)) partes.Add(v.Sku!);

        return new ProductoVarianteDto
        {
            Id = v.Id,
            ProductoId = v.ProductoId,
            ProductoNombre = v.Producto?.Nombre ?? string.Empty,
            MarcaId = v.MarcaId,
            MarcaNombre = v.Marca?.Nombre,
            ModeloId = v.ModeloId,
            ModeloNombre = v.Modelo?.Nombre,
            ColorId = v.ColorId,
            ColorNombre = v.Color?.Nombre,
            ColorCodigoVisual = v.Color?.CodigoVisual,
            TallaId = v.TallaId,
            TallaNombre = v.Talla?.Nombre,
            Etiqueta = partes.Count > 0 ? string.Join(" · ", partes) : "Variante técnica",
            Sku = v.Sku ?? string.Empty,
            CodigoBarras = v.CodigoBarras,
            Cantidad = v.Cantidad,
            UmbralStockBajo = v.UmbralStockBajo,
            Costo = v.Costo ?? 0m,
            Precio = v.Precio ?? 0m,
            Activo = v.Activo,
            EsTecnica = v.EsTecnica,
            TieneStockBajo = v.TieneStockBajo,
            EstaAgotada = v.EstaAgotada,
            EstadoInventario = v.EstaAgotada ? "Agotada" : v.TieneStockBajo ? "Stock bajo" : "Disponible",
            FechaCreacion = v.FechaCreacion,
            FechaActualizacion = v.FechaActualizacion
        };
    }
}
