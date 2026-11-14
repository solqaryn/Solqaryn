using System.Text.Json;
using Solqaryn.Domain.Entities.Catalogos;
using Xunit;

namespace Solqaryn.Tests;

public class MetodoPagoReglasOperativasTests
{
    [Fact]
    public void Orden_Negativo_EsRechazadoFailClosed()
    {
        var metodo = new MetodoPago();

        var error = Assert.Throws<ArgumentOutOfRangeException>(() => metodo.Orden = -1);

        Assert.Contains("orden", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, metodo.Orden);
    }

    [Fact]
    public void Metadata_JsonMalformado_EsRechazado()
    {
        var metodo = new MetodoPago();

        Assert.ThrowsAny<JsonException>(() => metodo.Metadata = "{\"terminal\":");
    }

    [Fact]
    public void Metadata_RaizNoObjeto_EsRechazada()
    {
        var metodo = new MetodoPago();

        var error = Assert.Throws<ArgumentException>(() => metodo.Metadata = "[1,2,3]");

        Assert.Contains("objeto JSON", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Metadata_ObjetoValido_SeCanonizaRecursivamente()
    {
        var metodo = new MetodoPago
        {
            Metadata = " { \"z\": 2, \"a\": { \"b\": true, \"a\": 1 }, \"lista\": [{\"y\":2,\"x\":1}] } "
        };

        Assert.Equal("{\"a\":{\"a\":1,\"b\":true},\"lista\":[{\"x\":1,\"y\":2}],\"z\":2}", metodo.Metadata);
    }

    [Fact]
    public void Metadata_Vacia_SeNormalizaANull()
    {
        var metodo = new MetodoPago { Metadata = "   " };

        Assert.Null(metodo.Metadata);
    }

    [Fact]
    public void OrdenarParaSeleccion_UsaOrdenCodigoNormalizadoEId()
    {
        var metodos = new[]
        {
            new MetodoPago { Id = 9, Codigo = " ZETA ", Nombre = "Z", Orden = 1 },
            new MetodoPago { Id = 7, Codigo = "beta", Nombre = "B", Orden = 0 },
            new MetodoPago { Id = 5, Codigo = "ALFA", Nombre = "A", Orden = 1 },
            new MetodoPago { Id = 3, Codigo = " alfa ", Nombre = "A legacy", Orden = 1 }
        };

        var ordenados = MetodoPago.OrdenarParaSeleccion(metodos);

        Assert.Equal(new[] { 7, 3, 5, 9 }, ordenados.Select(x => x.Id).ToArray());
    }
}
