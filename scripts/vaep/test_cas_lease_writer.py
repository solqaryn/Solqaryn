"""Contract tests: atomic claim, no skips, no false ACTIVE_REAL, safe release.

Includes comprehensive negative, concurrency, fencing, and fault-tolerance tests
required by VAEP Review 5588458124 and section D of SOLQARYN executive order.
"""
import unittest
from datetime import datetime, timedelta, timezone

from cas_lease_writer import (
    METADATA_BASE, METADATA_KEY, TTL_MINUTES, UnsafeSnapshot,
    EXECUTOR_METADATA_BASE, EXECUTOR_METADATA_KEY,
    CANONICAL_PRIMARY_IDS, CANONICAL_SUPERVISOR_IDS, CANONICAL_AUTOMATION_IDS,
    build_claim, build_release, verify_claim_response, verify_release_response,
    build_review_claim, verify_review_claim_response,
    build_review_completion, verify_review_completion_response,
    build_stale_lease_recovery, verify_stale_lease_recovery_response,
    build_takeover_claim, verify_takeover_claim_response,
)

COMMERCIAL = ["ID","PROGRAMA_PRIORIDAD","MICROTAREA","CRITERIO_EXIGIDO","ORDEN","PLAN_COMERCIAL_DOC_ID","VERSION_FUENTE","UNIDAD_ORIGEN_LANE"]
RUNTIME = ["ID","ESTADO","OWNER_AUTOMATION_ID","LEASE_STATUS","HEAD","EVIDENCIA","BLOQUEO","NEXT_ACTION","UPDATED_AT","DEPENDENCIAS_ACTUALES","DEPENDENCIAS_OK","PRIORIDAD_ACTUAL","P0","P1","ROLLBACK","RESULTADO","NOTA_RUNTIME","RESERVED"]
OWNER = "6ad7bbf79c488191bf893e6c08ea9b5f"
HEAD = "ba4edfffcb2473c42d731335cf25dc33121a72ad"
NOW = datetime(2026, 10, 20, 14, 30, tzinfo=timezone.utc)
REVIEWER = "6abd6235e100819195785fc76a44a8e0"


def fixture(n=2):
    comm = [COMMERCIAL.copy()]
    run = [RUNTIME.copy()]
    for i in range(1, n+1):
        task_id = f"P{i:03d}"
        comm.append([task_id, "01", task_id, "", str(i), "", "", "ANY"])
        run.append([task_id, "PENDIENTE", "", "", "", "", "", "", "", "", "SI", "R01", "", "", "", "", "", ""])
    return comm, run


def claim(*, n=2, commercial=None, runtime=None, chosen="P001", now=NOW, head=HEAD, run_id="RUN_123", owner=OWNER):
    c, r = fixture(n)
    return build_claim(
        commercial or c, runtime or r,
        task_id=chosen, owner=owner, run_id=run_id, head=head, now=now,
        runtime_sheet_id=2017583524, events_sheet_id=958655496,
        expected_count=len((commercial or c))-1,
    )


def confirmed_meta(k):
    return {"metadataId": k.metadata_id,
            "metadataKey": METADATA_KEY, "metadataValue": k.metadata_value}


def confirmed_exec_meta(k):
    return {"metadataId": k.executor_metadata_id,
            "metadataKey": EXECUTOR_METADATA_KEY, "metadataValue": k.executor_metadata_value}


def claimed_runtime(k):
    return [k.task_id, "EN_PROGRESO", k.owner, 
            f"ACTIVE|OWNER={k.owner}|TOKEN={k.token}|RUN_ID={k.run_id}|HEAD={k.head}"]


def claim_replies(k):
    return {
        "replies": [
            {"createDeveloperMetadata": {"developerMetadata": confirmed_meta(k)}},
            {"createDeveloperMetadata": {"developerMetadata": confirmed_exec_meta(k)}},
        ]
    }


