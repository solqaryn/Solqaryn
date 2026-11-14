using System.Reflection;
using Solqaryn.Infrastructure.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Solqaryn.Tests;

public class N76CWhatsAppProviderPersistenceTests
{
    [Fact]
    public void Migracion_Crea_Configuracion_WhatsApp_TenantSafe_Y_Solo_Referencias_Secretas()
    {
        var migration = new N76CWhatsAppProviderPersistence();
        var builder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql");
        Invoke(migration, "Up", builder);

        var tabla = builder.Operations
            .OfType<CreateTableOperation>()
            .Single(operation => operation.Name == "ConfiguracionesWhatsAppEmpresa");

        Assert.Contains(tabla.Columns, column => column.Name == "EmpresaId" && !column.IsNullable);
        Assert.Contains(tabla.Columns, column => column.Name == "NumeroTelefonoE164" && column.MaxLength == 16);
        Assert.Contains(tabla.Columns, column => column.Name == "TokenSecretoReferencia" && column.MaxLength == 512);
        Assert.Contains(tabla.Columns, column => column.Name == "WebhookSecretoReferencia" && column.MaxLength == 512);
        Assert.DoesNotContain(tabla.Columns, column => column.Name is "AccessToken" or "Token" or "WebhookSecret" or "Secreto");

        var fk = Assert.Single(tabla.ForeignKeys, foreignKey =>
            foreignKey.Name == "FK_ConfiguracionesWhatsAppEmpresa_Empresas_EmpresaId");
        Assert.Equal(new[] { "EmpresaId" }, fk.Columns);
        Assert.Equal("Empresas", fk.PrincipalTable);
        Assert.Equal(ReferentialAction.Restrict, fk.OnDelete);

        var tenantUnique = builder.Operations
            .OfType<CreateIndexOperation>()
            .Single(index => index.Name == "UX_ConfiguracionesWhatsAppEmpresa_EmpresaId");
        Assert.True(tenantUnique.IsUnique);
        Assert.Equal(new[] { "EmpresaId" }, tenantUnique.Columns);

        Assert.Contains(tabla.CheckConstraints, check =>
            check.Name == "CK_ConfiguracionesWhatsAppEmpresa_Token_Referencia" &&
            check.Sql.Contains("://", StringComparison.Ordinal));
        Assert.Contains(tabla.CheckConstraints, check =>
            check.Name == "CK_ConfiguracionesWhatsAppEmpresa_Webhook_Referencia" &&
            check.Sql.Contains("://", StringComparison.Ordinal));
    }

    [Fact]
    public void Migracion_Expone_Concurrencia_Y_Rollback_Aditivo()
    {
        var migration = new N76CWhatsAppProviderPersistence();
        var up = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql");
        Invoke(migration, "Up", up);

        var tabla = up.Operations
            .OfType<CreateTableOperation>()
            .Single(operation => operation.Name == "ConfiguracionesWhatsAppEmpresa");
        var version = tabla.Columns.Single(column => column.Name == "Version");
        Assert.False(version.IsNullable);
        Assert.Equal(1L, version.DefaultValue);

        var down = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql");
        Invoke(migration, "Down", down);
        var drop = Assert.Single(down.Operations.OfType<DropTableOperation>());
        Assert.Equal("ConfiguracionesWhatsAppEmpresa", drop.Name);
        Assert.DoesNotContain(down.Operations, operation => operation is SqlOperation);
    }

    private static void Invoke(Migration migration, string methodName, MigrationBuilder builder)
    {
        var method = migration.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"No se encontró {methodName} en la migración N7.6.C.");

        method.Invoke(migration, new object[] { builder });
    }
}
