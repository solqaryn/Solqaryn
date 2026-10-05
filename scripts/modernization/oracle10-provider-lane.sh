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
ET.SubElement(package_group, "PackageReference", Include="Microsoft.EntityFrameworkCore.Relational", Version="10.0.12")

compile_group = ET.SubElement(proj, "ItemGroup")
ET.SubElement(compile_group, "Compile", Remove="**/Migrations/**/*.cs")
snapshot = root / "src/Infrastructure/Migrations/AppDbContextModelSnapshot.cs"
snapshot_source = snapshot.read_text(encoding="utf-8-sig")
snapshot_source = re.sub(
    r"^\s*MySqlModelBuilderExtensions\.AutoIncrementColumns\(modelBuilder\);\s*$",
    "",
    snapshot_source,
    flags=re.MULTILINE,
)
snapshot_source = re.sub(
    r"^\s*MySqlPropertyBuilderExtensions\.UseMySqlIdentityColumn\(b\.Property<int>\(\"[^\"]+\"\)\);\s*$",
    "",
    snapshot_source,
    flags=re.MULTILINE,
)
if "MySqlModelBuilderExtensions." in snapshot_source or "MySqlPropertyBuilderExtensions." in snapshot_source:
    raise SystemExit("ORACLE10_SNAPSHOT_PROVIDER_ANNOTATIONS_UNCLASSIFIED")
snapshot.write_text(snapshot_source, encoding="utf-8")
ET.SubElement(compile_group, "Compile", Include="Migrations/AppDbContextModelSnapshot.cs")
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
dotnet add "$probe/Oracle10Probe.csproj" package Microsoft.EntityFrameworkCore.Relational --version 10.0.12 >/dev/null

cat > "$probe/Program.cs" <<'CS'
using System.Diagnostics;
using System.Data;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Solqaryn.Domain.Entities;
using MySql.EntityFrameworkCore.Extensions;
using MySql.Data.MySqlClient;
using Solqaryn.Application.Exceptions;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Solqaryn.Infrastructure.Services;

static IEnumerable<Exception> ExceptionChain(Exception exception)
{
    for (Exception? current = exception; current is not null; current = current.InnerException)
        yield return current;
}

static int? ErrorNumber(Exception exception)
{
    foreach (var item in ExceptionChain(exception))
    {
        var property = item.GetType().GetProperty("Number");
        if (property?.GetValue(item) is int number)
            return number;
    }
    return null;
}

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

var repositoryLinqProbes = 0;
var productoRepository = new ProductoRepository(db);
await productoRepository.GetByIdAsync(1);
repositoryLinqProbes++;
foreach (var sortBy in new[] { "marca", "modelo", "color", "talla", "cantidad", "costo", "precio", "fechacreacion", "nombre" })
{
    await productoRepository.GetPagedAsync(new ProductoPagedRequest
    {
        Page = 1,
        PageSize = 3,
        Search = "oracle-linq-probe",
        SortBy = sortBy,
        SortDirection = "desc",
        CategoriaId = 1,
        ColorId = 1,
        TallaId = 1,
        MarcaId = 1,
        ModeloId = 1,
        Activo = true,
        EsDestacado = false,
        Agotado = true
    });
    repositoryLinqProbes++;
}
await productoRepository.GetStockBajoAsync();
await productoRepository.GetUltimosAgregadosAsync(3);
await productoRepository.GetTotalUnidadesPorTipoAsync(TipoInventario.MercaderiaVenta);
await productoRepository.GetValorTotalCostoPorTipoAsync(TipoInventario.MercaderiaVenta);
await productoRepository.GetValorTotalPrecioPorTipoAsync(TipoInventario.MercaderiaVenta);
repositoryLinqProbes += 6;

var clienteRepository = new ClienteRepository(db);
await clienteRepository.GetByIdConVentasAsync(1);
await clienteRepository.BuscarActivosAsync("oracle-linq-probe", 3);
await clienteRepository.BuscarCoincidenciaActivaAsync("12-345", "oracle@example.invalid", "555-0100", "Oracle Probe");
await clienteRepository.ExisteNombreAsync("Oracle Probe", 1);
await clienteRepository.ExisteIdentidadAsync("12-345", 1);
repositoryLinqProbes += 5;

