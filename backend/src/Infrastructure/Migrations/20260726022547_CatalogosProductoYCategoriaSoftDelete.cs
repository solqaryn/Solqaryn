using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CatalogosProductoYCategoriaSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Categorias_Nombre",
                table: "Categorias");

            migrationBuilder.AddColumn<int>(
                name: "ColorId",
                table: "Productos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MarcaId",
                table: "Productos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModeloId",
                table: "Productos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TallaId",
                table: "Productos",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "Activa",
                table: "Categorias",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)");

            migrationBuilder.AddColumn<bool>(
                name: "Eliminada",
                table: "Categorias",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "EliminadaPorUsuarioId",
                table: "Categorias",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaEliminacion",
                table: "Categorias",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CatalogosProducto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Tipo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nombre = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CodigoVisual = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    Eliminado = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EliminadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CatalogoPadreId = table.Column<int>(type: "int", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CreadoPorNombreUsuario = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ActualizadoPorNombreUsuario = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogosProducto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogosProducto_CatalogosProducto_CatalogoPadreId",
                        column: x => x.CatalogoPadreId,
                        principalTable: "CatalogosProducto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // Conserva los datos anteriores: cada texto de Marca/Modelo se
            // convierte en un catálogo administrable y el producto queda
            // relacionado sin perder sus snapshots históricos.
            migrationBuilder.Sql(@"
INSERT INTO CatalogosProducto
    (Tipo, Nombre, Descripcion, CodigoVisual, Orden, Activo, Eliminado,
     FechaEliminacion, EliminadoPorUsuarioId, CatalogoPadreId,
     FechaCreacion, FechaActualizacion, CreadoPorUsuarioId,
     CreadoPorNombreUsuario, ActualizadoPorUsuarioId, ActualizadoPorNombreUsuario)
SELECT 'Marca', TRIM(p.Marca), NULL, NULL, 0, 1, 0,
       NULL, NULL, NULL, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6),
       NULL, 'Migración', NULL, NULL
FROM Productos p
WHERE p.Marca IS NOT NULL AND TRIM(p.Marca) <> ''
GROUP BY TRIM(p.Marca);

INSERT INTO CatalogosProducto
    (Tipo, Nombre, Descripcion, CodigoVisual, Orden, Activo, Eliminado,
     FechaEliminacion, EliminadoPorUsuarioId, CatalogoPadreId,
     FechaCreacion, FechaActualizacion, CreadoPorUsuarioId,
     CreadoPorNombreUsuario, ActualizadoPorUsuarioId, ActualizadoPorNombreUsuario)
SELECT 'Modelo', TRIM(p.Modelo), NULL, NULL, 0, 1, 0,
       NULL, NULL, marca.Id, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6),
       NULL, 'Migración', NULL, NULL
FROM Productos p
INNER JOIN CatalogosProducto marca
    ON marca.Tipo = 'Marca'
   AND LOWER(marca.Nombre) = LOWER(TRIM(p.Marca))
WHERE p.Modelo IS NOT NULL AND TRIM(p.Modelo) <> ''
GROUP BY TRIM(p.Modelo), marca.Id;

UPDATE Productos p
INNER JOIN CatalogosProducto marca
    ON marca.Tipo = 'Marca'
   AND LOWER(marca.Nombre) = LOWER(TRIM(p.Marca))
SET p.MarcaId = marca.Id
WHERE p.Marca IS NOT NULL AND TRIM(p.Marca) <> '';

UPDATE Productos p
INNER JOIN CatalogosProducto marca
    ON marca.Id = p.MarcaId AND marca.Tipo = 'Marca'
INNER JOIN CatalogosProducto modelo
    ON modelo.Tipo = 'Modelo'
   AND modelo.CatalogoPadreId = marca.Id
   AND LOWER(modelo.Nombre) = LOWER(TRIM(p.Modelo))
SET p.ModeloId = modelo.Id
WHERE p.Modelo IS NOT NULL AND TRIM(p.Modelo) <> '';");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_ColorId",
                table: "Productos",
                column: "ColorId");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_MarcaId",
                table: "Productos",
                column: "MarcaId");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_ModeloId",
                table: "Productos",
                column: "ModeloId");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_TallaId",
                table: "Productos",
                column: "TallaId");

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_Estado",
                table: "Categorias",
                columns: new[] { "Eliminada", "Activa" });

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_Nombre",
                table: "Categorias",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogosProducto_CatalogoPadreId",
                table: "CatalogosProducto",
                column: "CatalogoPadreId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogosProducto_Estado",
                table: "CatalogosProducto",
                columns: new[] { "Tipo", "Activo", "Eliminado" });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogosProducto_Tipo_Nombre_Padre",
                table: "CatalogosProducto",
                columns: new[] { "Tipo", "Nombre", "CatalogoPadreId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_CatalogosProducto_ColorId",
                table: "Productos",
                column: "ColorId",
                principalTable: "CatalogosProducto",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_CatalogosProducto_MarcaId",
                table: "Productos",
                column: "MarcaId",
                principalTable: "CatalogosProducto",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_CatalogosProducto_ModeloId",
                table: "Productos",
                column: "ModeloId",
                principalTable: "CatalogosProducto",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_CatalogosProducto_TallaId",
                table: "Productos",
                column: "TallaId",
                principalTable: "CatalogosProducto",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Productos_CatalogosProducto_ColorId",
                table: "Productos");

            migrationBuilder.DropForeignKey(
                name: "FK_Productos_CatalogosProducto_MarcaId",
                table: "Productos");

            migrationBuilder.DropForeignKey(
                name: "FK_Productos_CatalogosProducto_ModeloId",
                table: "Productos");

            migrationBuilder.DropForeignKey(
                name: "FK_Productos_CatalogosProducto_TallaId",
                table: "Productos");

            migrationBuilder.DropTable(
                name: "CatalogosProducto");

            migrationBuilder.DropIndex(
                name: "IX_Productos_ColorId",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_Productos_MarcaId",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_Productos_ModeloId",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_Productos_TallaId",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_Categorias_Estado",
                table: "Categorias");

            migrationBuilder.DropIndex(
                name: "IX_Categorias_Nombre",
                table: "Categorias");

            migrationBuilder.DropColumn(
                name: "ColorId",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "MarcaId",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "ModeloId",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "TallaId",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "Eliminada",
                table: "Categorias");

            migrationBuilder.DropColumn(
                name: "EliminadaPorUsuarioId",
                table: "Categorias");

            migrationBuilder.DropColumn(
                name: "FechaEliminacion",
                table: "Categorias");

            migrationBuilder.AlterColumn<bool>(
                name: "Activa",
                table: "Categorias",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldDefaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_Nombre",
                table: "Categorias",
                column: "Nombre",
                unique: true);
        }
    }
}
