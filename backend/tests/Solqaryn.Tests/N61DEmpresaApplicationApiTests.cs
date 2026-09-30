using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N61DEmpresaApplicationApiTests
{
    private readonly Mock<IEmpresaRepository> _repository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IAuditoriaService> _auditoria = new();
    private readonly EmpresaService _service;

    public N61DEmpresaApplicationApiTests()
    {
        _currentUser.SetupGet(x => x.UsuarioId).Returns(17);
        _currentUser.SetupGet(x => x.NombreUsuario).Returns("vaep-n61d");
        _auditoria
            .Setup(x => x.RegistrarAsync(
                It.IsAny<Solqaryn.Domain.Enums.ModuloSistema>(),
                It.IsAny<Solqaryn.Domain.Enums.AccionPermiso>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<object?>(),
                It.IsAny<object?>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        _service = new EmpresaService(_repository.Object, _currentUser.Object, _auditoria.Object);
    }

    [Fact]
    public async Task CreateAsync_NormalizesName_AndStampsCreatorAudit()
    {
        Empresa? persisted = null;
        _repository
            .Setup(x => x.AddAsync(It.IsAny<Empresa>(), It.IsAny<CancellationToken>()))
            .Callback<Empresa, CancellationToken>((empresa, _) => persisted = empresa)
            .Returns(Task.CompletedTask);
        _repository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.CreateAsync(new CreateEmpresaDto { Nombre = "  Solqaryn Honduras  " });

        Assert.NotNull(persisted);
        Assert.Equal("Solqaryn Honduras", persisted!.Nombre);
        Assert.True(persisted.Activa);
        Assert.Equal(17, persisted.CreadoPorUsuarioId);
        Assert.Equal("vaep-n61d", persisted.CreadoPorNombreUsuario);
        Assert.Equal("Solqaryn Honduras", result.Nombre);
        Assert.True(result.Activa);
        _repository.Verify(x => x.AddAsync(persisted, It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_BlankName_FailsClosedWithoutPersistence(string nombre)
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.CreateAsync(new CreateEmpresaDto { Nombre = nombre }));

        _repository.Verify(
            x => x.AddAsync(It.IsAny<Empresa>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _repository.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_MissingEmpresa_IsIdempotentAndDoesNotWrite()
    {
        _repository
            .Setup(x => x.GetByIdAsync(77, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Empresa?)null);

        var result = await _service.UpdateAsync(77, new UpdateEmpresaDto { Nombre = "No existe" });

        Assert.Null(result);
        _repository.Verify(x => x.Update(It.IsAny<Empresa>()), Times.Never);
        _repository.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CambiarEstadoAsync_SameState_IsIdempotentAndDoesNotWrite()
    {
        var empresa = new Empresa("Empresa activa");
        _repository
            .Setup(x => x.GetByIdAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(empresa);

        var result = await _service.CambiarEstadoAsync(9, true);

        Assert.NotNull(result);
        Assert.True(result!.Activa);
        _repository.Verify(x => x.Update(It.IsAny<Empresa>()), Times.Never);
        _repository.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_SaveFailure_PropagatesBusinessFailure()
    {
        _repository
            .Setup(x => x.AddAsync(It.IsAny<Empresa>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.CreateAsync(new CreateEmpresaDto { Nombre = "Empresa" }));
    }
}
