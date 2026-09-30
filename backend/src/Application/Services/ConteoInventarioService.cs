using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public sealed class ConteoInventarioService : IConteoInventarioService
{
    private readonly IConteoInventarioRepository _repository;
    private readonly IExistenciaVarianteRepository _existencias;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAjusteInventarioService? _ajustes;

    public ConteoInventarioService(
        IConteoInventarioRepository repository,
        IExistenciaVarianteRepository existencias,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IAjusteInventarioService? ajustes = null)
    {
        _repository = repository;
        _existencias = existencias;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _ajustes = ajustes;
    }

    public async Task<PagedResult<ConteoInventarioDto>> GetPagedAsync(ConteoInventarioQueryDto query)
    {
        ArgumentNullException.ThrowIfNull(query);
        query.Page = Math.Max(1, query.Page);
        query.PageSize = Math.Clamp(query.PageSize, 1, 100);
        var (items, total) = await _repository.GetPagedAsync(query);
        return new PagedResult<ConteoInventarioDto>
        {
            Items = items.Select(Map).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total
        };
    }

    public async Task<ConteoInventarioDto?> GetByIdAsync(int id)
    {
        if (id <= 0) return null;
        var conteo = await _repository.GetByIdAsync(id);
        return conteo is null ? null : Map(conteo);
    }

    public async Task<ConteoInventarioDto> CreateAsync(CreateConteoInventarioDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidarScope(dto.Tipo, dto.UbicacionAlmacenId, dto.CategoriaId);
        var usuarioId = ObtenerUsuarioId();
        var ahora = DateTime.UtcNow;
        var conteo = new ConteoInventario
        {
            Numero = await GenerarNumeroAsync(ahora),
            Tipo = dto.Tipo,
            AlmacenId = ValidarId(dto.AlmacenId, nameof(dto.AlmacenId)),
            UbicacionAlmacenId = dto.UbicacionAlmacenId,
            CategoriaId = dto.CategoriaId,
            EsCiego = dto.EsCiego || dto.Tipo == TipoConteoInventario.Ciego,
            Observaciones = Normalizar(dto.Observaciones),
            CreadoPorUsuarioId = usuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario,
            ActualizadoPorUsuarioId = usuarioId,
            ActualizadoPorNombreUsuario = _currentUser.NombreUsuario,
            FechaCreacion = ahora,
            FechaActualizacion = ahora
        };

        var detalles = await ConstruirDetallesAsync(conteo, dto.ProductoVarianteIds, usuarioId, ahora);
        foreach (var detalle in detalles) conteo.Detalles.Add(detalle);

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _repository.AddAsync(conteo);
            await _repository.SaveChangesAsync();
        });

        return Map(await _repository.GetByIdAsync(conteo.Id) ?? conteo);
    }

    public async Task<ConteoInventarioDto?> UpdateAsync(int id, UpdateConteoInventarioDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (id <= 0) return null;
        ValidarScope(dto.Tipo, dto.UbicacionAlmacenId, dto.CategoriaId);
        var usuarioId = ObtenerUsuarioId();
        ConteoInventario? resultado = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var conteo = await _repository.GetByIdForUpdateAsync(id);
            if (conteo is null) return;
            if (conteo.Estado != EstadoConteoInventario.Borrador)
                throw new BusinessRuleException("Solo un conteo en borrador puede editarse.");

            conteo.Tipo = dto.Tipo;
            conteo.AlmacenId = ValidarId(dto.AlmacenId, nameof(dto.AlmacenId));
            conteo.UbicacionAlmacenId = dto.UbicacionAlmacenId;
            conteo.CategoriaId = dto.CategoriaId;
            conteo.EsCiego = dto.EsCiego || dto.Tipo == TipoConteoInventario.Ciego;
            conteo.Observaciones = Normalizar(dto.Observaciones);
            conteo.Detalles.Clear();
            var detalles = await ConstruirDetallesAsync(conteo, dto.ProductoVarianteIds, usuarioId, DateTime.UtcNow);
            foreach (var detalle in detalles) conteo.Detalles.Add(detalle);
            MarcarActualizacion(conteo, usuarioId);
            _repository.Update(conteo);
            await _repository.SaveChangesAsync();
            resultado = conteo;
        });

        return resultado is null ? null : Map(await _repository.GetByIdAsync(id) ?? resultado);
    }

    public Task<ConteoInventarioDto?> IniciarAsync(int id) =>
        TransicionarAsync(
            id,
            EstadoConteoInventario.EnProceso,
            (conteo, usuarioId, ahora) => conteo.Iniciar(usuarioId, ahora));

    public async Task<ConteoInventarioDto?> CapturarDetalleAsync(int id, int detalleId, CapturarConteoInventarioDetalleDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (id <= 0 || detalleId <= 0) return null;
        var usuarioId = ObtenerUsuarioId();
        ConteoInventario? resultado = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var conteo = await _repository.GetByIdForUpdateAsync(id);
            if (conteo is null) return;
            if (conteo.Estado != EstadoConteoInventario.EnProceso)
                throw new BusinessRuleException("Solo un conteo en proceso admite capturas.");
            var detalle = conteo.Detalles.SingleOrDefault(x => x.Id == detalleId)
                ?? throw new BusinessRuleException("La línea no pertenece al conteo indicado.");

            if (detalle.CantidadContada == dto.CantidadContada)
            {
                resultado = conteo;
                return;
            }

            detalle.RegistrarConteo(dto.CantidadContada, usuarioId, DateTime.UtcNow);
            MarcarActualizacion(conteo, usuarioId);
            _repository.Update(conteo);
            await _repository.SaveChangesAsync();
            resultado = conteo;
        });

        return resultado is null ? null : Map(resultado);
    }

    public async Task<ConteoInventarioDto?> CapturarLoteAsync(int id, CapturarConteoInventarioLoteDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (id <= 0) return null;
        if (dto.Lineas.Count == 0)
            throw new BusinessRuleException("La captura por lote requiere al menos una línea.");
        if (dto.Lineas.Any(x => x.DetalleId <= 0))
            throw new BusinessRuleException("Todas las líneas del lote deben identificar un detalle válido.");
        if (dto.Lineas.Any(x => x.CantidadContada < 0))
            throw new BusinessRuleException("Las cantidades contadas no pueden ser negativas.");

        var idsDuplicados = dto.Lineas
            .GroupBy(x => x.DetalleId)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .OrderBy(x => x)
            .ToList();
        if (idsDuplicados.Count > 0)
            throw new BusinessRuleException($"La captura por lote contiene detalles duplicados: {string.Join(", ", idsDuplicados)}.");

        var usuarioId = ObtenerUsuarioId();
        ConteoInventario? resultado = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var conteo = await _repository.GetByIdForUpdateAsync(id);
            if (conteo is null) return;
            if (conteo.Estado != EstadoConteoInventario.EnProceso)
                throw new BusinessRuleException("Solo un conteo en proceso admite capturas.");

            var detallesPorId = conteo.Detalles.ToDictionary(x => x.Id);
            var inexistentes = dto.Lineas
                .Where(x => !detallesPorId.ContainsKey(x.DetalleId))
                .Select(x => x.DetalleId)
                .OrderBy(x => x)
                .ToList();
            if (inexistentes.Count > 0)
                throw new BusinessRuleException($"Una o más líneas no pertenecen al conteo indicado: {string.Join(", ", inexistentes)}.");

            var ahora = DateTime.UtcNow;
            var huboCambios = false;
            foreach (var linea in dto.Lineas.OrderBy(x => x.DetalleId))
            {
                var detalle = detallesPorId[linea.DetalleId];
                if (detalle.CantidadContada == linea.CantidadContada) continue;
                detalle.RegistrarConteo(linea.CantidadContada, usuarioId, ahora);
                huboCambios = true;
            }

            if (!huboCambios)
            {
                resultado = conteo;
                return;
            }

            MarcarActualizacion(conteo, usuarioId);
            _repository.Update(conteo);
            await _repository.SaveChangesAsync();
            resultado = conteo;
        });

        return resultado is null ? null : Map(resultado);
    }

    public Task<ConteoInventarioDto?> CerrarAsync(int id) =>
        TransicionarAsync(
            id,
            EstadoConteoInventario.Cerrado,
            (conteo, usuarioId, ahora) => conteo.Cerrar(usuarioId, ahora));

    public Task<ConteoInventarioDto?> AprobarAsync(int id) =>
        TransicionarAsync(
            id,
            EstadoConteoInventario.Aprobado,
            (conteo, usuarioId, ahora) => conteo.Aprobar(usuarioId, ahora));

    public async Task<AjusteInventarioDto?> GenerarAjusteAsync(int id)
    {
        if (id <= 0) return null;
        if (_ajustes is null)
            throw new InvalidOperationException("El servicio formal de ajustes no está disponible para materializar diferencias de conteo.");

        AjusteInventarioDto? resultado = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var conteo = await _repository.GetByIdForUpdateAsync(id);
            if (conteo is null) return;
            if (conteo.Estado != EstadoConteoInventario.Aprobado)
                throw new BusinessRuleException("Solo un conteo aprobado puede generar un ajuste formal de diferencias.");

            var diferencias = conteo.Detalles
                .Where(x => x.Diferencia.HasValue && x.Diferencia.Value != 0)
                .OrderBy(x => x.ProductoVarianteId)
                .ThenBy(x => x.AlmacenId)
                .ThenBy(x => x.UbicacionAlmacenId)
                .ToList();
            if (diferencias.Count == 0)
                throw new BusinessRuleException("El conteo aprobado no contiene diferencias que requieran ajuste.");

            var ajustesExistentes = diferencias
                .Where(x => x.AjusteInventarioId.HasValue)
                .Select(x => x.AjusteInventarioId!.Value)
                .Distinct()
                .ToList();
            if (ajustesExistentes.Count > 1 || (ajustesExistentes.Count == 1 && diferencias.Any(x => !x.AjusteInventarioId.HasValue)))
                throw new BusinessRuleException("Las diferencias del conteo presentan vínculos de ajuste inconsistentes y requieren reconciliación.");
            if (ajustesExistentes.Count == 1)
            {
                resultado = await _ajustes.GetByIdAsync(ajustesExistentes[0])
                    ?? throw new BusinessRuleException("El ajuste previamente vinculado al conteo ya no está disponible.");
                return;
            }

            var dto = new CreateAjusteInventarioDto
            {
                Motivo = $"Diferencias de conteo físico {conteo.Numero}",
                Observaciones = $"Ajuste borrador generado automáticamente desde el conteo aprobado {conteo.Numero}. Requiere confirmación formal posterior.",
                Detalles = diferencias.Select(detalle => new AjusteInventarioDetalleInputDto
                {
                    ProductoId = detalle.ProductoVariante.ProductoId,
                    ProductoVarianteId = detalle.ProductoVarianteId,
                    AlmacenId = detalle.AlmacenId,
                    UbicacionAlmacenId = detalle.UbicacionAlmacenId,
                    CantidadObjetivo = detalle.CantidadContada
                        ?? throw new BusinessRuleException("Una línea con diferencia no posee cantidad contada materializada.")
                }).ToList()
            };

            resultado = await _ajustes.CreateAsync(dto);
            foreach (var detalle in diferencias)
                detalle.VincularAjuste(resultado.Id);

            _repository.Update(conteo);
            await _repository.SaveChangesAsync();
        });

        return resultado;
    }

    public Task<ConteoInventarioDto?> CancelarAsync(int id, string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new BusinessRuleException("El motivo de cancelación es obligatorio.");
        return TransicionarAsync(
            id,
            EstadoConteoInventario.Cancelado,
            (conteo, usuarioId, ahora) => conteo.Cancelar(usuarioId, motivo, ahora));
    }

    private async Task<ConteoInventarioDto?> TransicionarAsync(
        int id,
        EstadoConteoInventario estadoObjetivo,
        Action<ConteoInventario, int, DateTime> accion)
    {
        if (id <= 0) return null;
        var usuarioId = ObtenerUsuarioId();
        ConteoInventario? resultado = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var conteo = await _repository.GetByIdForUpdateAsync(id);
            if (conteo is null) return;

            if (conteo.Estado == estadoObjetivo)
            {
                resultado = conteo;
                return;
            }

            accion(conteo, usuarioId, DateTime.UtcNow);
            MarcarActualizacion(conteo, usuarioId);
            _repository.Update(conteo);
            await _repository.SaveChangesAsync();
            resultado = conteo;
        });
        return resultado is null ? null : Map(resultado);
    }

    private async Task<List<ConteoInventarioDetalle>> ConstruirDetallesAsync(
        ConteoInventario conteo,
        IEnumerable<int> idsSolicitados,
        int usuarioId,
        DateTime ahora)
    {
        var ids = idsSolicitados.Where(x => x > 0).Distinct().ToHashSet();
        var candidatas = new List<ExistenciaVariante>();
        var pagina = 1;
        const int tamano = 100;
        while (true)
        {
            var (items, total) = await _existencias.BuscarAsync(
                null, null, conteo.AlmacenId, conteo.UbicacionAlmacenId,
                conteo.UbicacionAlmacenId.HasValue ? null : false,
                null, null, pagina, tamano);
            candidatas.AddRange(items);
            if (candidatas.Count >= total || items.Count == 0) break;
            pagina++;
        }

        if (ids.Count > 0)
            candidatas = candidatas.Where(x => ids.Contains(x.ProductoVarianteId)).ToList();
        if (conteo.Tipo == TipoConteoInventario.PorCategoria && conteo.CategoriaId.HasValue)
            candidatas = candidatas.Where(x => x.ProductoVariante.Producto.CategoriaId == conteo.CategoriaId.Value).ToList();
        if (ids.Count > 0 && candidatas.Select(x => x.ProductoVarianteId).Distinct().Count() != ids.Count)
            throw new BusinessRuleException("Una o más variantes no tienen existencia física dentro del scope solicitado.");
        if (candidatas.Count == 0)
            throw new BusinessRuleException("El scope del conteo no contiene existencias físicas materializables.");

        return candidatas.Select(existencia =>
        {
            var variante = existencia.ProductoVariante;
            var detalle = new ConteoInventarioDetalle
            {
                ProductoVarianteId = existencia.ProductoVarianteId,
                AlmacenId = existencia.AlmacenId,
                UbicacionAlmacenId = existencia.UbicacionAlmacenId,
                ProductoSkuSnapshot = variante.Sku,
                ProductoMarcaSnapshot = variante.Marca?.Nombre ?? variante.Producto.Marca,
                ProductoModeloSnapshot = variante.Modelo?.Nombre ?? variante.Producto.Modelo,
                ProductoColorSnapshot = variante.Color?.Nombre,
                ProductoTallaSnapshot = variante.Talla?.Nombre,
                CreadoPorUsuarioId = usuarioId,
                CreadoPorNombreUsuario = _currentUser.NombreUsuario,
                ActualizadoPorUsuarioId = usuarioId,
                ActualizadoPorNombreUsuario = _currentUser.NombreUsuario,
                FechaCreacion = ahora,
                FechaActualizacion = ahora
            };
            detalle.MaterializarSnapshot(existencia.StockFisico);
            return detalle;
        }).ToList();
    }

    private async Task<string> GenerarNumeroAsync(DateTime ahora)
    {
        for (var i = 0; i < 100; i++)
        {
            var candidato = $"CNT-{ahora:yyyyMMddHHmmss}-{i:00}";
            if (!await _repository.ExisteNumeroAsync(candidato)) return candidato;
        }
        throw new BusinessRuleException("No fue posible generar un número único para el conteo.");
    }

    private ConteoInventarioDto Map(ConteoInventario conteo)
    {
        var ocultarReferencia = conteo.EsCiego && !conteo.FechaCierre.HasValue;
        return new ConteoInventarioDto
        {
            Id = conteo.Id,
            Numero = conteo.Numero,
            Tipo = conteo.Tipo,
            Estado = conteo.Estado,
            AlmacenId = conteo.AlmacenId,
            AlmacenNombre = conteo.Almacen?.Nombre,
            UbicacionAlmacenId = conteo.UbicacionAlmacenId,
            UbicacionNombre = conteo.UbicacionAlmacen?.Nombre,
            CategoriaId = conteo.CategoriaId,
            CategoriaNombre = conteo.Categoria?.Nombre,
            EsCiego = conteo.EsCiego,
            Observaciones = conteo.Observaciones,
            FechaInicio = conteo.FechaInicio,
            IniciadoPorUsuarioId = conteo.IniciadoPorUsuarioId,
            FechaCierre = conteo.FechaCierre,
            CerradoPorUsuarioId = conteo.CerradoPorUsuarioId,
            FechaAprobacion = conteo.FechaAprobacion,
            AprobadoPorUsuarioId = conteo.AprobadoPorUsuarioId,
            FechaCancelacion = conteo.FechaCancelacion,
            CanceladoPorUsuarioId = conteo.CanceladoPorUsuarioId,
            MotivoCancelacion = conteo.MotivoCancelacion,
            CantidadLineas = conteo.CantidadLineas,
            CantidadCapturadas = conteo.CantidadCapturadas,
            CantidadConDiferencia = ocultarReferencia ? 0 : conteo.CantidadConDiferencia,
            DiferenciaNeta = ocultarReferencia ? 0 : conteo.DiferenciaNeta,
            Detalles = conteo.Detalles.Select(d => new ConteoInventarioDetalleDto
            {
                Id = d.Id,
                ConteoInventarioId = d.ConteoInventarioId,
                ProductoVarianteId = d.ProductoVarianteId,
                AlmacenId = d.AlmacenId,
                UbicacionAlmacenId = d.UbicacionAlmacenId,
                StockEsperado = ocultarReferencia ? null : d.StockEsperadoSnapshot,
                CantidadContada = d.CantidadContada,
                Diferencia = ocultarReferencia ? null : d.Diferencia,
                FechaConteo = d.FechaConteo,
                ContadoPorUsuarioId = d.ContadoPorUsuarioId,
                AjusteInventarioId = d.AjusteInventarioId,
                ProductoSku = d.ProductoSkuSnapshot,
                ProductoMarca = d.ProductoMarcaSnapshot,
                ProductoModelo = d.ProductoModeloSnapshot,
                ProductoColor = d.ProductoColorSnapshot,
                ProductoTalla = d.ProductoTallaSnapshot
            }).ToList()
        };
    }

    private int ObtenerUsuarioId() => _currentUser.UsuarioId is > 0
        ? _currentUser.UsuarioId.Value
        : throw new BusinessRuleException("No existe un usuario autenticado válido para operar el conteo.");

    private static int ValidarId(int id, string nombre) => id > 0
        ? id
        : throw new BusinessRuleException($"{nombre} debe ser válido.");

    private static void ValidarScope(TipoConteoInventario tipo, int? ubicacionAlmacenId, int? categoriaId)
    {
        if (ubicacionAlmacenId.HasValue && ubicacionAlmacenId.Value <= 0)
            throw new BusinessRuleException("UbicacionAlmacenId debe ser válido cuando se especifica.");
        if (categoriaId.HasValue && categoriaId.Value <= 0)
            throw new BusinessRuleException("CategoriaId debe ser válido cuando se especifica.");
        if (tipo == TipoConteoInventario.PorUbicacion && !ubicacionAlmacenId.HasValue)
            throw new BusinessRuleException("Un conteo por ubicación requiere UbicacionAlmacenId.");
        if (tipo == TipoConteoInventario.PorCategoria && !categoriaId.HasValue)
            throw new BusinessRuleException("Un conteo por categoría requiere CategoriaId.");
    }

    private static string? Normalizar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private void MarcarActualizacion(ConteoInventario conteo, int usuarioId)
    {
        conteo.ActualizadoPorUsuarioId = usuarioId;
        conteo.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        conteo.FechaActualizacion = DateTime.UtcNow;
    }
}
