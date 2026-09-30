using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N17ConteoInventarioPagingContractTests
{
    [Theory]
    [InlineData(0, 0, 1, 1)]
    [InlineData(-5, 500, 1, 100)]
    [InlineData(3, 50, 3, 50)]
    public async Task GetPaged_NormalizaLimitesAntesDeConsultarRepositorio(
        int page,
        int pageSize,
        int expectedPage,
        int expectedPageSize)
    {
        var repository = new Mock<IConteoInventarioRepository>();
        var existencias = new Mock<IExistenciaVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        ConteoInventarioQueryDto? capturada = null;

        repository
            .Setup(x => x.GetPagedAsync(It.IsAny<ConteoInventarioQueryDto>()))
            .Callback<ConteoInventarioQueryDto>(query => capturada = query)
            .ReturnsAsync((new List<Solqaryn.Domain.Entities.ConteoInventario>(), 0));

        var service = new ConteoInventarioService(
            repository.Object,
            existencias.Object,
            currentUser.Object,
            unitOfWork.Object);

        var resultado = await service.GetPagedAsync(new ConteoInventarioQueryDto
        {
            Page = page,
            PageSize = pageSize
        });

        Assert.NotNull(capturada);
        Assert.Equal(expectedPage, capturada!.Page);
        Assert.Equal(expectedPageSize, capturada.PageSize);
        Assert.Equal(expectedPage, resultado.Page);
        Assert.Equal(expectedPageSize, resultado.PageSize);
        repository.Verify(x => x.GetPagedAsync(It.IsAny<ConteoInventarioQueryDto>()), Times.Once);
    }
}
