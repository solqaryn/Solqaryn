using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTipoCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TipoClientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Codigo = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EsSistema = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NombreNormalizado = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ColorHex = table.Column<string>(type: "varchar(7)", maxLength: 7, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    EsPredeterminado = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Eliminado = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EliminadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_TipoClientes", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TipoClientes_Codigo",
                table: "TipoClientes",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TipoClientes_NombreNormalizado",
                table: "TipoClientes",
                column: "NombreNormalizado",
                unique: true);

            // Seed initial SIN_CLASIFICAR client type
            migrationBuilder.Sql("INSERT INTO TipoClientes (Codigo, EsSistema, Nombre, NombreNormalizado, Descripcion, ColorHex, Activo, Orden, EsPredeterminado, Eliminado, FechaCreacion, FechaActualizacion) VALUES ('SIN_CLASIFICAR', 1, 'Sin clasificar', 'SIN CLASIFICAR', 'Cliente sin clasificar', '#9E9E9E', 1, 0, 1, 0, UTC_TIMESTAMP(), UTC_TIMESTAMP());");

            // Add new column as nullable first
            migrationBuilder.AddColumn<int>(
                name: "TipoClienteId",
                table: "Clientes",
                type: "int",
                nullable: true);

            // Backfill existing Clientes to point to SIN_CLASIFICAR
            migrationBuilder.Sql("UPDATE Clientes SET TipoClienteId = (SELECT Id FROM TipoClientes WHERE Codigo = 'SIN_CLASIFICAR' LIMIT 1);");

            // Alter column to be NOT NULL now that it is populated
            migrationBuilder.AlterColumn<int>(
                name: "TipoClienteId",
                table: "Clientes",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_TipoClienteId",
                table: "Clientes",
                column: "TipoClienteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clientes_TipoClientes_TipoClienteId",
                table: "Clientes",
                column: "TipoClienteId",
                principalTable: "TipoClientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clientes_TipoClientes_TipoClienteId",
                table: "Clientes");

            migrationBuilder.DropTable(
                name: "TipoClientes");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_TipoClienteId",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "TipoClienteId",
                table: "Clientes");
        }
    }
}
