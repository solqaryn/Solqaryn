using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities.Cajas;
using Solqaryn.Domain.Enums;
using Solqaryn.Domain.Enums.Cajas;
using Moq;
using Xunit;

namespace Solqaryn.Tests.Application.Cajas;

public sealed class CajaServiceTests
{
    [Fact]
    public async Task ActivarCajaAsync_no_requiere_permiso_ver_tras_mutacion()
    {
        var caja = new Caja("Caja") { Id = 1 };
        var (service, repository, unitOfWork, _, _, permisos) = CrearServicio();
        permisos
            .Setup(x => x.VerificarPermisoAsync(ModuloSistema.Caja, AccionPermiso.Ver))
            .ThrowsAsync(new ForbiddenAccessException("El permiso Ver no debe ser requisito para devolver una mutación ya autorizada."));

        repository.Setup(x => x.GetCajaByIdForUpdateAsync(1)).ReturnsAsync(caja);
        repository.Setup(x => x.UpdateCaja(caja));
        repository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        repository.Setup(x => x.GetCajaByIdAsync(1, false)).ReturnsAsync(caja);
        PrepararTransaccion(unitOfWork);

        await service.ActivarCajaAsync(1);

        permisos.Verify(x => x.VerificarPermisoAsync(ModuloSistema.Caja, AccionPermiso.Ver), Times.Never);
    }

    [Fact]
    public async Task DesactivarCajaAsync_no_requiere_permiso_ver_tras_mutacion()
    {
        var caja = new Caja("Caja") { Id = 1 };
        var (service, repository, unitOfWork, _, _, permisos) = CrearServicio();
        permisos
            .Setup(x => x.VerificarPermisoAsync(ModuloSistema.Caja, AccionPermiso.Ver))
            .ThrowsAsync(new ForbiddenAccessException("El permiso Ver no debe ser requisito para devolver una mutación ya autorizada."));

        repository.Setup(x => x.GetCajaByIdForUpdateAsync(1)).ReturnsAsync(caja);
        repository.Setup(x => x.UpdateCaja(caja));
        repository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        repository.Setup(x => x.GetCajaByIdAsync(1, false)).ReturnsAsync(caja);
        PrepararTransaccion(unitOfWork);

        await service.DesactivarCajaAsync(1);

        permisos.Verify(x => x.VerificarPermisoAsync(ModuloSistema.Caja, AccionPermiso.Ver), Times.Never);
    }

    [Fact]
    public async Task AbrirSesionAsync_no_requiere_permiso_ver_tras_mutacion()
    {
        var caja = new Caja("Caja") { Id = 1 };
        caja.Activar();
        var sesion = new CajaSesion(1, 7, 100m) { Id = 2 };
        var dto = new AbrirCajaSesionDto { FondoInicial = 100m };
        var (service, repository, unitOfWork, _, _, permisos) = CrearServicio();
        permisos
            .Setup(x => x.VerificarPermisoAsync(ModuloSistema.Caja, AccionPermiso.Ver))
            .ThrowsAsync(new ForbiddenAccessException("El permiso Ver no debe ser requisito para devolver una mutación ya autorizada."));

        repository.Setup(x => x.GetCajaByIdForUpdateAsync(1)).ReturnsAsync(caja);
        repository.Setup(x => x.AddSesionAsync(It.IsAny<CajaSesion>())).Callback<CajaSesion>(s => s.Id = 2).Returns(Task.CompletedTask);
        repository.Setup(x => x.UpdateCaja(caja));
        repository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        repository.Setup(x => x.GetSesionByIdAsync(2, false)).ReturnsAsync(sesion);
        PrepararTransaccion(unitOfWork);

        await service.AbrirSesionAsync(1, dto);

        permisos.Verify(x => x.VerificarPermisoAsync(ModuloSistema.Caja, AccionPermiso.Ver), Times.Never);
    }

    [Fact]
    public async Task IniciarOperacionesAsync_no_requiere_permiso_ver_tras_mutacion()
    {
        var sesion = new CajaSesion(1, 7, 100m) { Id = 1 };
        var (service, repository, unitOfWork, _, _, permisos) = CrearServicio();
        permisos
            .Setup(x => x.VerificarPermisoAsync(ModuloSistema.Caja, AccionPermiso.Ver))
            .ThrowsAsync(new ForbiddenAccessException("El permiso Ver no debe ser requisito para devolver una mutación ya autorizada."));

        repository.Setup(x => x.GetSesionByIdForUpdateAsync(1)).ReturnsAsync(sesion);
        repository.Setup(x => x.UpdateSesion(sesion));
        repository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        repository.Setup(x => x.GetSesionByIdAsync(1, false)).ReturnsAsync(sesion);
        PrepararTransaccion(unitOfWork);

        await service.IniciarOperacionesAsync(1);

        permisos.Verify(x => x.VerificarPermisoAsync(ModuloSistema.Caja, AccionPermiso.Ver), Times.Never);
    }

