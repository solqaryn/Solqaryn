#!/usr/bin/env python3
"""SOLQARYN's fail-closed, atomic Google Sheets lease request builder.

This module has NO background process, network access or credentials. A trusted
operator passes the resulting requests to the connected Google Sheets
spreadsheets.batchUpdate action, then verifies its response and a live readback.

A plain updateCells is never a claim. A lease claim uses deterministic,
per-task and per-executor DeveloperMetadata IDs, whose uniqueness Google Sheets
enforces document-wide. The reservation itself does NOT certify ACTIVE_REAL
or completed work.
"""

from dataclasses import dataclass
from datetime import datetime, timedelta, timezone
import re
import secrets

from commercial_dispatch import UnsafeSnapshot, dispatch, verify_claim_choice, snapshot, eligible_independent_reviews


METADATA_BASE = 1_600_000_000
METADATA_KEY = "SOLQARYN_TASK_LEASE"
EXECUTOR_METADATA_BASE = 1_700_000_000
EXECUTOR_METADATA_KEY = "SOLQARYN_EXECUTOR_LEASE"
TTL_MINUTES = 10
ALLOWED_RELEASE = frozenset({"PENDIENTE", "VALIDANDO", "BLOQUEADO"})
_SHA = re.compile(r"^[0-9a-fA-F]{40}$")
_OWNER = re.compile(r"^[0-9a-f]{32}$")
_EVENTS_COUNT = 12

CANONICAL_PRIMARY_IDS = frozenset({
    "6ad7bbf79c488191bf893e6c08ea9b5f",  # :00
    "6ad7bc0d48748191aa1dd0ce2f18d22a",  # :12
    "6ad7bc1cc7b8819190665e5ed78610d4",  # :24
    "6ad7bc3001b88191add05d9b4d5a4130",  # :36
    "6ad7bc3ebea08191a16dc7c9f1f3303b",  # :48
})
CANONICAL_SUPERVISOR_IDS = frozenset({
    "6abd62255ae88191a2dba6e1b00d3b4d",  # :05
    "6abd6235e100819195785fc76a44a8e0",  # :17
    "6abd6241c3e48191a8856ed7b0f42c5c",  # :29
    "6abd624e5e108191bef6bb879559328c",  # :41
    "6abd625b397c8191a49904227946a20f",  # :53
})
CANONICAL_AUTOMATION_IDS = CANONICAL_PRIMARY_IDS | CANONICAL_SUPERVISOR_IDS


def _executor_metadata_id(owner):
    if not isinstance(owner, str) or not _OWNER.fullmatch(owner):
        raise UnsafeSnapshot("INVALID_OWNER")
    return EXECUTOR_METADATA_BASE + (int(owner, 16) % 100_000_000)


def _check_executor_wip(runtime_rows, owner, exempt_task_id=None):
    """Ensure owner does not already hold an active unreleased assignment (WIP limit = 1)."""
    for r in runtime_rows[1:]:
        if len(r) > 2 and r[2] == owner:
            task_id = r[0]
            if exempt_task_id and task_id == exempt_task_id:
                continue
            state = r[1] if len(r) > 1 else ""
            lease = str(r[3]) if len(r) > 3 else ""
            if state in ("EN_PROGRESO", "VALIDANDO") and not lease.startswith("RELEASED"):
                raise UnsafeSnapshot(f"EXECUTOR_WIP_LIMIT_EXCEEDED:{owner}:{task_id}")


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


def _parse_lease(lease_str):
    parts = str(lease_str or "").split("|")
    prefix = parts[0] if parts else ""
    data = {"_prefix": prefix}
    for p in parts[1:]:
        if "=" in p:
            k, v = p.split("=", 1)
            data[k] = v
    return data


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
    executor_metadata_id: int
    executor_metadata_value: str = ""


