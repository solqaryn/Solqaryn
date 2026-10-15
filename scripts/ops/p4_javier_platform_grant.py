#!/usr/bin/env python3
"""One-time owner-authorized SOLQARYN platform bootstrap (DEV only).

Only preflight, apply, verify and reversible deactivate. No schema changes,
password changes, tenant changes or legacy RolId changes.
Never call apply without a verified encrypted backup and restore drill.
"""
from __future__ import annotations

import argparse
import json
import os
import ssl
import sys
from datetime import datetime, timezone

import pymysql
from pymysql.cursors import DictCursor

ROLE_NAME = "Administrador global SOLQARYN"
ROLE_KEY = "SOLQARYN_ADMIN_PLATAFORMA"
ACTOR = "SOLQARYN_OWNER_APPROVED_BOOTSTRAP_P4"
LOCK_KEY = "SOLQARYN_DEV_P4_JAVIER_PLATFORM_GRANT"
TARGET_USERNAME = "javier"


class UnsafeState(Exception):
    pass


def require(condition: bool, message: str):
    if not condition:
        raise UnsafeState(message)


def validate_scope(action: str):
    expected = {
        "GITHUB_REPOSITORY": "solqaryn/Solqaryn",
        "GITHUB_REF_NAME": "dev",
        "DB_HOST": "solqaryn-mysql-solqaryn.h.aivencloud.com",
        "DB_PORT": "14402",
        "DB_NAME": "solqaryn_dev",
        "DB_USER": "solqaryn_dev_user",
    }
    for key, value in expected.items():
        require(os.environ.get(key) == value, "STOP: scope mismatch " + key)
    require(bool(os.environ.get("DB_PASSWORD")), "STOP: missing DEV database secret")
    require(os.environ.get("GITHUB_SHA", "").strip() != "",
            "STOP: GitHub commit reference missing")
    if action == "apply":
        require(os.environ.get("GITHUB_EVENT_NAME") == "push",
                "STOP: grant only by protected DEV merge push")
        require(os.environ.get("P4_BACKUP_RESTORED") == "VERIFIED",
                "STOP: encrypted backup + disposable restore verification required")
        require(os.environ.get("P4_OWNER_APPROVAL") == "P4_JAVIER_OWNER_APPROVED_20261015",
                "STOP: direct owner approval marker absent")
    if action == "rollback":
        require(os.environ.get("GITHUB_EVENT_NAME") == "workflow_dispatch" and
                os.environ.get("P4_ROLLBACK_APPROVAL") == "REVOKE_ONLY_P4_JAVIER",
                "STOP: specific rollback dispatch approval missing")


def connect():
    # Same encrypted-only transport as the existing --ssl-mode=REQUIRED DEV contract.
    # Aiven service certificate trust is not provisioned as a separate CA secret.
    context = ssl.SSLContext(ssl.PROTOCOL_TLS_CLIENT)
    context.check_hostname = False
    context.verify_mode = ssl.CERT_NONE
    c = pymysql.connect(
        host=os.environ["DB_HOST"],
        port=int(os.environ["DB_PORT"]),
        user=os.environ["DB_USER"],
        password=os.environ["DB_PASSWORD"],
        database=os.environ["DB_NAME"],
        ssl=context,
        charset="utf8mb4",
        autocommit=False,
        connect_timeout=12,
        read_timeout=25,
        write_timeout=25,
        cursorclass=DictCursor,
    )
    with c.cursor() as cur:
        cur.execute("SELECT DATABASE() AS db, SUBSTRING_INDEX(CURRENT_USER(),'@',1) AS user")
        row = cur.fetchone()
        require(row["db"] == "solqaryn_dev" and row["user"] == "solqaryn_dev_user",
                "STOP: unexpected connected database principal")
        cur.execute("SHOW SESSION STATUS LIKE 'Ssl_cipher'")
        ssl_row = cur.fetchone()
        require(bool(ssl_row and ssl_row.get("Value")), "STOP: unencrypted DB connection")
    return c


def select(c, sql: str, args=()):
    with c.cursor() as cur:
        cur.execute(sql, args)
        return cur.fetchall()


