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
