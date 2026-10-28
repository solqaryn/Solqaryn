#!/usr/bin/env python3
"""DEV-only public identity gate: signed tenant positive and unscoped negatives.

Uses the authorized Vercel proxy for the HMAC proof, and read-only MySQL to
choose two active public store bindings. It never creates proofs, credentials,
companies, products, or writes database rows. No tenant IDs, names, keys,
response bodies, or secrets are printed.
"""
import json
import os
import re
import subprocess
import sys
import uuid
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen

FRONTEND = "https://solqaryn-dev.vercel.app"
BACKEND = "https://solqaryn-api-dev-fxx8.onrender.com"
ISOLATION = "TENANT_DATA_ISOLATION_REQUIRED"
KEY_PATTERN = re.compile(r"[a-z][a-z0-9-]{0,46}[a-z0-9]\Z")


class GateError(RuntimeError):
    pass


def require(condition, code):
    if not condition:
        raise GateError(code)


def isolated(status, payload):
    return (
        status == 503
        and isinstance(payload, dict)
        and payload.get("code") == ISOLATION
        and payload.get("success") is not True
    )


def verified_binding(status, payload, expected_company_id):
    data = payload.get("data") if isinstance(payload, dict) else None
    return (
        status == 200
        and payload.get("success") is True
        and isinstance(data, dict)
        and type(data.get("empresaId")) is int
        and data["empresaId"] == expected_company_id
        and data.get("verificada") is True
    )


def verified_identity(status, payload, expected_company_name):
    data = payload.get("data") if isinstance(payload, dict) else None
    return (
        status == 200
        and payload.get("success") is True
        and isinstance(data, dict)
        and isinstance(data.get("nombreComercial"), str)
        and bool(data["nombreComercial"].strip())
        and data["nombreComercial"] == expected_company_name
    )


def read_bindings(raw):
    companies = set()
    result = []
    for line in raw.splitlines():
        columns = line.split("\t")
        require(len(columns) == 3, "INVALID_BINDING_RECORD")
        key, company_raw, company_hex = columns
        require(KEY_PATTERN.fullmatch(key) is not None, "INVALID_STORE_KEY")
        require(company_raw.isascii() and company_raw.isdigit() and
                int(company_raw) > 0, "INVALID_COMPANY_ID")
        try:
            company_name = bytes.fromhex(company_hex).decode("utf-8")
        except (ValueError, UnicodeDecodeError):
            raise GateError("INVALID_COMPANY_NAME_ENCODING") from None
        require(bool(company_name.strip()), "EMPTY_COMPANY_NAME")
        company_id = int(company_raw)
        if company_id not in companies:
            companies.add(company_id)
            result.append((key, company_id, company_name))
        if len(result) == 2:
            break
    require(len(result) == 2, "TWO_ACTIVE_TENANT_BINDINGS_REQUIRED")
    return result


def get_bindings():
    require(os.getenv("FRONTEND") == FRONTEND, "FRONTEND_NOT_CANONICAL_DEV")
    require(os.getenv("BACKEND") == BACKEND, "BACKEND_NOT_CANONICAL_DEV")
    require(os.getenv("DB_NAME") == "solqaryn_dev", "NOT_DEV_DATABASE")
    require(bool(os.getenv("DB_PASSWORD")), "DEV_DB_SECRET_NOT_AVAILABLE")
    for name in ("DB_HOST", "DB_PORT", "DB_USER"):
        require(bool(os.getenv(name)), name + "_NOT_CONFIGURED")
    require(os.environ["DB_PORT"].isascii() and
            os.environ["DB_PORT"].isdigit(), "INVALID_DB_PORT")
    query = (
        "SELECT b.StoreKey,b.EmpresaId,HEX(e.Nombre) "
        "FROM PublicStoreBindings b "
        "JOIN Empresas e ON e.Id=b.EmpresaId "
        "WHERE b.Activa=1 AND e.Activa=1 "
        "AND b.Host='solqaryn-dev.vercel.app' AND b.StoreKey<>'' "
        "ORDER BY b.EmpresaId,b.StoreKey LIMIT 100"
    )
    env = dict(os.environ)
    env["MYSQL_PWD"] = env.pop("DB_PASSWORD")
    cmd = [
        "mysql", "--protocol=TCP", "-h", env["DB_HOST"],
        "-P", env["DB_PORT"], "-u", env["DB_USER"],
        "--ssl-mode=REQUIRED", env["DB_NAME"],
        "--batch", "--skip-column-names", "--raw", "-e", query,
    ]
    try:
        result = subprocess.run(
            cmd, env=env, check=True, capture_output=True, text=True,
            timeout=35,
        )
    except (OSError, subprocess.SubprocessError):
        raise GateError("DEV_BINDING_READBACK_UNAVAILABLE") from None
    return read_bindings(result.stdout)


