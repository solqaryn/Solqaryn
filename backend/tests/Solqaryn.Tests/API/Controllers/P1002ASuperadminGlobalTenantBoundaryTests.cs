using Microsoft.EntityFrameworkCore;
using Moq;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

/// <summary>
/// P1-002.A: el permiso SaaS global no concede, por implicacion,
/// lectura comercial de los tenants A/B.
/// Prueba dirigida de servicios reales con EF InMemory: NO certifica
/// la seleccion HTTP con motivo ni una delegacion multiempresa.
/// </summary>
public sealed class P1002ASuperadminGlobalTenantBoundaryTests
{
    [Fact]
    public async Task AdministradorGlobalSinMembresiasNoObtieneContextoComercialDeAODeB()
    {
        await using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("p1-002-a-global-" + Guid.NewGuid().ToString("N"))
                .Options);

        var rolGlobal = new Rol
        {
            Nombre = "Administrador plataforma",
            NombreNormalizado = "P1002_GLOBAL",
            Ambito = AmbitoAutorizacion.Plataforma,
            EmpresaId = null,
            EsAdministrador = true,
            EsSistema = true,
            Activo = true
        };
        var rolEmpresa = new Rol
        {
            Nombre = "Administrador empresa",
            NombreNormalizado = "P1002_EMPRESA",
            Ambito = AmbitoAutorizacion.Empresa,
            EmpresaId = null,
            EsAdministrador = true,
            EsSistema = true,
            Activo = true
        };
        db.Roles.AddRange(rolGlobal, rolEmpresa);
        var empresaA = new Empresa("Empresa A");
        var empresaB = new Empresa("Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        var usuario = new Usuario
        {
            NombreUsuario = "operador-global",
            NombreCompleto = "Operador global de prueba",
            RolId = rolEmpresa.Id,
            PasswordHash = "solo-fixture-sin-credencial",
            Activo = true
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        db.Set<UsuarioRolPlataforma>().Add(
            new UsuarioRolPlataforma(usuario.Id, rolGlobal.Id));
        await db.SaveChangesAsync();

        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.EstaAutenticado).Returns(true);
        current.SetupGet(x => x.UsuarioId).Returns(usuario.Id);
        current.SetupGet(x => x.NombreUsuario).Returns(usuario.NombreUsuario);

        var plataforma = new PlataformaScopeService(db, current.Object);
        var comercial = new UsuarioScopeService(db, current.Object);

        // Control positivo: existe autoridad SaaS, sin necesidad de
        // membresia empresarial ni de inferir una empresa implicita.
        Assert.NotNull(await plataforma.ObtenerActualAsync());

        // Negativo obligatorio: ser superadmin no hace que el tenant A,
        // el tenant B ni el selector de empresa unica se autoricen solos.
        Assert.Null(await comercial.ObtenerActualAsync(empresaA.Id));
        Assert.Null(await comercial.ObtenerActualAsync(empresaB.Id));
        Assert.Null(await comercial.ObtenerUnicoActualAsync());

        // Una membresia explicita en A tampoco concede acceso a B.
        db.Set<UsuarioEmpresa>().Add(
            new UsuarioEmpresa(usuario.Id, empresaA.Id, rolEmpresa.Id));
        await db.SaveChangesAsync();
        Assert.Null(await comercial.ObtenerActualAsync(empresaB.Id));

        // No se crearon usuarios, permisos o membresias para B.
        Assert.False(await db.Set<UsuarioEmpresa>()
            .AnyAsync(m => m.UsuarioId == usuario.Id && m.EmpresaId == empresaB.Id));
    }
}
