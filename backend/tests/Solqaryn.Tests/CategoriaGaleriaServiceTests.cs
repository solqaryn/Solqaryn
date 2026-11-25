using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public sealed class CategoriaGaleriaServiceTests
{
    private readonly Mock<ICategoriaRepository> _categorias = new();
    private readonly Mock<ICategoriaGaleriaRepository> _galerias = new();
    private readonly Mock<IImageStorageService> _storage = new();
    private readonly StorageTenantContext _tenant = StorageTenantContext.Desde(new UsuarioTenantScopeActual(2, 7, 1, "Admin", true));
    private readonly CategoriaGaleriaService _service;

    public CategoriaGaleriaServiceTests()
    {
        _categorias.Setup(x => x.GetByIdAsync(4)).ReturnsAsync(new Categoria { Id = 4, Nombre = "Tennis" });
        _service = new CategoriaGaleriaService(_categorias.Object, _galerias.Object, _storage.Object,
            Mock.Of<IAuditoriaService>(), NullLogger<CategoriaGaleriaService>.Instance);
    }

    private static IFormFile Imagen(int i) =>
        new FormFile(new MemoryStream(new byte[] { 1, 2, 3 }), 0, 3, "Archivos", $"foto-{i}.png")
        { Headers = new HeaderDictionary(), ContentType = "image/png" };

    [Fact]
    public async Task GuardaCincoFotos_EnOrden_YPublicaSoloUrlsDelTenant()
    {
        CategoriaGaleria? guardada = null;
        _galerias.Setup(x => x.SaveAsync(It.IsAny<CategoriaGaleria>(), true, It.IsAny<CancellationToken>()))
            .Callback<CategoriaGaleria, bool, CancellationToken>((g, _, _) => guardada = g).Returns(Task.CompletedTask);
        _storage.Setup(x => x.UploadAsync(_tenant, It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StorageTenantContext t, IFormFile f, CancellationToken ct) =>
                ($"https://res.cloudinary.com/test/image/upload/{f.FileName}", $"empresas/7/{f.FileName}"));
        var dto = new ActualizarCategoriaGaleriaDto
        {
            Orden = Enumerable.Range(0, 5).Reverse().Select(i => $"n:{i}").ToList(),
            Archivos = Enumerable.Range(0, 5).Select(Imagen).ToList()
        };
        var resultado = await _service.GuardarAsync(_tenant, 4, dto, default);
        Assert.NotNull(resultado);
        Assert.Equal(5, resultado.Imagenes.Count);
        Assert.EndsWith("foto-4.png", resultado.Imagenes[0].Url);
        Assert.NotNull(guardada);
        Assert.Equal(7, guardada.EmpresaId);
        Assert.Equal(4, guardada.CategoriaId);
        _galerias.Setup(x => x.GetByCategoriasAsync(7, It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategoriaGaleria> { guardada, new() { EmpresaId = 8, CategoriaId = 4, ImagenesJson = guardada.ImagenesJson } });
        var publicas = await _service.ObtenerPublicasAsync(7, new[] { 4 }, default);
        Assert.Single(publicas);
        Assert.Equal(resultado.Imagenes.Select(i => i.Url), publicas[4]);
    }

    [Fact]
    public async Task RechazaSextaFoto_AntesDeSubir()
    {
        var dto = new ActualizarCategoriaGaleriaDto
        {
            Orden = Enumerable.Range(0, 6).Select(i => $"n:{i}").ToList(),
            Archivos = Enumerable.Range(0, 6).Select(Imagen).ToList()
        };
        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.GuardarAsync(_tenant, 4, dto, default));
        _storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RechazaReferenciasAImagenesAjenas_YVersionObsoleta()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.GuardarAsync(_tenant, 4,
            new ActualizarCategoriaGaleriaDto { Orden = new() { "e:" + Guid.NewGuid().ToString("N") } }, default));
        _galerias.Setup(x => x.GetAsync(7, 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CategoriaGaleria { EmpresaId = 7, CategoriaId = 4, Version = "vigente" });
        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.GuardarAsync(_tenant, 4,
            new ActualizarCategoriaGaleriaDto { Version = "anterior" }, default));
        _storage.VerifyNoOtherCalls();
    }
}
