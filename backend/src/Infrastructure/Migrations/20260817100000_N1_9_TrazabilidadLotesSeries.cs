using System;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260817100000_N1_9_TrazabilidadLotesSeries")]
    public partial class N1_9_TrazabilidadLotesSeries : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N19CGuard;
                CREATE TEMPORARY TABLE __N19CGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N19C_Guard_Cero CHECK (Violaciones = 0)
                );
                INSERT INTO __N19CGuard (Id, Violaciones)
                SELECT 1, COUNT(*)
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name IN ('LotesInventario','SeriesInventario');
                INSERT INTO __N19CGuard (Id, Violaciones)
                SELECT 2, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name = 'ProductoVariantes';
                DROP TEMPORARY TABLE __N19CGuard;
                """);

            migrationBuilder.AddColumn<bool>(
                name: "ControlaLote",
                table: "ProductoVariantes",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ControlaNumeroSerie",
                table: "ProductoVariantes",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ControlaFechaVencimiento",
                table: "ProductoVariantes",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DiasAlertaVencimiento",
                table: "ProductoVariantes",
                type: "int",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductoVariantes_TrazabilidadVencimiento",
                table: "ProductoVariantes",
                sql: "`ControlaFechaVencimiento` = 0 OR `ControlaLote` = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductoVariantes_AlertaVencimiento",
                table: "ProductoVariantes",
                sql: "(`DiasAlertaVencimiento` IS NULL AND `ControlaFechaVencimiento` = 0) OR (`ControlaFechaVencimiento` = 1 AND (`DiasAlertaVencimiento` IS NULL OR `DiasAlertaVencimiento` >= 0))");

            migrationBuilder.CreateTable(
                name: "LotesInventario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProductoVarianteId = table.Column<int>(type: "int", nullable: false),
                    Codigo = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                    FechaFabricacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CreadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ActualizadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LotesInventario", x => x.Id);
                    table.UniqueConstraint("AK_LotesInventario_Variante_Id", x => new { x.ProductoVarianteId, x.Id });
                    table.CheckConstraint("CK_LotesInventario_Fechas", "`FechaFabricacion` IS NULL OR `FechaVencimiento` IS NULL OR `FechaVencimiento` >= `FechaFabricacion`");
                    table.ForeignKey("FK_LotesInventario_ProductoVariantes_ProductoVarianteId", x => x.ProductoVarianteId, "ProductoVariantes", "Id", onDelete: ReferentialAction.Restrict);
                }).Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SeriesInventario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProductoVarianteId = table.Column<int>(type: "int", nullable: false),
                    LoteInventarioId = table.Column<int>(type: "int", nullable: true),
                    NumeroSerie = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CreadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ActualizadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeriesInventario", x => x.Id);
                    table.ForeignKey(
                        "FK_SeriesInventario_LotesInventario_Variante_Lote",
                        x => new { x.ProductoVarianteId, x.LoteInventarioId },
                        "LotesInventario",
                        new[] { "ProductoVarianteId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_SeriesInventario_ProductoVariantes_ProductoVarianteId", x => x.ProductoVarianteId, "ProductoVariantes", "Id", onDelete: ReferentialAction.Restrict);
                }).Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex("UX_LotesInventario_Variante_Codigo", "LotesInventario", new[] { "ProductoVarianteId", "Codigo" }, unique: true);
            migrationBuilder.CreateIndex("IX_LotesInventario_FechaVencimiento", "LotesInventario", "FechaVencimiento");
            migrationBuilder.CreateIndex("UX_SeriesInventario_NumeroSerie", "SeriesInventario", "NumeroSerie", unique: true);
            migrationBuilder.CreateIndex("IX_SeriesInventario_Variante_Estado", "SeriesInventario", new[] { "ProductoVarianteId", "Estado" });
            migrationBuilder.CreateIndex("IX_SeriesInventario_Variante_LoteInventarioId", "SeriesInventario", new[] { "ProductoVarianteId", "LoteInventarioId" });

            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N19CPostGuard;
                CREATE TEMPORARY TABLE __N19CPostGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N19C_PostGuard_Cero CHECK (Violaciones = 0)
                );
                INSERT INTO __N19CPostGuard (Id, Violaciones)
                SELECT 1, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name IN ('LotesInventario','SeriesInventario');
                INSERT INTO __N19CPostGuard (Id, Violaciones)
                SELECT 2, CASE WHEN COUNT(*) = 4 THEN 0 ELSE 1 END
                  FROM information_schema.columns
                 WHERE table_schema = DATABASE()
                   AND table_name = 'ProductoVariantes'
                   AND column_name IN ('ControlaLote','ControlaNumeroSerie','ControlaFechaVencimiento','DiasAlertaVencimiento');
                INSERT INTO __N19CPostGuard (Id, Violaciones)
                SELECT 3, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
                  FROM information_schema.key_column_usage
                 WHERE constraint_schema = DATABASE()
                   AND table_name = 'SeriesInventario'
                   AND constraint_name = 'FK_SeriesInventario_LotesInventario_Variante_Lote'
                   AND referenced_table_name = 'LotesInventario'
                   AND ((ordinal_position = 1 AND column_name = 'ProductoVarianteId' AND referenced_column_name = 'ProductoVarianteId')
                     OR (ordinal_position = 2 AND column_name = 'LoteInventarioId' AND referenced_column_name = 'Id'));
                DROP TEMPORARY TABLE __N19CPostGuard;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SeriesInventario");
            migrationBuilder.DropTable(name: "LotesInventario");
            migrationBuilder.DropCheckConstraint(name: "CK_ProductoVariantes_AlertaVencimiento", table: "ProductoVariantes");
            migrationBuilder.DropCheckConstraint(name: "CK_ProductoVariantes_TrazabilidadVencimiento", table: "ProductoVariantes");
            migrationBuilder.DropColumn(name: "ControlaLote", table: "ProductoVariantes");
            migrationBuilder.DropColumn(name: "ControlaNumeroSerie", table: "ProductoVariantes");
            migrationBuilder.DropColumn(name: "ControlaFechaVencimiento", table: "ProductoVariantes");
            migrationBuilder.DropColumn(name: "DiasAlertaVencimiento", table: "ProductoVariantes");
        }
    }
}
