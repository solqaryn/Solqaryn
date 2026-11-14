using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-019.C: la clasificacion comercial parcial no certifica por si sola
/// implementacion integral, autorizacion de release ni preparacion PROD.
/// Esta prueba documental NO reemplaza HTTP/JWT, MySQL o pruebas E2E.
/// </summary>
public sealed class P2019CReleaseParcialLimiteTests
{
    private const string Frontera = "docs/P0-011-A-FRONTERA-VERSION-INICIAL-DEV.md";
    private const string Readiness = "docs/PRODUCTION_READINESS_CONTRACT.md";

    [Theory]
    [InlineData("C08", "INICIAL")]
    [InlineData("C13", "CONDICIONAL")]
    [InlineData("C17", "FUTURO")]
    public void CapacidadClasificada_NoSeConvierteEnReleaseCertificado(
        string codigo, string claseEsperada)
    {
        var matriz = LeerArchivoDelRepositorio(Frontera);
        var fila = matriz.Split('\n')
            .Select(linea => linea.Trim())
            .Single(linea => linea.StartsWith($"| {codigo} |", StringComparison.Ordinal));
        var columnas = fila.Split('|').Select(campo => campo.Trim()).ToArray();

        Assert.Equal(codigo, columnas[1]);
        Assert.Equal(claseEsperada, columnas[3]);
        Assert.Contains(
            "NO acredita implementación, pruebas, despliegue ni estado LISTO",
            matriz);
        Assert.Contains("No inferir certificado desde CI", matriz);
    }

    [Fact]
    public void PruebasParciales_NoPuedenAnunciarERPCompletoNiPromocion()
    {
        var contrato = LeerArchivoDelRepositorio(Readiness);

        Assert.Contains("PRODUCTION_READY = FALSE", contrato);
        Assert.DoesNotContain("PRODUCTION_READY = TRUE", contrato);
        Assert.Contains(
            "Autorización humana explícita para cualquier operación de Producción",
            contrato);
        Assert.Contains(
            "Un health check verde o un build exitoso por sí solos",
            contrato);
    }

    private static string LeerArchivoDelRepositorio(string rutaRelativa)
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (directorio is not null)
        {
            var ruta = Path.Combine(
                directorio.FullName,
                rutaRelativa.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(ruta))
                return File.ReadAllText(ruta);

            directorio = directorio.Parent;
        }

        throw new FileNotFoundException(
            $"No se encontro el contrato versionado de release parcial: {rutaRelativa}");
    }
}