class LeaseWriterTests(unittest.TestCase):
    def test_migrated_primary_ids_reject_retired_claims(self):
        retired = ["6aa15346f5408191bdd9043fd26ff7aa","6aa1534deee481918280def1343adcfa","6abd70e42cb4819190b3b28916dc9dbb","6aa1535a51508191a610e6cdb90a2a4d","6aa1535f8cd48191b73e10f17843372a"]
        expected = set(["6ad7bbf79c488191bf893e6c08ea9b5f","6ad7bc0d48748191aa1dd0ce2f18d22a","6ad7bc1cc7b8819190665e5ed78610d4","6ad7bc3001b88191add05d9b4d5a4130","6ad7bc3ebea08191a16dc7c9f1f3303b"])
        self.assertEqual(CANONICAL_PRIMARY_IDS, expected)
        self.assertEqual(len(CANONICAL_AUTOMATION_IDS), 10)
        self.assertTrue(set(retired).isdisjoint(CANONICAL_AUTOMATION_IDS))
        for old_id in retired:
            with self.subTest(old_id=old_id):
                with self.assertRaisesRegex(UnsafeSnapshot, "UNKNOWN_CANONICAL_EXECUTOR"):
                    claim(owner=old_id)

    def test_claim_is_atomic_single_batch_with_five_requests(self):
        k = claim()
        self.assertEqual(k.metadata_id, METADATA_BASE + 1)
        self.assertEqual(k.executor_metadata_id, EXECUTOR_METADATA_BASE + (int(OWNER, 16) % 100_000_000))
        self.assertEqual(len(k.requests), 5)
        self.assertEqual(list(k.requests[0]), ["createDeveloperMetadata"])
        self.assertEqual(list(k.requests[1]), ["createDeveloperMetadata"])
        self.assertEqual(list(k.requests[2]), ["updateCells"])
        self.assertEqual(list(k.requests[3]), ["updateCells"])
        self.assertEqual(list(k.requests[4]), ["appendCells"])
        item = k.requests[0]["createDeveloperMetadata"]["developerMetadata"]
        self.assertEqual(item["metadataId"], METADATA_BASE + 1)
        self.assertEqual(item["metadataKey"], METADATA_KEY)
        self.assertEqual(item["location"]["dimensionRange"]["sheetId"], 2017583524)
        self.assertEqual(item["location"]["dimensionRange"]["startIndex"], 1)
        exec_item = k.requests[1]["createDeveloperMetadata"]["developerMetadata"]
        self.assertEqual(exec_item["metadataId"], k.executor_metadata_id)
        self.assertEqual(exec_item["metadataKey"], EXECUTOR_METADATA_KEY)
        self.assertEqual(exec_item["location"], {"spreadsheet": True})

    def test_claim_event_is_not_false_active_real(self):
        k = claim()
        data = k.requests[4]["appendCells"]["rows"][0]["values"]
        fields = [x["userEnteredValue"]["stringValue"] for x in data]
        self.assertEqual(len(fields), 12)
        self.assertEqual(fields[3], "LEASE_ACQUIRED")
        self.assertEqual(fields[8], "LEASE_RESERVED_MATERIAL_PENDING")
        self.assertNotIn("ACTIVE_REAL", fields[8])
        self.assertEqual(fields[11], k.token)

    def test_ttl_is_ten_minutes_utc(self):
        k = claim()
        self.assertEqual(
            datetime.fromisoformat(k.expires_at.replace("Z","+00:00")) -
            datetime.fromisoformat(k.acquired_at.replace("Z","+00:00")),
            timedelta(minutes=TTL_MINUTES),
        )

    def test_skip_lower_order_rejected(self):
        with self.assertRaisesRegex(UnsafeSnapshot, "OUT_OF_ORDER_CLAIM"):
            claim(chosen="P002")

    def test_claims_next_if_first_has_owner(self):
        c, r = fixture(2)
        r[1][2] = OWNER
        r[1][3] = "ACTIVE"
        k = claim(commercial=c, runtime=r, chosen="P002", owner=REVIEWER)
        self.assertEqual(k.metadata_id, METADATA_BASE + 2)
        self.assertEqual(k.row, 2)

    def test_released_lease_is_free_for_new_owner(self):
        c, r = fixture(2)
        r[1][3] = "RELEASED|OWNER=old"
        self.assertEqual(claim(commercial=c, runtime=r).task_id, "P001")

    def test_claim_refuses_owner_suspension_and_allows_next_unsuspended(self):
        c, runtime = fixture(3)
        runtime[1][7] = 'PENDIENTE SIN OWNER; SUSPENDIDO HASTA NUEVA ORDEN DEL PROPIETARIO'
        candidate = build_claim(
            c, runtime, task_id='P002', owner=OWNER, run_id='RUN_NEXT',
            head=HEAD, now=NOW, runtime_sheet_id=2017583524,
            events_sheet_id=958655496, expected_count=3,
        )
        self.assertEqual(candidate.task_id, 'P002')
        self.assertEqual(candidate.metadata_id, METADATA_BASE + 2)
        self.assertEqual(len(candidate.requests), 5)
        with self.assertRaisesRegex(UnsafeSnapshot, 'OUT_OF_ORDER_CLAIM'):
            build_claim(
                c, runtime, task_id='P001', owner=OWNER, run_id='RUN_FORBIDDEN',
                head=HEAD, now=NOW, runtime_sheet_id=2017583524,
                events_sheet_id=958655496, expected_count=3,
            )

    def test_naive_time_rejected(self):
        with self.assertRaisesRegex(UnsafeSnapshot, "UTC_AWARE_TIMESTAMP_REQUIRED"):
            claim(now=datetime(2026, 10, 20, 14, 30))

    def test_invalid_head_rejected(self):
        with self.assertRaisesRegex(UnsafeSnapshot, "INVALID_HEAD"):
            claim(head="not-a-commit")

    def test_pipe_in_runid_rejected(self):
        with self.assertRaisesRegex(UnsafeSnapshot, "INVALID_RUN_ID"):
            claim(run_id="a|b")

    def test_missing_dependency_fail_closed(self):
        c, r = fixture(2)
        r[1][9] = "NOT_FOUND"
        with self.assertRaisesRegex(UnsafeSnapshot, "UNKNOWN_DEPENDENCY"):
            claim(commercial=c, runtime=r)

    def test_claim_readback_exact_match_required(self):
        k = claim()
        good = claim_replies(k)
        self.assertTrue(verify_claim_response(k, good, claimed_runtime(k)))
        with self.assertRaisesRegex(UnsafeSnapshot, "CAS_RUNTIME_READBACK_MISMATCH"):
            verify_claim_response(k, good, [k.task_id, "EN_PROGRESO", "other", "ACTIVE|TOKEN=bad"])
        bad = {
            "replies": [
                {"createDeveloperMetadata": {"developerMetadata": {**confirmed_meta(k), "metadataId": 999}}},
                {"createDeveloperMetadata": {"developerMetadata": confirmed_exec_meta(k)}},
            ]
        }
        with self.assertRaisesRegex(UnsafeSnapshot, "CAS_TASK_METADATA_MISMATCH"):
            verify_claim_response(k, bad, claimed_runtime(k))

    def test_collision_does_not_count_as_success(self):
        k = claim()
        with self.assertRaisesRegex(UnsafeSnapshot, "NO_CAS_METADATA_CONFIRMATION"):
            verify_claim_response(k, {"error": {"code": 400}}, claimed_runtime(k))

    def test_release_fails_wrong_owner_and_token(self):
        k = claim()
        with self.assertRaisesRegex(UnsafeSnapshot, "RELEASE_OWNER_TOKEN_MISMATCH"):
            build_release(
                k, current_runtime_row=[k.task_id, "EN_PROGRESO", "someoneelse", "ACTIVE|TOKEN=other"],
                captured_metadata=confirmed_meta(k), captured_executor_metadata=confirmed_exec_meta(k),
                now=NOW, final_state="VALIDANDO",
                evidence="https://github.com/solqaryn/Solqaryn/pull/3691",
                result="REVIEW_FIRST_PENDING", details="handoff",
                runtime_sheet_id=2017583524, events_sheet_id=958655496,
            )

    def test_release_refuses_fake_listo(self):
        k = claim()
        with self.assertRaisesRegex(UnsafeSnapshot, "UNAUTHORIZED_FINAL_STATE"):
            build_release(
                k, current_runtime_row=claimed_runtime(k),
                captured_metadata=confirmed_meta(k), captured_executor_metadata=confirmed_exec_meta(k),
                now=NOW, final_state="LISTO",
                evidence="", result="FAKE", details="",
                runtime_sheet_id=2017583524, events_sheet_id=958655496,
            )

    def test_release_uses_exact_metadata_filter_and_handoff(self):
        k = claim()
        rel = build_release(
            k, current_runtime_row=claimed_runtime(k),
            captured_metadata=confirmed_meta(k), captured_executor_metadata=confirmed_exec_meta(k),
            now=NOW + timedelta(minutes=2),
            final_state="VALIDANDO", evidence="https://github.com/solqaryn/Solqaryn/pull/3691",
            result="REVIEW_FIRST_PENDING", details="material evidence saved",
            runtime_sheet_id=2017583524, events_sheet_id=958655496,
        )
        self.assertEqual(len(rel), 6)
        filt_task = rel[0]["deleteDeveloperMetadata"]["dataFilter"]["developerMetadataLookup"]
        self.assertEqual(filt_task["metadataId"], k.metadata_id)
        self.assertEqual(filt_task["metadataValue"], k.metadata_value)
        filt_exec = rel[1]["deleteDeveloperMetadata"]["dataFilter"]["developerMetadataLookup"]
        self.assertEqual(filt_exec["metadataId"], k.executor_metadata_id)
        self.assertEqual(filt_exec["metadataValue"], k.executor_metadata_value)
        self.assertEqual(
            rel[2]["updateCells"]["rows"][0]["values"][0]["userEnteredValue"]["stringValue"],
            "VALIDANDO",
        )

    def test_release_confirmed_only_on_exact_deletion(self):
        k = claim()
        good = {
            "replies": [
                {"deleteDeveloperMetadata": {"deletedDeveloperMetadata": [confirmed_meta(k)]}},
                {"deleteDeveloperMetadata": {"deletedDeveloperMetadata": [confirmed_exec_meta(k)]}},
            ]
        }
        row = [k.task_id, "VALIDANDO", "", "RELEASED|OWNER=" + k.owner + "|TOKEN=" + k.token]
        self.assertTrue(verify_release_response(k, good, row))
        with self.assertRaisesRegex(UnsafeSnapshot, "RELEASE_DID_NOT_DELETE_EXACT_TASK_LOCK"):
            verify_release_response(k, {"replies": [
                {"deleteDeveloperMetadata": {"deletedDeveloperMetadata": []}},
                {"deleteDeveloperMetadata": {"deletedDeveloperMetadata": [confirmed_exec_meta(k)]}},
            ]}, row)

    def test_parallel_ten_have_distinct_fixed_metadata_ids(self):
        c, r = fixture(32)
        issued = []
        canonical_list = sorted(CANONICAL_AUTOMATION_IDS)
        for i in range(1, 11):
            owner_i = canonical_list[i - 1]
            k = claim(commercial=c, runtime=r, chosen=f"P{i:03d}", owner=owner_i)
            issued.append(k.metadata_id)
            r[i][1] = "EN_PROGRESO"
            r[i][2] = k.owner
            r[i][3] = "ACTIVE|TOKEN=" + k.token
        self.assertEqual(issued, [METADATA_BASE + i for i in range(1, 11)])
        self.assertEqual(len(set(issued)), 10)