    [Fact]
    public async Task RegistrarMovimientoAsync_no_requiere_permiso_ver_tras_mutacion()
    {
        var sesion = new CajaSesion(1, 7, 100m) { Id = 1 };
        sesion.IniciarOperaciones();
        var dto = new RegistrarMovimientoCajaDto { Tipo = TipoMovimientoCaja.Ingreso, Monto = 50m, Referencia = "Ingreso" };
        var (service, repository, unitOfWork, _, _, permisos) = CrearServicio();
        permisos
            .Setup(x => x.VerificarPermisoAsync(ModuloSistema.Caja, AccionPermiso.Ver))
            .ThrowsAsync(new ForbiddenAccessException("El permiso Ver no debe ser requisito para devolver una mutación ya autorizada."));

        repository.Setup(x => x.GetSesionByIdForUpdateAsync(1)).ReturnsAsync(sesion);
        repository.Setup(x => x.UpdateSesion(sesion));
        repository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        repository.Setup(x => x.GetSesionByIdAsync(1, false)).ReturnsAsync(sesion);
        PrepararTransaccion(unitOfWork);

        await service.RegistrarMovimientoAsync(1, dto);

        permisos.Verify(x => x.VerificarPermisoAsync(ModuloSistema.Caja, AccionPermiso.Ver), Times.Never);
    }

    [Fact]
    public async Task IniciarArqueoAsync_no_requiere_permiso_ver_tras_mutacion()
    {
        var sesion = new CajaSesion(1, 7, 100m) { Id = 1 };
        sesion.IniciarOperaciones();
        var (service, repository, unitOfWork, _, _, permisos) = CrearServicio();
        permisos
            .Setup(x => x.VerificarPermisoAsync(ModuloSistema.Caja, AccionPermiso.Ver))
            .ThrowsAsync(new ForbiddenAccessException("El permiso Ver no debe ser requisito para devolver una mutación ya autorizada."));

        repository.Setup(x => x.GetSesionByIdForUpdateAsync(1)).ReturnsAsync(sesion);
        repository.Setup(x => x.UpdateSesion(sesion));
        repository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        repository.Setup(x => x.GetSesionByIdAsync(1, false)).ReturnsAsync(sesion);
        PrepararTransaccion(unitOfWork);

        await service.IniciarArqueoAsync(1);

        permisos.Verify(x => x.VerificarPermisoAsync(ModuloSistema.Caja, AccionPermiso.Ver), Times.Never);
    }

    [Fact]
    public async Task CerrarSesionAsync_no_requiere_permiso_ver_tras_mutacion()
    {
        var caja = new Caja("Caja") { Id = 1 };
        caja.Activar();
        caja.RegistrarSesionActiva(1);
        var sesion = new CajaSesion(1, 7, 100m) { Id = 1 };
        sesion.IniciarOperaciones();
        sesion.IniciarArqueo();
        var dto = new CerrarCajaSesionDto { SaldoContado = 100m, Observaciones = "Ok" };
        var (service, repository, unitOfWork, _, _, permisos) = CrearServicio();
        permisos
            .Setup(x => x.VerificarPermisoAsync(ModuloSistema.Caja, AccionPermiso.Ver))
            .ThrowsAsync(new ForbiddenAccessException("El permiso Ver no debe ser requisito para devolver una mutación ya autorizada."));

        repository.Setup(x => x.GetSesionByIdForUpdateAsync(1)).ReturnsAsync(sesion);
        repository.Setup(x => x.GetCajaByIdForUpdateAsync(1)).ReturnsAsync(caja);
        repository.Setup(x => x.UpdateSesion(sesion));
        repository.Setup(x => x.UpdateCaja(caja));
        repository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        repository.Setup(x => x.GetSesionByIdAsync(1, false)).ReturnsAsync(sesion);
        PrepararTransaccion(unitOfWork);

        await service.CerrarSesionAsync(1, dto);

        permisos.Verify(x => x.VerificarPermisoAsync(ModuloSistema.Caja, AccionPermiso.Ver), Times.Never);
    }

