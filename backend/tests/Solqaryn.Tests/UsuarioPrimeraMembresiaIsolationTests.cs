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
        var rol = new Rol { Nombre = "Vendedor", NombreNormalizado = "VENDEDOR" };
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
}
