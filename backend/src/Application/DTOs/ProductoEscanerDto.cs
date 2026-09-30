namespace Solqaryn.Application.DTOs;

public enum EstadoResolucionProductoEscaner
{
    Encontrado,
    EntradaInvalida,
    NoEncontrado,
    Conflicto,
    NoOperativo
}

public sealed class ResultadoResolucionProductoEscaner<T> where T : class
{
    public EstadoResolucionProductoEscaner Estado { get; init; }
    public T? Dato { get; init; }
    public string Mensaje { get; init; } = string.Empty;

    public static ResultadoResolucionProductoEscaner<T> Encontrado(T dato) =>
        new() { Estado = EstadoResolucionProductoEscaner.Encontrado, Dato = dato };

    public static ResultadoResolucionProductoEscaner<T> Fallo(
        EstadoResolucionProductoEscaner estado,
        string mensaje) =>
        new() { Estado = estado, Mensaje = mensaje };
}

public abstract class ProductoEscaneadoBaseDto
{
    public int ProductoId { get; init; }
    public int ProductoVarianteId { get; init; }
    public string ProductoNombre { get; init; } = string.Empty;
    public string Marca { get; init; } = string.Empty;
    public string Modelo { get; init; } = string.Empty;
    public int? MarcaId { get; init; }
    public string? MarcaNombre { get; init; }
    public int? ModeloId { get; init; }
    public string? ModeloNombre { get; init; }
    public int? ColorId { get; init; }
    public string? ColorNombre { get; init; }
    public int? TallaId { get; init; }
    public string? TallaNombre { get; init; }
    public string Etiqueta { get; init; } = string.Empty;
    public bool EsVarianteTecnica { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string? CodigoBarras { get; init; }
    public int CantidadDisponible { get; init; }
    public decimal Precio { get; init; }
    public string? ImagenMiniaturaUrl { get; init; }
}

public sealed class ProductoEscaneadoVentaDto : ProductoEscaneadoBaseDto
{
}

public sealed class ProductoEscaneadoCompraDto : ProductoEscaneadoBaseDto
{
    public decimal Costo { get; init; }
}