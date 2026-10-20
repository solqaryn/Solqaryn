"""Executable continuity-contract tests: same powers, safe failover, zero fake work."""
import unittest
from datetime import datetime, timezone

from commercial_dispatch import UnsafeSnapshot
from continuity_dispatch import decide, EXECUTORS, PAIRS


HEAD = "7d05a608d5e619d619ea8030da45cc50ef1a87cf"
NOW = datetime(2026, 10, 20, 17, 0, tzinfo=timezone.utc)
COMMERCIAL = [
    "ID", "PROGRAMA_PRIORIDAD", "MICROTAREA", "CRITERIO_EXIGIDO", "ORDEN",
    "PLAN_COMERCIAL_DOC_ID", "VERSION_FUENTE", "UNIDAD_ORIGEN_LANE",
]
RUNTIME = [
    "ID", "ESTADO", "OWNER_AUTOMATION_ID", "LEASE_STATUS", "HEAD",
    "EVIDENCIA", "BLOQUEO", "NEXT_ACTION", "UPDATED_AT",
    "DEPENDENCIAS_ACTUALES", "DEPENDENCIAS_OK", "PRIORIDAD_ACTUAL",
    "P0", "P1", "ROLLBACK", "RESULTADO", "NOTA_RUNTIME", "RESERVED",
]


def tables(*items):
    commercial, runtime = [COMMERCIAL.copy()], [RUNTIME.copy()]
    for order, task, state, dependency, owner, lease, evidence in items:
        commercial.append([task, "P0", task, "", str(order), "", "", "ANY"])
        runtime.append([
            task, state, owner, lease, HEAD, evidence, "", "", "",
            dependency, "", "", "", "", "", "", "", "",
        ])
    return commercial, runtime


def run(*items, actor=EXECUTORS[0], supervised=None):
    c, r = tables(*items)
    return decide(c, r, actor_id=actor, supervised_actor_id=supervised,
                  now=NOW, expected_count=len(items))


AVAILABLE = (1, "TASK-1", "PENDIENTE", "", "", "", "")
SECOND = (2, "TASK-2", "PENDIENTE", "", "", "", "")
FRESH = (
    "ACTIVE|OWNER=" + EXECUTORS[0] +
    "|TOKEN=fresh|RUN_ID=R1|START=2026-10-20T16:59:00Z"
    "|EXPIRES=2026-10-20T17:09:00Z|HEAD=" + HEAD
)
STALE = (
    "ACTIVE|OWNER=" + EXECUTORS[0] +
    "|TOKEN=old|RUN_ID=R1|START=2026-10-20T16:40:00Z"
    "|EXPIRES=2026-10-20T16:50:00Z|HEAD=" + HEAD
)


