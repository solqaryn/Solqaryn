#!/usr/bin/env python3
"""Fail-closed multi-tenant certification gate for SOLQARYN.

Validates the current tenant architecture rather than stale filename/token
heuristics. A dimension can be not applicable when no corresponding runtime
primitive exists, for example BackgroundService/IHostedService.
"""
from __future__ import annotations

import argparse
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def text(relative: str) -> str:
    path = ROOT / relative
    return path.read_text(encoding="utf-8", errors="replace") if path.exists() else ""


def cs_sources(folder: str):
    base = ROOT / folder
    if not base.exists():
        return []
    return [(path, path.read_text(encoding="utf-8", errors="replace"))
            for path in base.rglob("*.cs")]


def any_source_contains(folder: str, tokens: tuple[str, ...]) -> bool:
    return any(all(token in content for token in tokens)
               for _, content in cs_sources(folder))


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--require-certified", action="store_true")
    args = parser.parse_args()

    usuario_empresa = text("backend/src/Domain/Entities/UsuarioEmpresa.cs")
    scope = text("backend/src/Application/Interfaces/IUsuarioScopeService.cs")
    scope_impl = text("backend/src/Infrastructure/Services/UsuarioScopeService.cs")
    sucursal = text("backend/src/Application/Services/SucursalService.cs")
    sucursal_controller = text("backend/src/API/Controllers/SucursalesController.cs")
    sucursal_repository = text("backend/src/Infrastructure/Repositories/SucursalRepository.cs")
    permiso_filter = text("backend/src/API/Filters/RequierePermisoAttribute.cs")
    tenant_context = text("backend/src/API/Filters/TenantPermissionContext.cs")
    factura_share = text("backend/src/Application/Services/FacturaCompartirService.cs")
    factura_entity = text("backend/src/Domain/Entities/Factura.cs")

    hosted_sources = [
        (path, content)
        for path, content in cs_sources("backend/src")
        if ": BackgroundService" in content or "IHostedService" in content
    ]
    outbox_processors = [
        (path, content)
        for path, content in cs_sources("backend/src")
        if "class OutboxRetryProcessor" in content
    ]
    background_sources = hosted_sources + outbox_processors
    background_ok = None if not background_sources else all(
        "EmpresaId" in content and
        "ProcesarLoteAsync" in content and
        "ClaimDisponiblesAsync" in content
        for _, content in background_sources
    )

    checks = {
        "identity_company_binding": (
            all(token in usuario_empresa for token in ("UsuarioId", "EmpresaId", "RolId")) and
            "UsuarioEmpresas" in scope_impl
        ),
        "live_scope_company_binding": (
            "UsuarioTenantScopeActual" in scope and
            "EmpresaId" in scope and
            "UsuarioEmpresas" in scope_impl and
            "membresia.EmpresaId" in scope_impl
        ),
        "resource_company_authorization": (
            "empresaIdAutorizada" in sucursal and
            "ExigirCoincidenciaTenant" in sucursal and
            "ForbiddenAccessException" in sucursal and
            "GetAuthorizedEmpresaId" in sucursal_controller and
            "MarkAuthorizedEmpresaId" in permiso_filter and
            "AuthorizedEmpresaIdItemKey" in tenant_context
        ),
        "persistence_tenant_filter_or_explicit_scoping": (
            "s.EmpresaId == empresaId.Value" in sucursal_repository and
            "ResolverEmpresaIdConsulta" in sucursal and
            "PerteneceAlTenantAutorizado" in sucursal
        ),
        "reports_tenant_aware": any_source_contains(
            "backend/src/Application/Services", ("Reporte", "EmpresaId")),
        "files_tenant_aware": any_source_contains(
            "backend/src", ("CompraDocumento", "EmpresaId")),
        # Factura.Id is a global PK. The idempotency/cache key also includes the
        # authenticated user, normalized recipient and idempotency key.
        "cache_keys_tenant_aware": (
            "public int Id" in factura_entity and
            "facturaId" in factura_share and
            "_currentUser.UsuarioId" in factura_share and
            "claveIdempotencia" in factura_share and
            "CorreoLocks" in factura_share and
            "CorreosProcesados" in factura_share
        ),
        "background_process_tenant_aware": background_ok,
    }

    missing = [name for name, ok in checks.items() if ok is False]
    certified = not missing
    print("PRIORITY3_MULTITENANT_CERTIFICATION=" + ("PASS" if certified else "NOT_CERTIFIED"))
    for name, ok in checks.items():
        result = "NOT_APPLICABLE" if ok is None else "PASS" if ok else "MISSING"
        print(f"TENANT_CHECK {name}={result}")
    print(f"TENANT_BACKGROUND_RUNTIME_COUNT={len(background_sources)}")
    print(f"TENANT_BACKGROUND_HOSTED_WORKER_COUNT={len(hosted_sources)}")
    print(f"TENANT_BACKGROUND_OUTBOX_PROCESSOR_COUNT={len(outbox_processors)}")
    if missing:
        print("TENANT_MISSING=" + ",".join(missing))
        print("SECURITY_RULE=Tenant isolation must be proven by live authorization and scoping boundaries")

    if args.require_certified and not certified:
        return 2
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
