using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Solqaryn.Tests;

public class N71COutboxPersistenceModelTests
{
    [Fact]
    public void Modelo_Outbox_Blinda_Tenant_Idempotencia_Y_Claim_Durable()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n71c-outbox-{Guid.NewGuid():N}")
            .Options;

        using var context = new AppDbContext(options);
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(MensajeOutbox));

        Assert.NotNull(entity);
        Assert.Equal("MensajesOutbox", entity!.GetTableName());
        Assert.Equal(160, entity.FindProperty(nameof(MensajeOutbox.TipoEvento))!.GetMaxLength());
        Assert.Equal(200, entity.FindProperty(nameof(MensajeOutbox.ClaveIdempotencia))!.GetMaxLength());
        var payload = entity.FindProperty(nameof(MensajeOutbox.PayloadJson));
        Assert.NotNull(payload);
        // HasColumnType is a relational annotation; avoid provider-specific type-mapping casts in this model test.
        Assert.Equal("longtext", payload!.FindAnnotation("Relational:ColumnType")?.Value);

        var idempotency = Assert.Single(entity.GetIndexes().Where(index =>
            index.Properties.Select(property => property.Name).SequenceEqual(new[]
            {
                nameof(MensajeOutbox.EmpresaId),
                nameof(MensajeOutbox.ClaveIdempotencia)
            })));
        Assert.True(idempotency.IsUnique);
        Assert.Equal("UX_MensajesOutbox_EmpresaId_ClaveIdempotencia", idempotency.GetDatabaseName());

        var evento = Assert.Single(entity.GetIndexes().Where(index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(MensajeOutbox.EventoId) })));
        Assert.True(evento.IsUnique);

        var claim = Assert.Single(entity.GetIndexes().Where(index =>
            index.Properties.Select(property => property.Name).SequenceEqual(new[]
            {
                nameof(MensajeOutbox.Estado),
                nameof(MensajeOutbox.DisponibleDesdeUtc),
                nameof(MensajeOutbox.Id)
            })));
        Assert.Equal("IX_MensajesOutbox_Claim", claim.GetDatabaseName());

        var empresa = Assert.Single(entity.GetForeignKeys());
        Assert.Equal(typeof(Empresa), empresa.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Restrict, empresa.DeleteBehavior);

        var checks = entity.GetCheckConstraints().Select(check => check.Name).ToHashSet();
        Assert.Contains("CK_MensajesOutbox_EmpresaId_Positivo", checks);
        Assert.Contains("CK_MensajesOutbox_Intentos_NoNegativo", checks);
        Assert.Contains("CK_MensajesOutbox_Estado_Valido", checks);
        Assert.Contains("CK_MensajesOutbox_ClaveIdempotencia_NoVacia", checks);
        Assert.Contains("CK_MensajesOutbox_Payload_NoVacio", checks);
        Assert.Contains("CK_MensajesOutbox_TipoEvento_NoVacio", checks);
    }
}
