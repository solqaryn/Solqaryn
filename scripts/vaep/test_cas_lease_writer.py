"""Contract tests: atomic claim, no skips, no false ACTIVE_REAL, safe release."""
import unittest
from datetime import datetime, timedelta, timezone

from cas_lease_writer import (
    METADATA_BASE, METADATA_KEY, TTL_MINUTES, UnsafeSnapshot,
    EXECUTOR_METADATA_BASE, EXECUTOR_METADATA_KEY,
    build_claim, build_release, verify_claim_response, verify_release_response,
    build_review_claim, verify_review_claim_response,
    build_review_completion, verify_review_completion_response,
    build_stale_lease_recovery, verify_stale_lease_recovery_response,
    build_takeover_claim, verify_takeover_claim_response,
)

COMMERCIAL = ["ID","PROGRAMA_PRIORIDAD","MICROTAREA","CRITERIO_EXIGIDO","ORDEN","PLAN_COMERCIAL_DOC_ID","VERSION_FUENTE","UNIDAD_ORIGEN_LANE"]
RUNTIME = ["ID","ESTADO","OWNER_AUTOMATION_ID","LEASE_STATUS","HEAD","EVIDENCIA","BLOQUEO","NEXT_ACTION","UPDATED_AT","DEPENDENCIAS_ACTUALES","DEPENDENCIAS_OK","PRIORIDAD_ACTUAL","P0","P1","ROLLBACK","RESULTADO","NOTA_RUNTIME","RESERVED"]
OWNER = "6aa15346f5408191bdd9043fd26ff7aa"
HEAD = "ba4edfffcb2473c42d731335cf25dc33121a72ad"
NOW = datetime(2026, 10, 20, 14, 30, tzinfo=timezone.utc)


def fixture(n=2):
    comm = [COMMERCIAL.copy()]
    run = [RUNTIME.copy()]
    for i in range(1, n+1):
        task_id = f"P{i:03d}"
        comm.append([task_id, "01", task_id, "", str(i), "", "", "ANY"])
        run.append([task_id, "PENDIENTE", "", "", "", "", "", "", "", "", "SI", "R01", "", "", "", "", "", ""])
    return comm, run


def claim(*, n=2, commercial=None, runtime=None, chosen="P001", now=NOW, head=HEAD, run_id="RUN_123", owner=OWNER, enforce_executor_metadata=False):
    c, r = fixture(n)
    return build_claim(
        commercial or c, runtime or r,
        task_id=chosen, owner=owner, run_id=run_id, head=head, now=now,
        runtime_sheet_id=2017583524, events_sheet_id=958655496,
        expected_count=len((commercial or c))-1,
        enforce_executor_metadata=enforce_executor_metadata,
    )


def confirmed_meta(k):
    return {"metadataId": k.metadata_id,
            "metadataKey": METADATA_KEY, "metadataValue": k.metadata_value}


def claimed_runtime(k):
    return [k.task_id, "EN_PROGRESO", k.owner, 
            f"ACTIVE|OWNER={k.owner}|TOKEN={k.token}|RUN_ID={k.run_id}|HEAD={k.head}"]


