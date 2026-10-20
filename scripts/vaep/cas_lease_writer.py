#!/usr/bin/env python3
"""SOLQARYN's fail-closed, atomic Google Sheets lease request builder.

This module has NO background process, network access or credentials. A trusted
operator passes the resulting requests to the connected Google Sheets
spreadsheets.batchUpdate action, then verifies its response and a live readback.

A plain updateCells is never a claim. A lease claim uses a deterministic,
per-task DeveloperMetadata ID, whose uniqueness Google Sheets enforces.
The reservation itself does NOT certify ACTIVE_REAL or completed work.
"""

from dataclasses import dataclass
from datetime import datetime, timedelta, timezone
import re
import secrets

from commercial_dispatch import UnsafeSnapshot, dispatch, verify_claim_choice


METADATA_BASE = 1_600_000_000
METADATA_KEY = "SOLQARYN_TASK_LEASE"
TTL_MINUTES = 10
ALLOWED_RELEASE = frozenset({"PENDIENTE", "VALIDANDO", "BLOQUEADO"})
_SHA = re.compile(r"^[0-9a-fA-F]{40}$")
_OWNER = re.compile(r"^[0-9a-f]{32}$")
_EVENTS_COUNT = 12


def _s(value):
    return {"userEnteredValue": {"stringValue": str(value)}}


def _cell_range(sheet_id, row, start_col, values):
    return {
        "updateCells": {
            "range": {
                "sheetId": sheet_id,
                "startRowIndex": row,
                "endRowIndex": row + 1,
                "startColumnIndex": start_col,
                "endColumnIndex": start_col + len(values),
            },
            "rows": [{"values": [_s(value) for value in values]}],
            "fields": "userEnteredValue",
        }
    }


def _event(sheet_id, row):
    if len(row) != _EVENTS_COUNT:
        raise UnsafeSnapshot("INVALID_EVENT_COLUMNS")
    return {
        "appendCells": {
            "sheetId": sheet_id,
            "rows": [{"values": [_s(value) for value in row]}],
            "fields": "userEnteredValue",
        }
    }


def _stamp(value):
    if not isinstance(value, datetime) or value.tzinfo is None:
        raise UnsafeSnapshot("UTC_AWARE_TIMESTAMP_REQUIRED")
    return value.astimezone(timezone.utc).isoformat(timespec="milliseconds").replace("+00:00", "Z")


def _clean(value, name, max_len=200):
    if not isinstance(value, str) or not value or len(value) > max_len or "|" in value or "\n" in value:
        raise UnsafeSnapshot(f"INVALID_{name}")
    return value


@dataclass(frozen=True)
class Claim:
    task_id: str
    order: int
    row: int
    owner: str
    token: str
    run_id: str
    head: str
    metadata_id: int
    metadata_value: str
    acquired_at: str
    expires_at: str
    requests: tuple


def build_claim(
    commercial_rows, runtime_rows, *,
    task_id, owner, run_id, head, now,
    runtime_sheet_id, events_sheet_id, expected_count=1123,
):
    """Reject skips and build an indivisible metadata+row+event claim."""
    if not _OWNER.fullmatch(owner or ""):
        raise UnsafeSnapshot("INVALID_OWNER")
    if not _SHA.fullmatch(head or ""):
        raise UnsafeSnapshot("INVALID_HEAD")
    _clean(run_id, "RUN_ID")
    _clean(task_id, "TASK_ID")
    if not isinstance(runtime_sheet_id, int) or not isinstance(events_sheet_id, int):
        raise UnsafeSnapshot("INVALID_SHEET_ID")
    result = dispatch(commercial_rows, runtime_rows, expected_count)
    verify_claim_choice(result, task_id)
    chosen = result["selected"]
    order = chosen["order"]
    row = next((i for i, r in enumerate(runtime_rows) if i and r[0] == task_id), None)
    if row is None:
        raise UnsafeSnapshot("TASK_NOT_IN_RUNTIME")
    if order >= 100_000:
        raise UnsafeSnapshot("INVALID_METADATA_ORDER")
    acquired = _stamp(now)
    expires = _stamp(now + timedelta(minutes=TTL_MINUTES))
    token = secrets.token_hex(16)
    metadata_id = METADATA_BASE + order
    metadata_value = "|".join([task_id, run_id, owner, token, acquired, expires])
    lease_value = (
        f"ACTIVE|OWNER={owner}|TOKEN={token}|RUN_ID={run_id}|"
        f"START={acquired}|EXPIRES={expires}|HEAD={head}"
    )
    meta = {
        "createDeveloperMetadata": {
            "developerMetadata": {
                "metadataId": metadata_id,
                "metadataKey": METADATA_KEY,
                "metadataValue": metadata_value,
                "visibility": "DOCUMENT",
                "location": {
                    "dimensionRange": {
                        "sheetId": runtime_sheet_id,
                        "dimension": "ROWS",
                        "startIndex": row,
                        "endIndex": row + 1,
                    }
                },
            }
        }
    }
    requests = (
        meta,
        _cell_range(runtime_sheet_id, row, 1, ["EN_PROGRESO", owner, lease_value, head]),
        _cell_range(runtime_sheet_id, row, 8, [acquired]),
        _event(events_sheet_id, [
            acquired, owner, task_id, "LEASE_ACQUIRED", "PENDIENTE",
            "EN_PROGRESO", head, "", "LEASE_RESERVED_MATERIAL_PENDING",
            "Google Sheets DeveloperMetadata CAS claim; no product work certified",
            run_id, token,
        ]),
    )
    return Claim(task_id, order, row, owner, token, run_id, head,
                 metadata_id, metadata_value, acquired, expires, requests)