def one(c, sql: str, args=()):
    rows = select(c, sql, args)
    require(len(rows) == 1, "STOP: expected single DB result")
    return rows[0]


def execute(c, sql: str, args=()):
    with c.cursor() as cur:
        n = cur.execute(sql, args)
        return n, cur.lastrowid


def check_schema(c):
    required = {
        "Usuarios": {"Id", "NombreUsuario", "Activo", "Bloqueado", "Eliminado", "RolId"},
        "Roles": {"Id", "Nombre", "NombreNormalizado", "Descripcion", "EsSistema",
                  "EsAdministrador", "Ambito", "EmpresaId", "Activo", "Eliminado",
                  "FechaCreacion", "CreadoPorUsuarioId"},
        "UsuarioRolesPlataforma": {"Id", "UsuarioId", "RolId", "Activa",
                                   "FechaCreacion", "FechaActualizacion",
                                   "CreadoPorNombreUsuario"},
        "RegistrosAuditoria": {"Id", "Fecha", "NombreUsuario", "EmpresaId", "UsuarioId",
                              "AmbitoAutoridad", "OrigenAutorizacion", "Modulo", "Accion",
                              "Entidad", "ReferenciaId", "Descripcion", "ValoresNuevos",
                              "Motivo", "CorrelationId", "Resultado"},
        "Empresas": {"Id", "Activa"},
        "UsuarioEmpresas": {"Id", "UsuarioId", "EmpresaId", "Activa"},
    }
    names = list(required.keys())
    placeholder = ",".join(["%s"] * len(names))
    rows = select(c,
        "SELECT TABLE_NAME, COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS "
        "WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME IN (" + placeholder + ")",
        names)
    present = {k: set() for k in names}
    for row in rows:
        present[row["TABLE_NAME"]].add(row["COLUMN_NAME"])
    for table, cols in required.items():
        require(not (cols - present[table]), "STOP: DB schema missing columns in " + table)
    triggers = one(c,
        "SELECT COUNT(*) AS n FROM INFORMATION_SCHEMA.TRIGGERS "
        "WHERE TRIGGER_SCHEMA=DATABASE() AND EVENT_OBJECT_TABLE IN "
        "('Roles', 'UsuarioRolesPlataforma', 'RegistrosAuditoria')")
    require(triggers["n"] == 0, "STOP: unexpected privilege/audit table triggers")


def principal(c, lock=False):
    suffix = " FOR UPDATE" if lock else ""
    rows = select(c,
        "SELECT u.Id AS uid,u.NombreUsuario AS login,u.Activo AS enabled,"
        "u.Bloqueado AS blocked,u.Eliminado AS deleted,u.RolId AS legacy_role,"
        "r.Ambito AS legacy_scope,r.EsAdministrador AS legacy_admin,"
        "r.Activo AS legacy_enabled,r.Eliminado AS legacy_deleted "
        "FROM Usuarios u JOIN Roles r ON r.Id=u.RolId "
        "WHERE LOWER(u.NombreUsuario)=LOWER(%s)" + suffix, (TARGET_USERNAME,))
    require(len(rows) == 1, "STOP: javier identity not unique or legacy role absent")
    user = rows[0]
    require(user["login"] == TARGET_USERNAME, "STOP: unexpected persisted username")
    require(user["enabled"] == 1 and user["blocked"] == 0 and user["deleted"] == 0,
            "STOP: platform target account is not active")
    require(user["legacy_scope"] == 1 and user["legacy_admin"] == 1 and
            user["legacy_enabled"] == 1 and user["legacy_deleted"] == 0,
            "STOP: unrecognized original enterprise role")
    return user


def company_baseline(c):
    r = one(c, "SELECT COUNT(*) AS total, COALESCE(SUM(Activa=1),0) AS active "
                  "FROM Empresas")
    require(r["total"] == 1 and r["active"] == 0,
            "STOP: unexpected company baseline; no company will be modified")


