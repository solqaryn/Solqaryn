#!/usr/bin/env bash
set -euo pipefail

repo_root="$GITHUB_WORKSPACE"
work_root="$RUNNER_TEMP/solqaryn-oracle10-lane"
candidate="$work_root/repo"
probe="$work_root/probe"
rm -rf "$work_root"
mkdir -p "$work_root"
cp -a "$repo_root/." "$candidate/"

python3 - "$candidate/backend" <<'PY'
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

root = Path(sys.argv[1])

def save_xml(path, tree):
    ET.indent(tree, space="  ")
    tree.write(path, encoding="unicode")

for path in root.rglob("*.csproj"):
    tree = ET.parse(path)
    proj = tree.getroot()
    for tfm in proj.findall(".//TargetFramework"):
        if tfm.text == "net8.0":
            tfm.text = "net10.0"
    for ref in proj.findall(".//PackageReference"):
        name = ref.attrib.get("Include", "")
        if name in {
            "Microsoft.EntityFrameworkCore",
            "Microsoft.EntityFrameworkCore.Design",
            "Microsoft.EntityFrameworkCore.InMemory",
            "Microsoft.EntityFrameworkCore.Sqlite",
        }:
            ref.set("Version", "10.0.12")
    save_xml(path, tree)

infra = root / "src/Infrastructure/Solqaryn.Infrastructure.csproj"
tree = ET.parse(infra)
proj = tree.getroot()
for group in proj.findall("ItemGroup"):
    for ref in list(group.findall("PackageReference")):
        if ref.attrib.get("Include") in {"Pomelo.EntityFrameworkCore.MySql", "MySqlConnector"}:
            group.remove(ref)

package_group = next((g for g in proj.findall("ItemGroup") if g.findall("PackageReference")), None)
if package_group is None:
    package_group = ET.SubElement(proj, "ItemGroup")
ET.SubElement(package_group, "PackageReference", Include="MySql.EntityFrameworkCore", Version="10.0.9")

compile_group = ET.SubElement(proj, "ItemGroup")
ET.SubElement(compile_group, "Compile", Remove="**/Migrations/**/*.cs")
save_xml(infra, tree)

for relative in [
    "src/API/Program.cs",
    "src/Infrastructure/Persistence/AppDbContextFactory.cs",
]:
    path = root / relative
    text = path.read_text(encoding="utf-8-sig")
    text = text.replace(
        ".UseMySql(connectionString, new MySqlServerVersion(mysqlServerVersion))",
        ".UseMySQL(connectionString)")
    text = text.replace(
        ".UseMySql(connectionString, new MySqlServerVersion(serverVersion))",
        ".UseMySQL(connectionString)")
    if "using MySql.EntityFrameworkCore.Extensions;" not in text:
        text = "using MySql.EntityFrameworkCore.Extensions;\n" + text
    path.write_text(text, encoding="utf-8")

print("ORACLE10_EPHEMERAL_REWRITE=PASS")
PY

cd "$candidate/backend"
dotnet restore src/API/Solqaryn.API.csproj
dotnet build src/API/Solqaryn.API.csproj --configuration Release --no-restore

dotnet new console --framework net10.0 --name Oracle10Probe --output "$probe" --force >/dev/null
dotnet add "$probe/Oracle10Probe.csproj" reference "$candidate/backend/src/Infrastructure/Solqaryn.Infrastructure.csproj"
dotnet add "$probe/Oracle10Probe.csproj" package MySql.EntityFrameworkCore --version 10.0.9 >/dev/null

cat > "$probe/Program.cs" <<'CS'
using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Extensions;
using MySql.Data.MySqlClient;
using Solqaryn.Application.Exceptions;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;

var connectionString = Environment.GetEnvironmentVariable("ORACLE10_CONNECTION")
    ?? throw new InvalidOperationException("ORACLE10_CONNECTION missing");

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseMySQL(connectionString)
    .Options;

await using var db = new AppDbContext(options);
if (!await db.Database.CanConnectAsync())
    throw new InvalidOperationException("ORACLE10_CONNECT_FAIL");

var properties = db.Model.GetEntityTypes().SelectMany(x => x.GetProperties()).ToArray();
if (properties.Count(x => string.Equals(x.GetColumnType(), "json", StringComparison.OrdinalIgnoreCase)) < 3)
    throw new InvalidOperationException("ORACLE10_JSON_MODEL_FAIL");
if (!properties.Any(x => x.GetColumnType()?.Contains("decimal(18,2)", StringComparison.OrdinalIgnoreCase) == true))
    throw new InvalidOperationException("ORACLE10_DECIMAL182_FAIL");
