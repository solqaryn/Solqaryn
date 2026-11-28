using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Solqaryn.Infrastructure.Persistence;

namespace Solqaryn.Infrastructure.Migrations.Oracle;

[DbContext(typeof(AppDbContext))]
[Migration("20261128183000_QaCheckoutPedidoV6")]
public sealed class QaCheckoutPedidoV6 : Migration
{
    protected override void Up(MigrationBuilder m) =>
        m.Sql("ALTER TABLE `PedidosVenta` ADD COLUMN `CostoEnvio` decimal(18,2) NULL, ADD COLUMN `MonedaCheckout` varchar(3) NULL, ADD COLUMN `TarifaEnvioCheckout` varchar(120) NULL, ADD COLUMN `EntregaCheckoutJson` longtext NULL, ADD CONSTRAINT `CK_PedidosVenta_Checkout_Atomico` CHECK (((`CostoEnvio` IS NULL AND `MonedaCheckout` IS NULL AND `TarifaEnvioCheckout` IS NULL AND `EntregaCheckoutJson` IS NULL) OR (`CostoEnvio` IS NOT NULL AND `CostoEnvio` >= 0 AND `MonedaCheckout` IS NOT NULL AND CHAR_LENGTH(`MonedaCheckout`) = 3 AND `TarifaEnvioCheckout` IS NOT NULL AND `EntregaCheckoutJson` IS NOT NULL AND `CotizacionId` IS NULL)))");

    protected override void Down(MigrationBuilder m) =>
        throw new NotSupportedException("Use the verified pre-migration backup; never discard registered order snapshots.");
}