def initial_state(c, lock=False):
    check_schema(c)
    u = principal(c, lock)
    company_baseline(c)
    suffix = " FOR UPDATE" if lock else ""
    existing = select(c, "SELECT Id FROM UsuarioRolesPlataforma "
                         "WHERE UsuarioId=%s" + suffix, (u["uid"],))
    require(len(existing) == 0, "STOP: existing platform assignment requires human review")
    candidates = select(c, "SELECT Id FROM Roles WHERE "
        "Ambito=2 AND EsAdministrador=1 AND Activo=1 AND Eliminado=0"
        + suffix)
    require(len(candidates) == 0, "STOP: another platform admin role already exists")
    role_collision = select(c, "SELECT Id FROM Roles WHERE "
         "UPPER(NombreNormalizado)=UPPER(%s)" + suffix, (ROLE_KEY,))
    require(len(role_collision) == 0, "STOP: platform role key already exists")
    membership = one(c, "SELECT COUNT(*) AS total FROM UsuarioEmpresas "
                        "WHERE UsuarioId=%s", (u["uid"],))
    require(membership["total"] == 1,
            "STOP: unexpected original enterprise memberships")
    return u


def write_audit(c, role_id: int, grant_id: int, username: str, action: str):
    correlation = "P4JAVIER" + os.environ["GITHUB_RUN_ID"]
    msg = "Autorizacion explicita del propietario SOLQARYN; bootstrap global DEV"
    for key, item_id, verb in [("Rol", role_id, "Crear"),
                               ("UsuarioRolPlataforma", grant_id, "AsignarRol")]:
        payload = json.dumps({
            "stage": action, "roleId": role_id, "assignmentId": grant_id,
            "principal": username, "ownerAuthorization": "2026-10-15",
            "sourceCommit": os.environ["GITHUB_SHA"],
        }, separators=(",", ":"))
        n, _ = execute(c,
            "INSERT INTO RegistrosAuditoria "
            "(Fecha, UsuarioId, NombreUsuario, EmpresaId, AmbitoAutoridad,"
            " OrigenAutorizacion, Modulo, Accion, Entidad, ReferenciaId,"
            " Descripcion, ValoresNuevos, Motivo, CorrelationId, Resultado) "
            "VALUES (UTC_TIMESTAMP(6),NULL,%s,NULL,2,NULL,"
            "'Roles',%s,%s,%s,%s,%s,%s,%s,'Exito')",
            (ACTOR, verb, key, item_id, msg, payload,
             "Owner approved role bootstrap exclusively in DEV", correlation))
        require(n == 1, "STOP: audit insert failed")


def initial_apply():
    c = connect()
    locked = False
    committed = False
    try:
        require(one(c, "SELECT GET_LOCK(%s,15) AS acquired",
                    (LOCK_KEY,))["acquired"] == 1,
                "STOP: concurrent platform grant operation")
        locked = True
        execute(c, "SET TRANSACTION ISOLATION LEVEL SERIALIZABLE")
        c.begin()
        u = initial_state(c, lock=True)
        n, role_id = execute(c,
            "INSERT INTO Roles "
            "(Nombre,NombreNormalizado,Descripcion,EsSistema,EsAdministrador,"
            " Ambito,EmpresaId,Activo,Eliminado,FechaCreacion,CreadoPorUsuarioId)"
            " VALUES (%s,%s,%s,1,1,2,NULL,1,0,UTC_TIMESTAMP(6),%s)",
            (ROLE_NAME, ROLE_KEY, "Autoridad global de SOLQARYN; no concede "
              "lectura ni escritura automatica de datos empresariales.", u["uid"]))
        require(n == 1 and role_id > 0, "STOP: global role was not created")
        n, grant_id = execute(c,
            "INSERT INTO UsuarioRolesPlataforma "
            "(UsuarioId,RolId,Activa,FechaCreacion,FechaActualizacion,"
            "CreadoPorUsuarioId,CreadoPorNombreUsuario) "
            "VALUES (%s,%s,1,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6),NULL,%s)",
            (u["uid"], role_id, ACTOR))
        require(n == 1 and grant_id > 0, "STOP: platform assignment was not created")
        write_audit(c, role_id, grant_id, u["login"], "APPLY")
        internal = one(c,
            "SELECT COUNT(*) AS n FROM UsuarioRolesPlataforma a "
            "JOIN Roles r ON r.Id=a.RolId JOIN Usuarios u ON u.Id=a.UsuarioId "
            "WHERE a.Id=%s AND a.UsuarioId=%s AND a.RolId=%s "
            "AND a.Activa=1 AND r.Activo=1 AND r.Eliminado=0 "
            "AND r.Ambito=2 AND r.EsAdministrador=1 AND r.EmpresaId IS NULL "
            "AND u.Activo=1 AND u.Bloqueado=0 AND u.Eliminado=0",
            (grant_id, u["uid"], role_id))
        require(internal["n"] == 1, "STOP: in-transaction grant verification failed")
        company_baseline(c)
        c.commit()
        committed = True
        print("P4_JAVIER_GRANT_COMMITTED=1")
    except Exception:
        if not committed:
            c.rollback()
        raise
    finally:
        if locked:
            execute(c, "SELECT RELEASE_LOCK(%s)", (LOCK_KEY,))
        c.close()
    # A new connection proves the changes are persisted and visible externally.
    verify_grant()


