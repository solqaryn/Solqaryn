using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Solqaryn.Infrastructure.Persistence;

#nullable disable
namespace Solqaryn.Infrastructure.Migrations.Oracle;

[DbContext(typeof(AppDbContext))]
[Migration("20261016200000_PublicStoreProofReplayGuard")]
public sealed class PublicStoreProofReplayGuard : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Short-lived security ledger, deliberately outside commercial EF entities.
        migrationBuilder.Sql("""
            CREATE TABLE PublicStoreProofReplays (
                ProofDigest BINARY(32) NOT NULL,
                ExpiresUtc DATETIME(6) NOT NULL,
                CONSTRAINT PK_PublicStoreProofReplays PRIMARY KEY (ProofDigest),
                KEY IX_PublicStoreProofReplays_ExpiresUtc (ExpiresUtc)
            ) ENGINE=InnoDB;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "PublicStoreProofReplays");
}