class LeaseWriterTests(unittest.TestCase):
    def test_claim_is_atomic_single_batch_with_four_requests(self):
        k = claim()
        self.assertEqual(k.metadata_id, METADATA_BASE+1)
        self.assertEqual(len(k.requests), 4)
        self.assertEqual(list(k.requests[0]), ["createDeveloperMetadata"])
        self.assertEqual(list(k.requests[1]), ["updateCells"])
        self.assertEqual(list(k.requests[2]), ["updateCells"])
        self.assertEqual(list(k.requests[3]), ["appendCells"])
        item = k.requests[0]["createDeveloperMetadata"]["developerMetadata"]
        self.assertEqual(item["metadataId"], METADATA_BASE+1)
        self.assertEqual(item["metadataKey"], METADATA_KEY)
        self.assertEqual(item["location"]["dimensionRange"]["sheetId"], 2017583524)
        self.assertEqual(item["location"]["dimensionRange"]["startIndex"], 1)

    def test_claim_event_is_not_false_active_real(self):
        k = claim()
        data = k.requests[3]["appendCells"]["rows"][0]["values"]
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
        c,r = fixture(2)
        r[1][2] = OWNER
        r[1][3] = "ACTIVE"
        k = claim(commercial=c, runtime=r, chosen="P002")
        self.assertEqual(k.metadata_id, METADATA_BASE+2)
        self.assertEqual(k.row, 2)

    def test_released_lease_is_free_for_new_owner(self):
        c,r = fixture(2)
        r[1][3] = "RELEASED|OWNER=old"
        self.assertEqual(claim(commercial=c, runtime=r).task_id, "P001")

    def test_naive_time_rejected(self):
        with self.assertRaisesRegex(UnsafeSnapshot, "UTC_AWARE_TIMESTAMP_REQUIRED"):
            claim(now=datetime(2026,10,20,14,30))

    def test_invalid_head_rejected(self):
        with self.assertRaisesRegex(UnsafeSnapshot, "INVALID_HEAD"):
            claim(head="not-a-commit")

    def test_pipe_in_runid_rejected(self):
        with self.assertRaisesRegex(UnsafeSnapshot, "INVALID_RUN_ID"):
            claim(run_id="a|b")

    def test_missing_dependency_fail_closed(self):
        c,r=fixture(2)
        r[1][9]="NOT_FOUND"
        with self.assertRaisesRegex(UnsafeSnapshot, "UNKNOWN_DEPENDENCY"):
            claim(commercial=c, runtime=r)

    def test_claim_readback_exact_match_required(self):
        k=claim()
        good={"replies":[{"createDeveloperMetadata":{"developerMetadata":confirmed_meta(k)}}]}
        self.assertTrue(verify_claim_response(k,good,claimed_runtime(k)))
        with self.assertRaisesRegex(UnsafeSnapshot,"CAS_RUNTIME_READBACK_MISMATCH"):
            verify_claim_response(k,good,[k.task_id,"EN_PROGRESO","other","ACTIVE|TOKEN=bad"])
        bad={"replies":[{"createDeveloperMetadata":{"developerMetadata":{**confirmed_meta(k),"metadataId":999}}}]}
        with self.assertRaisesRegex(UnsafeSnapshot,"CAS_METADATA_MISMATCH"):
            verify_claim_response(k,bad,claimed_runtime(k))

    def test_collision_does_not_count_as_success(self):
        k=claim()
        with self.assertRaisesRegex(UnsafeSnapshot,"NO_CAS_METADATA_CONFIRMATION"):
            verify_claim_response(k,{"error":{"code":400}},claimed_runtime(k))

    def test_release_fails_wrong_owner_and_token(self):
        k=claim()
        with self.assertRaisesRegex(UnsafeSnapshot,"RELEASE_OWNER_TOKEN_MISMATCH"):
            build_release(k, current_runtime_row=[k.task_id,"EN_PROGRESO","someoneelse","ACTIVE|TOKEN=other"],
                captured_metadata=confirmed_meta(k), now=NOW, final_state="VALIDANDO",
                evidence="https://github.com/solqaryn/Solqaryn/pull/3691",
                result="REVIEW_FIRST_PENDING", details="handoff",
                runtime_sheet_id=2017583524, events_sheet_id=958655496)

    def test_release_refuses_fake_listo(self):
        k=claim()
        with self.assertRaisesRegex(UnsafeSnapshot,"UNAUTHORIZED_FINAL_STATE"):
            build_release(k, current_runtime_row=claimed_runtime(k),
                captured_metadata=confirmed_meta(k), now=NOW, final_state="LISTO",
                evidence="", result="FAKE", details="",
                runtime_sheet_id=2017583524, events_sheet_id=958655496)

    def test_release_uses_exact_metadata_filter_and_handoff(self):
        k=claim()
        rel=build_release(k,current_runtime_row=claimed_runtime(k),
            captured_metadata=confirmed_meta(k),now=NOW+timedelta(minutes=2),
            final_state="VALIDANDO",evidence="https://github.com/solqaryn/Solqaryn/pull/3691",
            result="REVIEW_FIRST_PENDING",details="material evidence saved",
            runtime_sheet_id=2017583524,events_sheet_id=958655496)
        self.assertEqual(len(rel),5)
        filt=rel[0]["deleteDeveloperMetadata"]["dataFilter"]["developerMetadataLookup"]
        self.assertEqual(filt["metadataId"],k.metadata_id)
        self.assertEqual(filt["metadataValue"],k.metadata_value)
        self.assertEqual(
            rel[1]["updateCells"]["rows"][0]["values"][0]["userEnteredValue"]["stringValue"],
            "VALIDANDO")

    def test_release_confirmed_only_on_exact_deletion(self):
        k=claim()
        good={"replies":[{"deleteDeveloperMetadata":{"deletedDeveloperMetadata":[confirmed_meta(k)]}}]}
        row=[k.task_id,"VALIDANDO","","RELEASED|TOKEN="+k.token]
        self.assertTrue(verify_release_response(k,good,row))
        with self.assertRaisesRegex(UnsafeSnapshot,"RELEASE_DID_NOT_DELETE_EXACT_LOCK"):
            verify_release_response(k,{"replies":[{"deleteDeveloperMetadata":{"deletedDeveloperMetadata":[]}}]},row)

    def test_parallel_ten_have_distinct_fixed_metadata_ids(self):
        c, r = fixture(32)
        issued = []
        for i in range(1, 11):
            owner_i = f"6aa15346f5408191bdd9043fd26ff7{i:02x}"
            k = claim(commercial=c, runtime=r, chosen=f"P{i:03d}", owner=owner_i)
            issued.append(k.metadata_id)
            r[i][1] = "EN_PROGRESO"
            r[i][2] = k.owner
            r[i][3] = "ACTIVE|TOKEN=" + k.token
        self.assertEqual(issued, [METADATA_BASE + i for i in range(1, 11)])
        self.assertEqual(len(set(issued)), 10)