def verify_grant():
    c = connect()
    try:
        execute(c, "START TRANSACTION READ ONLY")
        check_schema(c)
        u = principal(c)
        company_baseline(c)
        grants = select(c,
            "SELECT a.Id AS grant_id, a.Activa AS grant_active,"
            "r.Id AS role_id,r.Activo AS role_active,r.EsAdministrador AS admin,"
            "r.EsSistema AS system_role,r.Ambito AS scope,r.EmpresaId AS company,"
            "r.NombreNormalizado AS role_key,r.Eliminado AS deleted,"
            "r.CreadoPorUsuarioId AS created_by,"
            "a.CreadoPorNombreUsuario AS marker "
            "FROM UsuarioRolesPlataforma a JOIN Roles r ON r.Id=a.RolId "
            "WHERE a.UsuarioId=%s", (u["uid"],))
        require(len(grants) == 1, "STOP: persisted platform assignments not unique")
        g = grants[0]
        require(g["grant_active"] == 1 and g["role_active"] == 1 and
                g["admin"] == 1 and g["system_role"] == 1 and
                g["scope"] == 2 and g["company"] is None and
                g["role_key"] == ROLE_KEY and g["deleted"] == 0 and
                g["created_by"] == u["uid"] and g["marker"] == ACTOR,
                "STOP: global authority readback invalid")
        require(one(c,
            "SELECT COUNT(*) AS n FROM UsuarioRolesPlataforma "
            "WHERE RolId=%s", (g["role_id"],))["n"] == 1,
            "STOP: unexpected other platform principal")
        require(one(c,
            "SELECT COUNT(*) AS n FROM RolPermisos WHERE RolId=%s",
            (g["role_id"],))["n"] == 0,
            "STOP: unexpected commercial permission mapping")
        require(one(c,
            "SELECT COUNT(*) AS n FROM RegistrosAuditoria "
            "WHERE NombreUsuario=%s AND ReferenciaId=%s "
            "AND Entidad='UsuarioRolPlataforma' AND Accion='AsignarRol' "
            "AND AmbitoAutoridad=2 AND Resultado='Exito'",
            (ACTOR, g["grant_id"]))["n"] == 1,
            "STOP: grant audit row missing")
        enterprise = one(c, "SELECT COUNT(*) AS n FROM UsuarioEmpresas "
                           "WHERE UsuarioId=%s", (u["uid"],))
        require(enterprise["n"] == 1,
                "STOP: enterprise membership was unexpectedly modified")
        print("P4_JAVIER_GLOBAL_AUTHORITY_READBACK=PASS")
        print("P4_JAVIER_PLATFORM_ASSIGNED=1")
        print("P4_JAVIER_COMMERCIAL_PERMISSIONS_IMPORTED=0")
        print("P4_JAVIER_COMPANIES_ACTIVATED=0")
        c.rollback()
    finally:
        c.close()