def review_fixture(n=2, author=OWNER, task_index=1):
    c, r = fixture(n)
    target = r[task_index]
    target[1] = "VALIDANDO"
    target[3] = f"RELEASED|OWNER={author}|TOKEN=old|HEAD={HEAD}"
    target[4] = HEAD
    target[5] = "https://github.com/solqaryn/Solqaryn/pull/3691"
    return c, r


def review_claim(*, c=None, r=None, reviewer=REVIEWER, task_id="P001"):
    if c is None or r is None:
        c, r = review_fixture()
    return build_review_claim(
        c, r, task_id=task_id, reviewer=reviewer, run_id="REVIEW_RUN_1",
        head=HEAD, now=NOW, runtime_sheet_id=2017583524,
        events_sheet_id=958655496, expected_count=len(c)-1,
    )


def review_row(lease):
    return ["P001", "VALIDANDO", lease.owner,
            "ACTIVE_REVIEW|OWNER="+lease.owner+
            "|TOKEN="+lease.token+"|RUN_ID="+lease.run_id+
            "|HEAD="+lease.head]


class IndependentReviewLeaseTests(unittest.TestCase):
    def test_independent_review_claim_is_atomic_and_not_listo(self):
        k = review_claim()
        self.assertEqual(k.metadata_id, METADATA_BASE+1)
        self.assertEqual(len(k.requests), 5)
        self.assertEqual(list(k.requests[0]), ["createDeveloperMetadata"])
        self.assertEqual(list(k.requests[1]), ["createDeveloperMetadata"])
        self.assertEqual(k.metadata_value.split("|")[-1], OWNER)
        self.assertEqual(k.requests[2]["updateCells"]["rows"][0]["values"][0]["userEnteredValue"]["stringValue"], "VALIDANDO")
        event = k.requests[4]["appendCells"]["rows"][0]["values"]
        self.assertEqual(event[3]["userEnteredValue"]["stringValue"], "REVIEW_LEASE_ACQUIRED")
        self.assertNotIn("LISTO", event[8]["userEnteredValue"]["stringValue"])
        self.assertEqual(
            k.requests[1]["createDeveloperMetadata"]["developerMetadata"]["location"],
            {"spreadsheet": True},
        )

    def test_review_must_have_original_handoff_with_evidence(self):
        c, r = review_fixture()
        r[1][5] = ""
        with self.assertRaisesRegex(UnsafeSnapshot, "REVIEW_MISSING_ORIGINAL_EVIDENCE"):
            review_claim(c=c, r=r)
        c, r = review_fixture()
        r[1][3] = "ACTIVE|OWNER=" + OWNER
        with self.assertRaisesRegex(UnsafeSnapshot, "REVIEW_MISSING_RELEASED_HANDOFF"):
            review_claim(c=c, r=r)

    def test_rejected_review_does_not_change_implementation_author(self):
        c, r = review_fixture()
        r[1][3] = (
            f"RELEASED|OWNER={REVIEWER}|ORIGINAL_AUTHOR={OWNER}|TOKEN=prior")
        # The original author must never become an independent reviewer
        # just because the previous reviewer released a failed review.
        with self.assertRaisesRegex(UnsafeSnapshot, "SELF_REVIEW_FORBIDDEN"):
            review_claim(c=c, r=r, reviewer=OWNER)
        other = sorted(CANONICAL_AUTOMATION_IDS - {OWNER, REVIEWER})[0]
        new_claim = review_claim(c=c, r=r, reviewer=other)
        self.assertEqual(new_claim.metadata_value.split("|")[-1], OWNER)

    def test_review_rejects_original_author_even_if_validating(self):
        with self.assertRaisesRegex(UnsafeSnapshot, "SELF_REVIEW_FORBIDDEN"):
            review_claim(reviewer=OWNER)

    def test_validando_review_precedes_lower_order_pending(self):
        c, r = review_fixture(n=2, task_index=2)
        accepted = review_claim(c=c, r=r, task_id="P002")
        self.assertEqual(accepted.task_id, "P002")

    def test_pending_claim_blocked_for_actor_with_independent_review(self):
        c, r = review_fixture(n=2, task_index=2)
        with self.assertRaisesRegex(UnsafeSnapshot, "VALIDANDO_PRIORITY_BEFORE_PENDING:P002"):
            claim(commercial=c, runtime=r, chosen="P001", owner=REVIEWER)
        # The original author cannot review their own delivery.
        other = claim(commercial=c, runtime=r, chosen="P001", owner=OWNER)
        self.assertEqual(other.task_id, "P001")

    def test_review_cannot_skip_other_lower_review(self):
        c, r = review_fixture(n=2)
        r[2] = r[1].copy()
        r[2][0] = "P002"
        with self.assertRaisesRegex(UnsafeSnapshot, "OUT_OF_ORDER_REVIEW_CLAIM"):
            review_claim(c=c, r=r, task_id="P002")

    def test_review_readback_checks_token_and_metadata_exactly(self):
        k = review_claim()
        good = {
            "replies": [
                {"createDeveloperMetadata": {"developerMetadata": confirmed_meta(k)}},
                {"createDeveloperMetadata": {"developerMetadata": confirmed_exec_meta(k)}},
            ]
        }
        self.assertTrue(verify_review_claim_response(k, good, review_row(k)))
        with self.assertRaisesRegex(UnsafeSnapshot, "REVIEW_LEASE_MISMATCH"):
            verify_review_claim_response(k, good, ["P001", "VALIDANDO", k.owner, "ACTIVE_REVIEW|TOKEN=wrong"])
        with self.assertRaisesRegex(UnsafeSnapshot, "NO_REVIEW_CAS_CONFIRMATION"):
            verify_review_claim_response(k, {}, review_row(k))

    def test_listo_requires_positive_causal_proofs(self):
        k = review_claim()
        opts = dict(
            current_runtime_row=review_row(k), captured_metadata=confirmed_meta(k),
            captured_executor_metadata=confirmed_exec_meta(k),
            now=NOW + timedelta(minutes=2), final_state="LISTO",
            review_evidence="https://github.com/solqaryn/Solqaryn/pull/3691",
            ci_evidence="https://github.com/solqaryn/Solqaryn/actions/runs/123",
            review_details="Checked exact-head with independent diff and negative tests",
            p0=0, p1=0,
            runtime_sheet_id=2017583524, events_sheet_id=958655496,
        )
        self.assertEqual(build_review_completion(k, **opts)[2]["updateCells"]["rows"][0]["values"][0]["userEnteredValue"]["stringValue"], "LISTO")
        with self.assertRaisesRegex(UnsafeSnapshot, "REVIEW_P0_P1_NOT_ZERO"):
            build_review_completion(k, **{**opts, "p0": 1})
        with self.assertRaisesRegex(UnsafeSnapshot, "REVIEW_CI_PROOF_REQUIRED"):
            build_review_completion(k, **{**opts, "ci_evidence": "not verified"})
        with self.assertRaisesRegex(UnsafeSnapshot, "REVIEW_PROOF_REQUIRED"):
            build_review_completion(k, **{**opts, "review_evidence": ""})
        with self.assertRaisesRegex(UnsafeSnapshot, "REVIEW_LEASE_MISMATCH"):
            build_review_completion(k, **{**opts, "current_runtime_row": ["P001", "VALIDANDO", "someoneelse", "ACTIVE"]})

    def test_release_preserves_original_author_and_original_evidence(self):
        k = review_claim()
        rel = build_review_completion(
            k, current_runtime_row=review_row(k), captured_metadata=confirmed_meta(k),
            captured_executor_metadata=confirmed_exec_meta(k),
            now=NOW + timedelta(minutes=2), final_state="LISTO",
            review_evidence="https://github.com/solqaryn/Solqaryn/pull/3691",
            ci_evidence="https://github.com/solqaryn/Solqaryn/actions/runs/123",
            review_details="Verified", p0=0, p1=0,
            runtime_sheet_id=2017583524, events_sheet_id=958655496,
        )
        self.assertEqual(list(rel[0]), ["deleteDeveloperMetadata"])
        self.assertEqual(list(rel[1]), ["deleteDeveloperMetadata"])
        row = rel[2]["updateCells"]["rows"][0]["values"]
        self.assertIn("ORIGINAL_AUTHOR=" + OWNER, row[2]["userEnteredValue"]["stringValue"])
        self.assertEqual(rel[2]["updateCells"]["range"]["endColumnIndex"], 4)
        self.assertEqual(rel[-1]["appendCells"]["rows"][0]["values"][3]["userEnteredValue"]["stringValue"], "REVIEW_FIRST_ACCEPTED_LISTO")

    def test_failed_review_releases_validando_not_fakes_listo(self):
        k = review_claim()
        rel = build_review_completion(
            k, current_runtime_row=review_row(k), captured_metadata=confirmed_meta(k),
            captured_executor_metadata=confirmed_exec_meta(k),
            now=NOW + timedelta(minutes=2), final_state="VALIDANDO",
            review_evidence="https://github.com/solqaryn/Solqaryn/pull/3691",
            ci_evidence="", review_details="Negative test failed; author must fix",
            p0=1, p1=0, runtime_sheet_id=2017583524, events_sheet_id=958655496,
        )
        self.assertEqual(rel[2]["updateCells"]["rows"][0]["values"][0]["userEnteredValue"]["stringValue"], "VALIDANDO")
        self.assertEqual(rel[-1]["appendCells"]["rows"][0]["values"][3]["userEnteredValue"]["stringValue"], "REVIEW_FIRST_NOT_APPROVED")

    def test_review_finish_requires_exact_locks_removed_and_readback(self):
        k = review_claim()
        ok = {
            "replies": [
                {"deleteDeveloperMetadata": {"deletedDeveloperMetadata": [confirmed_meta(k)]}},
                {"deleteDeveloperMetadata": {"deletedDeveloperMetadata": [confirmed_exec_meta(k)]}},
            ]
        }
        row = ["P001", "LISTO", "", "RELEASED|OWNER=" + k.owner + "|ORIGINAL_AUTHOR=" + OWNER + "|TOKEN=" + k.token]
        self.assertTrue(verify_review_completion_response(k, ok, row, "LISTO"))
        with self.assertRaisesRegex(UnsafeSnapshot, "REVIEW_RELEASE_WRONG_TASK_LOCK"):
            verify_review_completion_response(k, {"replies": [
                {"deleteDeveloperMetadata": {"deletedDeveloperMetadata": []}},
                {"deleteDeveloperMetadata": {"deletedDeveloperMetadata": [confirmed_exec_meta(k)]}},
            ]}, row, "LISTO")
        with self.assertRaisesRegex(UnsafeSnapshot, "REVIEW_COMPLETION_READBACK_MISMATCH"):
            verify_review_completion_response(k, ok, ["P001", "VALIDANDO", "other", "ACTIVE"], "LISTO")


