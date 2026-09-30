using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Persistence.Migrations;

/// <summary>
/// ERP-N2.2.D: soporte durable de idempotencia para la creación HTTP de OrdenCompra.
/// No introduce recepción ni efectos de inventario/Kardex/costeo/finanzas.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260818210000_N2_2_OrdenCompraIdempotencia")]
public sealed class N2_2_OrdenCompraIdempotencia : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N22DGuard;
            CREATE TEMPORARY TABLE __N22DGuard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N22D_Guard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N22DGuard (Id, Violaciones)
            SELECT 1, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
              FROM information_schema.tables
             WHERE table_schema = DATABASE()
               AND table_name = 'OrdenesCompra';

            INSERT INTO __N22DGuard (Id, Violaciones)
            SELECT 2, COUNT(*)
              FROM information_schema.columns
             WHERE table_schema = DATABASE()
               AND table_name = 'OrdenesCompra'
               AND column_name IN ('IdempotencyKey', 'IdempotencyFingerprint');

            INSERT INTO __N22DGuard (Id, Violaciones)
            SELECT 3, COUNT(*)
              FROM information_schema.statistics
             WHERE table_schema = DATABASE()
               AND table_name = 'OrdenesCompra'
               AND index_name = 'UX_OrdenesCompra_IdempotencyKey';

            DROP TEMPORARY TABLE __N22DGuard;
            """);

        migrationBuilder.AddColumn<string>(
            name: "IdempotencyKey",
            table: "OrdenesCompra",
            type: "varchar(128)",
            maxLength: 128,
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AddColumn<string>(
            name: "IdempotencyFingerprint",
            table: "OrdenesCompra",
            type: "varchar(64)",
            maxLength: 64,
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "UX_OrdenesCompra_IdempotencyKey",
            table: "OrdenesCompra",
            column: "IdempotencyKey",
            unique: true);

        migrationBuilder.Sql("""
            ALTER TABLE OrdenesCompra
                ADD CONSTRAINT CK_OrdenesCompra_IdempotenciaConsistente
                CHECK ((IdempotencyKey IS NULL AND IdempotencyFingerprint IS NULL)
                    OR (IdempotencyKey IS NOT NULL
                        AND CHAR_LENGTH(TRIM(IdempotencyKey)) > 0
                        AND IdempotencyFingerprint IS NOT NULL
                        AND CHAR_LENGTH(IdempotencyFingerprint) = 64));
            """);

        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N22DPostGuard;
            CREATE TEMPORARY TABLE __N22DPostGuard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N22D_PostGuard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N22DPostGuard (Id, Violaciones)
            SELECT 1, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
              FROM information_schema.columns
             WHERE table_schema = DATABASE()
               AND table_name = 'OrdenesCompra'
               AND column_name IN ('IdempotencyKey', 'IdempotencyFingerprint');

            INSERT INTO __N22DPostGuard (Id, Violaciones)
            SELECT 2, CASE WHEN COUNT(DISTINCT index_name) = 1 THEN 0 ELSE 1 END
              FROM information_schema.statistics
             WHERE table_schema = DATABASE()
               AND table_name = 'OrdenesCompra'
               AND index_name = 'UX_OrdenesCompra_IdempotencyKey';

            INSERT INTO __N22DPostGuard (Id, Violaciones)
            SELECT 3, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
              FROM information_schema.table_constraints
             WHERE constraint_schema = DATABASE()
               AND table_name = 'OrdenesCompra'
               AND constraint_type = 'CHECK'
               AND constraint_name = 'CK_OrdenesCompra_IdempotenciaConsistente';

            INSERT INTO __N22DPostGuard (Id, Violaciones)
            SELECT 4, COUNT(*)
              FROM OrdenesCompra
             WHERE (IdempotencyKey IS NULL) <> (IdempotencyFingerprint IS NULL);

            DROP TEMPORARY TABLE __N22DPostGuard;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N22DDownGuard;
            CREATE TEMPORARY TABLE __N22DDownGuard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N22D_DownGuard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N22DDownGuard (Id, Violaciones)
            SELECT 1, COUNT(*)
              FROM OrdenesCompra
             WHERE IdempotencyKey IS NOT NULL OR IdempotencyFingerprint IS NOT NULL;

            DROP TEMPORARY TABLE __N22DDownGuard;
            """);

        migrationBuilder.Sql("ALTER TABLE OrdenesCompra DROP CHECK CK_OrdenesCompra_IdempotenciaConsistente;");
        migrationBuilder.DropIndex(name: "UX_OrdenesCompra_IdempotencyKey", table: "OrdenesCompra");
        migrationBuilder.DropColumn(name: "IdempotencyFingerprint", table: "OrdenesCompra");
        migrationBuilder.DropColumn(name: "IdempotencyKey", table: "OrdenesCompra");
    }
}
