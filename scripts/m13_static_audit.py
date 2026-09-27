#!/usr/bin/env python3
"""Auditoría estática fail-closed de M13.

No imprime valores potencialmente sensibles: solo regla y ruta afectada.
La detección de deuda técnica informativa se reporta sin bloquear; secretos,
conflictos, binarios/temporales versionados y configuración de aislamiento sí bloquean.
"""
from __future__ import annotations

import json
import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def tracked_files() -> list[Path]:
    out = subprocess.check_output(["git", "ls-files", "-z"], cwd=ROOT)
    return [ROOT / p.decode() for p in out.split(b"\0") if p]


def is_text(path: Path) -> bool:
    try:
        data = path.read_bytes()[:4096]
    except OSError:
        return False
    return b"\0" not in data


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8", errors="replace")


files = tracked_files()
errors: list[dict[str, str]] = []
warnings: list[dict[str, str]] = []

forbidden_parts = {
    "node_modules", "bin", "obj", "dist", ".angular", "TestResults",
    "playwright-report", "test-results", "coverage", "tmp", "temp",
}
for path in files:
    rel = path.relative_to(ROOT)
    if any(part in forbidden_parts for part in rel.parts) or path.suffix.lower() in {".log", ".tmp", ".bak", ".swp", ".swo"}:
        errors.append({"rule": "tracked-temporary", "path": str(rel)})

secret_rules = [
    ("private-key", re.compile(r"-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----")),
    ("github-token", re.compile(r"\bgh[pousr]_[A-Za-z0-9]{30,}\b")),
    ("aws-access-key", re.compile(r"\bAKIA[0-9A-Z]{16}\b")),
    ("cloudinary-uri", re.compile(r"cloudinary://[^\s\"']+", re.I)),
]

# Workflows, fixtures y documentación contienen credenciales efímeras/ejemplos deliberados.
# El escaneo de firmas de secretos se concentra en código/configuración operativa.
secret_excluded_prefixes = (".github/", "docs/", "backend/tests/", "frontend/e2e/", "scripts/")
for path in files:
    rel = str(path.relative_to(ROOT)).replace("\\", "/")
    if rel.startswith(secret_excluded_prefixes) or not is_text(path):
        continue
    text = read(path)
    for rule, pattern in secret_rules:
        if pattern.search(text):
            errors.append({"rule": rule, "path": rel})

    # Literales tipo Password/ApiSecret se evalúan solo en archivos de configuración.
    # Así no se confunden campos UI type="password" ni metadata de package-lock con secretos.
    config_like = (
        path.name.startswith("appsettings")
        or path.name in {"render.yaml", "vercel.json"}
        or rel.startswith("frontend/src/environments/")
        or path.name.startswith(".env")
    )
    if config_like:
        literal_pattern = re.compile(
            r'(?i)(?:password|passwordsmtp|apisecret|jwt:secret)\s*[\"\']?\s*[:=]\s*[\"\']([^\"\']+)[\"\']'
        )
        for match in literal_pattern.finditer(text):
            value = match.group(1).strip()
            allowed = (
                not value
                or "CHANGE_ME" in value
                or "not-used" in value.lower()
                or "environment" in value.lower()
                or value.startswith("${")
            )
            if not allowed:
                errors.append({"rule": "password-literal", "path": rel})
                break

conflict = re.compile(r"^(<<<<<<<|=======|>>>>>>>)", re.M)
for path in files:
    rel = str(path.relative_to(ROOT)).replace("\\", "/")
    if rel.endswith(".md") or not is_text(path):
        continue
    if conflict.search(read(path)):
        errors.append({"rule": "merge-conflict-marker", "path": rel})

# Deuda técnica: inventario informativo; no se considera P0/P1 por sí solo.
todo_re = re.compile(r"\b(TODO|FIXME|HACK|XXX)\b", re.I)
for path in files:
    rel = str(path.relative_to(ROOT)).replace("\\", "/")
    if rel.startswith(("docs/", ".github/", "scripts/m13_static_audit.py")) or not is_text(path):
        continue
    count = len(todo_re.findall(read(path)))
    if count:
        warnings.append({"rule": f"technical-debt-markers:{count}", "path": rel})

# Contratos de aislamiento de DEV.
render = read(ROOT / "render.yaml")
required_render = [
    "branch: dev",
    "name: solqaryn-api-dev",
    "value: solqaryn_dev",
    "value: https://solqaryn-dev.vercel.app",
    "value: https://solqaryn-api-dev-fxx8.onrender.com",
]
for token in required_render:
    if token not in render:
        errors.append({"rule": "development-isolation", "path": "render.yaml"})

# La configuración versionada debe contener placeholders, nunca credenciales reales.
appsettings = json.loads(read(ROOT / "backend/src/API/appsettings.json"))
for section, key in [
    ("ConnectionStrings", "DefaultConnection"),
    ("Jwt", "Secret"),
    ("Cloudinary", "ApiSecret"),
    ("Smtp", "OAuth2RefreshToken"),
]:
    value = str(appsettings.get(section, {}).get(key, ""))
    if "CHANGE_ME" not in value:
        errors.append({"rule": "operational-config-must-use-placeholder", "path": "backend/src/API/appsettings.json"})

report = {
    "phase": "M13",
    "trackedFiles": len(files),
    "blockingFindings": errors,
    "informationalFindings": warnings,
    "result": "FAIL" if errors else "PASS",
}
(ROOT / "m13-static-audit.json").write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")

print(f"M13 static audit: {report['result']} | tracked={len(files)} | blocking={len(errors)} | info={len(warnings)}")
for item in errors:
    print(f"BLOCK {item['rule']} -> {item['path']}")
for item in warnings[:50]:
    print(f"INFO  {item['rule']} -> {item['path']}")
if len(warnings) > 50:
    print(f"INFO  ... {len(warnings) - 50} hallazgos informativos adicionales en artifact")

raise SystemExit(1 if errors else 0)
