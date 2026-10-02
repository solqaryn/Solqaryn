#!/usr/bin/env python3
"""Validate N8.17.G matrix -> code -> test/evidence traceability without inflating lifecycle state."""
from __future__ import annotations

import glob
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
    raise SystemExit(f"N8.17.G TRACEABILITY FAIL: {message}")


manifest = load_json(GOV / "N8_17_A_BATCH_MANIFEST.json")
matrix_ids = [mid for batch in manifest["batches"] for mid in batch["matrix_ids"]]
if manifest.get("frozen_matrix_id_count") != 49 or len(matrix_ids) != 49 or len(set(matrix_ids)) != 49:
    fail("frozen manifest must contain exactly 49 unique MATRIX_ID values")
expected = set(matrix_ids)

# Canonical catalog: one root row per frozen id and a physical implementation root.
catalog_text = (GOV / "CATALOGO_MATRICES.md").read_text(encoding="utf-8")
row_re = re.compile(r"^\|\s*(VAEP-MX::[^|]+?)\s*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|", re.M)
rows = {}
for match in row_re.finditer(catalog_text):
    mid, domain, kind, impl = [part.strip() for part in match.groups()]
    if mid in rows:
        fail(f"duplicate catalog row for {mid}")
    rows[mid] = {"domain": domain, "kind": kind, "implementation_ref": impl}
if set(rows) != expected:
    fail(f"catalog mismatch missing={sorted(expected-set(rows))} extra={sorted(set(rows)-expected)}")

for mid, row in rows.items():
    refs = re.findall(r"`([^`]+)`", row["implementation_ref"])
    if not refs:
        fail(f"{mid} lacks IMPLEMENTATION_REF")
    for ref in refs:
        patterns = [part.strip() for part in ref.split(";") if part.strip()]
        if not patterns:
            fail(f"{mid} has empty IMPLEMENTATION_REF")
        for pattern in patterns:
            matches = list(ROOT.glob(pattern)) if any(ch in pattern for ch in "*?[") else [ROOT / pattern]
            if not any(path.exists() for path in matches):
                fail(f"{mid} implementation ref does not resolve: {pattern}")

# Root contracts B/C must map exactly 49/49.
b = load_json(GOV / "N8_17_B_DOMAIN_CONTRACTS.json")
b_ids = [entry.get("matrix_id") for entry in b.get("contracts", [])]
if len(b_ids) != 49 or set(b_ids) != expected or len(set(b_ids)) != 49:
    fail("N8.17.B contracts do not map exactly 49/49")
for entry in b["contracts"]:
    for key in ("objective", "actors", "preconditions", "invariants", "happy_path", "alternate_flows", "side_effects", "idempotency"):
        if not entry.get(key):
            fail(f"N8.17.B {entry['matrix_id']} missing {key}")

c = load_json(GOV / "N8_17_C_DATA_CONTRACTS.json")
c_ids = [entry.get("matrix_id") for entry in c.get("contracts", [])]
if len(c_ids) != 49 or set(c_ids) != expected or len(set(c_ids)) != 49:
    fail("N8.17.C contracts do not map exactly 49/49")
if c.get("unknowns"):
    fail(f"N8.17.C contains unresolved unknowns: {c['unknowns']}")

# D maps all roots through batches; E is deliberately shared-contract + catalog ownership.
d = load_json(GOV / "N8_17_D_BACKEND_CONTRACTS.json")
d_ids = [mid for batch in d.get("batches", []) for mid in batch.get("matrix_ids", [])]
if len(d_ids) != 49 or set(d_ids) != expected or len(set(d_ids)) != 49:
    fail("N8.17.D batches do not map exactly 49/49")
if d.get("unresolved"):
    fail(f"N8.17.D contains unresolved items: {d['unresolved']}")
if not all(d.get("review_assertions", {}).values()):
    fail("N8.17.D review assertions are not all true")

e = load_json(GOV / "N8_17_E_FRONTEND_CONTRACTS.json")
if e.get("expected_total") != 49 or e.get("unresolved"):
    fail("N8.17.E expected_total/unresolved invalid")
if not all(e.get("review_assertions", {}).values()):
    fail("N8.17.E review assertions are not all true")

# F must explicitly review every frozen root and close P0/P1/P2.
f_text = (GOV / "N8_17_F_SEC_AUDIT.md").read_text(encoding="utf-8")
missing_f = [mid for mid in matrix_ids if mid not in f_text]
if missing_f:
    fail(f"N8.17.F missing security rows: {missing_f}")
for marker in ("P0: `0`", "P1: `0`", "P2: `0`"):
    if marker not in f_text:
        fail(f"N8.17.F missing REVIEW_FIRST marker {marker}")

# Evidence chain B-F must already contain durable LISTO_REAL receipts.
for stage in "BCDEF":
    paths = sorted(RECEIPTS.glob(f"N8.17.{stage}_LISTO_REAL_*.json"))
    if not paths:
        fail(f"missing LISTO_REAL receipt for N8.17.{stage}")
    receipt = load_json(paths[-1])
    if receipt.get("result") != "LISTO_REAL" or receipt.get("parent") != f"N8.17.{stage}":
        fail(f"invalid latest receipt for N8.17.{stage}: {paths[-1].name}")

# No silent lifecycle inflation: G validates traceability; H owns SPEC_COMPLETE/CERTIFIED publication.
if "MATRIX_STATE = LEVANTAMIENTO_COMPLETO" not in catalog_text:
    fail("catalog baseline lifecycle state is not explicitly preserved before N8.17.H")

print("N8.17.G TRACEABILITY PASS")
print("frozen_matrix_ids=49 unique=49 catalog=49")
print("domain_contracts=49 data_contracts=49 backend_map=49 frontend_shared_contract=PASS security_rows=49")
print("implementation_refs=49/49 resolved")
print("receipts_B_to_F=5/5 LISTO_REAL")
print("silent_TBD_or_unknown_blockers=0")
print("lifecycle_inflation=0; promotion remains owned by N8.17.H")
