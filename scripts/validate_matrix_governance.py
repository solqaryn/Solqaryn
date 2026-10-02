#!/usr/bin/env python3
"""Validate N8.16/N8.17 matrix governance without depending on row order."""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MATRIX_ROOT = ROOT / "docs" / "matrices-evaluacion"
GOV = MATRIX_ROOT / "00_GOBERNANZA"
CATALOG = GOV / "CATALOGO_MATRICES.md"
TEMPLATE = GOV / "PLANTILLA_MATRIZ_UI.md"
BATCH_MANIFEST = GOV / "N8_17_A_BATCH_MANIFEST.json"
DOMAIN_CONTRACTS = GOV / "N8_17_B_DOMAIN_CONTRACTS.json"

ID_RE = re.compile(r"^VAEP-MX::[A-Z0-9_]+::[A-Z0-9_]+$")
ROW_ID_RE = re.compile(r"^(?:ROW|MATRIX|MX)[-_]?\d+$", re.IGNORECASE)
PLACEHOLDER_RE = re.compile(
    r"(?i)(?<![A-Z0-9_])(?:TBD|TODO|POR\s+DEFINIR|PENDIENTE\s+DE\s+DEFINIR)(?![A-Z0-9_])"
)
ALLOWED_MATRIX_STATES = {
    "BASELINE_CREATED",
    "LEVANTAMIENTO_COMPLETO",
    "SPEC_COMPLETE",
    "IMPLEMENTATION_REVIEWED",
    "CERTIFIED",
}

REQUIRED_TEMPLATE_TOKENS = {
    "identity": [
        "MATRIX_ID:", "MATRIX_CHANGE_ID:", "MATRIX_VERSION:",
        "PARENT_MATRIX_ID:", "CONTRACT_KIND:", "MATRIX_STATE:"
    ],
    "ownership": ["CONTRACT_OWNER:", "DATA_OWNER:", "DEPENDS_ON_MATRIX_IDS:"],
    "data": [
        "DATA_ENTITIES:", "DB_TABLES:", "DB_FIELDS:", "MIGRATION_REFS:",
        "FK_CONSTRAINTS:", "INDEX_REFS:", "TRANSACTION_BOUNDARY:", "INTEGRITY_RULES:"
    ],
    "backend": [
        "API_ROUTE:", "HTTP_METHOD:", "CONTROLLER_ACTION:", "REQUEST_DTO:",
        "APPLICATION_USE_CASE:", "REPOSITORY:", "BACKGROUND_JOB:",
        "INTEGRATION_PROVIDER:", "CONFIG_KEYS:"
    ],
    "frontend": [
        "PRIMARY_ROUTE_OR_SURFACE:", "COMPONENT_REFS:", "FORM_REFS:",
        "DIALOG_REFS:", "WIDGET_REFS:", "STATE_MODEL:", "INTERACTIONS:",
        "ACCESSIBILITY_CONTRACT:"
    ],
    "security": [
        "AUTHN_REQUIRED:", "AUTHZ_POLICY_OR_PERMISSION:", "RBAC_MODULE_ACTION:",
        "TENANT_SCOPE:", "AUDIT_EVENTS:", "PII_CLASSIFICATION:", "LOG_REDACTION:",
        "RATE_LIMIT_POLICY:", "OBSERVABILITY_SIGNALS:"
    ],
    "evidence": ["CI_RUN_REFS:", "RECEIPT_REF:", "REVIEW_FIRST:"],
}
REQUIRED_MATERIAL_TOKENS = tuple(
    token for tokens in REQUIRED_TEMPLATE_TOKENS.values() for token in tokens
)
REQUIRED_DOMAIN_ARRAYS = (
    "actors", "preconditions", "invariants", "happy_path", "alternate_flows", "side_effects"
)
REQUIRED_DOMAIN_STRINGS = ("objective", "idempotency")


def fail(errors: list[str]) -> int:
    for error in errors:
        print(f"ERROR: {error}", file=sys.stderr)
    return 1


def parse_catalog(text: str) -> tuple[list[dict[str, str]], list[str]]:
    rows: list[dict[str, str]] = []
    errors: list[str] = []
    for line_no, line in enumerate(text.splitlines(), start=1):
        if not line.startswith("| VAEP-MX::"):
            continue
        cells = [cell.strip().strip("`") for cell in line.strip().strip("|").split("|")]
        if len(cells) != 7:
            errors.append(f"catalog line {line_no}: expected 7 columns, found {len(cells)}")
            continue
        matrix_id, domain, kind, implementation, parent, status, classification = cells
        rows.append({
            "id": matrix_id,
            "domain": domain,
            "kind": kind,
            "implementation": implementation,
            "parent": parent,
            "status": status,
            "classification": classification,
            "line": str(line_no),
        })
    return rows, errors


