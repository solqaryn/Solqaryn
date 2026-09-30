using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

/// Módulo real y administrable de descuentos (sección 11). Reemplaza el campo
/// decimal manual que antes se enviaba directo desde Angular.
public class Descuento
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? CodigoPromocional { get; set; }
    public string? CodigoPromocionalNormalizado { get; set; }

    public TipoDescuento Tipo { get; set; }
    public decimal Valor { get; set; } // % (0-100) o monto fijo, según Tipo

    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }

    public decimal? MontoMinimo { get; set; }
    public decimal? MontoMaximoDescuento { get; set; }
    public int? CantidadMinima { get; set; }

    public bool RequiereAprobacion { get; set; }
    public bool Acumulable { get; set; }
    public int Prioridad { get; set; } = 100; // menor número = mayor prioridad

    public int? LimiteTotalUsos { get; set; }
    public int? LimiteUsosPorCliente { get; set; }
    public int UsosRealizados { get; set; }

    public bool Activo { get; set; } = true;
    public bool Eliminado { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }
    public DateTime? FechaEliminacion { get; set; }

    public int? CreadoPorUsuarioId { get; set; }
    public int? ActualizadoPorUsuarioId { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }

    public ICollection<DescuentoProducto> Productos { get; set; } = new List<DescuentoProducto>();
    public ICollection<DescuentoCategoria> Categorias { get; set; } = new List<DescuentoCategoria>();
    public ICollection<DescuentoCliente> Clientes { get; set; } = new List<DescuentoCliente>();
    public ICollection<DescuentoRol> Roles { get; set; } = new List<DescuentoRol>();
    public ICollection<HistorialUsoDescuento> Historial { get; set; } = new List<HistorialUsoDescuento>();
}

/// Alcance: Producto. Fila presente = el descuento aplica a ese producto.
/// Si un Descuento con AlcanceDescuento.Producto no tiene ninguna fila aquí,
/// no aplica a ningún producto (evita "global por accidente").
public class DescuentoProducto
{
    public int Id { get; set; }
    public int DescuentoId { get; set; }
    public int ProductoId { get; set; }
}

public class DescuentoCategoria
{
    public int Id { get; set; }
    public int DescuentoId { get; set; }
    public int CategoriaId { get; set; }
}

public class DescuentoCliente
{
    public int Id { get; set; }
    public int DescuentoId { get; set; }
    public int ClienteId { get; set; }
}

public class DescuentoRol
{
    public int Id { get; set; }
    public int DescuentoId { get; set; }
    public int RolId { get; set; }
}

/// Registro histórico inmutable de cada vez que un descuento se aplicó a una
/// venta. Nunca se recalcula ni se borra al editar el Descuento original
/// (sección 11: "conservar el valor histórico aplicado").
public class HistorialUsoDescuento
{
    public int Id { get; set; }
    public int DescuentoId { get; set; }
    public Descuento? Descuento { get; set; }

    public int VentaId { get; set; }
    public int? ClienteId { get; set; }

    public decimal MontoAplicado { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public int? UsuarioId { get; set; }
}

/// Snapshot del descuento aplicado a una venta específica (parte del desglose
/// histórico de la venta). Independiente de HistorialUsoDescuento, que es el
/// registro de auditoría de uso; VentaDescuento es lo que se muestra en el
/// detalle/factura de esa venta puntual.
public class VentaDescuento
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public int DescuentoId { get; set; }
    public string DescuentoNombreSnapshot { get; set; } = string.Empty;
    public string DescuentoCodigoSnapshot { get; set; } = string.Empty;
    public TipoDescuento TipoSnapshot { get; set; }
    public decimal ValorSnapshot { get; set; }
    public decimal MontoAplicado { get; set; }
}
