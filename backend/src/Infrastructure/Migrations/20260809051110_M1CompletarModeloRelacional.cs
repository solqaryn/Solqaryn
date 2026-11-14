using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class M1CompletarModeloRelacional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Facturas_Ventas_VentaId",
                table: "Facturas");

            migrationBuilder.AlterColumn<string>(
                name: "ImpuestoNombreSnapshot",
                table: "VentaImpuestos",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "DescuentoNombreSnapshot",
                table: "VentaDescuentos",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "DescuentoCodigoSnapshot",
                table: "VentaDescuentos",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<decimal>(
                name: "MontoAplicado",
                table: "HistorialUsoDescuentos",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TasaAplicada",
                table: "HistorialAplicacionImpuestos",
                type: "decimal(9,4)",
                precision: 9,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");

            migrationBuilder.AlterColumn<decimal>(
                name: "MontoAplicado",
                table: "HistorialAplicacionImpuestos",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");

            migrationBuilder.AlterColumn<string>(
                name: "DocumentoTipo",
                table: "HistorialAplicacionImpuestos",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<decimal>(
                name: "BaseImponible",
                table: "HistorialAplicacionImpuestos",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");

            migrationBuilder.AlterColumn<string>(
                name: "ImpuestoNombreSnapshot",
                table: "CompraImpuestos",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_VentaImpuestos_ImpuestoId",
                table: "VentaImpuestos",
                column: "ImpuestoId");

            migrationBuilder.CreateIndex(
                name: "IX_VentaDescuentos_DescuentoId",
                table: "VentaDescuentos",
                column: "DescuentoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosFinancieros_CompraId",
                table: "MovimientosFinancieros",
                column: "CompraId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosFinancieros_Estado_Fecha",
                table: "MovimientosFinancieros",
                columns: new[] { "Estado", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosFinancieros_FacturaId",
                table: "MovimientosFinancieros",
                column: "FacturaId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosFinancieros_ModuloOrigen_ReferenciaId",
                table: "MovimientosFinancieros",
                columns: new[] { "ModuloOrigen", "ReferenciaId" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosFinancieros_VentaId",
                table: "MovimientosFinancieros",
                column: "VentaId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialUsoDescuentos_ClienteId",
                table: "HistorialUsoDescuentos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialUsoDescuentos_VentaId_Fecha",
                table: "HistorialUsoDescuentos",
                columns: new[] { "VentaId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialAplicacionImpuestos_DocumentoTipo_DocumentoId",
                table: "HistorialAplicacionImpuestos",
                columns: new[] { "DocumentoTipo", "DocumentoId" });

            migrationBuilder.CreateIndex(
                name: "IX_CompraImpuestos_ImpuestoId",
                table: "CompraImpuestos",
                column: "ImpuestoId");

            migrationBuilder.AddForeignKey(
                name: "FK_CompraImpuestos_Impuestos_ImpuestoId",
                table: "CompraImpuestos",
                column: "ImpuestoId",
                principalTable: "Impuestos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Facturas_Ventas_VentaId",
                table: "Facturas",
                column: "VentaId",
                principalTable: "Ventas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HistorialUsoDescuentos_Clientes_ClienteId",
                table: "HistorialUsoDescuentos",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HistorialUsoDescuentos_Ventas_VentaId",
                table: "HistorialUsoDescuentos",
                column: "VentaId",
                principalTable: "Ventas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosFinancieros_Compras_CompraId",
                table: "MovimientosFinancieros",
                column: "CompraId",
                principalTable: "Compras",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosFinancieros_Facturas_FacturaId",
                table: "MovimientosFinancieros",
                column: "FacturaId",
                principalTable: "Facturas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosFinancieros_Ventas_VentaId",
                table: "MovimientosFinancieros",
                column: "VentaId",
                principalTable: "Ventas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VentaDescuentos_Descuentos_DescuentoId",
                table: "VentaDescuentos",
                column: "DescuentoId",
                principalTable: "Descuentos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VentaImpuestos_Impuestos_ImpuestoId",
                table: "VentaImpuestos",
                column: "ImpuestoId",
                principalTable: "Impuestos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompraImpuestos_Impuestos_ImpuestoId",
                table: "CompraImpuestos");

            migrationBuilder.DropForeignKey(
                name: "FK_Facturas_Ventas_VentaId",
                table: "Facturas");

            migrationBuilder.DropForeignKey(
                name: "FK_HistorialUsoDescuentos_Clientes_ClienteId",
                table: "HistorialUsoDescuentos");

            migrationBuilder.DropForeignKey(
                name: "FK_HistorialUsoDescuentos_Ventas_VentaId",
                table: "HistorialUsoDescuentos");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosFinancieros_Compras_CompraId",
                table: "MovimientosFinancieros");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosFinancieros_Facturas_FacturaId",
                table: "MovimientosFinancieros");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosFinancieros_Ventas_VentaId",
                table: "MovimientosFinancieros");

            migrationBuilder.DropForeignKey(
                name: "FK_VentaDescuentos_Descuentos_DescuentoId",
                table: "VentaDescuentos");

            migrationBuilder.DropForeignKey(
                name: "FK_VentaImpuestos_Impuestos_ImpuestoId",
                table: "VentaImpuestos");

            migrationBuilder.DropIndex(
                name: "IX_VentaImpuestos_ImpuestoId",
                table: "VentaImpuestos");

            migrationBuilder.DropIndex(
                name: "IX_VentaDescuentos_DescuentoId",
                table: "VentaDescuentos");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosFinancieros_CompraId",
                table: "MovimientosFinancieros");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosFinancieros_Estado_Fecha",
                table: "MovimientosFinancieros");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosFinancieros_FacturaId",
                table: "MovimientosFinancieros");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosFinancieros_ModuloOrigen_ReferenciaId",
                table: "MovimientosFinancieros");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosFinancieros_VentaId",
                table: "MovimientosFinancieros");

            migrationBuilder.DropIndex(
                name: "IX_HistorialUsoDescuentos_ClienteId",
                table: "HistorialUsoDescuentos");

            migrationBuilder.DropIndex(
                name: "IX_HistorialUsoDescuentos_VentaId_Fecha",
                table: "HistorialUsoDescuentos");

            migrationBuilder.DropIndex(
                name: "IX_HistorialAplicacionImpuestos_DocumentoTipo_DocumentoId",
                table: "HistorialAplicacionImpuestos");

            migrationBuilder.DropIndex(
                name: "IX_CompraImpuestos_ImpuestoId",
                table: "CompraImpuestos");

            migrationBuilder.AlterColumn<string>(
                name: "ImpuestoNombreSnapshot",
                table: "VentaImpuestos",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(150)",
                oldMaxLength: 150)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "DescuentoNombreSnapshot",
                table: "VentaDescuentos",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(150)",
                oldMaxLength: 150)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "DescuentoCodigoSnapshot",
                table: "VentaDescuentos",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<decimal>(
                name: "MontoAplicado",
                table: "HistorialUsoDescuentos",
                type: "decimal(65,30)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "TasaAplicada",
                table: "HistorialAplicacionImpuestos",
                type: "decimal(65,30)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,4)",
                oldPrecision: 9,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "MontoAplicado",
                table: "HistorialAplicacionImpuestos",
                type: "decimal(65,30)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<string>(
                name: "DocumentoTipo",
                table: "HistorialAplicacionImpuestos",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<decimal>(
                name: "BaseImponible",
                table: "HistorialAplicacionImpuestos",
                type: "decimal(65,30)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<string>(
                name: "ImpuestoNombreSnapshot",
                table: "CompraImpuestos",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(150)",
                oldMaxLength: 150)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddForeignKey(
                name: "FK_Facturas_Ventas_VentaId",
                table: "Facturas",
                column: "VentaId",
                principalTable: "Ventas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