def verify_claim_response(claim, api_response, runtime_row):
    """Require the exact metadata from API and owner/token in fresh runtime."""
    try:
        meta = api_response["replies"][0]["createDeveloperMetadata"]["developerMetadata"]
    except (KeyError, IndexError, TypeError) as exc:
        raise UnsafeSnapshot("NO_CAS_METADATA_CONFIRMATION") from exc
    if (meta.get("metadataId") != claim.metadata_id
            or meta.get("metadataKey") != METADATA_KEY
            or meta.get("metadataValue") != claim.metadata_value):
        raise UnsafeSnapshot("CAS_METADATA_MISMATCH")
    if (runtime_row[0] != claim.task_id or runtime_row[1] != "EN_PROGRESO"
            or runtime_row[2] != claim.owner
            or f"TOKEN={claim.token}" not in runtime_row[3]
            or f"RUN_ID={claim.run_id}" not in runtime_row[3]):
        raise UnsafeSnapshot("CAS_RUNTIME_READBACK_MISMATCH")
    return True


def build_release(
    claim, *, current_runtime_row, captured_metadata, now,
    final_state, evidence, result, details,
    runtime_sheet_id, events_sheet_id,
):
    """Release only the captured, exact metadata owned by this claim.

    Call only after verifying fresh ownership and a completed invocation or
    handoff. Never recover another actor's stale lease using this function.
    """
    if final_state not in ALLOWED_RELEASE:
        raise UnsafeSnapshot("UNAUTHORIZED_FINAL_STATE")
    if (current_runtime_row[0] != claim.task_id
            or current_runtime_row[1] != "EN_PROGRESO"
            or current_runtime_row[2] != claim.owner
            or f"TOKEN={claim.token}" not in current_runtime_row[3]):
        raise UnsafeSnapshot("RELEASE_OWNER_TOKEN_MISMATCH")
    if (captured_metadata.get("metadataId") != claim.metadata_id
            or captured_metadata.get("metadataKey") != METADATA_KEY
            or captured_metadata.get("metadataValue") != claim.metadata_value):
        raise UnsafeSnapshot("RELEASE_METADATA_MISMATCH")
    _clean(result, "RESULT")
    _clean(details, "DETAILS", 1600)
    if not isinstance(evidence, str) or len(evidence) > 1200:
        raise UnsafeSnapshot("INVALID_EVIDENCE")
    released = _stamp(now)
    released_value = (
        f"RELEASED|OWNER={claim.owner}|TOKEN={claim.token}|"
        f"RUN_ID={claim.run_id}|RELEASED_AT={released}|HEAD={claim.head}"
    )
    deletion = {
        "deleteDeveloperMetadata": {
            "dataFilter": {
                "developerMetadataLookup": {
                    "metadataId": claim.metadata_id,
                    "metadataKey": METADATA_KEY,
                    "metadataValue": claim.metadata_value,
                    "visibility": "DOCUMENT",
                }
            }
        }
    }
    requests = (
        deletion,
        _cell_range(runtime_sheet_id, claim.row, 1, [final_state, "", released_value]),
        _cell_range(runtime_sheet_id, claim.row, 5, [evidence]),
        _cell_range(runtime_sheet_id, claim.row, 8, [released]),
        _event(events_sheet_id, [
            released, claim.owner, claim.task_id, "LEASE_RELEASED",
            "EN_PROGRESO", final_state, claim.head, evidence, result,
            details, claim.run_id, claim.token,
        ]),
    )
    return requests