var existenciaRepository = new ExistenciaVarianteRepository(db);
await existenciaRepository.BuscarAsync(1, 1, 1, 1, true, true, false, 1, 3);
await existenciaRepository.GetOperativasPublicasPorVariantesAsync(new[] { 1, 2, 3 });
await existenciaRepository.ExisteClaveAsync(1, 1, 1, 2);
repositoryLinqProbes += 3;

var currentUser = new OracleProbeCurrentUser();
var usuarioScope = new OracleProbeUserScope();
var compraRepository = new CompraRepository(db, currentUser, usuarioScope);
await compraRepository.GetPagedAsync(new PagedRequest { Page = 1, PageSize = 3, Search = "oracle-linq-probe", SortBy = "total", SortDirection = "desc" });
await compraRepository.GetTotalDelMesAsync();
await compraRepository.GetCuentasPorPagarAsync();
await compraRepository.GetUltimasAsync(3);
repositoryLinqProbes += 4;

var ventaRepository = new VentaRepository(db, currentUser, usuarioScope);
await ventaRepository.GetPagedAsync(new PagedRequest { Page = 1, PageSize = 3, Search = "oracle-linq-probe", SortBy = "total", SortDirection = "desc" });
await ventaRepository.GetTotalDelMesAsync();
await ventaRepository.GetIngresosDelMesAsync();
await ventaRepository.GetCuentasPorCobrarAsync();
await ventaRepository.GetUtilidadBrutaTotalAsync();
await ventaRepository.GetUltimasAsync(3);
repositoryLinqProbes += 6;
if (repositoryLinqProbes < 34)
    throw new InvalidOperationException($"ORACLE10_REPOSITORY_LINQ_COVERAGE_INCOMPLETE probes={repositoryLinqProbes}");

await db.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS Phase6Oracle10Contract;");
await db.Database.ExecuteSqlRawAsync("""
CREATE TABLE Phase6Oracle10Contract(
  Id INT NOT NULL AUTO_INCREMENT,
  Code VARCHAR(40) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
  Amount DECIMAL(18,4) NOT NULL,
  AtUtc DATETIME(6) NOT NULL,
  Payload JSON NOT NULL,
  PRIMARY KEY(Id),
  UNIQUE KEY IX_TipoClientes_EsPredeterminadoUnico(Code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
""");

var columnCollation = await db.Database.SqlQueryRaw<string>(
    "SELECT COLLATION_NAME AS Value FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Phase6Oracle10Contract' AND COLUMN_NAME='Code'").SingleAsync();
var tableCollation = await db.Database.SqlQueryRaw<string>(
    "SELECT TABLE_COLLATION AS Value FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Phase6Oracle10Contract'").SingleAsync();
if (columnCollation != "utf8mb4_bin" || tableCollation != "utf8mb4_0900_ai_ci")
    throw new InvalidOperationException($"ORACLE10_COLLATION_METADATA_MISMATCH column={columnCollation} table={tableCollation}");

await db.Database.OpenConnectionAsync();
await db.Database.ExecuteSqlRawAsync("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED;");
await using (var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted))
{
    var isolation = await db.Database.SqlQueryRaw<string>("SELECT @@transaction_isolation AS Value").SingleAsync();
    if (!string.Equals(isolation, "READ-COMMITTED", StringComparison.Ordinal))
        throw new InvalidOperationException($"ORACLE10_TRANSACTION_ISOLATION_MISMATCH:{isolation}");
    await db.Database.ExecuteSqlRawAsync(
        "INSERT INTO Phase6Oracle10Contract(Code,Amount,AtUtc,Payload) VALUES('ROLLBACK',1.2345,'2026-10-04 12:34:56.123456',JSON_OBJECT('ok',true));");
    await tx.RollbackAsync();
}

var rollbackCount = await db.Database.SqlQueryRaw<long>(
    "SELECT COUNT(*) AS Value FROM Phase6Oracle10Contract WHERE Code='ROLLBACK'").SingleAsync();
if (rollbackCount != 0)
    throw new InvalidOperationException("ORACLE10_ROLLBACK_FAIL");

await using (var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted))
{
    await db.Database.ExecuteSqlRawAsync(
        "INSERT INTO Phase6Oracle10Contract(Code,Amount,AtUtc,Payload) VALUES('CommitProbe',1,UTC_TIMESTAMP(6),JSON_OBJECT('x',1));");
    await tx.CommitAsync();
}
var commitCount = await db.Database.SqlQueryRaw<long>(
    "SELECT COUNT(*) AS Value FROM Phase6Oracle10Contract WHERE Code='CommitProbe'").SingleAsync();
