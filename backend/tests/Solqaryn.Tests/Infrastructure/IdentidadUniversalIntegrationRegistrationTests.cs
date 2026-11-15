using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Domain.Security;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

public sealed class IdentidadUniversalIntegrationRegistrationTests
{
    private static ServiceProvider CrearProveedor()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("identidad-integracion-" + Guid.NewGuid().ToString("N")));
        services.AddIdentidadUniversalIntegration();
        return services.BuildServiceProvider(validateScopes: true);
    }

    [Fact]
    public async Task DI_resuelve_fuente_y_servicios_scoped_sin_compartir_contextos()
    {
        await using var provider = CrearProveedor();
        await using var first = provider.CreateAsyncScope();
        await using var second = provider.CreateAsyncScope();
        Assert.IsType<IdentidadUniversalSnapshotEfSource>(
            first.ServiceProvider.GetRequiredService<IIdentidadUniversalSnapshotSource>());
        Assert.Same(
            first.ServiceProvider.GetRequiredService<IIdentidadUniversalSnapshotSource>(),
            first.ServiceProvider.GetRequiredService<IIdentidadUniversalSnapshotSource>());
        Assert.NotSame(
            first.ServiceProvider.GetRequiredService<IIdentidadUniversalSnapshotSource>(),
            second.ServiceProvider.GetRequiredService<IIdentidadUniversalSnapshotSource>());
        Assert.NotNull(first.ServiceProvider.GetRequiredService<IdentidadUniversalApplicationService>());
        Assert.NotNull(first.ServiceProvider.GetRequiredService<IdentidadUniversalAuthorizationGuard>());
    }

    [Fact]
    public async Task Integracion_real_denegada_para_tenant_B_y_membresia_revocada()
    {
        await using var provider = CrearProveedor();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rol = new Rol
        {
            Nombre = "Operador empresa",
            NombreNormalizado = "OPERADOR_EMPRESA",
            Ambito = AmbitoAutorizacion.Empresa,
            EsSistema = true,
            Activo = true
        };
        var a = new Empresa("Empresa A");
        var b = new Empresa("Empresa B");
        db.Set<Rol>().Add(rol);
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();

        var usuario = new Usuario
        {
            NombreUsuario = "integracion-a",
            NombreCompleto = "Actor A",
            PasswordHash = "fixture-no-real",
            RolId = 999,
            Activo = true
        };
        db.Set<Usuario>().Add(usuario);
        await db.SaveChangesAsync();
        var membresia = new UsuarioEmpresa(usuario.Id, a.Id, rol.Id);
        db.Set<UsuarioEmpresa>().Add(membresia);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var autenticado = IdentidadUniversalReglas.CrearContextoVerificado(
            usuario, a, membresia, rol);
        var resolver = scope.ServiceProvider
            .GetRequiredService<IdentidadUniversalApplicationService>();
        var actual = await resolver.ResolverActualAsync(autenticado, a.Id);
        Assert.Equal(a.Id, actual.EmpresaId);
        Assert.Equal(rol.Id, actual.RolId);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            resolver.ResolverActualAsync(autenticado, b.Id));

        var guardia = scope.ServiceProvider
            .GetRequiredService<IdentidadUniversalAuthorizationGuard>();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            guardia.ExigirAccesoActualAsync(
                autenticado, a.Id,
                Enum.GetValues<ModuloSistema>()[0],
                Enum.GetValues<AccionPermiso>()[0]));

        var actualMembresia = await db.Set<UsuarioEmpresa>()
            .SingleAsync(m => m.Id == membresia.Id);
        actualMembresia.Desactivar();
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            resolver.ResolverActualAsync(autenticado, a.Id));
    }
}
