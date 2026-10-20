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