# Independent reviewer path, including CAS ownership and LISTO acceptance.
REVIEWER = "6abd6235e100819195785fc76a44a8e0"


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
        c,r = review_fixture()
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
        self.assertEqual(len(k.requests), 4)
        self.assertEqual(list(k.requests[0]), ["createDeveloperMetadata"])
        self.assertEqual(k.metadata_value.split("|")[-1], OWNER)
        self.assertEqual(k.requests[1]["updateCells"]["rows"][0]["values"][0]["userEnteredValue"]["stringValue"], "VALIDANDO")
        event = k.requests[3]["appendCells"]["rows"][0]["values"]
        self.assertEqual(event[3]["userEnteredValue"]["stringValue"], "REVIEW_LEASE_ACQUIRED")
        self.assertNotIn("LISTO", event[8]["userEnteredValue"]["stringValue"])

    def test_review_must_have_original_handoff_with_evidence(self):
        c,r=review_fixture()
        r[1][5]=""
        with self.assertRaisesRegex(UnsafeSnapshot,"REVIEW_MISSING_ORIGINAL_EVIDENCE"):
            review_claim(c=c,r=r)
        c,r=review_fixture()
        r[1][3]="ACTIVE|OWNER="+OWNER
        with self.assertRaisesRegex(UnsafeSnapshot,"REVIEW_MISSING_RELEASED_HANDOFF"):
            review_claim(c=c,r=r)

    def test_review_rejects_original_author_even_if_validating(self):
        with self.assertRaisesRegex(UnsafeSnapshot, "SELF_REVIEW_FORBIDDEN"):
            review_claim(reviewer=OWNER)

    def test_review_cannot_skip_lower_order_pending(self):
        c,r=review_fixture(n=2,task_index=2)
        with self.assertRaisesRegex(UnsafeSnapshot, "OUT_OF_ORDER_REVIEW_CLAIM"):
            review_claim(c=c,r=r,task_id="P002")

    def test_review_cannot_skip_other_lower_review(self):
        c,r=review_fixture(n=2)
        r[2]=r[1].copy()
        r[2][0]="P002"
        with self.assertRaisesRegex(UnsafeSnapshot, "OUT_OF_ORDER_REVIEW_CLAIM"):
            review_claim(c=c,r=r,task_id="P002")

    def test_review_readback_checks_token_and_metadata_exactly(self):
        k = review_claim()
        good = {"replies":[{"createDeveloperMetadata":{"developerMetadata":confirmed_meta(k)}}]}
        self.assertTrue(verify_review_claim_response(k, good, review_row(k)))
        with self.assertRaisesRegex(UnsafeSnapshot,"REVIEW_LEASE_MISMATCH"):
            verify_review_claim_response(k,good,["P001","VALIDANDO",k.owner,"ACTIVE_REVIEW|TOKEN=wrong"])
        with self.assertRaisesRegex(UnsafeSnapshot,"NO_REVIEW_CAS_CONFIRMATION"):
            verify_review_claim_response(k,{},review_row(k))

    def test_listo_requires_positive_causal_proofs(self):
        k=review_claim()
        opts=dict(
            current_runtime_row=review_row(k), captured_metadata=confirmed_meta(k),
            now=NOW+timedelta(minutes=2), final_state="LISTO",
            review_evidence="https://github.com/solqaryn/Solqaryn/pull/3691",
            ci_evidence="https://github.com/solqaryn/Solqaryn/actions/runs/123",
            review_details="Checked exact-head with independent diff and negative tests",
            p0=0, p1=0,
            runtime_sheet_id=2017583524,events_sheet_id=958655496,
        )
        self.assertEqual(build_review_completion(k,**opts)[1]["updateCells"]["rows"][0]["values"][0]["userEnteredValue"]["stringValue"],"LISTO")
        with self.assertRaisesRegex(UnsafeSnapshot,"REVIEW_P0_P1_NOT_ZERO"):
            build_review_completion(k,**{**opts,"p0":1})
        with self.assertRaisesRegex(UnsafeSnapshot,"REVIEW_CI_PROOF_REQUIRED"):
            build_review_completion(k,**{**opts,"ci_evidence":"not verified"})
        with self.assertRaisesRegex(UnsafeSnapshot,"REVIEW_PROOF_REQUIRED"):
            build_review_completion(k,**{**opts,"review_evidence":""})
        with self.assertRaisesRegex(UnsafeSnapshot,"REVIEW_LEASE_MISMATCH"):
            build_review_completion(k,**{**opts,"current_runtime_row":["P001","VALIDANDO","someoneelse","ACTIVE"]})

    def test_release_preserves_original_author_and_original_evidence(self):
        k=review_claim()
        rel=build_review_completion(
            k,current_runtime_row=review_row(k),captured_metadata=confirmed_meta(k),
            now=NOW+timedelta(minutes=2),final_state="LISTO",
            review_evidence="https://github.com/solqaryn/Solqaryn/pull/3691",
            ci_evidence="https://github.com/solqaryn/Solqaryn/actions/runs/123",
            review_details="Verified",p0=0,p1=0,
            runtime_sheet_id=2017583524,events_sheet_id=958655496,
        )
        self.assertEqual(list(rel[0]),["deleteDeveloperMetadata"])
        row=rel[1]["updateCells"]["rows"][0]["values"]
        self.assertIn("ORIGINAL_AUTHOR="+OWNER,row[2]["userEnteredValue"]["stringValue"])
        self.assertEqual(rel[1]["updateCells"]["range"]["endColumnIndex"],4)
        self.assertEqual(rel[-1]["appendCells"]["rows"][0]["values"][3]["userEnteredValue"]["stringValue"],"REVIEW_FIRST_ACCEPTED_LISTO")

    def test_failed_review_releases_validando_not_fakes_listo(self):
        k=review_claim()
        rel=build_review_completion(
            k,current_runtime_row=review_row(k),captured_metadata=confirmed_meta(k),
            now=NOW+timedelta(minutes=2),final_state="VALIDANDO",
            review_evidence="https://github.com/solqaryn/Solqaryn/pull/3691",
            ci_evidence="",review_details="Negative test failed; author must fix",
            p0=1,p1=0,runtime_sheet_id=2017583524,events_sheet_id=958655496,
        )
        self.assertEqual(rel[1]["updateCells"]["rows"][0]["values"][0]["userEnteredValue"]["stringValue"],"VALIDANDO")
        self.assertEqual(rel[-1]["appendCells"]["rows"][0]["values"][3]["userEnteredValue"]["stringValue"],"REVIEW_FIRST_NOT_APPROVED")

    def test_review_finish_requires_exact_single_lock_removed_and_readback(self):
        k=review_claim()
        ok={"replies":[{"deleteDeveloperMetadata":{"deletedDeveloperMetadata":[confirmed_meta(k)]}}]}
        row=["P001","LISTO","","RELEASED|ORIGINAL_AUTHOR="+OWNER+"|TOKEN="+k.token]
        self.assertTrue(verify_review_completion_response(k,ok,row,"LISTO"))
        with self.assertRaisesRegex(UnsafeSnapshot,"REVIEW_RELEASE_WRONG_LOCK"):
            verify_review_completion_response(k,{"replies":[{"deleteDeveloperMetadata":{"deletedDeveloperMetadata":[]}}]},row,"LISTO")
        with self.assertRaisesRegex(UnsafeSnapshot,"REVIEW_COMPLETION_READBACK_MISMATCH"):
            verify_review_completion_response(k,ok,["P001","VALIDANDO","other","ACTIVE"],"LISTO")


