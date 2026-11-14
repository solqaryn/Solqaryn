#!/usr/bin/env python3
"""Valida logs de ejecución sin imprimir secretos.

Mantiene compatibilidad con los nombres históricos de Fase 8 y permite que
fases de certificación posteriores reutilicen exactamente las mismas reglas
sobre sus logs versionados.
"""

from __future__ import annotations

import json
import os
import re
import sys
from pathlib import Path


def resolve_log_path(primary: str, fallback: str) -> Path:
    primary_path = Path(primary)
    if primary_path.exists():
        return primary_path
    fallback_path = Path(fallback)
    if fallback_path.exists():
        return fallback_path
    return primary_path


LOG_PATHS = [
    resolve_log_path("backend/fase8-api.log", "backend/m13-api.log"),
    resolve_log_path("frontend/fase8-frontend.log", "frontend/m13-frontend.log"),
    resolve_log_path("fase8-smtp.log", "m13-smtp.log"),
]

FATAL_PATTERNS = {
    "error_no_controlado": re.compile(r"Error no controlado|Unhandled exception", re.IGNORECASE),
    "excepcion_fatal": re.compile(r"\b(FATAL|CRITICAL)\b", re.IGNORECASE),
    # Los diagnósticos Angular NGxxxx también se emiten para WARNING (p. ej. NG8113).
    # Solo son fatales cuando la misma línea está marcada explícitamente como ERROR.
    "error_angular": re.compile(r"(?:\bERROR\b|✘\s*\[ERROR\]).*\bNG\d{4}:|ERROR in ", re.IGNORECASE),
    "fallo_proceso": re.compile(r"Application startup exception|Process terminated", re.IGNORECASE),
}

WARNING_PATTERNS = {
    "warning": re.compile(r"\bwarn(?:ing)?\b", re.IGNORECASE),
    "deprecated": re.compile(r"\bdeprecated\b", re.IGNORECASE),
    "failed": re.compile(r"\bfailed\b", re.IGNORECASE),
}

# Valores de prueba conocidos. Nunca se imprimen; solo se informa el archivo y tipo.
FORBIDDEN_VALUES = [
    value
    for value in [
        os.getenv("PHASE8_ADMIN_PASSWORD"),
        os.getenv("PHASE8_JWT_SECRET"),
        os.getenv("PHASE8_SMTP_PASSWORD"),
        os.getenv("PHASE8_DATABASE_PASSWORD"),
    ]
    if value and len(value) >= 6
]


def read_logs() -> dict[str, str]:
    logs: dict[str, str] = {}
    missing: list[str] = []
    for path in LOG_PATHS:
        if not path.exists():
            missing.append(str(path))
            continue
        logs[str(path)] = path.read_text(encoding="utf-8", errors="replace")
    if missing:
        raise SystemExit(f"Faltan logs obligatorios: {', '.join(missing)}")
    return logs


def main() -> int:
    logs = read_logs()
    fatal_findings: list[dict[str, str]] = []
    secret_findings: list[dict[str, str]] = []
    warning_counts: dict[str, dict[str, int]] = {}

    for path, content in logs.items():
        warning_counts[path] = {
            name: len(pattern.findall(content))
            for name, pattern in WARNING_PATTERNS.items()
        }

        for name, pattern in FATAL_PATTERNS.items():
            if pattern.search(content):
                fatal_findings.append({"archivo": path, "tipo": name})

        for index, value in enumerate(FORBIDDEN_VALUES, start=1):
            if value in content:
                secret_findings.append({"archivo": path, "tipo": f"valor_prohibido_{index}"})

    report = {
        "archivos": {
            path: {
                "bytes": len(content.encode("utf-8")),
                "lineas": content.count("\n") + 1,
                "advertencias": warning_counts[path],
            }
            for path, content in logs.items()
        },
        "hallazgosFatales": fatal_findings,
        "hallazgosSecretos": secret_findings,
        "resultado": "aprobado" if not fatal_findings and not secret_findings else "rechazado",
    }

    output = Path("fase8-runtime-report.json")
    output.write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")

    print(f"Auditoría de runtime: {report['resultado']}.")
    print(f"Reporte: {output}")
    if fatal_findings:
        print("Se detectaron patrones fatales en los logs.", file=sys.stderr)
    if secret_findings:
        print("Se detectaron valores sensibles en los logs.", file=sys.stderr)

    return 1 if fatal_findings or secret_findings else 0


if __name__ == "__main__":
    raise SystemExit(main())
