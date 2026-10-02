#!/usr/bin/env python3
"""Validate N8.17.H DOC_CERT without lifecycle inflation."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
GOV = ROOT / "docs/matrices-evaluacion/00_GOBERNANZA"
RECEIPTS = ROOT / "vaep/evidence/receipts"


def load_json(path: Path):
    with path.open(encoding="utf-8") as fh:
        return json.load(fh)


def fail(message: str) -> None:
    raise SystemExit(f"N8.17.H CERTIFICATION FAIL: {message}")


manifest = load_json(GOV / "N8_17_A_BATCH_MANIFEST.json")
ids = [mid for batch in manifest.get("batches", []) for mid in batch.get("matrix_ids", [])]
expected = set(ids)
if manifest.get("frozen_matrix_id_count") != 49 or len(ids) != 49 or len(expected) != 49:
    fail("manifest must contain exactly 49 unique frozen MATRIX_ID values")

catalog = (GOV / "CATALOGO_MATRICES.md").read_text(encoding="utf-8")
catalog_ids = re.findall(r"^\|\s*(VAEP-MX::[^|]+?)\s*\|", catalog, re.M)
if len(catalog_ids) != 49 or set(catalog_ids) != expected or len(set(catalog_ids)) != 49:
    fail("canonical catalog must map exactly the 49 frozen MATRIX_ID values")

cert = load_json(GOV / "N8_17_H_CERTIFICATION.json")
if cert.get("parent") != "N8.17.H" or cert.get("branch") != "dev":
    fail("certification parent/branch mismatch")
if cert.get("expected_total") != 49 or cert.get("spec_complete_count") != 49:
    fail("all 49 frozen roots must be published at least SPEC_COMPLETE")
if cert.get("spec_complete_scope") != "ALL_FROZEN_MATRIX_IDS_FROM_MANIFEST":
    fail("SPEC_COMPLETE scope must bind to the frozen manifest")
if cert.get("missing_matrix_ids") or cert.get("unjustified_tbd") or cert.get("unresolved"):
    fail("missing/TBD/unresolved certification gaps remain")
certified = cert.get("certified_matrix_ids", [])
if cert.get("certified_count") != len(certified):
    fail("certified_count does not match certified_matrix_ids")
if not set(certified).issubset(expected):
    fail("CERTIFIED list contains unknown MATRIX_ID values")
policy = cert.get("state_policy", {})
if policy.get("published_minimum") != "SPEC_COMPLETE":
    fail("published minimum is not SPEC_COMPLETE")
if policy.get("certified_requires_feature_specific_implementation_and_test_proof") is not True:
    fail("CERTIFIED proof policy is not fail-closed")
if policy.get("universal_certified_claim") is not False:
    fail("universal CERTIFIED claim would inflate lifecycle state")
if policy.get("historical_catalog_state_is_not_rewritten") is not True:
    fail("historical catalog state must remain immutable evidence")

required_artifacts = cert.get("artifacts", {})
for key in ("A_manifest", "B_domain", "C_data", "D_backend", "E_frontend", "F_security", "G_test_ci", "catalog"):
    rel = required_artifacts.get(key)
    if not rel or not (ROOT / rel).exists():
        fail(f"missing required artifact {key}: {rel}")

b = load_json(GOV / "N8_17_B_DOMAIN_CONTRACTS.json")
b_ids = [entry.get("matrix_id") for entry in b.get("contracts", [])]
if len(b_ids) != 49 or set(b_ids) != expected or len(set(b_ids)) != 49:
    fail("N8.17.B is not exact 49/49")

c = load_json(GOV / "N8_17_C_DATA_CONTRACTS.json")
c_ids = [entry.get("matrix_id") for entry in c.get("contracts", [])]
if len(c_ids) != 49 or set(c_ids) != expected or len(set(c_ids)) != 49 or c.get("unknowns"):
    fail("N8.17.C is not exact 49/49 with unknowns=[]")

d = load_json(GOV / "N8_17_D_BACKEND_CONTRACTS.json")
d_ids = [mid for batch in d.get("batches", []) for mid in batch.get("matrix_ids", [])]
if len(d_ids) != 49 or set(d_ids) != expected or len(set(d_ids)) != 49 or d.get("unresolved"):
    fail("N8.17.D is not exact 49/49 with unresolved=[]")
if not all(d.get("review_assertions", {}).values()):
    fail("N8.17.D review assertions are not all true")

e = load_json(GOV / "N8_17_E_FRONTEND_CONTRACTS.json")
if e.get("expected_total") != 49 or e.get("unresolved"):
    fail("N8.17.E total/unresolved mismatch")
if not all(e.get("review_assertions", {}).values()):
    fail("N8.17.E review assertions are not all true")

f_text = (GOV / "N8_17_F_SEC_AUDIT.md").read_text(encoding="utf-8")
for mid in ids:
    if mid not in f_text:
        fail(f"N8.17.F missing security disposition for {mid}")
for marker in ("P0: `0`", "P1: `0`", "P2: `0`"):
    if marker not in f_text:
        fail(f"N8.17.F missing {marker}")

g_text = (GOV / "N8_17_G_TEST_CI_VALIDATION.md").read_text(encoding="utf-8")
if "49" not in g_text or not (ROOT / "scripts/validate_n8_17_g_traceability.py").exists():
    fail("N8.17.G traceability evidence/validator missing")

# B-G are the contract publication chain whose durable closure H consumes.
for stage in "BCDEFG":
    receipts = sorted(RECEIPTS.glob(f"N8.17.{stage}_LISTO_REAL_*.json"))
    if not receipts:
        fail(f"missing durable LISTO_REAL receipt for N8.17.{stage}")
    receipt = load_json(receipts[-1])
    if receipt.get("parent") != f"N8.17.{stage}" or receipt.get("result") != "LISTO_REAL":
        fail(f"invalid latest receipt for N8.17.{stage}: {receipts[-1].name}")

# H must not mutate historical evidence in-place. Catalog baseline remains explicit.
if "MATRIX_STATE = LEVANTAMIENTO_COMPLETO" not in catalog:
    fail("historical catalog baseline marker disappeared")

print("N8.17.H CERTIFICATION PASS")
print("frozen_matrix_ids=49 catalog_roots=49")
print("contracts_B_to_G=complete receipts_B_to_G=6/6_LISTO_REAL")
print("spec_complete=49 certified=%d" % len(certified))
print("missing=0 unjustified_tbd=0 unresolved=0")
print("state_inflation=0 historical_catalog_rewrite=0")
