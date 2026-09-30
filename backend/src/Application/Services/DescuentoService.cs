using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public class DescuentoService : IDescuentoService
{
    private readonly IDescuentoRepository _repository;
    private readonly IAuditoriaService _auditoria;
    private readonly ICurrentUserService _currentUser;

    public DescuentoService(IDescuentoRepository repository, IAuditoriaService auditoria, ICurrentUserService currentUser)
    {
        _repository = repository;
        _auditoria = auditoria;
        _currentUser = currentUser;
    }

    public async Task<List<DescuentoDto>> GetAllAsync(bool incluirEliminados = false)
    {
        var lista = await _repository.GetAllAsync(incluirEliminados);
        var resultado = new List<DescuentoDto>();
        foreach (var d in lista)
        {
            var conRelaciones = await _repository.GetByIdConRelacionesAsync(d.Id);
            if (conRelaciones is not null) resultado.Add(ToDto(conRelaciones));
        }
        return resultado;
    }

    public async Task<DescuentoDto?> GetByIdAsync(int id)
    {
        var d = await _repository.GetByIdConRelacionesAsync(id);
        return d is null ? null : ToDto(d);
    }

    private static void ValidarComun(GuardarDescuentoDto dto, TipoDescuento tipo)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            throw new BusinessRuleException("El nombre del descuento es obligatorio.");
        if (dto.Valor < 0)
            throw new BusinessRuleException("El valor del descuento no puede ser negativo.");
        if (tipo == TipoDescuento.Porcentaje && dto.Valor > 100)
            throw new BusinessRuleException("Un descuento porcentual no puede ser mayor a 100%.");
        if (dto.FechaInicio.HasValue && dto.FechaFin.HasValue && dto.FechaFin < dto.FechaInicio)
            throw new BusinessRuleException("La fecha final no puede ser anterior a la fecha de inicio.");
        if (dto.MontoMinimo is < 0) throw new BusinessRuleException("El monto mínimo no puede ser negativo.");
        if (dto.MontoMaximoDescuento is < 0) throw new BusinessRuleException("El monto máximo de descuento no puede ser negativo.");
        if (dto.CantidadMinima is < 0) throw new BusinessRuleException("La cantidad mínima no puede ser negativa.");
        if (dto.LimiteTotalUsos is < 0 || dto.LimiteUsosPorCliente is < 0)
            throw new BusinessRuleException("Los límites de uso no pueden ser negativos.");
    }

    public async Task<DescuentoDto> CreateAsync(GuardarDescuentoDto dto)
    {
        if (!Enum.TryParse<TipoDescuento>(dto.Tipo, true, out var tipo))
            throw new BusinessRuleException($"Tipo de descuento '{dto.Tipo}' no es válido.");

        ValidarComun(dto, tipo);

        string? codigoNormalizado = null;
        if (!string.IsNullOrWhiteSpace(dto.CodigoPromocional))
        {
            codigoNormalizado = dto.CodigoPromocional.Trim().ToUpperInvariant();
            if (await _repository.ExisteCodigoAsync(codigoNormalizado))
                throw new BusinessRuleException($"Ya existe un descuento con el código '{dto.CodigoPromocional}'.");
        }

        var descuento = new Descuento
        {
            Nombre = dto.Nombre.Trim(),
            Descripcion = dto.Descripcion,
            CodigoPromocional = string.IsNullOrWhiteSpace(dto.CodigoPromocional) ? null : dto.CodigoPromocional.Trim(),
            CodigoPromocionalNormalizado = codigoNormalizado,
            Tipo = tipo,
            Valor = dto.Valor,
            FechaInicio = dto.FechaInicio,
            FechaFin = dto.FechaFin,
            MontoMinimo = dto.MontoMinimo,
            MontoMaximoDescuento = dto.MontoMaximoDescuento,
            CantidadMinima = dto.CantidadMinima,
            RequiereAprobacion = dto.RequiereAprobacion,
            Acumulable = dto.Acumulable,
            Prioridad = dto.Prioridad,
            LimiteTotalUsos = dto.LimiteTotalUsos,
            LimiteUsosPorCliente = dto.LimiteUsosPorCliente,
            Activo = true,
            CreadoPorUsuarioId = _currentUser.UsuarioId
        };

        AplicarAlcances(descuento, dto);

        await _repository.AddAsync(descuento);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Descuentos, AccionPermiso.Crear, $"Creó el descuento '{descuento.Nombre}'.", descuento.Id);

        var conRelaciones = await _repository.GetByIdConRelacionesAsync(descuento.Id);
        return ToDto(conRelaciones!);
    }

    private static void AplicarAlcances(Descuento descuento, GuardarDescuentoDto dto)
    {
        descuento.Productos = dto.ProductoIds.Distinct().Select(id => new DescuentoProducto { ProductoId = id }).ToList();
        descuento.Categorias = dto.CategoriaIds.Distinct().Select(id => new DescuentoCategoria { CategoriaId = id }).ToList();
        descuento.Clientes = dto.ClienteIds.Distinct().Select(id => new DescuentoCliente { ClienteId = id }).ToList();
        descuento.Roles = dto.RolIds.Distinct().Select(id => new DescuentoRol { RolId = id }).ToList();
    }

    public async Task<DescuentoDto> UpdateAsync(int id, GuardarDescuentoDto dto)
    {
        var descuento = await _repository.GetByIdConRelacionesAsync(id)
            ?? throw new BusinessRuleException("El descuento no existe.");

        if (!Enum.TryParse<TipoDescuento>(dto.Tipo, true, out var tipo))
            throw new BusinessRuleException($"Tipo de descuento '{dto.Tipo}' no es válido.");

        ValidarComun(dto, tipo);

        string? codigoNormalizado = null;
        if (!string.IsNullOrWhiteSpace(dto.CodigoPromocional))
        {
            codigoNormalizado = dto.CodigoPromocional.Trim().ToUpperInvariant();
            if (await _repository.ExisteCodigoAsync(codigoNormalizado, id))
                throw new BusinessRuleException($"Ya existe un descuento con el código '{dto.CodigoPromocional}'.");
        }

        // No se recalculan ventas históricas: editar el descuento solo afecta
        // aplicaciones futuras (sección 11: "no recalcular ventas históricas").
        var anterior = new { descuento.Nombre, descuento.Valor, descuento.Tipo, descuento.Activo };

        descuento.Nombre = dto.Nombre.Trim();
        descuento.Descripcion = dto.Descripcion;
        descuento.CodigoPromocional = string.IsNullOrWhiteSpace(dto.CodigoPromocional) ? null : dto.CodigoPromocional.Trim();
        descuento.CodigoPromocionalNormalizado = codigoNormalizado;
        descuento.Tipo = tipo;
        descuento.Valor = dto.Valor;
        descuento.FechaInicio = dto.FechaInicio;
        descuento.FechaFin = dto.FechaFin;
        descuento.MontoMinimo = dto.MontoMinimo;
        descuento.MontoMaximoDescuento = dto.MontoMaximoDescuento;
        descuento.CantidadMinima = dto.CantidadMinima;
        descuento.RequiereAprobacion = dto.RequiereAprobacion;
        descuento.Acumulable = dto.Acumulable;
        descuento.Prioridad = dto.Prioridad;
        descuento.LimiteTotalUsos = dto.LimiteTotalUsos;
        descuento.LimiteUsosPorCliente = dto.LimiteUsosPorCliente;
        descuento.FechaActualizacion = DateTime.UtcNow;
        descuento.ActualizadoPorUsuarioId = _currentUser.UsuarioId;

        descuento.Productos.Clear();
        descuento.Categorias.Clear();
        descuento.Clientes.Clear();
        descuento.Roles.Clear();
        AplicarAlcances(descuento, dto);

        _repository.Update(descuento);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Descuentos, AccionPermiso.Editar, $"Editó el descuento '{descuento.Nombre}'.", descuento.Id,
            entidad: "Descuento", valoresAnteriores: anterior,
            valoresNuevos: new { descuento.Nombre, descuento.Valor, descuento.Tipo, descuento.Activo });

        var conRelaciones = await _repository.GetByIdConRelacionesAsync(id);
        return ToDto(conRelaciones!);
    }

    public async Task<DescuentoDto> ActivarAsync(int id)
    {
        var d = await _repository.GetByIdAsync(id) ?? throw new BusinessRuleException("El descuento no existe.");
        d.Activo = true;
        d.FechaActualizacion = DateTime.UtcNow;
        d.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        _repository.Update(d);
        await _repository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(ModuloSistema.Descuentos, AccionPermiso.Activar, $"Activó el descuento '{d.Nombre}'.", d.Id);
        return ToDto((await _repository.GetByIdConRelacionesAsync(id))!);
    }

    public async Task<DescuentoDto> DesactivarAsync(int id)
    {
        var d = await _repository.GetByIdAsync(id) ?? throw new BusinessRuleException("El descuento no existe.");
        d.Activo = false;
        d.FechaActualizacion = DateTime.UtcNow;
        d.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        _repository.Update(d);
        await _repository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(ModuloSistema.Descuentos, AccionPermiso.Desactivar, $"Desactivó el descuento '{d.Nombre}'.", d.Id);
        return ToDto((await _repository.GetByIdConRelacionesAsync(id))!);
    }

    public async Task EliminarLogicoAsync(int id)
    {
        var d = await _repository.GetByIdAsync(id) ?? throw new BusinessRuleException("El descuento no existe.");
        var usos = await _repository.ContarUsosAsync(id);
        if (usos > 0)
            throw new BusinessRuleException("No se puede eliminar un descuento que ya fue utilizado en ventas. Desactívalo en su lugar.");

        d.Eliminado = true;
        d.Activo = false;
        d.FechaEliminacion = DateTime.UtcNow;
        d.EliminadoPorUsuarioId = _currentUser.UsuarioId;
        _repository.Update(d);
        await _repository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(ModuloSistema.Descuentos, AccionPermiso.EliminarLogico, $"Eliminó (lógico) el descuento '{d.Nombre}'.", d.Id);
    }

    public async Task EliminarPermanenteAsync(int id)
    {
        var d = await _repository.GetByIdAsync(id) ?? throw new BusinessRuleException("El descuento no existe.");
        var usos = await _repository.ContarUsosAsync(id);
        if (usos > 0)
            throw new BusinessRuleException("No se puede eliminar permanentemente un descuento que ya fue utilizado (rompería el historial de ventas).");

        var nombre = d.Nombre;
        _repository.Remove(d);
        await _repository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(ModuloSistema.Descuentos, AccionPermiso.EliminarPermanente, $"Eliminó permanentemente el descuento '{nombre}'.", id);
    }

    public async Task<DescuentoDto> DuplicarAsync(int id, string nuevoNombre)
    {
        var original = await _repository.GetByIdConRelacionesAsync(id) ?? throw new BusinessRuleException("El descuento no existe.");

        var copia = new Descuento
        {
            Nombre = nuevoNombre.Trim(),
            Descripcion = original.Descripcion,
            Tipo = original.Tipo,
            Valor = original.Valor,
            FechaInicio = original.FechaInicio,
            FechaFin = original.FechaFin,
            MontoMinimo = original.MontoMinimo,
            MontoMaximoDescuento = original.MontoMaximoDescuento,
            CantidadMinima = original.CantidadMinima,
            RequiereAprobacion = original.RequiereAprobacion,
            Acumulable = original.Acumulable,
            Prioridad = original.Prioridad,
            LimiteTotalUsos = original.LimiteTotalUsos,
            LimiteUsosPorCliente = original.LimiteUsosPorCliente,
            Activo = true,
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            Productos = original.Productos.Select(p => new DescuentoProducto { ProductoId = p.ProductoId }).ToList(),
            Categorias = original.Categorias.Select(c => new DescuentoCategoria { CategoriaId = c.CategoriaId }).ToList(),
            Clientes = original.Clientes.Select(c => new DescuentoCliente { ClienteId = c.ClienteId }).ToList(),
            Roles = original.Roles.Select(r => new DescuentoRol { RolId = r.RolId }).ToList()
            // El código promocional NO se duplica (evita colisión); queda sin código.
        };

        await _repository.AddAsync(copia);
        await _repository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(ModuloSistema.Descuentos, AccionPermiso.Duplicar, $"Duplicó el descuento '{original.Nombre}' como '{copia.Nombre}'.", copia.Id);

        return ToDto((await _repository.GetByIdConRelacionesAsync(copia.Id))!);
    }

    private static DescuentoDto ToDto(Descuento d) => new()
    {
        Id = d.Id,
        Nombre = d.Nombre,
        Descripcion = d.Descripcion,
        CodigoPromocional = d.CodigoPromocional,
        Tipo = d.Tipo.ToString(),
        Valor = d.Valor,
        FechaInicio = d.FechaInicio,
        FechaFin = d.FechaFin,
        MontoMinimo = d.MontoMinimo,
        MontoMaximoDescuento = d.MontoMaximoDescuento,
        CantidadMinima = d.CantidadMinima,
        RequiereAprobacion = d.RequiereAprobacion,
        Acumulable = d.Acumulable,
        Prioridad = d.Prioridad,
        LimiteTotalUsos = d.LimiteTotalUsos,
        LimiteUsosPorCliente = d.LimiteUsosPorCliente,
        UsosRealizados = d.UsosRealizados,
        Activo = d.Activo,
        ProductoIds = d.Productos.Select(p => p.ProductoId).ToList(),
        CategoriaIds = d.Categorias.Select(c => c.CategoriaId).ToList(),
        ClienteIds = d.Clientes.Select(c => c.ClienteId).ToList(),
        RolIds = d.Roles.Select(r => r.RolId).ToList(),
        FechaCreacion = d.FechaCreacion,
        FechaActualizacion = d.FechaActualizacion
    };
}
