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
        // Expand-first: nunca dejar la tabla sin una unicidad válida si
        // MySQL interrumpe/auto-confirma alguna instrucción de DDL.
        migrationBuilder.CreateIndex(
            name: "UX_TiendaCuentasCliente_Empresa_Correo_Prepare",
            table: "TiendaCuentasCliente",
            columns: new[] { "EmpresaId", "CorreoNormalizado" },
            unique: true);

        migrationBuilder.DropIndex(
            name: "UX_TiendaCuentasCliente_Correo",
            table: "TiendaCuentasCliente");

        migrationBuilder.RenameIndex(
            name: "UX_TiendaCuentasCliente_Empresa_Correo_Prepare",
            table: "TiendaCuentasCliente",
            newName: "UX_TiendaCuentasCliente_Correo");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Contract-first: si hay correos iguales de A/B, falla el primer
        // CREATE UNIQUE y conserva INTACTO el índice empresarial actual.
        // MySQL DDL autocommit; nunca DROP antes del preflight físico.
        migrationBuilder.CreateIndex(
            name: "UX_TiendaCuentasCliente_Correo_Global_Prepare",
            table: "TiendaCuentasCliente",
            column: "CorreoNormalizado",
            unique: true);

        migrationBuilder.DropIndex(
            name: "UX_TiendaCuentasCliente_Correo",
            table: "TiendaCuentasCliente");

        migrationBuilder.RenameIndex(
            name: "UX_TiendaCuentasCliente_Correo_Global_Prepare",
            table: "TiendaCuentasCliente",
            newName: "UX_TiendaCuentasCliente_Correo");
    }
}
