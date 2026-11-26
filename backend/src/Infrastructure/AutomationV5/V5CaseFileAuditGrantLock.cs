using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// Supplemental API12 persistence guard: retains shared locks on the live relational
/// authority until the caller's transaction ends. Never authenticates a principal,
/// resolves a case/resource, grants a permission or commits a transaction.
/// </summary>
public static class V5CaseFileAuditGrantLock
{
    public static async Task<bool> TryAcquireAsync(AppDbContext db, int actorId,
        int baseRoleId, int tenantId, int branchId, int tenantRoleId,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var tx = db.Database.CurrentTransaction;
        if (tx is null || actorId <= 0 || baseRoleId <= 0 || tenantId <= 0 ||
            branchId <= 0 || tenantRoleId <= 0 ||
            db.AuthorizedCommercialEmpresaId != tenantId)
            return false;

        // Match the commercial base-session and selected-context membership predicates.
        // A locking read (not a snapshot SELECT) also serializes grant deletion,
        // permission disable, membership revocation and branch deactivation.
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = tx.GetDbTransaction();
        command.CommandText = """
            SELECT g.Id
            FROM Usuarios u
            JOIN Roles br ON br.Id = u.RolId
            JOIN UsuarioEmpresas bm ON bm.UsuarioId = u.Id AND bm.RolId = br.Id
            JOIN Empresas be ON be.Id = bm.EmpresaId
            JOIN UsuarioEmpresas m ON m.UsuarioId = u.Id
            JOIN Empresas e ON e.Id = m.EmpresaId
            JOIN Roles r ON r.Id = m.RolId
            JOIN Sucursales s ON s.EmpresaId = e.Id
            JOIN RolPermisos g ON g.RolId = r.Id
            JOIN Permisos p ON p.Id = g.PermisoId
            JOIN RolPermisos sg ON sg.RolId = r.Id
            JOIN Permisos sp ON sp.Id = sg.PermisoId
            WHERE u.Id = @actor AND u.RolId = @baseRole
              AND u.Activo = 1 AND u.Bloqueado = 0 AND u.Eliminado = 0
              AND br.Activo = 1 AND br.Eliminado = 0 AND br.Ambito = @scope
              AND bm.Activa = 1 AND be.Activa = 1
              AND (br.EmpresaId = bm.EmpresaId OR (br.EmpresaId IS NULL AND br.EsSistema = 1))
              AND m.EmpresaId = @tenant AND m.RolId = @tenantRole
              AND m.Activa = 1 AND e.Activa = 1
              AND r.Activo = 1 AND r.Eliminado = 0 AND r.Ambito = @scope
              AND (r.EmpresaId = m.EmpresaId OR (r.EmpresaId IS NULL AND r.EsSistema = 1))
              AND s.Id = @branch AND s.Activa = 1 AND s.Eliminado = 0
              AND p.Modulo = @audit AND p.Accion = @correlation
              AND p.Ambito = @scope AND p.Activo = 1 AND p.Eliminado = 0
              AND sp.Modulo = @branches AND sp.Accion = @view
              AND sp.Ambito = @scope AND sp.Activo = 1 AND sp.Eliminado = 0
            LIMIT 1 FOR SHARE
            """;
        foreach (var (name, value) in new (string, int)[]
        {
            ("@actor", actorId), ("@baseRole", baseRoleId), ("@tenant", tenantId),
            ("@tenantRole", tenantRoleId), ("@branch", branchId),
            ("@scope", (int)AmbitoAutorizacion.Empresa),
            ("@audit", (int)ModuloSistema.Auditoria),
            ("@correlation", (int)AccionPermiso.ReadAuditCorrelation),
            ("@branches", (int)ModuloSistema.Sucursales),
            ("@view", (int)AccionPermiso.Ver)
        })
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }
        var result = await command.ExecuteScalarAsync(ct);
        ct.ThrowIfCancellationRequested();
        return result is not null and not DBNull &&
            ReferenceEquals(tx, db.Database.CurrentTransaction) &&
            db.AuthorizedCommercialEmpresaId == tenantId;
    }
}