class StaleLeaseRecoveryAndTakeoverTests(unittest.TestCase):
    def test_double_owner_wip_rejected(self):
        c, r = fixture(2)
        r[1][1] = "EN_PROGRESO"
        r[1][2] = OWNER
        r[1][3] = f"ACTIVE|OWNER={OWNER}|TOKEN=tok1234567890123456|RUN_ID=run1|EXPIRES=2026-10-20T15:00:00Z|HEAD={HEAD}"
        with self.assertRaisesRegex(UnsafeSnapshot, "EXECUTOR_WIP_LIMIT_EXCEEDED"):
            claim(commercial=c, runtime=r, chosen="P002", owner=OWNER)

    def test_unexpired_lease_recovery_rejected(self):
        c, r = fixture(2)
        r[1][1] = "EN_PROGRESO"
        r[1][2] = OWNER
        expires = (NOW + timedelta(minutes=5)).isoformat().replace("+00:00", "Z")
        r[1][3] = f"ACTIVE|OWNER={OWNER}|TOKEN=tok1234567890123456|RUN_ID=run1|START=2026-10-20T14:25:00Z|EXPIRES={expires}|HEAD={HEAD}"
        captured_task = {
            "metadataId": METADATA_BASE + 1,
            "metadataKey": METADATA_KEY,
            "metadataValue": f"P001|run1|{OWNER}|tok1234567890123456|2026-10-20T14:25:00Z|{expires}",
        }
        with self.assertRaisesRegex(UnsafeSnapshot, "LEASE_NOT_EXPIRED"):
            build_stale_lease_recovery(
                c, r, task_id="P001", recovered_by=REVIEWER, run_id="REC_RUN_1",
                head=HEAD, now=NOW, reason="TEST_RECOVERY",
                captured_metadata=captured_task,
                runtime_sheet_id=2017583524, events_sheet_id=958655496,
            )

    def test_expired_lease_fenced_recovery(self):
        c, r = fixture(2)
        r[1][1] = "EN_PROGRESO"
        r[1][2] = OWNER
        expired = (NOW - timedelta(minutes=5)).isoformat().replace("+00:00", "Z")
        r[1][3] = f"ACTIVE|OWNER={OWNER}|TOKEN=tok1234567890123456|RUN_ID=run1|START=2026-10-20T14:15:00Z|EXPIRES={expired}|HEAD={HEAD}"
        captured_task = {
            "metadataId": METADATA_BASE + 1,
            "metadataKey": METADATA_KEY,
            "metadataValue": f"P001|run1|{OWNER}|tok1234567890123456|2026-10-20T14:15:00Z|{expired}",
        }
        stale_exec_id = EXECUTOR_METADATA_BASE + (int(OWNER, 16) % 100_000_000)
        captured_exec = {
            "metadataId": stale_exec_id,
            "metadataKey": EXECUTOR_METADATA_KEY,
            "metadataValue": f"{OWNER}|P001|tok1234567890123456|2026-10-20T14:15:00Z|{expired}",
        }
        rec = build_stale_lease_recovery(
            c, r, task_id="P001", recovered_by=REVIEWER, run_id="REC_RUN_1",
            head=HEAD, now=NOW, reason="CRASHED_AGENT_TTL_EXPIRED",
            captured_metadata=captured_task, captured_executor_metadata=captured_exec,
            runtime_sheet_id=2017583524, events_sheet_id=958655496,
        )
        self.assertEqual(rec.task_id, "P001")
        self.assertEqual(rec.stale_owner, OWNER)
        self.assertEqual(rec.stale_token, "tok1234567890123456")
        self.assertEqual(rec.target_state, "PENDIENTE")
        self.assertEqual(len(rec.requests), 6)
        self.assertEqual(list(rec.requests[0]), ["deleteDeveloperMetadata"])
        self.assertEqual(list(rec.requests[1]), ["deleteDeveloperMetadata"])
        readback_row = [
            "P001", "PENDIENTE", "",
            f"RECOVERED|STALE_OWNER={OWNER}|STALE_TOKEN=tok1234567890123456|FENCING_TOKEN={rec.fencing_token}|RECOVERED_BY={REVIEWER}|RECOVERED_AT={rec.requests[2]['updateCells']['rows'][0]['values'][2]['userEnteredValue']['stringValue'].split('RECOVERED_AT=')[1].split('|')[0]}|HEAD={HEAD}",
        ]
        api_rep = {
            "replies": [
                {"deleteDeveloperMetadata": {"deletedDeveloperMetadata": [{"metadataId": rec.metadata_id, "metadataValue": captured_task["metadataValue"]}]}},
                {"deleteDeveloperMetadata": {"deletedDeveloperMetadata": [{"metadataId": rec.stale_executor_metadata_id, "metadataValue": captured_exec["metadataValue"]}]}},
            ]
        }
        self.assertTrue(verify_stale_lease_recovery_response(rec, api_rep, readback_row))

    def test_crashed_owner_locked_out_by_fencing(self):
        c, r = fixture(2)
        k = claim(commercial=c, runtime=r, chosen="P001", owner=OWNER, now=NOW)
        r[1][1] = "EN_PROGRESO"
        r[1][2] = k.owner
        r[1][3] = f"ACTIVE|OWNER={k.owner}|TOKEN={k.token}|RUN_ID={k.run_id}|START={k.acquired_at}|EXPIRES={k.expires_at}|HEAD={k.head}"

        captured_task = confirmed_meta(k)
        captured_exec = confirmed_exec_meta(k)
        later = NOW + timedelta(minutes=15)
        rec = build_stale_lease_recovery(
            c, r, task_id="P001", recovered_by=REVIEWER, run_id="REC_RUN_2",
            head=HEAD, now=later, reason="RUNNER_TIMEOUT",
            captured_metadata=captured_task, captured_executor_metadata=captured_exec,
            runtime_sheet_id=2017583524, events_sheet_id=958655496,
        )
        fenced_row = [
            "P001", "PENDIENTE", "",
            f"RECOVERED|STALE_OWNER={k.owner}|STALE_TOKEN={k.token}|FENCING_TOKEN={rec.fencing_token}|RECOVERED_BY={REVIEWER}|HEAD={HEAD}",
        ]
        with self.assertRaisesRegex(UnsafeSnapshot, "RELEASE_OWNER_TOKEN_MISMATCH"):
            build_release(
                k, current_runtime_row=fenced_row,
                captured_metadata=captured_task, captured_executor_metadata=captured_exec,
                now=later,
                final_state="VALIDANDO", evidence="https://github.com/solqaryn/Solqaryn/pull/3691",
                result="LATE_ATTEMPT", details="Late attempt after crash",
                runtime_sheet_id=2017583524, events_sheet_id=958655496,
            )

    def test_idempotent_recovery_cannot_recover_completed_task(self):
        c, r = fixture(2)
        r[1][1] = "LISTO"
        r[1][2] = ""
        r[1][3] = "RELEASED|OWNER=old"
        with self.assertRaisesRegex(UnsafeSnapshot, "CANNOT_RECOVER_COMPLETED_TASK"):
            build_stale_lease_recovery(
                c, r, task_id="P001", recovered_by=REVIEWER, run_id="REC_RUN_3",
                head=HEAD, now=NOW, reason="TEST_RECOVERY",
                captured_metadata={"metadataId": METADATA_BASE + 1, "metadataKey": METADATA_KEY, "metadataValue": "val"},
                runtime_sheet_id=2017583524, events_sheet_id=958655496,
            )

    def test_takeover_claim_success_and_readback(self):
        c, r = fixture(2)
        r[1][1] = "EN_PROGRESO"
        r[1][2] = OWNER
        expired = (NOW - timedelta(minutes=5)).isoformat().replace("+00:00", "Z")
        r[1][3] = f"ACTIVE|OWNER={OWNER}|TOKEN=tok_old1234567890|RUN_ID=run_old|START=2026-10-20T14:15:00Z|EXPIRES={expired}|HEAD={HEAD}"
        stale_meta_val = f"P001|run_old|{OWNER}|tok_old1234567890|2026-10-20T14:15:00Z|{expired}"
        captured_task = {"metadataId": METADATA_BASE + 1, "metadataKey": METADATA_KEY, "metadataValue": stale_meta_val}
        stale_exec_id = EXECUTOR_METADATA_BASE + (int(OWNER, 16) % 100_000_000)
        captured_exec = {"metadataId": stale_exec_id, "metadataKey": EXECUTOR_METADATA_KEY, "metadataValue": f"{OWNER}|P001|tok_old1234567890"}
        tk = build_takeover_claim(
            c, r, task_id="P001", new_owner=REVIEWER, run_id="TAKEOVER_1",
            head=HEAD, now=NOW, reason="PRIMARY_STALLED",
            captured_metadata=captured_task, captured_executor_metadata=captured_exec,
            runtime_sheet_id=2017583524, events_sheet_id=958655496,
        )
        self.assertEqual(tk.task_id, "P001")
        self.assertEqual(tk.owner, REVIEWER)
        executor_creates = [
            request["createDeveloperMetadata"]["developerMetadata"]
            for request in tk.requests
            if "createDeveloperMetadata" in request
            and request["createDeveloperMetadata"]["developerMetadata"]["metadataKey"] == EXECUTOR_METADATA_KEY
        ]
        self.assertEqual(len(executor_creates), 1)
        self.assertEqual(executor_creates[0]["location"], {"spreadsheet": True})
        api_rep = {
            "replies": [
                {"deleteDeveloperMetadata": {"deletedDeveloperMetadata": [{"metadataId": tk.metadata_id, "metadataValue": stale_meta_val}]}},
                {"deleteDeveloperMetadata": {"deletedDeveloperMetadata": [{"metadataId": stale_exec_id, "metadataValue": captured_exec["metadataValue"]}]}},
                {"createDeveloperMetadata": {"developerMetadata": confirmed_meta(tk)}},
                {"createDeveloperMetadata": {"developerMetadata": confirmed_exec_meta(tk)}},
            ]
        }
        tk_runtime = [tk.task_id, "EN_PROGRESO", tk.owner, f"ACTIVE|OWNER={tk.owner}|TOKEN={tk.token}|RUN_ID={tk.run_id}|HEAD={tk.head}"]
        self.assertTrue(verify_takeover_claim_response(tk, api_rep, tk_runtime, has_stale_executor=True))

    def test_takeover_claim_rejects_wip_exceeded(self):
        c, r = fixture(2)
        r[1][1] = "EN_PROGRESO"
        r[1][2] = OWNER
        expired = (NOW - timedelta(minutes=5)).isoformat().replace("+00:00", "Z")
        r[1][3] = f"ACTIVE|OWNER={OWNER}|TOKEN=tok_old1234567890|EXPIRES={expired}"
        # Make REVIEWER already own P002
        r[2][1] = "EN_PROGRESO"
        r[2][2] = REVIEWER
        r[2][3] = f"ACTIVE|OWNER={REVIEWER}|TOKEN=tok_rev1234567890"
        captured_task = {"metadataId": METADATA_BASE + 1, "metadataKey": METADATA_KEY, "metadataValue": f"P001|run|{OWNER}|tok_old1234567890"}
        with self.assertRaisesRegex(UnsafeSnapshot, "EXECUTOR_WIP_LIMIT_EXCEEDED"):
            build_takeover_claim(
                c, r, task_id="P001", new_owner=REVIEWER, run_id="TAKEOVER_2",
                head=HEAD, now=NOW, reason="TAKEOVER_ATTEMPT",
                captured_metadata=captured_task,
                runtime_sheet_id=2017583524, events_sheet_id=958655496,
            )

    def test_dual_exclusivity_creates_two_metadata_locks(self):
        k = claim()
        self.assertEqual(len(k.requests), 5)
        self.assertEqual(k.requests[0]["createDeveloperMetadata"]["developerMetadata"]["metadataKey"], METADATA_KEY)
        self.assertEqual(k.requests[1]["createDeveloperMetadata"]["developerMetadata"]["metadataKey"], EXECUTOR_METADATA_KEY)
        self.assertEqual(k.executor_metadata_id, EXECUTOR_METADATA_BASE + (int(OWNER, 16) % 100_000_000))


