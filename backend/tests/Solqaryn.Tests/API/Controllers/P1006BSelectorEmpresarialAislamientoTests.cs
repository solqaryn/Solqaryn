using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

/// <summary>
/// P1-006.B: certificacion negativa del selector empresarial.
/// Cada llamada depende de la identidad autenticada y de las membresias
/// vigentes en persistencia, nunca de un ID de empresa solicitado por el cliente.
/// </summary>
public sealed class P1006BSelectorEmpresarialAislamientoTests
{
    [Fact]
    public async Task CambioDeActorRevocacionYSesionAnonimaNuncaEnumeranEmpresasAjenas()
    {
        await using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("p1-006-b-" + Guid.NewGuid().ToString("N"))
                .Options);

        var empresaA = new Empresa("A - Propia");
        var empresaB = new Empresa("B - Membresia revocable");
        var empresaC = new Empresa("C - Ajena");
        db.Set<Empresa>().AddRange(empresaA, empresaB, empresaC);

        var rol = new Rol
        {
            Nombre = "Operador del tenant",
            NombreNormalizado = "P1006B_EMPRESA",
            Ambito = AmbitoAutorizacion.Empresa,
            EsSistema = true,
            Activo = true
        };
        db.Roles.Add(rol);
        await db.SaveChangesAsync();

        var actorA = new Usuario
        {
            NombreUsuario = "p1006b-actor-a",
            NombreCompleto = "Actor A",
            RolId = rol.Id,
            PasswordHash = "fixture-sin-secreto",
            Activo = true
        };
        var actorB = new Usuario
        {
            NombreUsuario = "p1006b-actor-b",
            NombreCompleto = "Actor B",
            RolId = rol.Id,
            PasswordHash = "fixture-sin-secreto",
            Activo = true
        };
        db.Usuarios.AddRange(actorA, actorB);
        await db.SaveChangesAsync();

        var membresiaRevocable = new UsuarioEmpresa(actorA.Id, empresaB.Id, rol.Id);
        db.UsuarioEmpresas.AddRange(
            new UsuarioEmpresa(actorA.Id, empresaA.Id, rol.Id),
            membresiaRevocable,
            new UsuarioEmpresa(actorB.Id, empresaC.Id, rol.Id));
        await db.SaveChangesAsync();

        var jwtA = new Mock<ICurrentUserService>();
        jwtA.SetupGet(x => x.EstaAutenticado).Returns(true);
        jwtA.SetupGet(x => x.UsuarioId).Returns(actorA.Id);
        var servicioA = new UsuarioScopeService(db, jwtA.Object);

        var listaA = await servicioA.ListarEmpresasSeleccionablesAsync();
        Assert.Equal(new[] { empresaA.Id, empresaB.Id },
            listaA.Select(x => x.EmpresaId).OrderBy(x => x).ToArray());
        Assert.DoesNotContain(listaA, x => x.EmpresaId == empresaC.Id);
        Assert.Null(await servicioA.ObtenerActualAsync(empresaC.Id));

        // La misma base no debe mezclar membresias de identidades JWT diferentes.
        var jwtB = new Mock<ICurrentUserService>();
        jwtB.SetupGet(x => x.EstaAutenticado).Returns(true);
        jwtB.SetupGet(x => x.UsuarioId).Returns(actorB.Id);
        var servicioB = new UsuarioScopeService(db, jwtB.Object);
        var listaB = await servicioB.ListarEmpresasSeleccionablesAsync();
        Assert.Equal(empresaC.Id, Assert.Single(listaB).EmpresaId);
        Assert.Null(await servicioB.ObtenerActualAsync(empresaA.Id));
        Assert.Null(await servicioB.ObtenerActualAsync(empresaB.Id));

        // Una revocacion efectiva deja de aparecer en la siguiente consulta.
        membresiaRevocable.Desactivar();
        await db.SaveChangesAsync();
        Assert.Equal(empresaA.Id,
            Assert.Single(await servicioA.ListarEmpresasSeleccionablesAsync()).EmpresaId);
        Assert.Null(await servicioA.ObtenerActualAsync(empresaB.Id));

        // Con un UsuarioId simulado pero sin autenticacion NO hay autorizacion.
        var anonimo = new Mock<ICurrentUserService>();
        anonimo.SetupGet(x => x.UsuarioId).Returns(actorA.Id);
        var sinJwt = new UsuarioScopeService(db, anonimo.Object);
        Assert.Empty(await sinJwt.ListarEmpresasSeleccionablesAsync());
        Assert.Null(await sinJwt.ObtenerActualAsync(empresaA.Id));

        // Una cuenta desactivada tampoco conserva autoridad por un JWT vigente.
        actorA.Activo = false;
        await db.SaveChangesAsync();
        Assert.Empty(await servicioA.ListarEmpresasSeleccionablesAsync());
    }

    [Fact]
    public void RutaDeListadoPermaneceProtegidaYSinParametroEmpresaDelCliente()
    {
        var controller = typeof(TenantContextController);
        Assert.Contains(controller.GetCustomAttributes(inherit: true),
            atributo => atributo is AuthorizeAttribute);

        var endpoint = controller.GetMethod(nameof(TenantContextController.ListarEmpresas));
        Assert.NotNull(endpoint);
        Assert.Contains(endpoint!.GetCustomAttributes(inherit: true),
            atributo => atributo is HttpGetAttribute get && get.Template == "empresas");
        Assert.Single(endpoint.GetParameters());
        Assert.Equal(typeof(CancellationToken), endpoint.GetParameters()[0].ParameterType);
    }
}