def build_claim(
    commercial_rows, runtime_rows, *,
    task_id, owner, run_id, head, now,
    runtime_sheet_id, events_sheet_id, expected_count=1123,
):
    """Reject skips and build an indivisible dual-lock (task + executor) claim."""
    if not isinstance(owner, str) or not _OWNER.fullmatch(owner):
        raise UnsafeSnapshot("INVALID_OWNER")
    if owner not in CANONICAL_AUTOMATION_IDS:
        raise UnsafeSnapshot("UNKNOWN_CANONICAL_EXECUTOR")
    if not _SHA.fullmatch(head or ""):
        raise UnsafeSnapshot("INVALID_HEAD")
    _clean(run_id, "RUN_ID")
    _clean(task_id, "TASK_ID")
    if not isinstance(runtime_sheet_id, int) or not isinstance(events_sheet_id, int):
        raise UnsafeSnapshot("INVALID_SHEET_ID")
    _check_executor_wip(runtime_rows, owner, exempt_task_id=task_id)
    # Prefer independent VALIDANDO review over any new PENDIENTE claim.
    ready_reviews = eligible_independent_reviews(
        commercial_rows, runtime_rows, expected_count, reviewer=owner)
    if ready_reviews:
        raise UnsafeSnapshot("VALIDANDO_PRIORITY_BEFORE_PENDING:" + ready_reviews[0]["id"])
    result = dispatch(commercial_rows, runtime_rows, expected_count, candidate_owner=owner)
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
    meta_task = {
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
    exec_meta_id = _executor_metadata_id(owner)
    exec_meta_val = f"{owner}|{task_id}|{token}|{acquired}|{expires}"
    meta_exec = {
        "createDeveloperMetadata": {
            "developerMetadata": {
                "metadataId": exec_meta_id,
                "metadataKey": EXECUTOR_METADATA_KEY,
                "metadataValue": exec_meta_val,
                "visibility": "DOCUMENT",
                "location": {"spreadsheet": True},
            }
        }
    }
    req_list = [
        meta_task,
        meta_exec,
        _cell_range(runtime_sheet_id, row, 1, ["EN_PROGRESO", owner, lease_value, head]),
        _cell_range(runtime_sheet_id, row, 8, [acquired]),
        _event(events_sheet_id, [
            acquired, owner, task_id, "LEASE_ACQUIRED", "PENDIENTE",
            "EN_PROGRESO", head, "", "LEASE_RESERVED_MATERIAL_PENDING",
            "Google Sheets DeveloperMetadata CAS claim; no product work certified",
            run_id, token,
        ]),
    ]
    return Claim(task_id, order, row, owner, token, run_id, head,
                 metadata_id, metadata_value, acquired, expires, tuple(req_list),
                 executor_metadata_id=exec_meta_id, executor_metadata_value=exec_meta_val)


def verify_claim_response(claim, api_response, runtime_row):
    """Require both task and executor metadata confirmations and fresh runtime readback."""
    try:
        replies = api_response["replies"]
        meta_task = replies[0]["createDeveloperMetadata"]["developerMetadata"]
        meta_exec = replies[1]["createDeveloperMetadata"]["developerMetadata"]
    except (KeyError, IndexError, TypeError) as exc:
        raise UnsafeSnapshot("NO_CAS_METADATA_CONFIRMATION") from exc
    if (meta_task.get("metadataId") != claim.metadata_id
            or meta_task.get("metadataKey") != METADATA_KEY
            or meta_task.get("metadataValue") != claim.metadata_value):
        raise UnsafeSnapshot("CAS_TASK_METADATA_MISMATCH")
    if (meta_exec.get("metadataId") != claim.executor_metadata_id
            or meta_exec.get("metadataKey") != EXECUTOR_METADATA_KEY
            or meta_exec.get("metadataValue") != claim.executor_metadata_value):
        raise UnsafeSnapshot("CAS_EXECUTOR_METADATA_MISMATCH")
    if (runtime_row[0] != claim.task_id or runtime_row[1] != "EN_PROGRESO"
            or runtime_row[2] != claim.owner
            or f"TOKEN={claim.token}" not in runtime_row[3]
            or f"RUN_ID={claim.run_id}" not in runtime_row[3]):
        raise UnsafeSnapshot("CAS_RUNTIME_READBACK_MISMATCH")
    return True


def build_release(
    claim, *, current_runtime_row, captured_metadata, captured_executor_metadata=None, now,
    final_state, evidence, result, details,
    runtime_sheet_id, events_sheet_id,
):
    """Release exact task and executor metadata owned by this claim.

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
    if not isinstance(captured_metadata, dict):
        raise UnsafeSnapshot("CAPTURED_METADATA_REQUIRED")
    if (captured_metadata.get("metadataId") != claim.metadata_id
            or captured_metadata.get("metadataKey") != METADATA_KEY
            or captured_metadata.get("metadataValue") != claim.metadata_value):
        raise UnsafeSnapshot("RELEASE_METADATA_MISMATCH")
    if not isinstance(captured_executor_metadata, dict):
        raise UnsafeSnapshot("RELEASE_EXECUTOR_METADATA_REQUIRED")
    if (captured_executor_metadata.get("metadataId") != claim.executor_metadata_id
            or captured_executor_metadata.get("metadataKey") != EXECUTOR_METADATA_KEY
            or captured_executor_metadata.get("metadataValue") != claim.executor_metadata_value):
        raise UnsafeSnapshot("RELEASE_EXECUTOR_METADATA_MISMATCH")
    _clean(result, "RESULT")
    _clean(details, "DETAILS", 1600)
    if not isinstance(evidence, str) or len(evidence) > 1200:
        raise UnsafeSnapshot("INVALID_EVIDENCE")
    released = _stamp(now)
    released_value = (
        f"RELEASED|OWNER={claim.owner}|TOKEN={claim.token}|"
        f"RUN_ID={claim.run_id}|RELEASED_AT={released}|HEAD={claim.head}"
    )
    deletion_task = {
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
    deletion_exec = {
        "deleteDeveloperMetadata": {
            "dataFilter": {
                "developerMetadataLookup": {
                    "metadataId": claim.executor_metadata_id,
                    "metadataKey": EXECUTOR_METADATA_KEY,
                    "metadataValue": claim.executor_metadata_value,
                    "visibility": "DOCUMENT",
                }
            }
        }
    }
    req_list = [
        deletion_task,
        deletion_exec,
        _cell_range(runtime_sheet_id, claim.row, 1, [final_state, "", released_value]),
        _cell_range(runtime_sheet_id, claim.row, 5, [evidence]),
        _cell_range(runtime_sheet_id, claim.row, 8, [released]),
        _event(events_sheet_id, [
            released, claim.owner, claim.task_id, "LEASE_RELEASED",
            "EN_PROGRESO", final_state, claim.head, evidence, result,
            details, claim.run_id, claim.token,
        ]),
    ]
    return tuple(req_list)


def verify_release_response(claim, response, fresh_row):
    """Fail if exact task or executor metadata was not deleted or sheet still claims ownership."""
    try:
        matches_task = response["replies"][0]["deleteDeveloperMetadata"]["deletedDeveloperMetadata"]
        matches_exec = response["replies"][1]["deleteDeveloperMetadata"]["deletedDeveloperMetadata"]
    except (KeyError, IndexError, TypeError) as exc:
        raise UnsafeSnapshot("RELEASE_DELETE_UNCONFIRMED") from exc
    if (len(matches_task) != 1 or matches_task[0].get("metadataId") != claim.metadata_id
            or matches_task[0].get("metadataValue") != claim.metadata_value):
        raise UnsafeSnapshot("RELEASE_DID_NOT_DELETE_EXACT_TASK_LOCK")
    if (len(matches_exec) != 1 or matches_exec[0].get("metadataId") != claim.executor_metadata_id
            or matches_exec[0].get("metadataValue") != claim.executor_metadata_value):
        raise UnsafeSnapshot("RELEASE_DID_NOT_DELETE_EXACT_EXECUTOR_LOCK")
    if (fresh_row[0] != claim.task_id
            or fresh_row[1] not in ALLOWED_RELEASE
            or fresh_row[2] != ""
            or not fresh_row[3].startswith("RELEASED|")
            or f"TOKEN={claim.token}" not in fresh_row[3]):
        raise UnsafeSnapshot("RELEASE_RUNTIME_READBACK_MISMATCH")
    return True


def _author_from_release_lease(lease):
    for part in (lease or "").split("|"):
        if part.startswith("OWNER="):
            author = part.split("=", 1)[1]
            if _OWNER.fullmatch(author):
                return author
    raise UnsafeSnapshot("AUTHOR_CANNOT_BE_RESOLVED_FROM_HANDOFF")


def _verify_review_lease(claim, row, metadata):
    if row[0] != claim.task_id or row[1] != "VALIDANDO" or row[2] != claim.owner:
        raise UnsafeSnapshot("REVIEW_LEASE_MISMATCH")
    lease = row[3] or ""
    if not lease.startswith("ACTIVE_REVIEW|"):
        raise UnsafeSnapshot("REVIEW_LEASE_MISMATCH")
    for key, expected in [
        ("OWNER", claim.owner),
        ("TOKEN", claim.token),
        ("RUN_ID", claim.run_id),
        ("HEAD", claim.head),
    ]:
        if f"{key}={expected}" not in lease:
            raise UnsafeSnapshot("REVIEW_LEASE_MISMATCH")
    parts = metadata.get("metadataValue", "").split("|")
    if len(parts) != 7:
        raise UnsafeSnapshot("REVIEW_METADATA_SHAPE_MISMATCH")
    if (metadata.get("metadataId") != claim.metadata_id
            or metadata.get("metadataKey") != METADATA_KEY
            or parts[0] != claim.task_id
            or parts[1] != claim.run_id
            or parts[2] != claim.owner
            or parts[3] != claim.token
            or parts[4] != claim.acquired_at
            or parts[5] != claim.expires_at):
        raise UnsafeSnapshot("REVIEW_METADATA_MISMATCH")
    author = parts[6]
    if author == claim.owner or not _OWNER.fullmatch(author):
        raise UnsafeSnapshot("SELF_REVIEW_OR_MALFORMED_AUTHOR")
    return author


def build_review_claim(
    commercial_rows, runtime_rows, *,
    task_id, reviewer, run_id, head, now,
    runtime_sheet_id, events_sheet_id, expected_count=1123,
):
    """Reserve VALIDANDO work for an independent reviewer with dual locks."""
    if not isinstance(reviewer, str) or not _OWNER.fullmatch(reviewer):
        raise UnsafeSnapshot("INVALID_REVIEWER")
    if reviewer not in CANONICAL_AUTOMATION_IDS:
        raise UnsafeSnapshot("UNKNOWN_CANONICAL_EXECUTOR")
    if not _SHA.fullmatch(head or ""):
        raise UnsafeSnapshot("INVALID_HEAD")
    _clean(run_id, "RUN_ID")
    _clean(task_id, "TASK_ID")
    if not isinstance(runtime_sheet_id, int) or not isinstance(events_sheet_id, int):
        raise UnsafeSnapshot("INVALID_SHEET_ID")
    _check_executor_wip(runtime_rows, reviewer, exempt_task_id=task_id)
    tasks = snapshot(commercial_rows, runtime_rows, expected_count)
    if task_id not in tasks:
        raise UnsafeSnapshot("TASK_NOT_IN_COMMERCIAL")
    item = tasks[task_id]
    if item["state"] != "VALIDANDO":
        raise UnsafeSnapshot("REVIEW_TASK_NOT_VALIDANDO")
    source_row = next(r for i, r in enumerate(runtime_rows) if i and r[0] == task_id)
    row_index = next(i for i, r in enumerate(runtime_rows) if i and r[0] == task_id)
    author = _author_from_release_lease(item["lease"])
    if author == reviewer:
        raise UnsafeSnapshot("SELF_REVIEW_FORBIDDEN")
    if not source_row[5] or not isinstance(source_row[5], str):
        raise UnsafeSnapshot("REVIEW_MISSING_ORIGINAL_EVIDENCE")
    if not item["lease"].startswith("RELEASED|"):
        raise UnsafeSnapshot("REVIEW_MISSING_RELEASED_HANDOFF")
    if not all(tasks[d]["state"] == "LISTO" for d in item["dependencies"]):
        raise UnsafeSnapshot("REVIEW_DEPENDENCIES_NOT_SATISFIED")
    other_reviews = eligible_independent_reviews(
        commercial_rows, runtime_rows, expected_count, reviewer=reviewer)
    if not other_reviews or other_reviews[0]["id"] != task_id:
        raise UnsafeSnapshot("OUT_OF_ORDER_REVIEW_CLAIM")
    # VALIDANDO precedes PENDIENTE; do not skip an earlier eligible review.
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
    meta_task = {"createDeveloperMetadata": {"developerMetadata": {
        "metadataId": metadata_id, "metadataKey": METADATA_KEY,
        "metadataValue": metadata_value, "visibility": "DOCUMENT",
        "location": {"dimensionRange": {
            "sheetId": runtime_sheet_id, "dimension": "ROWS",
            "startIndex": row_index, "endIndex": row_index + 1,
        }},
    }}}
    exec_meta_id = _executor_metadata_id(reviewer)
    exec_meta_val = f"{reviewer}|{task_id}|{token}|{acquired}|{expires}"
    meta_exec = {"createDeveloperMetadata": {"developerMetadata": {
        "metadataId": exec_meta_id, "metadataKey": EXECUTOR_METADATA_KEY, "metadataValue": exec_meta_val,
        "visibility": "DOCUMENT", "location": {"spreadsheet": True},
    }}}
    req_list = [
        meta_task,
        meta_exec,
        _cell_range(runtime_sheet_id, row_index, 1, ["VALIDANDO", reviewer, lease, head]),
        _cell_range(runtime_sheet_id, row_index, 8, [acquired]),
        _event(events_sheet_id, [
            acquired, reviewer, task_id, "REVIEW_LEASE_ACQUIRED", "VALIDANDO",
            "VALIDANDO", head, source_row[5], "REVIEW_RESERVED_VERIFICATION_PENDING",
            "Independent REVIEW_FIRST pending, no LISTO yet", run_id, token,
        ]),
    ]
    return Claim(task_id, item["order"], row_index, reviewer, token, run_id, head,
                 metadata_id, metadata_value, acquired, expires, tuple(req_list),
                 executor_metadata_id=exec_meta_id, executor_metadata_value=exec_meta_val)


def verify_review_claim_response(claim, api_response, fresh_runtime_row):
    try:
        replies = api_response["replies"]
        meta_task = replies[0]["createDeveloperMetadata"]["developerMetadata"]
        meta_exec = replies[1]["createDeveloperMetadata"]["developerMetadata"]
    except (KeyError, IndexError, TypeError) as exc:
        raise UnsafeSnapshot("NO_REVIEW_CAS_CONFIRMATION") from exc
    if (meta_task.get("metadataId") != claim.metadata_id
            or meta_task.get("metadataKey") != METADATA_KEY
            or meta_task.get("metadataValue") != claim.metadata_value):
        raise UnsafeSnapshot("CAS_TASK_METADATA_MISMATCH")
    if (meta_exec.get("metadataId") != claim.executor_metadata_id
            or meta_exec.get("metadataKey") != EXECUTOR_METADATA_KEY
            or meta_exec.get("metadataValue") != claim.executor_metadata_value):
        raise UnsafeSnapshot("CAS_EXECUTOR_METADATA_MISMATCH")
    _verify_review_lease(claim, fresh_runtime_row, meta_task)
    return True


def build_review_completion(
    claim, *, current_runtime_row, captured_metadata, captured_executor_metadata=None, now,
    final_state, review_evidence, ci_evidence, review_details, p0, p1,
    runtime_sheet_id, events_sheet_id,
):
    """Independently accept/reject the review; never infer certification from a lease."""
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
    if not isinstance(captured_executor_metadata, dict):
        raise UnsafeSnapshot("REVIEW_RELEASE_EXECUTOR_METADATA_REQUIRED")
    if (captured_executor_metadata.get("metadataId") != claim.executor_metadata_id
            or captured_executor_metadata.get("metadataKey") != EXECUTOR_METADATA_KEY
            or captured_executor_metadata.get("metadataValue") != claim.executor_metadata_value):
        raise UnsafeSnapshot("REVIEW_RELEASE_EXECUTOR_METADATA_MISMATCH")
    stamp = _stamp(now)
    lease = (
        f"RELEASED|OWNER={claim.owner}|ORIGINAL_AUTHOR={author}|TOKEN={claim.token}|"
        f"RUN_ID={claim.run_id}|RELEASED_AT={stamp}|HEAD={claim.head}"
    )
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
        {"deleteDeveloperMetadata": {"dataFilter": {
            "developerMetadataLookup": {
                "metadataId": claim.executor_metadata_id,
                "metadataKey": EXECUTOR_METADATA_KEY,
                "metadataValue": claim.executor_metadata_value,
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
        removed_task = api_response["replies"][0]["deleteDeveloperMetadata"]["deletedDeveloperMetadata"]
        removed_exec = api_response["replies"][1]["deleteDeveloperMetadata"]["deletedDeveloperMetadata"]
    except (KeyError, IndexError, TypeError) as exc:
        raise UnsafeSnapshot("REVIEW_RELEASE_UNCONFIRMED") from exc
    if (len(removed_task) != 1 or removed_task[0].get("metadataId") != claim.metadata_id
            or removed_task[0].get("metadataValue") != claim.metadata_value):
        raise UnsafeSnapshot("REVIEW_RELEASE_WRONG_TASK_LOCK")
    if (len(removed_exec) != 1 or removed_exec[0].get("metadataId") != claim.executor_metadata_id
            or removed_exec[0].get("metadataValue") != claim.executor_metadata_value):
        raise UnsafeSnapshot("REVIEW_RELEASE_WRONG_EXECUTOR_LOCK")
    if (fresh_runtime_row[0] != claim.task_id
            or fresh_runtime_row[1] != expected_final_state
            or fresh_runtime_row[2] != ""
            or not fresh_runtime_row[3].startswith("RELEASED|")
            or f"TOKEN={claim.token}" not in fresh_runtime_row[3]):
        raise UnsafeSnapshot("REVIEW_COMPLETION_READBACK_MISMATCH")
    return True


@dataclass(frozen=True)
class StaleLeaseRecovery:
    task_id: str
    order: int
    row: int
    stale_owner: str
    stale_token: str
    stale_run_id: str
    fencing_token: str
    recovered_by: str
    run_id: str
    head: str
    metadata_id: int
    stale_metadata_value: str
    target_state: str
    requests: tuple
    stale_executor_metadata_id: int = 0
    stale_executor_metadata_value: str = ""


def build_stale_lease_recovery(
    commercial_rows, runtime_rows, *,
    task_id, recovered_by, run_id, head, now, reason,
    captured_metadata, captured_executor_metadata=None,
    target_state=None,
    runtime_sheet_id, events_sheet_id,
    expected_count=None, fencing_token=None,
):
    """Safely recover an expired lease with transactional fencing.

    Requires captured_metadata with exact metadataValue to prevent deleting
    a renewed lease. Generates a new fencing_token that permanently locks out
    the stale owner from subsequent writes.
    """
    if not isinstance(recovered_by, str) or not _OWNER.fullmatch(recovered_by):
        raise UnsafeSnapshot("INVALID_RECOVERER")
    if recovered_by not in CANONICAL_AUTOMATION_IDS:
        raise UnsafeSnapshot("UNKNOWN_CANONICAL_EXECUTOR")
    if not _SHA.fullmatch(head or ""):
        raise UnsafeSnapshot("INVALID_HEAD")
    _clean(run_id, "RUN_ID")
    _clean(task_id, "TASK_ID")
    _clean(reason, "REASON", 1200)
    if not isinstance(runtime_sheet_id, int) or not isinstance(events_sheet_id, int):
        raise UnsafeSnapshot("INVALID_SHEET_ID")
    if expected_count is not None:
        count = len(commercial_rows) - 1
        if count != expected_count:
            raise UnsafeSnapshot("COUNT_MISMATCH")

    if not isinstance(captured_metadata, dict):
        raise UnsafeSnapshot("CAPTURED_METADATA_REQUIRED")

    order = None
    for r in commercial_rows[1:]:
        if r[0] == task_id:
            order = int(r[4])
            break
    if order is None:
        raise UnsafeSnapshot(f"TASK_NOT_FOUND_IN_COMMERCIAL:{task_id}")

    row_idx = None
    target_row = None
    for idx, r in enumerate(runtime_rows[1:], start=1):
        if r[0] == task_id:
            row_idx = idx
            target_row = r
            break
    if row_idx is None or target_row is None:
        raise UnsafeSnapshot(f"TASK_NOT_FOUND_IN_RUNTIME:{task_id}")

    current_state = target_row[1] if len(target_row) > 1 else ""
    stale_owner = target_row[2] if len(target_row) > 2 else ""
    lease_status = target_row[3] if len(target_row) > 3 else ""

    if current_state == "LISTO":
        raise UnsafeSnapshot("CANNOT_RECOVER_COMPLETED_TASK")
    if current_state == "PENDIENTE":
        raise UnsafeSnapshot("TASK_ALREADY_PENDIENTE")
    if current_state == "BLOQUEADO":
        raise UnsafeSnapshot("CANNOT_RECOVER_BLOCKED_TASK")

    if current_state == "VALIDANDO":
        if target_state is not None and target_state != "VALIDANDO":
            raise UnsafeSnapshot("CANNOT_DOWNGRADE_VALIDANDO_TO_PENDIENTE")
        target_state = "VALIDANDO"
    else:
        target_state = "PENDIENTE" if target_state is None else target_state

    lease_info = _parse_lease(lease_status)
    if not lease_info.get("_prefix", "").startswith("ACTIVE"):
        raise UnsafeSnapshot(f"LEASE_NOT_ACTIVE:{lease_status}")

    stale_token = lease_info.get("TOKEN", "")
    if not stale_token or len(stale_token) < 16:
        raise UnsafeSnapshot("MISSING_OR_INVALID_LEASE_TOKEN")
    stale_run_id = lease_info.get("RUN_ID", "")
    if not stale_run_id:
        raise UnsafeSnapshot("MISSING_LEASE_RUN_ID")

    expires_str = lease_info.get("EXPIRES")
    if not expires_str or not isinstance(expires_str, str):
        raise UnsafeSnapshot("MISSING_LEASE_EXPIRATION")
    try:
        expires_dt = datetime.fromisoformat(expires_str.replace("Z", "+00:00"))
    except (ValueError, TypeError) as exc:
        raise UnsafeSnapshot(f"MALFORMED_LEASE_EXPIRATION:{expires_str}") from exc
    if now <= expires_dt:
        raise UnsafeSnapshot(f"LEASE_NOT_EXPIRED:{expires_str}")

    metadata_id = METADATA_BASE + order
    if (captured_metadata.get("metadataId") != metadata_id
            or captured_metadata.get("metadataKey") != METADATA_KEY):
        raise UnsafeSnapshot("STALE_METADATA_ID_MISMATCH")
    stale_meta_val = captured_metadata.get("metadataValue", "")
    if not stale_meta_val:
        raise UnsafeSnapshot("CAPTURED_METADATA_VALUE_REQUIRED")
    if stale_token not in stale_meta_val or stale_owner not in stale_meta_val:
        raise UnsafeSnapshot("METADATA_VALUE_DOES_NOT_MATCH_RUNTIME_ROW")

    stale_exec_id = 0
    stale_exec_val = ""
    if stale_owner and _OWNER.fullmatch(stale_owner):
        stale_exec_id = _executor_metadata_id(stale_owner)
        if captured_executor_metadata and isinstance(captured_executor_metadata, dict):
            if (captured_executor_metadata.get("metadataId") == stale_exec_id
                    and captured_executor_metadata.get("metadataKey") == EXECUTOR_METADATA_KEY):
                stale_exec_val = captured_executor_metadata.get("metadataValue", "")

    fence = fencing_token or secrets.token_hex(16)
    stamp = _stamp(now)
    fenced_lease = (
        f"RECOVERED|STALE_OWNER={stale_owner}|STALE_TOKEN={stale_token}|"
        f"FENCING_TOKEN={fence}|RECOVERED_BY={recovered_by}|"
        f"RECOVERED_AT={stamp}|HEAD={head}"
    )

    requests = []
    task_lookup = {
        "metadataId": metadata_id,
        "metadataKey": METADATA_KEY,
        "metadataValue": stale_meta_val,
        "visibility": "DOCUMENT",
    }
    requests.append({"deleteDeveloperMetadata": {"dataFilter": {"developerMetadataLookup": task_lookup}}})

    if stale_exec_id:
        exec_lookup = {
            "metadataId": stale_exec_id,
            "metadataKey": EXECUTOR_METADATA_KEY,
            "visibility": "DOCUMENT",
        }
        if stale_exec_val:
            exec_lookup["metadataValue"] = stale_exec_val
        requests.append({"deleteDeveloperMetadata": {"dataFilter": {"developerMetadataLookup": exec_lookup}}})

    next_action = f"LEASE_RECUPERADO; DISPATCH_ELEGIBLE (FENCING={fence[:8]})"
    audit_note = f"RECOVERED by={recovered_by} stale_owner={stale_owner} stale_token={stale_token} reason={reason}"
    requests.extend([
        _cell_range(runtime_sheet_id, row_idx, 1, [target_state, "", fenced_lease, head]),
        _cell_range(runtime_sheet_id, row_idx, 7, [next_action, stamp]),
        _cell_range(runtime_sheet_id, row_idx, 16, [audit_note]),
        _event(events_sheet_id, [
            stamp, recovered_by, task_id, "LEASE_STALE_RECOVERED",
            current_state, target_state, head, f"FENCED_RECOVERY|STALE_OWNER={stale_owner}|STALE_TOKEN={stale_token}",
            "FENCED_LEASE_RECOVERY", f"Reason: {reason} | Fencing: {fence}",
            run_id, fence,
        ]),
    ])

    return StaleLeaseRecovery(
        task_id=task_id, order=order, row=row_idx, stale_owner=stale_owner,
        stale_token=stale_token, stale_run_id=stale_run_id, fencing_token=fence,
        recovered_by=recovered_by, run_id=run_id, head=head, metadata_id=metadata_id,
        stale_metadata_value=stale_meta_val, target_state=target_state,
        requests=tuple(requests), stale_executor_metadata_id=stale_exec_id,
        stale_executor_metadata_value=stale_exec_val,
    )


def verify_stale_lease_recovery_response(recovery, api_response, fresh_runtime_row):
    """Verify that stale DeveloperMetadata was deleted and runtime readback matches fenced recovery."""
    try:
        deleted_task = api_response["replies"][0]["deleteDeveloperMetadata"]["deletedDeveloperMetadata"]
    except (KeyError, IndexError, TypeError) as exc:
        raise UnsafeSnapshot("RECOVERY_API_ERROR") from exc
    if len(deleted_task) != 1 or deleted_task[0].get("metadataId") != recovery.metadata_id:
        raise UnsafeSnapshot("RECOVERY_STALE_LOCK_NOT_MATCHED")
    if recovery.stale_metadata_value and deleted_task[0].get("metadataValue") != recovery.stale_metadata_value:
        raise UnsafeSnapshot("RECOVERY_STALE_METADATA_VALUE_MISMATCH")
    if recovery.stale_executor_metadata_id:
        try:
            deleted_exec = api_response["replies"][1]["deleteDeveloperMetadata"]["deletedDeveloperMetadata"]
            if len(deleted_exec) != 1 or deleted_exec[0].get("metadataId") != recovery.stale_executor_metadata_id:
                raise UnsafeSnapshot("RECOVERY_STALE_EXECUTOR_LOCK_NOT_MATCHED")
        except (KeyError, IndexError, TypeError) as exc:
            raise UnsafeSnapshot("RECOVERY_EXECUTOR_API_ERROR") from exc
    if (fresh_runtime_row[0] != recovery.task_id
            or fresh_runtime_row[1] != recovery.target_state
            or fresh_runtime_row[2] != ""
            or not fresh_runtime_row[3].startswith("RECOVERED|")
            or f"FENCING_TOKEN={recovery.fencing_token}" not in fresh_runtime_row[3]
            or f"STALE_TOKEN={recovery.stale_token}" not in fresh_runtime_row[3]):
        raise UnsafeSnapshot("RECOVERY_RUNTIME_READBACK_MISMATCH")
    return True


def build_takeover_claim(
    commercial_rows, runtime_rows, *,
    task_id, new_owner, run_id, head, now, reason,
    captured_metadata, captured_executor_metadata=None,
    runtime_sheet_id, events_sheet_id,
    expected_count=None,
):
    """Safely take over an expired lease atomically with dual locks and fencing.

    1. Deletes stale metadata for task and stale executor matching exact captured metadata.
    2. Creates new DeveloperMetadata for task AND new executor lock for new_owner.
    3. Enforces new_owner WIP.
    4. Enforces review vs builder takeover according to current task state.
    """
    if not isinstance(new_owner, str) or not _OWNER.fullmatch(new_owner):
        raise UnsafeSnapshot("INVALID_OWNER")
    if new_owner not in CANONICAL_AUTOMATION_IDS:
        raise UnsafeSnapshot("UNKNOWN_CANONICAL_EXECUTOR")
    if not _SHA.fullmatch(head or ""):
        raise UnsafeSnapshot("INVALID_HEAD")
    _clean(run_id, "RUN_ID")
    _clean(task_id, "TASK_ID")
    _clean(reason, "REASON", 1200)
    if not isinstance(runtime_sheet_id, int) or not isinstance(events_sheet_id, int):
        raise UnsafeSnapshot("INVALID_SHEET_ID")
    if expected_count is not None:
        count = len(commercial_rows) - 1
        if count != expected_count:
            raise UnsafeSnapshot("COUNT_MISMATCH")
    if not isinstance(captured_metadata, dict):
        raise UnsafeSnapshot("CAPTURED_METADATA_REQUIRED")

    _check_executor_wip(runtime_rows, new_owner, exempt_task_id=task_id)

    order = None
    for r in commercial_rows[1:]:
        if r[0] == task_id:
            order = int(r[4])
            break
    if order is None:
        raise UnsafeSnapshot(f"TASK_NOT_FOUND_IN_COMMERCIAL:{task_id}")

    row_idx = None
    target_row = None
    for idx, r in enumerate(runtime_rows[1:], start=1):
        if r[0] == task_id:
            row_idx = idx
            target_row = r
            break
    if row_idx is None or target_row is None:
        raise UnsafeSnapshot(f"TASK_NOT_FOUND_IN_RUNTIME:{task_id}")

    current_state = target_row[1] if len(target_row) > 1 else ""
    stale_owner = target_row[2] if len(target_row) > 2 else ""
    lease_status = target_row[3] if len(target_row) > 3 else ""

    if current_state == "LISTO":
        raise UnsafeSnapshot("CANNOT_TAKEOVER_COMPLETED_TASK")
    if current_state == "BLOQUEADO":
        raise UnsafeSnapshot("CANNOT_TAKEOVER_BLOCKED_TASK")

    deps_ok = target_row[10] if len(target_row) > 10 else ""
    if deps_ok != "SI":
        raise UnsafeSnapshot(f"TAKEOVER_DEPENDENCIES_NOT_SATISFIED:{task_id}")

    lease_info = _parse_lease(lease_status)
    stale_token = lease_info.get("TOKEN", "")
    expires_str = lease_info.get("EXPIRES")
    if not expires_str or not isinstance(expires_str, str):
        raise UnsafeSnapshot("MISSING_LEASE_EXPIRATION")
    try:
        expires_dt = datetime.fromisoformat(expires_str.replace("Z", "+00:00"))
    except (ValueError, TypeError) as exc:
        raise UnsafeSnapshot(f"MALFORMED_LEASE_EXPIRATION:{expires_str}") from exc
    if now <= expires_dt:
        raise UnsafeSnapshot(f"LEASE_NOT_EXPIRED:{expires_str}")

    metadata_id = METADATA_BASE + order
    if (captured_metadata.get("metadataId") != metadata_id
            or captured_metadata.get("metadataKey") != METADATA_KEY):
        raise UnsafeSnapshot("TAKEOVER_STALE_METADATA_MISMATCH")
    stale_meta_val = captured_metadata.get("metadataValue", "")

    token = secrets.token_hex(16)
    acquired = _stamp(now)
    expires = _stamp(now + timedelta(minutes=TTL_MINUTES))

    stale_exec_id = 0
    stale_exec_val = ""
    if stale_owner and _OWNER.fullmatch(stale_owner):
        stale_exec_id = _executor_metadata_id(stale_owner)
        if captured_executor_metadata and isinstance(captured_executor_metadata, dict):
            stale_exec_val = captured_executor_metadata.get("metadataValue", "")

    requests = []
    task_del_lookup = {"metadataId": metadata_id, "metadataKey": METADATA_KEY, "visibility": "DOCUMENT"}
    if stale_meta_val:
        task_del_lookup["metadataValue"] = stale_meta_val
    requests.append({"deleteDeveloperMetadata": {"dataFilter": {"developerMetadataLookup": task_del_lookup}}})

    if stale_exec_id:
        exec_del_lookup = {"metadataId": stale_exec_id, "metadataKey": EXECUTOR_METADATA_KEY, "visibility": "DOCUMENT"}
        if stale_exec_val:
            exec_del_lookup["metadataValue"] = stale_exec_val
        requests.append({"deleteDeveloperMetadata": {"dataFilter": {"developerMetadataLookup": exec_del_lookup}}})

    if current_state == "VALIDANDO":
        author = lease_info.get("ORIGINAL_AUTHOR") or stale_owner
        if new_owner == author:
            raise UnsafeSnapshot("SELF_REVIEW_FORBIDDEN")
        new_target_state = "VALIDANDO"
        new_meta_val = "|".join([task_id, run_id, new_owner, token, acquired, expires, author])
        new_lease = (
            f"ACTIVE_REVIEW|OWNER={new_owner}|ORIGINAL_AUTHOR={author}|TOKEN={token}|"
            f"RUN_ID={run_id}|START={acquired}|EXPIRES={expires}|HEAD={head}|TAKEOVER_FROM={stale_owner}"
        )
        event_name = "REVIEW_TAKEOVER_CLAIMED"
        event_status = "REVIEW_RESERVED_VERIFICATION_PENDING"
    else:
        new_target_state = "EN_PROGRESO"
        new_meta_val = f"{task_id}|{run_id}|{new_owner}|{token}|{acquired}|{expires}"
        new_lease = (
            f"ACTIVE|OWNER={new_owner}|TOKEN={token}|"
            f"RUN_ID={run_id}|START={acquired}|EXPIRES={expires}|HEAD={head}|TAKEOVER_FROM={stale_owner}"
        )
        event_name = "LEASE_TAKEOVER_CLAIMED"
        event_status = "LEASE_RESERVED_MATERIAL_PENDING"

    requests.append({
        "createDeveloperMetadata": {
            "developerMetadata": {
                "metadataId": metadata_id,
                "metadataKey": METADATA_KEY,
                "metadataValue": new_meta_val,
                "visibility": "DOCUMENT",
                "location": {
                    "dimensionRange": {
                        "sheetId": runtime_sheet_id,
                        "dimension": "ROWS",
                        "startIndex": row_idx,
                        "endIndex": row_idx + 1,
                    }
                },
            }
        }
    })

    new_exec_id = _executor_metadata_id(new_owner)
    new_exec_val = f"{new_owner}|{task_id}|{token}|{acquired}|{expires}"
    requests.append({
        "createDeveloperMetadata": {
            "developerMetadata": {
                "metadataId": new_exec_id,
                "metadataKey": EXECUTOR_METADATA_KEY,
                "metadataValue": new_exec_val,
                "visibility": "DOCUMENT",
                "location": {"spreadsheet": True},
            }
        }
    })

    requests.extend([
        _cell_range(runtime_sheet_id, row_idx, 1, [new_target_state, new_owner, new_lease, head]),
        _cell_range(runtime_sheet_id, row_idx, 8, [acquired]),
        _event(events_sheet_id, [
            acquired, new_owner, task_id, event_name,
            current_state, new_target_state, head, f"TAKEOVER_FROM_{stale_owner}",
            event_status, f"Takeover reason: {reason}",
            run_id, token,
        ]),
    ])

    return Claim(
        task_id=task_id, order=order, row=row_idx, owner=new_owner,
        token=token, run_id=run_id, head=head, metadata_id=metadata_id,
        metadata_value=new_meta_val, acquired_at=acquired, expires_at=expires,
        requests=tuple(requests), executor_metadata_id=new_exec_id,
        executor_metadata_value=new_exec_val,
    )


def verify_takeover_claim_response(claim, api_response, runtime_row, has_stale_executor=True):
    """Verify response for takeover: stale deleted, new created, and readback verified."""
    replies = api_response.get("replies", [])
    try:
        deleted_task = replies[0]["deleteDeveloperMetadata"]["deletedDeveloperMetadata"]
        if len(deleted_task) != 1 or deleted_task[0].get("metadataId") != claim.metadata_id:
            raise UnsafeSnapshot("TAKEOVER_STALE_TASK_DELETE_MISMATCH")
        idx = 1
        if has_stale_executor:
            deleted_exec = replies[idx]["deleteDeveloperMetadata"]["deletedDeveloperMetadata"]
            if len(deleted_exec) != 1:
                raise UnsafeSnapshot("TAKEOVER_STALE_EXEC_DELETE_MISMATCH")
            idx += 1
        created_task = replies[idx]["createDeveloperMetadata"]["developerMetadata"]
        created_exec = replies[idx + 1]["createDeveloperMetadata"]["developerMetadata"]
    except (KeyError, IndexError, TypeError) as exc:
        raise UnsafeSnapshot("NO_TAKEOVER_CAS_CONFIRMATION") from exc

    if (created_task.get("metadataId") != claim.metadata_id
            or created_task.get("metadataValue") != claim.metadata_value):
        raise UnsafeSnapshot("TAKEOVER_METADATA_MISMATCH")
    if (created_exec.get("metadataId") != claim.executor_metadata_id
            or created_exec.get("metadataValue") != claim.executor_metadata_value):
        raise UnsafeSnapshot("TAKEOVER_EXECUTOR_METADATA_MISMATCH")
    if (runtime_row[0] != claim.task_id
            or runtime_row[2] != claim.owner
            or f"TOKEN={claim.token}" not in runtime_row[3]
            or f"RUN_ID={claim.run_id}" not in runtime_row[3]):
        raise UnsafeSnapshot("TAKEOVER_RUNTIME_READBACK_MISMATCH")
    return True