if (commitCount != 1)
    throw new InvalidOperationException("ORACLE10_COMMIT_FAIL");

await using (var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted))
{
    await db.Database.ExecuteSqlRawAsync(
        "INSERT INTO Phase6Oracle10Contract(Code,Amount,AtUtc,Payload) VALUES('RollbackProbe',1,UTC_TIMESTAMP(6),JSON_OBJECT('x',1));");
    await tx.RollbackAsync();
}
var explicitRollbackCount = await db.Database.SqlQueryRaw<long>(
    "SELECT COUNT(*) AS Value FROM Phase6Oracle10Contract WHERE Code='RollbackProbe'").SingleAsync();
if (explicitRollbackCount != 0)
    throw new InvalidOperationException("ORACLE10_EXPLICIT_ROLLBACK_FAIL");

await db.Database.ExecuteSqlRawAsync(
    "INSERT INTO Phase6Oracle10Contract(Code,Amount,AtUtc,Payload) VALUES('CaseProbe',1,UTC_TIMESTAMP(6),JSON_OBJECT('x',1)),('caseprobe',1,UTC_TIMESTAMP(6),JSON_OBJECT('x',1)),('DUP',1,UTC_TIMESTAMP(6),JSON_OBJECT('x',1));");
var upperCaseMatch = await db.Database.SqlQueryRaw<long>(
    "SELECT COUNT(*) AS Value FROM Phase6Oracle10Contract WHERE Code='CaseProbe'").SingleAsync();
var lowerCaseMatch = await db.Database.SqlQueryRaw<long>(
    "SELECT COUNT(*) AS Value FROM Phase6Oracle10Contract WHERE Code='caseprobe'").SingleAsync();
if (upperCaseMatch != 1 || lowerCaseMatch != 1)
    throw new InvalidOperationException($"ORACLE10_COLLATION_CASE_SEMANTICS_MISMATCH upper={upperCaseMatch} lower={lowerCaseMatch}");

var uow = new UnitOfWork(db);
var duplicateAttempts = 0;
UniqueConstraintViolationException? translatedDuplicate = null;
try
{
    await uow.ExecuteInTransactionAsync(async () =>
    {
        duplicateAttempts++;
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO Phase6Oracle10Contract(Code,Amount,AtUtc,Payload) VALUES('ErrorRollbackProbe',1,UTC_TIMESTAMP(6),JSON_OBJECT('x',1));");
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO Phase6Oracle10Contract(Code,Amount,AtUtc,Payload) VALUES('DUP',2,UTC_TIMESTAMP(6),JSON_OBJECT('x',2));");
    });
}
catch (UniqueConstraintViolationException exception)
{
    translatedDuplicate = exception;
}

if (translatedDuplicate is null)
    throw new InvalidOperationException("ORACLE10_1062_NOT_TRANSLATED");
if (duplicateAttempts != 1)
    throw new InvalidOperationException($"ORACLE10_1062_RETRIED attempts={duplicateAttempts}");
if (translatedDuplicate.ConstraintName != "TipoClientePredeterminadoUnico")
    throw new InvalidOperationException($"ORACLE10_1062_CONSTRAINT_CHANGED name={translatedDuplicate.ConstraintName}");
if (translatedDuplicate.Message != "Conflicto de concurrencia: Ya existe otro tipo de cliente marcado como predeterminado único. Inténtalo de nuevo.")
    throw new InvalidOperationException("ORACLE10_1062_MESSAGE_CHANGED");
if (ErrorNumber(translatedDuplicate) != 1062 ||
    !ExceptionChain(translatedDuplicate).Any(exception => exception.GetType().FullName == "MySql.Data.MySqlClient.MySqlException"))
    throw new InvalidOperationException("ORACLE10_1062_PROVIDER_ERROR_NOT_PRESERVED");
var errorRollbackCount = await db.Database.SqlQueryRaw<long>(
    "SELECT COUNT(*) AS Value FROM Phase6Oracle10Contract WHERE Code='ErrorRollbackProbe'").SingleAsync();
var duplicatePersistedCount = await db.Database.SqlQueryRaw<long>(
    "SELECT COUNT(*) AS Value FROM Phase6Oracle10Contract WHERE Code='DUP'").SingleAsync();
