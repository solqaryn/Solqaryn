using System;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260811032000_N0_3_ConsolidarProductoVariante")]
public sealed class N0_3_ConsolidarProductoVariante : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __PreflightN03;
            CREATE TEMPORARY TABLE __PreflightN03
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones INT NOT NULL,
                CONSTRAINT CK_PreflightN03_Cero CHECK (Violaciones = 0)
            );
            INSERT INTO __PreflightN03 (Id, Violaciones)
            SELECT 1,
                (SELECT COUNT(*) FROM ProductoVariantes WHERE Eliminado = 0 AND (Cantidad < 0 OR UmbralStockBajo < 0 OR (Costo IS NOT NULL AND Costo < 0) OR (Precio IS NOT NULL AND Precio < 0)))
              + (SELECT COUNT(*) FROM ProductoVariantes WHERE Eliminado = 0 AND ModeloId IS NOT NULL AND MarcaId IS NULL)
              + (SELECT COUNT(*) FROM ProductoVariantes pv JOIN Modelos m ON m.Id = pv.ModeloId WHERE pv.Eliminado = 0 AND pv.ModeloId IS NOT NULL AND pv.MarcaId <> m.MarcaId)
              + (SELECT COUNT(*) FROM ProductoImagenes pi JOIN ProductoVariantes pv ON pv.Id = pi.ProductoVarianteId WHERE pi.ProductoVarianteId IS NOT NULL AND pi.ProductoId <> pv.ProductoId)
              + (SELECT COUNT(*) FROM (SELECT UPPER(TRIM(Sku)) k FROM ProductoVariantes WHERE Sku IS NOT NULL AND TRIM(Sku) <> '' GROUP BY UPPER(TRIM(Sku)) HAVING COUNT(*) > 1) x)
              + (SELECT COUNT(*) FROM (SELECT TRIM(CodigoBarras) k FROM ProductoVariantes WHERE CodigoBarras IS NOT NULL AND TRIM(CodigoBarras) <> '' GROUP BY TRIM(CodigoBarras) HAVING COUNT(*) > 1) x)
              + (SELECT COUNT(*) FROM (SELECT ProductoId FROM ProductoVariantes WHERE Eliminado = 0 GROUP BY ProductoId HAVING SUM(EsTecnica = 1) > 0 AND SUM(EsTecnica = 0) > 0) x)
              + (SELECT COUNT(*) FROM Productos p WHERE p.Eliminado = 0 AND p.ModeloId IS NOT NULL AND (p.MarcaId IS NULL OR NOT EXISTS (SELECT 1 FROM Modelos m WHERE m.Id = p.ModeloId AND m.MarcaId = p.MarcaId)))
              + (SELECT COUNT(*) FROM Productos p WHERE p.Eliminado = 0 AND NOT EXISTS (SELECT 1 FROM ProductoVariantes pv WHERE pv.ProductoId = p.Id AND pv.Eliminado = 0) AND EXISTS (SELECT 1 FROM ProductoVariantes z WHERE UPPER(TRIM(z.Sku)) = UPPER(CONCAT('TEC-', LPAD(p.Id, 10, '0')))));
            DROP TEMPORARY TABLE __PreflightN03;
            """);

        migrationBuilder.Sql("""
            UPDATE ProductoVariantes
               SET Sku = UPPER(TRIM(Sku)),
                   CodigoBarras = NULLIF(TRIM(CodigoBarras), '')
             WHERE Sku IS NOT NULL OR CodigoBarras IS NOT NULL;

            INSERT INTO ProductoVariantes
                (ProductoId, MarcaId, ModeloId, ColorId, TallaId, Sku, CodigoBarras,
                 Cantidad, UmbralStockBajo, Costo, Precio, EsTecnica, Activo, Eliminado,
                 FechaCreacion, FechaActualizacion, CreadoPorNombreUsuario)
            SELECT p.Id, p.MarcaId, p.ModeloId, p.ColorId, p.TallaId,
                   CONCAT('TEC-', LPAD(p.Id, 10, '0')), NULL,
                   p.Cantidad, p.UmbralStockBajo, p.Costo, p.Precio, 1, p.Activo, 0,
                   UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), 'ERP-N0.3 backfill'
              FROM Productos p
             WHERE p.Eliminado = 0
               AND NOT EXISTS (SELECT 1 FROM ProductoVariantes pv WHERE pv.ProductoId = p.Id AND pv.Eliminado = 0);

            UPDATE ProductoVariantes pv
            JOIN Productos p ON p.Id = pv.ProductoId
               SET pv.MarcaId = COALESCE(pv.MarcaId, p.MarcaId),
                   pv.ModeloId = COALESCE(pv.ModeloId, p.ModeloId),
                   pv.ColorId = COALESCE(pv.ColorId, p.ColorId),
                   pv.TallaId = COALESCE(pv.TallaId, p.TallaId),
                   pv.Costo = COALESCE(pv.Costo, p.Costo),
                   pv.Precio = COALESCE(pv.Precio, p.Precio),
                   pv.FechaActualizacion = UTC_TIMESTAMP(6)
             WHERE pv.Eliminado = 0 AND pv.EsTecnica = 1;
            """);

        migrationBuilder.Sql("ALTER TABLE ProductoVariantes ADD CONSTRAINT CK_ProductoVariantes_N03_Sku CHECK (Sku IS NOT NULL AND TRIM(Sku) <> '');");
        migrationBuilder.Sql("ALTER TABLE ProductoVariantes ADD CONSTRAINT CK_ProductoVariantes_N03_Barcode CHECK (CodigoBarras IS NULL OR TRIM(CodigoBarras) <> '');");
        migrationBuilder.Sql("ALTER TABLE ProductoVariantes ADD CONSTRAINT CK_ProductoVariantes_N03_Stock CHECK (Cantidad >= 0 AND UmbralStockBajo >= 0);");
        migrationBuilder.Sql("ALTER TABLE ProductoVariantes ADD CONSTRAINT CK_ProductoVariantes_N03_Importes CHECK ((Costo IS NULL OR Costo >= 0) AND (Precio IS NULL OR Precio >= 0));");
        migrationBuilder.Sql("ALTER TABLE ProductoVariantes ADD CONSTRAINT CK_ProductoVariantes_N03_ModeloMarca CHECK (ModeloId IS NULL OR MarcaId IS NOT NULL);");
        migrationBuilder.Sql("ALTER TABLE ProductoVariantes ADD CONSTRAINT CK_ProductoVariantes_N03_TecnicaBarcode CHECK (EsTecnica = 0 OR CodigoBarras IS NULL);");

        migrationBuilder.Sql("CREATE UNIQUE INDEX UX_Modelos_Id_MarcaId_N03 ON Modelos (Id, MarcaId);");
        migrationBuilder.Sql("ALTER TABLE ProductoVariantes ADD CONSTRAINT FK_ProductoVariantes_Modelos_ModeloMarca_N03 FOREIGN KEY (ModeloId, MarcaId) REFERENCES Modelos (Id, MarcaId) ON DELETE RESTRICT;");
        migrationBuilder.Sql("CREATE UNIQUE INDEX UX_ProductoVariantes_Id_ProductoId_N03 ON ProductoVariantes (Id, ProductoId);");
        migrationBuilder.Sql("ALTER TABLE ProductoImagenes ADD CONSTRAINT FK_ProductoImagenes_VarianteProducto_N03 FOREIGN KEY (ProductoVarianteId, ProductoId) REFERENCES ProductoVariantes (Id, ProductoId) ON DELETE RESTRICT;");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("ERP-N0.3 es forward-only. Para revertir constraints o backfill debe restaurarse el respaldo/preflight anterior a N0.3.");
}
