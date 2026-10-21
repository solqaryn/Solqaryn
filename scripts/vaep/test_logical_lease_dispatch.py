import unittest
from datetime import datetime, timezone

from logical_lease_dispatch import (
    CANONICAL_EXECUTORS, freeze_pool, writer_for_order, reviewer_for_order,
    select, build_logical_claim, verify_logical_claim,
)
from commercial_dispatch import UnsafeSnapshot

EPOCH = "VAEP_LOGICAL_20261021_INITIAL"
HEAD = "a" * 40


def rows(count=20):
    commercial = [["ID", "PROGRAMA_PRIORIDAD", "MICROTAREA", "CRITERIO_EXIGIDO", "ORDEN"]]
    runtime = [["ID", "ESTADO", "OWNER_AUTOMATION_ID", "LEASE_STATUS", "HEAD",
                "EVIDENCIA", "BLOQUEO", "NEXT_ACTION", "UPDATED_AT", "DEPENDENCIAS_ACTUALES"]]
    for n in range(1, count + 1):
        ident = f"T-{n:04}"
        commercial.append([ident, "P0", "scope", "PASS", str(n)])
        runtime.append([ident, "PENDIENTE", "", "", "", "", "", "", "", ""])
    return commercial, runtime


class LogicalLeaseTests(unittest.TestCase):
    def test_ten_writers_are_disjoint_for_all_scopes(self):
        c, r = rows(30)
        winners = [select(c, r, actor_id=actor, writer_ids=CANONICAL_EXECUTORS,
                          epoch=EPOCH, expected_count=30) for actor in CANONICAL_EXECUTORS]
        self.assertEqual({x["action"] for x in winners}, {"BUILD"})
        self.assertEqual(len({x["selected"] for x in winners}), 10)
        self.assertEqual({x["order"] for x in winners}, set(range(1, 11)))

    def test_one_verified_writer_can_work_without_claim_from_other_nine(self):
        c, r = rows()
        writer = CANONICAL_EXECUTORS[0]
        self.assertEqual(select(c, r, actor_id=writer, writer_ids=[writer],
                                epoch=EPOCH, expected_count=20)["selected"], "T-0001")
        for other in CANONICAL_EXECUTORS[1:]:
            self.assertEqual(select(c, r, actor_id=other, writer_ids=[writer],
                                    epoch=EPOCH, expected_count=20)["action"],
                             "NO_GITHUB_TASK_WRITE_PROOF")

    def test_stable_route_and_separate_review(self):
        for n in range(1, 1130):
            x = writer_for_order(n, CANONICAL_EXECUTORS, EPOCH)
            y = reviewer_for_order(n, CANONICAL_EXECUTORS, EPOCH)
            self.assertNotEqual(x, y)
            self.assertEqual(x, CANONICAL_EXECUTORS[(n - 1) % 10])

    def test_fail_closed_missing_reviewer(self):
        with self.assertRaisesRegex(UnsafeSnapshot, "INDEPENDENT_REVIEWER_NOT_AVAILABLE"):
            reviewer_for_order(1, [CANONICAL_EXECUTORS[0]], EPOCH)

    def test_stale_foreign_owner_never_claimed(self):
        c, r = rows()
        r[1][2] = CANONICAL_EXECUTORS[1]
        r[1][3] = "ACTIVE|EXPIRES=2025-01-01T00:00:00Z"
        sel = select(c, r, actor_id=CANONICAL_EXECUTORS[0],
                     writer_ids=[CANONICAL_EXECUTORS[0]], epoch=EPOCH,
                     expected_count=20)
        self.assertEqual(sel["selected"], "T-0002")

    def test_block_unmet_dependencies(self):
        c, r = rows()
        r[1][9] = "T-0002"
        x = select(c, r, actor_id=CANONICAL_EXECUTORS[0],
                   writer_ids=[CANONICAL_EXECUTORS[0]], epoch=EPOCH,
                   expected_count=20)
        self.assertEqual(x["selected"], "T-0002")

    def test_active_owner_must_resume_not_take_next(self):
        c, r = rows()
        r[1][1] = "EN_PROGRESO"
        r[1][2] = CANONICAL_EXECUTORS[0]
        r[1][3] = "ACTIVE|OWNER=..."
        x = select(c, r, actor_id=CANONICAL_EXECUTORS[0],
                   writer_ids=[CANONICAL_EXECUTORS[0]], epoch=EPOCH,
                   expected_count=20)
        self.assertEqual(x["action"], "CONTINUE_OWNED_OR_RECOVER")
        self.assertEqual(x["selected"], "T-0001")

    def test_no_forged_pool_or_new_epoch(self):
        for pool, epoch in [(["wrong"], EPOCH), ([], EPOCH),
                            ([CANONICAL_EXECUTORS[1], CANONICAL_EXECUTORS[0]], EPOCH),
                            ([CANONICAL_EXECUTORS[0]] * 2, EPOCH),
                            ([CANONICAL_EXECUTORS[0]], "unapproved")]:
            with self.assertRaises(UnsafeSnapshot):
                freeze_pool(pool, epoch)

    def test_simple_batch_no_cas_and_readback(self):
        c, r = rows()
        task = build_logical_claim(
            c, r, task_id="T-0001", actor_id=CANONICAL_EXECUTORS[0],
            writer_ids=[CANONICAL_EXECUTORS[0]], epoch=EPOCH,
            run_id="TASK00-20261021T080000Z", head=HEAD,
            now=datetime(2026, 10, 21, 14, tzinfo=timezone.utc),
            runtime_sheet_id=2017583524, events_sheet_id=958655496,
            previous_run_terminal_proven=True, expected_count=20)
        self.assertEqual(len(task.requests), 3)
        self.assertFalse(any("createDeveloperMetadata" in req for req in task.requests))
        self.assertEqual(task.requests[2]["appendCells"]["sheetId"], 958655496)
        self.assertTrue(verify_logical_claim(
            task, {"replies": [{}, {}, {}]},
            ["T-0001", "EN_PROGRESO", CANONICAL_EXECUTORS[0], task.lease]))
        with self.assertRaisesRegex(UnsafeSnapshot, "MISMATCH"):
            verify_logical_claim(task, {"replies": [{}, {}, {}]},
                                 ["T-0001", "EN_PROGRESO", CANONICAL_EXECUTORS[1], task.lease])

    def test_never_claim_without_previous_run_terminal_or_correct_writer(self):
        c, r = rows()
        kw = dict(task_id="T-0001", actor_id=CANONICAL_EXECUTORS[0],
                  writer_ids=[CANONICAL_EXECUTORS[0]], epoch=EPOCH,
                  run_id="TASK00-20261021T080000Z", head=HEAD,
                  now=datetime.now(timezone.utc), runtime_sheet_id=3,
                  events_sheet_id=4, expected_count=20)
        with self.assertRaisesRegex(UnsafeSnapshot, "TERMINAL_PROOF_REQUIRED"):
            build_logical_claim(c, r, **kw)
        kw["previous_run_terminal_proven"] = True
        kw["actor_id"] = CANONICAL_EXECUTORS[1]
        with self.assertRaisesRegex(UnsafeSnapshot, "NOT_ADMISSIBLE"):
            build_logical_claim(c, r, **kw)


if __name__ == "__main__":
    unittest.main()