class ConcurrencyAndFaultToleranceTests(unittest.TestCase):
    """Mandatory negative, concurrency, and fault tolerance tests from Review 5588458124."""

    def test_1_concurrency_race_lease_renewed_between_read_and_recovery(self):
        c, r = fixture(2)
        r[1][1] = "EN_PROGRESO"
        r[1][2] = OWNER
        expired = (NOW - timedelta(minutes=5)).isoformat().replace("+00:00", "Z")
        r[1][3] = f"ACTIVE|OWNER={OWNER}|TOKEN=tok_old1234567890|RUN_ID=run1|START=2026-10-20T14:15:00Z|EXPIRES={expired}|HEAD={HEAD}"
        captured_task = {
            "metadataId": METADATA_BASE + 1,
            "metadataKey": METADATA_KEY,
            "metadataValue": f"P001|run1|{OWNER}|tok_old1234567890|2026-10-20T14:15:00Z|{expired}",
        }
        rec = build_stale_lease_recovery(
            c, r, task_id="P001", recovered_by=REVIEWER, run_id="REC_RACE_1",
            head=HEAD, now=NOW, reason="TEST_RACE",
            captured_metadata=captured_task,
            runtime_sheet_id=2017583524, events_sheet_id=958655496,
        )
        # If the lease was renewed in Google Sheets, the lookup metadataValue won't match, deleting 0 rows
        race_response = {
            "replies": [
                {"deleteDeveloperMetadata": {"deletedDeveloperMetadata": []}},  # 0 deleted!
                {"deleteDeveloperMetadata": {"deletedDeveloperMetadata": []}},
            ]
        }
        readback_row = ["P001", "PENDIENTE", "", f"RECOVERED|FENCING_TOKEN={rec.fencing_token}"]
        with self.assertRaisesRegex(UnsafeSnapshot, "RECOVERY_STALE_LOCK_NOT_MATCHED"):
            verify_stale_lease_recovery_response(rec, race_response, readback_row)

    def test_2_concurrency_two_executors_attempting_same_takeover(self):
        c, r = fixture(2)
        r[1][1] = "EN_PROGRESO"
        r[1][2] = OWNER
        expired = (NOW - timedelta(minutes=5)).isoformat().replace("+00:00", "Z")
        r[1][3] = f"ACTIVE|OWNER={OWNER}|TOKEN=tok_old|RUN_ID=run1|START=2026-10-20T14:15:00Z|EXPIRES={expired}|HEAD={HEAD}"
        captured_task = {
            "metadataId": METADATA_BASE + 1,
            "metadataKey": METADATA_KEY,
            "metadataValue": f"P001|run1|{OWNER}|tok_old|2026-10-20T14:15:00Z|{expired}",
        }
        # Executor 2 attempts takeover after Executor 1 already deleted the stale metadata
        exec2 = "6abd6241c3e48191a8856ed7b0f42c5c"
        tk2 = build_takeover_claim(
            c, r, task_id="P001", new_owner=exec2, run_id="TAKEOVER_RACE_2",
            head=HEAD, now=NOW, reason="SECOND_TAKEOVER_ATTEMPT",
            captured_metadata=captured_task,
            runtime_sheet_id=2017583524, events_sheet_id=958655496,
        )
        api_rep_stale_already_deleted = {
            "replies": [
                {"deleteDeveloperMetadata": {"deletedDeveloperMetadata": []}},
                {"deleteDeveloperMetadata": {"deletedDeveloperMetadata": []}},
            ]
        }
        with self.assertRaisesRegex(UnsafeSnapshot, "TAKEOVER_STALE_TASK_DELETE_MISMATCH"):
            verify_takeover_claim_response(tk2, api_rep_stale_already_deleted, ["P001", "EN_PROGRESO", exec2, "ACTIVE"])

    def test_3_concurrency_two_executors_claiming_same_task(self):
        c, r = fixture(2)
        k1 = claim(commercial=c, runtime=r, chosen="P001", owner=OWNER)
        exec2 = "6abd6241c3e48191a8856ed7b0f42c5c"
        k2 = claim(commercial=c, runtime=r, chosen="P001", owner=exec2)
        # Both share the same fixed metadataId
        self.assertEqual(k1.metadata_id, k2.metadata_id)
        # Google Sheets rejects duplicate metadataId with 400 error
        with self.assertRaisesRegex(UnsafeSnapshot, "NO_CAS_METADATA_CONFIRMATION"):
            verify_claim_response(k2, {"error": {"code": 400, "message": "DeveloperMetadata ID already exists"}}, claimed_runtime(k2))

    def test_4_concurrency_one_executor_attempting_two_tasks(self):
        c, r = fixture(3)
        # Claim first task
        k1 = claim(commercial=c, runtime=r, chosen="P001", owner=OWNER)
        r[1][1] = "EN_PROGRESO"
        r[1][2] = OWNER
        r[1][3] = f"ACTIVE|OWNER={OWNER}|TOKEN={k1.token}"
        # Second task claim by same owner must be blocked by WIP check
        with self.assertRaisesRegex(UnsafeSnapshot, "EXECUTOR_WIP_LIMIT_EXCEEDED"):
            claim(commercial=c, runtime=r, chosen="P002", owner=OWNER)

    def test_5_fault_lease_expiration_checks(self):
        c, r = fixture(2)
        r[1][1] = "EN_PROGRESO"
        r[1][2] = OWNER
        captured = {"metadataId": METADATA_BASE + 1, "metadataKey": METADATA_KEY, "metadataValue": f"P001|run|{OWNER}|tok"}

        # 5a: Missing EXPIRES field
        r[1][3] = f"ACTIVE|OWNER={OWNER}|TOKEN=tok1234567890123456|RUN_ID=run1"
        with self.assertRaisesRegex(UnsafeSnapshot, "MISSING_LEASE_EXPIRATION"):
            build_stale_lease_recovery(c, r, task_id="P001", recovered_by=REVIEWER, run_id="REC", head=HEAD, now=NOW, reason="TEST",
                                       captured_metadata=captured, runtime_sheet_id=1, events_sheet_id=2)

        # 5b: Malformed EXPIRES field
        r[1][3] = f"ACTIVE|OWNER={OWNER}|TOKEN=tok1234567890123456|RUN_ID=run1|EXPIRES=not-a-date"
        with self.assertRaisesRegex(UnsafeSnapshot, "MALFORMED_LEASE_EXPIRATION"):
            build_stale_lease_recovery(c, r, task_id="P001", recovered_by=REVIEWER, run_id="REC", head=HEAD, now=NOW, reason="TEST",
                                       captured_metadata=captured, runtime_sheet_id=1, events_sheet_id=2)

        # 5c: Unexpired lease
        unexpired = (NOW + timedelta(minutes=5)).isoformat().replace("+00:00", "Z")
        r[1][3] = f"ACTIVE|OWNER={OWNER}|TOKEN=tok1234567890123456|RUN_ID=run1|EXPIRES={unexpired}"
        with self.assertRaisesRegex(UnsafeSnapshot, "LEASE_NOT_EXPIRED"):
            build_stale_lease_recovery(c, r, task_id="P001", recovered_by=REVIEWER, run_id="REC", head=HEAD, now=NOW, reason="TEST",
                                       captured_metadata=captured, runtime_sheet_id=1, events_sheet_id=2)

    def test_6_fault_metadata_missing_or_belonging_to_other_owner(self):
        c, r = fixture(2)
        r[1][1] = "EN_PROGRESO"
        r[1][2] = OWNER
        expired = (NOW - timedelta(minutes=5)).isoformat().replace("+00:00", "Z")
        r[1][3] = f"ACTIVE|OWNER={OWNER}|TOKEN=tok1234567890123456|RUN_ID=run1|EXPIRES={expired}"

        # 6a: Captured metadata missing
        with self.assertRaisesRegex(UnsafeSnapshot, "CAPTURED_METADATA_REQUIRED"):
            build_stale_lease_recovery(c, r, task_id="P001", recovered_by=REVIEWER, run_id="REC", head=HEAD, now=NOW, reason="TEST",
                                       captured_metadata=None, runtime_sheet_id=1, events_sheet_id=2)

        # 6b: Metadata belongs to other task
        wrong_meta = {"metadataId": METADATA_BASE + 999, "metadataKey": METADATA_KEY, "metadataValue": "val"}
        with self.assertRaisesRegex(UnsafeSnapshot, "STALE_METADATA_ID_MISMATCH"):
            build_stale_lease_recovery(c, r, task_id="P001", recovered_by=REVIEWER, run_id="REC", head=HEAD, now=NOW, reason="TEST",
                                       captured_metadata=wrong_meta, runtime_sheet_id=1, events_sheet_id=2)

        # 6c: Metadata value does not match runtime row token/owner
        mismatched_val_meta = {"metadataId": METADATA_BASE + 1, "metadataKey": METADATA_KEY, "metadataValue": "P001|other_owner|wrong_token"}
        with self.assertRaisesRegex(UnsafeSnapshot, "METADATA_VALUE_DOES_NOT_MATCH_RUNTIME_ROW"):
            build_stale_lease_recovery(c, r, task_id="P001", recovered_by=REVIEWER, run_id="REC", head=HEAD, now=NOW, reason="TEST",
                                       captured_metadata=mismatched_val_meta, runtime_sheet_id=1, events_sheet_id=2)

    def test_7_fault_old_execution_writing_after_recovery(self):
        c, r = fixture(2)
        k = claim(commercial=c, runtime=r, chosen="P001", owner=OWNER, now=NOW)
        captured = confirmed_meta(k)
        # Fenced recovery occurred
        fenced_row = ["P001", "PENDIENTE", "", f"RECOVERED|STALE_OWNER={k.owner}|STALE_TOKEN={k.token}|FENCING_TOKEN=abc1234567890123"]
        # Old owner attempts release
        with self.assertRaisesRegex(UnsafeSnapshot, "RELEASE_OWNER_TOKEN_MISMATCH"):
            build_release(
                k, current_runtime_row=fenced_row, captured_metadata=captured, now=NOW + timedelta(minutes=20),
                final_state="VALIDANDO", evidence="https://github.com/solqaryn/Solqaryn/pull/3691",
                result="LATE", details="late", runtime_sheet_id=1, events_sheet_id=2,
            )

    def test_8_fault_partial_failure_or_connection_loss(self):
        k = claim()
        # Missing replies array
        with self.assertRaisesRegex(UnsafeSnapshot, "NO_CAS_METADATA_CONFIRMATION"):
            verify_claim_response(k, {}, claimed_runtime(k))
        # Empty replies
        with self.assertRaisesRegex(UnsafeSnapshot, "NO_CAS_METADATA_CONFIRMATION"):
            verify_claim_response(k, {"replies": []}, claimed_runtime(k))

    def test_9_fault_repeated_retry_idempotency(self):
        c, r = fixture(2)
        # First claim succeeds
        k = claim(commercial=c, runtime=r, chosen="P001", owner=OWNER)
        r[1][1] = "EN_PROGRESO"
        r[1][2] = OWNER
        r[1][3] = f"ACTIVE|OWNER={OWNER}|TOKEN={k.token}"
        # Same executor retrying same task claim must fail closed
        with self.assertRaisesRegex(UnsafeSnapshot, "WIP_LIMIT_EXCEEDED"):
            claim(commercial=c, runtime=r, chosen="P001", owner=OWNER)

    def test_10_fault_recovering_validando_review_cannot_downgrade(self):
        c, r = review_fixture()
        expired = (NOW - timedelta(minutes=5)).isoformat().replace("+00:00", "Z")
        r[1][2] = REVIEWER
        r[1][3] = f"ACTIVE_REVIEW|OWNER={REVIEWER}|ORIGINAL_AUTHOR={OWNER}|TOKEN=tok_rev123456789|RUN_ID=run1|START=2026-10-20T14:15:00Z|EXPIRES={expired}|HEAD={HEAD}"
        captured = {
            "metadataId": METADATA_BASE + 1, "metadataKey": METADATA_KEY,
            "metadataValue": f"P001|run1|{REVIEWER}|tok_rev123456789|2026-10-20T14:15:00Z|{expired}|{OWNER}",
        }
        # Attempting to downgrade VALIDANDO to PENDIENTE is forbidden
        other_sup = next(iter(CANONICAL_SUPERVISOR_IDS - {REVIEWER}))
        with self.assertRaisesRegex(UnsafeSnapshot, "CANNOT_DOWNGRADE_VALIDANDO_TO_PENDIENTE"):
            build_stale_lease_recovery(
                c, r, task_id="P001", recovered_by=other_sup,
                run_id="REC", head=HEAD, now=NOW, reason="TEST",
                captured_metadata=captured, target_state="PENDIENTE",
                runtime_sheet_id=1, events_sheet_id=2,
            )

    def test_11_fault_reviewer_cannot_approve_own_work(self):
        c, r = review_fixture(author=OWNER)
        # Author attempting to claim review of own work
        with self.assertRaisesRegex(UnsafeSnapshot, "SELF_REVIEW_FORBIDDEN"):
            review_claim(c=c, r=r, reviewer=OWNER, task_id="P001")

    def test_12_fault_head_and_dependencies_mismatch(self):
        # Invalid HEAD sha format
        with self.assertRaisesRegex(UnsafeSnapshot, "INVALID_HEAD"):
            claim(head="invalid_sha_123")
        # Takeover with unsatisfied dependencies
        c, r = fixture(2)
        r[1][1] = "EN_PROGRESO"
        r[1][2] = OWNER
        r[1][10] = "NO"  # DEPENDENCIAS_OK != SI
        expired = (NOW - timedelta(minutes=5)).isoformat().replace("+00:00", "Z")
        r[1][3] = f"ACTIVE|OWNER={OWNER}|TOKEN=tok1234567890123456|EXPIRES={expired}"
        captured = {"metadataId": METADATA_BASE + 1, "metadataKey": METADATA_KEY, "metadataValue": "val"}
        with self.assertRaisesRegex(UnsafeSnapshot, "TAKEOVER_DEPENDENCIES_NOT_SATISFIED"):
            build_takeover_claim(
                c, r, task_id="P001", new_owner=REVIEWER, run_id="TK", head=HEAD, now=NOW, reason="TEST",
                captured_metadata=captured, runtime_sheet_id=1, events_sheet_id=2,
            )

    def test_13_fault_lock_release_error(self):
        k = claim()
        # Deletion returned 0 items deleted
        bad_rep = {
            "replies": [
                {"deleteDeveloperMetadata": {"deletedDeveloperMetadata": []}},
                {"deleteDeveloperMetadata": {"deletedDeveloperMetadata": [confirmed_exec_meta(k)]}},
            ]
        }
        with self.assertRaisesRegex(UnsafeSnapshot, "RELEASE_DID_NOT_DELETE_EXACT_TASK_LOCK"):
            verify_release_response(k, bad_rep, [k.task_id, "VALIDANDO", "", "RELEASED|TOKEN=" + k.token])

    def test_14_fault_recovery_clean_subsequent_claim(self):
        c, r = fixture(2)
        r[1][1] = "EN_PROGRESO"
        r[1][2] = OWNER
        expired = (NOW - timedelta(minutes=5)).isoformat().replace("+00:00", "Z")
        r[1][3] = f"ACTIVE|OWNER={OWNER}|TOKEN=tok1234567890123456|RUN_ID=run1|EXPIRES={expired}"
        captured = {
            "metadataId": METADATA_BASE + 1, "metadataKey": METADATA_KEY,
            "metadataValue": f"P001|run1|{OWNER}|tok1234567890123456|start|{expired}",
        }
        rec = build_stale_lease_recovery(
            c, r, task_id="P001", recovered_by=REVIEWER, run_id="REC", head=HEAD, now=NOW, reason="TEST",
            captured_metadata=captured, runtime_sheet_id=1, events_sheet_id=2,
        )
        # Apply fenced recovery to runtime row
        r[1][1] = "PENDIENTE"
        r[1][2] = ""
        r[1][3] = f"RECOVERED|STALE_OWNER={OWNER}|STALE_TOKEN=tok1234567890123456|FENCING_TOKEN={rec.fencing_token}|RECOVERED_BY={REVIEWER}|HEAD={HEAD}"
        # A new runner (or any canonical runner) can now claim P001 cleanly
        runner3 = "6ad7bc0d48748191aa1dd0ce2f18d22a"
        k3 = claim(commercial=c, runtime=r, chosen="P001", owner=runner3)
        self.assertEqual(k3.task_id, "P001")
        self.assertEqual(k3.owner, runner3)



