using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N17ConteoInventarioBlindActiveAggregateSecurityRegressionTests
{
    [Fact]
    public async Task GetPaged_ConteoCiegoEnProcesoConCapturas_NoFiltraReferenciaPorLineasNiAgregados()
    {
        var repository = new Mock<IConteoInventarioRepository>();
        var existencias = new Mock<IExistenciaVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var ahora = DateTime.UtcNow;

        var primera = new ConteoInventarioDetalle
        {
            Id = 31,
            ProductoVarianteId = 101,
            AlmacenId = 7
        };
        primera.MaterializarSnapshot(18);
        primera.RegistrarConteo(14, 91, ahora.AddMinutes(1));

        var segunda = new ConteoInventarioDetalle
        {
            Id = 32,
            ProductoVarianteId = 102,
            AlmacenId = 7
        };
        segunda.MaterializarSnapshot(9);
        segunda.RegistrarConteo(11, 91, ahora.AddMinutes(2));

        var conteo = new ConteoInventario
        {
            Id = 45,
            Numero = "CNT-CIEGO-ACTIVO-45",
            Tipo = TipoConteoInventario.Ciego,
            AlmacenId = 7,
            EsCiego = true,
            Detalles = new List<ConteoInventarioDetalle> { primera, segunda }
        };
        conteo.Iniciar(91, ahora);

        repository.Setup(x => x.GetPagedAsync(It.IsAny<ConteoInventarioQueryDto>()))
            .ReturnsAsync((new List<ConteoInventario> { conteo }, 1));

        var service = new ConteoInventarioService(
            repository.Object,
            existencias.Object,
            currentUser.Object,
            unitOfWork.Object);

        var pagina = await service.GetPagedAsync(new ConteoInventarioQueryDto());

        var dto = Assert.Single(pagina.Items);
        Assert.True(dto.EsCiego);
        Assert.Equal(EstadoConteoInventario.EnProceso, dto.Estado);
        Assert.Null(dto.FechaCierre);
        Assert.Equal(0, dto.CantidadConDiferencia);
        Assert.Equal(0, dto.DiferenciaNeta);
        Assert.Equal(2, dto.Detalles.Count);

        Assert.Collection(
            dto.Detalles,
            linea =>
            {
                Assert.Equal(14, linea.CantidadContada.GetValueOrDefault());
                Assert.Null(linea.StockEsperado);
                Assert.Null(linea.Diferencia);
            },
            linea =>
            {
                Assert.Equal(11, linea.CantidadContada.GetValueOrDefault());
                Assert.Null(linea.StockEsperado);
                Assert.Null(linea.Diferencia);
            });
    }
}
