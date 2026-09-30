using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Solqaryn.Tests;

public sealed class SuscripcionesSaaSPersistenceContractTests
{
    [Fact]
    public void Modelo_PersistePlanLimitesYSuscripcionTenantConIntegridadRelacional()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n69c-{Guid.NewGuid():N}")
            .Options;

        using var db = new AppDbContext(options);
        var model = db.Model;

        var plan = Assert.IsAssignableFrom<IEntityType>(model.FindEntityType(typeof(Plan)));
        Assert.Equal("Planes", plan.GetTableName());
        Assert.Contains(plan.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(Plan.Codigo) }));

        var limite = Assert.Single(model.GetEntityTypes().Where(x => x.ClrType == typeof(PlanLimite)));
        Assert.True(limite.IsOwned());
        Assert.Equal("PlanLimites", limite.GetTableName());
        Assert.Equal(
            new[] { "PlanId", nameof(PlanLimite.Clave) },
            limite.FindPrimaryKey()!.Properties.Select(p => p.Name).ToArray());

        var limiteFk = Assert.Single(limite.GetForeignKeys());
        Assert.Equal(typeof(Plan), limiteFk.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Cascade, limiteFk.DeleteBehavior);

        var suscripcion = Assert.IsAssignableFrom<IEntityType>(model.FindEntityType(typeof(Suscripcion)));
        Assert.Equal("Suscripciones", suscripcion.GetTableName());
        Assert.Contains(suscripcion.GetIndexes(), index =>
            index.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(Suscripcion.EmpresaId), nameof(Suscripcion.Estado) }));
        Assert.Contains(suscripcion.GetIndexes(), index =>
            index.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(Suscripcion.PlanId), nameof(Suscripcion.Estado) }));

        var fks = suscripcion.GetForeignKeys().ToArray();
        Assert.Contains(fks, fk =>
            fk.PrincipalEntityType.ClrType == typeof(Empresa) &&
            fk.Properties.Single().Name == nameof(Suscripcion.EmpresaId) &&
            fk.DeleteBehavior == DeleteBehavior.Restrict);
        Assert.Contains(fks, fk =>
            fk.PrincipalEntityType.ClrType == typeof(Plan) &&
            fk.Properties.Single().Name == nameof(Suscripcion.PlanId) &&
            fk.DeleteBehavior == DeleteBehavior.Restrict);
    }
}
