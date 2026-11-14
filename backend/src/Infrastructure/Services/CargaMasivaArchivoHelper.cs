using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Infrastructure.Services;

internal static class CargaMasivaArchivoHelper
{
    internal const int MaximoBytes = 5 * 1024 * 1024;
    internal const int MaximoFilas = 2000;
    internal static readonly string[] ExtensionesPermitidas = [".csv", ".xlsx"];

    private static readonly Dictionary<TipoCargaMasiva, string[]> Columnas = new()
    {
        [TipoCargaMasiva.Clientes] = ["Nombre", "Telefono", "IdentidadORTN", "Correo", "Direccion", "TipoCliente", "Activo"],
        [TipoCargaMasiva.Proveedores] = ["Nombre", "Telefono", "Documento", "Correo", "Direccion", "Activo"],
        [TipoCargaMasiva.Colores] = ["Nombre", "CodigoVisual", "Descripcion", "Orden", "Activo"],
        [TipoCargaMasiva.Productos] = ["Nombre", "Marca", "Modelo", "Categoria", "Talla", "Descripcion", "Costo", "Precio", "UmbralStockBajo", "Activo"],
        [TipoCargaMasiva.VariantesInventario] = ["Producto", "Marca", "Modelo", "Color", "Talla", "SKU", "CodigoBarras", "Cantidad", "UmbralStockBajo", "Costo", "Precio", "Activo"]
    };

    private static readonly Dictionary<TipoCargaMasiva, string[]> Ejemplos = new()
    {
        [TipoCargaMasiva.Clientes] = ["Cliente de ejemplo", "9999-9999", "0801-1990-00001", "cliente@ejemplo.test", "Tegucigalpa", "Sin clasificar", "Si"],
        [TipoCargaMasiva.Proveedores] = ["Proveedor de ejemplo", "2222-2222", "08019000000000", "proveedor@ejemplo.test", "Tegucigalpa", "Si"],
        [TipoCargaMasiva.Colores] = ["Azul", "#2563EB", "Color azul", "10", "Si"],
        [TipoCargaMasiva.Productos] = ["Cobertor premium", "Samsung", "Galaxy S24 Ultra", "Fundas", "", "Cobertor de protección", "100.00", "220.00", "5", "Si"],
        [TipoCargaMasiva.VariantesInventario] = ["Cobertor premium", "Samsung", "Galaxy S24 Ultra", "Azul", "XL", "", "", "10", "2", "100.00", "220.00", "Si"]
    };

    internal static string[] ObtenerColumnas(TipoCargaMasiva tipo) =>
        Columnas.TryGetValue(tipo, out var columnas) ? columnas : throw new ArgumentOutOfRangeException(nameof(tipo));

    internal static string NombreAmigable(TipoCargaMasiva tipo) => tipo switch
    {
        TipoCargaMasiva.Clientes => "Clientes",
        TipoCargaMasiva.Proveedores => "Proveedores",
        TipoCargaMasiva.Colores => "Colores",
        TipoCargaMasiva.Productos => "Productos",
        TipoCargaMasiva.VariantesInventario => "Variantes e inventario inicial",
        _ => tipo.ToString()
    };

    internal static string Descripcion(TipoCargaMasiva tipo) => tipo switch
    {
        TipoCargaMasiva.Clientes => "Crear o actualizar clientes por identidad, correo, teléfono o nombre.",
        TipoCargaMasiva.Proveedores => "Crear o actualizar proveedores por documento, correo, teléfono o nombre.",
        TipoCargaMasiva.Colores => "Crear o actualizar el catálogo de colores.",
        TipoCargaMasiva.Productos => "Crear o actualizar productos. Marca y modelo deben existir previamente.",
        TipoCargaMasiva.VariantesInventario => "Crear o actualizar variantes exactas por producto + marca + modelo + color + talla, con SKU, código de barras e inventario controlado.",
        _ => string.Empty
    };

