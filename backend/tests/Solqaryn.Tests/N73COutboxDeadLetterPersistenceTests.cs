using System.Reflection;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Solqaryn.Tests;

public class N73COutboxDeadLetterPersistenceTests
{
    [Fact]
    public void Persistencia_Existente_Soporta_DeadLetter_Sin_Migracion_Destructiva()
    {
        Assert.Equal(4, (int)EstadoMensajeOutbox.DeadLetter);

        var migration = new N71COutboxPersistence();
        var builder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql");
        Invoke(migration, "Up", builder);

        var tabla = builder.Operations
            .OfType<CreateTableOperation>()
            .Single(operation => operation.Name == "MensajesOutbox");

        var estado = tabla.Columns.Single(column => column.Name == "Estado");
        Assert.False(estado.IsNullable);
        Assert.Equal(0, estado.DefaultValue);

        var estadoValido = tabla.CheckConstraints.Single(check =>
            check.Name == "CK_MensajesOutbox_Estado_Valido");
        Assert.Contains("BETWEEN 0 AND 4", estadoValido.Sql, StringComparison.OrdinalIgnoreCase);

        var idempotencia = builder.Operations
            .OfType<CreateIndexOperation>()
            .Single(index => index.Name == "UX_MensajesOutbox_EmpresaId_ClaveIdempotencia");
        Assert.True(idempotencia.IsUnique);
        Assert.Equal(new[] { "EmpresaId", "ClaveIdempotencia" }, idempotencia.Columns);

        var tenantEstado = builder.Operations
            .OfType<CreateIndexOperation>()
            .Single(index => index.Name == "IX_MensajesOutbox_EmpresaId_Estado_Disponible");
        Assert.Equal(new[] { "EmpresaId", "Estado", "DisponibleDesdeUtc" }, tenantEstado.Columns);
    }

    [Fact]
    public void Rollback_Base_Elimina_Solo_La_Tabla_Outbox()
    {
        var migration = new N71COutboxPersistence();
        var builder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql");
        Invoke(migration, "Down", builder);

        var drop = Assert.Single(builder.Operations.OfType<DropTableOperation>());
        Assert.Equal("MensajesOutbox", drop.Name);
        Assert.DoesNotContain(builder.Operations, operation => operation is SqlOperation);
    }

    private static void Invoke(Migration migration, string methodName, MigrationBuilder builder)
    {
        var method = migration.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"No se encontró {methodName} en la migración outbox.");

        method.Invoke(migration, new object[] { builder });
    }
}
