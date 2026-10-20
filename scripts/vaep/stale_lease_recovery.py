"""Recovery of SOLQARYN stale Google Sheets leases; never a blind expiry reset.

Builds a single atomic batch to delete precisely the captured DeveloperMetadata
and mark that same task PENDIENTE (implementation) or VALIDANDO (review).
Does not access network or prove that an invocation has ended. The caller
must obtain a separate, authoritative invocation liveness check and capture
exact matching metadata; expiry alone is NOT safe fencing.
"""
from datetime import datetime, timedelta, timezone
import re

from commercial_dispatch import UnsafeSnapshot

METADATA_BASE = 1_600_000_000
METADATA_KEY = "SOLQARYN_TASK_LEASE"
_OWNER = re.compile(r"^[0-9a-f]{32}$")
_HEAD = re.compile(r"^[0-9a-f]{40}$")


def _utc(timestamp):
    if not isinstance(timestamp, datetime) or timestamp.tzinfo is None:
        raise UnsafeSnapshot("AWARE_DATETIME_REQUIRED")
    return timestamp.astimezone(timezone.utc)


def _parse_iso(text):
    try:
        if not isinstance(text, str) or not text.endswith("Z"):
            raise ValueError()
        return datetime.fromisoformat(text[:-1] + "+00:00")
    except (ValueError, TypeError):
        raise UnsafeSnapshot("INVALID_LEASE_TIMESTAMP")


def _cell(value):
    return {"userEnteredValue": {"stringValue": str(value)}}


def _patch(sheet_id, row_index, column, cells):
    return {"updateCells": {
        "range": {"sheetId": sheet_id, "startRowIndex": row_index,
                  "endRowIndex": row_index+1, "startColumnIndex": column,
                  "endColumnIndex": column+len(cells)},
        "rows": [{"values": [_cell(v) for v in cells]}],
        "fields": "userEnteredValue"}}


