using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// Initial, narrowly allowlisted commercial case-file resource: an existing
/// warehouse, whose company is derived from its actual branch. An authenticated
/// caller must separately lock the exact case-file operation and membership.
/// </summary>
public static class V5CaseFileWarehouseResource
{
    public const string ResourceType = "Almacen";

    public static async ValueTask<bool> TryLockAsync(AppDbContext db, int tenant,
        int branch, int role, string resourceType, string resourceRef,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var tx = db.Database.CurrentTransaction;
        if (tx is null || tenant <= 0 || branch <= 0 || role <= 0 ||
            db.AuthorizedCommercialEmpresaId != tenant ||
            !string.Equals(resourceType, ResourceType, StringComparison.Ordinal) ||
            !int.TryParse(resourceRef, NumberStyles.None, CultureInfo.InvariantCulture, out var warehouse) ||
            warehouse <= 0 || resourceRef != warehouse.ToString(CultureInfo.InvariantCulture))
            return false;

        // Lock the actual resource, branch, company and its own read grant.
        // A caller cannot use API12 or the writer permission to access a warehouse
        // that its selected role cannot view. No names or business fields leave SQL.
        await using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.Transaction = tx.GetDbTransaction();
        cmd.CommandText = """
            SELECT a.Id FROM Almacenes a
            JOIN Sucursales s ON s.Id=a.SucursalId
            JOIN Empresas e ON e.Id=s.EmpresaId
            JOIN Roles r ON r.Id=@role
            JOIN RolPermisos g ON g.RolId=r.Id
            JOIN Permisos p ON p.Id=g.PermisoId
            WHERE a.Id=@warehouse AND a.Activo=1 AND a.Eliminado=0
              AND s.Id=@branch AND s.EmpresaId=@tenant AND s.Activa=1 AND s.Eliminado=0
              AND e.Activa=1 AND r.Activo=1 AND r.Eliminado=0 AND r.Ambito=@scope
              AND (r.EmpresaId=e.Id OR (r.EmpresaId IS NULL AND r.EsSistema=1))
              AND p.Modulo=@module AND p.Accion=@view AND p.Ambito=@scope
              AND p.Activo=1 AND p.Eliminado=0
            LIMIT 1 FOR SHARE
            """;
        foreach (var (name, value) in new (string, int)[]
        {
            ("@role", role), ("@warehouse", warehouse), ("@branch", branch), ("@tenant", tenant),
            ("@scope", (int)AmbitoAutorizacion.Empresa),
            ("@module", (int)ModuloSistema.Almacenes), ("@view", (int)AccionPermiso.Ver)
        })
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            cmd.Parameters.Add(p);
        }
        var result = await cmd.ExecuteScalarAsync(ct);
        ct.ThrowIfCancellationRequested();
        return result is not null and not DBNull &&
            ReferenceEquals(tx, db.Database.CurrentTransaction) &&
            db.AuthorizedCommercialEmpresaId == tenant;
    }
}
