using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Solqaryn.Infrastructure.Persistence;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations.Oracle;

/// <summary>Corrective P01/P02 storage. No legacy attribution, seed, grants or data migration.</summary>
[DbContext(typeof(AppDbContext))]
[Migration("20261126165100_V44CaseFileOwnershipStore")]
public sealed class V44CaseFileOwnershipStore : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE V5CaseFileOwnerships (
              EmpresaId INT NOT NULL,
              Id INT NOT NULL,
              SucursalId INT NOT NULL,
              RecursoTipo VARCHAR(64) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
              RecursoRef VARCHAR(128) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
              PRIMARY KEY (EmpresaId, Id),
              KEY IX_V5CaseFileOwnership_Branch (EmpresaId, SucursalId),
              KEY IX_V5CaseFileOwnership_Resource (EmpresaId, RecursoTipo, RecursoRef),
              CONSTRAINT FK_V5CaseFileOwnership_Empresa FOREIGN KEY (EmpresaId)
                REFERENCES Empresas (Id) ON DELETE RESTRICT,
              CONSTRAINT FK_V5CaseFileOwnership_Sucursal FOREIGN KEY (EmpresaId, SucursalId)
                REFERENCES Sucursales (EmpresaId, Id) ON DELETE RESTRICT,
              CONSTRAINT CK_V5CaseFileOwnership_Positive CHECK (EmpresaId > 0 AND Id > 0 AND SucursalId > 0)
            ) ENGINE=InnoDB;
            """);
        migrationBuilder.Sql("""
            CREATE TABLE V5CaseFileVersions (
              EmpresaId INT NOT NULL,
              ExpedienteId INT NOT NULL,
              Version BIGINT NOT NULL,
              ActorId INT NOT NULL,
              ContentHash CHAR(64) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
              ReasonCode VARCHAR(64) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
              FechaUtc DATETIME(6) NOT NULL,
              PRIMARY KEY (EmpresaId, ExpedienteId, Version),
              KEY IX_V5CaseFileVersion_Actor (ActorId),
              CONSTRAINT FK_V5CaseFileVersion_Ownership FOREIGN KEY (EmpresaId, ExpedienteId)
                REFERENCES V5CaseFileOwnerships (EmpresaId, Id) ON DELETE RESTRICT,
              CONSTRAINT FK_V5CaseFileVersion_Actor FOREIGN KEY (ActorId)
                REFERENCES Usuarios (Id) ON DELETE RESTRICT,
              CONSTRAINT CK_V5CaseFileVersion_Positive CHECK (Version > 0 AND ActorId > 0)
            ) ENGINE=InnoDB;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Empty disposable schema only. Never purge retained evidence on downgrade.
        // MySQL DDL implicitly commits; this is not a transactional DDL rollback.
        migrationBuilder.Sql("CREATE TEMPORARY TABLE V5CaseFileEmptyGuard (N BIGINT NOT NULL CHECK (N=0));");
        migrationBuilder.Sql("""
            INSERT INTO V5CaseFileEmptyGuard (N)
            SELECT (SELECT COUNT(*) FROM V5CaseFileVersions) +
                   (SELECT COUNT(*) FROM V5CaseFileOwnerships);
            """);
        migrationBuilder.Sql("DROP TABLE V5CaseFileVersions;");
        migrationBuilder.Sql("DROP TABLE V5CaseFileOwnerships;");
        migrationBuilder.Sql("DROP TEMPORARY TABLE V5CaseFileEmptyGuard;");
    }
}
