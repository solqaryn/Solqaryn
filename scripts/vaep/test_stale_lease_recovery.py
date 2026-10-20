"""Negative and successful stale-lease recovery cases."""
import unittest
from datetime import datetime, timedelta, timezone
from stale_lease_recovery import build_stale_recovery, verify_stale_recovery
from commercial_dispatch import UnsafeSnapshot

NOW = datetime(2026, 10, 20, 18, 0, tzinfo=timezone.utc)
OWNER = "a"*32
REVIEWER = "b"*32
HEAD = "c"*40
START = "2026-10-20T17:20:00.000Z"
EXPIRY = "2026-10-20T17:30:00.000Z"
TOKEN = "token1234"


def sample(review=False):
    ident = "P0-002.B" if review else "P0-003.A"
    state = "VALIDANDO" if review else "EN_PROGRESO"
    lease = ("ACTIVE_REVIEW" if review else "ACTIVE") + f"|OWNER={OWNER}|TOKEN={TOKEN}|EXPIRES={EXPIRY}"
    row = [ident, state, OWNER, lease, HEAD, "https://github.com/solqaryn/Solqaryn/pull/3691", "", "", START, ""]
    parts = [ident,"RUN-1",OWNER,TOKEN,START,EXPIRY]
    if review: parts.append(REVIEWER)
    metadata = {"metadataId":1600000004 if review else 1600000005,
                "metadataKey":"SOLQARYN_TASK_LEASE", "metadataValue":"|".join(parts),
                "visibility":"DOCUMENT",
                "location":{"dimensionRange":{"sheetId":2017583524,
                                              "dimension":"ROWS", "startIndex":4 if review else 5,
                                              "endIndex":5 if review else 6}}}
    opts = dict(runtime_row=row,metadata=metadata,order=4 if review else 5,
                row_index=4 if review else 5,now=NOW,authoritative_run_ended=True,
                no_new_material_progress=True,runtime_sheet_id=2017583524,
                events_sheet_id=958655496,recovery_actor=REVIEWER,head=HEAD,
                expected_last_event_at=START)
    return opts


class StaleRecoveryTests(unittest.TestCase):
    def test_implementation_returns_to_pending_with_audited_exact_delete(self):
        req = build_stale_recovery(**sample())
        self.assertEqual(req[0]["deleteDeveloperMetadata"]["dataFilter"]["developerMetadataLookup"]["metadataId"],1600000005)
        self.assertEqual(req[1]["updateCells"]["rows"][0]["values"][0]["userEnteredValue"]["stringValue"],"PENDIENTE")
        self.assertEqual(req[-1]["appendCells"]["rows"][0]["values"][3]["userEnteredValue"]["stringValue"],"STALE_LOCK_RECOVERED")

    def test_review_remains_validando(self):
        req=build_stale_recovery(**sample(True))
        self.assertEqual(req[1]["updateCells"]["rows"][0]["values"][0]["userEnteredValue"]["stringValue"],"VALIDANDO")

    def test_no_run_end_attestation_blocks(self):
        x=sample()
        x["authoritative_run_ended"]=False
        with self.assertRaisesRegex(UnsafeSnapshot,"INVOCATION_OR_MATERIAL_NOT_PROVEN_ENDED"):
            build_stale_recovery(**x)

    def test_recent_progress_blocks(self):
        x=sample()
        x["expected_last_event_at"]="2026-10-20T17:40:00.000Z"
        with self.assertRaisesRegex(UnsafeSnapshot,"PROGRESS_AFTER_EXPIRY"):
            build_stale_recovery(**x)

    def test_not_expired_or_just_expired_blocks(self):
        x=sample()
        x["now"]=datetime(2026,10,20,17,30,30,tzinfo=timezone.utc)
        with self.assertRaisesRegex(UnsafeSnapshot,"LEASE_NOT_OLD_ENOUGH"):
            build_stale_recovery(**x)

    def test_token_collision_blocks(self):
        x=sample()
        x["runtime_row"][3]=x["runtime_row"][3].replace(TOKEN,"other")
        with self.assertRaisesRegex(UnsafeSnapshot,"METADATA_TOKEN_MISMATCH"):
            build_stale_recovery(**x)

    def test_wrong_metadata_row_blocks(self):
        x=sample()
        x["metadata"]["location"]["dimensionRange"]["startIndex"]=9
        with self.assertRaisesRegex(UnsafeSnapshot,"METADATA_ROW_MISMATCH"):
            build_stale_recovery(**x)

    def test_readback_must_confirm_exact_deletion(self):
        ok={"replies":[{"deleteDeveloperMetadata":{"deletedDeveloperMetadata":[{"metadataId":1600000005}]}}]}
        self.assertTrue(verify_stale_recovery(ok,["P0-003.A","PENDIENTE","","RELEASED_STALE|"],1600000005))
        with self.assertRaisesRegex(UnsafeSnapshot,"WRONG_STALE_LOCK_DELETED"):
            verify_stale_recovery(ok,["P0-003.A","PENDIENTE","","RELEASED_STALE|"],1600000006)


if __name__ == "__main__":
    unittest.main()