def extract_material_field(text: str, token: str) -> str | None:
    match = re.search(
        rf"^[ \t]*-[ \t]*{re.escape(token)}[ \t]*(.*?)[ \t]*$",
        text,
        flags=re.MULTILINE,
    )
    if match is None:
        return None
    return match.group(1).strip().strip("`").strip()


def validate_material_matrix_text(text: str, label: str) -> list[str]:
    errors: list[str] = []
    for token in REQUIRED_MATERIAL_TOKENS:
        value = extract_material_field(text, token)
        if value is None:
            errors.append(f"{label}: missing required field {token}")
            continue
        if not value:
            errors.append(f"{label}: blank required field {token}; use N/A:<reason> when not applicable")
            continue
        if PLACEHOLDER_RE.search(value):
            errors.append(f"{label}: unjustified placeholder in {token}: {value!r}")
    matrix_id = extract_material_field(text, "MATRIX_ID:")
    if matrix_id and not ID_RE.fullmatch(matrix_id):
        errors.append(f"{label}: invalid stable MATRIX_ID {matrix_id!r}")
    matrix_state = extract_material_field(text, "MATRIX_STATE:")
    if matrix_state and not matrix_state.startswith("N/A:") and matrix_state not in ALLOWED_MATRIX_STATES:
        errors.append(f"{label}: invalid MATRIX_STATE {matrix_state!r}")
    return errors


def self_test_negative_contracts() -> list[str]:
    errors: list[str] = []
    lines = []
    for token in REQUIRED_MATERIAL_TOKENS:
        if token == "MATRIX_ID:":
            value = "VAEP-MX::SELF_TEST::VALID"
        elif token == "MATRIX_STATE:":
            value = "LEVANTAMIENTO_COMPLETO"
        else:
            value = "N/A:self-test"
        lines.append(f"- {token} {value}")
    valid = "\n".join(lines) + "\n"
    if validate_material_matrix_text(valid, "self-test valid fixture"):
        errors.append("self-test valid fixture rejected")
    missing = valid.replace("- CI_RUN_REFS: N/A:self-test\n", "")
    if not any("missing required field CI_RUN_REFS:" in e for e in validate_material_matrix_text(missing, "self-test missing")):
        errors.append("self-test missing-field fixture did not trigger rejection")
    blank = valid.replace("- REQUEST_DTO: N/A:self-test", "- REQUEST_DTO:")
    if not any("blank required field REQUEST_DTO:" in e for e in validate_material_matrix_text(blank, "self-test blank")):
        errors.append("self-test blank-field fixture did not trigger rejection")
    placeholder = valid.replace("- API_ROUTE: N/A:self-test", "- API_ROUTE: TBD")
    if not any("unjustified placeholder" in e for e in validate_material_matrix_text(placeholder, "self-test placeholder")):
        errors.append("self-test placeholder fixture did not trigger rejection")
    invalid_id = valid.replace("VAEP-MX::SELF_TEST::VALID", "ROW-17")
    if not any("invalid stable MATRIX_ID" in e for e in validate_material_matrix_text(invalid_id, "self-test invalid-id")):
        errors.append("self-test invalid-id fixture did not trigger rejection")
    invalid_state = valid.replace("- MATRIX_STATE: LEVANTAMIENTO_COMPLETO", "- MATRIX_STATE: MATERIAL")
    if not any("invalid MATRIX_STATE" in e for e in validate_material_matrix_text(invalid_state, "self-test invalid-state")):
        errors.append("self-test invalid-state fixture did not trigger rejection")
    return errors


