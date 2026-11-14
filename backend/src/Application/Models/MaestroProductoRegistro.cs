using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Models;

/// <summary>
/// Proyección tipada de los maestros normalizados de producto. No es una entidad
/// persistente: cada registro vive exclusivamente en Marcas, Modelos, Colores o Tallas.
/// </summary>
public sealed class MaestroProductoRegistro
{
    public int Id { get; set; }
    public TipoCatalogoProducto Tipo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? CodigoVisual { get; set; }
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
    public bool Eliminado { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }
    public int? CatalogoPadreId { get; set; }
    public string? CatalogoPadreNombre { get; set; }
    public bool? CatalogoPadreActivo { get; set; }
    public int TotalProductos { get; set; }
    public int TotalModelos { get; set; }
    public int TotalModelosActivos { get; set; }
    public int? CreadoPorUsuarioId { get; set; }
    public string? CreadoPorNombreUsuario { get; set; }
    public int? ActualizadoPorUsuarioId { get; set; }
    public string? ActualizadoPorNombreUsuario { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
}
