using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Solqaryn.Infrastructure.Persistence;

#nullable disable
namespace Solqaryn.Infrastructure.Migrations.Oracle;

[DbContext(typeof(AppDbContext))]
[Migration("20261016095000_PublicStoreBindingsPersistence")]
public sealed class PublicStoreBindingsPersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE PublicStoreBindings (
                Id BIGINT NOT NULL AUTO_INCREMENT,
                EmpresaId INT NOT NULL,
                Host VARCHAR(253) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
                StoreKey VARCHAR(48) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
                Activa TINYINT(1) NOT NULL DEFAULT 0,
                FechaCreacionUtc DATETIME(6) NOT NULL,
                FechaActualizacionUtc DATETIME(6) NOT NULL,
                CONSTRAINT PK_PublicStoreBindings PRIMARY KEY (Id),
                CONSTRAINT FK_PublicStoreBindings_Empresas_EmpresaId
                  FOREIGN KEY (EmpresaId) REFERENCES Empresas (Id) ON DELETE RESTRICT,
                UNIQUE KEY UX_PublicStoreBindings_Host_Key (Host, StoreKey),
                KEY IX_PublicStoreBindings_Empresa_Activa (EmpresaId, Activa)
            ) CHARACTER SET=utf8mb4;
            """);
    }
    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "PublicStoreBindings");
}
