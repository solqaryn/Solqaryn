using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fase6SeguridadFacturacionPerfil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Eliminado",
                table: "Ventas",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "EliminadoPorUsuarioId",
                table: "Ventas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaEliminacion",
                table: "Ventas",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IncluidoEnPrecioSnapshot",
                table: "VentaImpuestos",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FotoPerfilPublicId",
                table: "Usuarios",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FotoPerfilUrl",
                table: "Usuarios",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Productos",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "Eliminado",
                table: "Productos",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "EliminadoPorUsuarioId",
                table: "Productos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaEliminacion",
                table: "Productos",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Eliminado",
                table: "Compras",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "EliminadoPorUsuarioId",
                table: "Compras",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaEliminacion",
                table: "Compras",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IncluidoEnPrecioSnapshot",
                table: "CompraImpuestos",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CompraDocumentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CompraId = table.Column<int>(type: "int", nullable: false),
                    NombreOriginal = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContentType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Url = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PublicId = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResourceType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Eliminado = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CreadoPorNombreUsuario = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EliminadoPorUsuarioId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompraDocumentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompraDocumentos_Compras_CompraId",
                        column: x => x.CompraId,
                        principalTable: "Compras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FotoPerfilPublicId", "FotoPerfilUrl" },
                values: new object[] { null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_Eliminado",
                table: "Ventas",
                column: "Eliminado");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_Estado",
                table: "Productos",
                columns: new[] { "Eliminado", "Activo" });

            migrationBuilder.CreateIndex(
                name: "IX_Compras_Eliminado",
                table: "Compras",
                column: "Eliminado");

            migrationBuilder.CreateIndex(
                name: "IX_CompraDocumentos_CompraId_Eliminado",
                table: "CompraDocumentos",
                columns: new[] { "CompraId", "Eliminado" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompraDocumentos");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_Eliminado",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_Productos_Estado",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_Compras_Eliminado",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "Eliminado",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "EliminadoPorUsuarioId",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "FechaEliminacion",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "IncluidoEnPrecioSnapshot",
                table: "VentaImpuestos");

            migrationBuilder.DropColumn(
                name: "FotoPerfilPublicId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "FotoPerfilUrl",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "Eliminado",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "EliminadoPorUsuarioId",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "FechaEliminacion",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "Eliminado",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "EliminadoPorUsuarioId",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "FechaEliminacion",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "IncluidoEnPrecioSnapshot",
                table: "CompraImpuestos");
        }
    }
}
