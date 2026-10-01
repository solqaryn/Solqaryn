using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Persistence.Migrations;

/// <summary>
/// N7.8.C — persistencia aditiva/reversible de pagos online.
/// Conserva sólo referencias opacas e identificadores del proveedor; no almacena
/// PAN, CVV, número de tarjeta ni credenciales del proveedor.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260914193800_N7_8_C_PagosOnlinePersistence")]
public sealed class N7_8_C_PagosOnlinePersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE `PagosOnline` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `EmpresaId` int NOT NULL,
                `FacturaId` int NOT NULL,
                `Proveedor` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
                `ReferenciaProveedor` varchar(160) CHARACTER SET utf8mb4 NULL,
                `ProviderEventId` varchar(200) CHARACTER SET utf8mb4 NULL,
                `Monto` decimal(18,2) NOT NULL,
                `Moneda` varchar(3) CHARACTER SET utf8mb4 NOT NULL,
                `Estado` int NOT NULL,
                `UrlPago` varchar(2048) CHARACTER SET utf8mb4 NULL,
                `CreadoUtc` datetime(6) NOT NULL,
                `ConfirmadoUtc` datetime(6) NULL,
                `UltimoError` varchar(2000) CHARACTER SET utf8mb4 NULL,
                `FechaCreacion` datetime(6) NOT NULL,
                `FechaActualizacion` datetime(6) NOT NULL,
                `CreadoPorUsuarioId` int NULL,
                `CreadoPorNombreUsuario` varchar(200) CHARACTER SET utf8mb4 NULL,
                `ActualizadoPorUsuarioId` int NULL,
                `ActualizadoPorNombreUsuario` varchar(200) CHARACTER SET utf8mb4 NULL,
                CONSTRAINT `PK_PagosOnline` PRIMARY KEY (`Id`),
                CONSTRAINT `FK_PagosOnline_Empresas_EmpresaId`
                    FOREIGN KEY (`EmpresaId`) REFERENCES `Empresas` (`Id`) ON DELETE RESTRICT,
                CONSTRAINT `FK_PagosOnline_Facturas_FacturaId`
                    FOREIGN KEY (`FacturaId`) REFERENCES `Facturas` (`Id`) ON DELETE RESTRICT,
                CONSTRAINT `CK_PagosOnline_EmpresaId_Positivo` CHECK (`EmpresaId` > 0),
                CONSTRAINT `CK_PagosOnline_FacturaId_Positivo` CHECK (`FacturaId` > 0),
                CONSTRAINT `CK_PagosOnline_Proveedor_NoVacio` CHECK (CHAR_LENGTH(TRIM(`Proveedor`)) > 0),
                CONSTRAINT `CK_PagosOnline_Monto_Positivo` CHECK (`Monto` > 0),
                CONSTRAINT `CK_PagosOnline_Moneda_Valida` CHECK (CHAR_LENGTH(TRIM(`Moneda`)) = 3),
                CONSTRAINT `CK_PagosOnline_Estado_Valido` CHECK (`Estado` BETWEEN 1 AND 3)
            ) CHARACTER SET=utf8mb4;

            CREATE INDEX `IX_PagosOnline_Empresa_Factura_Creado`
                ON `PagosOnline` (`EmpresaId`, `FacturaId`, `CreadoUtc`);
            CREATE INDEX `IX_PagosOnline_Empresa_Estado_Creado`
                ON `PagosOnline` (`EmpresaId`, `Estado`, `CreadoUtc`, `Id`);
            CREATE UNIQUE INDEX `UX_PagosOnline_Empresa_Proveedor_Referencia`
                ON `PagosOnline` (`EmpresaId`, `Proveedor`, `ReferenciaProveedor`);
            CREATE UNIQUE INDEX `UX_PagosOnline_Empresa_Proveedor_Evento`
                ON `PagosOnline` (`EmpresaId`, `Proveedor`, `ProviderEventId`);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE `PagosOnline`;");
    }
}
