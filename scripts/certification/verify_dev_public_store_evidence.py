#!/usr/bin/env python3
"""Fail-closed verification of the reviewed DEV microphase-1 evidence baseline.

This verifier does NOT replay a signature or re-suspend a company. It validates
the GitHub-recorded, owner-attested Render LIVE evidence and checks that the
security-sensitive code is byte-identical to the attested DEV commit. The caller
still performs fresh MySQL and HTTP checks in the same workflow execution.
Historical failed workflow attempts must remain visible, not rewritten as PASS.
"""
from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = ROOT / "docs/certifications/dev-public-store-microfase1.json"
REPO = "solqaryn/Solqaryn"
ATTESTED_SHA = "49195f34d6c00e0c0b7684465619af0a96afeba5"
RUN_ID = 39084418982
COMMENT_ID = 6251764256
AUTHOR_ID = 318818775
SECURITY_PATHS = (
    "backend/src/API/Middleware/UnscopedCommercialTenantGuardMiddleware.cs",
    "backend/src/API/Middleware/PublicStoreProxyProof.cs",
    "backend/src/API/Middleware/PublicStoreProofReplayStore.cs",
    "backend/src/API/Controllers/TiendaBindingController.cs",
    "frontend/server/public-store-proxy-context.js",
)
HMAC_RESULT = (
    "PUBLIC_STORE_HMAC_REPLAY_LIVE=PASS env=DEV first=200 replay=503 "
    "fresh=200 ledger=PASS company_binding=PASS commit=" + ATTESTED_SHA
)
LOG_IDS = (
    "4ae84eb2-c823-4a39-8fb0-c2a5fa2d0967",
    "4dfcfc7c-edc1-425a-bfbd-e5dfc1f71e77",
    "78134dab-d60b-43e0-9982-e426a0cfca1d",
)


def require(condition: bool, reason: str) -> None:
    if not condition:
        raise ValueError(reason)


def validate_manifest(data: dict[str, Any]) -> None:
    require(data.get("schema_version") == 1, "unsupported evidence schema")
    require(data.get("project") == "SOLQARYN", "invalid project")
    require(data.get("repository") == REPO, "foreign repository evidence")
    require(data.get("environment") == "DEV", "foreign environment evidence")
    require(data.get("certificate_scope") ==
            "public_store_binding_identity_isolation_hmac_replay",
            "unexpected certification scope")
    require(data.get("attested_security_commit") == ATTESTED_SHA,
            "unreviewed security baseline")
    historical = data.get("historical_readback", {})
    require(historical.get("workflow_run_id") == RUN_ID,
            "unexpected historical run")
    require(historical.get("run_head") ==
            "db75a59fad08159534ed1c16f17e2445ac4dc26b",
            "historical HEAD mismatch")
    require(historical.get("suspended_attempt") == 5 and
            historical.get("suspended_job_id") == 117567817345,
            "suspended attempt identity mismatch")
    require(historical.get("active_attempt") == 6 and
            historical.get("active_job_id") == 117568173016,
            "reactivation attempt identity mismatch")
    require(historical.get("suspended_result") ==
            "HTTP_SUSPENDED=503 CODE=TENANT_DATA_ISOLATION_REQUIRED",
            "historical suspension result missing")
    require(historical.get("active_result") ==
            "POSITIVE_AB_BINDINGS_VERIFIED=2",
            "historical A/B result missing")
    require(historical.get("historical_workflow_outcome") == "BLOCKED",
            "attempts must retain their actual conclusion")
    render = data.get("render_hmac", {})
    require(render.get("service_id") == "srv-daqvla49v7es738up9vg",
            "unauthorized Render service")
    require(render.get("accepted_replay_fresh") ==
            "first=200 replay=503 fresh=200 ledger=PASS company_binding=PASS",
            "incomplete HMAC evidence")
    require(render.get("verified_log_ids") == list(LOG_IDS),
            "unreviewed Render proof IDs")
    require(render.get("render_rollback_deploy") == "dep-dbaeb65g1s2s73bpkbdg",
            "missing DEV worker shutdown evidence")
    require(render.get("disabled_flag") ==
            "PublicStore__ReplayLiveCertificationOnStartup=false",
            "worker deactivation missing")
    attested = data.get("audited_attestation", {})
    require(attested.get("github_comment_id") == COMMENT_ID and
            attested.get("github_author_id") == AUTHOR_ID and
            attested.get("github_author_login") == "solqaryn-platform" and
            attested.get("github_pr_number") == 3666 and
            attested.get("created_at") == "2026-10-18T15:36:19Z",
            "unreviewed GitHub attestation origin")
    require(data.get("invariants_git_paths") == list(SECURITY_PATHS),
            "security invariant paths must not be weakened")
    require(data.get("policy") == {
        "live_checks_required": True,
        "preserve_historical_failures_as_audit": True,
        "historical_checks_attested_not_executed_in_current_run": True,
        "qa_prod_not_in_scope": True,
    }, "fail-closed certification policy mismatch")