if (!properties.Any(x => x.GetColumnType()?.Contains("decimal(18,4)", StringComparison.OrdinalIgnoreCase) == true))
    throw new InvalidOperationException("ORACLE10_DECIMAL184_FAIL");
if (!properties.Any(x => x.GetColumnType()?.Contains("datetime(6)", StringComparison.OrdinalIgnoreCase) == true))
    throw new InvalidOperationException("ORACLE10_DATETIME6_FAIL");

var sql = db.Productos.AsNoTracking().Where(x => x.Id > 0).OrderBy(x => x.Id).Take(2).ToQueryString();
if (!sql.Contains("SELECT", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("ORACLE10_LINQ_FAIL");
_ = await db.Productos.AsNoTracking().OrderBy(x => x.Id).Take(1).ToListAsync();

await db.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS Phase6Oracle10Contract;");
await db.Database.ExecuteSqlRawAsync("""
CREATE TABLE Phase6Oracle10Contract(
  Id INT NOT NULL AUTO_INCREMENT,
  Code VARCHAR(40) COLLATE utf8mb4_bin NOT NULL,
  Amount DECIMAL(18,4) NOT NULL,
  AtUtc DATETIME(6) NOT NULL,
  Payload JSON NOT NULL,
  PRIMARY KEY(Id),
  UNIQUE KEY IX_TipoClientes_EsPredeterminadoUnico(Code)
) ENGINE=InnoDB;
""");

await using (var tx = await db.Database.BeginTransactionAsync())
{
    await db.Database.ExecuteSqlRawAsync(
        "INSERT INTO Phase6Oracle10Contract(Code,Amount,AtUtc,Payload) VALUES('ROLLBACK',1.2345,'2026-10-04 12:34:56.123456',JSON_OBJECT('ok',true));");
    await tx.RollbackAsync();
}

var rollbackCount = await db.Database.SqlQueryRaw<long>(
    "SELECT COUNT(*) AS Value FROM Phase6Oracle10Contract WHERE Code='ROLLBACK'").SingleAsync();
if (rollbackCount != 0)
    throw new InvalidOperationException("ORACLE10_ROLLBACK_FAIL");

await db.Database.ExecuteSqlRawAsync(
    "INSERT INTO Phase6Oracle10Contract(Code,Amount,AtUtc,Payload) VALUES('DUP',1,UTC_TIMESTAMP(6),JSON_OBJECT('x',1));");

var uow = new UnitOfWork(db);
try
{
    await uow.ExecuteInTransactionAsync(async () =>
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO Phase6Oracle10Contract(Code,Amount,AtUtc,Payload) VALUES('DUP',2,UTC_TIMESTAMP(6),JSON_OBJECT('x',2));"));
    throw new InvalidOperationException("ORACLE10_1062_NOT_THROWN");
}
catch (UniqueConstraintViolationException)
{
}

await db.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS Phase6Oracle10Retry;");
await db.Database.ExecuteSqlRawAsync("CREATE TABLE Phase6Oracle10Retry(Id INT PRIMARY KEY, Value INT NOT NULL);");
await db.Database.ExecuteSqlRawAsync("INSERT INTO Phase6Oracle10Retry VALUES(1,0);");

await using var blocker = new MySqlConnection(connectionString);
await blocker.OpenAsync();
await using var blockerTx = await blocker.BeginTransactionAsync();
await using (var cmd = blocker.CreateCommand())
{
    cmd.Transaction = blockerTx;
    cmd.CommandText = "UPDATE Phase6Oracle10Retry SET Value=Value+1 WHERE Id=1;";
    await cmd.ExecuteNonQueryAsync();
}

await db.Database.OpenConnectionAsync();
await db.Database.ExecuteSqlRawAsync("SET SESSION innodb_lock_wait_timeout=1;");
var attempts = 0;
var release = Task.Run(async () =>
{
    await Task.Delay(1050);
    await blockerTx.RollbackAsync();
});

await uow.ExecuteInTransactionAsync(async () =>
{
    attempts++;
    await db.Database.ExecuteSqlRawAsync(
        "UPDATE Phase6Oracle10Retry SET Value=Value+1 WHERE Id=1;");
});
await release;

if (attempts < 2)
    throw new InvalidOperationException("ORACLE10_RETRY_FAIL");

Console.WriteLine($"ORACLE10_PROVIDER_LANE_RUNTIME=PASS attempts={attempts}");
CS

dotnet restore "$probe/Oracle10Probe.csproj"
dotnet run --project "$probe/Oracle10Probe.csproj" --configuration Release

cd "$repo_root"
python3 scripts/security/priority3_tenant_isolation_audit.py
test -z "$(git status --porcelain)"
echo "ORACLE_EF10_NET10_PROVIDER_LANE=PASS"
