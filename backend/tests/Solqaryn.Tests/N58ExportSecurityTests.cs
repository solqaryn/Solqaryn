using System.Reflection;
using System.Text;
using ClosedXML.Excel;
using Solqaryn.API.Controllers;
using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N58ExportSecurityTests
{
    [Theory]
    [InlineData("=1+1")]
    [InlineData("+SUM(A1:A2)")]
    [InlineData("-10+20")]
    [InlineData("@SUM(A1:A2)")]
    [InlineData("   =cmd|' /C calc'!A0")]
    public void ValorSeguro_NeutralizaPrefijosDeFormula(string value)
    {
        var safe = InvokePrivate<string>("ValorSeguro", value);

        Assert.StartsWith("'", safe, StringComparison.Ordinal);
        Assert.EndsWith(value, safe, StringComparison.Ordinal);
    }

    [Fact]
    public void CsvYNombres_NeutralizanFormulaYConservanContratoSeguro()
    {
        var file = InvokePrivate<ArchivoDescargableDto>(
            "CrearCsv",
            new[] { "Campo" },
            new List<string[]> { new[] { "=HYPERLINK(\"https://example.invalid\")" } },
            "usuarios-accesos");

        var text = Encoding.UTF8.GetString(file.Contenido);
        Assert.Contains("'=HYPERLINK", text, StringComparison.Ordinal);
        Assert.Equal("text/csv; charset=utf-8", file.ContentType);
        Assert.Equal("usuarios-accesos.csv", file.NombreArchivo);
    }

    [Fact]
    public void Xlsx_EscribeEntradaDeFormulaComoTextoNeutralizado()
    {
        var file = InvokePrivate<ArchivoDescargableDto>(
            "CrearXlsx",
            new[] { "Campo" },
            new List<string[]> { new[] { "=1+1" } },
            "roles-permisos");

        using var stream = new MemoryStream(file.Contenido);
        using var workbook = new XLWorkbook(stream);
        var cell = workbook.Worksheet("Reporte").Cell(2, 1);

        Assert.False(cell.HasFormula);
        Assert.Equal(XLDataType.Text, cell.DataType);
        Assert.Equal("=1+1", cell.GetString());
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.ContentType);
        Assert.Equal("roles-permisos.xlsx", file.NombreArchivo);
    }

    [Fact]
    public void Pdf_GeneraBytesPdfLocalesConMimeYNombreDeterministas()
    {
        var file = InvokePrivate<ArchivoDescargableDto>(
            "CrearPdf",
            new[] { "Usuario", "Rol" },
            new List<string[]> { new[] { "admin", "Administrador" } },
            "usuarios-accesos");

        Assert.True(file.Contenido.Length > 100);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(file.Contenido, 0, 4));
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal("usuarios-accesos.pdf", file.NombreArchivo);
    }

    [Fact]
    public void ExportEndpoint_ExigeAutenticacionYPermisoExportar()
    {
        var controllerType = typeof(ReportesAdministrativosController);
        Assert.NotNull(controllerType.GetCustomAttribute<AuthorizeAttribute>());

        var method = controllerType.GetMethod(nameof(ReportesAdministrativosController.Exportar));
        Assert.NotNull(method);

        var permission = method!.CustomAttributes.SingleOrDefault(a => a.AttributeType.Name == "RequierePermisoAttribute");
        Assert.NotNull(permission);
        Assert.Equal(2, permission!.ConstructorArguments.Count);
        Assert.Equal((int)ModuloSistema.ReportesAdministrativos, Convert.ToInt32(permission.ConstructorArguments[0].Value));
        Assert.Equal((int)AccionPermiso.Exportar, Convert.ToInt32(permission.ConstructorArguments[1].Value));
    }

    private static T InvokePrivate<T>(string name, params object?[] arguments)
    {
        var method = typeof(ReporteAdministrativoService).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return Assert.IsType<T>(method!.Invoke(null, arguments));
    }
}
