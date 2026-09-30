using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Persistence.Migrations;

/// <summary>
/// N4.8.C: persistencia aditiva y reversible de la configuración por evento contable.
/// No ejecuta backfill ni toca datos productivos.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260905061000_N4_8_C_ConfiguracionContable")]
public sealed class N4_8_C_ConfiguracionContable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE `ConfiguracionesContables` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `Evento` int NOT NULL,
                `CuentaDebeId` int NOT NULL,
                `CuentaHaberId` int NOT NULL,
                `Activo` tinyint(1) NOT NULL DEFAULT TRUE,
                `Descripcion` varchar(500) CHARACTER SET utf8mb4 NULL,
                `FechaCreacion` datetime(6) NOT NULL,
                `FechaActualizacion` datetime(6) NOT NULL,
                `CreadoPorUsuarioId` int NULL,
                `CreadoPorNombreUsuario` longtext CHARACTER SET utf8mb4 NULL,
                `ActualizadoPorUsuarioId` int NULL,
                `ActualizadoPorNombreUsuario` longtext CHARACTER SET utf8mb4 NULL,
                CONSTRAINT `PK_ConfiguracionesContables` PRIMARY KEY (`Id`),
                CONSTRAINT `FK_ConfiguracionesContables_CuentasContables_CuentaDebeId`
                    FOREIGN KEY (`CuentaDebeId`) REFERENCES `CuentasContables` (`Id`) ON DELETE RESTRICT,
                CONSTRAINT `FK_ConfiguracionesContables_CuentasContables_CuentaHaberId`
                    FOREIGN KEY (`CuentaHaberId`) REFERENCES `CuentasContables` (`Id`) ON DELETE RESTRICT
            ) CHARACTER SET=utf8mb4;

            CREATE INDEX `IX_ConfiguracionesContables_CuentaDebeId`
                ON `ConfiguracionesContables` (`CuentaDebeId`);
            CREATE INDEX `IX_ConfiguracionesContables_CuentaHaberId`
                ON `ConfiguracionesContables` (`CuentaHaberId`);
            CREATE UNIQUE INDEX `UX_ConfiguracionesContables_Evento`
                ON `ConfiguracionesContables` (`Evento`);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE `ConfiguracionesContables`;");
    }
}
