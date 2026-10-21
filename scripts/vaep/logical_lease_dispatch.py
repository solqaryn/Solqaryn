"""Deterministic alternative to the Sheets DeveloperMetadata CAS writer.

A scope has exactly one designated writer in a frozen routing epoch. This is
read-only policy/planning code, not an authenticated Task, a lock, or a proof
of GitHub write access. The runtime must check real Task-origin write proof,
prior-run termination, sheet readback, and authorized mode before writing.
"""
from dataclasses import dataclass
from datetime import datetime, timedelta, timezone
import re
import secrets

from commercial_dispatch import UnsafeSnapshot, snapshot, audit_wip_violations

CANONICAL_EXECUTORS = (
    "6ad7bbf79c488191bf893e6c08ea9b5f",  # Primary :00
    "6abd62255ae88191a2dba6e1b00d3b4d",  # Supervisor :05
    "6ad7bc0d48748191aa1dd0ce2f18d22a",  # Primary :12
    "6abd6235e100819195785fc76a44a8e0",  # Supervisor :17
    "6ad7bc1cc7b8819190665e5ed78610d4",  # Primary :24
    "6abd6241c3e48191a8856ed7b0f42c5c",  # Supervisor :29
    "6ad7bc3001b88191add05d9b4d5a4130",  # Primary :36
    "6abd624e5e108191bef6bb879559328c",  # Supervisor :41
    "6ad7bc3ebea08191a16dc7c9f1f3303b",  # Primary :48
    "6abd625b397c8191a49904227946a20f",  # Supervisor :53
)
SHA_RE = re.compile(r"^[a-f0-9]{40}$")
ID_RE = re.compile(r"^[a-f0-9]{32}$")


def freeze_pool(writer_ids, epoch):
    """Require explicit stable order and epoch; never infer rights from a chat."""
    if not isinstance(epoch, str) or not re.fullmatch(r"VAEP_LOGICAL_[A-Z0-9_]{8,64}", epoch):
        raise UnsafeSnapshot("LOGICAL_ROUTING_EPOCH_NOT_FROZEN")
    if (not isinstance(writer_ids, (tuple, list)) or not writer_ids
            or len(set(writer_ids)) != len(writer_ids)
            or any(x not in CANONICAL_EXECUTORS for x in writer_ids)):
        raise UnsafeSnapshot("INVALID_VERIFIED_WRITER_POOL")
    pool = tuple(x for x in CANONICAL_EXECUTORS if x in writer_ids)
    if tuple(writer_ids) != pool:
        raise UnsafeSnapshot("WRITER_POOL_ORDER_MISMATCH")
    return pool


def writer_for_order(order, writer_ids, epoch):
    pool = freeze_pool(writer_ids, epoch)
    if not isinstance(order, int) or isinstance(order, bool) or order <= 0:
        raise UnsafeSnapshot("INVALID_LOGICAL_ORDER")
    return pool[(order - 1) % len(pool)]


def reviewer_for_order(order, writer_ids, epoch):
    pool = freeze_pool(writer_ids, epoch)
    if len(pool) < 2:
        raise UnsafeSnapshot("INDEPENDENT_REVIEWER_NOT_AVAILABLE")
    return pool[order % len(pool)]


def select(commercial_rows, runtime_rows, *, actor_id, writer_ids, epoch,
           expected_count=1123):
    """Select only scopes owned by this actor's frozen deterministic lane.

    No dynamic fallback to another actor: cutover/rebalance requires zero
    unreleased scopes and a NEW globally approved frozen epoch.
    """
    pool = freeze_pool(writer_ids, epoch)
    tasks = snapshot(commercial_rows, runtime_rows, expected_count)
    if actor_id not in pool:
        return {"action": "NO_GITHUB_TASK_WRITE_PROOF", "selected": None}
    duplicate = audit_wip_violations(runtime_rows)
    if duplicate:
        raise UnsafeSnapshot("DUPLICATE_ACTIVE_WIP")
    active = [t for t in tasks.values()
              if t["owner"] == actor_id
              and t["state"] in ("EN_PROGRESO", "VALIDANDO")
              and not t["lease"].startswith("RELEASED")]
    if active:
        return {"action": "CONTINUE_OWNED_OR_RECOVER", "selected": min(
            active, key=lambda x: x["order"])["id"]}
    for t in sorted(tasks.values(), key=lambda x: x["order"]):
        if t["state"] != "PENDIENTE" or t["owner"]:
            continue
        if t["lease"] and not t["lease"].startswith(("RELEASED", "RECOVERED")):
            continue
        if not all(tasks[d]["state"] == "LISTO" for d in t["dependencies"]):
            continue
        if writer_for_order(t["order"], pool, epoch) == actor_id:
            return {"action": "BUILD", "selected": t["id"], "order": t["order"]}
    return {"action": "NO_ELIGIBLE_SCOPE_FOR_DESIGNATED_WRITER", "selected": None}


