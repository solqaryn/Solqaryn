namespace Solqaryn.Application.DTOs;

public enum FacturaFormatoPdf
{
    A4,
    Carta,
    Legal,
    Oficio,
    A5,
    Pos58,
    Pos80
}

public sealed class FacturaFormatoPdfDto
{
    public string Codigo { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public decimal AnchoMm { get; init; }
    public decimal? AltoMm { get; init; }
    public bool EsContinuo { get; init; }
    public string UsoRecomendado { get; init; } = string.Empty;
}

public static class FacturaFormatoPdfCatalogo
{
    private static readonly IReadOnlyList<FacturaFormatoPdfDto> Formatos =
    [
        new() { Codigo = "a4", Nombre = "A4", Descripcion = "210 × 297 mm", AnchoMm = 210m, AltoMm = 297m, UsoRecomendado = "Impresoras convencionales y archivo digital" },
        new() { Codigo = "carta", Nombre = "Carta", Descripcion = "8.5 × 11 pulgadas", AnchoMm = 215.9m, AltoMm = 279.4m, UsoRecomendado = "Impresoras de oficina en Norteamérica y Centroamérica" },
        new() { Codigo = "legal", Nombre = "Legal", Descripcion = "8.5 × 14 pulgadas", AnchoMm = 215.9m, AltoMm = 355.6m, UsoRecomendado = "Documentos extensos en papel legal" },
        new() { Codigo = "oficio", Nombre = "Oficio", Descripcion = "8.5 × 13 pulgadas", AnchoMm = 215.9m, AltoMm = 330.2m, UsoRecomendado = "Impresoras configuradas para papel oficio" },
        new() { Codigo = "a5", Nombre = "A5", Descripcion = "148 × 210 mm", AnchoMm = 148m, AltoMm = 210m, UsoRecomendado = "Comprobantes compactos y archivadores pequeños" },
        new() { Codigo = "pos58", Nombre = "POS 58 mm", Descripcion = "Rollo continuo de 58 mm", AnchoMm = 58m, AltoMm = null, EsContinuo = true, UsoRecomendado = "Impresoras térmicas móviles y handheld" },
        new() { Codigo = "pos80", Nombre = "POS 80 mm", Descripcion = "Rollo continuo de 80 mm", AnchoMm = 80m, AltoMm = null, EsContinuo = true, UsoRecomendado = "Impresoras térmicas POS e industriales" }
    ];

    public static IReadOnlyList<FacturaFormatoPdfDto> ObtenerTodos() => Formatos;

    public static FacturaFormatoPdfDto Obtener(FacturaFormatoPdf formato) =>
        Formatos.First(x => x.Codigo == ObtenerCodigo(formato));

    public static string ObtenerCodigo(FacturaFormatoPdf formato) => formato switch
    {
        FacturaFormatoPdf.A4 => "a4",
        FacturaFormatoPdf.Carta => "carta",
        FacturaFormatoPdf.Legal => "legal",
        FacturaFormatoPdf.Oficio => "oficio",
        FacturaFormatoPdf.A5 => "a5",
        FacturaFormatoPdf.Pos58 => "pos58",
        FacturaFormatoPdf.Pos80 => "pos80",
        _ => "a4"
    };

    public static bool TryParse(string? valor, out FacturaFormatoPdf formato)
    {
        var normalizado = (valor ?? "a4")
            .Trim()
            .ToLowerInvariant()
            .Replace("-", string.Empty)
            .Replace("_", string.Empty)
            .Replace(" ", string.Empty);

        formato = normalizado switch
        {
            "a4" => FacturaFormatoPdf.A4,
            "carta" or "letter" => FacturaFormatoPdf.Carta,
            "legal" => FacturaFormatoPdf.Legal,
            "oficio" or "folio" => FacturaFormatoPdf.Oficio,
            "a5" => FacturaFormatoPdf.A5,
            "pos58" or "58mm" or "ticket58" => FacturaFormatoPdf.Pos58,
            "pos80" or "80mm" or "ticket80" => FacturaFormatoPdf.Pos80,
            _ => FacturaFormatoPdf.A4
        };

        return normalizado is "a4" or "carta" or "letter" or "legal" or "oficio" or "folio" or "a5" or "pos58" or "58mm" or "ticket58" or "pos80" or "80mm" or "ticket80";
    }
}
