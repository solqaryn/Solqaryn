using System.Text.RegularExpressions;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-019.D: evita que un release comercial parcial se declare listo
/// para Produccion por clasificacion de alcance o CI aislada.
/// Es una regresion documental; no prueba despliegue ni integracion real.
/// </summary>
public sealed class P2019DReadinessGateNoPromotionTests
{
    private const string Frontera = "docs/P0-011-A-FRONTERA-VERSION-INICIAL-DEV.md";
    private const string Readiness = "docs/PRODUCTION_READINESS_CONTRACT.md";

    [Fact]
    public void ClasificacionComercialParcial_NoEliminaGatesDePromocion()
    {
        var matriz = LeerArchivoDelRepositorio(Frontera);
        var filaPromocion = matriz.Split('\n')
            .Select(linea => linea.Trim())
            .Single(linea => linea.StartsWith("| C13 |", StringComparison.Ordinal));

        Assert.Contains("| CONDICIONAL |", filaPromocion);
        Assert.Contains("una PR fusionada en dev o CI verde no concede despliegue", filaPromocion);

        var contrato = LeerArchivoDelRepositorio(Readiness);
        var pasos = contrato.Split('\n')
            .Select(linea => Regex.Match(linea.Trim(), @"^(?<numero>\d+)\.\s+(?<contenido>.+)$"))
            .Where(resultado => resultado.Success)
            .ToArray();

        Assert.Equal(12, pasos.Length);
        Assert.Equal(
            Enumerable.Range(1, 12).ToArray(),
            pasos.Select(resultado => int.Parse(resultado.Groups["numero"].Value)).ToArray());

        Assert.Contains("Build Release backend", pasos[0].Groups["contenido"].Value);
        Assert.Contains("staging comparable", pasos[2].Groups["contenido"].Value);
        Assert.Contains("backup restaurable", pasos[2].Groups["contenido"].Value);
        Assert.Contains("Rollback", pasos[8].Groups["contenido"].Value);
        Assert.Contains("P0/P1 abiertos = 0", pasos[10].Groups["contenido"].Value);
        Assert.Contains("Autorización humana explícita", pasos[11].Groups["contenido"].Value);
        Assert.Contains("PRODUCTION_READY = FALSE", contrato);
        Assert.DoesNotContain("PRODUCTION_READY = TRUE", contrato);
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
            $"No se encontro el contrato versionado: {rutaRelativa}");
    }
}
