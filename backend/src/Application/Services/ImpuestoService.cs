using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public class ImpuestoService : IImpuestoService
{
    private readonly IImpuestoRepository _repository;
    private readonly IAuditoriaService _auditoria;
    private readonly ICurrentUserService _currentUser;

    public ImpuestoService(IImpuestoRepository repository, IAuditoriaService auditoria, ICurrentUserService currentUser)
    {
        _repository = repository;
        _auditoria = auditoria;
        _currentUser = currentUser;
    }

    public async Task<List<ImpuestoDto>> GetAllAsync(bool incluirEliminados = false)
    {
        var lista = await _repository.GetAllAsync(incluirEliminados);
        var resultado = new List<ImpuestoDto>();
        foreach (var i in lista)
        {
            var conRelaciones = await _repository.GetByIdConRelacionesAsync(i.Id);
            if (conRelaciones is not null) resultado.Add(ToDto(conRelaciones));
        }
        return resultado;
    }

    public async Task<ImpuestoDto?> GetByIdAsync(int id)
    {
        var i = await _repository.GetByIdConRelacionesAsync(id);
        return i is null ? null : ToDto(i);
    }

    private static List<OperacionImpuesto> ParseOperaciones(List<string> valores)
    {
        var resultado = new List<OperacionImpuesto>();
        foreach (var v in valores)
        {
            if (!Enum.TryParse<OperacionImpuesto>(v, true, out var op))
                throw new BusinessRuleException($"Operación '{v}' no es válida (use Venta o Compra).");
            resultado.Add(op);
        }
        if (resultado.Count == 0)
            throw new BusinessRuleException("Debe indicar al menos una operación (Venta y/o Compra) para el impuesto.");
        return resultado;
    }

    private static void ValidarComun(GuardarImpuestoDto dto, TipoImpuesto tipo)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            throw new BusinessRuleException("El nombre del impuesto es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Codigo))
            throw new BusinessRuleException("El código del impuesto es obligatorio.");
        if (dto.Tasa < 0)
            throw new BusinessRuleException("La tasa del impuesto no puede ser negativa.");
        if (tipo == TipoImpuesto.MontoFijo && (dto.MontoFijo is null || dto.MontoFijo < 0))
            throw new BusinessRuleException("Un impuesto de monto fijo requiere un monto fijo mayor o igual a 0.");
        if (dto.FechaInicio.HasValue && dto.FechaFin.HasValue && dto.FechaFin < dto.FechaInicio)
            throw new BusinessRuleException("La fecha final no puede ser anterior a la fecha de inicio.");
    }

    public async Task<ImpuestoDto> CreateAsync(GuardarImpuestoDto dto)
    {
        if (!Enum.TryParse<TipoImpuesto>(dto.Tipo, true, out var tipo))
            throw new BusinessRuleException($"Tipo de impuesto '{dto.Tipo}' no es válido.");

        ValidarComun(dto, tipo);
        var operaciones = ParseOperaciones(dto.Operaciones);

        var codigo = dto.Codigo.Trim().ToUpperInvariant();
        if (await _repository.ExisteCodigoAsync(codigo))
            throw new BusinessRuleException($"Ya existe un impuesto con el código '{dto.Codigo}'.");

        var impuesto = new Impuesto
        {
            Nombre = dto.Nombre.Trim(),
            Codigo = codigo,
            Descripcion = dto.Descripcion,
            Tipo = tipo,
            Tasa = dto.Tasa,
            MontoFijo = dto.MontoFijo,
            FechaInicio = dto.FechaInicio,
            FechaFin = dto.FechaFin,
            IncluidoEnPrecio = dto.IncluidoEnPrecio,
            SeCalculaAntesDescuento = dto.SeCalculaAntesDescuento,
            Acumulativo = dto.Acumulativo,
            Prioridad = dto.Prioridad,
            RequiereRetencion = dto.RequiereRetencion,
            Activo = true,
            CreadoPorUsuarioId = _currentUser.UsuarioId
        };

        AplicarAlcances(impuesto, dto, operaciones);

        await _repository.AddAsync(impuesto);
        await _repository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(ModuloSistema.Impuestos, AccionPermiso.Crear, $"Creó el impuesto '{impuesto.Nombre}'.", impuesto.Id);

        return ToDto((await _repository.GetByIdConRelacionesAsync(impuesto.Id))!);
    }

    private static void AplicarAlcances(Impuesto impuesto, GuardarImpuestoDto dto, List<OperacionImpuesto> operaciones)
    {
        impuesto.Productos = dto.ProductoIds.Distinct().Select(id => new ImpuestoProducto { ProductoId = id }).ToList();
        impuesto.Categorias = dto.CategoriaIds.Distinct().Select(id => new ImpuestoCategoria { CategoriaId = id }).ToList();
        impuesto.Operaciones = operaciones.Distinct().Select(op => new ImpuestoOperacion { Operacion = op }).ToList();
        impuesto.ClientesExentos = dto.ClienteExentoIds.Distinct().Select(id => new ImpuestoCliente { ClienteId = id }).ToList();
        impuesto.ProveedoresExentos = dto.ProveedorExentoIds.Distinct().Select(id => new ImpuestoProveedor { ProveedorId = id }).ToList();
    }

    public async Task<ImpuestoDto> UpdateAsync(int id, GuardarImpuestoDto dto)
    {
        var impuesto = await _repository.GetByIdConRelacionesAsync(id) ?? throw new BusinessRuleException("El impuesto no existe.");

        if (!Enum.TryParse<TipoImpuesto>(dto.Tipo, true, out var tipo))
            throw new BusinessRuleException($"Tipo de impuesto '{dto.Tipo}' no es válido.");

        ValidarComun(dto, tipo);
        var operaciones = ParseOperaciones(dto.Operaciones);

        var codigo = dto.Codigo.Trim().ToUpperInvariant();
        if (!string.Equals(codigo, impuesto.Codigo, StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleException("El código del impuesto es su identidad fiscal estable y no puede modificarse después de crearlo.");

        // No se recalculan documentos históricos: editar el impuesto solo afecta
        // aplicaciones futuras (sección 12).
        impuesto.Nombre = dto.Nombre.Trim();
        impuesto.Descripcion = dto.Descripcion;
        impuesto.Tipo = tipo;
        impuesto.Tasa = dto.Tasa;
        impuesto.MontoFijo = dto.MontoFijo;
        impuesto.FechaInicio = dto.FechaInicio;
        impuesto.FechaFin = dto.FechaFin;
        impuesto.IncluidoEnPrecio = dto.IncluidoEnPrecio;
        impuesto.SeCalculaAntesDescuento = dto.SeCalculaAntesDescuento;
        impuesto.Acumulativo = dto.Acumulativo;
        impuesto.Prioridad = dto.Prioridad;
        impuesto.RequiereRetencion = dto.RequiereRetencion;
        impuesto.FechaActualizacion = DateTime.UtcNow;
        impuesto.ActualizadoPorUsuarioId = _currentUser.UsuarioId;

        impuesto.Productos.Clear();
        impuesto.Categorias.Clear();
        impuesto.Operaciones.Clear();
        impuesto.ClientesExentos.Clear();
        impuesto.ProveedoresExentos.Clear();
        AplicarAlcances(impuesto, dto, operaciones);

        _repository.Update(impuesto);
        await _repository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(ModuloSistema.Impuestos, AccionPermiso.Editar, $"Editó el impuesto '{impuesto.Nombre}'.", impuesto.Id);

        return ToDto((await _repository.GetByIdConRelacionesAsync(id))!);
    }

    public async Task<ImpuestoDto> ActivarAsync(int id)
    {
        var i = await _repository.GetByIdAsync(id) ?? throw new BusinessRuleException("El impuesto no existe.");
        i.Activo = true;
        i.FechaActualizacion = DateTime.UtcNow;
        i.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        _repository.Update(i);
        await _repository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(ModuloSistema.Impuestos, AccionPermiso.Activar, $"Activó el impuesto '{i.Nombre}'.", i.Id);
        return ToDto((await _repository.GetByIdConRelacionesAsync(id))!);
    }

    public async Task<ImpuestoDto> DesactivarAsync(int id)
    {
        var i = await _repository.GetByIdAsync(id) ?? throw new BusinessRuleException("El impuesto no existe.");
        i.Activo = false;
        i.FechaActualizacion = DateTime.UtcNow;
        i.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        _repository.Update(i);
        await _repository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(ModuloSistema.Impuestos, AccionPermiso.Desactivar, $"Desactivó el impuesto '{i.Nombre}'.", i.Id);
        return ToDto((await _repository.GetByIdConRelacionesAsync(id))!);
    }

    public async Task EliminarLogicoAsync(int id)
    {
        var i = await _repository.GetByIdAsync(id) ?? throw new BusinessRuleException("El impuesto no existe.");
        var usos = await _repository.ContarAplicacionesAsync(id);
        if (usos > 0)
            throw new BusinessRuleException("No se puede eliminar un impuesto que ya fue aplicado en documentos. Desactívalo en su lugar.");

        i.Eliminado = true;
        i.Activo = false;
        i.FechaEliminacion = DateTime.UtcNow;
        i.EliminadoPorUsuarioId = _currentUser.UsuarioId;
        _repository.Update(i);
        await _repository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(ModuloSistema.Impuestos, AccionPermiso.EliminarLogico, $"Eliminó (lógico) el impuesto '{i.Nombre}'.", i.Id);
    }

    public async Task EliminarPermanenteAsync(int id)
    {
        var i = await _repository.GetByIdAsync(id) ?? throw new BusinessRuleException("El impuesto no existe.");
        var usos = await _repository.ContarAplicacionesAsync(id);
        if (usos > 0)
            throw new BusinessRuleException("No se puede eliminar permanentemente un impuesto ya aplicado (rompería el historial de documentos).");

        var nombre = i.Nombre;
        _repository.Remove(i);
        await _repository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(ModuloSistema.Impuestos, AccionPermiso.EliminarPermanente, $"Eliminó permanentemente el impuesto '{nombre}'.", id);
    }

    public async Task<ImpuestoDto> DuplicarAsync(int id, string nuevoNombre, string nuevoCodigo)
    {
        var original = await _repository.GetByIdConRelacionesAsync(id) ?? throw new BusinessRuleException("El impuesto no existe.");

        var codigo = nuevoCodigo.Trim().ToUpperInvariant();
        if (await _repository.ExisteCodigoAsync(codigo))
            throw new BusinessRuleException($"Ya existe un impuesto con el código '{nuevoCodigo}'.");

        var copia = new Impuesto
        {
            Nombre = nuevoNombre.Trim(),
            Codigo = codigo,
            Descripcion = original.Descripcion,
            Tipo = original.Tipo,
            Tasa = original.Tasa,
            MontoFijo = original.MontoFijo,
            FechaInicio = original.FechaInicio,
            FechaFin = original.FechaFin,
            IncluidoEnPrecio = original.IncluidoEnPrecio,
            SeCalculaAntesDescuento = original.SeCalculaAntesDescuento,
            Acumulativo = original.Acumulativo,
            Prioridad = original.Prioridad,
            RequiereRetencion = original.RequiereRetencion,
            Activo = true,
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            Productos = original.Productos.Select(p => new ImpuestoProducto { ProductoId = p.ProductoId }).ToList(),
            Categorias = original.Categorias.Select(c => new ImpuestoCategoria { CategoriaId = c.CategoriaId }).ToList(),
            Operaciones = original.Operaciones.Select(o => new ImpuestoOperacion { Operacion = o.Operacion }).ToList(),
            ClientesExentos = original.ClientesExentos.Select(c => new ImpuestoCliente { ClienteId = c.ClienteId }).ToList(),
            ProveedoresExentos = original.ProveedoresExentos.Select(p => new ImpuestoProveedor { ProveedorId = p.ProveedorId }).ToList()
        };

        await _repository.AddAsync(copia);
        await _repository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(ModuloSistema.Impuestos, AccionPermiso.Duplicar, $"Duplicó el impuesto '{original.Nombre}' como '{copia.Nombre}'.", copia.Id);

        return ToDto((await _repository.GetByIdConRelacionesAsync(copia.Id))!);
    }

    private static ImpuestoDto ToDto(Impuesto i) => new()
    {
        Id = i.Id,
        Nombre = i.Nombre,
        Codigo = i.Codigo,
        Descripcion = i.Descripcion,
        Tipo = i.Tipo.ToString(),
        Tasa = i.Tasa,
        MontoFijo = i.MontoFijo,
        FechaInicio = i.FechaInicio,
        FechaFin = i.FechaFin,
        IncluidoEnPrecio = i.IncluidoEnPrecio,
        SeCalculaAntesDescuento = i.SeCalculaAntesDescuento,
        Acumulativo = i.Acumulativo,
        Prioridad = i.Prioridad,
        RequiereRetencion = i.RequiereRetencion,
        Activo = i.Activo,
        ProductoIds = i.Productos.Select(p => p.ProductoId).ToList(),
        CategoriaIds = i.Categorias.Select(c => c.CategoriaId).ToList(),
        Operaciones = i.Operaciones.Select(o => o.Operacion.ToString()).ToList(),
        ClienteExentoIds = i.ClientesExentos.Select(c => c.ClienteId).ToList(),
        ProveedorExentoIds = i.ProveedoresExentos.Select(p => p.ProveedorId).ToList(),
        FechaCreacion = i.FechaCreacion,
        FechaActualizacion = i.FechaActualizacion
    };
}