class UniversalContinuityTests(unittest.TestCase):
    def test_retired_primary_ids_are_not_executors(self):
        retired = ["6aa15346f5408191bdd9043fd26ff7aa","6aa1534deee481918280def1343adcfa","6abd70e42cb4819190b3b28916dc9dbb","6aa1535a51508191a610e6cdb90a2a4d","6aa1535f8cd48191b73e10f17843372a"]
        self.assertTrue(set(retired).isdisjoint(EXECUTORS))
        c, r = tables(AVAILABLE)
        for old_id in retired:
            with self.subTest(old_id=old_id):
                with self.assertRaisesRegex(UnsafeSnapshot, "UNKNOWN_EXECUTOR"):
                    decide(c, r, actor_id=old_id, now=NOW, expected_count=1)


    def test_all_ten_identical_eligibility_for_build(self):
        self.assertEqual(len(EXECUTORS), 10)
        self.assertEqual(len(set(EXECUTORS)), 10)
        for actor in EXECUTORS:
            with self.subTest(actor=actor):
                result = run(AVAILABLE, SECOND, actor=actor)
                self.assertEqual(result["action"], "BUILD")
                self.assertEqual(result["selected"]["id"], "TASK-1")
                self.assertTrue(result["same_capabilities_for_all_ten"])

    def test_all_five_supervisors_can_build_when_primary_has_no_claim(self):
        for sup, primary in PAIRS.items():
            with self.subTest(supervisor=sup):
                result = run(AVAILABLE, SECOND, actor=sup, supervised=primary)
                self.assertEqual(result["action"], "BUILD")
                self.assertEqual(result["selected"]["order"], 1)
                self.assertEqual(result["supervision"], "NO_CURRENT_OWNED_SCOPE")

    def test_supervisor_does_not_wait_if_primary_is_fresh_on_other_task(self):
        claimed = (1, "TASK-1", "EN_PROGRESO", "", EXECUTORS[0], FRESH, "")
        result = run(claimed, SECOND, actor=EXECUTORS[1], supervised=EXECUTORS[0])
        self.assertEqual(result["action"], "BUILD")
        self.assertEqual(result["selected"]["id"], "TASK-2")
        self.assertEqual(result["supervision"], "FRESH_OWNERSHIP_NOT_MATERIAL_PROOF")

    def test_supervisor_does_not_steal_stale_primary_claim(self):
        claimed = (1, "TASK-1", "EN_PROGRESO", "", EXECUTORS[0], STALE, "")
        result = run(claimed, SECOND, actor=EXECUTORS[1], supervised=EXECUTORS[0])
        self.assertEqual(result["action"], "BUILD")
        self.assertEqual(result["selected"]["id"], "TASK-2")
        self.assertEqual(result["recovery_candidate_needing_verified_terminal_run"]["id"], "TASK-1")
        observed = result["inspected_inflight"]["EN_PROGRESO"][0]
        self.assertEqual(observed["owner_automation_id"], EXECUTORS[0])
        self.assertFalse(observed["live_run_confirmed"])
        self.assertTrue(observed["external_run_readback_required"])
        self.assertEqual(result["supervision"], "STALE_OR_UNVERIFIED_OWNERSHIP")

    def test_actor_resumes_own_fresh_work_instead_of_claiming_another(self):
        claimed = (1, "TASK-1", "EN_PROGRESO", "", EXECUTORS[0], FRESH, "")
        result = run(claimed, SECOND)
        self.assertEqual(result["action"], "CONTINUE_OWNED_SCOPE")
        self.assertEqual(result["selected"]["id"], "TASK-1")

    def test_own_stale_lease_requires_safe_reconciliation(self):
        claimed = (1, "TASK-1", "EN_PROGRESO", "", EXECUTORS[0], STALE, "")
        result = run(claimed, SECOND)
        self.assertEqual(result["action"], "RECOVER_OWN_STALE_SCOPE")
        self.assertIn("FENCING", result["reason"])

    def test_duplicate_wip_cannot_claim_third_task(self):
        a = (1, "TASK-1", "EN_PROGRESO", "", EXECUTORS[0], FRESH, "")
        b = (2, "TASK-2", "VALIDANDO", "", EXECUTORS[0], FRESH, "")
        third = (3, "TASK-3", "PENDIENTE", "", "", "", "")
        result = run(a, b, third)
        self.assertEqual(result["action"], "RECONCILE_DUPLICATE_WIP")

    def test_validando_later_order_precedes_earlier_pending(self):
        pending = (1, "TASK-1", "PENDIENTE", "", "", "", "")
        validating = (2, "TASK-2", "VALIDANDO", "", "",
                      "RELEASED|OWNER=" + EXECUTORS[0] + "|TOKEN=prior",
                      "https://github.com/solqaryn/Solqaryn/pull/3691")
        chosen = run(pending, validating, actor=EXECUTORS[1])
        self.assertEqual(chosen["action"], "REVIEW")
        self.assertEqual(chosen["selected"]["id"], "TASK-2")
        self.assertIn("VALIDANDO_FIRST", chosen["reason"])

    def test_review_owner_is_inspected_without_stealing(self):
        reviewing = (1, "TASK-1", "VALIDANDO", "", EXECUTORS[2], FRESH,
                     "https://github.com/solqaryn/Solqaryn/pull/3691")
        candidate = run(reviewing, SECOND, actor=EXECUTORS[1])
        self.assertEqual(candidate["action"], "BUILD")
        self.assertEqual(candidate["selected"]["id"], "TASK-2")
        evidence = candidate["inspected_inflight"]["VALIDANDO"][0]
        self.assertEqual(evidence["owner_automation_id"], EXECUTORS[2])
        self.assertEqual(evidence["lease_observation"], "FRESH_LEASE_NOT_LIVE_RUN_PROOF")
        self.assertFalse(evidence["live_run_confirmed"])

    def test_lifecycle_reported_distinct_from_triage(self):
        response = run(AVAILABLE)
        self.assertEqual(response["canonical_lifecycle"],
                         ["PENDIENTE", "EN_PROGRESO", "VALIDANDO", "LISTO"])
        self.assertEqual(response["next_hierarchical_pending"]["id"], "TASK-1")

    def test_eight_startup_scopes_review_and_safe_recovery_before_pending(self):
        # Historical owners are NOT cleared merely because a lease expired.
        retired_primary = "6aa15346f5408191bdd9043fd26ff7aa"
        retired_author = "6aa1535f8cd48191b73e10f17843372a"
        def stale(owner):
            return (f"ACTIVE|OWNER={owner}|TOKEN=previous|"
                    "START=2026-10-20T16:40:00Z|"
                    "EXPIRES=2026-10-20T16:50:00Z|HEAD=" + HEAD)
        def delivered():
            return "RELEASED|OWNER=" + retired_author + "|TOKEN=handed_off"
        proof = "https://github.com/solqaryn/Solqaryn/pull/3691"
        items = [
            (4, "P0-002.B", "VALIDANDO", "", EXECUTORS[3], stale(EXECUTORS[3]), proof),
            (5, "P0-003.A", "EN_PROGRESO", "", retired_primary, stale(retired_primary), ""),
            (7, "P0-004.A", "EN_PROGRESO", "", EXECUTORS[1], stale(EXECUTORS[1]), ""),
            (9, "P0-005.A", "EN_PROGRESO", "", EXECUTORS[7], stale(EXECUTORS[7]), ""),
            (11, "P0-006.A", "EN_PROGRESO", "", EXECUTORS[3], stale(EXECUTORS[3]), ""),
            (13, "P0-007.A", "EN_PROGRESO", "", EXECUTORS[7], stale(EXECUTORS[7]), ""),
            (17, "P0-009.A", "VALIDANDO", "", "", delivered(), proof),
            (19, "P0-010.A", "VALIDANDO", "", "", delivered(), proof),
            (21, "P0-011.A", "PENDIENTE", "", "", "", ""),
        ]
        actual = run(*items, actor=EXECUTORS[4])
        self.assertEqual(actual["action"], "REVIEW")
        self.assertEqual(actual["selected"]["id"], "P0-009.A")
        self.assertEqual(len(actual["inspected_inflight"]["VALIDANDO"]), 3)
        self.assertEqual(len(actual["inspected_inflight"]["EN_PROGRESO"]), 5)
        self.assertEqual(actual["inspected_inflight"]["EN_PROGRESO"][0]
                         ["owner_automation_id"], retired_primary)
        self.assertFalse(actual["inspected_inflight"]["EN_PROGRESO"][0]
                         ["live_run_confirmed"])

        # Hypothetical certified closure of the two independent handoffs.
        closed = [
            (order, ident, "LISTO", dep, "", "RELEASED", evidence)
            if ident in {"P0-009.A", "P0-010.A"}
            else (order, ident, state, dep, owner, lease, evidence)
            for order, ident, state, dep, owner, lease, evidence in items
        ]
        fallback = run(*closed, actor=EXECUTORS[4])
        self.assertEqual(fallback["action"], "BUILD")
        self.assertEqual(fallback["selected"]["id"], "P0-011.A")
        self.assertEqual(fallback["recovery_candidate_needing_verified_terminal_run"]
                         ["id"], "P0-002.B")
        # Only after a real fenced recovery may old WIP become free.
        recovered = [
            (order, ident, "PENDIENTE", dep, "", "RECOVERED|FENCING_TOKEN=proof", evidence)
            if ident == "P0-003.A"
            else (order, ident, state, dep, owner, lease, evidence)
            for order, ident, state, dep, owner, lease, evidence in closed
        ]
        reclaimed = run(*recovered, actor=EXECUTORS[4])
        self.assertEqual(reclaimed["action"], "BUILD")
        self.assertEqual(reclaimed["selected"]["id"], "P0-003.A")

    def test_pending_hierarchy_skips_blocked_dependencies(self):
        blocked = (1, "TASK-1", "PENDIENTE", "TASK-3", "", "", "")
        available = (2, "TASK-2", "PENDIENTE", "", "", "", "")
        reviewer_busy = (3, "TASK-3", "VALIDANDO", "", EXECUTORS[0], FRESH, "")
        result = run(blocked, available, reviewer_busy, actor=EXECUTORS[1])
        self.assertEqual(result["action"], "BUILD")
        self.assertEqual(result["selected"]["id"], "TASK-2")

    def test_review_of_earlier_order_before_later_build(self):
        validating = (1, "TASK-1", "VALIDANDO", "", "",
                      "RELEASED|OWNER=" + EXECUTORS[0] + "|TOKEN=prior",
                      "https://github.com/solqaryn/Solqaryn/pull/3691")
        result = run(validating, SECOND, actor=EXECUTORS[1])
        self.assertEqual(result["action"], "REVIEW")
        self.assertEqual(result["selected"]["id"], "TASK-1")

    def test_original_author_cannot_review_own_work(self):
        validating = (1, "TASK-1", "VALIDANDO", "", "",
                      "RELEASED|OWNER=" + EXECUTORS[0] + "|TOKEN=prior",
                      "https://github.com/solqaryn/Solqaryn/pull/3691")
        result = run(validating, SECOND, actor=EXECUTORS[0])
        self.assertEqual(result["action"], "BUILD")
        self.assertEqual(result["selected"]["id"], "TASK-2")

    def test_review_without_evidence_is_not_claimable(self):
        validating = (1, "TASK-1", "VALIDANDO", "", "",
                      "RELEASED|OWNER=" + EXECUTORS[0], "")
        result = run(validating, SECOND, actor=EXECUTORS[1])
        self.assertEqual(result["action"], "BUILD")

    def test_review_with_foreign_owner_is_not_claimable(self):
        validating = (1, "TASK-1", "VALIDANDO", "", EXECUTORS[0],
                      FRESH, "https://github.com/solqaryn/Solqaryn/pull/3691")
        result = run(validating, SECOND, actor=EXECUTORS[1])
        self.assertEqual(result["action"], "BUILD")
        self.assertEqual(result["selected"]["id"], "TASK-2")

    def test_dependency_block_makes_other_independent_work_eligible(self):
        dep = (1, "TASK-1", "PENDIENTE", "TASK-2", "", "", "")
        blocker = (2, "TASK-2", "VALIDANDO", "", EXECUTORS[0], STALE, "")
        independent = (3, "TASK-3", "PENDIENTE", "", "", "", "")
        result = run(dep, blocker, independent, actor=EXECUTORS[1])
        self.assertEqual(result["action"], "BUILD")
        self.assertEqual(result["selected"]["id"], "TASK-3")
        self.assertEqual(result["recovery_candidate_needing_verified_terminal_run"]["id"], "TASK-2")

    def test_stale_validando_assessed_before_lower_order_en_progreso(self):
        older_progress = (1, "TASK-1", "EN_PROGRESO", "", EXECUTORS[0], STALE, "")
        later_validation = (2, "TASK-2", "VALIDANDO", "", EXECUTORS[2], STALE, "")
        pending = (3, "TASK-3", "PENDIENTE", "", "", "", "")
        result = run(older_progress, later_validation, pending, actor=EXECUTORS[1])
        self.assertEqual(result["action"], "BUILD")
        self.assertEqual(result["selected"]["id"], "TASK-3")
        self.assertEqual(result["recovery_candidate_needing_verified_terminal_run"]["id"], "TASK-2")
        self.assertEqual(result["recovery_candidate_needing_verified_terminal_run"]["state"], "VALIDANDO")
        self.assertEqual([x["id"] for x in result["inspected_inflight"]["EN_PROGRESO"]], ["TASK-1"])
        self.assertEqual([x["id"] for x in result["inspected_inflight"]["VALIDANDO"]], ["TASK-2"])

    def test_only_stale_owner_requires_recovery_not_idle(self):
        stale = (1, "TASK-1", "EN_PROGRESO", "", EXECUTORS[0], STALE, "")
        result = run(stale, actor=EXECUTORS[1])
        self.assertEqual(result["action"], "SAFE_RECOVERY_REQUIRED")
        self.assertTrue(result["needs_atomic_lease_before_write"])

    def test_no_eligible_work_may_wait_for_dependency(self):
        blocked = (1, "TASK-1", "BLOQUEADO", "", "", "", "")
        pending = (2, "TASK-2", "PENDIENTE", "TASK-1", "", "", "")
        result = run(blocked, pending)
        self.assertEqual(result["action"], "WAIT_DEPENDENCIES")

    def test_foreign_supervision_pair_rejected(self):
        c, r = tables(AVAILABLE)
        with self.assertRaisesRegex(UnsafeSnapshot, "UNAUTHORIZED_SUPERVISION_PAIR"):
            decide(c, r, actor_id=EXECUTORS[1], supervised_actor_id=EXECUTORS[2],
                   now=NOW, expected_count=1)

    def test_outsider_cannot_write_as_unrecognized_executor(self):
        c, r = tables(AVAILABLE)
        with self.assertRaisesRegex(UnsafeSnapshot, "UNKNOWN_EXECUTOR"):
            decide(c, r, actor_id="f" * 32, now=NOW, expected_count=1)

    def test_naive_time_rejected(self):
        c, r = tables(AVAILABLE)
        with self.assertRaisesRegex(UnsafeSnapshot, "UTC_AWARE_TIMESTAMP_REQUIRED"):
            decide(c, r, actor_id=EXECUTORS[0],
                   now=datetime(2026, 10, 20, 17), expected_count=1)

    def test_build_selection_is_not_claim_or_certification(self):
        result = run(AVAILABLE)
        self.assertEqual(result["action"], "BUILD")
        self.assertTrue(result["needs_atomic_lease_before_write"])
        self.assertFalse(result["is_resident_process"])


if __name__ == "__main__":
    unittest.main()