def build_stale_recovery(*, runtime_row, metadata, order, row_index,
                         now, authoritative_run_ended,
                         no_new_material_progress, runtime_sheet_id,
                         events_sheet_id, recovery_actor, head,
                         expected_last_event_at):
    """Construct a fenced, compare-by-metadata recovery; never executes it.

    Before submitting, caller MUST re-read row, metadata and event head, and
    re-check live invocation; any change invalidates this candidate. Must not
    be called while a worker may still be writing. A stale lease is necessary
    but never sufficient for takeover.
    """
    now = _utc(now)
    if authoritative_run_ended is not True or no_new_material_progress is not True:
        raise UnsafeSnapshot("INVOCATION_OR_MATERIAL_NOT_PROVEN_ENDED")
    if not isinstance(order, int) or not 1 <= order <= 1123:
        raise UnsafeSnapshot("INVALID_TASK_ORDER")
    if not isinstance(row_index, int) or row_index < 1:
        raise UnsafeSnapshot("INVALID_RUNTIME_ROW")
    if not all(isinstance(v, int) for v in [runtime_sheet_id, events_sheet_id]):
        raise UnsafeSnapshot("INVALID_SHEET_ID")
    if not _OWNER.fullmatch(recovery_actor or "") or not _HEAD.fullmatch(head or ""):
        raise UnsafeSnapshot("INVALID_RECOVERY_IDENTITY")
    if len(runtime_row) < 10:
        raise UnsafeSnapshot("INCOMPLETE_RUNTIME_ROW")
    task_id, state, owner, lease = runtime_row[:4]
    if state not in ("EN_PROGRESO", "VALIDANDO") or not _OWNER.fullmatch(owner or ""):
        raise UnsafeSnapshot("NOT_AN_OWNED_OPEN_SCOPE")
    if state == "VALIDANDO" and not lease.startswith("ACTIVE_REVIEW|"):
        raise UnsafeSnapshot("INVALID_REVIEW_LEASE")
    if state == "EN_PROGRESO" and not lease.startswith("ACTIVE|"):
        raise UnsafeSnapshot("INVALID_IMPLEMENTATION_LEASE")
    if f"OWNER={owner}" not in lease or "TOKEN=" not in lease or "EXPIRES=" not in lease:
        raise UnsafeSnapshot("INVALID_RUNTIME_LEASE")
    if metadata.get("metadataId") != METADATA_BASE + order or metadata.get("metadataKey") != METADATA_KEY or metadata.get("visibility") != "DOCUMENT":
        raise UnsafeSnapshot("METADATA_IDENTITY_MISMATCH")
    if metadata.get("location", {}).get("dimensionRange", {}).get("startIndex") != row_index:
        raise UnsafeSnapshot("METADATA_ROW_MISMATCH")
    val = metadata.get("metadataValue")
    fields = val.split("|") if isinstance(val, str) else []
    if len(fields) not in (6,7) or fields[0] != task_id or fields[2] != owner:
        raise UnsafeSnapshot("METADATA_VALUE_MISMATCH")
    token, acquired, expires = fields[3:6]
    if not token or f"TOKEN={token}" not in lease or f"EXPIRES={expires}" not in lease:
        raise UnsafeSnapshot("METADATA_TOKEN_MISMATCH")
    if _parse_iso(expires) + timedelta(seconds=60) >= now:
        raise UnsafeSnapshot("LEASE_NOT_OLD_ENOUGH")
    last_seen = _parse_iso(runtime_row[8])
    event_seen = _parse_iso(expected_last_event_at)
    if last_seen > _parse_iso(expires) or event_seen > _parse_iso(expires):
        raise UnsafeSnapshot("PROGRESS_AFTER_EXPIRY_REQUIRES_INVESTIGATION")
    if state == "VALIDANDO" and len(fields) != 7:
        raise UnsafeSnapshot("REVIEW_AUTHOR_NOT_PRESERVED")
    timestamp = now.isoformat(timespec="milliseconds").replace("+00:00","Z")
    # A recovered review must preserve the original implementer identity.
    # Otherwise a later review can mistake the prior reviewer for the author.
    original_author = fields[6] if state == "VALIDANDO" else owner
    if not _OWNER.fullmatch(original_author):
        raise UnsafeSnapshot("INVALID_ORIGINAL_AUTHOR")
    released = (f"RELEASED_STALE|OWNER={owner}|ORIGINAL_AUTHOR={original_author}|TOKEN={token}|"
                f"RELEASED_AT={timestamp}|HEAD={head}|REASON=INVOCATION_ENDED")
    # Recovery does not certify anything. VALIDANDO preserves original evidence
    # and author. EN_PROGRESO returns to PENDIENTE for safe future claim.
    new_state = "VALIDANDO" if state == "VALIDANDO" else "PENDIENTE"
    marker = {"metadataId": metadata["metadataId"],
              "metadataKey": METADATA_KEY, "metadataValue": val,
              "visibility": "DOCUMENT"}
    desc = ("Expired CAS lock recovered after independent run-ended attestation; "
            "no microtask certified; original evidence preserved")
    event = [timestamp, recovery_actor, task_id, "STALE_LOCK_RECOVERED",
             state, new_state, head, runtime_row[5] or "",
             "SCOPE_REAVAILABLE_NO_CERTIFICATION", desc,
             f"RECOVERY-{task_id}-{order}", token]
    return (
        {"deleteDeveloperMetadata": {"dataFilter": {"developerMetadataLookup": marker}}},
        _patch(runtime_sheet_id, row_index, 1, [new_state, "", released, head]),
        _patch(runtime_sheet_id, row_index, 7, ["RECLAIM_WITH_NEW_CAS_BEFORE_WRITE", timestamp]),
        {"appendCells": {"sheetId": events_sheet_id,
                         "rows": [{"values": [_cell(v) for v in event]}],
                         "fields": "userEnteredValue"}},
    )


def verify_stale_recovery(response, runtime_row, original_metadata_id):
    try:
        deleted = response["replies"][0]["deleteDeveloperMetadata"]["deletedDeveloperMetadata"]
    except (KeyError, TypeError, IndexError):
        raise UnsafeSnapshot("RECOVERY_DELETION_NOT_CONFIRMED")
    if len(deleted) != 1 or deleted[0].get("metadataId") != original_metadata_id:
        raise UnsafeSnapshot("WRONG_STALE_LOCK_DELETED")
    if runtime_row[1] not in ("PENDIENTE", "VALIDANDO") or runtime_row[2] or not runtime_row[3].startswith("RELEASED_STALE|"):
        raise UnsafeSnapshot("STALE_RECOVERY_READBACK_MISMATCH")
    return True
