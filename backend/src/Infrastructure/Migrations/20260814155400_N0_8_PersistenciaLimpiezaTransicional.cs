using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations;

/// <summary>
/// ERP-N0.8.C: materializa la relación MetodoPago de Compra y reconcilia el
/// modelo EF con las FKs tipadas de MovimientoInventario creadas en N0.6/N0.7.
/// No elimina snapshots/columnas legacy; esa decisión requiere completar D-G.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260814155400_N0_8_PersistenciaLimpiezaTransicional")]
public sealed class N0_8_PersistenciaLimpiezaTransicional : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Fail-closed: todos los valores históricos de Compra deben ser representables
        // por los códigos estables del catálogo antes de añadir/backfillear la FK.
        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N08CGuard;
            CREATE TEMPORARY TABLE __N08CGuard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N08C_Guard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N08CGuard (Id, Violaciones)
            SELECT 1,
                (SELECT COUNT(*)
                   FROM Compras c
                  WHERE LOWER(TRIM(c.MetodoPago)) NOT IN ('efectivo','transferencia','tarjeta','otro'))
              + (SELECT COUNT(*)
                   FROM Compras c
                   LEFT JOIN MetodosPago mp
                     ON LOWER(TRIM(mp.Codigo)) = LOWER(TRIM(c.MetodoPago))
                  WHERE mp.Id IS NULL);

            DROP TEMPORARY TABLE __N08CGuard;
            """);

        migrationBuilder.AddColumn<int>(
            name: "MetodoPagoId",
            table: "Compras",
            type: "int",
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE Compras c
            JOIN MetodosPago mp
              ON LOWER(TRIM(mp.Codigo)) = LOWER(TRIM(c.MetodoPago))
               SET c.MetodoPagoId = mp.Id;
            """);

        // Postcheck inmediato del backfill. El campo se mantiene nullable durante la
        // transición C->D para no romper escritores legacy aún no migrados.
        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N08CPostGuard;
            CREATE TEMPORARY TABLE __N08CPostGuard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N08C_PostGuard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N08CPostGuard (Id, Violaciones)
            SELECT 1, COUNT(*)
              FROM Compras c
              LEFT JOIN MetodosPago mp ON mp.Id = c.MetodoPagoId
             WHERE c.MetodoPagoId IS NULL
                OR mp.Id IS NULL
                OR LOWER(TRIM(mp.Codigo)) <> LOWER(TRIM(c.MetodoPago));

            DROP TEMPORARY TABLE __N08CPostGuard;
            """);

        migrationBuilder.CreateIndex(
            name: "IX_Compras_MetodoPagoId",
            table: "Compras",
            column: "MetodoPagoId");

        migrationBuilder.AddForeignKey(
            name: "FK_Compras_MetodosPago_MetodoPagoId",
            table: "Compras",
            column: "MetodoPagoId",
            principalTable: "MetodosPago",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException(
            "ERP-N0.8.C es forward-only: una vez que operaciones nuevas usen MetodoPagoId, retirar la FK podría perder la identidad de métodos administrables no representables por el enum legacy. El rollback seguro requiere respaldo/restauración o corrección forward.");
}
