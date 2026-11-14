using Solqaryn.API.Controllers;
using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Solqaryn.Tests;

public class M9CargaMasivaProfesionalTests
{
    [Fact]
    public void Configuracion_M9_ExponeVersionLoteVistaPreviaYEtapas()
    {
        var dto = new CargaMasivaConfiguracionDto();

        Assert.Equal("M9.1", dto.VersionPlantillaActual);
        Assert.Equal(250, dto.TamanoLoteProcesamiento);
        Assert.Equal(200, dto.MaximoFilasVistaPrevia);
        Assert.Equal(new[] { "Carga", "Lectura", "Validacion", "VistaPrevia", "Confirmacion" }, dto.EtapasProceso);
    }

    [Fact]
    public void TipoCarga_ConservaVersionDePlantillaExplicita()
    {
        var dto = new CargaMasivaTipoDto();

        Assert.Equal("M9.1", dto.VersionPlantilla);
    }

    [Fact]
    public void Progreso_ExponeCorrectosErroresOmitidosYEtapas()
    {
        var dto = new CargaMasivaProgresoDto
        {
            TotalFilas = 10,
            FilasCorrectas = 7,
            FilasConError = 2,
            FilasOmitidas = 1,
            Etapas =
            [
                new CargaMasivaEtapaDto { Codigo = "Carga", Estado = "Completada", Porcentaje = 100 },
                new CargaMasivaEtapaDto { Codigo = "Validacion", Estado = "Completada", Porcentaje = 100 }
            ]
        };

        Assert.Equal(dto.TotalFilas, dto.FilasCorrectas + dto.FilasConError + dto.FilasOmitidas);
        Assert.All(dto.Etapas, etapa => Assert.InRange(etapa.Porcentaje, 0, 100));
    }

    [Fact]
    public void Controller_PublicaEndpointDeProgresoProtegidoPorIdEntero()
    {
        var metodo = typeof(CargasMasivasController).GetMethod(nameof(CargasMasivasController.Progreso));
        Assert.NotNull(metodo);

        var httpGet = metodo!.GetCustomAttributes(typeof(HttpGetAttribute), inherit: true)
            .Cast<HttpGetAttribute>()
            .Single();

        Assert.Equal("{id:int}/progreso", httpGet.Template);
    }

    [Fact]
    public void VariantesInventario_SigueSiendoTipoOficialDeCarga()
    {
        Assert.True(Enum.IsDefined(TipoCargaMasiva.VariantesInventario));
    }
}
