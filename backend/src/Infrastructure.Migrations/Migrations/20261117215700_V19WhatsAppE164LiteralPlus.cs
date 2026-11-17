using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Solqaryn.Infrastructure.Persistence;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations.Oracle;

/// <summary>Correct MySQL 8.4 E.164 CHECK without rewriting historical migration N76C.</summary>
[DbContext(typeof(AppDbContext))]
[Migration("20261117215700_V19WhatsAppE164LiteralPlus")]
public sealed class V19WhatsAppE164LiteralPlus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE ConfiguracionesWhatsAppEmpresa DROP CHECK CK_ConfiguracionesWhatsAppEmpresa_Numero_E164;");
        migrationBuilder.Sql("ALTER TABLE ConfiguracionesWhatsAppEmpresa ADD CONSTRAINT CK_ConfiguracionesWhatsAppEmpresa_Numero_E164 CHECK (NumeroTelefonoE164 REGEXP '^[+][0-9]{8,15}$');");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Do not restore a defective constraint rejecting valid E164 values.
        throw new InvalidOperationException(
            "NO_GO: refuse unsafe E164 CHECK downgrade; use a reviewed forward migration and verified backup.");
    }
}
