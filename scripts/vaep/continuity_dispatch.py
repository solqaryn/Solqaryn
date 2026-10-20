#!/usr/bin/env python3
"""Read-only SOLQARYN VAEP universal-executor continuity decision.

All 5 Primary and all 5 Supervisor have identical build/review/recovery
eligibility. Supervision means checking the paired actor's liveness, NOT
waiting when other work is eligible. The current plan and runtime are the only
authority. No historic CURRENT_PARENT, extra Tasks, or backend workers.

THIS IS A DECISION ENGINE, NOT A LEASE WRITER OR A RESIDENT PROCESS.
A runtime MUST obtain independently verified atomic ownership and actual
repository write access before attempting any material change.
"""
from datetime import datetime, timezone
import re

from commercial_dispatch import UnsafeSnapshot, dispatch, snapshot

EXECUTORS = (
    "6aa15346f5408191bdd9043fd26ff7aa",  # Primary :00
    "6abd62255ae88191a2dba6e1b00d3b4d",  # Supervisor :05
    "6aa1534deee481918280def1343adcfa",  # Primary :12
    "6abd6235e100819195785fc76a44a8e0",  # Supervisor :17
    "6abd70e42cb4819190b3b28916dc9dbb",  # Primary :24
    "6abd6241c3e48191a8856ed7b0f42c5c",  # Supervisor :29
    "6aa1535a51508191a610e6cdb90a2a4d",  # Primary :36
    "6abd624e5e108191bef6bb879559328c",  # Supervisor :41
    "6aa1535f8cd48191b73e10f17843372a",  # Primary :48
    "6abd625b397c8191a49904227946a20f",  # Supervisor :53
)
PAIRS = {
    EXECUTORS[1]: EXECUTORS[0],
    EXECUTORS[3]: EXECUTORS[2],
    EXECUTORS[5]: EXECUTORS[4],
    EXECUTORS[7]: EXECUTORS[6],
    EXECUTORS[9]: EXECUTORS[8],
}
_DATE_EXPIRES = re.compile(r"(?:^|\\|)EXPIRES=([^|]+)")
_DATE_AUTHOR = re.compile(r"(?:^|\\|)(?:ORIGINAL_AUTHOR|OWNER)=([0-9a-f]{32})(?:\\||$)")


def _expiration(lease):
    match = _DATE_EXPIRES.search(str(lease))
    if not match:
        return None
    try:
        ts = datetime.fromisoformat(match.group(1).replace("Z", "+00:00"))
    except (ValueError, TypeError):
        return None
    return ts.astimezone(timezone.utc) if ts.tzinfo else None


def _author(lease):
    # Preserve the implementation author even if a previous REVIEW_FIRST
    # rejected the delivery and released its review lease.
    value = str(lease)
    provenance = re.search(r"(?:^|\\|)ORIGINAL_AUTHOR=([0-9a-f]{32})(?:\\||$)", value)
    if provenance:
        return provenance.group(1)
    owner = _DATE_AUTHOR.search(value)
    return owner.group(1) if owner else None


