using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Solqaryn.Infrastructure.Persistence;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations.Oracle;

/// <summary>
/// P4: el correo público identifica la cuenta dentro de UNA tienda/Empresa.
/// Preserva las filas y los hashes de contraseñas; sin backfill de datos legacy.
/// No crea dominios públicos ni habilita la cuarentena HTTP multiempresa.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20261014112000_PublicStoreAccountEmailPerEmpresa")]
public partial class PublicStoreAccountEmailPerEmpresa : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // La misma identidad comercial externa puede comprar en A y B,
        // sin vincular sesiones ni historiales de ambas tiendas.
        migrationBuilder.DropIndex(
            name: "UX_TiendaCuentasCliente_Correo",
            table: "TiendaCuentasCliente");

        migrationBuilder.CreateIndex(
            name: "UX_TiendaCuentasCliente_Correo",
            table: "TiendaCuentasCliente",
            columns: new[] { "EmpresaId", "CorreoNormalizado" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // MySQL falla seguro al recrear la unicidad global si ya existen
        // correos repetidos entre empresas. Nunca borrar cuentas automáticamente.
        migrationBuilder.DropIndex(
            name: "UX_TiendaCuentasCliente_Correo",
            table: "TiendaCuentasCliente");

        migrationBuilder.CreateIndex(
            name: "UX_TiendaCuentasCliente_Correo",
            table: "TiendaCuentasCliente",
            column: "CorreoNormalizado",
            unique: true);
    }
}
