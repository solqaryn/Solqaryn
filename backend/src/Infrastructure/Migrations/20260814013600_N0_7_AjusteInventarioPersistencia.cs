using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations;

/// <summary>
/// ERP-N0.7 C: persiste el documento AjusteInventario y extiende el origen físico
/// de MovimientosInventario con AjusteInventarioId sin reinterpretar ajustes legacy.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260814013600_N0_7_AjusteInventarioPersistencia")]
public sealed class N0_7_AjusteInventarioPersistencia : Migration
{
    private const string ConstraintOrigen = "CK_MovimientosInventario_OrigenTipado_Exclusivo_N06";
    private const string TriggerInsert = "TR_MovimientosInventario_N06_OrigenTipado_BI";
    private const string TriggerUpdate = "TR_MovimientosInventario_N06_OrigenTipado_BU";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Reserva fail-closed del nuevo snapshot. No se reinterpretan ajustes históricos.
        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N07CGuard;
            CREATE TEMPORARY TABLE __N07CGuard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N07C_Guard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N07CGuard (Id, Violaciones)
            SELECT 1, COUNT(*)
              FROM MovimientosInventario
             WHERE CAST(ReferenciaTipo AS BINARY) = CAST('AjusteInventario' AS BINARY);

            DROP TEMPORARY TABLE __N07CGuard;
            """);

        migrationBuilder.Sql("""
            CREATE TABLE AjustesInventario
            (
                Id INT NOT NULL AUTO_INCREMENT,
                NumeroAjuste VARCHAR(20) NOT NULL,
                FechaAjuste DATETIME(6) NOT NULL,
                Estado VARCHAR(20) NOT NULL,
                Motivo VARCHAR(500) NOT NULL,
                Observaciones VARCHAR(1000) NULL,
                FechaCreacion DATETIME(6) NOT NULL,
                FechaActualizacion DATETIME(6) NOT NULL,
                CreadoPorUsuarioId INT NULL,
                CreadoPorNombreUsuario VARCHAR(150) NULL,
                ActualizadoPorUsuarioId INT NULL,
                ActualizadoPorNombreUsuario VARCHAR(150) NULL,
                FechaConfirmacion DATETIME(6) NULL,
                ConfirmadoPorUsuarioId INT NULL,
                ConfirmadoPorNombreUsuario VARCHAR(150) NULL,
                FechaAnulacion DATETIME(6) NULL,
                AnuladoPorUsuarioId INT NULL,
                AnuladoPorNombreUsuario VARCHAR(150) NULL,
                MotivoAnulacion VARCHAR(500) NULL,
                CONSTRAINT PK_AjustesInventario PRIMARY KEY (Id),
                CONSTRAINT UX_AjustesInventario_Numero UNIQUE (NumeroAjuste),
                INDEX IX_AjustesInventario_Estado_Fecha (Estado, FechaAjuste)
            ) ENGINE=InnoDB;

            CREATE TABLE AjusteInventarioDetalles
            (
                Id INT NOT NULL AUTO_INCREMENT,
                AjusteInventarioId INT NOT NULL,
                ProductoId INT NOT NULL,
                ProductoVarianteId INT NULL,
                CantidadObjetivo INT NOT NULL,
                CantidadAnteriorSnapshot INT NULL,
                CantidadNuevaSnapshot INT NULL,
                CostoUnitarioSnapshot DECIMAL(18,2) NULL,
                NombreSnapshot VARCHAR(150) NULL,
                SkuSnapshot VARCHAR(80) NULL,
                MarcaSnapshot VARCHAR(100) NULL,
                ModeloSnapshot VARCHAR(100) NULL,
                ColorSnapshot VARCHAR(100) NULL,
                TallaSnapshot VARCHAR(100) NULL,
                FechaCreacion DATETIME(6) NOT NULL,
                FechaActualizacion DATETIME(6) NOT NULL,
                CONSTRAINT PK_AjusteInventarioDetalles PRIMARY KEY (Id),
                CONSTRAINT FK_AjusteInventarioDetalles_AjustesInventario
                    FOREIGN KEY (AjusteInventarioId) REFERENCES AjustesInventario(Id) ON DELETE CASCADE,
                CONSTRAINT FK_AjusteInventarioDetalles_Productos
                    FOREIGN KEY (ProductoId) REFERENCES Productos(Id) ON DELETE RESTRICT,
                CONSTRAINT FK_AjusteInventarioDetalles_ProductoVariantes
                    FOREIGN KEY (ProductoVarianteId) REFERENCES ProductoVariantes(Id) ON DELETE RESTRICT,
                CONSTRAINT CK_AjusteInventarioDetalles_CantidadObjetivo
                    CHECK (CantidadObjetivo >= 0),
                CONSTRAINT CK_AjusteInventarioDetalles_Snapshots
                    CHECK (
                        (CantidadAnteriorSnapshot IS NULL AND CantidadNuevaSnapshot IS NULL AND CostoUnitarioSnapshot IS NULL)
                        OR
                        (CantidadAnteriorSnapshot IS NOT NULL AND CantidadAnteriorSnapshot >= 0
                         AND CantidadNuevaSnapshot IS NOT NULL AND CantidadNuevaSnapshot >= 0
                         AND CostoUnitarioSnapshot IS NOT NULL AND CostoUnitarioSnapshot >= 0
                         AND CantidadNuevaSnapshot <> CantidadAnteriorSnapshot)
                    ),
                INDEX IX_AjusteInventarioDetalles_AjusteInventarioId (AjusteInventarioId),
                INDEX IX_AjusteInventarioDetalles_Producto_Variante (ProductoId, ProductoVarianteId)
            ) ENGINE=InnoDB;