def decide(commercial_rows, runtime_rows, *, actor_id, now,
           supervised_actor_id=None, expected_count=1123):
    """Return one deterministic *next action* for a given existing executor.

    Priority among *free and eligible* actions is global ascending ORDER.
    An owned scope must be completed, handed off, or safely recovered BEFORE
    acquiring another: WIP=1 applies to both Primary and Supervisor.
    Expired leases are NOT automatically stolen or released.
    """
    if actor_id not in EXECUTORS:
        raise UnsafeSnapshot("UNKNOWN_EXECUTOR")
    if supervised_actor_id is not None and (
        PAIRS.get(actor_id) != supervised_actor_id
    ):
        raise UnsafeSnapshot("UNAUTHORIZED_SUPERVISION_PAIR")
    if not isinstance(now, datetime) or now.tzinfo is None:
        raise UnsafeSnapshot("UTC_AWARE_TIMESTAMP_REQUIRED")
    now = now.astimezone(timezone.utc)
    tasks = snapshot(commercial_rows, runtime_rows, expected_count)
    if len(set(EXECUTORS)) != 10:
        raise UnsafeSnapshot("DUPLICATED_CANONICAL_EXECUTOR")
    raw = {r[0]: r for r in runtime_rows[1:]}
    active = sorted(
        (t for t in tasks.values() if t["owner"] == actor_id
         and t["state"] in ("EN_PROGRESO", "VALIDANDO")
         and not t["lease"].startswith("RELEASED")),
        key=lambda t: t["order"],
    )

    supervised = []
    if supervised_actor_id:
        supervised = sorted(
            (t for t in tasks.values() if t["owner"] == supervised_actor_id
             and t["state"] in ("EN_PROGRESO", "VALIDANDO")
             and not t["lease"].startswith("RELEASED")),
            key=lambda t: t["order"],
        )

    supervision = "NOT_REQUESTED"
    if supervised_actor_id:
        # No claim is not proof that the scheduled Task did not run.
        supervision = "NO_CURRENT_OWNED_SCOPE"
        if supervised:
            supervision = "OWNED_SCOPE_UNVERIFIED"
            if any(_expiration(t["lease"]) is None or
                   _expiration(t["lease"]) <= now for t in supervised):
                supervision = "STALE_OR_UNVERIFIED_OWNERSHIP"
            else:
                supervision = "FRESH_OWNERSHIP_NOT_MATERIAL_PROOF"

    def result(action, selected=None, reason=None, available=0, reviews=0):
        return {
            "action": action, "actor_id": actor_id,
            "supervised_actor_id": supervised_actor_id,
            "supervision": supervision, "selected": selected,
            "reason": reason, "eligible_builds": available,
            "eligible_reviews": reviews,
            "same_capabilities_for_all_ten": True,
            "needs_atomic_lease_before_write": True,
            "is_resident_process": False,
        }

    if len(active) > 1:
        return result("RECONCILE_DUPLICATE_WIP",
                      reason="OWNER_HAS_MULTIPLE_SCOPES:DO_NOT_TAKE_ANOTHER")
    if active:
        own = active[0]
        expires = _expiration(own["lease"])
        if expires is None or expires <= now:
            return result(
                "RECOVER_OWN_STALE_SCOPE",
                selected={"id": own["id"], "order": own["order"]},
                reason="VERIFY_TERMINAL_RUN_AND_FENCING_BEFORE_WRITING",
            )
        return result("CONTINUE_OWNED_SCOPE",
                      selected={"id": own["id"], "order": own["order"]},
                      reason="RESUME_MATERIAL_WORK_NOT_PRE-FLIGHT_ONLY")

    # Another actor's scope is not stealable, even if its TTL has passed.
    # However, that is not a reason to sit idle while other work is free.
    read_only = dispatch(commercial_rows, runtime_rows, expected_count)
    candidates = [
        (t["order"], "BUILD", {"id": t["id"], "order": t["order"]})
        for t in (tasks[a["id"]] for a in read_only["available"])
    ]
    excluded_reviews = []
    for t in tasks.values():
        if t["state"] != "VALIDANDO" or t["owner"] or not t["lease"].startswith("RELEASED|"):
            continue
        if any(tasks[d]["state"] != "LISTO" for d in t["dependencies"]):
            continue
        author = _author(t["lease"])
        proof = raw[t["id"]][5] if len(raw[t["id"]]) > 5 else ""
        if author is None or not isinstance(proof, str) or not proof.startswith("https://"):
            excluded_reviews.append(t["id"] + ":MISSING_AUTHOR_OR_EVIDENCE")
            continue
        if actor_id == author:
            excluded_reviews.append(t["id"] + ":SELF_REVIEW_FORBIDDEN")
            continue
        candidates.append((t["order"], "REVIEW", {"id": t["id"], "order": t["order"]}))
    candidates.sort(key=lambda item: item[0])
    if candidates:
        chosen = candidates[0]
        return result(chosen[1], chosen[2],
                      reason="GLOBAL_ASCENDING_ORDER_FREE_ELIGIBLE",
                      available=len(read_only["available"]),
                      reviews=len([x for x in candidates if x[1] == "REVIEW"]))

    stale = sorted(
        (t for t in tasks.values() if t["owner"]
         and t["state"] in ("EN_PROGRESO", "VALIDANDO")
         and (_expiration(t["lease"]) is None or _expiration(t["lease"]) <= now)),
        key=lambda t: t["order"],
    )
    if stale:
        return result(
            "SAFE_RECOVERY_REQUIRED",
            selected={"id": stale[0]["id"], "order": stale[0]["order"]},
            reason="NEVER_STEAL_WITHOUT_TERMINATION_PROOF_AND_FENCING",
        )
    if excluded_reviews:
        return result("BLOCKED_REVIEW_PROVENANCE",
                      reason=";".join(excluded_reviews[:5]))
    return result("WAIT_DEPENDENCIES",
                  reason="NO_FREE_ELIGIBLE_BUILD_OR_INDEPENDENT_REVIEW")