    [Fact]
    public async Task CrearCajaAsync_con_datos_validos_crea_persiste_y_audita_correctamente()
    {
        var dto = new CrearCajaDto { Nombre = "Caja Principal" };
        var (service, repository, unitOfWork, _, auditoria, permisos) = CrearServicio();
        permisos
            .Setup(x => x.VerificarPermisoAsync(ModuloSistema.Caja, AccionPermiso.Ver))
            .ThrowsAsync(new ForbiddenAccessException("El permiso Ver no debe ser requisito para devolver una creación ya autorizada."));

        repository
            .Setup(x => x.AddCajaAsync(It.IsAny<Caja>()))
            .Callback<Caja>(c => c.Id = 10)
            .Returns(Task.CompletedTask);

        repository
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(true);

        var cajaPersistida = new Caja(dto.Nombre) { Id = 10 };
        repository
            .Setup(x => x.GetCajaByIdAsync(10, false))
            .ReturnsAsync(cajaPersistida);

        PrepararTransaccion(unitOfWork);

        var result = await service.CrearCajaAsync(dto);

        Assert.NotNull(result);
        Assert.Equal(10, result.Id);
        Assert.Equal("Caja Principal", result.Nombre);
        Assert.Equal(EstadoCaja.Inactiva, result.Estado);

        permisos.Verify(x => x.VerificarPermisoAsync(ModuloSistema.Caja, AccionPermiso.Crear), Times.Once);
        permisos.Verify(x => x.VerificarPermisoAsync(ModuloSistema.Caja, AccionPermiso.Ver), Times.Never);
        unitOfWork.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()), Times.Once);
        repository.Verify(x => x.AddCajaAsync(It.Is<Caja>(c => c.Nombre == dto.Nombre)), Times.Once);
        repository.Verify(x => x.SaveChangesAsync(), Times.Once);
        auditoria.Verify(x => x.RegistrarEstrictoAsync(
            ModuloSistema.Caja,
            AccionPermiso.Crear,
            It.Is<string>(s => s.Contains("Caja Principal")),
            10,
            "Caja",
            null,
            null,
            null,
            "Exito",
            null), Times.Once);
    }

    [Fact]
    public async Task CrearCajaAsync_sin_usuario_autenticado_falla_cerrado()
    {
        var (service, _, _, _, _, _) = CrearServicio(autenticado: false);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            service.CrearCajaAsync(new CrearCajaDto { Nombre = "Caja principal" }));
    }

    [Fact]
    public async Task CrearCajaAsync_sin_permiso_crear_falla_antes_de_transaccion()
    {
        var (service, _, unitOfWork, _, _, permisos) = CrearServicio();
        permisos
            .Setup(x => x.VerificarPermisoAsync(ModuloSistema.Caja, AccionPermiso.Crear))
            .ThrowsAsync(new ForbiddenAccessException("Sin permiso para crear Caja."));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            service.CrearCajaAsync(new CrearCajaDto { Nombre = "Caja principal" }));

        permisos.Verify(x => x.VerificarPermisoAsync(
            ModuloSistema.Caja,
            AccionPermiso.Crear), Times.Once);
        unitOfWork.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()), Times.Never);
    }

    [Fact]
    public async Task GetCajaByIdAsync_exige_permiso_ver()
    {
        var caja = new Caja("Caja principal");
        var (service, repository, _, _, _, permisos) = CrearServicio();
        repository.Setup(x => x.GetCajaByIdAsync(1, false)).ReturnsAsync(caja);

        await service.GetCajaByIdAsync(1);

        permisos.Verify(x => x.VerificarPermisoAsync(
            ModuloSistema.Caja,
            AccionPermiso.Ver), Times.Once);
    }

    [Fact]
    public async Task DesactivarCajaAsync_con_sesion_activa_rechaza_sin_mutar_estado()
    {
        var caja = new Caja("Caja principal");
        caja.Activar();
        caja.RegistrarSesionActiva(10);
        var (service, repository, unitOfWork, _, _, _) = CrearServicio();
        repository.Setup(x => x.GetCajaByIdForUpdateAsync(1)).ReturnsAsync(caja);
        PrepararTransaccion(unitOfWork);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.DesactivarCajaAsync(1));
        Assert.Equal(EstadoCaja.Activa, caja.Estado);
        Assert.Equal(10, caja.SesionActivaId);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RegistrarMovimientoAsync_diferencia_durante_operaciones_rechaza_fail_closed()
    {
        var sesion = new CajaSesion(1, 7, 100m) { Id = 1 };
        sesion.IniciarOperaciones();
        var (service, repository, unitOfWork, _, _, _) = CrearServicio();
        repository.Setup(x => x.GetSesionByIdForUpdateAsync(1)).ReturnsAsync(sesion);
        PrepararTransaccion(unitOfWork);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.RegistrarMovimientoAsync(
            1,
            new RegistrarMovimientoCajaDto
            {
                Tipo = TipoMovimientoCaja.DiferenciaFaltante,
                Monto = 5m,
                Referencia = "No permitido durante operaciones"
            }));

        Assert.Empty(sesion.Movimientos);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task IniciarOperacionesAsync_usa_for_update_transaccion_rbac_y_auditoria_estricta()
    {
        var sesion = new CajaSesion(1, 7, 100m) { Id = 1 };
        var (service, repository, unitOfWork, _, auditoria, permisos) = CrearServicio();
        repository.Setup(x => x.GetSesionByIdForUpdateAsync(1)).ReturnsAsync(sesion);
        repository.Setup(x => x.UpdateSesion(It.IsAny<CajaSesion>()));
        repository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        repository.Setup(x => x.GetSesionByIdAsync(1, false)).ReturnsAsync(sesion);
        PrepararTransaccion(unitOfWork);

        var resultado = await service.IniciarOperacionesAsync(1);

        Assert.Equal(EstadoCajaSesion.Operaciones, resultado.Estado);
        repository.Verify(x => x.GetSesionByIdForUpdateAsync(1), Times.Once);
        unitOfWork.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()), Times.Once);
        permisos.Verify(x => x.VerificarPermisoAsync(
            ModuloSistema.Caja,
            AccionPermiso.Actualizar), Times.Once);
        auditoria.Verify(x => x.RegistrarEstrictoAsync(
            ModuloSistema.Caja,
            AccionPermiso.Actualizar,
            It.IsAny<string>(),
            1,
            "CajaSesion",
            null,
            null,
            null,
            "Exito",
            null), Times.Once);
    }

    [Fact]
    public async Task IniciarOperacionesAsync_si_auditoria_estricta_falla_propaga_error()
    {
        var sesion = new CajaSesion(1, 7, 100m) { Id = 1 };
        var (service, repository, unitOfWork, _, auditoria, _) = CrearServicio();
        repository.Setup(x => x.GetSesionByIdForUpdateAsync(1)).ReturnsAsync(sesion);
        repository.Setup(x => x.UpdateSesion(It.IsAny<CajaSesion>()));
        repository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        repository.Setup(x => x.GetSesionByIdAsync(1, false)).ReturnsAsync(sesion);
        PrepararTransaccion(unitOfWork);
        auditoria.Setup(x => x.RegistrarEstrictoAsync(
                ModuloSistema.Caja,
                AccionPermiso.Actualizar,
                It.IsAny<string>(),
                1,
                "CajaSesion",
                null,
                null,
                null,
                "Exito",
                null))
            .ThrowsAsync(new InvalidOperationException("auditoría no disponible"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.IniciarOperacionesAsync(1));

        Assert.Equal("auditoría no disponible", ex.Message);
        unitOfWork.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()), Times.Once);
    }

    private static (
        CajaService Service,
        Mock<ICajaRepository> Repository,
        Mock<IUnitOfWork> UnitOfWork,
        Mock<ICurrentUserService> CurrentUser,
        Mock<IAuditoriaService> Auditoria,
        Mock<IPermisoService> Permisos)
        CrearServicio(bool autenticado = true)
    {
        var repository = new Mock<ICajaRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        currentUser.SetupGet(x => x.EstaAutenticado).Returns(autenticado);
        currentUser.SetupGet(x => x.UsuarioId).Returns(autenticado ? 7 : null);
        permisos
            .Setup(x => x.VerificarPermisoAsync(
                ModuloSistema.Caja,
                It.IsAny<AccionPermiso>()))
            .Returns(Task.CompletedTask);
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
        var service = new CajaService(
            repository.Object,
            currentUser.Object,
            unitOfWork.Object,
            auditoria.Object,
            permisos.Object);
        return (service, repository, unitOfWork, currentUser, auditoria, permisos);
    }

    private static void PrepararTransaccion(Mock<IUnitOfWork> unitOfWork)
    {
        unitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(operation => operation());
    }
}
