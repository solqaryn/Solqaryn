using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ERP_N05_PermiteCambioAuditable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Cambio",
                table: "FacturaPagos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoRecibido",
                table: "FacturaPagos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Preserva la semantica historica: antes de introducir cambio auditable,
            // todo Monto persistido representaba tambien el importe efectivamente recibido.
            migrationBuilder.Sql(
                "UPDATE FacturaPagos SET MontoRecibido = Monto, Cambio = 0 WHERE MontoRecibido = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cambio",
                table: "FacturaPagos");

            migrationBuilder.DropColumn(
                name: "MontoRecibido",
                table: "FacturaPagos");
        }
    }
}
