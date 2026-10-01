#!/usr/bin/env python3
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
GOV = ROOT / "docs/matrices-evaluacion/00_GOBERNANZA"
BOUNDARIES = GOV / "N8_18_B_DOMAIN_BOUNDARIES.json"
BACKEND = GOV / "N8_17_D_BACKEND_CONTRACTS.json"
REFACTOR = GOV / "N8_18_D_BACKEND_REFACTOR.json"


def load(path: Path):
    with path.open(encoding="utf-8") as fh:
        return json.load(fh)


def fail(message):
    raise SystemExit(f"N8.18.D FAIL: {message}")


b = load(BOUNDARIES)
n817d = load(BACKEND)
d = load(REFACTOR)

if d.get("parent") != "N8.18.D" or d.get("stage") != "BACKEND_API":
    fail("invalid parent/stage")
if d.get("branch") != "dev":
    fail("branch is not dev")
if d.get("unresolved"):
    fail("unresolved backend refactor findings remain")
if d.get("domain_count") != b.get("domain_count") or d.get("domain_count") != 9:
    fail("canonical domain count drift")
if d.get("contract_root_count") != b.get("contract_root_count") or d.get("contract_root_count") != 49:
    fail("49-root contract coverage drift")
if n817d.get("expected_total") != 49 or n817d.get("unresolved"):
    fail("N8.17.D backend contract is not fully reconciled")

arch03 = d.get("arch_03_disposition", {})
if arch03.get("classification") != "CONSOLIDATE_WITH_PROOF_ONLY":
    fail("ARCH-03 is not fail-closed")
if arch03.get("physical_relocation_required") is not False:
    fail("speculative physical relocation was claimed")

expected_paths = {
    "backend/src/Application/Bancos",
    "backend/src/Application/Services",
    "backend/src/Application/DTOs",
    "backend/src/Application/Interfaces",
}
entries = arch03.get("paths", [])
actual_paths = {item.get("path") for item in entries}
if actual_paths != expected_paths:
    fail("ARCH-03 path inventory differs from the N8.18.B scope")

allowed_dispositions = {"DOMAIN_SLICE_KEEP", "KEEP_PENDING_PER_FILE_MOVE_PROOF"}
for item in entries:
    if item.get("disposition") not in allowed_dispositions:
        fail(f"unsafe disposition for {item.get('path')}")
    path = ROOT / item["path"]
    if not path.exists():
        fail(f"required backend path missing: {item['path']}")

bancos = next(item for item in entries if item.get("path") == "backend/src/Application/Bancos")
if bancos.get("owner_domain") != "CASH_BANKS" or bancos.get("disposition") != "DOMAIN_SLICE_KEEP":
    fail("Bancos use-case slice ownership is not explicit")
required_files = set(bancos.get("required_files", []))
expected_files = {
    "BancosIdempotencyKey.cs",
    "BancosOperationPolicy.cs",
    "CuentaBancariaPage.cs",
    "CuentaBancariaQueryFilter.cs",
    "OperacionBancariaService.cs",
}
if required_files != expected_files:
    fail("Bancos required-file inventory drift")
for name in expected_files:
    if not (ROOT / "backend/src/Application/Bancos" / name).exists():
        fail(f"Bancos domain file missing: {name}")

banking = d.get("banking_use_case", {})
for key in ("application_service", "service_interface", "idempotency_value_object", "operation_policy"):
    relative = banking.get(key)
    if not relative or not (ROOT / relative).exists():
        fail(f"banking evidence missing: {key}")
if banking.get("duplicate_application_service_demonstrated") is not False:
    fail("unsafe duplicate-service consolidation claim")
if banking.get("duplicate_endpoint_demonstrated") is not False:
    fail("unsafe duplicate-endpoint consolidation claim")
if banking.get("authority") != "SERVER_SIDE":
    fail("backend authority was not preserved")
if banking.get("api_compatibility_delta") != "NONE":
    fail("unexpected API compatibility delta")
for key in ("tenant_and_permission_authority_preserved", "idempotency_preserved", "audit_trace_preserved"):
    if banking.get(key) is not True:
        fail(f"banking invariant not preserved: {key}")

shared = n817d.get("shared_backend_contract", {})
for key in ("backend_authority", "permissions_contract", "idempotency_contract", "error_contract", "trace_contract", "audit_contract"):
    if not shared.get(key):
        fail(f"N8.17.D shared backend contract missing {key}")
assertions = n817d.get("review_assertions", {})
if not assertions or any(value is not True for value in assertions.values()):
    fail("N8.17.D backend assertions are not all true")

for forbidden in d.get("forbidden_changes", []):
    if not forbidden:
        fail("blank forbidden-change rule")
acceptance = d.get("acceptance", {})
required_true = {
    "all_49_backend_contracts_preserved",
    "banking_domain_slice_explicit",
    "banking_interface_contract_preserved",
    "banking_idempotency_contract_preserved",
    "demonstrated_duplicate_services_consolidated_or_none",
    "demonstrated_duplicate_endpoints_consolidated_or_none",
}
if any(acceptance.get(key) is not True for key in required_true):
    fail("one or more N8.18.D acceptance assertions are false")
for key in ("speculative_relocations", "speculative_deletions", "api_compatibility_breaks", "security_bypasses", "destructive_db_changes"):
    if acceptance.get(key) != 0:
        fail(f"non-zero prohibited delta: {key}")

service_text = (ROOT / banking["application_service"]).read_text(encoding="utf-8")
interface_text = (ROOT / banking["service_interface"]).read_text(encoding="utf-8")
idempotency_text = (ROOT / banking["idempotency_value_object"]).read_text(encoding="utf-8")
if "namespace Solqaryn.Application.Bancos;" not in service_text:
    fail("banking service is not in the Bancos application namespace")
if ": IOperacionBancariaService" not in service_text:
    fail("banking service no longer implements its interface")
if "namespace Solqaryn.Application.Interfaces;" not in interface_text:
    fail("banking interface boundary drift")
if "MaxLength = 100" not in idempotency_text or "string.IsNullOrWhiteSpace" not in idempotency_text:
    fail("banking idempotency guard drift")

print("N8.18.D PASS: 49/49 backend contracts preserved; Bancos domain slice explicit; no demonstrated duplicate service/endpoint; speculative moves=0; API/security authority preserved")