class StaleLeaseRecoveryAndTakeoverTests(unittest.TestCase):
    def test_double_owner_wip_rejected(self):
        c, r = fixture(2)
        r[1][1] = "EN_PROGRESO"
        r[1][2] = OWNER
        r[1][3] = f"ACTIVE|OWNER={OWNER}|TOKEN=tok1|RUN_ID=run1|EXPIRES=2026-10-20T15:00:00Z|HEAD={HEAD}"
        with self.assertRaisesRegex(UnsafeSnapshot, "EXECUTOR_WIP_LIMIT_EXCEEDED"):
            claim(commercial=c, runtime=r, chosen="P002", owner=OWNER)

    def test_unexpired_lease_recovery_rejected(self):
        c, r = fixture(2)
        r[1][1] = "EN_PROGRESO"
        r[1][2] = OWNER
        expires = (NOW + timedelta(minutes=5)).isoformat().replace("+00:00", "Z")
        r[1][3] = f"ACTIVE|OWNER={OWNER}|TOKEN=tok1|RUN_ID=run1|START=2026-10-20T14:25:00Z|EXPIRES={expires}|HEAD={HEAD}"
        with self.assertRaisesRegex(UnsafeSnapshot, "LEASE_NOT_EXPIRED"):
            build_stale_lease_recovery(
                c, r, task_id="P001", recovered_by=REVIEWER, run_id="REC_RUN_1",
                head=HEAD, now=NOW, reason="TEST_RECOVERY",
                runtime_sheet_id=2017583524, events_sheet_id=958655496,
            )

    def test_expired_lease_fenced_recovery(self):
        c, r = fixture(2)
        r[1][1] = "EN_PROGRESO"
        r[1][2] = OWNER
        expired = (NOW - timedelta(minutes=5)).isoformat().replace("+00:00", "Z")
        r[1][3] = f"ACTIVE|OWNER={OWNER}|TOKEN=tok1|RUN_ID=run1|START=2026-10-20T14:15:00Z|EXPIRES={expired}|HEAD={HEAD}"
        rec = build_stale_lease_recovery(
            c, r, task_id="P001", recovered_by=REVIEWER, run_id="REC_RUN_1",
            head=HEAD, now=NOW, reason="CRASHED_AGENT_TTL_EXPIRED",
            runtime_sheet_id=2017583524, events_sheet_id=958655496,
        )
        self.assertEqual(rec.task_id, "P001")
        self.assertEqual(rec.stale_owner, OWNER)
        self.assertEqual(rec.stale_token, "tok1")
        self.assertEqual(rec.target_state, "PENDIENTE")
        # 1 delete task metadata, 1 delete executor metadata, 3 updateCells, 1 appendCells = 6
        self.assertEqual(len(rec.requests), 6)
        self.assertEqual(list(rec.requests[0]), ["deleteDeveloperMetadata"])
        self.assertEqual(list(rec.requests[1]), ["deleteDeveloperMetadata"])
        # Verify readback passes
        readback_row = [
            "P001", "PENDIENTE", "",
            f"RECOVERED|STALE_OWNER={OWNER}|STALE_TOKEN=tok1|FENCING_TOKEN={rec.fencing_token}|RECOVERED_BY={REVIEWER}|RECOVERED_AT={rec.requests[2]['updateCells']['rows'][0]['values'][2]['userEnteredValue']['stringValue'].split('RECOVERED_AT=')[1].split('|')[0]}|HEAD={HEAD}",
        ]
        api_rep = {"replies": [{"deleteDeveloperMetadata": {"deletedDeveloperMetadata": [{"metadataId": rec.metadata_id}]}}]}
        self.assertTrue(verify_stale_lease_recovery_response(rec, api_rep, readback_row))

    def test_crashed_owner_locked_out_by_fencing(self):
        # Initial claim
        c, r = fixture(2)
        k = claim(commercial=c, runtime=r, chosen="P001", owner=OWNER, now=NOW)
        r[1][1] = "EN_PROGRESO"
        r[1][2] = k.owner
        r[1][3] = f"ACTIVE|OWNER={k.owner}|TOKEN={k.token}|RUN_ID={k.run_id}|START={k.acquired_at}|EXPIRES={k.expires_at}|HEAD={k.head}"

        # Stale recovery by supervisor at T + 15 min
        later = NOW + timedelta(minutes=15)
        rec = build_stale_lease_recovery(
            c, r, task_id="P001", recovered_by=REVIEWER, run_id="REC_RUN_2",
            head=HEAD, now=later, reason="RUNNER_TIMEOUT",
            runtime_sheet_id=2017583524, events_sheet_id=958655496,
        )
        # Runtime row updated after recovery
        fenced_row = [
            "P001", "PENDIENTE", "",
            f"RECOVERED|STALE_OWNER={k.owner}|STALE_TOKEN={k.token}|FENCING_TOKEN={rec.fencing_token}|RECOVERED_BY={REVIEWER}|HEAD={HEAD}",
        ]
        # Late release attempt by original crashed owner must fail
        with self.assertRaisesRegex(UnsafeSnapshot, "RELEASE_OWNER_TOKEN_MISMATCH"):
            build_release(
                k, current_runtime_row=fenced_row,
                captured_metadata=confirmed_meta(k), now=later,
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
                runtime_sheet_id=2017583524, events_sheet_id=958655496,
            )

    def test_takeover_claim_success_and_readback(self):
        c, r = fixture(2)
        r[1][1] = "EN_PROGRESO"
        r[1][2] = OWNER
        expired = (NOW - timedelta(minutes=5)).isoformat().replace("+00:00", "Z")
        r[1][3] = f"ACTIVE|OWNER={OWNER}|TOKEN=tok_old|RUN_ID=run_old|START=2026-10-20T14:15:00Z|EXPIRES={expired}|HEAD={HEAD}"
        tk = build_takeover_claim(
            c, r, task_id="P001", new_owner=REVIEWER, run_id="TAKEOVER_1",
            head=HEAD, now=NOW, reason="PRIMARY_STALLED",
            runtime_sheet_id=2017583524, events_sheet_id=958655496,
        )
        self.assertEqual(tk.task_id, "P001")
        self.assertEqual(tk.owner, REVIEWER)
        # Response simulation
        api_rep = {
            "replies": [
                {"deleteDeveloperMetadata": {"deletedDeveloperMetadata": [{"metadataId": tk.metadata_id}]}},
                {"createDeveloperMetadata": {"developerMetadata": confirmed_meta(tk)}},
            ]
        }
        tk_runtime = [tk.task_id, "EN_PROGRESO", tk.owner, f"ACTIVE|OWNER={tk.owner}|TOKEN={tk.token}|RUN_ID={tk.run_id}|HEAD={tk.head}"]
        self.assertTrue(verify_takeover_claim_response(tk, api_rep, tk_runtime))

    def test_takeover_claim_rejects_wip_exceeded(self):
        c, r = fixture(2)
        r[1][1] = "EN_PROGRESO"
        r[1][2] = OWNER
        expired = (NOW - timedelta(minutes=5)).isoformat().replace("+00:00", "Z")
        r[1][3] = f"ACTIVE|OWNER={OWNER}|TOKEN=tok_old|EXPIRES={expired}"
        # Make REVIEWER already own P002
        r[2][1] = "EN_PROGRESO"
        r[2][2] = REVIEWER
        r[2][3] = f"ACTIVE|OWNER={REVIEWER}|TOKEN=tok_rev"
        with self.assertRaisesRegex(UnsafeSnapshot, "EXECUTOR_WIP_LIMIT_EXCEEDED"):
            build_takeover_claim(
                c, r, task_id="P001", new_owner=REVIEWER, run_id="TAKEOVER_2",
                head=HEAD, now=NOW, reason="TAKEOVER_ATTEMPT",
                runtime_sheet_id=2017583524, events_sheet_id=958655496,
            )

    def test_dual_exclusivity_creates_two_metadata_locks(self):
        k = claim(enforce_executor_metadata=True)
        self.assertEqual(len(k.requests), 5)
        # req 0: task metadata
        self.assertEqual(k.requests[0]["createDeveloperMetadata"]["developerMetadata"]["metadataKey"], METADATA_KEY)
        # req 1: executor metadata
        self.assertEqual(k.requests[1]["createDeveloperMetadata"]["developerMetadata"]["metadataKey"], EXECUTOR_METADATA_KEY)
        self.assertEqual(k.executor_metadata_id, EXECUTOR_METADATA_BASE + (int(OWNER, 16) % 100_000_000))


if __name__ == "__main__":
    unittest.main()
