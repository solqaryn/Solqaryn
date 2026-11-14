#!/usr/bin/env python3
"""Priority 3 secret scanner.

Scans the current worktree, reachable Git history, and optionally exported
GitHub Actions logs/artifacts. It never prints matched secret material: only the
rule and object/path that triggered the finding.
"""
from __future__ import annotations

import argparse
import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MAX_BYTES = 2 * 1024 * 1024

RULES = [
    ("private-key", re.compile(rb"-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----")),
    ("github-token", re.compile(rb"\bgh[pousr]_[A-Za-z0-9]{30,}\b")),
    ("aws-access-key", re.compile(rb"\bAKIA[0-9A-Z]{16}\b")),
    ("google-api-key", re.compile(rb"\bAIza[0-9A-Za-z_-]{30,}\b")),
    ("stripe-live-key", re.compile(rb"\bsk_live_[0-9A-Za-z]{20,}\b")),
    ("sendgrid-key", re.compile(rb"\bSG\.[A-Za-z0-9_-]{16,}\.[A-Za-z0-9_-]{20,}\b")),
    ("cloudinary-credential-uri", re.compile(
        rb"cloudinary://[A-Za-z0-9_-]{3,}:[A-Za-z0-9_-]{8,}@[A-Za-z0-9_.-]+",
        re.I)),
]

# This immutable historical blob is the P0-008.B negative-test fixture. Its
# token/key-shaped strings are deliberate synthetic inputs, not credentials.
# The exception is limited to those two detector rules, this exact blob, and
# this exact path. The current fixture is assembled at runtime so new versions
# do not need a history exemption.
SYNTHETIC_FIXTURE_HISTORY_ALLOWLIST = {
    (
        "a60e4fb318dae3aad89f502776be01a5f3b8ca85",
        "scripts/verify-p0-008-b-secret-evidence.mjs",
    ): frozenset({"github-token", "private-key"}),
}


def scan_bytes(
    data: bytes,
    label: str,
    findings: list[tuple[str, str]],
    ignored_rules: frozenset[str] = frozenset(),
) -> None:
    if not data or len(data) > MAX_BYTES or b"\0" in data[:4096]:
        return
    for name, pattern in RULES:
        if name not in ignored_rules and pattern.search(data):
            findings.append((name, label))


def scan_tree(path: Path, prefix: str, findings: list[tuple[str, str]]) -> None:
    if not path.exists():
        return
    for item in path.rglob("*"):
        if not item.is_file() or item.is_symlink():
            continue
        try:
            data = item.read_bytes()
        except OSError:
            continue
        scan_bytes(data, f"{prefix}:{item.relative_to(path)}", findings)


def git(*args: str) -> bytes:
    return subprocess.check_output(["git", *args], cwd=ROOT, stderr=subprocess.DEVNULL)


def scan_worktree(findings: list[tuple[str, str]]) -> None:
    for raw in git("ls-files", "-z").split(b"\0"):
        if not raw:
            continue
        rel = raw.decode("utf-8", errors="replace")
        path = ROOT / rel
        try:
            data = path.read_bytes()
        except OSError:
            continue
        scan_bytes(data, f"worktree:{rel}", findings)


def scan_history(findings: list[tuple[str, str]]) -> None:
    seen: set[str] = set()
    for line in git("rev-list", "--objects", "--all").decode("utf-8", errors="replace").splitlines():
        if not line.strip():
            continue
        object_id, _, path = line.partition(" ")
        if object_id in seen:
            continue
        seen.add(object_id)
        try:
            kind = git("cat-file", "-t", object_id).strip()
            if kind != b"blob":
                continue
            size = int(git("cat-file", "-s", object_id).strip())
            if size <= 0 or size > MAX_BYTES:
                continue
            data = git("cat-file", "blob", object_id)
        except (subprocess.CalledProcessError, ValueError):
            continue
        history_path = path or "<path-unknown>"
        ignored_rules = SYNTHETIC_FIXTURE_HISTORY_ALLOWLIST.get(
            (object_id, history_path), frozenset()
        )
        scan_bytes(
            data,
            f"history:{object_id}:{history_path}",
            findings,
            ignored_rules=ignored_rules,
        )


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--external-dir", help="Directory containing exported Actions logs/artifacts")
    args = parser.parse_args()

    findings: list[tuple[str, str]] = []
    scan_worktree(findings)
    scan_history(findings)
    if args.external_dir:
        scan_tree(Path(args.external_dir), "actions", findings)

    unique = sorted(set(findings))
    print(f"PRIORITY3_SECRET_SCAN tracked_and_history=CHECKED external={'CHECKED' if args.external_dir else 'NOT_REQUESTED'} findings={len(unique)}")
    for rule, label in unique:
        print(f"BLOCK {rule} -> {label}")
    if unique:
        print("PRIORITY3_SECRET_SCAN=FAIL (secret values intentionally suppressed)")
        return 1
    print("PRIORITY3_SECRET_SCAN=PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
