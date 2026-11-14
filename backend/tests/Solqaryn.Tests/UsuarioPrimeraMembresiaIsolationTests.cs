using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Xunit;

namespace Solqaryn.Tests;

public sealed class UsuarioPrimeraMembresiaIsolationTests
{
    [Fact]
    public async Task PrimeraAsignacion_SoloElCreadorPuedeRecuperarUsuarioSinEmpresa()
    {
        var http = new DefaultHttpContext();
        await using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("primera-membresia-" + Guid.NewGuid().ToString("N"))
                .Options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });
        var empresaA = new Empresa("Empresa A");
        var empresaB = new Empresa("Empresa B");
        var rol = new Rol { Nombre = "Vendedor", NombreNormalizado = "VENDEDOR", EsSistema = true };
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        db.Roles.Add(rol);
        await db.SaveChangesAsync();

        const int creadorId = 77;
        var pendiente = new Usuario
        {
            NombreUsuario = "nueva-identidad",
            NombreCompleto = "Identidad pendiente",
            PasswordHash = "valor-ficticio",
            RolId = rol.Id,
            CreadoPorUsuarioId = creadorId
        };
        db.Usuarios.Add(pendiente);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var repository = new UsuarioRepository(db);
        Assert.Null(await repository.GetByIdAsync(pendiente.Id));
        Assert.Null(await repository.GetPendientePrimeraMembresiaAsync(pendiente.Id, 78));

        var inicial = await repository.GetPendientePrimeraMembresiaAsync(pendiente.Id, creadorId);
        Assert.NotNull(inicial);
        Assert.Equal(pendiente.Id, inicial!.Id);

        db.UsuarioEmpresas.Add(new UsuarioEmpresa(pendiente.Id, empresaA.Id, rol.Id));
        await db.SaveChangesAsync();
        Assert.Null(await repository.GetPendientePrimeraMembresiaAsync(pendiente.Id, creadorId));
        Assert.Equal(pendiente.Id, (await repository.GetByIdAsync(pendiente.Id))!.Id);

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        Assert.Null(await repository.GetByIdAsync(pendiente.Id));
        Assert.Null(await repository.GetPendientePrimeraMembresiaAsync(pendiente.Id, creadorId));
    }
    [Fact]
    public async Task MembresiaInactiva_SoloSePuedeAdministrarDesdeSuEmpresa_NoDesdeB()
    {
        var http = new DefaultHttpContext();
        await using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("membresia-inactiva-" + Guid.NewGuid().ToString("N"))
                .Options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });
        var empresaA = new Empresa("Empresa A inactiva");
        var empresaB = new Empresa("Empresa B activa");
        var rol = new Rol { Nombre = "Vendedor", NombreNormalizado = "VENDEDOR", EsSistema = true };
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        db.Roles.Add(rol);
        await db.SaveChangesAsync();

        var usuario = new Usuario
        {
            NombreUsuario = "miembro-inactivo",
            NombreCompleto = "Miembro inactivo",
            PasswordHash = "prueba-no-credencial",
            RolId = rol.Id
        };
        var ajeno = new Usuario
        {
            NombreUsuario = "solo-en-b",
            NombreCompleto = "Solo B",
            PasswordHash = "prueba-no-credencial",
            RolId = rol.Id
        };
        db.Usuarios.AddRange(usuario, ajeno);
        await db.SaveChangesAsync();
        var inactiva = new UsuarioEmpresa(usuario.Id, empresaA.Id, rol.Id);
        inactiva.Desactivar();
        db.UsuarioEmpresas.AddRange(
            inactiva,
            new UsuarioEmpresa(usuario.Id, empresaB.Id, rol.Id),
            new UsuarioEmpresa(ajeno.Id, empresaB.Id, rol.Id));
        await db.SaveChangesAsync();

        var repository = new UsuarioRepository(db);
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        // Una membresia inactiva A ya da visibilidad administrativa en A;
        // la identidad que solo pertenece a B sigue totalmente oculta.
        Assert.Equal(usuario.Id, (await repository.GetByIdAsync(usuario.Id))?.Id);
        Assert.Null(await repository.GetByIdAsync(ajeno.Id));
        var visibles = await repository.GetEmpresasAsync(usuario.Id);
        Assert.Single(visibles);
        Assert.False(visibles[0].Activa);
        Assert.Equal(empresaA.Id, visibles[0].EmpresaId);
        Assert.Null(await repository.GetEmpresaAsync(usuario.Id, empresaB.Id));

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        Assert.Equal(usuario.Id, (await repository.GetByIdAsync(usuario.Id))?.Id);
        Assert.Equal(empresaB.Id, (await repository.GetEmpresasAsync(usuario.Id)).Single().EmpresaId);
        Assert.Null(await repository.GetEmpresaAsync(usuario.Id, empresaA.Id));
    }

}
