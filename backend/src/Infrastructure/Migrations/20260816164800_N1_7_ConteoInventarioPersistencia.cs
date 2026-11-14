using System;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260816164800_N1_7_ConteoInventarioPersistencia")]
    public partial class N1_7_ConteoInventarioPersistencia : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N17CGuard;
                CREATE TEMPORARY TABLE __N17CGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N17C_Guard_Cero CHECK (Violaciones = 0)
                );
                INSERT INTO __N17CGuard (Id, Violaciones)
                SELECT 1, COUNT(*) FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name IN ('ConteosInventario','ConteoInventarioDetalles');
                INSERT INTO __N17CGuard (Id, Violaciones)
                SELECT 2, CASE WHEN COUNT(*) = 4 THEN 0 ELSE 1 END
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name IN ('Almacenes','UbicacionesAlmacen','ProductoVariantes','AjustesInventario');
                INSERT INTO __N17CGuard (Id, Violaciones)
                SELECT 3, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
                  FROM information_schema.table_constraints
                 WHERE constraint_schema = DATABASE()
                   AND table_name = 'UbicacionesAlmacen'
                   AND constraint_name = 'AK_UbicacionesAlmacen_AlmacenId_Id'
                   AND constraint_type = 'UNIQUE';
                DROP TEMPORARY TABLE __N17CGuard;
                """);

            migrationBuilder.CreateTable(
                name: "ConteosInventario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Numero = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    AlmacenId = table.Column<int>(type: "int", nullable: false),
                    UbicacionAlmacenId = table.Column<int>(type: "int", nullable: true),
                    CategoriaId = table.Column<int>(type: "int", nullable: true),
                    EsCiego = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Observaciones = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    FechaInicio = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IniciadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    FechaCierre = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CerradoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    FechaAprobacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AprobadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    FechaCancelacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CanceladoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    MotivoCancelacion = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CreadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ActualizadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConteosInventario", x => x.Id);
                    table.UniqueConstraint("AK_ConteosInventario_Id_AlmacenId", x => new { x.Id, x.AlmacenId });
                    table.ForeignKey("FK_ConteosInventario_Almacenes_AlmacenId", x => x.AlmacenId, "Almacenes", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_ConteosInventario_Categorias_CategoriaId", x => x.CategoriaId, "Categorias", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConteosInventario_Ubicacion_MismoAlmacen",
                        columns: x => new { x.AlmacenId, x.UbicacionAlmacenId },
                        principalTable: "UbicacionesAlmacen",
                        principalColumns: new[] { "AlmacenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                }).Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ConteoInventarioDetalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ConteoInventarioId = table.Column<int>(type: "int", nullable: false),
                    ProductoVarianteId = table.Column<int>(type: "int", nullable: false),
                    AlmacenId = table.Column<int>(type: "int", nullable: false),
                    UbicacionAlmacenId = table.Column<int>(type: "int", nullable: true),
                    StockEsperadoSnapshot = table.Column<int>(type: "int", nullable: false),
                    SnapshotMaterializado = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    CantidadContada = table.Column<int>(type: "int", nullable: true),
                    Diferencia = table.Column<int>(type: "int", nullable: true),
                    FechaConteo = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ContadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    AjusteInventarioId = table.Column<int>(type: "int", nullable: true),
                    ProductoSkuSnapshot = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    ProductoMarcaSnapshot = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    ProductoModeloSnapshot = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    ProductoColorSnapshot = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    ProductoTallaSnapshot = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    UbicacionNormalizada = table.Column<int>(type: "int", nullable: false, computedColumnSql: "COALESCE(`UbicacionAlmacenId`, 0)", stored: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CreadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ActualizadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConteoInventarioDetalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConteoInventarioDetalles_Conteo_MismoAlmacen",
                        columns: x => new { x.ConteoInventarioId, x.AlmacenId },
                        principalTable: "ConteosInventario",
                        principalColumns: new[] { "Id", "AlmacenId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_ConteoDetalles_ProductoVariantes_ProductoVarianteId", x => x.ProductoVarianteId, "ProductoVariantes", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_ConteoDetalles_Almacenes_AlmacenId", x => x.AlmacenId, "Almacenes", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_ConteoDetalles_AjustesInventario_AjusteInventarioId", x => x.AjusteInventarioId, "AjustesInventario", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConteoDetalles_Ubicacion_MismoAlmacen",
                        columns: x => new { x.AlmacenId, x.UbicacionAlmacenId },
                        principalTable: "UbicacionesAlmacen",
                        principalColumns: new[] { "AlmacenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                }).Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex("UX_ConteosInventario_Numero", "ConteosInventario", "Numero", unique: true);
            migrationBuilder.CreateIndex("IX_ConteosInventario_Almacen_Estado", "ConteosInventario", new[] { "AlmacenId", "Estado" });
            migrationBuilder.CreateIndex("IX_ConteosInventario_Tipo_Estado", "ConteosInventario", new[] { "Tipo", "Estado" });
            migrationBuilder.CreateIndex("IX_ConteosInventario_UbicacionAlmacenId", "ConteosInventario", "UbicacionAlmacenId");
            migrationBuilder.CreateIndex("IX_ConteosInventario_CategoriaId", "ConteosInventario", "CategoriaId");
            migrationBuilder.CreateIndex("IX_ConteosInventario_AlmacenId_UbicacionAlmacenId", "ConteosInventario", new[] { "AlmacenId", "UbicacionAlmacenId" });

            migrationBuilder.CreateIndex("IX_ConteoDetalles_AjusteInventarioId", "ConteoInventarioDetalles", "AjusteInventarioId");
            migrationBuilder.CreateIndex("IX_ConteoDetalles_AlmacenId_UbicacionAlmacenId", "ConteoInventarioDetalles", new[] { "AlmacenId", "UbicacionAlmacenId" });
            migrationBuilder.CreateIndex("IX_ConteoDetalles_ConteoInventarioId_AlmacenId", "ConteoInventarioDetalles", new[] { "ConteoInventarioId", "AlmacenId" });
            migrationBuilder.CreateIndex("IX_ConteoDetalles_ExistenciaFisica", "ConteoInventarioDetalles", new[] { "ProductoVarianteId", "AlmacenId", "UbicacionAlmacenId" });
            migrationBuilder.CreateIndex("UX_ConteoDetalles_ClaveFisica", "ConteoInventarioDetalles", new[] { "ConteoInventarioId", "ProductoVarianteId", "AlmacenId", "UbicacionNormalizada" }, unique: true);

            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N17CPostGuard;
                CREATE TEMPORARY TABLE __N17CPostGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N17C_PostGuard_Cero CHECK (Violaciones = 0)
                );
                INSERT INTO __N17CPostGuard (Id, Violaciones)
                SELECT 1, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name IN ('ConteosInventario','ConteoInventarioDetalles');
                INSERT INTO __N17CPostGuard (Id, Violaciones)
                SELECT 2, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
                  FROM information_schema.table_constraints
                 WHERE constraint_schema = DATABASE()
                   AND table_name = 'ConteosInventario'
                   AND constraint_name = 'AK_ConteosInventario_Id_AlmacenId'
                   AND constraint_type = 'UNIQUE';
                INSERT INTO __N17CPostGuard (Id, Violaciones)
                SELECT 3, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
                  FROM information_schema.statistics
                 WHERE table_schema = DATABASE()
                   AND table_name = 'ConteoInventarioDetalles'
                   AND index_name = 'UX_ConteoDetalles_ClaveFisica'
                   AND non_unique = 0
                   AND seq_in_index = 1;
                DROP TEMPORARY TABLE __N17CPostGuard;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ConteoInventarioDetalles");
            migrationBuilder.DropTable(name: "ConteosInventario");
        }
    }
}
