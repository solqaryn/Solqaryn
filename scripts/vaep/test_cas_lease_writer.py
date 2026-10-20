"""Contract tests: atomic claim, no skips, no false ACTIVE_REAL, safe release."""
import unittest
from datetime import datetime, timedelta, timezone

from cas_lease_writer import (
    METADATA_BASE, METADATA_KEY, TTL_MINUTES, UnsafeSnapshot,
    build_claim, build_release, verify_claim_response, verify_release_response,
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


def claim(*, n=2, commercial=None, runtime=None, chosen="P001", now=NOW, head=HEAD, run_id="RUN_123"):
    c, r = fixture(n)
    return build_claim(
        commercial or c, runtime or r,
        task_id=chosen, owner=OWNER, run_id=run_id, head=head, now=now,
        runtime_sheet_id=2017583524, events_sheet_id=958655496,
        expected_count=len((commercial or c))-1,
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
        c,r=fixture(32)
        issued=[]
        for i in range(1,11):
            k=claim(commercial=c,runtime=r,chosen=f"P{i:03d}")
            issued.append(k.metadata_id)
            r[i][1]="EN_PROGRESO";r[i][2]=k.owner;r[i][3]="ACTIVE|TOKEN="+k.token
        self.assertEqual(issued,[METADATA_BASE+i for i in range(1,11)])
        self.assertEqual(len(set(issued)),10)


if __name__ == "__main__":
    unittest.main()
