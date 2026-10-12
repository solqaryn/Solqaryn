#!/usr/bin/env python3
"""Audita metadatos Cloudinary persistidos en Aiven DEV; NUNCA escribe datos."""

from __future__ import annotations

from collections import Counter
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import subprocess
from urllib.parse import urlsplit

EXPECTED_CLOUD = "riyrzmob"
EXPECTED_DATABASE = "solqaryn_dev"
EXPECTED_HOST = "solqaryn-mysql-solqaryn.h.aivencloud.com"
EXPECTED_PORT = "14402"
EXPECTED_USER = "solqaryn_dev_user"
EXPECTED_PREFIX = "solqaryn_dev/"
# Carpetas de subida reales del runtime; no son aliases de aplicaciones antiguas.
SURFACES = {
    "productos": ("ProductoImagenes", "Url", "PublicId", "solqaryn/productos/"),
    "compras": ("CompraDocumentos", "Url", "PublicId", "solqaryn/compras/"),
    "perfiles": ("Usuarios", "FotoPerfilUrl", "FotoPerfilPublicId", "solqaryn/perfiles/"),
}
CLASSIFICATIONS = (
    "canonico",
    "dev_carpeta_no_canonica",
    "origen_ajeno_o_entorno_cruzado",
    "locator_incompleto_o_inconsistente",
    "sin_referencia",
)


def classify_locator(url: str, public_id: str, subfolder: str) -> str:
    """Solo lee cadenas. No se permite inferir que un asset ha sido migrado."""
    url = (url or "").strip()
    public_id = (public_id or "").strip()
    if not url and not public_id:
        return "sin_referencia"
    if not url or not public_id:
        return "locator_incompleto_o_inconsistente"
    try:
        parsed = urlsplit(url)
        host = parsed.hostname
        port = parsed.port
    except ValueError:
        return "locator_incompleto_o_inconsistente"
    if parsed.scheme != "https" or parsed.username or parsed.password or port not in (None, 443):
        return "locator_incompleto_o_inconsistente"
    if host != "res.cloudinary.com":
        return "origen_ajeno_o_entorno_cruzado"
    pieces = parsed.path.split("/")
    if len(pieces) < 6 or pieces[1] != EXPECTED_CLOUD or pieces[2] not in ("image", "raw") or pieces[3] != "upload":
        return "origen_ajeno_o_entorno_cruzado"
    if not public_id.startswith(EXPECTED_PREFIX):
        return "origen_ajeno_o_entorno_cruzado"
    # La URL registrada debe contener el PublicId exacto como ruta final.
    path_without_extension = parsed.path.rsplit(".", 1)[0]
    if not path_without_extension.endswith("/" + public_id):
        return "locator_incompleto_o_inconsistente"
    canonical_prefix = EXPECTED_PREFIX + subfolder
    if public_id.startswith(canonical_prefix):
        return "canonico"
    return "dev_carpeta_no_canonica"


def mysql_query(sql: str) -> list[str]:
    if os.environ.get("DB_HOST") != EXPECTED_HOST or os.environ.get("DB_PORT") != EXPECTED_PORT:
        raise RuntimeError("Aiven DEV: host o puerto incorrectos")
    if os.environ.get("DB_NAME") != EXPECTED_DATABASE or os.environ.get("DB_USER") != EXPECTED_USER:
        raise RuntimeError("Aiven DEV: base o usuario incorrectos")
    if not os.environ.get("DB_PASSWORD"):
        raise RuntimeError("Aiven DEV: credencial no configurada")
    args = [
        "mysql", "--protocol=TCP", "--connect-timeout=12", "--ssl-mode=REQUIRED",
        "--default-character-set=utf8mb4", "-h", EXPECTED_HOST, "-P", EXPECTED_PORT,
        "-u", EXPECTED_USER, "--batch", "--raw", "--skip-column-names",
        EXPECTED_DATABASE, "-e", sql,
    ]
    env = dict(os.environ, MYSQL_PWD=os.environ["DB_PASSWORD"])
    output = subprocess.check_output(args, env=env, text=True, stderr=subprocess.DEVNULL)
    return output.splitlines()


def main() -> int:
    # Nunca interpolar identificadores tomados de los datos ni de la solicitud.
    if os.environ.get("GITHUB_REF_NAME") != "dev":
        raise RuntimeError("Solo se permite auditar desde la rama DEV")
    rows = mysql_query("SELECT DATABASE(), SUBSTRING_INDEX(CURRENT_USER(),'@',1), VERSION();")
    if len(rows) != 1:
        raise RuntimeError("La identidad de Aiven DEV no pudo verificarse")
    identity = rows[0].split("\t")
    if len(identity) != 3 or identity[:2] != [EXPECTED_DATABASE, EXPECTED_USER] or not (
        identity[2] == "8.4.8" or identity[2].startswith("8.4.8-")
    ):
        raise RuntimeError("La identidad o version de Aiven no coincide con MySQL 8.4.8 aprobado")

    available = {
        tuple(line.split("\t", 1))
        for line in mysql_query(
            "SELECT TABLE_NAME,COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS "
            "WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME IN "
            "('ProductoImagenes','CompraDocumentos','Usuarios')"
        )
    }
    counts = {}
    total = Counter()
    for surface, (table, url_column, id_column, prefix) in SURFACES.items():
        for column in (url_column, id_column):
            if (table, column) not in available:
                raise RuntimeError("Esquema DEV no contiene columnas requeridas para el inventario")
        query = (
            f"SELECT COALESCE(\`{url_column}\`,''),COALESCE(\`{id_column}\`,'') "
            f"FROM \`{table}\`;"
        )
        counter = Counter()
        for line in mysql_query(query):
            fields = line.split("\t")
            if len(fields) != 2:
                raise RuntimeError("Locator DEV con formato de salida inesperado")
            kind = classify_locator(fields[0], fields[1], prefix)
            counter[kind] += 1
            total[kind] += 1
        counts[surface] = {"total_filas": sum(counter.values())}
        counts[surface].update({c: counter[c] for c in CLASSIFICATIONS})

    violations = sum(total[c] for c in CLASSIFICATIONS if c not in ("canonico", "sin_referencia"))
    report = {
        "audit": "CLOUDINARY_DEV_LOCATORS_READ_ONLY",
        "executed_at_utc": datetime.now(timezone.utc).isoformat(),
        "database": EXPECTED_DATABASE,
        "mysql_version": "8.4.8",
        "cloud": EXPECTED_CLOUD,
        "write_operations": 0,
        "qa_prod_touched": False,
        "contains_asset_urls_or_public_ids": False,
        "count_by_surface": counts,
        "remaining_noncanonical_or_invalid": violations,
        "normalization_certified": violations == 0,
        "audit_status": "PASS_READ_ONLY",
    }
    path = Path("evidence/cloudinary-dev-locator-audit.json")
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print("CLOUDINARY_DEV_LOCATOR_AUDIT=PASS_READ_ONLY")
    print(f"CLOUDINARY_DEV_REMAINING_NONCANONICAL_OR_INVALID={violations}")
    for name, surface_count in counts.items():
        print(f"CLOUDINARY_DEV_{name.upper()}_ROWCOUNT={surface_count['total_filas']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
