using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Persistence.Migrations;

/// <summary>
/// Materializa la autoridad de stock por variante/almacén de ERP-N1.4.
/// El backfill del campo legacy ProductoVariantes.Cantidad se ejecuta en un
/// script separado y fail-closed: esta migración no elimina ni altera Cantidad.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260815012500_N1_4_ExistenciaVariante")]
public sealed class N1_4_ExistenciaVariante : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE `ExistenciasVariante` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `ProductoVarianteId` int NOT NULL,
                `AlmacenId` int NOT NULL,
                `UbicacionAlmacenId` int NULL,
                `StockFisico` int NOT NULL DEFAULT 0,
                `StockReservado` int NOT NULL DEFAULT 0,
                `StockDisponible` int AS (`StockFisico` - `StockReservado`) STORED,
                `StockTransito` int NOT NULL DEFAULT 0,
                `StockMinimo` int NOT NULL DEFAULT 0,
                `StockMaximo` int NULL,
                `UbicacionAlmacenIdUnica` int AS (IFNULL(`UbicacionAlmacenId`, 0)) STORED,
                `FechaCreacion` datetime(6) NOT NULL,
                `FechaActualizacion` datetime(6) NOT NULL,
                `CreadoPorUsuarioId` int NULL,
                `CreadoPorNombreUsuario` varchar(150) CHARACTER SET utf8mb4 NULL,
                `ActualizadoPorUsuarioId` int NULL,
                `ActualizadoPorNombreUsuario` varchar(150) CHARACTER SET utf8mb4 NULL,
                CONSTRAINT `PK_ExistenciasVariante` PRIMARY KEY (`Id`),
                CONSTRAINT `CK_ExistenciasVariante_StockFisico` CHECK (`StockFisico` >= 0),
                CONSTRAINT `CK_ExistenciasVariante_StockReservado` CHECK (`StockReservado` >= 0 AND `StockReservado` <= `StockFisico`),
                CONSTRAINT `CK_ExistenciasVariante_StockTransito` CHECK (`StockTransito` >= 0),
                CONSTRAINT `CK_ExistenciasVariante_StockMinimo` CHECK (`StockMinimo` >= 0),
                CONSTRAINT `CK_ExistenciasVariante_StockMaximo` CHECK (`StockMaximo` IS NULL OR `StockMaximo` >= `StockMinimo`),
                CONSTRAINT `FK_ExistenciasVariante_ProductoVariantes_ProductoVarianteId`
                    FOREIGN KEY (`ProductoVarianteId`) REFERENCES `ProductoVariantes` (`Id`) ON DELETE RESTRICT,
                CONSTRAINT `FK_ExistenciasVariante_Almacenes_AlmacenId`
                    FOREIGN KEY (`AlmacenId`) REFERENCES `Almacenes` (`Id`) ON DELETE RESTRICT,
                CONSTRAINT `FK_ExistenciasVariante_Ubicacion_MismoAlmacen`
                    FOREIGN KEY (`AlmacenId`, `UbicacionAlmacenId`)
                    REFERENCES `UbicacionesAlmacen` (`AlmacenId`, `Id`) ON DELETE RESTRICT
            ) CHARACTER SET=utf8mb4 ENGINE=InnoDB;

            CREATE UNIQUE INDEX `UX_ExistenciasVariante_Variante_Almacen_Ubicacion`
                ON `ExistenciasVariante` (`ProductoVarianteId`, `AlmacenId`, `UbicacionAlmacenIdUnica`);

            CREATE INDEX `IX_ExistenciasVariante_AlmacenId`
                ON `ExistenciasVariante` (`AlmacenId`);

            CREATE INDEX `IX_ExistenciasVariante_UbicacionAlmacenId`
                ON `ExistenciasVariante` (`UbicacionAlmacenId`);

            CREATE INDEX `IX_ExistenciasVariante_Variante_Almacen`
                ON `ExistenciasVariante` (`ProductoVarianteId`, `AlmacenId`);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ExistenciasVariante");
    }
}
