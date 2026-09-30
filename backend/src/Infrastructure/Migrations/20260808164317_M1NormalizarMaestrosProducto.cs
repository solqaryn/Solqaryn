using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class M1NormalizarMaestrosProducto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Colores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
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
                    NombreActivoUnico = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true, computedColumnSql: "IF(Eliminado = 0, LOWER(TRIM(Nombre)), NULL)", stored: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                    table.PrimaryKey("PK_Colores", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Marcas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    Eliminado = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EliminadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    NombreActivoUnico = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true, computedColumnSql: "IF(Eliminado = 0, LOWER(TRIM(Nombre)), NULL)", stored: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                    table.PrimaryKey("PK_Marcas", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Tallas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    Eliminado = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EliminadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    NombreActivoUnico = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true, computedColumnSql: "IF(Eliminado = 0, LOWER(TRIM(Nombre)), NULL)", stored: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                    table.PrimaryKey("PK_Tallas", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Modelos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    MarcaId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    Eliminado = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EliminadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    NombreMarcaActivoUnico = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: true, computedColumnSql: "IF(Eliminado = 0, CONCAT(CAST(MarcaId AS CHAR), ':', LOWER(TRIM(Nombre))), NULL)", stored: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                    table.PrimaryKey("PK_Modelos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Modelos_Marcas_MarcaId",
                        column: x => x.MarcaId,
                        principalTable: "Marcas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // Backfill no destructivo: conserva exactamente los IDs del catálogo legacy
            // para que Productos/ProductoVariantes sigan siendo compatibles hasta M2.
            migrationBuilder.Sql(@"
INSERT INTO `Marcas`
    (`Id`,`Nombre`,`Descripcion`,`Orden`,`Activo`,`Eliminado`,`FechaEliminacion`,`EliminadoPorUsuarioId`,`FechaCreacion`,`FechaActualizacion`,`CreadoPorUsuarioId`,`CreadoPorNombreUsuario`,`ActualizadoPorUsuarioId`,`ActualizadoPorNombreUsuario`)
SELECT `Id`,`Nombre`,`Descripcion`,`Orden`,`Activo`,`Eliminado`,`FechaEliminacion`,`EliminadoPorUsuarioId`,`FechaCreacion`,`FechaActualizacion`,`CreadoPorUsuarioId`,`CreadoPorNombreUsuario`,`ActualizadoPorUsuarioId`,`ActualizadoPorNombreUsuario`
FROM `CatalogosProducto` WHERE `Tipo` = 'Marca';

INSERT INTO `Colores`
    (`Id`,`Nombre`,`Descripcion`,`CodigoVisual`,`Orden`,`Activo`,`Eliminado`,`FechaEliminacion`,`EliminadoPorUsuarioId`,`FechaCreacion`,`FechaActualizacion`,`CreadoPorUsuarioId`,`CreadoPorNombreUsuario`,`ActualizadoPorUsuarioId`,`ActualizadoPorNombreUsuario`)
SELECT `Id`,`Nombre`,`Descripcion`,`CodigoVisual`,`Orden`,`Activo`,`Eliminado`,`FechaEliminacion`,`EliminadoPorUsuarioId`,`FechaCreacion`,`FechaActualizacion`,`CreadoPorUsuarioId`,`CreadoPorNombreUsuario`,`ActualizadoPorUsuarioId`,`ActualizadoPorNombreUsuario`
FROM `CatalogosProducto` WHERE `Tipo` = 'Color';

INSERT INTO `Tallas`
    (`Id`,`Nombre`,`Descripcion`,`Orden`,`Activo`,`Eliminado`,`FechaEliminacion`,`EliminadoPorUsuarioId`,`FechaCreacion`,`FechaActualizacion`,`CreadoPorUsuarioId`,`CreadoPorNombreUsuario`,`ActualizadoPorUsuarioId`,`ActualizadoPorNombreUsuario`)
SELECT `Id`,`Nombre`,`Descripcion`,`Orden`,`Activo`,`Eliminado`,`FechaEliminacion`,`EliminadoPorUsuarioId`,`FechaCreacion`,`FechaActualizacion`,`CreadoPorUsuarioId`,`CreadoPorNombreUsuario`,`ActualizadoPorUsuarioId`,`ActualizadoPorNombreUsuario`
FROM `CatalogosProducto` WHERE `Tipo` = 'Talla';

INSERT INTO `Modelos`
    (`Id`,`MarcaId`,`Nombre`,`Descripcion`,`Orden`,`Activo`,`Eliminado`,`FechaEliminacion`,`EliminadoPorUsuarioId`,`FechaCreacion`,`FechaActualizacion`,`CreadoPorUsuarioId`,`CreadoPorNombreUsuario`,`ActualizadoPorUsuarioId`,`ActualizadoPorNombreUsuario`)
SELECT `Id`,`CatalogoPadreId`,`Nombre`,`Descripcion`,`Orden`,`Activo`,`Eliminado`,`FechaEliminacion`,`EliminadoPorUsuarioId`,`FechaCreacion`,`FechaActualizacion`,`CreadoPorUsuarioId`,`CreadoPorNombreUsuario`,`ActualizadoPorUsuarioId`,`ActualizadoPorNombreUsuario`
FROM `CatalogosProducto` WHERE `Tipo` = 'Modelo';
");

            migrationBuilder.CreateIndex(
                name: "IX_Colores_Estado",
                table: "Colores",
                columns: new[] { "Activo", "Eliminado" });

            migrationBuilder.CreateIndex(
                name: "UX_Colores_Nombre_Activo",
                table: "Colores",
                column: "NombreActivoUnico",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Marcas_Estado",
                table: "Marcas",
                columns: new[] { "Activo", "Eliminado" });

            migrationBuilder.CreateIndex(
                name: "UX_Marcas_Nombre_Activo",
                table: "Marcas",
                column: "NombreActivoUnico",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Modelos_Marca_Estado",
                table: "Modelos",
                columns: new[] { "MarcaId", "Activo", "Eliminado" });

            migrationBuilder.CreateIndex(
                name: "UX_Modelos_Marca_Nombre_Activo",
                table: "Modelos",
                column: "NombreMarcaActivoUnico",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tallas_Estado",
                table: "Tallas",
                columns: new[] { "Activo", "Eliminado" });

            migrationBuilder.CreateIndex(
                name: "UX_Tallas_Nombre_Activo",
                table: "Tallas",
                column: "NombreActivoUnico",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Colores");

            migrationBuilder.DropTable(
                name: "Modelos");

            migrationBuilder.DropTable(
                name: "Tallas");

            migrationBuilder.DropTable(
                name: "Marcas");
        }
    }
}
