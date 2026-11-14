using System.Reflection;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Migrations;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Solqaryn.Tests;

public sealed class PlatformAuthorityFoundationTests
{
    [Fact]
    public void UsuarioRolPlataforma_es_independiente_de_empresa_y_activo_por_defecto()
    {
        var asignacion = new UsuarioRolPlataforma(usuarioId: 7, rolId: 9);

        Assert.Equal(7, asignacion.UsuarioId);
        Assert.Equal(9, asignacion.RolId);
        Assert.True(asignacion.Activa);

        asignacion.Desactivar();
        Assert.False(asignacion.Activa);

        asignacion.Activar();
        asignacion.CambiarRol(10);
        Assert.True(asignacion.Activa);
        Assert.Equal(10, asignacion.RolId);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public void UsuarioRolPlataforma_rechaza_identificadores_invalidos(int usuarioId, int rolId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new UsuarioRolPlataforma(usuarioId, rolId));
    }

    [Fact]
    public void Modelo_EF_separa_autoridad_de_empresa_y_plataforma()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"platform-authority-{Guid.NewGuid()}")
            .Options;

        using var db = new AppDbContext(options);

        var rol = db.Model.FindEntityType(typeof(Rol));
        var permiso = db.Model.FindEntityType(typeof(Permiso));
        var plataforma = db.Model.FindEntityType(typeof(UsuarioRolPlataforma));
        var auditoria = db.Model.FindEntityType(typeof(RegistroAuditoria));

        Assert.NotNull(rol?.FindProperty(nameof(Rol.Ambito)));
        Assert.NotNull(rol?.FindProperty(nameof(Rol.EmpresaId)));
        Assert.NotNull(permiso?.FindProperty(nameof(Permiso.Ambito)));
        Assert.NotNull(auditoria?.FindProperty(nameof(RegistroAuditoria.EmpresaId)));
        Assert.NotNull(auditoria?.FindProperty(nameof(RegistroAuditoria.AmbitoAutoridad)));
        Assert.NotNull(auditoria?.FindProperty(nameof(RegistroAuditoria.OrigenAutorizacion)));

        Assert.NotNull(plataforma);
        Assert.Null(plataforma!.FindProperty("EmpresaId"));

        var uniqueGrant = Assert.Single(
            plataforma.GetIndexes(),
            index => index.IsUnique &&
                     index.Properties.Select(p => p.Name)
                         .SequenceEqual(new[]
                         {
                             nameof(UsuarioRolPlataforma.UsuarioId),
                             nameof(UsuarioRolPlataforma.RolId)
                         }));
        Assert.True(uniqueGrant.IsUnique);
    }

    [Fact]
    public void Entidades_existentes_conservan_ambito_empresarial_por_defecto()
    {
        var rol = new Rol();
        var permiso = new Permiso();

        Assert.Equal(AmbitoAutorizacion.Empresa, rol.Ambito);
        Assert.Equal(AmbitoAutorizacion.Empresa, permiso.Ambito);
    }

    [Fact]
    public void Migracion_es_aditiva_y_no_concede_superadministrador()
    {
        var migration = new PlatformAuthorityFoundation();
        var up = typeof(PlatformAuthorityFoundation)
            .GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(up);

        var builder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql");
        up!.Invoke(migration, new object[] { builder });

        Assert.Contains(builder.Operations.OfType<AddColumnOperation>(),
            op => op.Table == "Roles" && op.Name == "Ambito");
        Assert.Contains(builder.Operations.OfType<AddColumnOperation>(),
            op => op.Table == "Permisos" && op.Name == "Ambito");
        Assert.Contains(builder.Operations.OfType<CreateTableOperation>(),
            op => op.Name == "UsuarioRolesPlataforma");

        Assert.Empty(builder.Operations.OfType<SqlOperation>());
        Assert.DoesNotContain(builder.Operations.OfType<InsertDataOperation>(), _ => true);
    }
}
