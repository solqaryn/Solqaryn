#!/usr/bin/env python3
"""Fail-closed static audit for Priority 4 quality invariants.

This gate intentionally distinguishes enforceable repository invariants from
future runtime certifications (full performance load test, external telemetry,
staging/UAT). It must never turn planned work into a false PASS.
"""
from __future__ import annotations

import json
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
errors: list[str] = []
warnings: list[str] = []
passes: list[str] = []


def ok(condition: bool, message: str) -> None:
    (passes if condition else errors).append(message)


def read(rel: str) -> str:
    path = ROOT / rel
    if not path.is_file():
        errors.append(f"MISSING_FILE:{rel}")
        return ""
    return path.read_text(encoding="utf-8")


# 29/31 - composition root and Clean Architecture project references.
expected_refs = {
    "backend/src/Domain/Solqaryn.Domain.csproj": set(),
    "backend/src/Application/Solqaryn.Application.csproj": {"Solqaryn.Domain.csproj"},
    "backend/src/Infrastructure/Solqaryn.Infrastructure.csproj": {"Solqaryn.Application.csproj"},
    "backend/src/API/Solqaryn.API.csproj": {"Solqaryn.Application.csproj", "Solqaryn.Infrastructure.csproj"},
}
for rel, expected in expected_refs.items():
    text = read(rel)
    if not text:
        continue
    root = ET.fromstring(text)
    actual = {Path(node.attrib["Include"].replace("\\", "/")).name for node in root.findall(".//ProjectReference")}
    ok(actual == expected, f"ARCH_REFS:{rel}:expected={sorted(expected)}:actual={sorted(actual)}")

manual_empresa = []
for path in (ROOT / "backend/src").rglob("*.cs"):
    if re.search(r"\bnew\s+EmpresaService\s*\(", path.read_text(encoding="utf-8", errors="ignore")):
        manual_empresa.append(str(path.relative_to(ROOT)))
ok(not manual_empresa, f"EMPRESA_SERVICE_RUNTIME_DI_ONLY:{manual_empresa or 'PASS'}")
program = read("backend/src/API/Program.cs")
ok("AddScoped<IEmpresaService, EmpresaService>()" in program, "EMPRESA_SERVICE_REGISTERED_IN_COMPOSITION_ROOT")

# 28 - stale-response guard must stay in Empresa UI.
empresa_ui = read("frontend/src/app/features/configuracion/empresa-administracion-card.component.ts")
ok("cargaRequestId" in empresa_ui and "requestId !== this.cargaRequestId" in empresa_ui,
   "EMPRESA_FILTER_STALE_RESPONSE_GUARD")
empresa_spec = read("frontend/src/app/features/configuracion/empresa-administracion-card.component.spec.ts")
ok("responses arrive out of order" in empresa_spec and "Respuesta Obsoleta" in empresa_spec,
   "EMPRESA_FILTER_OUT_OF_ORDER_REGRESSION_TEST")

# 30/31 - every lazy route must declare an authentication/permission intent,
# except the intentionally public storefront/login allowlist. For permisoGuard,
# Customer account shell is public for optional login/registration; customer PII and
# order data remain server-protected by the separate X-Storefront-Session boundary.
# modulo+accion are mandatory. The small balanced-brace extractor handles
# multiline route objects without trying to execute Angular source.
public_paths = {"", "login", "tienda", "tienda/productos", "tienda/ofertas", "tienda/producto/:slug", "tienda/categorias", "tienda/categoria/:slug", "tienda/carrito", "tienda/checkout", "tienda/cuenta", "tienda/pedido/:id"}
route_files = [ROOT / "frontend/src/app/app.routes.ts"] + sorted((ROOT / "frontend/src/app").rglob("*.routes.ts"))
route_findings: list[str] = []


def route_block(text: str, marker: int) -> str:
    depth = 0
    start = 0
    for i in range(marker - 1, -1, -1):
        c = text[i]
        if c == "}":
            depth += 1
        elif c == "{":
            if depth == 0:
                start = i
                break
            depth -= 1
    depth = 0
    end = len(text)
    for i in range(start, len(text)):
        c = text[i]
        if c == "{":
            depth += 1
        elif c == "}":
            depth -= 1
            if depth == 0:
                end = i + 1
                break
    return text[start:end]


seen_route_files = set()
for path in route_files:
    if path in seen_route_files or not path.is_file():
        continue
    seen_route_files.add(path)
    text = path.read_text(encoding="utf-8")
    for match in re.finditer(r"loadComponent\s*:", text):
        block = route_block(text, match.start())
        path_match = re.search(r"path\s*:\s*'([^']*)'", block)
        if not path_match:
            continue
        route_path = path_match.group(1)
        if route_path in public_paths or route_path == "**" or route_path == "":
            continue
        guarded = "authGuard" in block or "permisoGuard" in block
        if not guarded:
            route_findings.append(f"UNGUARDED:{path.relative_to(ROOT)}:{route_path}")
        if "permisoGuard" in block and not (re.search(r"modulo\s*:", block) and re.search(r"accion\s*:", block)):
            route_findings.append(f"PERMISSION_WITHOUT_CONTRACT:{path.relative_to(ROOT)}:{route_path}")
ok(not route_findings, f"FRONTEND_ROUTE_SECURITY_CONTRACT:{route_findings or 'PASS'}")

