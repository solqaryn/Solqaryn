using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class M2VariantesMultidimensionales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TEMPORARY TABLE `__M2_Preflight` (
    `Id` TINYINT NOT NULL,
    `Ok` TINYINT NOT NULL,
    CONSTRAINT `PK___M2_Preflight` PRIMARY KEY (`Id`),
    CONSTRAINT `CK___M2_Preflight_Ok` CHECK (`Ok` = 1)
);
INSERT INTO `__M2_Preflight` (`Id`, `Ok`)
SELECT 1, IF(
    EXISTS(
        SELECT 1 FROM `ProductoVariantes` pv
        LEFT JOIN `Colores` c ON c.`Id` = pv.`ColorId`
        WHERE pv.`ColorId` IS NOT NULL AND c.`Id` IS NULL
    )
    OR EXISTS(
        SELECT 1 FROM `Productos` p
        LEFT JOIN `Marcas` ma ON ma.`Id` = p.`MarcaId`
        LEFT JOIN `Modelos` mo ON mo.`Id` = p.`ModeloId`
        LEFT JOIN `Colores` co ON co.`Id` = p.`ColorId`
        LEFT JOIN `Tallas` ta ON ta.`Id` = p.`TallaId`
        WHERE (p.`MarcaId` IS NOT NULL AND ma.`Id` IS NULL)
           OR (p.`ModeloId` IS NOT NULL AND mo.`Id` IS NULL)
           OR (p.`ColorId` IS NOT NULL AND co.`Id` IS NULL)
           OR (p.`TallaId` IS NOT NULL AND ta.`Id` IS NULL)
    )
    OR EXISTS(
        SELECT 1 FROM `Productos` p
        JOIN `Modelos` mo ON mo.`Id` = p.`ModeloId`
        WHERE p.`ModeloId` IS NOT NULL
          AND (p.`MarcaId` IS NULL OR mo.`MarcaId` <> p.`MarcaId`)
    ), 0, 1
);
DROP TEMPORARY TABLE `__M2_Preflight`;
");

            // Primero expandimos la tabla. El índice legacy todavía sostiene la FK de ProductoId
            // en MySQL, por lo que no puede retirarse hasta crear el nuevo índice multidimensional.
            migrationBuilder.AddColumn<int>(
                name: "MarcaId",
                table: "ProductoVariantes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModeloId",
                table: "ProductoVariantes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TallaId",
                table: "ProductoVariantes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdentidadActivaUnica",
                table: "ProductoVariantes",
                type: "varchar(160)",
                maxLength: 160,
                nullable: true,
                computedColumnSql: "CASE WHEN `Eliminado` = 0 THEN CONCAT(`ProductoId`, ':', COALESCE(`MarcaId`, 0), ':', COALESCE(`ModeloId`, 0), ':', COALESCE(`ColorId`, 0), ':', COALESCE(`TallaId`, 0)) ELSE NULL END",
                stored: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql(@"
UPDATE `ProductoVariantes` pv
JOIN `Productos` p ON p.`Id` = pv.`ProductoId`
SET
    pv.`MarcaId` = CASE WHEN pv.`EsTecnica` = 0 THEN p.`MarcaId` ELSE NULL END,
    pv.`ModeloId` = CASE WHEN pv.`EsTecnica` = 0 THEN p.`ModeloId` ELSE NULL END,
    pv.`TallaId` = CASE WHEN pv.`EsTecnica` = 0 THEN p.`TallaId` ELSE NULL END,
    pv.`ColorId` = CASE WHEN pv.`EsTecnica` = 0 THEN pv.`ColorId` ELSE NULL END;
");

            migrationBuilder.CreateIndex(
                name: "IX_ProductoVariantes_Dimensiones",
                table: "ProductoVariantes",
                columns: new[] { "ProductoId", "MarcaId", "ModeloId", "ColorId", "TallaId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductoVariantes_MarcaId",
                table: "ProductoVariantes",
                column: "MarcaId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductoVariantes_ModeloId",
                table: "ProductoVariantes",
                column: "ModeloId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductoVariantes_TallaId",
                table: "ProductoVariantes",
                column: "TallaId");

            // Ya existe un índice con ProductoId como primera columna, así que el índice
            // legacy puede retirarse sin dejar la FK de ProductoId sin soporte.
            migrationBuilder.DropForeignKey(
                name: "FK_ProductoVariantes_CatalogosProducto_ColorId",
                table: "ProductoVariantes");

            migrationBuilder.DropIndex(
                name: "IX_ProductoVariantes_ProductoId_ColorId",
                table: "ProductoVariantes");

            migrationBuilder.CreateIndex(
                name: "UX_ProductoVariantes_IdentidadActiva",
                table: "ProductoVariantes",
                column: "IdentidadActivaUnica",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductoVariantes_Colores_ColorId",
                table: "ProductoVariantes",
                column: "ColorId",
                principalTable: "Colores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductoVariantes_Marcas_MarcaId",
                table: "ProductoVariantes",
                column: "MarcaId",
                principalTable: "Marcas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductoVariantes_Modelos_ModeloId",
                table: "ProductoVariantes",
                column: "ModeloId",
                principalTable: "Modelos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductoVariantes_Tallas_TallaId",
                table: "ProductoVariantes",
                column: "TallaId",
                principalTable: "Tallas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TEMPORARY TABLE `__M2_DownGuard` (
    `Id` TINYINT NOT NULL,
    `Ok` TINYINT NOT NULL,
    CONSTRAINT `PK___M2_DownGuard` PRIMARY KEY (`Id`),
    CONSTRAINT `CK___M2_DownGuard_Ok` CHECK (`Ok` = 1)
);
INSERT INTO `__M2_DownGuard` (`Id`, `Ok`)
SELECT 1, IF(
    EXISTS(
        SELECT 1 FROM `ProductoVariantes`
        WHERE `ColorId` IS NOT NULL
        GROUP BY `ProductoId`, `ColorId`
        HAVING COUNT(*) > 1
    )
    OR EXISTS(
        SELECT 1 FROM `ProductoVariantes` pv
        LEFT JOIN `CatalogosProducto` cp
          ON cp.`Id` = pv.`ColorId` AND cp.`Tipo` = 'Color'
        WHERE pv.`ColorId` IS NOT NULL AND cp.`Id` IS NULL
    ), 0, 1
);
DROP TEMPORARY TABLE `__M2_DownGuard`;
");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductoVariantes_Colores_ColorId",
                table: "ProductoVariantes");
            migrationBuilder.DropForeignKey(
                name: "FK_ProductoVariantes_Marcas_MarcaId",
                table: "ProductoVariantes");
            migrationBuilder.DropForeignKey(
                name: "FK_ProductoVariantes_Modelos_ModeloId",
                table: "ProductoVariantes");
            migrationBuilder.DropForeignKey(
                name: "FK_ProductoVariantes_Tallas_TallaId",
                table: "ProductoVariantes");

            // Crear primero el índice legacy mantiene soportada la FK de ProductoId cuando
            // posteriormente se retire IX_ProductoVariantes_Dimensiones.
            migrationBuilder.CreateIndex(
                name: "IX_ProductoVariantes_ProductoId_ColorId",
                table: "ProductoVariantes",
                columns: new[] { "ProductoId", "ColorId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductoVariantes_CatalogosProducto_ColorId",
                table: "ProductoVariantes",
                column: "ColorId",
                principalTable: "CatalogosProducto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropIndex(
                name: "IX_ProductoVariantes_Dimensiones",
                table: "ProductoVariantes");
            migrationBuilder.DropIndex(
                name: "IX_ProductoVariantes_MarcaId",
                table: "ProductoVariantes");
            migrationBuilder.DropIndex(
                name: "IX_ProductoVariantes_ModeloId",
                table: "ProductoVariantes");
            migrationBuilder.DropIndex(
                name: "IX_ProductoVariantes_TallaId",
                table: "ProductoVariantes");
            migrationBuilder.DropIndex(
                name: "UX_ProductoVariantes_IdentidadActiva",
                table: "ProductoVariantes");

            migrationBuilder.DropColumn(
                name: "IdentidadActivaUnica",
                table: "ProductoVariantes");
            migrationBuilder.DropColumn(
                name: "MarcaId",
                table: "ProductoVariantes");
            migrationBuilder.DropColumn(
                name: "ModeloId",
                table: "ProductoVariantes");
            migrationBuilder.DropColumn(
                name: "TallaId",
                table: "ProductoVariantes");
        }
    }
}
