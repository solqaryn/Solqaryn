using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVarianteTecnicaProductoSimple : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // La propia migración vuelve a ejecutar el preflight antes del primer ALTER TABLE.
            // MySQL confirma DDL de forma implícita; por eso el rechazo debe ocurrir antes de
            // agregar columnas para no dejar un esquema parcialmente aplicado.
            migrationBuilder.Sql(
                """
                DROP TEMPORARY TABLE IF EXISTS __PreflightVarianteTecnica2C1;
                """);

            migrationBuilder.Sql(
                """
                CREATE TEMPORARY TABLE __PreflightVarianteTecnica2C1
                (
                    Id TINYINT NOT NULL,
                    Violaciones INT NOT NULL,
                    CONSTRAINT PK_PreflightVarianteTecnica2C1 PRIMARY KEY (Id),
                    CONSTRAINT CK_PreflightVarianteTecnica2C1_Cero
                        CHECK (Violaciones = 0)
                );
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO __PreflightVarianteTecnica2C1 (Id, Violaciones)
                SELECT
                    1,
                    (SELECT COUNT(*)
                       FROM Productos p
                      WHERE p.Eliminado = 0
                        AND p.Cantidad < 0)
                  + (SELECT COUNT(*)
                       FROM ProductoVariantes pv
                      WHERE pv.Eliminado = 0
                        AND pv.Cantidad < 0)
                  + (SELECT COUNT(*)
                       FROM (
                            SELECT UPPER(TRIM(pv.Sku)) AS Valor
                              FROM ProductoVariantes pv
                             WHERE pv.Eliminado = 0
                               AND pv.Sku IS NOT NULL
                               AND TRIM(pv.Sku) <> ''
                             GROUP BY UPPER(TRIM(pv.Sku))
                            HAVING COUNT(*) > 1
                       ) duplicados_sku)
                  + (SELECT COUNT(*)
                       FROM (
                            SELECT TRIM(pv.CodigoBarras) AS Valor
                              FROM ProductoVariantes pv
                             WHERE pv.Eliminado = 0
                               AND pv.CodigoBarras IS NOT NULL
                               AND TRIM(pv.CodigoBarras) <> ''
                             GROUP BY TRIM(pv.CodigoBarras)
                            HAVING COUNT(*) > 1
                       ) duplicados_codigo)
                  + (SELECT COUNT(*)
                       FROM ProductoVariantes pv
                      WHERE pv.Eliminado = 0
                        AND pv.ColorId IS NULL)
                  + (SELECT COUNT(*)
                       FROM Productos p
                       JOIN (
                            SELECT pv.ProductoId, SUM(pv.Cantidad) AS CantidadVariantes
                              FROM ProductoVariantes pv
                             WHERE pv.Eliminado = 0
                             GROUP BY pv.ProductoId
                       ) inventario ON inventario.ProductoId = p.Id
                      WHERE p.Eliminado = 0
                        AND p.Cantidad <> inventario.CantidadVariantes)
                  + (SELECT COUNT(*)
                       FROM Productos p
                      WHERE p.Eliminado = 0
                        AND NOT EXISTS (
                            SELECT 1
                              FROM ProductoVariantes actual
                             WHERE actual.ProductoId = p.Id
                               AND actual.Eliminado = 0
                        )
                        AND EXISTS (
                            SELECT 1
                              FROM ProductoVariantes existente
                             WHERE UPPER(TRIM(existente.Sku)) =
                                   UPPER(CONCAT('TEC-', LPAD(p.Id, 10, '0')))
                        ));
                """);

            migrationBuilder.Sql(
                """
                DROP TEMPORARY TABLE __PreflightVarianteTecnica2C1;
                """);

            migrationBuilder.AddColumn<bool>(
                name: "EsTecnica",
                table: "ProductoVariantes",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ProductoTecnicoUnico",
                table: "ProductoVariantes",
                type: "int",
                nullable: true,
                computedColumnSql: "CASE WHEN `EsTecnica` = 1 AND `Eliminado` = 0 THEN `ProductoId` ELSE NULL END",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductoVariantes_ProductoTecnicoUnico",
                table: "ProductoVariantes",
                column: "ProductoTecnicoUnico",
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO ProductoVariantes
                    (ProductoId, ColorId, Sku, CodigoBarras, Cantidad, UmbralStockBajo,
                     Costo, Precio, Activo, Eliminado, FechaEliminacion, EliminadoPorUsuarioId,
                     FechaCreacion, FechaActualizacion, CreadoPorUsuarioId, CreadoPorNombreUsuario,
                     ActualizadoPorUsuarioId, ActualizadoPorNombreUsuario, EsTecnica)
                SELECT
                    p.Id,
                    NULL,
                    CONCAT('TEC-', LPAD(p.Id, 10, '0')),
                    NULL,
                    p.Cantidad,
                    p.UmbralStockBajo,
                    p.Costo,
                    p.Precio,
                    p.Activo,
                    0,
                    NULL,
                    NULL,
                    UTC_TIMESTAMP(6),
                    UTC_TIMESTAMP(6),
                    NULL,
                    'Migración Bloque 2C.1',
                    NULL,
                    NULL,
                    1
                FROM Productos p
                WHERE p.Eliminado = 0
                  AND NOT EXISTS (
                      SELECT 1
                        FROM ProductoVariantes pv
                       WHERE pv.ProductoId = p.Id
                         AND pv.Eliminado = 0
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Fail-closed: si una variante técnica ya tiene historial protegido,
            // las restricciones foráneas impedirán eliminarla y abortarán el rollback.
            migrationBuilder.Sql(
                """
                DELETE FROM ProductoVariantes
                WHERE EsTecnica = 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_ProductoVariantes_ProductoTecnicoUnico",
                table: "ProductoVariantes");

            migrationBuilder.DropColumn(
                name: "ProductoTecnicoUnico",
                table: "ProductoVariantes");

            migrationBuilder.DropColumn(
                name: "EsTecnica",
                table: "ProductoVariantes");
        }
    }
}