def validate_batch_manifest(rows: list[dict[str, str]]) -> list[str]:
    errors: list[str] = []
    if not BATCH_MANIFEST.is_file():
        return errors
    try:
        manifest = json.loads(BATCH_MANIFEST.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        return [f"invalid N8.17.A batch manifest JSON: {exc}"]
    batches = manifest.get("batches")
    if not isinstance(batches, list) or not batches:
        return ["N8.17.A batch manifest must contain a non-empty batches array"]
    catalog_ids = [row["id"] for row in rows]
    by_id = {row["id"]: row for row in rows}
    manifest_ids: list[str] = []
    batch_names: list[str] = []
    for index, batch in enumerate(batches, start=1):
        if not isinstance(batch, dict):
            errors.append(f"batch {index}: expected object")
            continue
        name = batch.get("batch")
        ids = batch.get("matrix_ids")
        declared_count = batch.get("count")
        if not isinstance(name, str) or not name:
            errors.append(f"batch {index}: missing batch name")
            continue
        if name in batch_names:
            errors.append(f"duplicate batch name: {name}")
        batch_names.append(name)
        if not isinstance(ids, list) or not all(isinstance(x, str) for x in ids):
            errors.append(f"batch {name}: matrix_ids must be a string array")
            continue
        if declared_count != len(ids):
            errors.append(f"batch {name}: declared count {declared_count!r} != actual {len(ids)}")
        if len(ids) != len(set(ids)):
            errors.append(f"batch {name}: duplicate MATRIX_ID inside batch")
        for matrix_id in ids:
            if matrix_id not in by_id:
                errors.append(f"batch {name}: unknown MATRIX_ID {matrix_id}")
            elif by_id[matrix_id]["domain"] != name:
                errors.append(f"batch {name}: MATRIX_ID {matrix_id} belongs to {by_id[matrix_id]['domain']}")
        manifest_ids.extend(ids)
    duplicates = sorted({x for x in manifest_ids if manifest_ids.count(x) > 1})
    if duplicates:
        errors.append(f"N8.17.A manifest duplicate MATRIX_ID values: {duplicates}")
    missing = sorted(set(catalog_ids) - set(manifest_ids))
    extra = sorted(set(manifest_ids) - set(catalog_ids))
    if missing:
        errors.append(f"N8.17.A manifest missing catalog MATRIX_ID values: {missing}")
    if extra:
        errors.append(f"N8.17.A manifest contains extra MATRIX_ID values: {extra}")
    if manifest.get("expected_total") != len(catalog_ids):
        errors.append(f"N8.17.A expected_total {manifest.get('expected_total')!r} != catalog count {len(catalog_ids)}")
    if manifest.get("frozen_matrix_id_count") != len(catalog_ids):
        errors.append(f"N8.17.A frozen_matrix_id_count {manifest.get('frozen_matrix_id_count')!r} != catalog count {len(catalog_ids)}")
    if manifest.get("expected_batch_count") != len(batches):
        errors.append(f"N8.17.A expected_batch_count {manifest.get('expected_batch_count')!r} != actual {len(batches)}")
    if len(manifest_ids) != len(catalog_ids):
        errors.append(f"N8.17.A manifest occurrences {len(manifest_ids)} != catalog count {len(catalog_ids)}")
    return errors


def validate_domain_contracts(rows: list[dict[str, str]]) -> list[str]:
    """N8.17.B: every frozen root must have a complete explicit business contract."""
    errors: list[str] = []
    if not DOMAIN_CONTRACTS.is_file():
        return errors
    try:
        doc = json.loads(DOMAIN_CONTRACTS.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        return [f"invalid N8.17.B domain contracts JSON: {exc}"]
    contracts = doc.get("contracts")
    if doc.get("parent") != "N8.17.B":
        errors.append(f"N8.17.B domain contracts parent must be N8.17.B, got {doc.get('parent')!r}")
    if not isinstance(contracts, list):
        return errors + ["N8.17.B domain contracts must contain a contracts array"]
    catalog_ids = [row["id"] for row in rows]
    by_id = {row["id"]: row for row in rows}
    ids: list[str] = []
    for index, contract in enumerate(contracts, start=1):
        if not isinstance(contract, dict):
            errors.append(f"N8.17.B contract {index}: expected object")
            continue
        matrix_id = contract.get("matrix_id")
        domain = contract.get("domain")
        if not isinstance(matrix_id, str) or not matrix_id:
            errors.append(f"N8.17.B contract {index}: missing matrix_id")
            continue
        ids.append(matrix_id)
        if matrix_id not in by_id:
            errors.append(f"N8.17.B unknown MATRIX_ID {matrix_id}")
        elif domain != by_id[matrix_id]["domain"]:
            errors.append(f"N8.17.B {matrix_id}: domain {domain!r} != catalog {by_id[matrix_id]['domain']!r}")
        for key in REQUIRED_DOMAIN_STRINGS:
            value = contract.get(key)
            if not isinstance(value, str) or not value.strip():
                errors.append(f"N8.17.B {matrix_id}: {key} must be a non-empty string")
            elif PLACEHOLDER_RE.search(value):
                errors.append(f"N8.17.B {matrix_id}: unjustified placeholder in {key}")
        for key in REQUIRED_DOMAIN_ARRAYS:
            value = contract.get(key)
            if not isinstance(value, list) or not value or not all(isinstance(item, str) and item.strip() for item in value):
                errors.append(f"N8.17.B {matrix_id}: {key} must be a non-empty string array")
                continue
            for item in value:
                if PLACEHOLDER_RE.search(item):
                    errors.append(f"N8.17.B {matrix_id}: unjustified placeholder in {key}")
    if len(ids) != len(set(ids)):
        duplicates = sorted({x for x in ids if ids.count(x) > 1})
        errors.append(f"N8.17.B duplicate MATRIX_ID values: {duplicates}")
    missing = sorted(set(catalog_ids) - set(ids))
    extra = sorted(set(ids) - set(catalog_ids))
    if missing:
        errors.append(f"N8.17.B missing catalog MATRIX_ID values: {missing}")
    if extra:
        errors.append(f"N8.17.B contains extra MATRIX_ID values: {extra}")
    if doc.get("expected_total") != len(catalog_ids):
        errors.append(f"N8.17.B expected_total {doc.get('expected_total')!r} != catalog count {len(catalog_ids)}")
    if len(contracts) != len(catalog_ids):
        errors.append(f"N8.17.B contract count {len(contracts)} != catalog count {len(catalog_ids)}")
    return errors


def validate_material_matrices() -> tuple[int, list[str]]:
    governed = 0
    errors: list[str] = []
    for path in sorted(MATRIX_ROOT.rglob("*.md")):
        if GOV in path.parents:
            continue
        text = path.read_text(encoding="utf-8")
        if "MATRIX_ID:" not in text:
            continue
        governed += 1
        errors.extend(validate_material_matrix_text(text, str(path.relative_to(ROOT))))
    return governed, errors


def main() -> int:
    errors: list[str] = []
    if not CATALOG.is_file():
        errors.append(f"missing catalog: {CATALOG.relative_to(ROOT)}")
    if not TEMPLATE.is_file():
        errors.append(f"missing template: {TEMPLATE.relative_to(ROOT)}")
    if errors:
        return fail(errors)
    errors.extend(self_test_negative_contracts())
    catalog_text = CATALOG.read_text(encoding="utf-8")
    template_text = TEMPLATE.read_text(encoding="utf-8")
    rows, parse_errors = parse_catalog(catalog_text)
    errors.extend(parse_errors)
    declared = re.search(r"Conteo exacto:\s*\*\*(\d+) contract roots\*\*", catalog_text)
    if not declared:
        errors.append("catalog does not declare exact contract-root count")
    elif int(declared.group(1)) != len(rows):
        errors.append(f"declared count {declared.group(1)} != parsed rows {len(rows)}")
    ids = [row["id"] for row in rows]
    if len(ids) != len(set(ids)):
        duplicates = sorted({x for x in ids if ids.count(x) > 1})
        errors.append(f"duplicate MATRIX_ID values: {duplicates}")
    id_set = set(ids)
    implementation_refs: set[str] = set()
    for row in rows:
        matrix_id = row["id"]
        line = row["line"]
        if not ID_RE.fullmatch(matrix_id):
            errors.append(f"line {line}: invalid stable MATRIX_ID {matrix_id!r}")
        if ROW_ID_RE.fullmatch(matrix_id.rsplit("::", 1)[-1]):
            errors.append(f"line {line}: row/index-derived identity forbidden: {matrix_id}")
        if not row["domain"] or not row["kind"] or not row["implementation"]:
            errors.append(f"line {line}: blank required catalog column")
        for column in ("domain", "kind", "implementation", "parent", "status", "classification"):
            if PLACEHOLDER_RE.search(row[column]):
                errors.append(f"line {line}: unjustified placeholder in catalog {column}: {row[column]!r}")
        if row["parent"] != "ROOT" and row["parent"] not in id_set:
            errors.append(f"line {line}: unknown PARENT_MATRIX_ID {row['parent']}")
        if row["status"] == "MATERIAL_WITHOUT_ID":
            errors.append(f"line {line}: forbidden status MATERIAL_WITHOUT_ID")
        if row["implementation"] in implementation_refs:
            errors.append(f"line {line}: duplicate implementation root {row['implementation']}")
        implementation_refs.add(row["implementation"])
    for section, tokens in REQUIRED_TEMPLATE_TOKENS.items():
        for token in tokens:
            if token not in template_text:
                errors.append(f"template missing {section} token: {token}")
    if "jamás se deriva de fila, índice, orden visual" not in template_text:
        errors.append("template does not explicitly reject row/index identity")
    if "MATERIAL_WITHOUT_ID" not in catalog_text:
        errors.append("catalog does not encode no-material-without-id invariant")
    for state in ALLOWED_MATRIX_STATES:
        if state not in template_text:
            errors.append(f"template missing canonical MATRIX_STATE value: {state}")
    errors.extend(validate_batch_manifest(rows))
    errors.extend(validate_domain_contracts(rows))
    governed_matrices, material_errors = validate_material_matrices()
    errors.extend(material_errors)
    if errors:
        return fail(errors)
    material = sum(1 for row in rows if row["status"] == "MATERIAL")
    containers = sum(1 for row in rows if row["status"] == "DISCOVERY_CONTAINER")
    print(
        "matrix governance PASS: "
        f"ids={len(rows)} unique={len(id_set)} material={material} "
        f"discovery_containers={containers} governed_matrices={governed_matrices} "
        f"n8_17_batch_manifest={'present' if BATCH_MANIFEST.is_file() else 'absent'} "
        f"n8_17_domain_contracts={'present' if DOMAIN_CONTRACTS.is_file() else 'absent'}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