            ALTER TABLE MovimientosInventario
                ADD COLUMN AjusteInventarioId INT NULL,
                ADD INDEX IX_MovimientosInventario_AjusteInventarioId (AjusteInventarioId),
                ADD CONSTRAINT FK_MovInv_AjusteInventarioId_N07
                    FOREIGN KEY (AjusteInventarioId) REFERENCES AjustesInventario(Id) ON DELETE RESTRICT;
            """);

        // Evolución del bridge typed-first N0.6: cuatro orígenes mutuamente exclusivos.
        migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {TriggerUpdate};");
        migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {TriggerInsert};");
        migrationBuilder.Sql($"ALTER TABLE MovimientosInventario DROP CHECK {ConstraintOrigen};");

        migrationBuilder.Sql($"""
            CREATE TRIGGER {TriggerInsert}
            BEFORE INSERT ON MovimientosInventario
            FOR EACH ROW
            SET
                NEW.CompraId = CASE
                    WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL
                    THEN NEW.CompraId
                    WHEN CAST(NEW.ReferenciaTipo AS BINARY) IN (CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY))
                    THEN NEW.ReferenciaId ELSE NULL END,
                NEW.VentaId = CASE
                    WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL
                    THEN NEW.VentaId
                    WHEN CAST(NEW.ReferenciaTipo AS BINARY) IN (CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY))
                    THEN NEW.ReferenciaId ELSE NULL END,
                NEW.ConsumoInsumoId = CASE
                    WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL
                    THEN NEW.ConsumoInsumoId
                    WHEN CAST(NEW.ReferenciaTipo AS BINARY) = CAST('ConsumoInsumo' AS BINARY)
                    THEN NEW.ReferenciaId ELSE NULL END,
                NEW.AjusteInventarioId = CASE
                    WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL
                    THEN NEW.AjusteInventarioId
                    ELSE NULL END;
            """);

        migrationBuilder.Sql($"""
            CREATE TRIGGER {TriggerUpdate}
            BEFORE UPDATE ON MovimientosInventario
            FOR EACH ROW
            SET
                NEW.CompraId = CASE
                    WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL
                    THEN NEW.CompraId
                    WHEN CAST(NEW.ReferenciaTipo AS BINARY) IN (CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY))
                    THEN NEW.ReferenciaId ELSE NULL END,
                NEW.VentaId = CASE
                    WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL
                    THEN NEW.VentaId
                    WHEN CAST(NEW.ReferenciaTipo AS BINARY) IN (CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY))
                    THEN NEW.ReferenciaId ELSE NULL END,
                NEW.ConsumoInsumoId = CASE
                    WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL
                    THEN NEW.ConsumoInsumoId
                    WHEN CAST(NEW.ReferenciaTipo AS BINARY) = CAST('ConsumoInsumo' AS BINARY)
                    THEN NEW.ReferenciaId ELSE NULL END,
                NEW.AjusteInventarioId = CASE
                    WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL
                    THEN NEW.AjusteInventarioId
                    ELSE NULL END;
            """);

        migrationBuilder.Sql($"""
            ALTER TABLE MovimientosInventario
            ADD CONSTRAINT {ConstraintOrigen}
            CHECK (
                ((CompraId IS NOT NULL) + (VentaId IS NOT NULL) + (ConsumoInsumoId IS NOT NULL) + (AjusteInventarioId IS NOT NULL) <= 1)
                AND
                (
                    (CAST(ReferenciaTipo AS BINARY) IN (CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY))
                        AND CompraId = ReferenciaId AND VentaId IS NULL AND ConsumoInsumoId IS NULL AND AjusteInventarioId IS NULL)
                    OR
                    (CAST(ReferenciaTipo AS BINARY) IN (CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY))
                        AND VentaId = ReferenciaId AND CompraId IS NULL AND ConsumoInsumoId IS NULL AND AjusteInventarioId IS NULL)
                    OR
                    (CAST(ReferenciaTipo AS BINARY) = CAST('ConsumoInsumo' AS BINARY)
                        AND ConsumoInsumoId = ReferenciaId AND CompraId IS NULL AND VentaId IS NULL AND AjusteInventarioId IS NULL)
                    OR
                    (CAST(ReferenciaTipo AS BINARY) = CAST('AjusteInventario' AS BINARY)
                        AND AjusteInventarioId = ReferenciaId AND CompraId IS NULL AND VentaId IS NULL AND ConsumoInsumoId IS NULL)
                    OR
                    (CAST(Tipo AS BINARY) = CAST('Ajuste' AS BINARY)
                        AND CAST(ReferenciaTipo AS BINARY) NOT IN (
                            CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY),
                            CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY),
                            CAST('ConsumoInsumo' AS BINARY), CAST('AjusteInventario' AS BINARY))
                        AND CompraId IS NULL AND VentaId IS NULL AND ConsumoInsumoId IS NULL AND AjusteInventarioId IS NULL)
                )
            );
            """);

        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N07CPostGuard;
            CREATE TEMPORARY TABLE __N07CPostGuard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N07C_PostGuard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N07CPostGuard (Id, Violaciones)
            SELECT 1, COUNT(*)
              FROM MovimientosInventario
             WHERE AjusteInventarioId IS NOT NULL;

            INSERT INTO __N07CPostGuard (Id, Violaciones)
            SELECT 2, COUNT(*)
              FROM AjustesInventario;

            DROP TEMPORARY TABLE __N07CPostGuard;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException(
            "ERP-N0.7 C es forward-only: el rollback seguro requiere restaurar el respaldo/preflight correspondiente para preservar documentos y movimientos históricos.");
}
