"""Offline fail-closed regression tests for the DEV composite attestation."""
from __future__ import annotations

import json
import os
import unittest
from pathlib import Path
from unittest.mock import patch

import verify_dev_public_store_evidence as cert


class DevPublicStoreEvidenceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.good = json.loads(cert.MANIFEST.read_text(encoding="utf-8"))

    @staticmethod
    def comment_and_pr():
        doc = {
            "id": cert.COMMENT_ID,
            "user": {"login": "solqaryn-platform", "id": cert.AUTHOR_ID},
            "issue_url": "https://api.github.com/repos/solqaryn/Solqaryn/issues/3666",
            "author_association": "MEMBER",
            "created_at": "2026-10-18T15:36:19Z",
            "updated_at": "2026-10-18T15:36:19Z",
            "body": "\n".join((
                "Microfase 1 DEV: acta de certificación técnica por evidencias compuestas",
                "srv-daqvla49v7es738up9vg",
                cert.HMAC_RESULT,
                "run 39084418982",
                "117567817345",
                "117568173016",
                "SUSPENDED_COMPANIES_WITH_ACTIVE_BINDING=1",
                "HTTP_SUSPENDED=503 CODE=TENANT_DATA_ISOLATION_REQUIRED",
                "ACTIVE_DEV_COMPANIES_WITH_STORE=2",
                "POSITIVE_AB_BINDINGS_VERIFIED=2",
                "SECURITY_FAILURE_COUNT=0",
                "MICROFASE1_LIVE_CERT=BLOCKED",
                "HMAC_REPLAY_LIVE=NOT_RUN_REQUIRES_AUTHORIZED_TEST",
                "dep-dbaeb65g1s2s73bpkbdg",
                "PublicStore__ReplayLiveCertificationOnStartup=false",
                *cert.LOG_IDS,
            )),
        }
        pull = {
            "number": 3666,
            "state": "closed",
            "merged": True,
            "merge_commit_sha": cert.ATTESTED_SHA,
            "base": {"ref": "dev"},
        }
        return doc, pull

    def test_manifest_is_reviewed_pinned_baseline(self):
        cert.validate_manifest(self.good)

    def test_wrong_render_source_fails_closed(self):
        d = json.loads(json.dumps(self.good))
        d["render_hmac"]["verified_log_ids"][0] = "unrelated"
        with self.assertRaises(ValueError):
            cert.validate_manifest(d)

    def test_missing_historical_suspension_fails_closed(self):
        d = json.loads(json.dumps(self.good))
        d["historical_readback"]["suspended_job_id"] = 0
        with self.assertRaises(ValueError):
            cert.validate_manifest(d)

    def test_missing_security_file_invalidation_fails_closed(self):
        d = json.loads(json.dumps(self.good))
        d["invariants_git_paths"].pop()
        with self.assertRaises(ValueError):
            cert.validate_manifest(d)

    def test_immutable_comment_merged_pr_accepted(self):
        comment, pull = self.comment_and_pr()
        cert.validate_github_attestation(comment, pull, self.good)

    def test_edited_comment_fails_closed(self):
        comment, pull = self.comment_and_pr()
        comment["updated_at"] = "2026-10-19T10:00:00Z"
        with self.assertRaises(ValueError):
            cert.validate_github_attestation(comment, pull, self.good)

    def test_contextual_association_is_not_an_authority_claim(self):
        comment, pull = self.comment_and_pr()
        # GitHub's association badge can vary by API actor; account ID cannot.
        comment["author_association"] = "NONE"
        cert.validate_github_attestation(comment, pull, self.good)

    def test_wrong_github_author_fails_closed(self):
        comment, pull = self.comment_and_pr()
        comment["user"]["id"] += 1
        with self.assertRaises(ValueError):
            cert.validate_github_attestation(comment, pull, self.good)

    def test_missing_hmac_fresh_step_fails_closed(self):
        comment, pull = self.comment_and_pr()
        comment["body"] = comment["body"].replace("fresh=200", "fresh=UNKNOWN")
        with self.assertRaises(ValueError):
            cert.validate_github_attestation(comment, pull, self.good)

    def test_unmerged_attestation_fails_closed(self):
        comment, pull = self.comment_and_pr()
        pull["merged"] = False
        with self.assertRaises(ValueError):
            cert.validate_github_attestation(comment, pull, self.good)

    def test_byte_identical_guard_required(self):
        observed = []
        def check(cmd, timeout=25):
            observed.append(cmd)
            return cert.ATTESTED_SHA if cmd[1:3] == ["rev-parse", "HEAD"] else ""
        with patch.object(cert, "run_checked", side_effect=check):
            cert.verify_commit_continuity(cert.ATTESTED_SHA)
        self.assertEqual(sum(1 for x in observed if "diff" in x), len(cert.SECURITY_PATHS))
        self.assertIn("merge-base", observed[1])

    def test_guard_changed_fails_closed(self):
        def check(cmd, timeout=25):
            if "rev-parse" in cmd:
                return cert.ATTESTED_SHA
            if "diff" in cmd and cert.SECURITY_PATHS[0] in cmd:
                raise ValueError("verification subprocess returned failure")
            return ""
        with patch.object(cert, "run_checked", side_effect=check):
            with self.assertRaises(ValueError):
                cert.verify_commit_continuity(cert.ATTESTED_SHA)

    def test_wrong_repository_is_not_certifiable(self):
        d = json.loads(json.dumps(self.good))
        d["repository"] = "another-org/project"
        with self.assertRaises(ValueError):
            cert.validate_manifest(d)

    def test_pr_provenance_only_requires_dev_base(self):
        with patch.dict(os.environ, {
            "GITHUB_REPOSITORY": cert.REPO,
            "GITHUB_REF_NAME": "3668/merge",
            "GITHUB_EVENT_NAME": "pull_request",
            "GITHUB_BASE_REF": "main",
            "GITHUB_SHA": cert.ATTESTED_SHA,
        }, clear=True):
            self.assertEqual(cert.main(["--provenance-only"]), 1)

    def test_pr_provenance_only_checks_live_origin_without_env_write(self):
        comment, pull = self.comment_and_pr()
        def fetch(resource):
            return pull if resource.endswith("/pulls/3666") else comment
        with patch.dict(os.environ, {
            "GITHUB_REPOSITORY": cert.REPO,
            "GITHUB_REF_NAME": "3668/merge",
            "GITHUB_EVENT_NAME": "pull_request",
            "GITHUB_BASE_REF": "dev",
            "GITHUB_SHA": cert.ATTESTED_SHA,
        }, clear=True), patch.object(cert, "verify_commit_continuity") as code, \
             patch.object(cert, "fetch_github_json", side_effect=fetch) as api:
            self.assertEqual(cert.main(["--provenance-only"]), 0)
            code.assert_called_once_with(cert.ATTESTED_SHA)
            self.assertEqual(api.call_count, 2)

    def test_no_pr_provenance_flag_on_real_dev_readback(self):
        with patch.dict(os.environ, {
            "GITHUB_REPOSITORY": cert.REPO,
            "GITHUB_REF_NAME": "dev",
            "GITHUB_EVENT_NAME": "push",
            "GITHUB_SHA": cert.ATTESTED_SHA,
        }, clear=True):
            self.assertEqual(cert.main(["--provenance-only"]), 1)


if __name__ == "__main__":
    unittest.main()
