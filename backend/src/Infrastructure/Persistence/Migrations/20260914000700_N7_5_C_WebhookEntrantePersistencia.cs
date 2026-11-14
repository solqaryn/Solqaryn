using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Persistence.Migrations;

/// <summary>
/// N7.5.C: persistencia aditiva y reversible para webhooks entrantes ya
/// verificados. No ejecuta Producción, no hace backfill y no persiste firma,
/// secreto ni payload completo.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260914000700_N7_5_C_WebhookEntrantePersistencia")]
public sealed class N7_5_C_WebhookEntrantePersistencia : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE `WebhooksEntrantes` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `EmpresaId` int NOT NULL,
                `Proveedor` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
                `EventoExternoId` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
                `TipoEvento` varchar(160) CHARACTER SET utf8mb4 NOT NULL,
                `PayloadHash` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
                `CorrelationId` varchar(160) CHARACTER SET utf8mb4 NULL,
                `EmitidoEnUtc` datetime(6) NOT NULL,
                `RecibidoEnUtc` datetime(6) NOT NULL,
                `Estado` int NOT NULL DEFAULT 1,
                `ProcesandoDesdeUtc` datetime(6) NULL,
                `ProcesadoEnUtc` datetime(6) NULL,
                `RechazadoEnUtc` datetime(6) NULL,
                `MotivoRechazo` varchar(1000) CHARACTER SET utf8mb4 NULL,
                `FechaCreacion` datetime(6) NOT NULL,
                `FechaActualizacion` datetime(6) NOT NULL,
                CONSTRAINT `PK_WebhooksEntrantes` PRIMARY KEY (`Id`),
                CONSTRAINT `FK_WebhooksEntrantes_Empresas_EmpresaId`
                    FOREIGN KEY (`EmpresaId`) REFERENCES `Empresas` (`Id`) ON DELETE RESTRICT,
                CONSTRAINT `CK_WebhooksEntrantes_EmpresaId_Positivo` CHECK (`EmpresaId` > 0),
                CONSTRAINT `CK_WebhooksEntrantes_Proveedor_NoVacio` CHECK (CHAR_LENGTH(TRIM(`Proveedor`)) > 0),
                CONSTRAINT `CK_WebhooksEntrantes_EventoExternoId_NoVacio` CHECK (CHAR_LENGTH(TRIM(`EventoExternoId`)) > 0),
                CONSTRAINT `CK_WebhooksEntrantes_TipoEvento_NoVacio` CHECK (CHAR_LENGTH(TRIM(`TipoEvento`)) > 0),
                CONSTRAINT `CK_WebhooksEntrantes_PayloadHash_NoVacio` CHECK (CHAR_LENGTH(TRIM(`PayloadHash`)) > 0),
                CONSTRAINT `CK_WebhooksEntrantes_Estado_Valido` CHECK (`Estado` BETWEEN 1 AND 4)
            ) CHARACTER SET=utf8mb4;

            CREATE UNIQUE INDEX `UX_WebhooksEntrantes_Empresa_Proveedor_Evento`
                ON `WebhooksEntrantes` (`EmpresaId`, `Proveedor`, `EventoExternoId`);
            CREATE INDEX `IX_WebhooksEntrantes_Empresa_Estado_Recibido`
                ON `WebhooksEntrantes` (`EmpresaId`, `Estado`, `RecibidoEnUtc`, `Id`);
            CREATE INDEX `IX_WebhooksEntrantes_Empresa_Correlation`
                ON `WebhooksEntrantes` (`EmpresaId`, `CorrelationId`);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE `WebhooksEntrantes`;");
    }
}
