using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-019.E: negativos reproducibles del contrato de release comercial parcial.
/// Comprueba que un alcance DEV o un build verde no habiliten PROD ni
/// sustituyan evidencias de seguridad, restore, rollback y autorizacion.
/// Solo valida contratos versionados, NO ejecuta despliegues o E2E.
/// </summary>
public sealed class P2019EReleaseParcialNegativosTests
{
    private const string Frontera = "docs/P0-011-A-FRONTERA-VERSION-INICIAL-DEV.md";
    private const string Readiness = "docs/PRODUCTION_READINESS_CONTRACT.md";

    [Theory]
    [InlineData(2, "Unit/integration/contract/E2E/security")]
    [InlineData(3, "backup restaurable probado")]
    [InlineData(5, "CORS exacto, JWT, RBAC y protección por recurso")]
    [InlineData(6, "logging estructurado presentes sin datos sensibles")]
    [InlineData(9, "Rollback de aplicación/configuración/DB")]
    [InlineData(11, "P0/P1 abiertos = 0")]
    [InlineData(12, "Autorización humana explícita")]
    public void GateObligatorioNoPuedeDesaparecerDeLaCertificacion(
        int paso, string controlEsperado)
    {
        var contrato = LeerArchivo(Readiness);
        var filas = Regex.Matches(contrato, @"(?m)^(\d+)\.\s+(.+)$")
            .Cast<Match>()
            .Where(m => int.Parse(m.Groups[1].Value) == paso)
            .ToArray();

        var fila = Assert.Single(filas);
        Assert.Contains(controlEsperado, fila.Groups[2].Value);
    }

    [Theory]
    [InlineData("C03", "INICIAL")]
    [InlineData("C08", "INICIAL")]
    [InlineData("C13", "CONDICIONAL")]
    [InlineData("C17", "FUTURO")]
    [InlineData("C20", "FUTURO")]
    public void ClaseComercialNuncaEsPermisoDePromocion(
        string codigo, string claseEsperada)
    {
        var documento = LeerArchivo(Frontera);
        var fila = documento.Split('\n')
            .Select(x => x.Trim())
            .Single(x => x.StartsWith($"| {codigo} |", StringComparison.Ordinal));
        var campos = fila.Split('|').Select(x => x.Trim()).ToArray();

        Assert.Equal(claseEsperada, campos[3]);
        Assert.Contains("no concede despliegue", documento);
        Assert.Contains("NO acredita implementación, pruebas, despliegue ni estado LISTO",
            documento);
    }

    [Fact]
    public void ReadyFailClosedYSecretosNuncaPorUnBuildVerde()
    {
        var contrato = LeerArchivo(Readiness);

        Assert.Contains("PRODUCTION_READY = FALSE", contrato);
        Assert.DoesNotContain("PRODUCTION_READY = TRUE", contrato);
        Assert.Contains("exclusivamente desde el gestor de secretos", contrato);
        Assert.Contains("Nunca se debe promover `CHANGE_ME`", contrato);
        Assert.Contains("Un health check verde o un build exitoso", contrato);
        Assert.Contains("Autorización humana explícita", contrato);
    }

    private static string LeerArchivo(string rutaRelativa)
    {
        DirectoryInfo? actual = new(AppContext.BaseDirectory);
        while (actual is not null)
        {
            var ruta = Path.Combine(actual.FullName,
                rutaRelativa.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(ruta))
                return File.ReadAllText(ruta);

            actual = actual.Parent;
        }

        throw new FileNotFoundException(
            $"P2-019.E requiere contrato versionado: {rutaRelativa}");
    }
}
