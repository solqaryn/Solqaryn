using System.IO.Compression;
using Solqaryn.Application.Exceptions;

namespace Solqaryn.Infrastructure.Services;

public static class CargaMasivaArchivoSecurity
{
    private const int MaximoEntradasZip = 2500;
    private const long MaximoDescomprimidoTotal = 50L * 1024L * 1024L;
    private const long MaximoEntradaDescomprimida = 15L * 1024L * 1024L;
    private const double MaximaRelacionCompresion = 250d;

    /// <summary>
    /// XLSX es un contenedor ZIP. Esta validación se ejecuta antes de ClosedXML
    /// para rechazar rutas inseguras y expansiones desproporcionadas.
    /// El stream queda reposicionado para su lectura posterior.
    /// </summary>
    public static void ValidarXlsx(Stream contenido)
    {
        if (!contenido.CanRead)
            throw new BusinessRuleException("El archivo XLSX no puede leerse.");

        var posicionOriginal = contenido.CanSeek ? contenido.Position : 0;
        try
        {
            using var archive = new ZipArchive(contenido, ZipArchiveMode.Read, leaveOpen: true);
            if (archive.Entries.Count == 0)
                throw new BusinessRuleException("El archivo XLSX está vacío o no es válido.");
            if (archive.Entries.Count > MaximoEntradasZip)
                throw new BusinessRuleException("El archivo XLSX contiene demasiados componentes internos.");

            long total = 0;
            foreach (var entry in archive.Entries)
            {
                var nombre = entry.FullName.Replace('\\', '/');
                if (nombre.StartsWith('/') || nombre.Contains("../", StringComparison.Ordinal) || nombre.Contains(':'))
                    throw new BusinessRuleException("El archivo XLSX contiene rutas internas no permitidas.");

                if (entry.Length > MaximoEntradaDescomprimida)
                    throw new BusinessRuleException("El archivo XLSX contiene un componente interno demasiado grande.");

                total = checked(total + entry.Length);
                if (total > MaximoDescomprimidoTotal)
                    throw new BusinessRuleException("El contenido descomprimido del XLSX supera el límite permitido.");

                if (entry.CompressedLength > 0 && entry.Length > 1024 * 1024)
                {
                    var relacion = (double)entry.Length / entry.CompressedLength;
                    if (relacion > MaximaRelacionCompresion)
                        throw new BusinessRuleException("El archivo XLSX presenta una relación de compresión no permitida.");
                }
            }

            var tipos = archive.GetEntry("[Content_Types].xml");
            var workbook = archive.GetEntry("xl/workbook.xml");
            if (tipos is null || workbook is null)
                throw new BusinessRuleException("El archivo no corresponde a un libro XLSX válido.");
        }
        catch (InvalidDataException)
        {
            throw new BusinessRuleException("El archivo XLSX está dañado o no corresponde al formato esperado.");
        }
        catch (OverflowException)
        {
            throw new BusinessRuleException("El tamaño interno del archivo XLSX no es válido.");
        }
        finally
        {
            if (contenido.CanSeek) contenido.Position = posicionOriginal;
        }
    }
}
