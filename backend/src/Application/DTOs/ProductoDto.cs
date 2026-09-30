using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.DTOs;

public class ProductoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public TipoInventario TipoInventario { get; set; }
    public int Cantidad { get; set; }
    public decimal Costo { get; set; }
    public decimal Precio { get; set; }
    public decimal PrecioMinimo { get; set; }
    public decimal PrecioMaximo { get; set; }
    public int UmbralStockBajo { get; set; }
    public bool TieneStockBajo { get; set; }
    public bool EstaAgotado { get; set; }
    public string EstadoInventario { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public bool EsDestacado { get; set; }

    public int? CategoriaId { get; set; }
    public string? CategoriaNombre { get; set; }

    public int? ColorId { get; set; }
    public string? ColorNombre { get; set; }
    public string? ColorCodigoVisual { get; set; }
    public int? TallaId { get; set; }
    public string? TallaNombre { get; set; }
    public int? MarcaId { get; set; }
    public string? MarcaNombre { get; set; }
    public int? ModeloId { get; set; }
    public string? ModeloNombre { get; set; }

    public string? ImagenPrincipalUrl { get; set; }
    public List<ProductoImagenDto> Imagenes { get; set; } = new();
    public int TotalImagenes { get; set; }

    public List<ProductoVarianteDto> Variantes { get; set; } = new();
    public int TotalVariantes { get; set; }
    public bool UsaVariantes => TotalVariantes > 0;

    public int? CreadoPorUsuarioId { get; set; }
    public string? CreadoPorNombreUsuario { get; set; }
    public int? ActualizadoPorUsuarioId { get; set; }
    public string? ActualizadoPorNombreUsuario { get; set; }

    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
}
