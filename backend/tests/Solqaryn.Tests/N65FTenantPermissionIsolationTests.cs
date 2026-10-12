using Solqaryn.API.Filters;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N65FTenantPermissionIsolationTests
{
    [Fact]
    public async Task RequierePermiso_falla_cerrado_sin_empresa_solicitada()
    {
        var fake = new TenantAwarePermisoService();
        var context = CreateContext(fake);
        var filter = new RequierePermisoAttribute(ModuloSistema.Usuarios, AccionPermiso.Ver);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            filter.OnActionExecutionAsync(context, CreateNext(context)));

        Assert.Null(fake.EmpresaVerificada);
        Assert.False(fake.LegacyInvocado);
    }

    [Fact]
    public async Task RequierePermiso_resuelve_unica_membresia_activa_sin_header()
    {
        var fake = new TenantAwarePermisoService();
        var contextoUnico = new UsuarioTenantScopeActual(
            UsuarioId: 7,
            EmpresaId: 31,
            RolId: 5,
            RolNombre: "Administrador",
            EsAdministrador: true);
        var context = CreateContext(fake, contextoUnico: contextoUnico);
        var filter = new RequierePermisoAttribute(ModuloSistema.Usuarios, AccionPermiso.Ver);
        var nextInvoked = false;

        await filter.OnActionExecutionAsync(context, async () =>
        {
            nextInvoked = true;
            return await Task.FromResult(new ActionExecutedContext(
                context,
                new List<IFilterMetadata>(),
                context.Controller));
        });

        Assert.True(nextInvoked);
        Assert.Equal(31, fake.EmpresaVerificada);
        Assert.Equal(31, context.HttpContext.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"]);
        Assert.False(fake.LegacyInvocado);
    }

    [Fact]
    public async Task RequierePermiso_valida_el_rol_de_la_membresia_del_tenant_solicitado()
    {
        var fake = new TenantAwarePermisoService();
        var context = CreateContext(fake, empresaId: 17);
        var filter = new RequierePermisoAttribute(ModuloSistema.Usuarios, AccionPermiso.Ver);
        var nextInvoked = false;

        await filter.OnActionExecutionAsync(context, async () =>
        {
            nextInvoked = true;
            return await Task.FromResult(new ActionExecutedContext(
                context,
                new List<IFilterMetadata>(),
                context.Controller));
        });

        Assert.True(nextInvoked);
        Assert.Equal(17, fake.EmpresaVerificada);
        Assert.Equal(17, context.HttpContext.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"]);
        Assert.False(fake.LegacyInvocado);
    }

    [Fact]
    public async Task RequiereAlgunoPermiso_nunca_consulta_el_scope_legacy_global()
    {
        var fake = new TenantAwarePermisoService();
        var context = CreateContext(fake, empresaId: 23);
        var filter = new RequiereAlgunoPermisoAttribute(
            ModuloSistema.Inventario,
            AccionPermiso.Ver,
            AccionPermiso.Exportar);

        await filter.OnActionExecutionAsync(context, CreateNext(context));

        Assert.Equal(23, fake.EmpresaVerificada);
        Assert.Equal(23, context.HttpContext.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"]);
        Assert.False(fake.LegacyInvocado);
    }

    private static ActionExecutingContext CreateContext(
        TenantAwarePermisoService permisoService,
        int? empresaId = null,
        UsuarioTenantScopeActual? contextoUnico = null)
    {
        var services = new ServiceCollection()
            .AddSingleton<IPermisoService>(permisoService)
            .AddSingleton<IUsuarioScopeService>(new FixedUsuarioScopeService(contextoUnico))
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services
        };

        if (empresaId.HasValue)
            httpContext.Request.Headers["X-Empresa-Id"] = empresaId.Value.ToString();

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());
    }

    private static ActionExecutionDelegate CreateNext(ActionExecutingContext context) => () =>
        Task.FromResult(new ActionExecutedContext(
            context,
            new List<IFilterMetadata>(),
            context.Controller));

    private sealed class FixedUsuarioScopeService : IUsuarioScopeService
    {
        private readonly UsuarioTenantScopeActual? _contextoUnico;

        public FixedUsuarioScopeService(UsuarioTenantScopeActual? contextoUnico)
        {
            _contextoUnico = contextoUnico;
        }

        public Task<UsuarioScopeActual?> ObtenerActualAsync() =>
            Task.FromResult<UsuarioScopeActual?>(null);

        public Task<UsuarioTenantScopeActual?> ObtenerActualAsync(
            int empresaId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<UsuarioTenantScopeActual?>(null);

        public Task<UsuarioTenantScopeActual?> ObtenerUnicoActualAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_contextoUnico);
    }

    private sealed class TenantAwarePermisoService : IPermisoService
    {
        public int? EmpresaVerificada { get; private set; }
        public bool LegacyInvocado { get; private set; }

        public Task<List<PermisoMatrizItemDto>> GetMatrizAsync(int rolId) =>
            Task.FromResult(new List<PermisoMatrizItemDto>());

        public Task<List<PermisoMatrizItemDto>> UpdateMatrizAsync(
            int rolId,
            UpdatePermisoMatrizDto dto) =>
            Task.FromResult(new List<PermisoMatrizItemDto>());

        public Task PrecargarMatrizPorDefectoAsync(int rolId, bool esAdministrador) =>
            Task.CompletedTask;

        public Task<MisPermisosDto> GetMisPermisosAsync() =>
            Task.FromResult(new MisPermisosDto());

        public Task<MisPermisosDto> GetMisPermisosAsync(int empresaId) =>
            Task.FromResult(new MisPermisosDto());

        public Task<bool> TienePermisoAsync(ModuloSistema modulo, AccionPermiso accion)
        {
            LegacyInvocado = true;
            throw new InvalidOperationException("El scope legacy no debe autorizar flujos tenant-owned.");
        }

        public Task<bool> TienePermisoAsync(
            int empresaId,
            ModuloSistema modulo,
            AccionPermiso accion)
        {
            EmpresaVerificada = empresaId;
            return Task.FromResult(true);
        }

        public Task VerificarPermisoAsync(ModuloSistema modulo, AccionPermiso accion)
        {
            LegacyInvocado = true;
            throw new InvalidOperationException("El scope legacy no debe autorizar flujos tenant-owned.");
        }

        public Task VerificarPermisoAsync(
            int empresaId,
            ModuloSistema modulo,
            AccionPermiso accion)
        {
            EmpresaVerificada = empresaId;
            return Task.CompletedTask;
        }
    }
}
