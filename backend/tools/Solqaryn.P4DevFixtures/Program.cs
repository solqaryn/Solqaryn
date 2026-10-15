using System.Data;
using System.Data.Common;
using System.Text.Json;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;

// Herramienta operativa offline. NO es un endpoint HTTP ni modifica flags.
// Solo prepara B en la base DEV existente; nunca duplica la empresa A.
var mode = args.Length == 1 ? args[0].Trim().ToLowerInvariant() : "";
if (mode is not ("preflight" or "provision" or "revert"))
    throw new InvalidOperationException("Modo requerido: preflight | provision | revert.");

static string Env(string key) =>
    Environment.GetEnvironmentVariable(key)?.Trim() ?? "";

var connectionString = Env("ConnectionStrings__DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Falta la conexión inyectada por el operador.");
var connectionOptions = new MySqlConnectionStringBuilder(connectionString);
if (!string.Equals(Env("SOLQARYN_PROJECT_ID"), "SOLQARYN", StringComparison.Ordinal) ||
    !string.Equals(Env("SOLQARYN_EXECUTION_TARGET"), "DEV", StringComparison.Ordinal) ||
    !string.Equals(Env("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.Ordinal) ||
    !string.Equals(connectionOptions.Database, "solqaryn_dev", StringComparison.Ordinal))
    throw new InvalidOperationException("Scope lock: solo SOLQARYN/DEV y esquema solqaryn_dev.");
if (string.Equals(Env("MultiEmpresa__CommercialIsolationCertified"), "true",
        StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("CommercialIsolationCertified debe permanecer FALSE.");
if (string.Equals(Env("MultiEmpresa__PlatformOnboardingEnabled"), "true",
        StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("No usar el onboarding comercial en la certificación.");

await using var db = new MySqlConnection(connectionString);
await db.OpenAsync();
var schema = (await Scalar(db, null, "SELECT DATABASE();"))?.ToString();
if (!string.Equals(schema, "solqaryn_dev", StringComparison.Ordinal))
    throw new InvalidOperationException("La sesión MySQL no corresponde al esquema DEV autorizado.");

var count = Number(await Scalar(db, null, "SELECT COUNT(*) FROM `Empresas`;"));
Console.WriteLine($"P4_FIXTURE_PREFLIGHT: target=DEV companies={count} mode={mode} flags_unchanged=true");
if (mode == "preflight")
{
    Console.WriteLine(count == 1
        ? "APTO PARA EVALUAR: un tenant preexistente; provision requiere identidad A comprobada."
        : "STOP: esta receta requiere exactamente una empresa A preexistente.");
    return;
}

// La señal de autorización no es una credencial ni se usa como control único.
// Debe venir de una sesión operativa supervisada con acceso restringido a Aiven DEV.
if (Env("SOLQARYN_P4_APPROVED_SCOPE") != "DEV_CERTIFICATION_FIXTURE_ONLY")
    throw new InvalidOperationException("No existe aprobación explícita para escribir fixtures.");
var runId = Env("SOLQARYN_P4_RUN_ID").ToUpperInvariant();
if (!Regex.IsMatch(runId, "^[A-Z0-9]{8,16}$", RegexOptions.CultureInvariant))
    throw new InvalidOperationException("Run ID obligatorio: entre 8 y 16 caracteres A-Z/0-9.");
var companyBName = "P4_CERT_B_" + runId;
var loginB = "p4certb_" + runId.ToLowerInvariant();
var marker = "P4_CERT_DEV_" + runId;
var tag = "P4CERT_" + runId + "_%";

var aId = ParsePositive(Env("SOLQARYN_P4_COMPANY_A_ID"), "SOLQARYN_P4_COMPANY_A_ID");
var aUser = ParsePositive(Env("SOLQARYN_P4_USER_A_ID"), "SOLQARYN_P4_USER_A_ID");
await using var tx = await db.BeginTransactionAsync(IsolationLevel.Serializable);
var companies = Number(await Scalar(db, tx, "SELECT COUNT(*) FROM `Empresas`;"));
if (mode == "provision")
{
    if (companies != 1 ||
        Number(await Scalar(db, tx,
            "SELECT COUNT(*) FROM `Empresas` WHERE `Id`=@id AND `Activa`=1;",
            ("@id", aId))) != 1)
        throw new InvalidOperationException("STOP: se esperaba exactamente A activa y ninguna B.");

    var aMemberRole = Number(await Scalar(db, tx,
        """
        SELECT m.RolId
          FROM UsuarioEmpresas m
          JOIN Usuarios u ON u.Id=m.UsuarioId
          JOIN Roles r ON r.Id=m.RolId
         WHERE m.EmpresaId=@e AND m.UsuarioId=@u AND m.Activa=1
           AND u.Activo=1 AND u.Bloqueado=0 AND u.Eliminado=0
           AND r.Activo=1 AND r.Eliminado=0 AND r.EsAdministrador=1
           AND r.Ambito=1 AND (r.EmpresaId IS NULL OR r.EmpresaId=@e)
         LIMIT 1;
        """, ("@e", aId), ("@u", aUser)));
    if (aMemberRole == 0 || !await HasMasterGrants(db, tx, aMemberRole))
        throw new InvalidOperationException("STOP: el usuario A no es admin de A con grants Ver/Crear/Editar.");

    var template = Number(await Scalar(db, tx,
        """
        SELECT Id FROM Roles
         WHERE Ambito=1 AND EmpresaId IS NULL AND EsSistema=1
           AND EsAdministrador=1 AND Activo=1 AND Eliminado=0
         ORDER BY Id LIMIT 1;
        """));
    if (template == 0 || !await HasMasterGrants(db, tx, template))
        throw new InvalidOperationException("STOP: falta rol de plantilla con permisos explícitos para B.");
    if (Number(await Scalar(db, tx,
        "SELECT COUNT(*) FROM Usuarios WHERE NombreUsuario=@name;", ("@name", loginB))) > 0 ||
        Number(await Scalar(db, tx,
        "SELECT COUNT(*) FROM Empresas WHERE Nombre=@name;", ("@name", companyBName))) > 0)
        throw new InvalidOperationException("RunId reutilizado o colisión de identidad: ninguna escritura.");

    var password = Env("SOLQARYN_P4_USER_B_PASSWORD");
    if (password.Length < 20 || password.Length > 128 ||
        !password.Any(char.IsUpper) || !password.Any(char.IsLower) ||
        !password.Any(char.IsDigit) || !password.Any(x => !char.IsLetterOrDigit(x)))
        throw new InvalidOperationException("Contraseña B: 20-128 caracteres, mayúscula/minúscula/dígito/símbolo.");

    var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    if (await Execute(db, tx,
        """
        INSERT INTO Empresas (Nombre, Activa, FechaCreacion, FechaActualizacion,
                             CreadoPorNombreUsuario)
        VALUES (@name, 1, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), @marker);
        """, ("@name", companyBName), ("@marker", marker)) != 1)
        throw new InvalidOperationException("La empresa B no se insertó.");
    var bId = Number(await Scalar(db, tx, "SELECT LAST_INSERT_ID();"));

    if (await Execute(db, tx,
        """
        INSERT INTO Usuarios (NombreUsuario, NombreCompleto, PasswordHash, RolId,
                             Activo, Bloqueado, Eliminado, FechaCreacion)
        VALUES (@login, @name, @hash, @role, 1, 0, 0, UTC_TIMESTAMP(6));
        """, ("@login", loginB), ("@name", "Operador sintético " + runId),
        ("@hash", hash), ("@role", template)) != 1)
        throw new InvalidOperationException("No se creó la cuenta B.");
    var bUser = Number(await Scalar(db, tx, "SELECT LAST_INSERT_ID();"));
    if (await Execute(db, tx,
        """
        INSERT INTO UsuarioEmpresas
          (UsuarioId, EmpresaId, RolId, Activa, FechaCreacion, FechaActualizacion)
        VALUES (@u, @e, @role, 1, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6));
        """, ("@u", bUser), ("@e", bId), ("@role", template)) != 1)
        throw new InvalidOperationException("No se insertó la membresía B.");

    var audit = JsonSerializer.Serialize(new
    {
        RunId = runId, Mode = "DEV_Certification_Only",
        EmpresaAId = aId, EmpresaBId = bId, UsuarioBId = bUser
    });
    await Execute(db, tx,
        """
        INSERT INTO RegistrosAuditoria
          (Fecha, NombreUsuario, EmpresaId, Modulo, Accion, Entidad,
           ReferenciaId, Descripcion, ValoresNuevos, CorrelationId, Resultado)
        VALUES
          (UTC_TIMESTAMP(6), 'p4-cert-ops-cli', NULL, 'Configuracion', 'Crear', 'Empresa',
           @id, 'Aprovisionamiento temporal P4 DEV por operación aprobada',
           @payload, @runId, 'Exito');
        """, ("@id", bId), ("@payload", audit), ("@runId", runId));

    if (Number(await Scalar(db, tx, "SELECT COUNT(*) FROM Empresas;")) != 2 ||
        Number(await Scalar(db, tx,
            "SELECT COUNT(*) FROM UsuarioEmpresas WHERE EmpresaId=@e AND UsuarioId=@u AND Activa=1;",
            ("@e", bId), ("@u", bUser))) != 1)
        throw new InvalidOperationException("La identidad A/B quedó incompleta; se revierte.");
    await tx.CommitAsync();
    Console.WriteLine($"P4_FIXTURE_PROVISION_PASS run={runId} A={aId} B={bId} loginB={loginB}");
    Console.WriteLine("Contraseña nunca registrada. No se cambió CommercialIsolationCertified ni ninguna bandera piloto.");
    return;
}

// Reversión física del fixture, no un borrado general de tenants.
// Permite únicamente maestros sintéticos con el prefijo exacto; ante dependencias desconocidas STOP.
if (companies != 2)
    throw new InvalidOperationException("STOP: revert exige A+B exactamente; no toca 0/1/3+ empresas.");
var bIdToRemove = Number(await Scalar(db, tx,
    "SELECT Id FROM Empresas WHERE Nombre=@name AND CreadoPorNombreUsuario=@marker;",
    ("@name", companyBName), ("@marker", marker)));
var bUserId = Number(await Scalar(db, tx,
    "SELECT Id FROM Usuarios WHERE NombreUsuario=@name;",
    ("@name", loginB)));
if (bIdToRemove <= 0 || bIdToRemove == aId || bUserId <= 0 ||
    Number(await Scalar(db, tx,
        "SELECT COUNT(*) FROM Empresas WHERE Id=@id AND Activa=1;",
        ("@id", aId))) != 1)
    throw new InvalidOperationException("STOP: ownership, empresa A o usuario B no coinciden.");
if (Number(await Scalar(db, tx,
    "SELECT COUNT(*) FROM UsuarioEmpresas WHERE EmpresaId=@e;", ("@e", bIdToRemove))) != 1 ||
    Number(await Scalar(db, tx,
    "SELECT COUNT(*) FROM UsuarioEmpresas WHERE UsuarioId=@u AND EmpresaId=@e AND Activa=1;",
    ("@u", bUserId), ("@e", bIdToRemove))) != 1 ||
    Number(await Scalar(db, tx,
    "SELECT COUNT(*) FROM UsuarioEmpresas WHERE UsuarioId=@u AND EmpresaId<>@e;",
    ("@u", bUserId), ("@e", bIdToRemove))) != 0)
    throw new InvalidOperationException("STOP: membresías ajenas o inesperadas; requiere revisión manual.");

var permitted = new HashSet<string>(StringComparer.Ordinal)
{ "Clientes", "Proveedores", "TipoClientes" };
var tables = await Strings(db, tx,
    """
    SELECT c.TABLE_NAME
    FROM INFORMATION_SCHEMA.COLUMNS c
    JOIN INFORMATION_SCHEMA.TABLES t
      ON t.TABLE_SCHEMA=c.TABLE_SCHEMA AND t.TABLE_NAME=c.TABLE_NAME
    WHERE c.TABLE_SCHEMA=DATABASE() AND c.COLUMN_NAME='EmpresaId'
      AND t.TABLE_TYPE='BASE TABLE'
      AND c.TABLE_NAME NOT IN ('UsuarioEmpresas', 'RegistroAuditoria', 'Empresas')
    ORDER BY c.TABLE_NAME;
    """);
foreach (var table in tables)
{
    if (!Regex.IsMatch(table, "^[A-Za-z0-9_]+$", RegexOptions.CultureInvariant))
        throw new InvalidOperationException("Nombre físico de tabla inválido.");
    var countByTenant = Number(await Scalar(db, tx,
        $"SELECT COUNT(*) FROM `{table}` WHERE EmpresaId=@e;", ("@e", bIdToRemove)));
    if (countByTenant == 0) continue;
    if (!permitted.Contains(table))
        throw new InvalidOperationException(
            $"STOP: {table} contiene {countByTenant} filas de B; no eliminar datos no inventariados.");
    var foreign = Number(await Scalar(db, tx,
        $"SELECT COUNT(*) FROM `{table}` WHERE EmpresaId=@e AND (Nombre IS NULL OR Nombre NOT LIKE @tag);",
        ("@e", bIdToRemove), ("@tag", tag)));
    if (foreign > 0)
        throw new InvalidOperationException($"STOP: {table} tiene registros B fuera del prefijo certificado.");
}
foreach (var table in new[] { "Clientes", "Proveedores", "TipoClientes" })
{
    if (!tables.Contains(table))
        throw new InvalidOperationException("Falta tabla comercial canónica; rollback abortado.");
    await Execute(db, tx,
        $"DELETE FROM `{table}` WHERE EmpresaId=@e AND Nombre LIKE @tag;",
        ("@e", bIdToRemove), ("@tag", tag));
}
foreach (var table in tables)
    if (Number(await Scalar(db, tx,
        $"SELECT COUNT(*) FROM `{table}` WHERE EmpresaId=@e;", ("@e", bIdToRemove))) != 0)
        throw new InvalidOperationException("STOP: referencias B subsistentes tras purga acotada.");
if (await Execute(db, tx,
    "DELETE FROM UsuarioEmpresas WHERE EmpresaId=@e AND UsuarioId=@u;",
    ("@e", bIdToRemove), ("@u", bUserId)) != 1 ||
    await Execute(db, tx,
    "DELETE FROM Usuarios WHERE Id=@u AND NombreUsuario=@name;",
    ("@u", bUserId), ("@name", loginB)) != 1 ||
    await Execute(db, tx,
    "DELETE FROM Empresas WHERE Id=@e AND Nombre=@name AND CreadoPorNombreUsuario=@marker;",
    ("@e", bIdToRemove), ("@name", companyBName), ("@marker", marker)) != 1)
    throw new InvalidOperationException("STOP: alguna entidad del fixture no se pudo revertir.");
await Execute(db, tx,
    """
    INSERT INTO RegistrosAuditoria
      (Fecha, NombreUsuario, EmpresaId, Modulo, Accion, Entidad,
       ReferenciaId, Descripcion, CorrelationId, Resultado)
    VALUES
      (UTC_TIMESTAMP(6), 'p4-cert-ops-cli', NULL, 'Configuracion', 'EliminarLogico', 'Empresa',
       @id, 'Retiro exacto del fixture temporal P4 DEV',
       @runId, 'Exito');
    """, ("@id", bIdToRemove), ("@runId", runId));
if (Number(await Scalar(db, tx, "SELECT COUNT(*) FROM Empresas;")) != 1)
    throw new InvalidOperationException("STOP: rollback no restauró el conteo de empresas.");
await tx.CommitAsync();
Console.WriteLine($"P4_FIXTURE_REVERT_PASS run={runId} retained_A={aId} restored_companies=1");

static int ParsePositive(string raw, string label) =>
    int.TryParse(raw, out var value) && value > 0
        ? value : throw new InvalidOperationException(label + " es obligatorio y positivo.");

static long Number(object? value) =>
    value is null or DBNull ? 0 : Convert.ToInt64(value);

static async Task<DbCommand> Command(
    MySqlConnection db, DbTransaction? tx, string sql,
    params (string key, object? value)[] parameters)
{
    var cmd = db.CreateCommand();
    cmd.CommandText = sql;
    cmd.Transaction = tx;
    foreach (var (key, value) in parameters)
    {
        var parameter = cmd.CreateParameter();
        parameter.ParameterName = key;
        parameter.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(parameter);
    }
    await Task.CompletedTask;
    return cmd;
}

static async Task<object?> Scalar(MySqlConnection db, DbTransaction? tx,
    string sql, params (string key, object? value)[] parameters)
{
    await using var cmd = await Command(db, tx, sql, parameters);
    return await cmd.ExecuteScalarAsync();
}

static async Task<int> Execute(MySqlConnection db, DbTransaction? tx,
    string sql, params (string key, object? value)[] parameters)
{
    await using var cmd = await Command(db, tx, sql, parameters);
    return await cmd.ExecuteNonQueryAsync();
}

static async Task<List<string>> Strings(MySqlConnection db, DbTransaction tx, string sql)
{
    await using var cmd = await Command(db, tx, sql);
    await using var reader = await cmd.ExecuteReaderAsync();
    var names = new List<string>();
    while (await reader.ReadAsync())
        names.Add(reader.GetString(0));
    return names;
}

static async Task<bool> HasMasterGrants(
    MySqlConnection db, DbTransaction tx, long rolId)
{
    return Number(await Scalar(db, tx,
        """
        SELECT COUNT(DISTINCT CONCAT(p.Modulo, ':', p.Accion))
        FROM RolPermisos rp JOIN Permisos p ON p.Id=rp.PermisoId
        WHERE rp.RolId=@role AND p.Activo=1 AND p.Eliminado=0
          AND p.Modulo IN ('Proveedores','Clientes') AND p.Accion IN ('Ver','Crear','Editar');
        """, ("@role", rolId))) == 6;
}
