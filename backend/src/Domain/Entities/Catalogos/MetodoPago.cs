using System.Text;
using System.Text.Json;
using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities.Catalogos;

/// <summary>
/// Catálogo relacional y administrable de métodos de pago.
/// Convive temporalmente con Domain.Enums.MetodoPago durante la migración ERP-N0.
/// </summary>
public class MetodoPago : AuditableEntity
{
    private int _orden;
    private string? _metadata;

    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public bool RequiereReferencia { get; set; }
    public bool RequiereBanco { get; set; }
    public bool PermiteCambio { get; set; }

    /// <summary>
    /// Prioridad de presentación. Cero es válido; los valores negativos no lo son.
    /// </summary>
    public int Orden
    {
        get => _orden;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "El orden del método de pago no puede ser negativo.");
            _orden = value;
        }
    }

    /// <summary>
    /// Objeto JSON opcional con configuración extensible. Se almacena canonizado
    /// para que equivalentes semánticos no produzcan representaciones distintas.
    /// </summary>
    public string? Metadata
    {
        get => _metadata;
        set => _metadata = CanonicalizarMetadata(value);
    }

    public bool Eliminado { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }

    /// <summary>
    /// Columna calculada usada para garantizar unicidad estable de Codigo
    /// sin depender de mayúsculas, minúsculas o espacios periféricos.
    /// </summary>
    public string? CodigoNormalizado { get; private set; }

    /// <summary>
    /// Orden único de selección/listado: Orden, código normalizado e Id.
    /// El Id resuelve de forma estable incluso registros legacy con código equivalente.
    /// </summary>
    public static IReadOnlyList<MetodoPago> OrdenarParaSeleccion(IEnumerable<MetodoPago> metodos) =>
        metodos
            .OrderBy(x => x.Orden)
            .ThenBy(x => NormalizarCodigo(x.Codigo), StringComparer.Ordinal)
            .ThenBy(x => x.Id)
            .ToList();

    public static string? CanonicalizarMetadata(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
            return null;

        using var document = JsonDocument.Parse(metadata);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("La metadata del método de pago debe ser un objeto JSON.", nameof(metadata));

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
            EscribirCanonico(writer, document.RootElement);

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void EscribirCanonico(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    EscribirCanonico(writer, property.Value);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                    EscribirCanonico(writer, item);
                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }

    private static string NormalizarCodigo(string? codigo) =>
        (codigo ?? string.Empty).Trim().ToLowerInvariant();
}