if (errorRollbackCount != 0 || duplicatePersistedCount != 1)
    throw new InvalidOperationException($"ORACLE10_ERROR_ROLLBACK_FAIL partial={errorRollbackCount} duplicate={duplicatePersistedCount}");

var stockProduct = new Producto
{
    Nombre = "Oracle EF10 concurrency probe",
    Marca = "CI",
    Modelo = "Concurrency",
    Cantidad = 5,
    Costo = 10m,
    Precio = 20m,
    UmbralStockBajo = 1,
    Activo = true,
    Eliminado = false
};
db.Productos.Add(stockProduct);
await db.SaveChangesAsync();
db.ChangeTracker.Clear();

var stockLockProductRepository = new ProductoRepository(db);
var stockLockVariantRepository = new ProductoVarianteRepository(db);
var stockConcurrencyService = new InventarioConcurrencyService(db, stockLockProductRepository, stockLockVariantRepository);
long stockLockWaitMs;
await using (var lockTx = await db.Database.BeginTransactionAsync())
{
    await stockConcurrencyService.BloquearYValidarInventarioAsync(
        [new InventarioDemanda(stockProduct.Id, null, 1)]);

    var lockAttemptStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    var competingStockRead = Task.Run(async () =>
    {
        await using var competingDb = new AppDbContext(options);
        await using var competingTx = await competingDb.Database.BeginTransactionAsync();
        var competingRepository = new ProductoRepository(competingDb);
        var competingVariantRepository = new ProductoVarianteRepository(competingDb);
        var competingStockLockService = new InventarioConcurrencyService(competingDb, competingRepository, competingVariantRepository);
        lockAttemptStarted.TrySetResult(true);
        var timer = Stopwatch.StartNew();
        await competingStockLockService.BloquearYValidarInventarioAsync(
            [new InventarioDemanda(stockProduct.Id, null, 1)]);
        timer.Stop();
        await competingTx.RollbackAsync();
        return timer.ElapsedMilliseconds;
    });

    await lockAttemptStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
    await Task.Delay(350);
    if (competingStockRead.IsCompleted)
        throw new InvalidOperationException("ORACLE10_STOCK_LOCK_DID_NOT_SERIALIZE");
    await lockTx.CommitAsync();
    stockLockWaitMs = await competingStockRead.WaitAsync(TimeSpan.FromSeconds(15));
    if (stockLockWaitMs < 250)
        throw new InvalidOperationException($"ORACLE10_STOCK_LOCK_WAIT_TOO_SHORT:{stockLockWaitMs}");
}

var empresaSequence = new Empresa($"Oracle EF10 sequence probe {Guid.NewGuid():N}");
db.Set<Empresa>().Add(empresaSequence);
await db.SaveChangesAsync();
var sequence = new SecuenciaDocumento(empresaSequence.Id, null, "FACTURA", "CI-", 4, 100);
db.SecuenciasDocumento.Add(sequence);
await db.SaveChangesAsync();
var sequenceId = sequence.Id;
var empresaId = empresaSequence.Id;
db.ChangeTracker.Clear();

var reservations = await Task.WhenAll(Enumerable.Range(0, 10).Select(async _ =>
{
    await using var reservationDb = new AppDbContext(options);
    var service = new SecuenciaDocumentoService(reservationDb, new OracleProbeUserScope());
    return await service.ReservarSiguienteAsync(
        new ReservarSecuenciaDocumentoRequest(empresaId, null, "FACTURA"));
}));
var reservedValues = reservations.Select(x => x.Valor).OrderBy(x => x).ToArray();
var expectedValues = Enumerable.Range(101, 10).Select(x => (long)x).ToArray();
if (!reservedValues.SequenceEqual(expectedValues) || reservations.Select(x => x.Numero).Distinct().Count() != 10)
    throw new InvalidOperationException($"ORACLE10_SEQUENCE_CONCURRENCY_MISMATCH:{string.Join(',', reservedValues)}");

var finalSequence = await db.SecuenciasDocumento.AsNoTracking().SingleAsync(x => x.Id == sequenceId);
var sequenceAuditCount = await db.RegistrosAuditoria.AsNoTracking()
    .CountAsync(x => x.Entidad == nameof(SecuenciaDocumento) && x.ReferenciaId == sequenceId);
