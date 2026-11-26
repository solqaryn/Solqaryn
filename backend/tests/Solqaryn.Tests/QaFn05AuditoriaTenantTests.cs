using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Xunit;

namespace Solqaryn.Tests;

public sealed class QaFn05AuditoriaTenantTests
{
    [Fact]
    public async Task Consulta_ExigeAdministradorDeMembresiaVigente_NoClaimGlobal()
    {
        var service = new Mock<IAuditoriaService>(MockBehavior.Strict);
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        var filter = new AuditoriaFiltroDto();
        var http = new DefaultHttpContext();
        http.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"] = 12;
        http.Request.Headers["X-Empresa-Id"] = "11";
        var controller = new AuditoriaController(service.Object, scope.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };

        scope.Setup(x => x.ObtenerActualAsync(12, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(1, 12, 2, "Operador", false));
        Assert.IsType<ForbidResult>(await controller.GetFiltered(filter));
        service.VerifyNoOtherCalls();

        scope.Setup(x => x.ObtenerActualAsync(12, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsuarioTenantScopeActual?)null);
        Assert.IsType<ForbidResult>(await controller.GetFiltered(filter));
        service.VerifyNoOtherCalls();

        scope.Setup(x => x.ObtenerActualAsync(12, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(1, 12, 3, "Administrador B", true));
        service.Setup(x => x.GetFilteredAsync(filter))
            .ReturnsAsync(new PagedResult<RegistroAuditoriaDto> { Items = new List<RegistroAuditoriaDto>(), Page = 1, PageSize = 50 });
        Assert.IsType<OkObjectResult>(await controller.GetFiltered(filter));
        service.Verify(x => x.GetFilteredAsync(filter), Times.Once);

        http.Items.Clear();
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => controller.GetFiltered(filter));
    }

    [Fact]
    public async Task Repositorio_FiltraAntesDeContarYPaginar_YRegistroUsaSoloAutoridadVerificada()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"qa-audit-{Guid.NewGuid():N}").Options;
        await using var setup = new AppDbContext(options);
        setup.Set<Empresa>().AddRange(new Empresa("A") { Id = 11 }, new Empresa("B") { Id = 12 });
        setup.RegistrosAuditoria.AddRange(
            new RegistroAuditoria { Id = 1, EmpresaId = 11, AmbitoAutoridad = AmbitoAutorizacion.Empresa, NombreUsuario = "A", Descripcion = "A" },
            new RegistroAuditoria { Id = 2, EmpresaId = 12, AmbitoAutoridad = AmbitoAutorizacion.Empresa, NombreUsuario = "B", Descripcion = "B" },
            new RegistroAuditoria { Id = 3, EmpresaId = 11, AmbitoAutoridad = AmbitoAutorizacion.Plataforma, NombreUsuario = "plataforma", Descripcion = "plataforma" },
            new RegistroAuditoria { Id = 4, NombreUsuario = "legacy", Descripcion = "legacy" });
        await setup.SaveChangesAsync();
        var http = new DefaultHttpContext();
        http.Request.Headers["X-Empresa-Id"] = "12";
        var accessor = new HttpContextAccessor { HttpContext = http };
        await using var db = new AppDbContext(options, httpContextAccessor: accessor);
        var repo = new AuditoriaRepository(db);
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => repo.GetFilteredAsync(new AuditoriaFiltroDto()));
        http.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"] = 11;
        var result = await repo.GetFilteredAsync(new AuditoriaFiltroDto { PageSize = 1 });
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, Assert.Single(result.Items).Id);
        var foreign = await repo.GetFilteredAsync(new AuditoriaFiltroDto { Texto = "B" });
        Assert.Equal(0, foreign.TotalCount);
        Assert.Empty(foreign.Items);
        var user = new Mock<ICurrentUserService>();
        user.SetupGet(x => x.NombreUsuario).Returns("fixture-a");
        var service = new AuditoriaService(repo, user.Object, accessor, NullLogger<AuditoriaService>.Instance);
        await service.RegistrarEstrictoAsync(ModuloSistema.Configuracion, AccionPermiso.Editar, "Marca de prueba A");
        var written = await db.RegistrosAuditoria.SingleAsync(x => x.Descripcion == "Marca de prueba A");
        Assert.Equal(11, written.EmpresaId);
        Assert.Equal(AmbitoAutorizacion.Empresa, written.AmbitoAutoridad);
        Assert.Equal(OrigenAutorizacion.MembresiaEmpresa, written.OrigenAutorizacion);
        http.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"] = 12;
        result = await repo.GetFilteredAsync(new AuditoriaFiltroDto());
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(2, Assert.Single(result.Items).Id);
    }
}
