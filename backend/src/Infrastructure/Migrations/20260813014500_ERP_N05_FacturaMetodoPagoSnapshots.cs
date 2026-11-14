using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260813014500_ERP_N05_FacturaMetodoPagoSnapshots")]
public sealed class ERP_N05_FacturaMetodoPagoSnapshots : Migration
{
    private const string FacturaPagoInsert = "TR_FacturaPagos_N05_MetodoSnapshot_BI";
    private const string FacturaPagoUpdate = "TR_FacturaPagos_N05_MetodoSnapshot_BU";
    private const string FacturaInsert = "TR_Facturas_N05_MetodoSnapshot_BI";
    private const string FacturaUpdate = "TR_Facturas_N05_MetodoSnapshot_BU";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "MetodoPagoCodigoSnapshot", table: "FacturaPagos", type: "varchar(50)", maxLength: 50, nullable: true);
        migrationBuilder.AddColumn<string>(name: "MetodoPagoNombreSnapshot", table: "FacturaPagos", type: "varchar(120)", maxLength: 120, nullable: true);
        migrationBuilder.AddColumn<string>(name: "MetodoPagoCodigoSnapshot", table: "Facturas", type: "varchar(50)", maxLength: 50, nullable: true);
        migrationBuilder.AddColumn<string>(name: "MetodoPagoNombreSnapshot", table: "Facturas", type: "varchar(120)", maxLength: 120, nullable: true);

        migrationBuilder.Sql("""
            UPDATE FacturaPagos fp
            LEFT JOIN MetodosPago mp ON mp.Id = fp.MetodoPagoId
            SET fp.MetodoPagoCodigoSnapshot = COALESCE(fp.MetodoPagoCodigoSnapshot, mp.Codigo, CAST(fp.MetodoPago AS CHAR)),
                fp.MetodoPagoNombreSnapshot = COALESCE(fp.MetodoPagoNombreSnapshot, mp.Nombre, CAST(fp.MetodoPago AS CHAR))
            WHERE fp.MetodoPagoCodigoSnapshot IS NULL OR fp.MetodoPagoNombreSnapshot IS NULL;
            """);

        migrationBuilder.Sql("""
            UPDATE Facturas f
            INNER JOIN Ventas v ON v.Id = f.VentaId
            LEFT JOIN MetodosPago mp ON mp.Id = v.MetodoPagoId
            SET f.MetodoPagoCodigoSnapshot = COALESCE(f.MetodoPagoCodigoSnapshot, mp.Codigo, CAST(v.MetodoPago AS CHAR)),
                f.MetodoPagoNombreSnapshot = COALESCE(f.MetodoPagoNombreSnapshot, mp.Nombre, CAST(v.MetodoPago AS CHAR))
            WHERE f.MetodoPagoCodigoSnapshot IS NULL OR f.MetodoPagoNombreSnapshot IS NULL;
            """);

        migrationBuilder.Sql($"""
            CREATE TRIGGER {FacturaPagoInsert}
            BEFORE INSERT ON FacturaPagos
            FOR EACH ROW
            SET NEW.MetodoPagoCodigoSnapshot = COALESCE(
                    NEW.MetodoPagoCodigoSnapshot,
                    (SELECT mp.Codigo FROM MetodosPago mp WHERE mp.Id = NEW.MetodoPagoId LIMIT 1),
                    CAST(NEW.MetodoPago AS CHAR)),
                NEW.MetodoPagoNombreSnapshot = COALESCE(
                    NEW.MetodoPagoNombreSnapshot,
                    (SELECT mp.Nombre FROM MetodosPago mp WHERE mp.Id = NEW.MetodoPagoId LIMIT 1),
                    CAST(NEW.MetodoPago AS CHAR));
            """);

        migrationBuilder.Sql($"""
            CREATE TRIGGER {FacturaPagoUpdate}
            BEFORE UPDATE ON FacturaPagos
            FOR EACH ROW
            SET NEW.MetodoPagoCodigoSnapshot = OLD.MetodoPagoCodigoSnapshot,
                NEW.MetodoPagoNombreSnapshot = OLD.MetodoPagoNombreSnapshot;
            """);

        migrationBuilder.Sql($"""
            CREATE TRIGGER {FacturaInsert}
            BEFORE INSERT ON Facturas
            FOR EACH ROW
            SET NEW.MetodoPagoCodigoSnapshot = COALESCE(
                    NEW.MetodoPagoCodigoSnapshot,
                    (SELECT mp.Codigo FROM Ventas v LEFT JOIN MetodosPago mp ON mp.Id = v.MetodoPagoId WHERE v.Id = NEW.VentaId LIMIT 1),
                    (SELECT CAST(v.MetodoPago AS CHAR) FROM Ventas v WHERE v.Id = NEW.VentaId LIMIT 1)),
                NEW.MetodoPagoNombreSnapshot = COALESCE(
                    NEW.MetodoPagoNombreSnapshot,
                    (SELECT mp.Nombre FROM Ventas v LEFT JOIN MetodosPago mp ON mp.Id = v.MetodoPagoId WHERE v.Id = NEW.VentaId LIMIT 1),
                    (SELECT CAST(v.MetodoPago AS CHAR) FROM Ventas v WHERE v.Id = NEW.VentaId LIMIT 1));
            """);

        migrationBuilder.Sql($"""
            CREATE TRIGGER {FacturaUpdate}
            BEFORE UPDATE ON Facturas
            FOR EACH ROW
            SET NEW.MetodoPagoCodigoSnapshot = OLD.MetodoPagoCodigoSnapshot,
                NEW.MetodoPagoNombreSnapshot = OLD.MetodoPagoNombreSnapshot;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {FacturaUpdate};");
        migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {FacturaInsert};");
        migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {FacturaPagoUpdate};");
        migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {FacturaPagoInsert};");
        migrationBuilder.DropColumn(name: "MetodoPagoCodigoSnapshot", table: "FacturaPagos");
        migrationBuilder.DropColumn(name: "MetodoPagoNombreSnapshot", table: "FacturaPagos");
        migrationBuilder.DropColumn(name: "MetodoPagoCodigoSnapshot", table: "Facturas");
        migrationBuilder.DropColumn(name: "MetodoPagoNombreSnapshot", table: "Facturas");
    }
}
