#!/usr/bin/env bash
# R11 ownership migration lab. This file never changes the shared Git ref.
set -euo pipefail
[[ "${GITHUB_ACTIONS:-}" == true && "${LAB_RUNNER_ENVIRONMENT:-}" == github-hosted ]]
[[ "${GITHUB_REPOSITORY,,}" == solqaryn/solqaryn && "$GITHUB_REF" == refs/heads/dev ]]
[[ "$(git rev-parse HEAD)" == "$GITHUB_SHA" ]]
[[ "$(git rev-parse HEAD^)" == b8d21c119a9de7f904ca49e15a828a6a63ad2e4a ]]
[[ -z "$(git status --porcelain)" ]]
[[ "${MYSQL_SERVICE_ID:-}" =~ ^[0-9a-f]{64}$ ]]
[[ "$(docker inspect --format '{{.Config.Image}}' "$MYSQL_SERVICE_ID")" == mysql:8.4 ]]
[[ "$(docker port "$MYSQL_SERVICE_ID" 3306/tcp)" == *:3306* ]]
root="$(git rev-parse --show-toplevel)"
cd "$root"
patch="$root/.github/ci/c0104-r11-ownership.patch"
artifacts="$RUNNER_TEMP/c0104-r11-artifacts"
mkdir -p "$artifacts"
python3 - <<'PY'
import subprocess
expected = {'.github/ci/c0104-r11-ownership.patch', '.github/ci/c0104-r11-lab.sh', '.github/workflows/v23-c01-04-m03-06-durable-qa-mysql84.yml', 'CHANGELOG_AI.md'}
actual = set(subprocess.check_output(['git','diff','--name-only','HEAD^','HEAD'],text=True).splitlines())
if actual != expected:
    raise SystemExit(f'CI_ONLY_SCOPE_MISMATCH: {sorted(actual)}')
