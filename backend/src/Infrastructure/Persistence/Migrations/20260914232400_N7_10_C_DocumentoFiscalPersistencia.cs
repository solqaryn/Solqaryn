using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Persistence.Migrations;

/// <summary>
/// N7.10.C — persistencia aditiva/reversible de emisión fiscal provider-neutral.
/// No codifica legislación, autoridad, rangos ni credenciales específicos de un país.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260914232400_N7_10_C_DocumentoFiscalPersistencia")]
public sealed class N7_10_C_DocumentoFiscalPersistencia : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE `DocumentosFiscalesEmision` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `EmpresaId` int NOT NULL,
                `SucursalId` int NULL,
                `FacturaId` int NOT NULL,
                `Jurisdiccion` varchar(80) CHARACTER SET utf8mb4 NOT NULL,
                `Proveedor` varchar(80) CHARACTER SET utf8mb4 NOT NULL,
                `TipoDocumento` varchar(80) CHARACTER SET utf8mb4 NOT NULL,
                `ClaveIdempotenciaHash` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
                `HashSnapshot` varchar(160) CHARACTER SET utf8mb4 NOT NULL,
                `Estado` int NOT NULL,
                `ReferenciaExterna` varchar(200) CHARACTER SET utf8mb4 NULL,
                `CodigoProveedor` varchar(120) CHARACTER SET utf8mb4 NULL,
                `EsTransitorio` tinyint(1) NOT NULL,
                `CreadoUtc` datetime(6) NOT NULL,
                `UltimaActualizacionUtc` datetime(6) NOT NULL,
                `FechaCreacion` datetime(6) NOT NULL,
                `FechaActualizacion` datetime(6) NOT NULL,
                `CreadoPorUsuarioId` int NULL,
                `CreadoPorNombreUsuario` varchar(200) CHARACTER SET utf8mb4 NULL,
                `ActualizadoPorUsuarioId` int NULL,
                `ActualizadoPorNombreUsuario` varchar(200) CHARACTER SET utf8mb4 NULL,
                CONSTRAINT `PK_DocumentosFiscalesEmision` PRIMARY KEY (`Id`),
                CONSTRAINT `FK_DocFiscal_Empresas_EmpresaId`
                    FOREIGN KEY (`EmpresaId`) REFERENCES `Empresas` (`Id`) ON DELETE RESTRICT,
                CONSTRAINT `FK_DocFiscal_Sucursales_SucursalId`
                    FOREIGN KEY (`SucursalId`) REFERENCES `Sucursales` (`Id`) ON DELETE RESTRICT,
                CONSTRAINT `FK_DocFiscal_Facturas_FacturaId`
                    FOREIGN KEY (`FacturaId`) REFERENCES `Facturas` (`Id`) ON DELETE RESTRICT,
                CONSTRAINT `CK_DocFiscal_EmpresaId_Positivo` CHECK (`EmpresaId` > 0),
                CONSTRAINT `CK_DocFiscal_FacturaId_Positivo` CHECK (`FacturaId` > 0),
                CONSTRAINT `CK_DocFiscal_SucursalId_Positivo` CHECK (`SucursalId` IS NULL OR `SucursalId` > 0),
                CONSTRAINT `CK_DocFiscal_Jurisdiccion_NoVacia` CHECK (CHAR_LENGTH(TRIM(`Jurisdiccion`)) > 0),
                CONSTRAINT `CK_DocFiscal_Proveedor_NoVacio` CHECK (CHAR_LENGTH(TRIM(`Proveedor`)) > 0),
                CONSTRAINT `CK_DocFiscal_TipoDocumento_NoVacio` CHECK (CHAR_LENGTH(TRIM(`TipoDocumento`)) > 0),
                CONSTRAINT `CK_DocFiscal_IdempotenciaHash_SHA256` CHECK (CHAR_LENGTH(`ClaveIdempotenciaHash`) = 64),
                CONSTRAINT `CK_DocFiscal_HashSnapshot_NoVacio` CHECK (CHAR_LENGTH(TRIM(`HashSnapshot`)) > 0),
                CONSTRAINT `CK_DocFiscal_Estado_Valido` CHECK (`Estado` BETWEEN 1 AND 3)
            ) CHARACTER SET=utf8mb4;

            CREATE UNIQUE INDEX `UX_DocFiscal_Empresa_Proveedor_Idempotencia`
                ON `DocumentosFiscalesEmision` (`EmpresaId`, `Proveedor`, `ClaveIdempotenciaHash`);
            CREATE INDEX `IX_DocFiscal_Empresa_Factura_Creado`
                ON `DocumentosFiscalesEmision` (`EmpresaId`, `FacturaId`, `CreadoUtc`);
            CREATE INDEX `IX_DocFiscal_Empresa_Estado_Actualizado`
                ON `DocumentosFiscalesEmision` (`EmpresaId`, `Estado`, `UltimaActualizacionUtc`, `Id`);
            CREATE UNIQUE INDEX `UX_DocFiscal_Empresa_Proveedor_Referencia`
                ON `DocumentosFiscalesEmision` (`EmpresaId`, `Proveedor`, `ReferenciaExterna`);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE `DocumentosFiscalesEmision`;");
    }
}
