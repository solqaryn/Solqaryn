using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations.Oracle
{
    [Migration("20261006111818_OracleBaseline")]
    public partial class OracleBaseline : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("SELECT 1 FROM `Empresas` LIMIT 0;");
            migrationBuilder.Sql("SELECT 1 FROM `Productos` LIMIT 0;");
            migrationBuilder.Sql("SELECT 1 FROM `ConfigEmpresas` LIMIT 0;");
            migrationBuilder.Sql("SELECT 1 FROM `__EFMigrationsHistory` LIMIT 0;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Oracle baseline is irreversible; restore the certified SOLQARYN baseline instead';");
        }
    }
}