if (finalSequence.UltimoValor != 110 || sequenceAuditCount != 10)
    throw new InvalidOperationException($"ORACLE10_SEQUENCE_FINAL_STATE_MISMATCH value={finalSequence.UltimoValor} audits={sequenceAuditCount}");
db.ChangeTracker.Clear();
Console.WriteLine($"ORACLE10_CONCURRENCY_CONTRACT=PASS stockForUpdateWaitMs={stockLockWaitMs} sequenceReservations=10 uniqueMonotonic=true audits=10");

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

Console.WriteLine($"ORACLE10_PROVIDER_LANE_RUNTIME=PASS attempts={attempts} repositoryLinqProbes={repositoryLinqProbes}");
Console.WriteLine("ORACLE10_COLLATION_CASE_CONTRACT=PASS column=utf8mb4_bin table=utf8mb4_0900_ai_ci distinctCaseVariants=2");
Console.WriteLine("ORACLE10_TRANSACTION_CONTRACT=PASS isolation=READ-COMMITTED commit=true rollback=true errorRollback=true");

sealed class OracleProbeCurrentUser : ICurrentUserService
{
    public int? UsuarioId => 1;
    public string? NombreUsuario => "oracle-linq-probe";
    public string? NombreCompleto => "Oracle LINQ Probe";
    public int? RolId => 1;
    public bool EsAdministrador => true;
    public bool EstaAutenticado => true;
}

sealed class OracleProbeUserScope : IUsuarioScopeService
{
    public Task<UsuarioScopeActual?> ObtenerActualAsync() =>
        Task.FromResult<UsuarioScopeActual?>(new UsuarioScopeActual(1, 1, "Administrador", true));

    public Task<UsuarioTenantScopeActual?> ObtenerActualAsync(int empresaId, CancellationToken cancellationToken = default) =>
        Task.FromResult<UsuarioTenantScopeActual?>(new UsuarioTenantScopeActual(1, empresaId, 1, "Administrador", true));
}
CS

python3 - "$candidate/backend/tests/Solqaryn.Tests" <<'PY'
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

test_root = Path(sys.argv[1])
project = test_root / "Solqaryn.Tests.csproj"
migration_only = {
    "N08MigracionesLimpiezaPreflightIntegrationTests.cs",
    "N08PersistenciaLimpiezaIntegrationTests.cs",
    "N110CosteoMigrationIntegrationTests.cs",
    # N06 verifies MySQL migration-installed CHECK/bridge behavior rather than
    # provider-agnostic integration behavior; Oracle lane does not replay history.
    "MovimientoInventarioOrigenTipadoIntegrationTests.cs",
}
included = []
excluded = []
unit_files = []
unit_history_excluded = []

for path in sorted(test_root.rglob("*.cs")):
    if any(part in {"bin", "obj"} for part in path.parts):
        continue
    source = path.read_text(encoding="utf-8-sig")
    is_integration = '[Trait("Category", "Integration")]' in source
    if is_integration and path.name in migration_only:
        excluded.append(path.name)
        continue
    if not is_integration and any(
        marker in source
        for marker in (
            "using Solqaryn.Infrastructure.Migrations;",
            "using Solqaryn.Infrastructure.Persistence.Migrations;",
        )
    ):
        unit_history_excluded.append(path.relative_to(test_root).as_posix())
        continue

    source = source.replace("using MySqlConnector;", "using MySql.Data.MySqlClient;")
    source = source.replace(
        "new MySqlConnectionStringBuilder(raw)",
        'new MySqlConnectionStringBuilder(raw.Replace("SslMode=None", "SslMode=Disabled", StringComparison.OrdinalIgnoreCase))',
    )
    source = source.replace(".UseMySql(", ".UseMySQL(")
    source, _ = re.subn(
        r",\s*(?:new MySqlServerVersion\s*\(\s*new (?:System\.)?Version\([^)]*\)\s*\)|ServerVersion\.Parse\([^)]*\))\s*\)",
        ")",
        source,
    )
    if ".UseMySQL(" in source and "using MySql.EntityFrameworkCore.Extensions;" not in source:
        source = "using MySql.EntityFrameworkCore.Extensions;\n" + source
    source = re.sub(
        r"(\b\w+)\.Database\.MigrateAsync\(\)",
        r"OracleCandidateDatabaseBootstrap.EnsureCreatedAsync(\1)",
        source,
    )
    if "UseMySql(" in source or "MySqlServerVersion" in source or "MySqlConnector" in source:
        raise SystemExit(f"ORACLE10_INTEGRATION_PROVIDER_REWRITE_INCOMPLETE={path.name}")
    if ".Database.MigrateAsync(" in source or "IMigrator" in source:
        if is_integration:
            raise SystemExit(f"ORACLE10_INTEGRATION_HISTORY_DEPENDENCY_UNCLASSIFIED={path.name}")
    path.write_text(source, encoding="utf-8")
    relative = path.relative_to(test_root).as_posix()
    if is_integration:
        included.append(relative)
    else:
        unit_files.append(relative)