def verify_release_response(claim, response, fresh_row):
    """Fail if no exact metadata was deleted or sheet still claims ownership."""
    try:
        matches = response["replies"][0]["deleteDeveloperMetadata"]["deletedDeveloperMetadata"]
    except (KeyError, IndexError, TypeError) as exc:
        raise UnsafeSnapshot("RELEASE_DELETE_UNCONFIRMED") from exc
    if len(matches) != 1 or matches[0].get("metadataId") != claim.metadata_id:
        raise UnsafeSnapshot("RELEASE_DID_NOT_DELETE_EXACT_LOCK")
    if (fresh_row[0] != claim.task_id or fresh_row[2] != ""
            or not fresh_row[3].startswith("RELEASED")
            or f"TOKEN={claim.token}" not in fresh_row[3]):
        raise UnsafeSnapshot("RELEASE_READBACK_MISMATCH")
    return True


def _review_author(runtime_row):
    """Preserve the implementation author through successive independent reviews."""
    lease = str(runtime_row[3]) if len(runtime_row) > 3 else ""
    author = re.search(r"(?:^|\|)ORIGINAL_AUTHOR=([0-9a-f]{32})(?:\||$)", lease)
    if author:
        return author.group(1)
    if not lease.startswith("RELEASED|"):
        raise UnsafeSnapshot("REVIEW_MISSING_RELEASED_HANDOFF")
    author = re.search(r"(?:^|\|)OWNER=([0-9a-f]{32})(?:\||$)", lease)
    if not author:
        raise UnsafeSnapshot("REVIEW_MISSING_ORIGINAL_AUTHOR")
    return author.group(1)


def _verify_review_lease(claim, current_runtime_row, captured_metadata):
    """Readback must match the exact review claim; never steal another actor's lock."""
    if (current_runtime_row[0] != claim.task_id
            or current_runtime_row[1] != "VALIDANDO"
            or current_runtime_row[2] != claim.owner
            or f"TOKEN={claim.token}" not in current_runtime_row[3]
            or f"RUN_ID={claim.run_id}" not in current_runtime_row[3]
            or f"HEAD={claim.head}" not in current_runtime_row[3]):
        raise UnsafeSnapshot("REVIEW_LEASE_MISMATCH")
    if (captured_metadata.get("metadataId") != claim.metadata_id
            or captured_metadata.get("metadataKey") != METADATA_KEY
            or captured_metadata.get("metadataValue") != claim.metadata_value):
        raise UnsafeSnapshot("REVIEW_METADATA_MISMATCH")
    fields = claim.metadata_value.split("|")
    if len(fields) != 7 or fields[0] != claim.task_id or fields[2] != claim.owner:
        raise UnsafeSnapshot("REVIEW_PROVENANCE_INVALID")
    author = fields[6]
    if not _OWNER.fullmatch(author) or claim.owner == author:
        raise UnsafeSnapshot("SELF_REVIEW_FORBIDDEN")
    return author