# 32 - disabled/focused tests are not allowed in committed test sources.
forbidden_test_patterns = {
    "JS_SKIP": re.compile(r"\b(?:test|it|describe)\.skip\s*\("),
    "JS_ONLY": re.compile(r"\b(?:test|it|describe)\.only\s*\("),
    "JASMINE_DISABLED": re.compile(r"\b(?:xit|xdescribe|fit|fdescribe)\s*\("),
    "XUNIT_SKIP": re.compile(r"\[(?:Fact|Theory)\s*\([^\]]*\bSkip\s*="),
}
violations: list[str] = []
test_paths = list((ROOT / "frontend").rglob("*.spec.ts")) + list((ROOT / "backend/tests").rglob("*.cs"))
for path in test_paths:
    text = path.read_text(encoding="utf-8", errors="ignore")
    for name, pattern in forbidden_test_patterns.items():
        if pattern.search(text):
            violations.append(f"{name}:{path.relative_to(ROOT)}")
ok(not violations, f"NO_COMMITTED_SKIPPED_OR_FOCUSED_TESTS:{violations or 'PASS'}")
playwright = read("frontend/playwright.config.ts")
ok("forbidOnly: Boolean(process.env['CI'])" in playwright or "forbidOnly: !!process.env.CI" in playwright or "forbidOnly:" in playwright,
   "PLAYWRIGHT_FORBID_ONLY_CONFIGURED")

# 33 - performance guardrails that can be proven statically today.
angular = json.loads(read("frontend/angular.json") or "{}")
budget_blob = json.dumps(angular)
ok(
    '"type": "initial"' in budget_blob
    and '"maximumWarning": "650kb"' in budget_blob
    and '"maximumError": "750kb"' in budget_blob,
    "ANGULAR_INITIAL_BUNDLE_BUDGET_650KB_WARNING_750KB_ERROR",
)
ok('"type": "anyComponentStyle"' in budget_blob and '"maximumError": "32kb"' in budget_blob,
   "ANGULAR_COMPONENT_STYLE_ERROR_BUDGET_32KB")
ok((ROOT / "backend/src/API/Filters/MedirRendimientoBusquedaFilter.cs").is_file(),
   "API_SEARCH_LATENCY_FILTER_PRESENT")
perf_cert = read("docs/N5.9_REPORT_PERFORMANCE_CERTIFICATION.md")
ok("No se afirma mejora porcentual" in perf_cert and "benchmark reproducible" in perf_cert,
   "PERFORMANCE_CERTIFICATION_DOES_NOT_INVENT_LATENCY")
if not (ROOT / "docs/PERFORMANCE_BENCHMARK_CONTRACT.md").is_file():
    warnings.append("PERFORMANCE_LOAD_BENCHMARK_CONTRACT_MISSING")

# 34 - production readiness primitives. External telemetry is a tracked gap,
# not something this repository may claim merely because logs exist.
ok("CorrelationIdMiddleware" in program, "CORRELATION_ID_MIDDLEWARE_PRESENT")
ok("MapHealthChecks" in program or 'MapGet("/health' in program, "HEALTH_ENDPOINT_PRESENT")
ok("UseHsts" in program, "HSTS_NON_DEVELOPMENT_PRESENT")
ok("UseHttpsRedirection" in program, "HTTPS_REDIRECTION_PRESENT")
appsettings = read("backend/src/API/appsettings.json")
for placeholder in ["CHANGE_ME_TO_A_LONG_RANDOM_SECRET_MIN_32_CHARS", '"CloudName": "CHANGE_ME"', '"ApiSecret": "CHANGE_ME"', '"OAuth2RefreshToken": "CHANGE_ME"']:
    ok(placeholder in appsettings, f"TRACKED_CONFIG_KEEPS_SECRET_PLACEHOLDER:{placeholder[:36]}")
ok((ROOT / "docs/PRODUCTION_READINESS_CONTRACT.md").is_file(), "PRODUCTION_READINESS_CONTRACT_PRESENT")
external_telemetry = any(
    token in "\n".join(p.read_text(encoding="utf-8", errors="ignore") for p in (ROOT / "backend/src").rglob("*.csproj"))
    for token in ("OpenTelemetry", "ApplicationInsights", "Sentry")
)
if not external_telemetry:
    warnings.append("EXTERNAL_TELEMETRY_NOT_CONFIGURED__N8.12_T9_REMAINS_NOT_CERTIFIED")

# 35 - current planning authority must be canonical and current-state only.
agents = read("AGENTS.md")
vaep = read("docs/VAEP_AUTHORITY.md")
master_id = "1YdQlNJ312HuziyKb9E-GEt55dcgSmuFGgfsHxyzPUaw"
ok(master_id in agents and master_id in vaep,
   "CURRENT_PLAN_MAESTRO_AUTHORITY_PINNED")
ok("CURRENT_MASTER_ONLY=TRUE" in vaep and "PREVIOUS_PLAN_DEPENDENCY=PROHIBITED" in vaep,
   "RETIRED_PLAN_DEPENDENCIES_BLOCKED")
ok("CONTEXT_MODE=CURRENT_STATE_ONLY" in agents and "CONTEXT_MODE=CURRENT_STATE_ONLY" in vaep,
   "CURRENT_STATE_ONLY_PLANNING")

print("PRIORITY4_QUALITY_AUDIT")
for item in passes:
    print(f"PASS {item}")
for item in warnings:
    print(f"WARN {item}")
for item in errors:
    print(f"FAIL {item}")
print(json.dumps({"pass": len(passes), "warn": len(warnings), "fail": len(errors)}, separators=(",", ":")))
sys.exit(1 if errors else 0)
