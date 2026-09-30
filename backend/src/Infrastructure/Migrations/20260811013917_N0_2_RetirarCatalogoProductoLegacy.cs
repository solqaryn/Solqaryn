using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class N0_2_RetirarCatalogoProductoLegacy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_Colores_ColorId",
                table: "Productos",
                column: "ColorId",
                principalTable: "Colores",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_Marcas_MarcaId",
                table: "Productos",
                column: "MarcaId",
                principalTable: "Marcas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_Modelos_ModeloId",
                table: "Productos",
                column: "ModeloId",
                principalTable: "Modelos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_Tallas_TallaId",
                table: "Productos",
                column: "TallaId",
                principalTable: "Tallas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException(
                "ERP-N0.2 es una migración forward-only. CatalogosProducto no puede reconstruirse de forma segura " +
                "porque Marca, Modelo, Color y Talla ya utilizan espacios de identidad independientes. " +
                "Para volver a un estado anterior debe restaurarse un respaldo previo a ERP-N0.2.");
        }
    }
}