    internal static ArchivoDescargableDto CrearPlantilla(TipoCargaMasiva tipo, string formato)
    {
        var columnas = ObtenerColumnas(tipo);
        var ejemplo = Ejemplos[tipo];
        var nombreBase = $"plantilla-{tipo.ToString().ToLowerInvariant()}";

        if (formato.Equals("csv", StringComparison.OrdinalIgnoreCase))
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(',', columnas.Select(EscaparCsv)));
            sb.AppendLine(string.Join(',', ejemplo.Select(EscaparCsv)));
            return new ArchivoDescargableDto(
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(sb.ToString()),
                "text/csv; charset=utf-8",
                $"{nombreBase}.csv");
        }

        if (!formato.Equals("xlsx", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("El formato debe ser csv o xlsx.", nameof(formato));

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Datos");
        for (var i = 0; i < columnas.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = columnas[i];
            worksheet.Cell(2, i + 1).Value = ejemplo[i];
        }

        var encabezado = worksheet.Range(1, 1, 1, columnas.Length);
        encabezado.Style.Font.Bold = true;
        encabezado.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F6CBD");
        encabezado.Style.Font.FontColor = XLColor.White;
        encabezado.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns().AdjustToContents(12, 42);
        worksheet.Range(1, 1, 2, columnas.Length).SetAutoFilter();

        var activoIndex = Array.FindIndex(columnas, c => c.Equals("Activo", StringComparison.OrdinalIgnoreCase));
        if (activoIndex >= 0)
        {
            var validation = worksheet.Range(2, activoIndex + 1, MaximoFilas + 1, activoIndex + 1).CreateDataValidation();
            validation.List("Si,No", true);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return new ArchivoDescargableDto(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"{nombreBase}.xlsx");
    }

    internal static async Task<ArchivoLeido> LeerAsync(
        string extension,
        Stream contenido,
        CancellationToken cancellationToken)
    {
        if (extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
            return await LeerCsvAsync(contenido, cancellationToken);
        if (extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            return LeerXlsx(contenido);
        throw new InvalidOperationException("Extensión de archivo no permitida.");
    }

    internal static ArchivoDescargableDto CrearReporteErrores(
        IEnumerable<CargaMasivaErrorDto> errores,
        int cargaId,
        string formato)
    {
        var lista = errores.OrderBy(x => x.NumeroFila).ThenBy(x => x.Campo).ToList();
        if (formato.Equals("csv", StringComparison.OrdinalIgnoreCase))
        {
            var sb = new StringBuilder();
            sb.AppendLine("Fila,Tipo,Campo,Codigo,Mensaje,ValorOriginal");
            foreach (var error in lista)
            {
                var valores = new[]
                {
                    error.NumeroFila.ToString(CultureInfo.InvariantCulture),
                    error.EsAdvertencia ? "Advertencia" : "Error",
                    error.Campo ?? string.Empty,
                    error.Codigo,
                    error.Mensaje,
                    ProtegerFormula(error.ValorOriginal)
                };
                sb.AppendLine(string.Join(',', valores.Select(EscaparCsv)));
            }

            return new ArchivoDescargableDto(
                new UTF8Encoding(true).GetBytes(sb.ToString()),
                "text/csv; charset=utf-8",
                $"carga-{cargaId}-errores.csv");
        }

        if (!formato.Equals("xlsx", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("El formato debe ser csv o xlsx.", nameof(formato));

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Errores");
        var columnas = new[] { "Fila", "Tipo", "Campo", "Codigo", "Mensaje", "ValorOriginal" };
        for (var i = 0; i < columnas.Length; i++) worksheet.Cell(1, i + 1).Value = columnas[i];
        for (var i = 0; i < lista.Count; i++)
        {
            var error = lista[i];
            worksheet.Cell(i + 2, 1).Value = error.NumeroFila;
            worksheet.Cell(i + 2, 2).Value = error.EsAdvertencia ? "Advertencia" : "Error";
            worksheet.Cell(i + 2, 3).Value = error.Campo ?? string.Empty;
            worksheet.Cell(i + 2, 4).Value = error.Codigo;
            worksheet.Cell(i + 2, 5).Value = error.Mensaje;
            worksheet.Cell(i + 2, 6).Value = ProtegerFormula(error.ValorOriginal);
        }

        var header = worksheet.Range(1, 1, 1, columnas.Length);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#B42318");
        header.Style.Font.FontColor = XLColor.White;
        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns().AdjustToContents(12, 70);
        worksheet.RangeUsed()?.SetAutoFilter();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return new ArchivoDescargableDto(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"carga-{cargaId}-errores.xlsx");
    }

    private static async Task<ArchivoLeido> LeerCsvAsync(Stream contenido, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(contenido, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var texto = await reader.ReadToEndAsync(cancellationToken);
        var registros = ParsearCsv(texto);
        if (registros.Count == 0) return new ArchivoLeido();

        var cabeceras = registros[0].Select(NormalizarCabecera).ToList();
        var resultado = new ArchivoLeido { Cabeceras = cabeceras };
        for (var i = 1; i < registros.Count; i++)
        {
            var valores = registros[i];
            if (valores.All(string.IsNullOrWhiteSpace)) continue;
            if (resultado.Filas.Count >= MaximoFilas)
            {
                resultado.Problemas.Add(new ProblemaArchivo(i + 1, null, "MAXIMO_FILAS", $"El archivo supera el máximo de {MaximoFilas} filas.", null, false));
                break;
            }

            var fila = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (var c = 0; c < cabeceras.Count; c++)
            {
                var valor = c < valores.Count ? valores[c].Trim() : string.Empty;
                if (EsFormulaPeligrosa(valor))
                    resultado.Problemas.Add(new ProblemaArchivo(i + 1, cabeceras[c], "FORMULA_NO_PERMITIDA", "No se permiten fórmulas ni valores ejecutables.", valor, false));
                fila[cabeceras[c]] = valor;
            }
            resultado.Filas.Add(fila);
        }
        return resultado;
    }

    private static ArchivoLeido LeerXlsx(Stream contenido)
    {
        using var workbook = new XLWorkbook(contenido);
        var worksheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidOperationException("El archivo XLSX no contiene hojas.");
        var firstRow = worksheet.FirstRowUsed();
        var lastRow = worksheet.LastRowUsed();
        if (firstRow is null || lastRow is null) return new ArchivoLeido();

        var lastColumn = firstRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
        if (lastColumn == 0) return new ArchivoLeido();
        if (lastColumn > 50) throw new InvalidOperationException("El archivo contiene demasiadas columnas.");

        var cabeceras = Enumerable.Range(1, lastColumn)
            .Select(c => NormalizarCabecera(firstRow.Cell(c).GetFormattedString()))
            .ToList();
        var resultado = new ArchivoLeido { Cabeceras = cabeceras };

        for (var rowNumber = firstRow.RowNumber() + 1; rowNumber <= lastRow.RowNumber(); rowNumber++)
        {
            var row = worksheet.Row(rowNumber);
            if (row.Cells(1, lastColumn).All(c => c.IsEmpty())) continue;
            if (resultado.Filas.Count >= MaximoFilas)
            {
                resultado.Problemas.Add(new ProblemaArchivo(rowNumber, null, "MAXIMO_FILAS", $"El archivo supera el máximo de {MaximoFilas} filas.", null, false));
                break;
            }

            var fila = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (var c = 1; c <= lastColumn; c++)
            {
                var cell = row.Cell(c);
                var valor = cell.GetFormattedString().Trim();
                if (cell.HasFormula || EsFormulaPeligrosa(valor))
                    resultado.Problemas.Add(new ProblemaArchivo(rowNumber, cabeceras[c - 1], "FORMULA_NO_PERMITIDA", "No se permiten fórmulas en archivos de importación.", valor, false));
                fila[cabeceras[c - 1]] = valor;
            }
            resultado.Filas.Add(fila);
        }

        return resultado;
    }

    private static List<List<string>> ParsearCsv(string texto)
    {
        var registros = new List<List<string>>();
        var fila = new List<string>();
        var campo = new StringBuilder();
        var entreComillas = false;

        for (var i = 0; i < texto.Length; i++)
        {
            var ch = texto[i];
            if (entreComillas)
            {
                if (ch == '"' && i + 1 < texto.Length && texto[i + 1] == '"')
                {
                    campo.Append('"');
                    i++;
                }
                else if (ch == '"') entreComillas = false;
                else campo.Append(ch);
                continue;
            }

            if (ch == '"') entreComillas = true;
            else if (ch == ',')
            {
                fila.Add(campo.ToString());
                campo.Clear();
            }
            else if (ch == '\r' || ch == '\n')
            {
                if (ch == '\r' && i + 1 < texto.Length && texto[i + 1] == '\n') i++;
                fila.Add(campo.ToString());
                campo.Clear();
                registros.Add(fila);
                fila = new List<string>();
            }
            else campo.Append(ch);
        }

        if (campo.Length > 0 || fila.Count > 0)
        {
            fila.Add(campo.ToString());
            registros.Add(fila);
        }
        return registros;
    }

    internal static string NormalizarCabecera(string valor)
    {
        var normalized = valor.Trim().Normalize(NormalizationForm.FormD);
        var sinAcentos = new string(normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
        return new string(sinAcentos.Where(char.IsLetterOrDigit).ToArray());
    }

    private static bool EsFormulaPeligrosa(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return false;
        var trimmed = valor.TrimStart();
        if (trimmed.StartsWith('=') || trimmed.StartsWith('@') || trimmed.StartsWith('+')) return true;
        return trimmed.StartsWith('-') && (trimmed.Length == 1 || !char.IsDigit(trimmed[1]));
    }

    private static string ProtegerFormula(string? valor)
    {
        if (string.IsNullOrEmpty(valor)) return string.Empty;
        return EsFormulaPeligrosa(valor) ? $"'{valor}" : valor;
    }

    private static string EscaparCsv(string? valor)
    {
        var seguro = ProtegerFormula(valor).Replace("\"", "\"\"");
        return $"\"{seguro}\"";
    }
}

internal sealed class ArchivoLeido
{
    public List<string> Cabeceras { get; set; } = new();
    public List<Dictionary<string, string?>> Filas { get; set; } = new();
    public List<ProblemaArchivo> Problemas { get; set; } = new();
}

internal sealed record ProblemaArchivo(
    int NumeroFila,
    string? Campo,
    string Codigo,
    string Mensaje,
    string? ValorOriginal,
    bool EsAdvertencia);