def rollback():
    c = connect()
    locked = False
    committed = False
    try:
        require(one(c, "SELECT GET_LOCK(%s,15) AS acquired",
                    (LOCK_KEY,))["acquired"] == 1,
                "STOP: concurrent platform grant operation")
        locked = True
        execute(c, "SET TRANSACTION ISOLATION LEVEL SERIALIZABLE")
        c.begin()
        u = principal(c, lock=True)
        g = select(c,
            "SELECT a.Id AS assignment_id,a.Activa AS active,"
            "r.Id AS role_id,r.Activo AS role_active,"
            "r.CreadoPorUsuarioId AS creator,r.NombreNormalizado AS name,"
            "r.Ambito AS scope,r.EsAdministrador AS admin,"
            "a.CreadoPorNombreUsuario AS marker "
            "FROM UsuarioRolesPlataforma a JOIN Roles r ON r.Id=a.RolId "
            "WHERE a.UsuarioId=%s FOR UPDATE", (u["uid"],))
        require(len(g) == 1, "STOP: rollback target is not unique")
        g = g[0]
        require(g["active"] == 1 and g["role_active"] == 1 and
                g["creator"] == u["uid"] and g["name"] == ROLE_KEY and
                g["scope"] == 2 and g["admin"] == 1 and
                g["marker"] == ACTOR,
                "STOP: platform target was modified after grant")
        require(one(c, "SELECT COUNT(*) AS n FROM UsuarioRolesPlataforma "
                       "WHERE RolId=%s", (g["role_id"],))["n"] == 1,
                "STOP: role now serves another user, cannot deactivate")
        require(one(c, "SELECT COUNT(*) AS n FROM RolPermisos WHERE RolId=%s",
                       (g["role_id"],))["n"] == 0,
                "STOP: role acquired permissions, manual review required")
        n, _ = execute(c,
            "UPDATE UsuarioRolesPlataforma "
            "SET Activa=0,FechaActualizacion=UTC_TIMESTAMP(6),"
            "ActualizadoPorNombreUsuario=%s WHERE Id=%s AND Activa=1",
            (ACTOR, g["assignment_id"]))
        require(n == 1, "STOP: rollback assignment failed")
        n, _ = execute(c,
            "UPDATE Roles SET Activo=0,FechaActualizacion=UTC_TIMESTAMP(6) "
            "WHERE Id=%s AND Activo=1 AND NombreNormalizado=%s",
            (g["role_id"], ROLE_KEY))
        require(n == 1, "STOP: rollback role failed")
        write_audit(c, g["role_id"], g["assignment_id"], u["login"], "REVOKE")
        company_baseline(c)
        c.commit()
        committed = True
        print("P4_JAVIER_ROLLBACK_COMMITTED=1")
    except Exception:
        if not committed:
            c.rollback()
        raise
    finally:
        if locked:
            execute(c, "SELECT RELEASE_LOCK(%s)", (LOCK_KEY,))
        c.close()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("action",
        choices=("preflight", "apply", "verify", "rollback"))
    args = parser.parse_args()
    validate_scope(args.action)
    if args.action == "preflight":
        c = connect()
        try:
            execute(c, "START TRANSACTION READ ONLY")
            user = initial_state(c)
            print("P4_JAVIER_PLATFORM_PREFLIGHT=PASS")
            print("P4_JAVIER_VERIFIED_LOGIN=" + user["login"])
            print("P4_JAVIER_EXISTING_PLATFORM_GRANTS=0")
            c.rollback()
        finally:
            c.close()
    elif args.action == "apply":
        initial_apply()
    elif args.action == "verify":
        verify_grant()
    elif args.action == "rollback":
        rollback()


if __name__ == "__main__":
    try:
        main()
    except (UnsafeState, pymysql.MySQLError, OSError) as err:
        # Print only sanitized codes: no SQL with bound values or DB secret.
        print("P4_JAVIER_GRANT_STOP=" + err.__class__.__name__, file=sys.stderr)
        if isinstance(err, UnsafeState):
            print(str(err), file=sys.stderr)
        sys.exit(1)
