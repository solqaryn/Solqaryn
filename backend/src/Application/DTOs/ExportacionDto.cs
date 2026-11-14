namespace Solqaryn.Application.DTOs;

/// <summary>
/// Formatos de exportación soportados por los reportes administrativos.
/// El conjunto es cerrado para impedir que tokens desconocidos se propaguen
/// hacia infraestructura o respuestas HTTP.
/// </summary>
public enum FormatoExportacion
{
    Csv,
    Xlsx,
    Pdf
}

/// <summary>
/// Contrato canónico entre el token recibido, el formato tipado y sus metadatos HTTP/archivo.
/// </summary>
public sealed record FormatoExportacionContrato(
    FormatoExportacion Formato,
    string Token,
    string ContentType,
    string Extension)
{
    public static readonly FormatoExportacionContrato Csv = new(
        FormatoExportacion.Csv,
        "csv",
        "text/csv; charset=utf-8",
        ".csv");

    public static readonly FormatoExportacionContrato Xlsx = new(
        FormatoExportacion.Xlsx,
        "xlsx",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".xlsx");

    public static readonly FormatoExportacionContrato Pdf = new(
        FormatoExportacion.Pdf,
        "pdf",
        "application/pdf",
        ".pdf");

    public static bool TryParse(string? valor, out FormatoExportacionContrato contrato)
    {
        contrato = null!;
        if (string.IsNullOrWhiteSpace(valor))
            return false;

        contrato = valor.Trim().ToLowerInvariant() switch
        {
            "csv" => Csv,
            "xlsx" => Xlsx,
            "pdf" => Pdf,
            _ => null!
        };

        return contrato is not null;
    }

    public static FormatoExportacionContrato Parse(string? valor)
    {
        if (TryParse(valor, out var contrato))
            return contrato;

        throw new ArgumentException(
            "Formato de exportación no soportado. Use csv, xlsx o pdf.",
            nameof(valor));
    }
}
