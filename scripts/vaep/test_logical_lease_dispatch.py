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
        self.assertEqual({x["order"] for x in winners}, set(range(1, 20, 2)))

    def test_real_plan_odd_order_regression_does_not_starve_supervisors(self):
        # Before this regression, ORDER modulo ten selected only 6/10 lanes
        # for 31 pending scopes, because most eligible ORDERs were odd.
        representative_orders = [4, 5, 7, 9, 11, 13, 17, 19, 21] + list(range(25, 69, 2))
        self.assertEqual(len(representative_orders), 31)
        routes = [writer_for_order(n, CANONICAL_EXECUTORS, EPOCH)
                  for n in representative_orders]
        self.assertEqual(set(routes), set(CANONICAL_EXECUTORS))
        # Static pairing prevents shifting ownership as tasks complete.
        for n in range(1, 1123, 2):
            self.assertEqual(writer_for_order(n, CANONICAL_EXECUTORS, EPOCH),
                             writer_for_order(n + 1, CANONICAL_EXECUTORS, EPOCH))

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
            self.assertEqual(x, CANONICAL_EXECUTORS[((n - 1) // 2) % 10])

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



class LogicalLifecycleTests(unittest.TestCase):
    def test_review_first_handoff_and_independent_listo(self):
        from logical_lease_dispatch import (
            select_action, select_review, build_review_claim, build_review_completion,
        )
        c, r = rows()
        author, reviewer = CANONICAL_EXECUTORS[:2]
        pool = [author, reviewer]
        r[1][1] = "VALIDANDO"
        r[1][3] = f"RELEASED|ORIGINAL_AUTHOR={author}|TOKEN=oldtoken"
        r[1][5] = "https://github.com/solqaryn/Solqaryn/pull/3701"
        review_choice = select_review(c, r, actor_id=reviewer, writer_ids=pool,
                                      epoch=EPOCH, expected_count=20)
        self.assertEqual(review_choice["action"], "REVIEW")
        self.assertEqual(review_choice["selected"], "T-0001")
        self.assertEqual(select_action(c, r, actor_id=reviewer, writer_ids=pool,
                                       epoch=EPOCH, expected_count=20)["action"], "REVIEW")
        self.assertNotEqual(select_review(c, r, actor_id=author, writer_ids=pool,
                                          epoch=EPOCH, expected_count=20)["action"], "REVIEW")
        claim = build_review_claim(c, r, task_id="T-0001", actor_id=reviewer,
                                   writer_ids=pool, epoch=EPOCH,
                                   run_id="SUP05-20261021T080500Z", head=HEAD,
                                   now=datetime.now(timezone.utc),
                                   runtime_sheet_id=3, events_sheet_id=4,
                                   previous_run_terminal_proven=True,
                                   expected_count=20)
        self.assertEqual(claim.author, author)
        self.assertEqual(len(claim.requests), 3)
        r[1][2] = reviewer
        r[1][3] = claim.lease
        with self.assertRaisesRegex(UnsafeSnapshot, "GATES_MISSING"):
            build_review_completion(claim, r, now=datetime.now(timezone.utc),
                                    review_evidence_url="https://github.com/solqaryn/Solqaryn/pull/3701",
                                    exact_head_checks_passed=True, p0=1, p1=0,
                                    runtime_sheet_id=3, events_sheet_id=4)
        released, reqs = build_review_completion(
            claim, r, now=datetime.now(timezone.utc),
            review_evidence_url="https://github.com/solqaryn/Solqaryn/pull/3701",
            exact_head_checks_passed=True, p0=0, p1=0,
            runtime_sheet_id=3, events_sheet_id=4)
        self.assertIn(f"ORIGINAL_AUTHOR={author}", released)
        self.assertEqual(len(reqs), 3)
        self.assertEqual(reqs[0]["updateCells"]["rows"][0]["values"][0]["userEnteredValue"]["stringValue"], "LISTO")

    def test_build_owner_can_only_handoff_not_listo(self):
        from logical_lease_dispatch import build_handoff
        c, r = rows()
        author = CANONICAL_EXECUTORS[0]
        claim = build_logical_claim(c, r, task_id="T-0001", actor_id=author,
                                    writer_ids=[author], epoch=EPOCH,
                                    run_id="TASK00-20261021T080000Z", head=HEAD,
                                    now=datetime.now(timezone.utc),
                                    runtime_sheet_id=3, events_sheet_id=4,
                                    previous_run_terminal_proven=True,
                                    expected_count=20)
        r[1][1] = "EN_PROGRESO"
        r[1][2] = author
        r[1][3] = claim.lease
        with self.assertRaisesRegex(UnsafeSnapshot, "EVIDENCE_REQUIRED"):
            build_handoff(claim, r, evidence_url="missing", now=datetime.now(timezone.utc),
                          runtime_sheet_id=3, events_sheet_id=4)
        lease, requests = build_handoff(
            claim, r, evidence_url="https://github.com/solqaryn/Solqaryn/pull/3701",
            now=datetime.now(timezone.utc), runtime_sheet_id=3, events_sheet_id=4)
        self.assertEqual(len(requests), 4)
        self.assertIn(f"ORIGINAL_AUTHOR={author}", lease)
        self.assertEqual(requests[0]["updateCells"]["rows"][0]["values"][0]["userEnteredValue"]["stringValue"], "VALIDANDO")



class LogicalSafetyRegressionTests(unittest.TestCase):
    def test_build_claim_must_not_bypass_review_priority(self):
        from logical_lease_dispatch import select_action
        c, r = rows()
        author, reviewer = CANONICAL_EXECUTORS[:2]
        pool = [author, reviewer]
        r[1][1] = "VALIDANDO"
        r[1][3] = f"RELEASED|ORIGINAL_AUTHOR={author}|TOKEN=previous"
        r[1][5] = "https://github.com/solqaryn/Solqaryn/pull/3701"
        self.assertEqual(select_action(
            c, r, actor_id=reviewer, writer_ids=pool, epoch=EPOCH,
            expected_count=20)["action"], "REVIEW")
        with self.assertRaisesRegex(UnsafeSnapshot, "LOGICAL_CLAIM_NOT_ADMISSIBLE"):
            build_logical_claim(c, r, task_id="T-0003", actor_id=reviewer,
                                writer_ids=pool, epoch=EPOCH,
                                run_id="SUP05-20261021T080500Z", head=HEAD,
                                now=datetime.now(timezone.utc), runtime_sheet_id=3,
                                events_sheet_id=4,
                                previous_run_terminal_proven=True,
                                expected_count=20)

    def test_expired_builder_cannot_handoff(self):
        from datetime import timedelta
        from logical_lease_dispatch import build_handoff
        c, r = rows()
        now = datetime(2026, 10, 21, 10, tzinfo=timezone.utc)
        owner = CANONICAL_EXECUTORS[0]
        claim = build_logical_claim(c, r, task_id="T-0001", actor_id=owner,
                                    writer_ids=[owner], epoch=EPOCH,
                                    run_id="TASK00-20261021T100000Z", head=HEAD,
                                    now=now, runtime_sheet_id=3, events_sheet_id=4,
                                    previous_run_terminal_proven=True,
                                    expected_count=20)
        r[1][1], r[1][2], r[1][3] = "EN_PROGRESO", owner, claim.lease
        with self.assertRaisesRegex(UnsafeSnapshot, "LOGICAL_LEASE_EXPIRED"):
            build_handoff(claim, r, evidence_url="https://github.com/solqaryn/Solqaryn/pull/3701",
                          now=now + timedelta(minutes=11),
                          runtime_sheet_id=3, events_sheet_id=4)

    def test_expired_reviewer_cannot_certify(self):
        from datetime import timedelta
        from logical_lease_dispatch import build_review_claim, build_review_completion
        c, r = rows()
        a, b = CANONICAL_EXECUTORS[:2]
        r[1][1] = "VALIDANDO"
        r[1][3] = f"RELEASED|ORIGINAL_AUTHOR={a}|TOKEN=previous"
        r[1][5] = "https://github.com/solqaryn/Solqaryn/pull/3701"
        now = datetime(2026, 10, 21, 10, tzinfo=timezone.utc)
        review = build_review_claim(
            c, r, task_id="T-0001", actor_id=b,
            writer_ids=[a, b], epoch=EPOCH, run_id="SUP05-20261021T100000Z",
            head=HEAD, now=now, runtime_sheet_id=3, events_sheet_id=4,
            previous_run_terminal_proven=True, expected_count=20)
        r[1][2], r[1][3] = b, review.lease
        with self.assertRaisesRegex(UnsafeSnapshot, "LOGICAL_LEASE_EXPIRED"):
            build_review_completion(review, r,
                                    now=now + timedelta(minutes=11),
                                    review_evidence_url="https://github.com/solqaryn/Solqaryn/pull/3701",
                                    exact_head_checks_passed=True, p0=0, p1=0,
                                    runtime_sheet_id=3, events_sheet_id=4)

    def test_transition_readback_and_partial_failure(self):
        from logical_lease_dispatch import (
            build_handoff, verify_handoff_response, build_review_claim,
            verify_review_claim_response, build_review_completion,
            verify_review_completion_response,
        )
        c, r = rows()
        author, reviewer = CANONICAL_EXECUTORS[:2]
        pool = [author, reviewer]
        now = datetime.now(timezone.utc)
        delivery = "https://github.com/solqaryn/Solqaryn/pull/3701"
        claim = build_logical_claim(c, r, task_id="T-0001", actor_id=author,
                                    writer_ids=pool, epoch=EPOCH,
                                    run_id="TASK00-20261021T100000Z", head=HEAD,
                                    now=now, runtime_sheet_id=3, events_sheet_id=4,
                                    previous_run_terminal_proven=True,
                                    expected_count=20)
        r[1][1], r[1][2], r[1][3], r[1][4] = "EN_PROGRESO", author, claim.lease, HEAD
        lease, reqs = build_handoff(claim, r, evidence_url=delivery, now=now,
                                     runtime_sheet_id=3, events_sheet_id=4)
        r[1][1], r[1][2], r[1][3], r[1][5] = "VALIDANDO", "", lease, delivery
        self.assertTrue(verify_handoff_response(claim, lease, {"replies": [{}, {}, {}, {}]}, r[1], delivery))
        with self.assertRaisesRegex(UnsafeSnapshot, "REPLY_COUNT"):
            verify_handoff_response(claim, lease, {"replies": [{}, {}]}, r[1], delivery)
        rev = build_review_claim(c, r, task_id="T-0001", actor_id=reviewer,
                                 writer_ids=pool, epoch=EPOCH,
                                 run_id="SUP05-20261021T100500Z", head=HEAD,
                                 now=now, runtime_sheet_id=3, events_sheet_id=4,
                                 previous_run_terminal_proven=True, expected_count=20)
        r[1][2], r[1][3] = reviewer, rev.lease
        self.assertTrue(verify_review_claim_response(rev, {"replies": [{}, {}, {}]}, r[1]))
        new_lease, final_requests = build_review_completion(
            rev, r, now=now, review_evidence_url=delivery,
            exact_head_checks_passed=True, p0=0, p1=0,
            runtime_sheet_id=3, events_sheet_id=4)
        r[1][1], r[1][2], r[1][3] = "LISTO", "", new_lease
        self.assertTrue(verify_review_completion_response(
            rev, new_lease, {"replies": [{}, {}, {}]}, r[1]))
        r[1][3] = "RELEASED|WRONG_TOKEN"
        with self.assertRaisesRegex(UnsafeSnapshot, "LOGICAL_LISTO_READBACK_MISMATCH"):
            verify_review_completion_response(rev, new_lease, {"replies": [{}, {}, {}]}, r[1])



class LogicalRecoveryTests(unittest.TestCase):
    def test_review_rejection_only_original_author_can_rework(self):
        from logical_lease_dispatch import (
            build_review_claim, build_review_rejection,
            verify_review_rejection_response, select_review, select_action,
            build_rework_claim,
        )
        c, r = rows()
        a, b = CANONICAL_EXECUTORS[:2]
        now = datetime(2026, 10, 21, 10, tzinfo=timezone.utc)
        r[1][1], r[1][3] = ("VALIDANDO", f"RELEASED|ORIGINAL_AUTHOR={a}|TOKEN=handoff")
        r[1][5] = "https://github.com/solqaryn/Solqaryn/pull/3701"
        review = build_review_claim(
            c, r, task_id="T-0001", actor_id=b, writer_ids=[a, b],
            epoch=EPOCH, run_id="SUP05-20261021T100000Z",
            head=HEAD, now=now, runtime_sheet_id=3, events_sheet_id=4,
            previous_run_terminal_proven=True, expected_count=20)
        r[1][2], r[1][3], r[1][4] = b, review.lease, HEAD
        rejected, reqs = build_review_rejection(
            review, r, now=now, review_evidence_url=r[1][5],
            reason="Tests de seguridad pendientes", runtime_sheet_id=3,
            events_sheet_id=4)
        self.assertEqual(len(reqs), 4)
        self.assertIn("REVIEW_REJECTED=1", rejected)
        r[1][2], r[1][3] = "", rejected
        self.assertTrue(verify_review_rejection_response(
            review, rejected, {"replies": [{}, {}, {}, {}]}, r[1]))
        self.assertNotEqual(select_review(
            c, r, actor_id=b, writer_ids=[a, b],
            epoch=EPOCH, expected_count=20)["action"], "REVIEW")
        action = select_action(c, r, actor_id=a, writer_ids=[a, b],
                               epoch=EPOCH, expected_count=20)
        self.assertEqual((action["action"], action["selected"]), ("REWORK", "T-0001"))
        with self.assertRaisesRegex(UnsafeSnapshot, "REWORK_NOT_ADMISSIBLE"):
            build_rework_claim(c, r, task_id="T-0001", actor_id=b,
                               writer_ids=[a, b], epoch=EPOCH,
                               run_id="SUP05-20261021T110000Z", head=HEAD,
                               now=now, runtime_sheet_id=3, events_sheet_id=4,
                               previous_run_terminal_proven=True, expected_count=20)
        rework = build_rework_claim(
            c, r, task_id="T-0001", actor_id=a,
            writer_ids=[a, b], epoch=EPOCH, run_id="TASK00-20261021T110000Z",
            head=HEAD, now=now, runtime_sheet_id=3, events_sheet_id=4,
            previous_run_terminal_proven=True, expected_count=20)
        self.assertEqual(rework.task_id, "T-0001")
        self.assertEqual(len(rework.requests), 3)

    def test_heartbeat_and_fenced_own_stale_recovery(self):
        from datetime import timedelta
        from logical_lease_dispatch import (
            build_heartbeat, verify_heartbeat_response,
            build_own_stale_recovery, verify_own_stale_recovery_response,
            build_handoff,
        )
        c, r = rows()
        actor = CANONICAL_EXECUTORS[0]
        now = datetime(2026, 10, 21, 10, tzinfo=timezone.utc)
        claim = build_logical_claim(
            c, r, task_id="T-0001", actor_id=actor, writer_ids=[actor],
            epoch=EPOCH, run_id="TASK00-20261021T100000Z", head=HEAD, now=now,
            runtime_sheet_id=3, events_sheet_id=4,
            previous_run_terminal_proven=True, expected_count=20)
        r[1][1], r[1][2], r[1][3], r[1][4] = "EN_PROGRESO", actor, claim.lease, HEAD
        with self.assertRaisesRegex(UnsafeSnapshot, "LIVE_RUN_PROOF_REQUIRED"):
            build_heartbeat(claim, r, now=now + timedelta(minutes=2),
                            runtime_sheet_id=3, events_sheet_id=4)
        updated = build_heartbeat(
            claim, r, now=now + timedelta(minutes=2),
            run_still_active_proven=True, runtime_sheet_id=3, events_sheet_id=4)
        self.assertEqual(updated.token, claim.token)
        self.assertNotEqual(updated.lease, claim.lease)
        r[1][3] = updated.lease
        self.assertTrue(verify_heartbeat_response(
            updated, {"replies": [{}, {}, {}]}, r[1]))
        with self.assertRaisesRegex(UnsafeSnapshot, "TERMINAL_RUN_PROOF_REQUIRED"):
            build_own_stale_recovery(
                updated, r, now=now + timedelta(minutes=14),
                runtime_sheet_id=3, events_sheet_id=4)
        with self.assertRaisesRegex(UnsafeSnapshot, "LEASE_STILL_FRESH"):
            build_own_stale_recovery(
                updated, r, now=now + timedelta(minutes=9),
                previous_run_terminal_proven=True,
                runtime_sheet_id=3, events_sheet_id=4)
        recovered, reqs = build_own_stale_recovery(
            updated, r, now=now + timedelta(minutes=14),
            previous_run_terminal_proven=True,
            runtime_sheet_id=3, events_sheet_id=4)
        self.assertEqual(len(reqs), 3)
        self.assertIn("FENCING_TOKEN=", recovered)
        r[1][1], r[1][2], r[1][3] = "PENDIENTE", "", recovered
        self.assertTrue(verify_own_stale_recovery_response(
            updated, recovered, {"replies": [{}, {}, {}]}, r[1]))
        with self.assertRaisesRegex(UnsafeSnapshot, "LOGICAL_HANDOFF_OWNERSHIP_MISMATCH"):
            build_handoff(updated, r, evidence_url="https://github.com/solqaryn/Solqaryn/pull/3701",
                          now=now + timedelta(minutes=14),
                          runtime_sheet_id=3, events_sheet_id=4)



class LogicalCutoverGateTests(unittest.TestCase):
    def test_repository_manifest_is_inactive(self):
        import json
        from pathlib import Path
        from logical_lease_dispatch import evaluate_live_cutover_manifest
        path = Path(__file__).resolve().parents[2] / "vaep/control/logical-lease-candidate.json"
        with path.open(encoding="utf-8") as source:
            manifest = json.load(source)
        self.assertEqual(manifest["mode"], "PROPOSAL_ONLY")
        self.assertEqual(manifest["verified_writer_task_ids"], [])
        self.assertFalse(manifest["safety_check_bypass_allowed"])
        with self.assertRaisesRegex(UnsafeSnapshot, "LOGICAL_CUTOVER_NOT_ACTIVATED"):
            evaluate_live_cutover_manifest(
                manifest, dev_sha=HEAD,
                sheet_writer_policies=["LOGICAL_SINGLE_OWNER_TASK_WRITE_VERIFIED"] * 10,
                proven_task_writers=CANONICAL_EXECUTORS[:2],
                prior_runs_terminal=True)

    def test_hypothetical_cutover_cannot_mix_protocols_or_skip_task_proofs(self):
        from logical_lease_dispatch import evaluate_live_cutover_manifest
        writers = list(CANONICAL_EXECUTORS[:2])
        policy = "LOGICAL_SINGLE_OWNER_TASK_WRITE_VERIFIED"
        manifest = {
            "schema_version": 1, "platform": "SOLQARYN",
            "runtime_model": "TASKS_ONLY", "branch": "dev",
            "plan_task_count": 1123,
            "plan_drive_file_id": "1T41ppLvC3x-p6sORCtpaPvzLWPP9_Z7j",
            "safety_check_bypass_allowed": False,
            "activation_permission": "OWNER_APPROVED_REVIEWED",
            "base_dev_sha": HEAD, "routing_epoch": EPOCH,
            "write_policy_required": policy, "mode": "ACTIVE_CERTIFIED",
            "verified_writer_task_ids": writers}
        args = {
            "dev_sha": HEAD, "sheet_writer_policies": [policy] * 10,
            "proven_task_writers": writers, "prior_runs_terminal": True}
        decision = evaluate_live_cutover_manifest(manifest, **args)
        self.assertEqual(decision["writer_ids"], tuple(writers))
        with self.assertRaisesRegex(UnsafeSnapshot, "LOGICAL_MIXED_PROTOCOL_FORBIDDEN"):
            evaluate_live_cutover_manifest(
                manifest, **{**args, "sheet_writer_policies": ["DUAL_CAS"] * 10})
        with self.assertRaisesRegex(UnsafeSnapshot, "LOGICAL_TASK_OR_INDEPENDENT_REVIEW_PROOFS_MISSING"):
            evaluate_live_cutover_manifest(
                manifest, **{**args, "proven_task_writers": writers[:1]})
        with self.assertRaisesRegex(UnsafeSnapshot, "LOGICAL_CUTOVER_AUTHORIZATION_OR_TERMINAL_PROOF_MISSING"):
            evaluate_live_cutover_manifest(
                manifest, **{**args, "prior_runs_terminal": False})
        with self.assertRaisesRegex(UnsafeSnapshot, "LOGICAL_MANIFEST_SCOPE_LOCK_MISMATCH"):
            evaluate_live_cutover_manifest(
                {**manifest, "safety_check_bypass_allowed": True}, **args)



class LogicalHistoricProvenanceTests(unittest.TestCase):
    def test_historical_author_equal_default_reviewer_is_rerouted(self):
        from logical_lease_dispatch import (
            reviewer_for_original_author, reviewer_for_order, select_review,
            build_review_claim,
        )
        c, r = rows()
        pool = list(CANONICAL_EXECUTORS[:3])
        original = reviewer_for_order(1, pool, EPOCH)
        self.assertEqual(original, pool[1])
        candidate = reviewer_for_original_author(1, pool, EPOCH, original)
        self.assertEqual(candidate, pool[2])
        self.assertNotEqual(candidate, original)
        r[1][1] = "VALIDANDO"
        r[1][3] = f"RELEASED|ORIGINAL_AUTHOR={original}|TOKEN=old"
        r[1][5] = "https://github.com/solqaryn/Solqaryn/pull/3705"
        decision = select_review(
            c, r, actor_id=candidate, writer_ids=pool,
            epoch=EPOCH, expected_count=20)
        self.assertEqual(decision["selected"], "T-0001")
        self.assertEqual(decision["original_author"], original)
        self.assertNotEqual(
            select_review(c, r, actor_id=original, writer_ids=pool,
                          epoch=EPOCH, expected_count=20)["action"], "REVIEW")
        claim = build_review_claim(
            c, r, task_id="T-0001", actor_id=candidate,
            writer_ids=pool, epoch=EPOCH,
            run_id="TASK24-20261021T110000Z", head=HEAD,
            now=datetime.now(timezone.utc),
            runtime_sheet_id=3, events_sheet_id=4,
            previous_run_terminal_proven=True, expected_count=20)
        self.assertEqual(claim.reviewer, candidate)
        self.assertEqual(claim.author, original)

    def test_rejected_delivery_goes_to_original_author_after_epoch_reroute(self):
        from logical_lease_dispatch import select_rework, select_action, build_rework_claim
        c, r = rows()
        pool = list(CANONICAL_EXECUTORS[:3])
        original = pool[2]
        self.assertNotEqual(original, pool[0])
        r[1][1] = "VALIDANDO"
        r[1][3] = (f"RELEASED|ORIGINAL_AUTHOR={original}|REVIEW_REJECTED=1|"
                   "REVIEWER=prior|TOKEN=old")
        r[1][5] = "https://github.com/solqaryn/Solqaryn/pull/3705"
        candidate = select_rework(c, r, actor_id=original,
                                  writer_ids=pool, epoch=EPOCH,
                                  expected_count=20)
        self.assertEqual((candidate["action"], candidate["selected"]),
                         ("REWORK", "T-0001"))
        self.assertEqual(select_action(c, r, actor_id=original,
                                       writer_ids=pool, epoch=EPOCH,
                                       expected_count=20)["action"], "REWORK")
        for other in pool[:2]:
            self.assertNotEqual(
                select_rework(c, r, actor_id=other, writer_ids=pool,
                              epoch=EPOCH, expected_count=20)["action"], "REWORK")
        claim = build_rework_claim(
            c, r, task_id="T-0001", actor_id=original,
            writer_ids=pool, epoch=EPOCH,
            run_id="TASK24-20261021T110000Z", head=HEAD,
            now=datetime.now(timezone.utc), runtime_sheet_id=3,
            events_sheet_id=4, previous_run_terminal_proven=True,
            expected_count=20)
        self.assertEqual(claim.actor_id, original)

    def test_historic_author_malformed_or_single_writer_refused(self):
        from logical_lease_dispatch import reviewer_for_original_author
        pool = CANONICAL_EXECUTORS[:3]
        with self.assertRaisesRegex(UnsafeSnapshot, "INVALID_ORIGINAL_AUTHOR"):
            reviewer_for_original_author(1, pool, EPOCH, "fake")
        with self.assertRaisesRegex(UnsafeSnapshot, "INDEPENDENT_REVIEWER_NOT_AVAILABLE"):
            reviewer_for_original_author(1, pool[:1], EPOCH, pool[1])
