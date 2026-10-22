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

from commercial_dispatch import UnsafeSnapshot, dispatch, snapshot, eligible_independent_reviews

EXECUTORS = (
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
PAIRS = {
    EXECUTORS[1]: EXECUTORS[0],
    EXECUTORS[3]: EXECUTORS[2],
    EXECUTORS[5]: EXECUTORS[4],
    EXECUTORS[7]: EXECUTORS[6],
    EXECUTORS[9]: EXECUTORS[8],
}
_DATE_EXPIRES = re.compile(r"(?:^|\|)EXPIRES=([^|]+)")
_DATE_AUTHOR = re.compile(r"(?:^|\|)(?:ORIGINAL_AUTHOR|OWNER)=([0-9a-f]{32})(?:\||$)")


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
    provenance = re.search(r"(?:^|\|)ORIGINAL_AUTHOR=([0-9a-f]{32})(?:\||$)", value)
    if provenance:
        return provenance.group(1)
    owner = _DATE_AUTHOR.search(value)
    return owner.group(1) if owner else None


def inspect_inflight(tasks, now):
    """Report who holds VALIDANDO/EN_PROGRESO; a lease never proves a live run.

    The connected scheduler must independently verify the run state. This
    function does not release, reclaim or authorize any ownership.
    """
    groups = {"VALIDANDO": [], "EN_PROGRESO": []}
    for task in sorted(tasks.values(), key=lambda item: item["order"]):
        if task["state"] not in groups:
            continue
        owner = task["owner"] or None
        expiry = _expiration(task["lease"])
        if task["lease"].startswith("RELEASED|") and owner is None:
            ownership = "RELEASED_HANDOFF"
        elif owner and expiry and expiry > now:
            ownership = "FRESH_LEASE_NOT_LIVE_RUN_PROOF"
        elif owner:
            ownership = "EXPIRED_OR_UNKNOWN_LEASE_RUN_UNVERIFIED"
        else:
            ownership = "UNCLAIMED_OR_MALFORMED_LEASE"
        groups[task["state"]].append({
            "id": task["id"], "order": task["order"],
            "owner_automation_id": owner,
            "owner_is_canonical": owner in EXECUTORS if owner else False,
            "original_author_automation_id": _author(task["lease"]) if task["state"] == "VALIDANDO" else None,
            "lease_observation": ownership,
            "lease_expires_at": expiry.isoformat() if expiry else None,
            "live_run_confirmed": False,
            "external_run_readback_required": bool(owner),
        })
    return groups


def decide(commercial_rows, runtime_rows, *, actor_id, now,
           supervised_actor_id=None, expected_count=1123):
    """Return one deterministic *next action* for a given existing executor.

    The lifecycle is PENDIENTE -> EN_PROGRESO -> VALIDANDO -> LISTO.
    Admission preflight inspects VALIDANDO ownership/reviewer, then
    EN_PROGRESO owner/run evidence. Resolve independently reviewable
    handoffs and owned work when safe; otherwise choose lowest-ORDEN,
    dependency-valid PENDIENTE. A stale *foreign* lease is not recovery
    authorization. WIP=1 and independent review remain mandatory.
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
    inflight = inspect_inflight(tasks, now)
    read_only = dispatch(commercial_rows, runtime_rows, expected_count)
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

    def result(action, selected=None, reason=None, available=0, reviews=0,
               recovery_candidate=None):
        return {
            "action": action, "actor_id": actor_id,
            "supervised_actor_id": supervised_actor_id,
            "supervision": supervision, "selected": selected,
            "reason": reason, "eligible_builds": available,
            "eligible_reviews": reviews,
            "canonical_lifecycle": ["PENDIENTE", "EN_PROGRESO", "VALIDANDO", "LISTO"],
            "inspected_inflight": inflight,
            "recovery_candidate_needing_verified_terminal_run": recovery_candidate,
            "next_hierarchical_pending": read_only["selected"],
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
    # Inspect all VALIDANDO before selecting an eligible independent REVIEW.
    # In-progress review ownership remains untouched; no self-approval.
    reviews = eligible_independent_reviews(
        commercial_rows, runtime_rows, expected_count, reviewer=actor_id)
    if reviews:
        return result("REVIEW", {"id": reviews[0]["id"], "order": reviews[0]["order"]},
                      reason="VALIDANDO_FIRST_INDEPENDENT_REVIEW_LOWEST_ORDER",
                      available=len(read_only["available"]), reviews=len(reviews))
    # Inspect EN_PROGRESO and still-owned VALIDANDO. Without external proof
    # that their original executions ended, they are not reclaimable.
    # Return BUILD directly if an independent PENDIENTE is available, not
    # ASSESS_STALE_RECOVERY that could occupy the whole scheduled run.
    stale = sorted(
        (t for t in tasks.values() if t["owner"]
         and t["state"] in ("EN_PROGRESO", "VALIDANDO")
         and (_expiration(t["lease"]) is None or _expiration(t["lease"]) <= now)),
        key=lambda t: (0 if t["state"] == "VALIDANDO" else 1, t["order"]),
    )
    next_recovery = (
        {"id": stale[0]["id"], "order": stale[0]["order"],
         "state": stale[0]["state"], "owner": stale[0]["owner"]}
        if stale else None
    )
    if read_only["available"]:
        build = read_only["available"][0]
        return result(
            "BUILD", {"id": build["id"], "order": build["order"]},
            reason=("INFLIGHT_OWNER_RUN_UNVERIFIED_CHOOSE_SAFE_PENDING"
                    if stale else "NO_ADMISSIBLE_REVIEW_OR_OWN_WIP_CHOOSE_PENDING"),
            available=len(read_only["available"]),
            recovery_candidate=next_recovery,
        )
    if stale:
        return result(
            "SAFE_RECOVERY_REQUIRED", selected=next_recovery,
            reason="CHECK_TERMINAL_RUN_THEN_ATOMIC_LOGICAL_FENCE_OR_STAY_READ_ONLY",
            recovery_candidate=next_recovery,
        )

    excluded_reviews = []
    for t in tasks.values():
        if t["state"] != "VALIDANDO" or t["owner"] or not t["lease"].startswith("RELEASED|"):
            continue
        excluded_reviews.append(t["id"] + ":NO_ELIGIBLE_INDEPENDENT_REVIEW")
    if excluded_reviews:
        return result("BLOCKED_REVIEW_PROVENANCE",
                      reason=";".join(excluded_reviews[:5]))
    return result("WAIT_DEPENDENCIES",
                  reason="NO_FREE_ELIGIBLE_BUILD_OR_INDEPENDENT_REVIEW")

def main():
    """CLI consumes an authenticated fresh Sheet export; never claims a lease."""
    import argparse
    import json
    from pathlib import Path

    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--snapshot", required=True, type=Path)
    parser.add_argument("--actor", required=True, choices=EXECUTORS)
    parser.add_argument("--supervises", choices=EXECUTORS)
    parser.add_argument("--expected-count", type=int, default=1123)
    args = parser.parse_args()
    try:
        data = json.loads(args.snapshot.read_text(encoding="utf-8"))
        action = decide(
            data["commercial_rows"], data["runtime_rows"],
            actor_id=args.actor, supervised_actor_id=args.supervises,
            now=datetime.now(timezone.utc), expected_count=args.expected_count,
        )
    except (OSError, KeyError, ValueError, TypeError) as exc:
        print(json.dumps({"status": "NO_GO_SCOPE_ONLY", "error": str(exc)}))
        return 2
    print(json.dumps({"status": "READ_ONLY_CONTINUITY_DECISION", **action}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