def validate_github_attestation(comment: dict[str, Any],
                                pull: dict[str, Any],
                                data: dict[str, Any]) -> None:
    user = comment.get("user") or {}
    require(comment.get("id") == COMMENT_ID, "GitHub comment ID mismatch")
    require(user.get("login") == "solqaryn-platform" and
            user.get("id") == AUTHOR_ID,
            "GitHub evidence author not authorized")
    require(comment.get("issue_url") ==
            "https://api.github.com/repos/solqaryn/Solqaryn/issues/3666",
            "evidence not attached to the merged PR")
    require(comment.get("created_at") ==
            data["audited_attestation"]["created_at"] and
            comment.get("updated_at") == comment.get("created_at"),
            "attestation missing or edited")
    # GitHub's author_association is contextual/advisory, not a stable
    # authorization claim. The immutable numeric corporate user ID, repo,
    # merged PR and unedited comment remain the mandatory authority checks.
    body = comment.get("body") or ""
    markers = (
        "Microfase 1 DEV: acta de certificación técnica por evidencias compuestas",
        "srv-daqvla49v7es738up9vg",
        HMAC_RESULT,
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
    ) + LOG_IDS
    for marker in markers:
        require(marker in body, "attested source omitted: " + marker[:64])
    require(pull.get("number") == 3666 and
            pull.get("state") == "closed" and
            pull.get("merged") is True and
            pull.get("merge_commit_sha") == ATTESTED_SHA,
            "attestation PR was not merged at certified HEAD")
    require((pull.get("base") or {}).get("ref") == "dev",
            "attestation merged into unexpected branch")


def run_checked(args: list[str], *, timeout: int = 25) -> str:
    result = subprocess.run(args, cwd=ROOT, capture_output=True, text=True,
                            check=False, timeout=timeout)
    if result.returncode != 0:
        # Never echo process stderr: it could accidentally contain credentials.
        safe_kind = args[0] if args[0] in ("git", "gh") else "tool"
        safe_op = args[1] if len(args) > 1 and args[1] in (
            "rev-parse", "merge-base", "diff", "api") else "unknown"
        raise ValueError(f"subprocess_{safe_kind}_{safe_op}_failed")
    return result.stdout.strip()


def verify_commit_continuity(current_sha: str) -> None:
    require(len(current_sha) == 40 and
            all(c in "0123456789abcdef" for c in current_sha),
            "invalid current HEAD")
    require(run_checked(["git", "rev-parse", "HEAD"]) == current_sha,
            "checkout does not match workflow HEAD")
    run_checked(["git", "merge-base", "--is-ancestor", ATTESTED_SHA, current_sha])
    for path in SECURITY_PATHS:
        # No new HMAC attestation is needed for docs/tests-only changes;
        # any actual signer, isolation or binding code change stops this gate.
        run_checked(["git", "diff", "--quiet", ATTESTED_SHA, current_sha,
                     "--", path])


def fetch_github_json(resource: str) -> dict[str, Any]:
    # gh is preinstalled on GitHub-hosted runners. GH_TOKEN is only available
    # in this one read-only step and never written to artifacts or logs.
    raw = run_checked(["gh", "api",
                       "-H", "Accept: application/vnd.github+json", resource])
    doc = json.loads(raw)
    require(isinstance(doc, dict), "invalid GitHub API response")
    return doc


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--provenance-only", action="store_true")
    args = parser.parse_args(argv)
    phase = "SCOPE"
    try:
        require(os.getenv("GITHUB_REPOSITORY") == REPO,
                "repository scope mismatch")
        if args.provenance_only:
            require(os.getenv("GITHUB_EVENT_NAME") == "pull_request" and
                    os.getenv("GITHUB_BASE_REF") == "dev",
                    "PR provenance check restricted to DEV")
        else:
            require(os.getenv("GITHUB_REF_NAME") == "dev" and
                    os.getenv("GITHUB_EVENT_NAME") in ("push", "workflow_dispatch"),
                    "only dispatch/push on dev can certify")
        current = os.environ["GITHUB_SHA"]
        phase = "MANIFEST"
        data = json.loads(MANIFEST.read_text(encoding="utf-8"))
        validate_manifest(data)
        phase = "GIT_SECURITY_CONTINUITY"
        verify_commit_continuity(current)
        phase = "GITHUB_COMMENT_API"
        issue_comment = fetch_github_json(
            f"repos/{REPO}/issues/comments/{COMMENT_ID}")
        phase = "GITHUB_PR_API"
        pull = fetch_github_json(f"repos/{REPO}/pulls/3666")
        phase = "GITHUB_ATTESTATION_CONSISTENCY"
        validate_github_attestation(issue_comment, pull, data)
        if args.provenance_only:
            print("MICROFASE1_PR_PROVENANCE=PASS_VALIDATED")
            print("MICROFASE1_SECURITY_CODE_EQUIVALENCE=PASS")
            return 0
        phase = "WORKFLOW_ENV"
        envfile = os.getenv("GITHUB_ENV")
        require(bool(envfile), "workflow environment not available")
        with open(envfile, "a", encoding="utf-8") as f:
            f.write("MICROFASE1_COMPOSITE_ATTESTATION=PASS_ATTESTED\n")
        print("MICROFASE1_ATTESTATION_ORIGIN=GITHUB_REVIEWED_IMMUTABLE_COMMENT")
        print("MICROFASE1_HISTORICAL_SUSPENSION=PASS_ATTESTED")
        print("MICROFASE1_HMAC_REPLAY=PASS_ATTESTED")
        print("MICROFASE1_SECURITY_CODE_EQUIVALENCE=PASS")
        print("MICROFASE1_EVIDENCE=PASS_ATTESTED")
        return 0
    except (KeyError, ValueError, OSError, subprocess.TimeoutExpired,
            json.JSONDecodeError) as exc:
        print("MICROFASE1_EVIDENCE=BLOCKED", file=sys.stderr)
        print("CHECK=" + phase, file=sys.stderr)
        print("REASON_TYPE=" + type(exc).__name__, file=sys.stderr)
        # Controlled error labels only; no subprocess stderr, token or proof.
        if isinstance(exc, ValueError):
            print("REASON=" + str(exc)[:100], file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