def build_review_claim(
    commercial_rows, runtime_rows, *,
    task_id, reviewer, run_id, head, now,
    runtime_sheet_id, events_sheet_id, expected_count=1123,
):
    """Acquire a review-specific CAS lease, never masquerading as implementation."""
    from commercial_dispatch import snapshot
    if not _OWNER.fullmatch(reviewer or ""):
        raise UnsafeSnapshot("INVALID_REVIEWER")
    if not _SHA.fullmatch(head or ""):
        raise UnsafeSnapshot("INVALID_HEAD")
    _clean(run_id, "RUN_ID")
    _clean(task_id, "TASK_ID")
    if not isinstance(runtime_sheet_id, int) or not isinstance(events_sheet_id, int):
        raise UnsafeSnapshot("INVALID_SHEET_ID")
    tasks = snapshot(commercial_rows, runtime_rows, expected_count)
    if task_id not in tasks:
        raise UnsafeSnapshot("UNKNOWN_REVIEW_TASK")
    item = tasks[task_id]
    if item["state"] != "VALIDANDO" or item["owner"]:
        raise UnsafeSnapshot("REVIEW_NOT_FREE_OR_NOT_VALIDATING")
    if any(tasks[d]["state"] != "LISTO" for d in item["dependencies"]):
        raise UnsafeSnapshot("REVIEW_DEPENDENCIES_NOT_READY")
    row_index = next((i for i, r in enumerate(runtime_rows) if i and r[0] == task_id), None)
    if row_index is None:
        raise UnsafeSnapshot("TASK_NOT_IN_RUNTIME")
    source_row = runtime_rows[row_index]
    author = _review_author(source_row)
    if author == reviewer:
        raise UnsafeSnapshot("SELF_REVIEW_FORBIDDEN")
    if not isinstance(source_row[5], str) or not source_row[5].startswith("https://"):
        raise UnsafeSnapshot("REVIEW_MISSING_ORIGINAL_EVIDENCE")
    if not _SHA.fullmatch(source_row[4]):
        raise UnsafeSnapshot("REVIEW_MISSING_ORIGINAL_HEAD")
    other_reviews = sorted(
        (t for t in tasks.values()
         if t["state"] == "VALIDANDO" and not t["owner"]
         and t["lease"].startswith("RELEASED|")
         and all(tasks[d]["state"] == "LISTO" for d in t["dependencies"])),
        key=lambda t: t["order"],
    )
    if not other_reviews or other_reviews[0]["id"] != task_id:
        raise UnsafeSnapshot("OUT_OF_ORDER_REVIEW_CLAIM")
    # A lower-order implementation cannot be bypassed by a later review.
    pending = dispatch(commercial_rows, runtime_rows, expected_count)["selected"]
    if pending and pending["order"] < item["order"]:
        raise UnsafeSnapshot("OUT_OF_ORDER_REVIEW_CLAIM")
    acquired = _stamp(now)
    expires = _stamp(now + timedelta(minutes=TTL_MINUTES))
    token = secrets.token_hex(16)
    metadata_id = METADATA_BASE + item["order"]
    metadata_value = "|".join([
        task_id, run_id, reviewer, token, acquired, expires, author,
    ])
    lease = (
        f"ACTIVE_REVIEW|OWNER={reviewer}|ORIGINAL_AUTHOR={author}|TOKEN={token}|"
        f"RUN_ID={run_id}|START={acquired}|EXPIRES={expires}|HEAD={head}"
    )
    meta = {"createDeveloperMetadata": {"developerMetadata": {
        "metadataId": metadata_id, "metadataKey": METADATA_KEY,
        "metadataValue": metadata_value, "visibility": "DOCUMENT",
        "location": {"dimensionRange": {
            "sheetId": runtime_sheet_id, "dimension": "ROWS",
            "startIndex": row_index, "endIndex": row_index + 1,
        }},
    }}}
    requests = (
        meta,
        _cell_range(runtime_sheet_id, row_index, 1, ["VALIDANDO", reviewer, lease, head]),
        _cell_range(runtime_sheet_id, row_index, 8, [acquired]),
        _event(events_sheet_id, [
            acquired, reviewer, task_id, "REVIEW_LEASE_ACQUIRED", "VALIDANDO",
            "VALIDANDO", head, source_row[5], "REVIEW_RESERVED_VERIFICATION_PENDING",
            "Independent REVIEW_FIRST pending, no LISTO yet", run_id, token,
        ]),
    )
    return Claim(task_id, item["order"], row_index, reviewer, token, run_id, head,
                 metadata_id, metadata_value, acquired, expires, requests)


def verify_review_claim_response(claim, api_response, fresh_runtime_row):
    try:
        metadata = api_response["replies"][0]["createDeveloperMetadata"]["developerMetadata"]
    except (KeyError, IndexError, TypeError) as exc:
        raise UnsafeSnapshot("NO_REVIEW_CAS_CONFIRMATION") from exc
    _verify_review_lease(claim, fresh_runtime_row, metadata)
    return True


