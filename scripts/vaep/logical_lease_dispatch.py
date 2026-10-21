"""Deterministic alternative to the Sheets DeveloperMetadata CAS writer.

A scope has exactly one designated writer in a frozen routing epoch. This is
read-only policy/planning code, not an authenticated Task, a lock, or a proof
of GitHub write access. The runtime must check real Task-origin write proof,
prior-run termination, sheet readback, and authorized mode before writing.
"""
from dataclasses import dataclass, replace
from datetime import datetime, timedelta, timezone
import hashlib
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
    # Stable two-order buckets avoid starvation when current plan's eligible
    # scopes are overwhelmingly odd-numbered (A/B ordered work pairs).
    # This uses the permanent ORDER, never a live available-list position.
    return pool[((order - 1) // 2) % len(pool)]


def reviewer_for_order(order, writer_ids, epoch):
    pool = freeze_pool(writer_ids, epoch)
    if len(pool) < 2:
        raise UnsafeSnapshot("INDEPENDENT_REVIEWER_NOT_AVAILABLE")
    return pool[(((order - 1) // 2) + 1) % len(pool)]


def reviewer_for_original_author(order, writer_ids, epoch, original_author):
    """Route exactly one independent reviewer, including historic handoffs.

    A handoff may have an author other than the writer assigned in the current
    frozen epoch. Skip that author instead of leaving VALIDANDO stranded.
    The routing is deterministic and uses only the frozen writer cohort.
    """
    pool = freeze_pool(writer_ids, epoch)
    if not isinstance(original_author, str) or not ID_RE.fullmatch(original_author):
        raise UnsafeSnapshot("INVALID_ORIGINAL_AUTHOR")
    if len(pool) < 2:
        raise UnsafeSnapshot("INDEPENDENT_REVIEWER_NOT_AVAILABLE")
    start = (((order - 1) // 2) + 1) % len(pool)
    for offset in range(len(pool)):
        candidate = pool[(start + offset) % len(pool)]
        if candidate != original_author:
            return candidate
    raise UnsafeSnapshot("INDEPENDENT_REVIEWER_NOT_AVAILABLE")


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



def _fence_id(task_id, expected_lease):
    """One immutable guard per predecessor state. Never recycle IDs.

    A competing or stale invocation targeting the same predecessor creates
    the same namedRangeId; Sheets must accept at most one such batch.
    Changing epochs never authorizes deletion of an older guard.
    """
    if not isinstance(task_id, str) or not task_id:
        raise UnsafeSnapshot("LOGICAL_FENCE_TASK_ID_REQUIRED")
    if not isinstance(expected_lease, str):
        raise UnsafeSnapshot("LOGICAL_FENCE_PREDECESSOR_REQUIRED")
    raw = "VAEP_FENCE_V1" + chr(0) + task_id + chr(0) + expected_lease
    return "VAEP_FENCE_" + hashlib.sha256(raw.encode("utf-8")).hexdigest()


def _fence_transition(task_id, expected_lease, runtime_sheet_id, row):
    """Atomic unique-object barrier; NO DeveloperMetadata dual CAS.

    This request MUST be the first operation in the SAME Sheets batch as
    all updates and audit events. A reused ID makes the whole batch fail.
    Do not delete guards while old invocations can exist.
    """
    fence = _fence_id(task_id, expected_lease)
    return {"addNamedRange": {"namedRange": {
        "namedRangeId": fence, "name": fence,
        "range": {
            "sheetId": runtime_sheet_id,
            "startRowIndex": row, "endRowIndex": row + 1,
            "startColumnIndex": 3, "endColumnIndex": 4,
        },
    }}}


def _request_fence_id(requests):
    if not isinstance(requests, (tuple, list)) or not requests:
        raise UnsafeSnapshot("LOGICAL_ATOMIC_FENCE_MISSING")
    marker = requests[0].get("addNamedRange", {}).get("namedRange", {})
    fence = marker.get("namedRangeId")
    if not isinstance(fence, str) or not re.fullmatch(r"VAEP_FENCE_[a-f0-9]{64}", fence):
        raise UnsafeSnapshot("LOGICAL_ATOMIC_FENCE_MISSING")
    return fence


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
    """Prepare four requests: one atomic one-use guard and three updates.

    This is NOT a live claim. Caller must issue the *whole* guarded batch
    and verify provider receipt plus owner/run/token readback. No dual CAS.
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
    decision = select_action(commercial_rows, runtime_rows, actor_id=actor_id,
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
        _fence_transition(task_id, runtime_rows[row][3], runtime_sheet_id, row),
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
    _verify_batch(api_response, 4, _request_fence_id(claim.requests))
    if (len(runtime_row) < 4 or runtime_row[0] != claim.task_id
            or runtime_row[1] != "EN_PROGRESO"
            or runtime_row[2] != claim.actor_id
            or runtime_row[3] != claim.lease):
        raise UnsafeSnapshot("LOGICAL_OWNER_TOKEN_READBACK_MISMATCH")
    return True


def select_review(commercial_rows, runtime_rows, *, actor_id, writer_ids, epoch,
                  expected_count=1123):
    """Review is assigned to exactly one other verified writer per scope."""
    pool = freeze_pool(writer_ids, epoch)
    if actor_id not in pool:
        return {"action": "NO_GITHUB_TASK_WRITE_PROOF", "selected": None}
    if len(pool) < 2:
        return {"action": "INDEPENDENT_REVIEWER_NOT_AVAILABLE", "selected": None}
    tasks = snapshot(commercial_rows, runtime_rows, expected_count)
    if audit_wip_violations(runtime_rows):
        raise UnsafeSnapshot("DUPLICATE_ACTIVE_WIP")
    if any(t["owner"] == actor_id and t["state"] in ("EN_PROGRESO", "VALIDANDO")
           and not t["lease"].startswith("RELEASED") for t in tasks.values()):
        return {"action": "OWN_WIP_MUST_FINISH", "selected": None}
    rt = {row[0]: row for row in runtime_rows[1:]}
    for t in sorted(tasks.values(), key=lambda x: x["order"]):
        if t["state"] != "VALIDANDO" or t["owner"]:
            continue
        if not t["lease"].startswith("RELEASED|ORIGINAL_AUTHOR=") or "|REVIEW_REJECTED=1|" in t["lease"]:
            continue
        author = t["lease"].split("|", 2)[1].split("=", 1)[-1]
        if not ID_RE.fullmatch(author) or author == actor_id:
            continue
        if not all(tasks[d]["state"] == "LISTO" for d in t["dependencies"]):
            continue
        if len(rt[t["id"]]) < 6 or not str(rt[t["id"]][5]).startswith("https://"):
            continue
        if reviewer_for_original_author(t["order"], pool, epoch, author) == actor_id:
            return {"action": "REVIEW", "selected": t["id"],
                    "order": t["order"], "original_author": author}
    return {"action": "NO_INDEPENDENT_REVIEW_FOR_THIS_ACTOR", "selected": None}


def select_action(commercial_rows, runtime_rows, *, actor_id, writer_ids, epoch,
                  expected_count=1123):
    """Own WIP, then independent REVIEW, then BUILD; never autoapprove."""
    base = select(commercial_rows, runtime_rows, actor_id=actor_id,
                  writer_ids=writer_ids, epoch=epoch, expected_count=expected_count)
    if base["action"] in ("NO_GITHUB_TASK_WRITE_PROOF", "CONTINUE_OWNED_OR_RECOVER"):
        return base
    review = select_review(commercial_rows, runtime_rows, actor_id=actor_id,
                           writer_ids=writer_ids, epoch=epoch,
                           expected_count=expected_count)
    if review["action"] == "REVIEW":
        return review
    rework = select_rework(commercial_rows, runtime_rows, actor_id=actor_id,
                           writer_ids=writer_ids, epoch=epoch,
                           expected_count=expected_count)
    return rework if rework["action"] == "REWORK" else base


def _confirm_source_claim(claim, runtime_rows):
    row = next((i for i, r in enumerate(runtime_rows) if i and r[0] == claim.task_id), None)
    if row is None:
        raise UnsafeSnapshot("TASK_NOT_FOUND")
    old = runtime_rows[row]
    if (len(old) < 4 or old[1] != "EN_PROGRESO"
            or old[2] != claim.actor_id or old[3] != claim.lease):
        raise UnsafeSnapshot("LOGICAL_HANDOFF_OWNERSHIP_MISMATCH")
    return row


def build_handoff(claim, runtime_rows, *, evidence_url, now,
                  runtime_sheet_id, events_sheet_id):
    """Own build can only hand off to VALIDANDO; never directly to LISTO."""
    row = _confirm_source_claim(claim, runtime_rows)
    if not isinstance(evidence_url, str) or not evidence_url.startswith("https://"):
        raise UnsafeSnapshot("MATERIAL_EVIDENCE_REQUIRED")
    stamp = _stamp(now)
    _require_live_lease(claim.lease, now)
    lease = (f"RELEASED|ORIGINAL_AUTHOR={claim.actor_id}|TOKEN={claim.token}|"
             f"RUN_ID={claim.run_id}|AT={stamp}|HEAD={claim.head}")
    event = [stamp, claim.actor_id, claim.task_id, "LOGICAL_BUILD_HANDOFF",
             "EN_PROGRESO", "VALIDANDO", claim.head, evidence_url,
             "REVIEW_FIRST_PENDING", "ORIGINAL_AUTHOR_PRESERVED", claim.run_id, claim.token]
    return lease, (
        _fence_transition(claim.task_id, claim.lease, runtime_sheet_id, row),
        _write_range(runtime_sheet_id, row, 1, ["VALIDANDO", "", lease, claim.head]),
        _write_range(runtime_sheet_id, row, 5, [evidence_url]),
        _write_range(runtime_sheet_id, row, 8, [stamp]),
        _event_request(events_sheet_id, event),
    )


def _stamp(now):
    if not isinstance(now, datetime) or now.tzinfo is None:
        raise UnsafeSnapshot("UTC_TIMESTAMP_REQUIRED")
    return now.astimezone(timezone.utc).isoformat(timespec="milliseconds").replace("+00:00", "Z")


def _event_request(sheet_id, values):
    return {"appendCells": {"sheetId": sheet_id,
             "rows": [{"values": [_cell(v) for v in values]}],
             "fields": "userEnteredValue"}}


@dataclass(frozen=True)
class LogicalReview:
    task_id: str
    reviewer: str
    author: str
    run_id: str
    head: str
    token: str
    lease: str
    requests: tuple


def build_review_claim(commercial_rows, runtime_rows, *, task_id, actor_id,
                       writer_ids, epoch, run_id, head, now,
                       runtime_sheet_id, events_sheet_id,
                       previous_run_terminal_proven=False,
                       expected_count=1123):
    if not previous_run_terminal_proven:
        raise UnsafeSnapshot("PRIOR_SCHEDULED_RUN_TERMINAL_PROOF_REQUIRED")
    if not SHA_RE.fullmatch(str(head)):
        raise UnsafeSnapshot("INVALID_HEAD")
    if not re.fullmatch(r"[A-Za-z0-9_.:-]{8,128}", str(run_id)):
        raise UnsafeSnapshot("INVALID_RUN_ID")
    choice = select_review(commercial_rows, runtime_rows, actor_id=actor_id,
                           writer_ids=writer_ids, epoch=epoch,
                           expected_count=expected_count)
    if choice["action"] != "REVIEW" or choice["selected"] != task_id:
        raise UnsafeSnapshot("LOGICAL_REVIEW_NOT_ADMISSIBLE")
    row = next((i for i, r in enumerate(runtime_rows) if i and r[0] == task_id), None)
    stamp = _stamp(now)
    token = secrets.token_hex(16)
    expiry = _stamp(now + timedelta(minutes=10))
    lease = (f"ACTIVE|REVIEWER={actor_id}|ORIGINAL_AUTHOR={choice['original_author']}|"
             f"TOKEN={token}|RUN_ID={run_id}|HEAD={head}|EXPIRES={expiry}|ROUTING_EPOCH={epoch}")
    event = [stamp, actor_id, task_id, "LOGICAL_REVIEW_ACQUIRED",
             "VALIDANDO", "VALIDANDO", head, "",
             "REVIEW_MATERIAL_PENDING", "INDEPENDENT_REVIEWER_LEASE", run_id, token]
    requests = (
        _fence_transition(task_id, runtime_rows[row][3], runtime_sheet_id, row),
        _write_range(runtime_sheet_id, row, 2, [actor_id, lease, head]),
        _write_range(runtime_sheet_id, row, 8, [stamp]),
        _event_request(events_sheet_id, event),
    )
    return LogicalReview(task_id, actor_id, choice["original_author"],
                         run_id, head, token, lease, requests)


def build_review_completion(review, runtime_rows, *, now, review_evidence_url,
                            exact_head_checks_passed, p0, p1, runtime_sheet_id,
                            events_sheet_id):
    """Only independent review with zero P0/P1 and exact-head CI may mark LISTO."""
    row = next((i for i, r in enumerate(runtime_rows) if i and r[0] == review.task_id), None)
    if row is None:
        raise UnsafeSnapshot("TASK_NOT_FOUND")
    live = runtime_rows[row]
    if (review.author == review.reviewer or len(live) < 6 or live[1] != "VALIDANDO"
            or live[2] != review.reviewer or live[3] != review.lease
            or not str(live[5]).startswith("https://")):
        raise UnsafeSnapshot("REVIEW_OWNERSHIP_OR_ORIGINAL_EVIDENCE_MISMATCH")
    if (exact_head_checks_passed is not True or p0 != 0 or p1 != 0
            or not str(review_evidence_url).startswith("https://")):
        raise UnsafeSnapshot("REVIEW_FIRST_EVIDENCE_OR_GATES_MISSING")
    stamp = _stamp(now)
    _require_live_lease(review.lease, now)
    released = (f"RELEASED|ORIGINAL_AUTHOR={review.author}|REVIEWER={review.reviewer}|"
                f"TOKEN={review.token}|RUN_ID={review.run_id}|AT={stamp}|HEAD={review.head}")
    event = [stamp, review.reviewer, review.task_id, "LOGICAL_REVIEW_FIRST_LISTO",
             "VALIDANDO", "LISTO", review.head, review_evidence_url,
             "INDEPENDENT_REVIEW_APPROVED", "P0=0;P1=0;CI_EXACT_HEAD=PASS",
             review.run_id, review.token]
    return released, (
        _fence_transition(review.task_id, review.lease, runtime_sheet_id, row),
        _write_range(runtime_sheet_id, row, 1, ["LISTO", "", released, review.head]),
        _write_range(runtime_sheet_id, row, 8, [stamp]),
        _event_request(events_sheet_id, event),
    )



def _require_live_lease(lease, now):
    """No late writes: caller must still verify exact token/readback and run."""
    instant = _stamp(now)
    pairs = {}
    for part in str(lease).split("|"):
        if "=" in part:
            key, value = part.split("=", 1)
            pairs[key] = value
    if not pairs.get("EXPIRES") or not pairs.get("TOKEN"):
        raise UnsafeSnapshot("LOGICAL_LEASE_EXPIRY_OR_TOKEN_MISSING")
    try:
        expires = datetime.fromisoformat(pairs["EXPIRES"].replace("Z", "+00:00"))
    except ValueError as exc:
        raise UnsafeSnapshot("LOGICAL_LEASE_EXPIRY_INVALID") from exc
    if now.astimezone(timezone.utc) >= expires:
        raise UnsafeSnapshot("LOGICAL_LEASE_EXPIRED")
    return instant


def _verify_batch(api_response, count, fence_id):
    if not isinstance(api_response, dict) or not isinstance(api_response.get("replies"), list):
        raise UnsafeSnapshot("LOGICAL_BATCH_NOT_CONFIRMED")
    if len(api_response["replies"]) != count:
        raise UnsafeSnapshot("LOGICAL_BATCH_REPLY_COUNT_MISMATCH")
    receipt = api_response["replies"][0].get("addNamedRange", {}).get("namedRange", {})
    if receipt.get("namedRangeId") != fence_id:
        raise UnsafeSnapshot("LOGICAL_ATOMIC_FENCE_NOT_CONFIRMED")
    return True


def verify_handoff_response(claim, released_lease, api_response, runtime_row,
                            evidence_url):
    """Require both batch success and live handoff readback before review."""
    _verify_batch(api_response, 5, _fence_id(claim.task_id, claim.lease))
    if (len(runtime_row) < 6 or runtime_row[0] != claim.task_id
            or runtime_row[1] != "VALIDANDO" or runtime_row[2] != ""
            or runtime_row[3] != released_lease or runtime_row[4] != claim.head
            or runtime_row[5] != evidence_url):
        raise UnsafeSnapshot("LOGICAL_HANDOFF_READBACK_MISMATCH")
    return True


def verify_review_claim_response(review, api_response, runtime_row):
    """Review must be acquired by different actor, with matching token."""
    _verify_batch(api_response, 4, _request_fence_id(review.requests))
    if (len(runtime_row) < 6 or runtime_row[0] != review.task_id
            or runtime_row[1] != "VALIDANDO" or runtime_row[2] != review.reviewer
            or runtime_row[3] != review.lease or runtime_row[4] != review.head
            or review.reviewer == review.author):
        raise UnsafeSnapshot("LOGICAL_REVIEW_CLAIM_READBACK_MISMATCH")
    return True


def verify_review_completion_response(review, released_lease, api_response,
                                      runtime_row):
    """A true LISTO needs independent review and readback, not a report."""
    _verify_batch(api_response, 4, _fence_id(review.task_id, review.lease))
    if (len(runtime_row) < 5 or runtime_row[0] != review.task_id
            or runtime_row[1] != "LISTO" or runtime_row[2] != ""
            or runtime_row[3] != released_lease or runtime_row[4] != review.head
            or review.reviewer == review.author):
        raise UnsafeSnapshot("LOGICAL_LISTO_READBACK_MISMATCH")
    return True



def select_rework(commercial_rows, runtime_rows, *, actor_id, writer_ids, epoch,
                  expected_count=1123):
    """A rejected delivery returns exclusively to its original author."""
    pool = freeze_pool(writer_ids, epoch)
    if actor_id not in pool:
        return {"action": "NO_GITHUB_TASK_WRITE_PROOF", "selected": None}
    tasks = snapshot(commercial_rows, runtime_rows, expected_count)
    if any(t["owner"] == actor_id and t["state"] in ("EN_PROGRESO", "VALIDANDO")
           and not t["lease"].startswith("RELEASED") for t in tasks.values()):
        return {"action": "OWN_WIP_MUST_FINISH", "selected": None}
    for task in sorted(tasks.values(), key=lambda t: t["order"]):
        if task["state"] != "VALIDANDO" or task["owner"]:
            continue
        lease = task["lease"]
        if (not lease.startswith("RELEASED|ORIGINAL_AUTHOR=")
                or "|REVIEW_REJECTED=1|" not in lease):
            continue
        if lease.split("|", 2)[1] != f"ORIGINAL_AUTHOR={actor_id}":
            continue
        if not all(tasks[d]["state"] == "LISTO" for d in task["dependencies"]):
            continue
        # The delivery is already tied to its original author. The current
        # bucket may have changed after a reviewed epoch migration; that does
        # not grant another actor authority to rework it.
        if actor_id in pool:
            return {"action": "REWORK", "selected": task["id"],
                    "order": task["order"]}
    return {"action": "NO_AUTHORIZED_REWORK", "selected": None}


def build_review_rejection(review, runtime_rows, *, now, review_evidence_url,
                           reason, runtime_sheet_id, events_sheet_id):
    """Release rejected review to original author, keeping original evidence."""
    row = next((i for i, r in enumerate(runtime_rows) if i and r[0] == review.task_id), None)
    if row is None:
        raise UnsafeSnapshot("TASK_NOT_FOUND")
    live = runtime_rows[row]
    if (review.author == review.reviewer or len(live) < 6 or live[1] != "VALIDANDO"
            or live[2] != review.reviewer or live[3] != review.lease
            or not str(live[5]).startswith("https://")):
        raise UnsafeSnapshot("REJECTION_OWNERSHIP_OR_EVIDENCE_MISMATCH")
    if (not str(review_evidence_url).startswith("https://")
            or not isinstance(reason, str) or len(reason) < 8
            or len(reason) > 200 or "|" in reason or "\n" in reason):
        raise UnsafeSnapshot("REVIEW_REJECTION_PROOF_REQUIRED")
    stamp = _stamp(now)
    _require_live_lease(review.lease, now)
    released = (f"RELEASED|ORIGINAL_AUTHOR={review.author}|REVIEW_REJECTED=1|"
                f"REVIEWER={review.reviewer}|TOKEN={review.token}|"
                f"RUN_ID={review.run_id}|AT={stamp}|HEAD={review.head}")
    event = [stamp, review.reviewer, review.task_id,
             "LOGICAL_REVIEW_FIRST_REJECTED", "VALIDANDO", "VALIDANDO",
             review.head, review_evidence_url, "REWORK_REQUIRED",
             reason, review.run_id, review.token]
    return released, (
        _fence_transition(review.task_id, review.lease, runtime_sheet_id, row),
        _write_range(runtime_sheet_id, row, 2, ["", released, review.head]),
        _write_range(runtime_sheet_id, row, 7, [f"REWORK_ORIGINAL_AUTHOR={review.author}"]),
        _write_range(runtime_sheet_id, row, 8, [stamp]),
        _event_request(events_sheet_id, event),
    )


def build_rework_claim(commercial_rows, runtime_rows, *, task_id, actor_id,
                       writer_ids, epoch, run_id, head, now, runtime_sheet_id,
                       events_sheet_id, previous_run_terminal_proven=False,
                       expected_count=1123):
    """Only original authorized author can re-enter a rejected VALIDANDO."""
    if not previous_run_terminal_proven:
        raise UnsafeSnapshot("PRIOR_SCHEDULED_RUN_TERMINAL_PROOF_REQUIRED")
    if not SHA_RE.fullmatch(str(head)):
        raise UnsafeSnapshot("INVALID_HEAD")
    if not re.fullmatch(r"[A-Za-z0-9_.:-]{8,128}", str(run_id)):
        raise UnsafeSnapshot("INVALID_RUN_ID")
    decision = select_action(commercial_rows, runtime_rows, actor_id=actor_id,
                             writer_ids=writer_ids, epoch=epoch,
                             expected_count=expected_count)
    if decision["action"] != "REWORK" or decision["selected"] != task_id:
        raise UnsafeSnapshot("REWORK_NOT_ADMISSIBLE")
    row = next((i for i, r in enumerate(runtime_rows) if i and r[0] == task_id), None)
    stamp = _stamp(now)
    expiry = _stamp(now + timedelta(minutes=10))
    token = secrets.token_hex(16)
    lease = (f"ACTIVE|OWNER={actor_id}|TOKEN={token}|RUN_ID={run_id}|"
             f"START={stamp}|EXPIRES={expiry}|HEAD={head}|ROUTING_EPOCH={epoch}")
    event = [stamp, actor_id, task_id, "LOGICAL_REWORK_ACQUIRED",
             "VALIDANDO", "EN_PROGRESO", head, "",
             "REWORK_MATERIAL_PENDING", "ORIGINAL_AUTHOR_RESUMED", run_id, token]
    reqs = (
        _fence_transition(task_id, runtime_rows[row][3], runtime_sheet_id, row),
        _write_range(runtime_sheet_id, row, 1, ["EN_PROGRESO", actor_id, lease, head]),
        _write_range(runtime_sheet_id, row, 8, [stamp]),
        _event_request(events_sheet_id, event),
    )
    return LogicalClaim(task_id, actor_id, run_id, head, token, lease, reqs)


def verify_review_rejection_response(review, released_lease, api_response,
                                     runtime_row):
    _verify_batch(api_response, 5, _fence_id(review.task_id, review.lease))
    if (len(runtime_row) < 5 or runtime_row[0] != review.task_id
            or runtime_row[1] != "VALIDANDO" or runtime_row[2] != ""
            or runtime_row[3] != released_lease or runtime_row[4] != review.head):
        raise UnsafeSnapshot("REJECTED_REVIEW_READBACK_MISMATCH")
    return True



def _expired_at(lease, now):
    stamp = _stamp(now)
    parts = [p for p in str(lease).split("|") if p.startswith("EXPIRES=")]
    if len(parts) != 1:
        raise UnsafeSnapshot("LOGICAL_EXPIRY_REQUIRED")
    try:
        value = datetime.fromisoformat(parts[0].split("=", 1)[1].replace("Z", "+00:00"))
    except ValueError as exc:
        raise UnsafeSnapshot("LOGICAL_EXPIRY_INVALID") from exc
    return now.astimezone(timezone.utc) >= value


def build_heartbeat(claim, runtime_rows, *, now, run_still_active_proven=False,
                    runtime_sheet_id, events_sheet_id):
    """Renew only same active Task run, matching exact owner+token+HEAD."""
    if run_still_active_proven is not True:
        raise UnsafeSnapshot("LIVE_RUN_PROOF_REQUIRED_FOR_RENEWAL")
    row = _confirm_source_claim(claim, runtime_rows)
    _require_live_lease(claim.lease, now)
    old_expiry = [p for p in claim.lease.split("|") if p.startswith("EXPIRES=")]
    if len(old_expiry) != 1:
        raise UnsafeSnapshot("LEASE_EXPIRY_MISSING")
    new_expiry = f"EXPIRES={_stamp(now + timedelta(minutes=10))}"
    new_lease = claim.lease.replace(old_expiry[0], new_expiry)
    stamp = _stamp(now)
    event = [stamp, claim.actor_id, claim.task_id, "LOGICAL_LEASE_HEARTBEAT",
             "EN_PROGRESO", "EN_PROGRESO", claim.head, "",
             "RUN_MATERIAL_STILL_ACTIVE", "NO_OWNER_CHANGE", claim.run_id, claim.token]
    reqs = (
        _fence_transition(claim.task_id, claim.lease, runtime_sheet_id, row),
        _write_range(runtime_sheet_id, row, 3, [new_lease]),
        _write_range(runtime_sheet_id, row, 8, [stamp]),
        _event_request(events_sheet_id, event),
    )
    return replace(claim, lease=new_lease, requests=reqs)


def verify_heartbeat_response(claim, api_response, runtime_row):
    _verify_batch(api_response, 4, _request_fence_id(claim.requests))
    if (len(runtime_row) < 5 or runtime_row[0] != claim.task_id
            or runtime_row[1] != "EN_PROGRESO"
            or runtime_row[2] != claim.actor_id
            or runtime_row[3] != claim.lease
            or runtime_row[4] != claim.head):
        raise UnsafeSnapshot("HEARTBEAT_READBACK_MISMATCH")
    return True


def build_own_stale_recovery(claim, runtime_rows, *, now, 
                             previous_run_terminal_proven=False,
                             runtime_sheet_id, events_sheet_id):
    """Recover only same actor's terminal expired run with token fencing.

    Cross-actor recovery must never occur without a new globally approved
    routing epoch, all previous runs terminal and documented readbacks.
    """
    if previous_run_terminal_proven is not True:
        raise UnsafeSnapshot("TERMINAL_RUN_PROOF_REQUIRED_FOR_RECOVERY")
    row = _confirm_source_claim(claim, runtime_rows)
    if not _expired_at(claim.lease, now):
        raise UnsafeSnapshot("LEASE_STILL_FRESH")
    stamp = _stamp(now)
    fence = secrets.token_hex(16)
    recovered = (f"RECOVERED|STALE_OWNER={claim.actor_id}|STALE_TOKEN={claim.token}|"
                 f"FENCING_TOKEN={fence}|RECOVERED_AT={stamp}|HEAD={claim.head}")
    event = [stamp, claim.actor_id, claim.task_id,
             "LOGICAL_SELF_STALE_RECOVERY", "EN_PROGRESO", "PENDIENTE",
             claim.head, "", "STALE_TERMINAL_PROVEN_FENCED",
             "SAME_DESIGNATED_OWNER_RECLAIMABLE", claim.run_id, fence]
    reqs = (
        _fence_transition(claim.task_id, claim.lease, runtime_sheet_id, row),
        _write_range(runtime_sheet_id, row, 1, ["PENDIENTE", "", recovered, claim.head]),
        _write_range(runtime_sheet_id, row, 8, [stamp]),
        _event_request(events_sheet_id, event),
    )
    return recovered, reqs


def verify_own_stale_recovery_response(claim, recovered, api_response,
                                       runtime_row):
    _verify_batch(api_response, 4, _fence_id(claim.task_id, claim.lease))
    if (len(runtime_row) < 5 or runtime_row[0] != claim.task_id
            or runtime_row[1] != "PENDIENTE" or runtime_row[2] != ""
            or runtime_row[3] != recovered or runtime_row[4] != claim.head
            or f"STALE_TOKEN={claim.token}" not in recovered):
        raise UnsafeSnapshot("STALE_RECOVERY_READBACK_MISMATCH")
    return True





def evaluate_pre_go_manifest(manifest, *, dev_base_sha, sheet_writer_policies,
                             sheet_runtime_mode, unowned_task_count,
                             owner_acceptance_confirmed, security_review_approved,
                             exact_head_checks_passed):
    """Certify immutable PRE-GO evidence, NOT a commercial execution permit.

    Caller must obtain each live observation from GitHub/Sheets independently.
    No Task-origin write, terminal-run status or CAS proof is synthesized.
    """
    if (not isinstance(manifest, dict)
            or manifest.get("mode") != "PRE_GO_READ_ONLY_CERTIFIED"
            or manifest.get("activation_permission") != "OWNER_APPROVED_PRE_GO_ONLY"
            or manifest.get("schema_version") != 1
            or manifest.get("platform") != "SOLQARYN"
            or manifest.get("branch") != "dev"
            or manifest.get("runtime_model") != "TASKS_ONLY"
            or manifest.get("plan_task_count") != 1123
            or manifest.get("plan_drive_file_id") != "1T41ppLvC3x-p6sORCtpaPvzLWPP9_Z7j"
            or manifest.get("safety_check_bypass_allowed") is not False):
        raise UnsafeSnapshot("LOGICAL_PREGO_SCOPE_OR_MODE_MISMATCH")
    if (manifest.get("routing_epoch_state") != "FROZEN_PRE_GO"
            or manifest.get("base_dev_sha") != dev_base_sha
            or not isinstance(dev_base_sha, str)
            or not SHA_RE.fullmatch(dev_base_sha)):
        raise UnsafeSnapshot("LOGICAL_PREGO_BASE_OR_EPOCH_MISMATCH")
    writers = ["6ad7bbf79c488191bf893e6c08ea9b5f",
               "6abd6241c3e48191a8856ed7b0f42c5c"]
    if (manifest.get("verified_writer_task_ids") != writers
            or tuple(freeze_pool(writers, manifest.get("routing_epoch"))) != tuple(writers)):
        raise UnsafeSnapshot("LOGICAL_PREGO_WRITER_POOL_MISMATCH")
    evidence = manifest.get("writer_evidence")
    if not isinstance(evidence, list) or len(evidence) != 2:
        raise UnsafeSnapshot("LOGICAL_PREGO_EVIDENCE_MISSING")
    expected = (
        (writers[0], "jmejia31", "bdee818f0da1eb4261b7f848673d3e3acac48779"),
        (writers[1], "morales35alex", "963171871e5eebbbd48d522d2ef847bfdffb044b"),
    )
    for proof, (task_id, author, commit) in zip(evidence, expected):
        if (not isinstance(proof, dict)
                or proof.get("task_id") != task_id
                or proof.get("github_author") != author
                or proof.get("github_commit_sha") != commit
                or proof.get("fingerprint") != "VAEP_TASK_GH_WRITE:" + task_id
                or proof.get("purpose") != "ADMIN_DIAGNOSTIC_NO_LEASE"
                or proof.get("evidence_scope") != "OWNER_ACCEPTED_GITHUB_WRITE_ADMIN_ONLY"
                or not str(proof.get("task_run_origin", "")).startswith(
                    ("NOT_INDEPENDENTLY_", "OWNER_SUBMITTED_"))):
            raise UnsafeSnapshot("LOGICAL_PREGO_PROOF_CONTENT_MISMATCH")
    accept = manifest.get("writer_evidence_acceptance", {})
    if (not isinstance(accept, dict)
            or accept.get("event") != "OWNER_ACCEPTS_2_EXISTING_GITHUB_DIAGNOSTICS_GATE_CLOSED"
            or accept.get("event_row") != 94
            or accept.get("result") != "CLOSED_OWNER_ACCEPTED_NO_REPEAT_PROBES"
            or accept.get("repeat_write_probes_required") is not False
            or owner_acceptance_confirmed is not True):
        raise UnsafeSnapshot("LOGICAL_PREGO_OWNER_ACCEPTANCE_MISSING")
    review = manifest.get("independent_security_review", {})
    if (not isinstance(review, dict)
            or review.get("pull_request") != 3709
            or review.get("reviewer") != "jmejia31"
            or review.get("state") != "APPROVED"
            or security_review_approved is not True
            or exact_head_checks_passed is not True):
        raise UnsafeSnapshot("LOGICAL_PREGO_SECURITY_OR_CI_MISSING")
    source = manifest.get("readback_snapshot", {})
    legacy_policy = ("AUTOMATION_ONLY|RUNTIME_ACTIVO_OWNER_EXCEPTION|"
                     "PER_SCOPE_EFFECTIVE_WRITE|DUAL_CAS_WIP1_REVIEW_FIRST")
    if (not isinstance(source, dict)
            or source.get("base_dev_sha") != dev_base_sha
            or source.get("automation_count") != 10
            or source.get("automation_enabled_registered") != 10
            or source.get("commercial_task_count") != 1123
            or source.get("owners") != 0
            or source.get("in_progress") != 0
            or source.get("validating") != 0
            or source.get("sheet_runtime_mode") != "PAUSADO"
            or source.get("pre_cutover_write_policy") != legacy_policy
            or manifest.get("verified_task_terminated_count") != 0
            or sheet_runtime_mode != "PAUSADO"
            or unowned_task_count != 1123
            or not isinstance(sheet_writer_policies, (tuple, list))
            or len(sheet_writer_policies) != 10
            or any(policy != legacy_policy for policy in sheet_writer_policies)):
        raise UnsafeSnapshot("LOGICAL_PREGO_SHEET_HOLD_OR_TERMINALITY_MISMATCH")
    terms = manifest.get("cutover_conditions", {})
    contract = manifest.get("protocol_contract", {})
    if (not isinstance(terms, dict)
            or terms.get("require_all_ten_write_policies")
                != "LOGICAL_SINGLE_OWNER_TASK_WRITE_VERIFIED"
            or terms.get("may_run_commercial_claims_during_pre_go") is not False
            or terms.get("no_mixed_protocol") is not True
            or not isinstance(contract, dict)
            or contract.get("legacy_dual_cas_reintroduced") is not False
            or contract.get("guard") !=
               "ADD_NAMED_RANGE_WITH_SAME_ATOMIC_SPREADSHEETS_BATCHUPDATE"
            or contract.get("review_first_distinct_author") is not True
            or contract.get("tool_safety_bypass") is not False):
        raise UnsafeSnapshot("LOGICAL_PREGO_CONTRACT_UNSAFE")
    return {"stage": "PRE_GO_READ_ONLY_CERTIFIED",
            "writer_ids": tuple(writers), "epoch": manifest["routing_epoch"],
            "dev_base_sha": dev_base_sha, "commercial_admission": "NO_GO",
            "owner_accepted_admin_evidence_count": 2}


def evaluate_live_cutover_manifest(manifest, *, dev_sha, sheet_writer_policies,
                                   proven_task_writers, prior_runs_terminal):
    """Gate for a FUTURE migration, never a replacement for external auth.

    All inputs must originate from live readbacks in their own systems.
    Runtime callers must not synthesize proof from the manifest or this result.
    The bundled manifest is PROPOSAL_ONLY and must always return NO_GO.
    """
    if not isinstance(manifest, dict) or manifest.get("mode") != "ACTIVE_CERTIFIED":
        raise UnsafeSnapshot("LOGICAL_CUTOVER_NOT_ACTIVATED")
    if (manifest.get("schema_version") != 1
            or manifest.get("platform") != "SOLQARYN"
            or manifest.get("runtime_model") != "TASKS_ONLY"
            or manifest.get("branch") != "dev"
            or manifest.get("plan_task_count") != 1123
            or manifest.get("plan_drive_file_id") != "1T41ppLvC3x-p6sORCtpaPvzLWPP9_Z7j"
            or manifest.get("safety_check_bypass_allowed") is not False):
        raise UnsafeSnapshot("LOGICAL_MANIFEST_SCOPE_LOCK_MISMATCH")
    if (manifest.get("activation_permission") != "OWNER_APPROVED_REVIEWED"
            or prior_runs_terminal is not True):
        raise UnsafeSnapshot("LOGICAL_CUTOVER_AUTHORIZATION_OR_TERMINAL_PROOF_MISSING")
    if not isinstance(dev_sha, str) or not SHA_RE.fullmatch(dev_sha):
        raise UnsafeSnapshot("LIVE_DEV_SHA_REQUIRED")
    if manifest.get("base_dev_sha") != dev_sha:
        raise UnsafeSnapshot("LOGICAL_CUTOVER_DEV_SHA_MISMATCH")
    expected = manifest.get("write_policy_required")
    if (expected != "LOGICAL_SINGLE_OWNER_TASK_WRITE_VERIFIED"
            or not isinstance(sheet_writer_policies, (list, tuple))
            or len(sheet_writer_policies) != 10
            or any(value != expected for value in sheet_writer_policies)):
        raise UnsafeSnapshot("LOGICAL_MIXED_PROTOCOL_FORBIDDEN")
    listed = manifest.get("verified_writer_task_ids")
    if (not isinstance(listed, list)
            or len(listed) < 2
            or not isinstance(proven_task_writers, (list, tuple))
            or tuple(listed) != tuple(proven_task_writers)):
        raise UnsafeSnapshot("LOGICAL_TASK_OR_INDEPENDENT_REVIEW_PROOFS_MISSING")
    pool = freeze_pool(listed, manifest.get("routing_epoch"))
    return {"mode": "ACTIVE_CERTIFIED", "epoch": manifest["routing_epoch"],
            "writer_ids": pool, "head": dev_sha, "proofs_must_be_fresh": True}