PY
git apply --check "$patch"
cp "$patch" "$artifacts/ownership.patch"
sha256sum "$patch" | cut -d' ' -f1 > "$artifacts/patch.sha256"
local_password=ephemeral-v44-only
mysql_local() { docker exec -e MYSQL_PWD="$local_password" "$MYSQL_SERVICE_ID" mysql -h 127.0.0.1 -uroot --batch --skip-column-names "$@"; }
db_name="$(python3 -c 'import uuid; print("c0104_casefile_"+uuid.uuid4().hex)')"
[[ "$db_name" =~ ^c0104_casefile_[0-9a-f]{32}$ ]]
mysql_local -e "CREATE DATABASE \`$db_name\` CHARACTER SET utf8mb4;"
identity="$(mysql_local "$db_name" -e 'SELECT VERSION(), DATABASE();')"
[[ "$identity" == 8.4.*$'\t'"$db_name" ]]
[[ "$(mysql_local "$db_name" -e 'SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA=DATABASE();')" == 0 ]]
export ConnectionStrings__DefaultConnection="Server=127.0.0.1;Port=3306;Database=$db_name;User=root;Password=$local_password;SslMode=Disabled;AllowPublicKeyRetrieval=True;"
export Database__ApplyMigrationsOnStartup=false
export SOLQARYN_DATABASE_BOOTSTRAP_MODE=fresh
cd "$root/backend"
dotnet restore Solqaryn.sln
dotnet build Solqaryn.sln -c Release --no-restore
dotnet tool restore
dotnet tool run dotnet-ef --version | tee "$artifacts/ef-version.txt"
grep -q '10\.0\.12' "$artifacts/ef-version.txt"
dotnet run --project tools/Solqaryn.DatabaseBootstrap/Solqaryn.DatabaseBootstrap.csproj -c Release --no-build
previous=20261117215700_V19WhatsAppE164LiteralPlus
[[ "$(mysql_local "$db_name" -e 'SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC LIMIT 1;')" == "$previous" ]]
[[ "$(mysql_local "$db_name" -e "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME IN ('V5CaseFileOwnerships','V5CaseFileVersions');")" == 0 ]]
mysql_local "$db_name" -e 'SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA=DATABASE() ORDER BY TABLE_NAME;' > "$artifacts/schema-before.txt"
cd "$root"
git apply "$patch"
cd "$root/backend"
ef=(dotnet tool run dotnet-ef)
args=(--project src/Infrastructure.Migrations/Solqaryn.Infrastructure.Migrations.csproj --startup-project src/API/Solqaryn.API.csproj --context AppDbContext --configuration Release)
"${ef[@]}" migrations add V44C0104CaseFileOwnership "${args[@]}" --output-dir Migrations
mapfile -t candidates < <(find src/Infrastructure.Migrations/Migrations -maxdepth 1 -type f -name '*_V44C0104CaseFileOwnership.cs')
[[ "${#candidates[@]}" == 1 ]]
migration="${candidates[0]}"
target="$(basename "$migration" .cs)"
[[ "$target" =~ ^[0-9]{14}_V44C0104CaseFileOwnership$ && "$target" > "$previous" ]]
python3 - "$migration" <<'PY'
import pathlib,re,sys
path=pathlib.Path(sys.argv[1]); s=path.read_text(encoding='utf-8-sig')
up=re.search(r'protected override void Up\(MigrationBuilder (\w+)\)\s*\{(.*?)\n(?P<indent>[ \t]*)\}\s*\n\s*(?:///.*\n\s*)*protected override void Down\(MigrationBuilder \1\)',s,re.S)
if not up: raise SystemExit('UNRECOGNIZED_MIGRATION_UP')
v=up.group(1); body=up.group(2); expected={'V5CaseFileOwnerships','V5CaseFileVersions'}
created=re.findall(r'\b'+re.escape(v)+r'\.CreateTable\(\s*name:\s*"([^"]+)"',body)
methods=re.findall(r'\b'+re.escape(v)+r'\.(\w+)\(',body)
if set(created)!=expected or len(created)!=2 or set(methods)-{'CreateTable','CreateIndex'}: raise SystemExit('NON_ADDITIVE_UP')
down=s[up.end():]
dropped=re.findall(r'\b'+re.escape(v)+r'\.DropTable\(\s*name:\s*"([^"]+)"',down)
if dropped!=['V5CaseFileVersions','V5CaseFileOwnerships'] or set(re.findall(r'\b'+re.escape(v)+r'\.(\w+)\(',down))!={'DropTable'}: raise SystemExit('UNEXPECTED_DOWN')
# Existing legacy nullable Sucursal.EmpresaId is deliberately not an EF key.
sql='ALTER TABLE `V5CaseFileOwnerships` ADD CONSTRAINT `FK_V5CaseFileOwnerships_SucursalTenant` FOREIGN KEY (`EmpresaId`, `SucursalId`) REFERENCES `Sucursales` (`EmpresaId`, `Id`) ON DELETE RESTRICT;'
line='\n'+up.group('indent')+'    '+v+'.Sql("'+sql+'");'
s=s[:up.end(2)]+line+s[up.end(2):]
path.write_text(s,encoding='utf-8')
PY
cp "$migration" "$artifacts/"
[[ -f "${migration%.cs}.Designer.cs" ]]
cp "${migration%.cs}.Designer.cs" "$artifacts/"
mapfile -t snapshots < <(find src/Infrastructure.Migrations/Migrations -maxdepth 1 -type f -name '*ModelSnapshot.cs')
[[ "${#snapshots[@]}" == 1 ]]
cp "${snapshots[0]}" "$artifacts/"
dotnet build Solqaryn.sln -c Release --no-restore
"${ef[@]}" migrations has-pending-model-changes "${args[@]}" --no-build
"${ef[@]}" database update "$target" "${args[@]}" --no-build --connection "$ConnectionStrings__DefaultConnection"
[[ "$(mysql_local "$db_name" -e 'SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC LIMIT 1;')" == "$target" ]]
mysql_local "$db_name" -e "SELECT TABLE_NAME, CONSTRAINT_NAME, COLUMN_NAME, REFERENCED_TABLE_NAME, REFERENCED_COLUMN_NAME, ORDINAL_POSITION FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME IN ('V5CaseFileOwnerships','V5CaseFileVersions') AND REFERENCED_TABLE_NAME IS NOT NULL ORDER BY TABLE_NAME, CONSTRAINT_NAME, ORDINAL_POSITION;" > "$artifacts/foreign-keys.txt"
export SOLQARYN_CASEFILE_MIGRATION_LAB_CONNECTION="$ConnectionStrings__DefaultConnection"
export SOLQARYN_CASEFILE_MIGRATION_LAB_TARGET="$target"
dotnet test tests/Solqaryn.Tests/Solqaryn.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName=Solqaryn.Tests.C0104M0304OwnershipMigrationFixtureTests.MigratedOracleSchemaEnforcesOwnershipAndTenantFilters' --logger 'trx;LogFileName=c0104-ownership-migration.trx' --results-directory "$artifacts"
python3 - "$artifacts/c0104-ownership-migration.trx" <<'PY'
import sys,xml.etree.ElementTree as ET
root=ET.parse(sys.argv[1]).getroot(); c=root.find('.//{*}Counters'); results=root.findall('.//{*}UnitTestResult')
if c is None or any(int(c.get(k,-1))!=v for k,v in {'total':1,'passed':1,'failed':0,'notExecuted':0}.items()) or len(results)!=1 or 'MigratedOracleSchemaEnforcesOwnershipAndTenantFilters' not in results[0].get('testName','') or results[0].get('outcome')!='Passed': raise SystemExit('EXACT_FACT_NOT_PASSED')
PY
counts_sql='SELECT (SELECT COUNT(*) FROM Empresas),(SELECT COUNT(*) FROM Sucursales),(SELECT COUNT(*) FROM Usuarios),(SELECT COUNT(*) FROM Roles);'
mysql_local "$db_name" -e "$counts_sql" > "$artifacts/counts-before-rollback.txt"
"${ef[@]}" database update "$previous" "${args[@]}" --no-build --connection "$ConnectionStrings__DefaultConnection"
[[ "$(mysql_local "$db_name" -e 'SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC LIMIT 1;')" == "$previous" ]]
mysql_local "$db_name" -e 'SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA=DATABASE() ORDER BY TABLE_NAME;' > "$artifacts/schema-after-rollback.txt"
cmp "$artifacts/schema-before.txt" "$artifacts/schema-after-rollback.txt"
mysql_local "$db_name" -e "$counts_sql" > "$artifacts/counts-after-rollback.txt"
cmp "$artifacts/counts-before-rollback.txt" "$artifacts/counts-after-rollback.txt"
"${ef[@]}" database update "$target" "${args[@]}" --no-build --connection "$ConnectionStrings__DefaultConnection"
[[ "$(mysql_local "$db_name" -e 'SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC LIMIT 1;')" == "$target" ]]
[[ "$(mysql_local "$db_name" -e 'SELECT (SELECT COUNT(*) FROM V5CaseFileOwnerships)+(SELECT COUNT(*) FROM V5CaseFileVersions);')" == 0 ]]
mysql_local "$db_name" -e "$counts_sql" > "$artifacts/counts-after-reapply.txt"
cmp "$artifacts/counts-before-rollback.txt" "$artifacts/counts-after-reapply.txt"
printf '%s\n' "OWNERSHIP_MIGRATION_LAB_PASS sha=$GITHUB_SHA migration=$target" | tee "$artifacts/result.txt"
# GitHub-hosted service teardown discards the dedicated database. No remote DB cleanup.
