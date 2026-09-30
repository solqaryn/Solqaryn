using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N110PoliticaCosteoInventarioApplicationTests
{
    [Fact]
    public async Task Cambiar_mismo_metodo_es_idempotente_y_no_crea_historial_artificial()
    {
        var vigente = PoliticaCosteoInventario.Crear(
            1,
            MetodoCosteoInventario.PromedioPonderado,
            DateTime.UtcNow.AddDays(-10),
            "Política inicial");
        var (service, repository, _, auditoria) = CrearServicio(vigente);

        var result = await service.CambiarAsync(new CambiarPoliticaCosteoInventarioDto
        {
            Metodo = MetodoCosteoInventario.PromedioPonderado,
            Motivo = "Solicitud repetida"
        });

        Assert.Equal(MetodoCosteoInventario.PromedioPonderado, result.Metodo);
        Assert.True(result.EstaVigente);
        repository.Verify(x => x.AddAsync(It.IsAny<PoliticaCosteoInventario>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
        auditoria.Verify(x => x.RegistrarEstrictoAsync(
            It.IsAny<ModuloSistema>(),
            It.IsAny<AccionPermiso>(),
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<string?>(),
            It.IsAny<object?>(),
            It.IsAny<object?>(),
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task Cambiar_metodo_cierra_vigente_abre_version_y_audita_estrictamente()
    {
        var vigente = PoliticaCosteoInventario.Crear(
            1,
            MetodoCosteoInventario.PromedioPonderado,
            DateTime.UtcNow.AddDays(-10),
            "Política inicial");
        var (service, repository, capturada, auditoria) = CrearServicio(vigente);

        var result = await service.CambiarAsync(new CambiarPoliticaCosteoInventarioDto
        {
            Metodo = MetodoCosteoInventario.FIFO,
            Motivo = "Adopción FIFO empresarial"
        });

        Assert.False(vigente.EstaVigente);
        Assert.NotNull(vigente.VigenteHastaUtc);
        Assert.Equal(MetodoCosteoInventario.FIFO, result.Metodo);
        Assert.Equal("Adopción FIFO empresarial", capturada.Value!.Motivo);
        Assert.True(capturada.Value.EstaVigente);
        repository.Verify(x => x.AddAsync(It.IsAny<PoliticaCosteoInventario>()), Times.Once);
        repository.Verify(x => x.SaveChangesAsync(), Times.Once);
        auditoria.Verify(x => x.RegistrarEstrictoAsync(
            ModuloSistema.MovimientosInventario,
            AccionPermiso.Editar,
            "Política de costeo de inventario actualizada.",
            It.IsAny<int?>(),
            "PoliticaCosteoInventario",
            It.IsNotNull<object>(),
            It.IsNotNull<object>(),
            "Adopción FIFO empresarial",
            It.IsAny<string>(),
            It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task Historial_rechaza_rango_invertido_antes_de_consultar_persistencia()
    {
        var (service, repository, _, _) = CrearServicio(null);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.GetHistorialAsync(
            new PoliticaCosteoInventarioQueryDto
            {
                DesdeUtc = DateTime.UtcNow,
                HastaUtc = DateTime.UtcNow.AddDays(-1)
            }));

        repository.Verify(x => x.GetHistorialAsync(
            It.IsAny<int>(),
            It.IsAny<PoliticaCosteoInventarioQueryDto>()), Times.Never);
    }

    [Fact]
    public async Task Historial_rechaza_fechas_no_utc_antes_de_consultar_persistencia()
    {
        var (service, repository, _, _) = CrearServicio(null);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.GetHistorialAsync(
            new PoliticaCosteoInventarioQueryDto
            {
                DesdeUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Local)
            }));

        repository.Verify(x => x.GetHistorialAsync(
            It.IsAny<int>(),
            It.IsAny<PoliticaCosteoInventarioQueryDto>()), Times.Never);
    }

    [Fact]
    public async Task Sin_empresa_activa_falla_cerrado_antes_de_leer_politica()
    {
        var repository = new Mock<IPoliticaCosteoInventarioRepository>();
        var empresas = new Mock<IEmpresaConfiguracionRepository>();
        empresas.Setup(x => x.GetActivaAsync()).ReturnsAsync((EmpresaConfiguracion?)null);
        var currentUser = new Mock<ICurrentUserService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var auditoria = CrearAuditoria();
        var service = new PoliticaCosteoInventarioService(
            repository.Object,
            empresas.Object,
            currentUser.Object,
            unitOfWork.Object,
            auditoria.Object);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.GetVigenteAsync());
        repository.Verify(x => x.GetVigenteAsync(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Catalogo_expone_solo_los_tres_metodos_canonicos()
    {
        var (service, _, _, _) = CrearServicio(null);

        var metodos = await service.GetMetodosAsync();

        Assert.Equal(3, metodos.Count);
        Assert.Equal(
            new[] { MetodoCosteoInventario.PromedioPonderado, MetodoCosteoInventario.FIFO, MetodoCosteoInventario.Estandar },
            metodos.Select(x => x.Id).ToArray());
    }

    private static (
        PoliticaCosteoInventarioService Service,
        Mock<IPoliticaCosteoInventarioRepository> Repository,
        CapturaPolitica Capturada,
        Mock<IAuditoriaService> Auditoria) CrearServicio(PoliticaCosteoInventario? vigente)
    {
        var repository = new Mock<IPoliticaCosteoInventarioRepository>();
        repository
            .Setup(x => x.GetVigenteAsync(1, It.IsAny<bool>()))
            .ReturnsAsync(vigente);
        var capturada = new CapturaPolitica();
        repository
            .Setup(x => x.AddAsync(It.IsAny<PoliticaCosteoInventario>()))
            .Callback<PoliticaCosteoInventario>(x => capturada.Value = x)
            .Returns(Task.CompletedTask);
        repository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var empresas = new Mock<IEmpresaConfiguracionRepository>();
        empresas.Setup(x => x.GetActivaAsync()).ReturnsAsync(new EmpresaConfiguracion { Id = 1, Activa = true });

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.UsuarioId).Returns(7);
        currentUser.SetupGet(x => x.NombreUsuario).Returns("qa-costeo");

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(operation => operation());

        var auditoria = CrearAuditoria();

        return (
            new PoliticaCosteoInventarioService(
                repository.Object,
                empresas.Object,
                currentUser.Object,
                unitOfWork.Object,
                auditoria.Object),
            repository,
            capturada,
            auditoria);
    }

    private static Mock<IAuditoriaService> CrearAuditoria()
    {
        var auditoria = new Mock<IAuditoriaService>();
        auditoria
            .Setup(x => x.RegistrarEstrictoAsync(
                It.IsAny<ModuloSistema>(),
                It.IsAny<AccionPermiso>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<object?>(),
                It.IsAny<object?>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        return auditoria;
    }

    private sealed class CapturaPolitica
    {
        public PoliticaCosteoInventario? Value { get; set; }
    }
}