if len(included) + len(excluded) != 17 or len(included) != 13 or len(unit_files) < 400:
    raise SystemExit(
        f"ORACLE10_INTEGRATION_FILESET_MISMATCH included={len(included)} excluded={len(excluded)}"
    )

bootstrap = test_root / "OracleCandidateDatabaseBootstrap.cs"
bootstrap.write_text(r'''using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Solqaryn.Infrastructure.Persistence;
using System.Collections.Concurrent;

internal static class OracleCandidateDatabaseBootstrap
{
    private static readonly ConcurrentDictionary<string, Lazy<Task>> Copies = new(StringComparer.OrdinalIgnoreCase);

    public static async Task EnsureCreatedAsync(AppDbContext context)
    {
        var targetConnectionString = context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Oracle candidate test connection string is missing.");
        var sourceConnectionString = Environment.GetEnvironmentVariable("ORACLE10_CONNECTION")
            ?? throw new InvalidOperationException("ORACLE10_CONNECTION is required as the certified Pomelo schema template.");
        var targetDatabase = new MySqlConnectionStringBuilder(targetConnectionString).Database;
        var sourceDatabase = new MySqlConnectionStringBuilder(sourceConnectionString).Database;
        if (string.IsNullOrWhiteSpace(targetDatabase) || string.IsNullOrWhiteSpace(sourceDatabase))
            throw new InvalidOperationException("Oracle candidate source/target database name is missing.");
        if (string.Equals(targetDatabase, sourceDatabase, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Oracle candidate integration tests must not mutate the schema template.");

        await Copies.GetOrAdd(targetDatabase, _ => new Lazy<Task>(
            () => CloneCertifiedSchemaAsync(sourceConnectionString, sourceDatabase, targetConnectionString, targetDatabase),
            LazyThreadSafetyMode.ExecutionAndPublication)).Value;
    }

    private static async Task CloneCertifiedSchemaAsync(
        string sourceConnectionString,
        string sourceDatabase,
        string targetConnectionString,
        string targetDatabase)
    {
        var targetBuilder = new MySqlConnectionStringBuilder(targetConnectionString);
        targetBuilder.Database = string.Empty;
        await using (var server = new MySqlConnection(targetBuilder.ConnectionString))
        {
            await server.OpenAsync();
            await using var create = server.CreateCommand();
            create.CommandText = $"CREATE DATABASE IF NOT EXISTS `{Quote(targetDatabase)}` CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;";
            await create.ExecuteNonQueryAsync();
        }

        await using var source = new MySqlConnection(sourceConnectionString);
        await using var target = new MySqlConnection(targetConnectionString);
        await source.OpenAsync();
        await target.OpenAsync();

        var tables = new List<string>();
        await using (var list = source.CreateCommand())
        {
            list.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA=@schema AND TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME;";
            list.Parameters.AddWithValue("@schema", sourceDatabase);
            await using var reader = await list.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                tables.Add(reader.GetString(0));
        }
        if (tables.Count == 0)
            throw new InvalidOperationException("Certified Pomelo integration schema template has no tables.");

        foreach (var table in tables)
        {
            await using var create = target.CreateCommand();
            create.CommandText = $"CREATE TABLE `{Quote(targetDatabase)}`.`{Quote(table)}` LIKE `{Quote(sourceDatabase)}`.`{Quote(table)}`;";
            await create.ExecuteNonQueryAsync();
        }

        foreach (var table in tables)
        {
            string createTableSql;
            await using (var showCreate = source.CreateCommand())
            {
                showCreate.CommandText = $"SHOW CREATE TABLE `{Quote(sourceDatabase)}`.`{Quote(table)}`;";
                await using var reader = await showCreate.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    throw new InvalidOperationException($"SHOW CREATE TABLE returned no DDL for {table}.");
                createTableSql = reader.GetString(1);
            }
            foreach (var definition in ExtractCheckConstraints(createTableSql))
            {
            await using var addCheck = target.CreateCommand();
                addCheck.CommandText = $"ALTER TABLE `{Quote(targetDatabase)}`.`{Quote(table)}` ADD {definition};";
            await addCheck.ExecuteNonQueryAsync();
            }
        }

        var foreignKeys = new Dictionary<(string Table, string Name), (string ReferencedTable, List<(string Column, string ReferencedColumn)> Parts)>();
        await using (var listForeignKeys = source.CreateCommand())
        {
            listForeignKeys.CommandText = "SELECT TABLE_NAME, CONSTRAINT_NAME, COLUMN_NAME, REFERENCED_TABLE_NAME, REFERENCED_COLUMN_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA=@schema AND REFERENCED_TABLE_NAME IS NOT NULL ORDER BY TABLE_NAME, CONSTRAINT_NAME, ORDINAL_POSITION;";
            listForeignKeys.Parameters.AddWithValue("@schema", sourceDatabase);
            await using var reader = await listForeignKeys.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var key = (reader.GetString(0), reader.GetString(1));
                if (!foreignKeys.TryGetValue(key, out var definition))
                    definition = (reader.GetString(3), new List<(string Column, string ReferencedColumn)>());
                definition.Parts.Add((reader.GetString(2), reader.GetString(4)));
                foreignKeys[key] = definition;
            }
        }

        foreach (var (key, definition) in foreignKeys)
        {
            string deleteRule;
            string updateRule;
            await using (var rules = source.CreateCommand())
            {
                rules.CommandText = "SELECT DELETE_RULE, UPDATE_RULE FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=@schema AND TABLE_NAME=@table AND CONSTRAINT_NAME=@constraint;";
                rules.Parameters.AddWithValue("@schema", sourceDatabase);
                rules.Parameters.AddWithValue("@table", key.Table);
                rules.Parameters.AddWithValue("@constraint", key.Name);
                await using var reader = await rules.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    throw new InvalidOperationException($"Missing referential rules for {key.Table}.{key.Name}.");
                deleteRule = reader.GetString(0);
                updateRule = reader.GetString(1);
            }
            var columns = string.Join(",", definition.Parts.Select(part => $"`{Quote(part.Column)}`"));
            var referencedColumns = string.Join(",", definition.Parts.Select(part => $"`{Quote(part.ReferencedColumn)}`"));
            await using var addForeignKey = target.CreateCommand();
            addForeignKey.CommandText = $"ALTER TABLE `{Quote(targetDatabase)}`.`{Quote(key.Table)}` ADD CONSTRAINT `{Quote(key.Name)}` FOREIGN KEY ({columns}) REFERENCES `{Quote(targetDatabase)}`.`{Quote(definition.ReferencedTable)}` ({referencedColumns}) ON DELETE {deleteRule} ON UPDATE {updateRule};";
            await addForeignKey.ExecuteNonQueryAsync();
        }

        await using (var checks = target.CreateCommand())
        {
            checks.CommandText = "SET FOREIGN_KEY_CHECKS=0;";
            await checks.ExecuteNonQueryAsync();
        }
        foreach (var table in tables)
        {
            var columns = new List<string>();
            await using (var listColumns = source.CreateCommand())
            {
                listColumns.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=@schema AND TABLE_NAME=@table AND (GENERATION_EXPRESSION IS NULL OR GENERATION_EXPRESSION='') ORDER BY ORDINAL_POSITION;";
                listColumns.Parameters.AddWithValue("@schema", sourceDatabase);
                listColumns.Parameters.AddWithValue("@table", table);
                await using var reader = await listColumns.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    columns.Add(reader.GetString(0));
            }
            if (columns.Count == 0)
                continue;
            var names = string.Join(",", columns.Select(name => $"`{Quote(name)}`"));
            await using var copyRows = target.CreateCommand();
            copyRows.CommandText = $"INSERT INTO `{Quote(targetDatabase)}`.`{Quote(table)}` ({names}) SELECT {names} FROM `{Quote(sourceDatabase)}`.`{Quote(table)}`;";
            await copyRows.ExecuteNonQueryAsync();
        }
        await using var restoreChecks = target.CreateCommand();
        restoreChecks.CommandText = "SET FOREIGN_KEY_CHECKS=1;";
        await restoreChecks.ExecuteNonQueryAsync();
    }

    private static string Quote(string identifier) => identifier.Replace("`", "``");

    private static IEnumerable<string> ExtractCheckConstraints(string ddl)
    {
        var searchFrom = 0;
        while (true)
        {
            var start = ddl.IndexOf("CONSTRAINT", searchFrom, StringComparison.OrdinalIgnoreCase);
            if (start < 0)
                yield break;
            var check = ddl.IndexOf("CHECK", start + "CONSTRAINT".Length, StringComparison.OrdinalIgnoreCase);
            var nextConstraint = ddl.IndexOf("CONSTRAINT", start + "CONSTRAINT".Length, StringComparison.OrdinalIgnoreCase);
            if (check < 0 || (nextConstraint >= 0 && nextConstraint < check))
            {
                searchFrom = start + "CONSTRAINT".Length;
                continue;
            }

            var open = ddl.IndexOf('(', check + "CHECK".Length);
            if (open < 0)
                throw new InvalidOperationException("SHOW CREATE TABLE contains CHECK without an expression.");
            var depth = 0;
            var quote = '\0';
            var escaped = false;
            for (var index = open; index < ddl.Length; index++)
            {
                var current = ddl[index];
                if (quote != '\0')
                {
                    if (escaped)
                    {
                        escaped = false;
                        continue;
                    }
                    if (current == '\\' && quote != '`')
                    {
                        escaped = true;
                        continue;
                    }
                    if (current == quote)
                    {
                        if (index + 1 < ddl.Length && ddl[index + 1] == quote)
                        {
                            index++;
                            continue;
                        }
                        quote = '\0';
                    }
                    continue;
                }
                if (current is '\'' or '"' or '`')
                {
                    quote = current;
                    continue;
                }
                if (current == '(')
                    depth++;
                else if (current == ')' && --depth == 0)
                {
                    yield return ddl[start..(index + 1)];
                    searchFrom = index + 1;
                    break;
                }
            }
            if (searchFrom <= start)
                throw new InvalidOperationException("Could not balance a CHECK expression in SHOW CREATE TABLE.");
        }
    }
}
''', encoding="utf-8")