def build_review_completion(
    claim, *, current_runtime_row, captured_metadata, now,
    final_state, review_evidence, ci_evidence, review_details, p0, p1,
    runtime_sheet_id, events_sheet_id,
):
    """Independently accept/reject the review; never infer certification from a lease.

    Caller must verify real CI and test reports from their authoritative origin.
    This helper proves structural provenance, not the truth of external links.
    """
    author = _verify_review_lease(claim, current_runtime_row, captured_metadata)
    if final_state not in ("LISTO", "VALIDANDO"):
        raise UnsafeSnapshot("REVIEW_INVALID_FINAL_STATE")
    _clean(review_details, "REVIEW_DETAILS", 1600)
    if not isinstance(review_evidence, str) or not review_evidence.startswith(
            "https://github.com/solqaryn/Solqaryn/"):
        raise UnsafeSnapshot("REVIEW_PROOF_REQUIRED")
    if final_state == "LISTO":
        if p0 != 0 or p1 != 0 or isinstance(p0, bool) or isinstance(p1, bool):
            raise UnsafeSnapshot("REVIEW_P0_P1_NOT_ZERO")
        if not isinstance(ci_evidence, str) or not ci_evidence.startswith(
                "https://github.com/solqaryn/Solqaryn/actions/runs/"):
            raise UnsafeSnapshot("REVIEW_CI_PROOF_REQUIRED")
    if not isinstance(runtime_sheet_id, int) or not isinstance(events_sheet_id, int):
        raise UnsafeSnapshot("INVALID_SHEET_ID")
    stamp = _stamp(now)
    lease = (
        f"RELEASED|OWNER={claim.owner}|ORIGINAL_AUTHOR={author}|TOKEN={claim.token}|"
        f"RUN_ID={claim.run_id}|RELEASED_AT={stamp}|HEAD={claim.head}"
    )
    # Keep original implementation evidence in column F for audit continuity.
    result = "REVIEW_FIRST_ACCEPTED_LISTO" if final_state == "LISTO" else "REVIEW_FIRST_NOT_APPROVED"
    next_action = "CERRADA; RECALCULAR_DAG_GLOBAL" if final_state == "LISTO" else (
        "REVIEW_FIRST pendiente; corrección necesaria antes de LISTO")
    requests = [
        {"deleteDeveloperMetadata": {"dataFilter": {
            "developerMetadataLookup": {
                "metadataId": claim.metadata_id,
                "metadataKey": METADATA_KEY,
                "metadataValue": claim.metadata_value,
                "visibility": "DOCUMENT",
            },
        }}},
        _cell_range(runtime_sheet_id, claim.row, 1, [final_state, "", lease]),
        _cell_range(runtime_sheet_id, claim.row, 7, [next_action, stamp]),
        _cell_range(runtime_sheet_id, claim.row, 15, [
            "LISTO" if final_state == "LISTO" else "REVIEW_REQUIRES_CORRECTION",
            f"REVIEW_FIRST reviewer={claim.owner} author={author} "
            f"proof={review_evidence} CI={ci_evidence or 'NOT_ACCEPTED'} "
            f"notes={review_details}",
        ]),
    ]
    if final_state == "LISTO":
        requests.append(_cell_range(runtime_sheet_id, claim.row, 12, ["0", "0"]))
    requests.append(_event(events_sheet_id, [
        stamp, claim.owner, claim.task_id, result, "VALIDANDO", final_state,
        claim.head, review_evidence, result, review_details, claim.run_id, claim.token,
    ]))
    return tuple(requests)


def verify_review_completion_response(claim, api_response, fresh_runtime_row,
                                      expected_final_state):
    try:
        removed = api_response["replies"][0]["deleteDeveloperMetadata"][
            "deletedDeveloperMetadata"]
    except (KeyError, IndexError, TypeError) as exc:
        raise UnsafeSnapshot("REVIEW_RELEASE_UNCONFIRMED") from exc
    if len(removed) != 1 or removed[0].get("metadataId") != claim.metadata_id:
        raise UnsafeSnapshot("REVIEW_RELEASE_WRONG_LOCK")
    if (fresh_runtime_row[0] != claim.task_id
            or fresh_runtime_row[1] != expected_final_state
            or fresh_runtime_row[2] != ""
            or not fresh_runtime_row[3].startswith("RELEASED|")
            or f"TOKEN={claim.token}" not in fresh_runtime_row[3]):
        raise UnsafeSnapshot("REVIEW_COMPLETION_READBACK_MISMATCH")
    return True