class ExactReleaseDoubleLockRegressionTests(unittest.TestCase):
    def test_release_requires_executor_capture(self):
        k = claim()
        with self.assertRaisesRegex(UnsafeSnapshot, "RELEASE_EXECUTOR_METADATA_REQUIRED"):
            build_release(
                k, current_runtime_row=claimed_runtime(k),
                captured_metadata=confirmed_meta(k), now=NOW + timedelta(minutes=2),
                final_state="VALIDANDO",
                evidence="https://github.com/solqaryn/Solqaryn/pull/3691",
                result="REVIEW_FIRST_PENDING", details="Material handed off",
                runtime_sheet_id=2017583524, events_sheet_id=958655496,
            )

    def test_release_rejects_exact_id_with_other_task_value(self):
        k = claim()
        ok = {
            "replies": [
                {"deleteDeveloperMetadata": {"deletedDeveloperMetadata": [
                    {**confirmed_meta(k), "metadataValue": "OTHER-TOKEN"}
                ]}},
                {"deleteDeveloperMetadata": {"deletedDeveloperMetadata": [confirmed_exec_meta(k)]}},
            ]
        }
        with self.assertRaisesRegex(UnsafeSnapshot, "RELEASE_DID_NOT_DELETE_EXACT_TASK_LOCK"):
            verify_release_response(
                k, ok, [k.task_id, "VALIDANDO", "", "RELEASED|TOKEN=" + k.token]
            )

    def test_release_rejects_exact_executor_id_but_other_value(self):
        k = claim()
        ok = {
            "replies": [
                {"deleteDeveloperMetadata": {"deletedDeveloperMetadata": [confirmed_meta(k)]}},
                {"deleteDeveloperMetadata": {"deletedDeveloperMetadata": [
                    {**confirmed_exec_meta(k), "metadataValue": "OTHER-TOKEN"}
                ]}},
            ]
        }
        with self.assertRaisesRegex(UnsafeSnapshot, "RELEASE_DID_NOT_DELETE_EXACT_EXECUTOR_LOCK"):
            verify_release_response(
                k, ok, [k.task_id, "VALIDANDO", "", "RELEASED|TOKEN=" + k.token]
            )

    def test_review_requires_executor_capture(self):
        k = review_claim()
        with self.assertRaisesRegex(UnsafeSnapshot, "REVIEW_RELEASE_EXECUTOR_METADATA_REQUIRED"):
            build_review_completion(
                k, current_runtime_row=review_row(k),
                captured_metadata=confirmed_meta(k),
                now=NOW + timedelta(minutes=2), final_state="LISTO",
                review_evidence="https://github.com/solqaryn/Solqaryn/pull/3691",
                ci_evidence="https://github.com/solqaryn/Solqaryn/actions/runs/123",
                review_details="Reviewed independently", p0=0, p1=0,
                runtime_sheet_id=2017583524, events_sheet_id=958655496,
            )

    def test_review_readback_rejects_reused_id_with_other_value(self):
        k = review_claim()
        resp = {
            "replies": [
                {"deleteDeveloperMetadata": {"deletedDeveloperMetadata": [confirmed_meta(k)]}},
                {"deleteDeveloperMetadata": {"deletedDeveloperMetadata": [
                    {**confirmed_exec_meta(k), "metadataValue": "RENEWED-BY-OTHER"}
                ]}},
            ]
        }
        row = [k.task_id, "LISTO", "", "RELEASED|TOKEN=" + k.token]
        with self.assertRaisesRegex(UnsafeSnapshot, "REVIEW_RELEASE_WRONG_EXECUTOR_LOCK"):
            verify_review_completion_response(k, resp, row, "LISTO")


if __name__ == "__main__":
    unittest.main()