def http_get(url, headers=None):
    request = Request(url, headers={
        "Cache-Control": "no-store",
        "User-Agent": "SOLQARYN-DEV-FinalIdentityGate/1.0",
        **(headers or {}),
    })
    try:
        with urlopen(request, timeout=30) as response:
            status, body = response.status, response.read(65536)
    except HTTPError as response:
        status, body = response.code, response.read(65536)
    except (URLError, TimeoutError, OSError):
        raise GateError("DEV_HTTP_TRANSPORT_UNAVAILABLE") from None
    try:
        data = json.loads(body)
    except (ValueError, UnicodeDecodeError):
        raise GateError("DEV_HTTP_NON_JSON_RESPONSE") from None
    require(isinstance(data, dict), "DEV_HTTP_NON_OBJECT_RESPONSE")
    return status, data


def main():
    bindings = get_bindings()
    for index, (store_key, company_id, company_name) in enumerate(bindings, 1):
        base = FRONTEND + "/api/tiendas/" + store_key + "/tienda/"
        status, payload = http_get(base + "binding")
        require(verified_binding(status, payload, company_id),
                "SIGNED_STORE_BINDING_NOT_VERIFIED")
        status, payload = http_get(base + "configuracion")
        require(verified_identity(status, payload, company_name),
                "SIGNED_STORE_IDENTITY_NOT_VERIFIED")
        print("DEV_SIGNED_TENANT_" + str(index) + "_IDENTITY=PASS")

    for label, url, headers in [
        ("DIRECT_UNSCOPED", BACKEND + "/empresa-configuracion/publica", None),
        ("PROXY_UNSCOPED", FRONTEND + "/api/empresa-configuracion/publica", None),
        ("FORGED_COMPANY_HEADER", BACKEND + "/empresa-configuracion/publica",
         {"X-Empresa-Id": "999999", "X-Forwarded-Host": "solqaryn-dev.vercel.app"}),
        ("UNSIGNED_BINDING", BACKEND + "/tienda/binding", None),
        ("FORGED_HMAC", BACKEND + "/tienda/configuracion",
         {"X-Solqaryn-Store-Proof": "forged"}),
    ]:
        status, payload = http_get(url, headers)
        require(isolated(status, payload), "FAIL_CLOSED_DENIAL_MISMATCH_" + label)
        print("DEV_" + label + "_503_TENANT_ISOLATION=PASS")

    unknown = "cert-unknown-" + uuid.uuid4().hex[:18]
    require(all(unknown != item[0] for item in bindings),
            "NONEXISTENT_STORE_FIXTURE_COLLISION")
    status, payload = http_get(
        FRONTEND + "/api/tiendas/" + unknown + "/tienda/binding"
    )
    require(isolated(status, payload), "UNKNOWN_STORE_NOT_REJECTED")
    print("DEV_UNKNOWN_STORE_503_TENANT_ISOLATION=PASS")
    print("DEV_TENANT_IDENTITY_AB_POSITIVE_NEGATIVE=PASS")
    print("DEV_TENANT_IDENTITY_NO_DB_WRITES=TRUE")


if __name__ == "__main__":
    try:
        main()
    except GateError as error:
        print("::error title=DEV tenant identity certification::" +
              str(error), file=sys.stderr)
        sys.exit(1)