tree = ET.parse(project)
root = tree.getroot()
props = root.find("PropertyGroup")
if props is None:
    props = ET.SubElement(root, "PropertyGroup")
ET.SubElement(props, "EnableDefaultCompileItems").text = "false"
items = ET.SubElement(root, "ItemGroup")
for relative in unit_files + included + [bootstrap.name]:
    ET.SubElement(items, "Compile", Include=relative)
ET.indent(tree, space="  ")
tree.write(project, encoding="unicode")
print(f"ORACLE10_INTEGRATION_FILESET=PASS included={len(included)} historySpecificExcluded={len(excluded)}")
print(f"ORACLE10_UNIT_FILESET=PASS included={len(unit_files)} historySpecificExcluded={len(unit_history_excluded)}")
print("ORACLE10_INTEGRATION_INCLUDED=" + ",".join(included))
print("ORACLE10_INTEGRATION_EXCLUDED=" + ",".join(excluded))
print("ORACLE10_UNIT_HISTORY_EXCLUDED=" + ",".join(unit_history_excluded))
PY

dotnet test "$candidate/backend/tests/Solqaryn.Tests/Solqaryn.Tests.csproj" \
  --configuration Release \
  --filter "Category!=Integration" \
  --logger "console;verbosity=normal"

dotnet test "$candidate/backend/tests/Solqaryn.Tests/Solqaryn.Tests.csproj" \
  --configuration Release \
  --filter "Category=Integration" \
  --logger "console;verbosity=normal"

dotnet restore "$probe/Oracle10Probe.csproj"
dotnet run --project "$probe/Oracle10Probe.csproj" --configuration Release
ConnectionStrings__DefaultConnection="$ORACLE10_CONNECTION" \
  dotnet run --project "$candidate/backend/scripts/Solqaryn.JsonProbe.csproj" --configuration Release

cd "$repo_root"
python3 scripts/security/priority3_tenant_isolation_audit.py --require-certified
test -z "$(git status --porcelain)"
echo "ORACLE_EF10_NET10_PROVIDER_LANE=PASS"