def _cell(value):
    return {"userEnteredValue": {"stringValue": str(value)}}


def _write_range(sheet_id, row, col, values):
    return {"updateCells": {
        "range": {"sheetId": sheet_id, "startRowIndex": row,
                  "endRowIndex": row + 1, "startColumnIndex": col,
                  "endColumnIndex": col + len(values)},
        "rows": [{"values": [_cell(v) for v in values]}],
        "fields": "userEnteredValue"}}


@dataclass(frozen=True)
class LogicalClaim:
    task_id: str
    actor_id: str
    run_id: str
    head: str
    token: str
    lease: str
    requests: tuple


def build_logical_claim(commercial_rows, runtime_rows, *, task_id, actor_id,
                        writer_ids, epoch, run_id, head, now, runtime_sheet_id,
                        events_sheet_id, previous_run_terminal_proven=False,
                        expected_count=1123):
    """Prepare 3 simple Sheets requests, WITHOUT DeveloperMetadata CAS.

    This is NOT a live claim. Caller must run one authorized batch, then
    verify the response and exact owner/run/token in fresh _RUNTIME_SOURCE.
    """
    if not previous_run_terminal_proven:
        raise UnsafeSnapshot("PRIOR_SCHEDULED_RUN_TERMINAL_PROOF_REQUIRED")
    if not isinstance(now, datetime) or now.tzinfo is None:
        raise UnsafeSnapshot("UTC_TIMESTAMP_REQUIRED")
    if not isinstance(head, str) or not SHA_RE.fullmatch(head):
        raise UnsafeSnapshot("INVALID_HEAD")
    if not isinstance(run_id, str) or not re.fullmatch(r"[A-Za-z0-9_.:-]{8,128}", run_id):
        raise UnsafeSnapshot("INVALID_RUN_ID")
    if actor_id not in CANONICAL_EXECUTORS or not ID_RE.fullmatch(actor_id):
        raise UnsafeSnapshot("INVALID_ACTOR")
    if not isinstance(runtime_sheet_id, int) or not isinstance(events_sheet_id, int):
        raise UnsafeSnapshot("INVALID_SHEET_ID")
    decision = select(commercial_rows, runtime_rows, actor_id=actor_id,
                      writer_ids=writer_ids, epoch=epoch, expected_count=expected_count)
    if decision.get("action") != "BUILD" or decision.get("selected") != task_id:
        raise UnsafeSnapshot("LOGICAL_CLAIM_NOT_ADMISSIBLE")
    row = next((i for i, r in enumerate(runtime_rows) if i and r[0] == task_id), None)
    if row is None:
        raise UnsafeSnapshot("TASK_NOT_FOUND")
    now = now.astimezone(timezone.utc)
    stamp = now.isoformat(timespec="milliseconds").replace("+00:00", "Z")
    expiry = (now + timedelta(minutes=10)).isoformat(timespec="milliseconds").replace("+00:00", "Z")
    token = secrets.token_hex(16)
    lease = (f"ACTIVE|OWNER={actor_id}|TOKEN={token}|RUN_ID={run_id}|"
             f"START={stamp}|EXPIRES={expiry}|HEAD={head}|ROUTING_EPOCH={epoch}")
    event = [stamp, actor_id, task_id, "LOGICAL_LEASE_ACQUIRED",
             "PENDIENTE", "EN_PROGRESO", head, "",
             "LEASE_RESERVED_MATERIAL_PENDING", "LOGICAL_OWNER_WIP1_NO_CAS",
             run_id, token]
    requests = (
        _write_range(runtime_sheet_id, row, 1, ["EN_PROGRESO", actor_id, lease, head]),
        _write_range(runtime_sheet_id, row, 8, [stamp]),
        {"appendCells": {"sheetId": events_sheet_id,
                         "rows": [{"values": [_cell(v) for v in event]}],
                         "fields": "userEnteredValue"}},
    )
    return LogicalClaim(task_id, actor_id, run_id, head, token, lease, requests)


def verify_logical_claim(claim, api_response, runtime_row):
    """Provider acknowledgment AND readback are necessary, not sufficient.

    Runtime must recheck epoch, liveness and actual GitHub permissions before
    edits. Readback alone cannot prove absence of a concurrent same-actor run.
    """
    if not isinstance(api_response, dict) or len(api_response.get("replies", [])) != 3:
        raise UnsafeSnapshot("LOGICAL_SHEETS_BATCH_NOT_CONFIRMED")
    if (len(runtime_row) < 4 or runtime_row[0] != claim.task_id
            or runtime_row[1] != "EN_PROGRESO"
            or runtime_row[2] != claim.actor_id
            or runtime_row[3] != claim.lease):
        raise UnsafeSnapshot("LOGICAL_OWNER_TOKEN_READBACK_MISMATCH")
    return True
