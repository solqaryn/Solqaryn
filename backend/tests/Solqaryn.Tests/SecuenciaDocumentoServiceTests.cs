using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.API.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class SecuenciaDocumentoServiceTests
{
    [Fact]
    public async Task ObtenerAsync_DevuelveSecuenciaDelTenantYFormatoActual()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n66d-{Guid.NewGuid():N}")
            .Options;

        await using var db = new AppDbContext(options);
        db.SecuenciasDocumento.Add(new SecuenciaDocumento(1, null, "factura", "FAC-", 6, 41));
        await db.SaveChangesAsync();

        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(x => x.ObtenerActualAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(10, 1, 20, "Administrador", true));

        var service = new SecuenciaDocumentoService(db, scope.Object);
        var result = await service.ObtenerAsync(1, null, " factura ");

        Assert.Equal(1, result.EmpresaId);
        Assert.Equal("FACTURA", result.TipoDocumento);
        Assert.Equal(41, result.UltimoValor);
        Assert.Equal("FAC-000041", result.UltimoNumeroFormateado);
    }

    [Fact]
    public async Task ObtenerAsync_FallaCerradoSinMembresiaTenantActiva()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n66d-denied-{Guid.NewGuid():N}")
            .Options;

        await using var db = new AppDbContext(options);
        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(x => x.ObtenerActualAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsuarioTenantScopeActual?)null);

        var service = new SecuenciaDocumentoService(db, scope.Object);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.ObtenerAsync(2, null, "FACTURA"));

        Assert.Contains("no tiene acceso", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReservarSiguienteAsync_FallaCerradoSinMembresiaTenantActiva()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n66f-reserve-denied-{Guid.NewGuid():N}")
            .Options;

        await using var db = new AppDbContext(options);
        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(x => x.ObtenerActualAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsuarioTenantScopeActual?)null);

        var service = new SecuenciaDocumentoService(db, scope.Object);
        var request = new ReservarSecuenciaDocumentoRequest(2, null, "FACTURA");

        var error = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.ReservarSiguienteAsync(request));

        Assert.Contains("no tiene acceso", error.Message, StringComparison.OrdinalIgnoreCase);
        scope.Verify(
            x => x.ObtenerActualAsync(2, It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Empty(db.RegistrosAuditoria);
    }

    [Fact]
    public async Task ReservarSiguienteAsync_PersisteAuditoriaConLaReserva()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        connection.CreateFunction<string?, int?>("CHAR_LENGTH", value => value?.Length);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var empresa = new Empresa("Empresa audit test");
        db.Set<Empresa>().Add(empresa);
        await db.SaveChangesAsync();

        db.SecuenciasDocumento.Add(new SecuenciaDocumento(empresa.Id, null, "FACTURA", "FAC-", 6, 41));
        await db.SaveChangesAsync();

        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(x => x.ObtenerActualAsync(empresa.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(10, empresa.Id, 20, "Administrador", true));

        var service = new SecuenciaDocumentoService(db, scope.Object);
        await service.ReservarSiguienteAsync(new ReservarSecuenciaDocumentoRequest(empresa.Id, null, "FACTURA"));

        db.ChangeTracker.Clear();
        var secuencia = await db.SecuenciasDocumento.SingleAsync();
        var auditoria = await db.RegistrosAuditoria.SingleAsync();

        Assert.Equal(42, secuencia.UltimoValor);
        Assert.Equal(10, auditoria.UsuarioId);
        Assert.Equal(empresa.Id, auditoria.EmpresaId);
        Assert.Equal(AmbitoAutorizacion.Empresa, auditoria.AmbitoAutoridad);
        Assert.Equal(OrigenAutorizacion.MembresiaEmpresa, auditoria.OrigenAutorizacion);
        Assert.Equal(nameof(SecuenciaDocumento), auditoria.Entidad);
        Assert.Equal(secuencia.Id, auditoria.ReferenciaId);
        Assert.Equal("Exito", auditoria.Resultado);
        Assert.Contains($"\"empresaId\":{empresa.Id}", auditoria.ValoresNuevos ?? string.Empty, StringComparison.Ordinal);
        Assert.Contains("\"valorSiguiente\":42", auditoria.ValoresNuevos ?? string.Empty, StringComparison.Ordinal);
    }
    [Fact]
    public async Task Controller_RechazaConsultaYReservaDeEmpresaDistintaAntesDePersistencia()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"qa-fn05-sequence-{Guid.NewGuid():N}")
            .Options;
        await using var db = new AppDbContext(options);
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        var controller = new SecuenciasDocumentoController(db, scope.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.HttpContext.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"] = 11;

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            controller.Get(12, null, "FACTURA", CancellationToken.None));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            controller.ReservarSiguiente(
                new ReservarSecuenciaDocumentoRequest(12, null, "FACTURA"), CancellationToken.None));
        scope.VerifyNoOtherCalls();
        Assert.Empty(db.RegistrosAuditoria);
    }
}
